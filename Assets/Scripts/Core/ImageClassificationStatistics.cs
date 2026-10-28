using System;
using System.IO;
using Hardware;
using UnityEngine;

namespace Core
{
    /// <summary>Persistent, per-image counts of submitted reader categories on this device.</summary>
    public sealed class ImageClassificationStatistics
    {
        [Serializable]
        public sealed class Entry
        {
            public string cardId;
            public long ai;
            public long human;
        }

        [Serializable]
        private sealed class Data
        {
            public int version = 1;
            public long rounds;
            public Entry[] entries;
        }

        private readonly string path;
        private Data data;
        private bool available = true;
        public long CompletedRounds => data.rounds;
        public string StoragePath => path;

        public ImageClassificationStatistics(string storagePath = null)
        {
            path = storagePath ?? Path.Combine(Application.persistentDataPath, "image-classification-statistics-v1.json");
            data = Empty();
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return;
            try { data = Read(path); }
            catch (Exception primaryError)
            {
                try
                {
                    data = Read(path + ".bak");
                    Debug.LogWarning("[ClassificationStatistics] Restored the backup after a statistics read failure: " + primaryError.Message);
                }
                catch (Exception backupError)
                {
                    available = false;
                    Debug.LogError("[ClassificationStatistics] Cannot load saved responses. Existing files were preserved. " + backupError.Message);
                }
            }
        }

        public void GetCounts(string cardId, out long ai, out long human)
        {
            var entry = Array.Find(data.entries, item => item.cardId == cardId);
            ai = entry?.ai ?? 0;
            human = entry?.human ?? 0;
        }

        /// <summary>Cards are ordered Ai1..Ai3, Human1..Human3; truth never determines the vote.</summary>
        public bool TryRecordRound(string[] cards)
        {
            if (!available || cards == null || cards.Length != 6) return false;
            var next = JsonUtility.FromJson<Data>(JsonUtility.ToJson(data));
            for (int i = 0; i < cards.Length; i++)
            {
                if (RfidCardCatalog.GetCardType(cards[i]) == RfidCardType.Unknown || Array.IndexOf(cards, cards[i]) != i) return false;
                var entry = Array.Find(next.entries, item => item.cardId == cards[i]);
                if (entry == null) return false;
                if (i < 3) entry.ai++;
                else entry.human++;
            }
            next.rounds++;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(next, true));
                if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
                else File.Move(path + ".tmp", path);
                data = next;
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("[ClassificationStatistics] Could not save this round: " + error.Message);
                return false;
            }
        }

        private static Data Empty()
        {
            var entries = new Entry[6];
            for (int i = 0; i < 6; i++) entries[i] = new Entry
            {
                cardId = i < 3 ? RfidCardCatalog.AiCardIds[i] : RfidCardCatalog.HumanCardIds[i - 3]
            };
            return new Data { entries = entries };
        }

        private static Data Read(string source)
        {
            var saved = JsonUtility.FromJson<Data>(File.ReadAllText(source));
            if (saved == null || saved.version != 1 || saved.rounds < 0 || saved.entries == null || saved.entries.Length != 6)
                throw new InvalidDataException("Unsupported statistics data.");
            var expected = Empty();
            foreach (var entry in expected.entries)
            {
                var matches = Array.FindAll(saved.entries, item => item != null && item.cardId == entry.cardId);
                if (matches.Length != 1 || matches[0].ai < 0 || matches[0].human < 0 ||
                    matches[0].ai > saved.rounds || matches[0].human != saved.rounds - matches[0].ai)
                    throw new InvalidDataException("Invalid per-image response counts.");
            }
            return saved;
        }
    }
}
