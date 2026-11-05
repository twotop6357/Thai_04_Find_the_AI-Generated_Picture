using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HuliacDev.Data;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Adds the inactivity return keys with default values when an existing Settings.json lacks them.
    /// Runs at the earliest initialization stage so it finishes before AppSettingsProvider first reads the file.
    /// Existing values and other content stay as they are; a missing or malformed file is left untouched.
    /// </summary>
    public static class InactivitySettingsFile
    {
        public const int DefaultResetSeconds = 30;
        private const string FileName = "Settings.json";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void EnsureKeys()
        {
            string path = Path.Combine(Application.streamingAssetsPath, FileName);
            if (path.Contains("://") || !File.Exists(path)) return;

            string tempPath = path + ".tmp";
            try
            {
                string json = File.ReadAllText(path);
                JsonUtility.FromJson<Settings>(json);
                string updated = AddMissingKeys(json);
                if (updated == json) return;

                // Replace through a temp file so an interrupted write cannot truncate the existing file.
                File.WriteAllText(tempPath, updated);
                File.Replace(tempPath, path, null);
                Debug.Log($"[InactivitySettingsFile] Added missing inactivity keys to {FileName}.");
            }
            catch (Exception exception)
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
                Debug.LogWarning($"[InactivitySettingsFile] Could not update {FileName}: {exception.Message}");
            }
        }

        /// <summary>Returns the JSON with only the missing keys added at the start of the top-level object.</summary>
        public static string AddMissingKeys(string json)
        {
            var entries = new List<string>();
            if (!HasKey(json, "useInactivityTimer")) entries.Add("\"useInactivityTimer\": true");
            if (!HasKey(json, "resetTime")) entries.Add($"\"resetTime\": {DefaultResetSeconds}");
            int open = json.IndexOf('{');
            if (entries.Count == 0 || open < 0) return json;

            string newLine = json.Contains("\r\n") ? "\r\n" : "\n";
            string body = json.Substring(open + 1);
            string separator = body.TrimStart().StartsWith("}") ? "" : ",";
            string inserted = newLine + "  " + string.Join("," + newLine + "  ", entries) + separator;
            return json.Substring(0, open + 1) + inserted + (separator.Length == 0 ? newLine : "") + body;
        }

        private static bool HasKey(string json, string key) => Regex.IsMatch(json, "\"" + key + "\"\\s*:");
    }
}
