using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Hardware
{
    /// <summary>
    /// Converts RFID values received over TCP to card IDs and feeds RfidInput. Game code need not know the input source.
    /// Releases a reader when no value arrives for releaseSeconds after the last one.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class RfidReaderService : MonoBehaviour
    {
        public enum AddValueResult { Added, AlreadyOnCard, MappedToOtherCard, Ignored, SaveFailed }

        public static RfidReaderService Instance { get; private set; }

        /// <summary>Delivers every non-ignored received value on the main thread, whether or not the reader is assigned.</summary>
        public event Action<RfidFrame> FrameReceived;

        public RfidSettings Settings { get; private set; }
        public bool CanSave { get; private set; }
        public bool IsListening => server != null && Settings != null && server.IsListening(Settings.port);
        public List<RfidClientInfo> Clients => server != null ? server.GetClients() : new List<RfidClientInfo>();

        private readonly Dictionary<string, string> valueToCard = new Dictionary<string, string>();
        private readonly HashSet<string> ignoredValues = new HashSet<string>();
        private readonly Dictionary<RfidReaderSlot, long> lastSeen = new Dictionary<RfidReaderSlot, long>();
        private readonly List<RfidReaderSlot> releasedSlots = new List<RfidReaderSlot>();
        private RfidTcpServer server;
        private string serverSignature;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var root = new GameObject(nameof(RfidReaderService));
            DontDestroyOnLoad(root);
            root.AddComponent<RfidReaderService>();
            root.AddComponent<RfidMappingWindow>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ReloadSettings();
        }

        /// <summary>Rereads the file. Restarts the server only if network settings changed.</summary>
        public void ReloadSettings()
        {
            Settings = RfidSettings.Load(out bool canSave);
            CanSave = canSave;
            RebuildLookups();
            if (isActiveAndEnabled || server != null) RestartServerIfNeeded();
        }

        private void OnEnable()
        {
            if (Instance == this) RestartServerIfNeeded();
        }

        private void OnDisable()
        {
            if (Instance == this) StopServer();
        }

        private void OnDestroy()
        {
            StopServer();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (server == null) return;
            while (server.TryDequeueMessage(out string message)) Debug.Log("[RfidReaderService] " + message, this);
            while (server.TryDequeueError(out string error)) Debug.LogError("[RfidReaderService] " + error, this);
            // Apply values that already arrived first, so a slow frame cannot cause a false release.
            while (server.TryDequeueFrame(out RfidFrame frame)) HandleFrame(frame);
            ReleaseExpiredSlots();
        }

        private void HandleFrame(RfidFrame frame)
        {
            if (ignoredValues.Contains(frame.Hex)) return;
            FrameReceived?.Invoke(frame);
            if (!TryFindSlot(frame, out RfidReaderSlot slot)) return;
            lastSeen[slot] = frame.ReceivedTimestamp;
            // An unregistered value is still a physically placed card, so pass its HEX as is. The game treats it as Unknown.
            RfidInput.NotifyPlaced(slot, GetCardId(frame.Hex) ?? frame.Hex);
        }

        private void ReleaseExpiredSlots()
        {
            long now = Stopwatch.GetTimestamp();
            long timeout = (long)(Settings.releaseSeconds * Stopwatch.Frequency);
            releasedSlots.Clear();
            foreach (KeyValuePair<RfidReaderSlot, long> pair in lastSeen)
                if (now - pair.Value >= timeout) releasedSlots.Add(pair.Key);
            foreach (RfidReaderSlot slot in releasedSlots)
            {
                lastSeen.Remove(slot);
                RfidInput.NotifyRemoved(slot);
            }
        }

        public string GetCardId(string hex) => valueToCard.TryGetValue(hex, out string cardId) ? cardId : null;

        /// <summary>A card can have several registered values. Values registered to another card are not taken over.</summary>
        public AddValueResult AddCardValue(string cardId, string hex)
        {
            if (ignoredValues.Contains(hex)) return AddValueResult.Ignored;
            string current = GetCardId(hex);
            if (current == cardId) return AddValueResult.AlreadyOnCard;
            if (current != null) return AddValueResult.MappedToOtherCard;
            RfidCardMapping card = Settings.GetCard(cardId);
            if (card == null) return AddValueResult.SaveFailed;
            card.hexValues.Add(hex);
            if (Save()) { valueToCard[hex] = cardId; return AddValueResult.Added; }
            card.hexValues.Remove(hex);
            return AddValueResult.SaveFailed;
        }

        public bool RemoveCardValue(string cardId, string hex)
        {
            RfidCardMapping card = Settings.GetCard(cardId);
            if (card == null || !card.hexValues.Remove(hex)) return false;
            if (Save()) { valueToCard.Remove(hex); return true; }
            card.hexValues.Add(hex);
            return false;
        }

        /// <summary>Assigns a connected reader to a game slot and clears any other slot using the same connection.</summary>
        public bool AssignReader(RfidReaderSlot slot, RfidClientInfo client)
        {
            int listenPort = client.LocalPort == Settings.port ? 0 : client.LocalPort;
            foreach (RfidReaderMapping other in Settings.readers)
                if (other.remoteIp == client.RemoteIp && other.listenPort == listenPort)
                {
                    other.remoteIp = "";
                    other.listenPort = 0;
                }
            RfidReaderMapping reader = Settings.GetReader(slot);
            reader.remoteIp = client.RemoteIp;
            reader.listenPort = listenPort;
            lastSeen.Remove(slot);
            RfidInput.NotifyRemoved(slot);
            return Save();
        }

        public bool Matches(RfidReaderSlot slot, string remoteIp, int localPort)
        {
            return Matches(Settings.GetReader(slot), remoteIp, localPort);
        }

        private bool TryFindSlot(RfidFrame frame, out RfidReaderSlot slot)
        {
            foreach (RfidReaderMapping reader in Settings.readers)
                if (Matches(reader, frame.RemoteIp, frame.LocalPort) && Enum.TryParse(reader.slot, out slot))
                    return true;
            slot = default;
            return false;
        }

        /// <summary>All specified conditions (IP and dedicated port) must match. A slot with neither is unassigned.</summary>
        private bool Matches(RfidReaderMapping reader, string remoteIp, int localPort)
        {
            if (reader == null) return false;
            bool hasIp = !string.IsNullOrEmpty(reader.remoteIp);
            bool hasPort = reader.listenPort > 0;
            if (!hasIp && !hasPort) return false;
            if (hasIp && reader.remoteIp != remoteIp) return false;
            return (hasPort ? reader.listenPort : Settings.port) == localPort;
        }

        private bool Save()
        {
            if (CanSave) return Settings.TrySave(out _);
            Debug.LogError($"[RfidReaderService] {RfidSettings.FileName} could not be read, so changes are not saved. Fix the file and reopen the window.", this);
            return false;
        }

        private void RebuildLookups()
        {
            valueToCard.Clear();
            foreach (RfidCardMapping card in Settings.cards)
            {
                if (RfidCardCatalog.GetCardType(card.cardId) == RfidCardType.Unknown) continue;
                foreach (string hex in card.hexValues)
                {
                    if (valueToCard.TryGetValue(hex, out string other) && other != card.cardId)
                        Debug.LogWarning($"[RfidReaderService] Value {hex} is mapped to both {other} and {card.cardId}. Using {other}.", this);
                    else valueToCard[hex] = card.cardId;
                }
            }
            ignoredValues.Clear();
            foreach (string hex in Settings.ignoredHexValues)
                if (!string.IsNullOrEmpty(hex)) ignoredValues.Add(hex);
        }

        private void RestartServerIfNeeded()
        {
            var ports = new List<int> { Settings.port };
            foreach (RfidReaderMapping reader in Settings.readers)
                if (reader.listenPort > 0) ports.Add(reader.listenPort);
            string signature = $"{Settings.listenAddress}|{string.Join(",", ports)}|{Settings.frameGapMilliseconds}|{Settings.frameDelimiterHex}|{Settings.dataFormat}";
            if (server != null && signature == serverSignature) return;
            StopServer();

            if (!IPAddress.TryParse(Settings.listenAddress, out IPAddress address))
            {
                Debug.LogError($"[RfidReaderService] listenAddress '{Settings.listenAddress}' is invalid. Using 0.0.0.0.", this);
                address = IPAddress.Any;
            }
            byte[] delimiter = RfidFrameDecoder.ParseHexBytes(Settings.frameDelimiterHex);
            if (delimiter == null)
            {
                Debug.LogError($"[RfidReaderService] frameDelimiterHex '{Settings.frameDelimiterHex}' is invalid. Using the receive gap instead.", this);
                delimiter = Array.Empty<byte>();
            }
            if (!Enum.TryParse(Settings.dataFormat, true, out RfidDataFormat format))
            {
                Debug.LogError($"[RfidReaderService] dataFormat '{Settings.dataFormat}' is invalid. Using Auto.", this);
                format = RfidDataFormat.Auto;
            }

            server = new RfidTcpServer(address, ports, Settings.frameGapMilliseconds, delimiter, format);
            serverSignature = signature;
            server.Start();
        }

        private void StopServer()
        {
            server?.Dispose();
            server = null;
            serverSignature = null;
        }
    }
}
