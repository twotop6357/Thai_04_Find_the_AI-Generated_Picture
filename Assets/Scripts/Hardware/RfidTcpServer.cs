using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Hardware
{
    /// <summary>One message received from a reader. The receive time is when its last byte arrived.</summary>
    public sealed class RfidFrame
    {
        public string Hex;
        public string RemoteIp;
        public int RemotePort;
        public int LocalPort;
        public long ReceivedTimestamp;
    }

    public sealed class RfidClientInfo
    {
        public string RemoteIp;
        public int RemotePort;
        public int LocalPort;
    }

    /// <summary>
    /// Server accepting connections from readers (TCP clients). TCP has no message boundaries, so a message is
    /// completed by a delimiter or receive gap before delivery, so fragments do not look like other card values.
    /// Uses no Unity API; results are queued for the main thread to collect.
    /// </summary>
    public sealed class RfidTcpServer : IDisposable
    {
        private const int LargeFrameWarningBytes = 64 * 1024;
        private const int BindRetryMilliseconds = 3000;

        private readonly IPAddress address;
        private readonly int[] ports;
        private readonly int gapMicroseconds;
        private readonly byte[] delimiter;
        private readonly RfidDataFormat format;
        private readonly ConcurrentQueue<RfidFrame> frames = new ConcurrentQueue<RfidFrame>();
        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> errors = new ConcurrentQueue<string>();
        private readonly ManualResetEvent stopSignal = new ManualResetEvent(false);
        private readonly object sync = new object();
        private readonly List<TcpListener> listeners = new List<TcpListener>();
        private readonly Dictionary<TcpClient, RfidClientInfo> clients = new Dictionary<TcpClient, RfidClientInfo>();
        private readonly HashSet<int> listeningPorts = new HashSet<int>();
        private readonly List<Thread> threads = new List<Thread>();
        private volatile bool stopping;

        public RfidTcpServer(IPAddress address, IEnumerable<int> ports, int frameGapMilliseconds, byte[] delimiter, RfidDataFormat format)
        {
            this.address = address;
            this.ports = new List<int>(new HashSet<int>(ports)).ToArray();
            gapMicroseconds = Math.Max(1, frameGapMilliseconds) * 1000;
            this.delimiter = delimiter ?? Array.Empty<byte>();
            this.format = format;
        }

        public void Start()
        {
            foreach (int port in ports) StartThread(() => ListenLoop(port), "RfidListen" + port);
        }

        public bool TryDequeueFrame(out RfidFrame frame) => frames.TryDequeue(out frame);
        public bool TryDequeueMessage(out string message) => messages.TryDequeue(out message);
        public bool TryDequeueError(out string error) => errors.TryDequeue(out error);

        public List<RfidClientInfo> GetClients()
        {
            lock (sync) return new List<RfidClientInfo>(clients.Values);
        }

        public bool IsListening(int port)
        {
            lock (sync) return listeningPorts.Contains(port);
        }

        private void StartThread(ThreadStart body, string name)
        {
            var thread = new Thread(body) { IsBackground = true, Name = name };
            lock (sync)
            {
                threads.RemoveAll(item => !item.IsAlive);
                threads.Add(thread);
            }
            thread.Start();
        }

        private void ListenLoop(int port)
        {
            string lastError = null;
            while (!stopping)
            {
                TcpListener listener = null;
                try
                {
                    listener = new TcpListener(address, port);
                    // The port must be reopenable even in TIME_WAIT right after a restart.
                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    listener.Start();
                    lock (sync) { listeners.Add(listener); listeningPorts.Add(port); }
                    lastError = null;
                    messages.Enqueue($"Listening on {address}:{port}.");
                    while (!stopping)
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        StartThread(() => ReceiveLoop(client, port), "RfidClient" + port);
                    }
                }
                catch (Exception error) when (error is SocketException || error is ObjectDisposedException || error is InvalidOperationException)
                {
                    if (!stopping && error.Message != lastError)
                    {
                        lastError = error.Message;
                        errors.Enqueue($"Port {port} is unavailable and will be retried: {error.Message}");
                    }
                }
                finally
                {
                    lock (sync) { if (listener != null) listeners.Remove(listener); listeningPorts.Remove(port); }
                    listener?.Stop();
                }
                if (!stopping) stopSignal.WaitOne(BindRetryMilliseconds);
            }
        }

        private void ReceiveLoop(TcpClient client, int localPort)
        {
            var info = new RfidClientInfo { LocalPort = localPort };
            var buffer = new byte[4096];
            var pending = new List<byte>();
            long lastByte = 0;
            bool warned = false;
            try
            {
                var remote = (IPEndPoint)client.Client.RemoteEndPoint;
                IPAddress remoteAddress = remote.Address.IsIPv4MappedToIPv6 ? remote.Address.MapToIPv4() : remote.Address;
                info.RemoteIp = remoteAddress.ToString();
                info.RemotePort = remote.Port;
                client.NoDelay = true;
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                lock (sync)
                {
                    if (stopping) return;
                    clients[client] = info;
                }
                messages.Enqueue($"Reader connected from {info.RemoteIp}:{info.RemotePort} on port {localPort}.");

                Socket socket = client.Client;
                while (!stopping)
                {
                    if (!socket.Poll(gapMicroseconds, SelectMode.SelectRead))
                    {
                        if (delimiter.Length == 0 && pending.Count > 0)
                        {
                            Publish(pending.ToArray(), info, lastByte);
                            pending.Clear();
                            warned = false;
                        }
                        continue;
                    }

                    int count = socket.Receive(buffer);
                    if (count == 0) break;
                    lastByte = Stopwatch.GetTimestamp();
                    pending.AddRange(new ArraySegment<byte>(buffer, 0, count));
                    if (delimiter.Length > 0) PublishDelimited(pending, info, lastByte);
                    if (!warned && pending.Count > LargeFrameWarningBytes)
                    {
                        warned = true;
                        errors.Enqueue($"A message from {info.RemoteIp} exceeded {LargeFrameWarningBytes} bytes without ending. Check frameGapMilliseconds or frameDelimiterHex.");
                    }
                }
            }
            catch (Exception error) when (error is SocketException || error is ObjectDisposedException || error is InvalidOperationException)
            {
                if (!stopping) messages.Enqueue($"Reader {info.RemoteIp}:{info.RemotePort} connection ended: {error.Message}");
            }
            finally
            {
                // In delimiter mode an unfinished message is a truncated value, so discard it.
                if (!stopping && delimiter.Length == 0 && pending.Count > 0) Publish(pending.ToArray(), info, lastByte);
                bool registered;
                lock (sync) registered = clients.Remove(client);
                client.Close();
                if (registered && !stopping) messages.Enqueue($"Reader disconnected: {info.RemoteIp}:{info.RemotePort}.");
            }
        }

        private void PublishDelimited(List<byte> pending, RfidClientInfo info, long timestamp)
        {
            int start = 0;
            for (int i = 0; i <= pending.Count - delimiter.Length; i++)
            {
                if (!MatchesDelimiter(pending, i)) continue;
                if (i > start) Publish(pending.GetRange(start, i - start).ToArray(), info, timestamp);
                i += delimiter.Length - 1;
                start = i + 1;
            }
            if (start > 0) pending.RemoveRange(0, start);
        }

        private bool MatchesDelimiter(List<byte> data, int index)
        {
            for (int i = 0; i < delimiter.Length; i++)
                if (data[index + i] != delimiter[i]) return false;
            return true;
        }

        private void Publish(byte[] data, RfidClientInfo info, long timestamp)
        {
            foreach (string hex in RfidFrameDecoder.Decode(data, format))
                frames.Enqueue(new RfidFrame
                {
                    Hex = hex,
                    RemoteIp = info.RemoteIp,
                    RemotePort = info.RemotePort,
                    LocalPort = info.LocalPort,
                    ReceivedTimestamp = timestamp,
                });
        }

        public void Dispose()
        {
            stopping = true;
            stopSignal.Set();
            List<Thread> running;
            lock (sync)
            {
                foreach (TcpListener listener in listeners) listener.Stop();
                foreach (TcpClient client in clients.Keys) client.Close();
                clients.Clear();
                running = new List<Thread>(threads);
            }
            foreach (Thread thread in running) thread.Join(500);
            stopSignal.Dispose();
        }
    }
}
