using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class LocalizationEncodingRepairTool
    {
        private const string TableAssetPath = "Assets/ThirdPersonController/Resources/Localization/DefaultLocalizationTable.asset";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/localization_encoding_repair_report.csv";
        private const string LogPrefix = "[LocalizationEncodingRepair]";
        private static readonly Encoding Gb18030 = Encoding.GetEncoding(936);

        private static readonly string[] SuspiciousTokens =
        {
            "�",
            "锟",
            "閿",
            "闁",
            "缇ゆ",
            "鐖嗗",
            "浣嶇",
            "鑱氭",
            "杞诲",
            "宸﹂",
            "閲嶅",
            "鍙抽",
            "棣栭",
            "浠诲",
            "澶╄",
            "鎿嶄綔",
            "娑堣€",
            "鍑绘潃",
            "杩涘害",
            "鍏抽棴",
            "鏈В閿",
            "宸茶В閿",
            "馃",
            "閳",
            "纭"
        };

        private static readonly HashSet<char> SuspiciousChars = new HashSet<char>
        {
            '鍑', '鍙', '鍏', '鏈', '鏃', '寮', '缁', '杩', '娉', '鎺',
            '鎴', '鎹', '瑙', '娓', '鏍', '鐝', '璇', '閫', '瑁', '鏆',
            '鑳', '绗', '纭', '锟'
        };

        private struct RepairRow
        {
            public string key;
            public string status;
            public string oldZh;
            public string newZh;
            public string note;
        }

        [MenuItem("Tools/Productization/P0/Repair Localization Encoding (CSV)")]
        public static void ApplyInteractive()
        {
            Run(interactive: true);
        }

        public static void ApplyForBatch()
        {
            Run(interactive: false);
        }

        private static void Run(bool interactive)
        {
            LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TableAssetPath);
            if (table == null)
            {
                throw new InvalidOperationException($"{LogPrefix} missing table asset: {TableAssetPath}");
            }

            List<RepairRow> rows = new List<RepairRow>(256);
            int fixedCount = 0;

            if (table.entries != null)
            {
                for (int i = 0; i < table.entries.Count; i++)
                {
                    LocalizationEntry entry = table.entries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    {
                        continue;
                    }

                    string oldZh = SafeTrim(entry.zhCN);
                    string repairedZh = RepairText(oldZh);
                    bool changed = !string.Equals(oldZh, repairedZh, StringComparison.Ordinal);
                    bool suspiciousBefore = IsSuspicious(oldZh);
                    bool suspiciousAfter = IsSuspicious(repairedZh);

                    if (changed)
                    {
                        entry.zhCN = repairedZh;
                        fixedCount++;
                    }

                    rows.Add(new RepairRow
                    {
                        key = entry.key,
                        status = changed ? "Fixed" : "Kept",
                        oldZh = oldZh,
                        newZh = repairedZh,
                        note = changed
                            ? (suspiciousAfter ? "changed_but_still_suspicious" : "normalized")
                            : (suspiciousBefore ? "suspicious_not_changed" : "clean")
                    });
                }
            }

            if (fixedCount > 0)
            {
                table.RebuildLookup();
                EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssets();
            }

            string reportPath = WriteCsv(rows, fixedCount);
            AssetDatabase.Refresh();

            string summary = $"rows={rows.Count} fixed={fixedCount} report={reportPath}";
            Debug.Log($"{LogPrefix} {summary}");
            if (interactive)
            {
                EditorUtility.DisplayDialog("Localization Encoding Repair", summary, "OK");
            }
        }

        private static string RepairText(string value)
        {
            string original = SafeTrim(value);
            if (string.IsNullOrEmpty(original))
            {
                return original;
            }

            string best = original;
            int bestScore = ScoreCandidate(original);
            string candidate = original;

            // Some entries were mojibake-converted multiple times; iterate to find best readable form.
            for (int pass = 0; pass < 3; pass++)
            {
                if (!TryRecoverUtf8FromGb18030(candidate, out string recovered))
                {
                    break;
                }

                recovered = SanitizeRecoveredText(recovered);
                int score = ScoreCandidate(recovered);
                if (score > bestScore)
                {
                    best = recovered;
                    bestScore = score;
                }

                candidate = recovered;
            }

            return best;
        }

        private static bool TryRecoverUtf8FromGb18030(string value, out string recovered)
        {
            recovered = value;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            try
            {
                byte[] bytes = Gb18030.GetBytes(value);
                recovered = Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string SanitizeRecoveredText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string normalized = value.Replace("\uFFFD", string.Empty);
            if (ContainsCjk(normalized))
            {
                normalized = normalized.Replace("?", string.Empty);
            }

            while (normalized.Contains("  ", StringComparison.Ordinal))
            {
                normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
            }

            return normalized.Trim();
        }

        private static int ScoreCandidate(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return int.MinValue / 2;
            }

            int cjkCount = 0;
            int suspiciousCount = 0;
            int replacementCount = 0;
            int questionCount = 0;

            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (ch >= '\u4E00' && ch <= '\u9FFF')
                {
                    cjkCount++;
                }

                if (SuspiciousChars.Contains(ch))
                {
                    suspiciousCount++;
                }

                if (ch == '\uFFFD')
                {
                    replacementCount++;
                }

                if (ch == '?')
                {
                    questionCount++;
                }
            }

            int tokenHitCount = 0;
            for (int i = 0; i < SuspiciousTokens.Length; i++)
            {
                string token = SuspiciousTokens[i];
                if (!string.IsNullOrEmpty(token) && value.IndexOf(token, StringComparison.Ordinal) >= 0)
                {
                    tokenHitCount++;
                }
            }

            return (cjkCount * 4)
                - (suspiciousCount * 6)
                - (replacementCount * 10)
                - (questionCount * 8)
                - (tokenHitCount * 12);
        }

        private static bool IsSuspicious(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < SuspiciousTokens.Length; i++)
            {
                string token = SuspiciousTokens[i];
                if (!string.IsNullOrEmpty(token) && value.IndexOf(token, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            if (value.IndexOf('?', StringComparison.Ordinal) >= 0 && ContainsCjk(value))
            {
                return true;
            }

            int suspiciousChars = 0;
            for (int i = 0; i < value.Length; i++)
            {
                if (!SuspiciousChars.Contains(value[i]))
                {
                    continue;
                }

                suspiciousChars++;
                if (suspiciousChars >= 3)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCjk(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (ch >= '\u4E00' && ch <= '\u9FFF')
                {
                    return true;
                }
            }

            return false;
        }

        private static string WriteCsv(List<RepairRow> rows, int fixedCount)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            StringBuilder builder = new StringBuilder(8192);
            builder.AppendLine("key,status,old_zh,new_zh,note");
            for (int i = 0; i < rows.Count; i++)
            {
                RepairRow row = rows[i];
                builder
                    .Append(Escape(row.key)).Append(',')
                    .Append(Escape(row.status)).Append(',')
                    .Append(Escape(row.oldZh)).Append(',')
                    .Append(Escape(row.newZh)).Append(',')
                    .Append(Escape(row.note))
                    .AppendLine();
            }

            builder.Append("summary,")
                .Append("count,")
                .Append(rows.Count)
                .Append(',')
                .Append(fixedCount)
                .Append(',')
                .Append("done")
                .AppendLine();

            File.WriteAllText(fullPath, builder.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            bool needsQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (value.IndexOf('"') >= 0)
            {
                value = value.Replace("\"", "\"\"");
            }

            return needsQuote ? $"\"{value}\"" : value;
        }

        private static string SafeTrim(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        }
    }
}
