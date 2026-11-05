using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HuliacDev.Data;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// 기존 Settings.json에 비활동 복귀 키가 없으면 기본값으로 추가한다.
    /// AppSettingsProvider가 파일을 처음 읽기 전에 끝나도록 가장 이른 초기화 단계에서 실행한다.
    /// 이미 적힌 값과 나머지 내용은 그대로 두고, 파일이 없거나 형식이 잘못되었으면 건드리지 않는다.
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

                // 쓰는 도중 끊겨도 기존 파일이 잘리지 않도록 임시 파일을 거쳐 교체한다.
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

        /// <summary>최상위 객체의 맨 앞에 없는 키만 추가한 JSON을 반환한다.</summary>
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
