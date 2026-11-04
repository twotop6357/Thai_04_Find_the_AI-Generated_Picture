using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Hardware
{
    /// <summary>리더에서 받은 메시지 하나. 수신 시각은 마지막 바이트가 도착한 시점이다.</summary>
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
    /// 리더(TCP Client)의 접속을 받는 서버. TCP는 메시지 경계가 없으므로 구분자 또는 수신 공백으로
    /// 메시지 하나를 완성한 뒤 전달한다. 조각난 값이 다른 카드 값처럼 보이지 않게 하기 위함이다.
    /// Unity API를 쓰지 않으며, 결과는 큐에 쌓아 메인 스레드가 가져간다.
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
                    // 재시작 직후 TIME_WAIT 상태의 포트도 다시 열 수 있어야 한다.
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
                // 구분자 방식에서 끝나지 않은 메시지는 잘린 값이므로 버린다.
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
