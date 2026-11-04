using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Hardware
{
    [Serializable]
    public sealed class RfidCardMapping
    {
        public string cardId;
        public List<string> hexValues = new List<string>();
    }

    [Serializable]
    public sealed class RfidReaderMapping
    {
        public string slot;
        public string remoteIp = "";
        public int listenPort;
    }

    /// <summary>StreamingAssets/RfidSettings.json의 내용. F10 매핑 창이 같은 파일에 저장한다.</summary>
    [Serializable]
    public sealed class RfidSettings
    {
        public const string FileName = "RfidSettings.json";
        private const float DefaultReleaseSeconds = 1f;
        private const int DefaultPort = 5000;
        private const int DefaultFrameGapMilliseconds = 50;

        public static string FilePath => Path.Combine(Application.streamingAssetsPath, FileName);

        public string listenAddress = "0.0.0.0";
        public int port = DefaultPort;
        public float releaseSeconds = DefaultReleaseSeconds;
        public int frameGapMilliseconds = DefaultFrameGapMilliseconds;
        public string frameDelimiterHex = "";
        public string dataFormat = nameof(RfidDataFormat.Auto);
        public List<string> ignoredHexValues = new List<string>();
        public List<RfidReaderMapping> readers = new List<RfidReaderMapping>();
        public List<RfidCardMapping> cards = new List<RfidCardMapping>();

        /// <summary>파일을 읽는다. 읽지 못하면 기본값을 돌려주고, 사람이 고칠 수 있게 파일은 덮어쓰지 않는다.</summary>
        public static RfidSettings Load(out bool canSave)
        {
            var settings = new RfidSettings();
            canSave = true;
            string path = FilePath;
            if (File.Exists(path))
            {
                try { JsonUtility.FromJsonOverwrite(File.ReadAllText(path), settings); }
                catch (Exception error)
                {
                    canSave = false;
                    settings = new RfidSettings();
                    Debug.LogError($"[RfidSettings] Cannot read {path}. Using defaults and leaving the file unchanged: {error.Message}");
                }
            }
            else Debug.LogWarning($"[RfidSettings] {path} is missing. Using defaults.");
            settings.Normalize();
            return settings;
        }

        /// <summary>임시 파일을 먼저 쓰고 교체해, 저장 중 종료되어도 기존 파일이 깨지지 않게 한다.</summary>
        public bool TrySave(out string error)
        {
            // 점으로 시작하는 파일은 Unity가 임포트하지 않는다.
            string path = FilePath;
            string temporary = Path.Combine(Path.GetDirectoryName(path), "." + FileName + ".tmp");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonUtility.ToJson(this, true));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Debug.LogError($"[RfidSettings] Cannot save {path}: {exception.Message}");
                return false;
            }
        }

        public RfidCardMapping GetCard(string cardId) => cards.Find(card => card.cardId == cardId);
        public RfidReaderMapping GetReader(RfidReaderSlot slot) => readers.Find(reader => reader.slot == slot.ToString());

        private void Normalize()
        {
            if (releaseSeconds <= 0)
            {
                Debug.LogWarning($"[RfidSettings] releaseSeconds must be positive. Using {DefaultReleaseSeconds}.");
                releaseSeconds = DefaultReleaseSeconds;
            }
            if (port < 1 || port > 65535)
            {
                Debug.LogWarning($"[RfidSettings] port {port} is invalid. Using {DefaultPort}.");
                port = DefaultPort;
            }
            if (frameGapMilliseconds < 1) frameGapMilliseconds = DefaultFrameGapMilliseconds;

            if (ignoredHexValues == null) ignoredHexValues = new List<string>();
            for (int i = 0; i < ignoredHexValues.Count; i++) ignoredHexValues[i] = RfidFrameDecoder.NormalizeHex(ignoredHexValues[i]);
            if (readers == null) readers = new List<RfidReaderMapping>();
            if (cards == null) cards = new List<RfidCardMapping>();

            foreach (RfidReaderSlot slot in (RfidReaderSlot[])Enum.GetValues(typeof(RfidReaderSlot)))
                if (GetReader(slot) == null) readers.Add(new RfidReaderMapping { slot = slot.ToString() });
            foreach (RfidReaderMapping reader in readers)
            {
                if (!Enum.TryParse(reader.slot, out RfidReaderSlot _))
                    Debug.LogWarning($"[RfidSettings] Unknown reader slot '{reader.slot}' is ignored.");
                if (reader.remoteIp == null) reader.remoteIp = "";
            }

            EnsureCards(RfidCardCatalog.AiCardIds);
            EnsureCards(RfidCardCatalog.HumanCardIds);
            foreach (RfidCardMapping card in cards)
            {
                if (RfidCardCatalog.GetCardType(card.cardId) == RfidCardType.Unknown)
                    Debug.LogWarning($"[RfidSettings] Unknown card id '{card.cardId}' is ignored.");
                if (card.hexValues == null) card.hexValues = new List<string>();
                for (int i = 0; i < card.hexValues.Count; i++) card.hexValues[i] = RfidFrameDecoder.NormalizeHex(card.hexValues[i]);
                card.hexValues.RemoveAll(string.IsNullOrEmpty);
            }
        }

        private void EnsureCards(string[] cardIds)
        {
            foreach (string cardId in cardIds)
                if (GetCard(cardId) == null) cards.Add(new RfidCardMapping { cardId = cardId });
        }
    }
}
