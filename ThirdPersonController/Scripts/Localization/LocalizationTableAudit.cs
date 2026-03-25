using System;
using System.Collections.Generic;

namespace ThirdPersonController
{
    public enum LocalizationAuditIssueType
    {
        MissingTable,
        DuplicateKey,
        EmptyKey,
        EmptyChinese,
        EmptyEnglish,
        SuspiciousChineseEncoding,
        CriticalChineseNotLocalized,
        InvalidFormatTemplate
    }

    public class LocalizationAuditIssue
    {
        public LocalizationAuditIssueType type;
        public string key;
        public string detail;
    }

    public static class LocalizationTableAudit
    {
        private static readonly object[] FormatValidationArgs = BuildFormatValidationArgs(32);

        private static readonly string[] SuspiciousChineseTokens =
        {
            "�",
            "缇ゆ",
            "鐖嗗",
            "浣嶇",
            "鑱氭",
            "杞诲",
            "宸﹂",
            "閲嶅",
            "鍙抽",
            "闁",
            "鍒",
            "閲",
            "鎶",
            "馃",
            "閳",
            "纭"
        };

        public static List<LocalizationAuditIssue> Run(
            LocalizationTable table,
            IReadOnlyList<string> criticalPrefixes,
            IReadOnlyCollection<string> allowSameChineseEnglishKeys = null)
        {
            List<LocalizationAuditIssue> issues = new List<LocalizationAuditIssue>();
            if (table == null)
            {
                issues.Add(new LocalizationAuditIssue
                {
                    type = LocalizationAuditIssueType.MissingTable,
                    key = string.Empty,
                    detail = "Localization table is null."
                });
                return issues;
            }

            if (table.entries == null)
            {
                return issues;
            }

            HashSet<string> seenKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < table.entries.Count; i++)
            {
                LocalizationEntry entry = table.entries[i];
                if (entry == null)
                {
                    continue;
                }

                string key = entry.key == null ? string.Empty : entry.key.Trim();
                if (string.IsNullOrEmpty(key))
                {
                    issues.Add(new LocalizationAuditIssue
                    {
                        type = LocalizationAuditIssueType.EmptyKey,
                        key = string.Empty,
                        detail = $"Entry index {i} has empty key."
                    });
                    continue;
                }

                if (!seenKeys.Add(key))
                {
                    issues.Add(new LocalizationAuditIssue
                    {
                        type = LocalizationAuditIssueType.DuplicateKey,
                        key = key,
                        detail = "Duplicate localization key."
                    });
                }

                string zh = SafeTrim(entry.zhCN);
                string en = SafeTrim(entry.enUS);

                if (string.IsNullOrEmpty(zh))
                {
                    issues.Add(new LocalizationAuditIssue
                    {
                        type = LocalizationAuditIssueType.EmptyChinese,
                        key = key,
                        detail = "zhCN is empty."
                    });
                }
                else if (ContainsSuspiciousChineseEncoding(zh))
                {
                    issues.Add(new LocalizationAuditIssue
                    {
                        type = LocalizationAuditIssueType.SuspiciousChineseEncoding,
                        key = key,
                        detail = $"zhCN may contain mojibake: {zh}"
                    });
                }
                else if (ContainsFormatBrace(zh) && !IsValidFormatTemplate(zh))
                {
                    issues.Add(new LocalizationAuditIssue
                    {
                        type = LocalizationAuditIssueType.InvalidFormatTemplate,
                        key = key,
                        detail = $"zhCN format template is invalid: {zh}"
                    });
                }

                if (string.IsNullOrEmpty(en))
                {
                    issues.Add(new LocalizationAuditIssue
                    {
                        type = LocalizationAuditIssueType.EmptyEnglish,
                        key = key,
                        detail = "enUS is empty."
                    });
                }
                else if (ContainsFormatBrace(en) && !IsValidFormatTemplate(en))
                {
                    issues.Add(new LocalizationAuditIssue
                    {
                        type = LocalizationAuditIssueType.InvalidFormatTemplate,
                        key = key,
                        detail = $"enUS format template is invalid: {en}"
                    });
                }

                if (IsCriticalKey(key, criticalPrefixes)
                    && !IsAllowSameKey(key, allowSameChineseEnglishKeys)
                    && IsSameLocalizedValue(zh, en))
                {
                    issues.Add(new LocalizationAuditIssue
                    {
                        type = LocalizationAuditIssueType.CriticalChineseNotLocalized,
                        key = key,
                        detail = "Critical key has identical zhCN and enUS."
                    });
                }
            }

            return issues;
        }

        private static bool IsCriticalKey(string key, IReadOnlyList<string> criticalPrefixes)
        {
            if (string.IsNullOrEmpty(key) || criticalPrefixes == null || criticalPrefixes.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < criticalPrefixes.Count; i++)
            {
                string prefix = criticalPrefixes[i];
                if (string.IsNullOrEmpty(prefix))
                {
                    continue;
                }

                if (key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAllowSameKey(string key, IReadOnlyCollection<string> allowSameKeys)
        {
            if (allowSameKeys == null || allowSameKeys.Count == 0 || string.IsNullOrEmpty(key))
            {
                return false;
            }

            foreach (string allowKey in allowSameKeys)
            {
                if (string.Equals(allowKey, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameLocalizedValue(string zh, string en)
        {
            if (string.IsNullOrEmpty(zh) || string.IsNullOrEmpty(en))
            {
                return false;
            }

            return string.Equals(zh.Trim(), en.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsSuspiciousChineseEncoding(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < SuspiciousChineseTokens.Length; i++)
            {
                string token = SuspiciousChineseTokens[i];
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (value.IndexOf(token, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsFormatBrace(string value)
        {
            return !string.IsNullOrEmpty(value)
                && (value.IndexOf('{') >= 0 || value.IndexOf('}') >= 0);
        }

        private static bool IsValidFormatTemplate(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            try
            {
                string.Format(value, FormatValidationArgs);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static object[] BuildFormatValidationArgs(int count)
        {
            int size = Math.Max(1, count);
            object[] values = new object[size];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = i;
            }

            return values;
        }

        private static string SafeTrim(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        }
    }
}

