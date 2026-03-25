using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class LocalizationCoverageGateValidator
    {
        private const string ValidateMenuPath = "Tools/Productization/P2/Validate Localization Coverage (CSV)";
        private const string ValidateGateMenuPath = "Tools/Productization/P2/Validate Localization Coverage (CI Gate)";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/localization_coverage_gate_report.csv";
        private const string TableAssetPath = "Assets/ThirdPersonController/Resources/Localization/DefaultLocalizationTable.asset";
        private const string LogPrefix = "[LocalizationCoverageGate]";

        private static readonly ModuleCoverageRule[] CoverageRules =
        {
            new ModuleCoverageRule("UI_MainMenu", "ui.main_menu."),
            new ModuleCoverageRule("UI_LevelFlow", "ui.level_flow."),
            new ModuleCoverageRule("UI_HudHints", "ui.hud_hints."),
            new ModuleCoverageRule("UI_SkillBar", "ui.skill_bar."),
            new ModuleCoverageRule("UI_Quest", "ui.quest."),
            new ModuleCoverageRule("UI_Talent", "ui.talent."),
            new ModuleCoverageRule("UI_Economy", "ui.economy_overlay."),
            new ModuleCoverageRule("Boss", "boss.")
        };

        private static readonly string[] CriticalPrefixes =
        {
            "ui.main_menu.",
            "ui.level_flow.",
            "ui.hud_hints.",
            "ui.skill_bar.",
            "ui.quest.",
            "ui.talent.",
            "ui.economy_overlay.",
            "boss."
        };

        private static readonly HashSet<string> AllowSameKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "ui.economy_overlay.quick_slot_hint_default"
        };

        private struct ModuleCoverageRule
        {
            public readonly string module;
            public readonly string prefix;

            public ModuleCoverageRule(string module, string prefix)
            {
                this.module = module;
                this.prefix = prefix;
            }
        }

        private struct ValidationRow
        {
            public string layer;
            public string module;
            public string key;
            public string issueType;
            public int total;
            public int localized;
            public string status;
            public string note;
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(failOnBlocking: false, interactive: true);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(failOnBlocking: true, interactive: false);
        }

        public static void ValidateForBatch()
        {
            Run(failOnBlocking: true, interactive: false);
        }

        private static void Run(bool failOnBlocking, bool interactive)
        {
            var rows = new List<ValidationRow>(256);
            int gapTotal = 0;
            int warningTotal = 0;

            LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TableAssetPath);
            if (table == null)
            {
                rows.Add(new ValidationRow
                {
                    layer = "Summary",
                    module = "Global",
                    key = string.Empty,
                    issueType = "MissingTable",
                    total = 0,
                    localized = 0,
                    status = "Gap",
                    note = $"Localization table missing: {TableAssetPath}"
                });
                gapTotal++;
            }
            else
            {
                ValidateModuleCoverage(table, rows, ref gapTotal);
                ValidateAuditIssues(table, rows, ref gapTotal, ref warningTotal);
            }

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();

            string summary = $"rows={rows.Count} gap={gapTotal} warnings={warningTotal} report={reportPath}";
            Debug.Log($"{LogPrefix} {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Localization Coverage Gate", summary, "OK");
            }

            if (failOnBlocking && gapTotal > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. gap={gapTotal} report={reportPath}");
            }
        }

        private static void ValidateModuleCoverage(LocalizationTable table, List<ValidationRow> rows, ref int gapTotal)
        {
            List<LocalizationEntry> entries = table.entries ?? new List<LocalizationEntry>();
            for (int i = 0; i < CoverageRules.Length; i++)
            {
                ModuleCoverageRule rule = CoverageRules[i];
                int total = 0;
                int localized = 0;

                for (int e = 0; e < entries.Count; e++)
                {
                    LocalizationEntry entry = entries[e];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    {
                        continue;
                    }

                    if (!entry.key.StartsWith(rule.prefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    total++;
                    if (!string.IsNullOrWhiteSpace(entry.zhCN) && !string.IsNullOrWhiteSpace(entry.enUS))
                    {
                        localized++;
                    }
                }

                bool gap = total <= 0 || localized < total;
                if (gap)
                {
                    gapTotal++;
                }

                rows.Add(new ValidationRow
                {
                    layer = "Coverage",
                    module = rule.module,
                    key = rule.prefix,
                    issueType = "ModuleCoverage",
                    total = total,
                    localized = localized,
                    status = gap ? "Gap" : "Ok",
                    note = gap
                        ? (total <= 0
                            ? "No keys found for required prefix."
                            : $"Localized entries incomplete: {localized}/{total}.")
                        : "coverage_ok"
                });
            }
        }

        private static void ValidateAuditIssues(
            LocalizationTable table,
            List<ValidationRow> rows,
            ref int gapTotal,
            ref int warningTotal)
        {
            List<LocalizationAuditIssue> issues = LocalizationTableAudit.Run(table, CriticalPrefixes, AllowSameKeys);
            for (int i = 0; i < issues.Count; i++)
            {
                LocalizationAuditIssue issue = issues[i];
                if (issue == null)
                {
                    continue;
                }

                bool blocking = IsBlockingIssue(issue.type);
                if (blocking)
                {
                    gapTotal++;
                }
                else
                {
                    warningTotal++;
                }

                rows.Add(new ValidationRow
                {
                    layer = "AuditIssue",
                    module = "Global",
                    key = issue.key ?? string.Empty,
                    issueType = issue.type.ToString(),
                    total = 0,
                    localized = 0,
                    status = blocking ? "Gap" : "Ok",
                    note = issue.detail ?? string.Empty
                });
            }

            if (issues.Count == 0)
            {
                rows.Add(new ValidationRow
                {
                    layer = "AuditIssue",
                    module = "Global",
                    key = string.Empty,
                    issueType = "None",
                    total = 0,
                    localized = 0,
                    status = "Ok",
                    note = "no_issues"
                });
            }
        }

        private static bool IsBlockingIssue(LocalizationAuditIssueType type)
        {
            switch (type)
            {
                case LocalizationAuditIssueType.MissingTable:
                case LocalizationAuditIssueType.DuplicateKey:
                case LocalizationAuditIssueType.EmptyKey:
                case LocalizationAuditIssueType.EmptyChinese:
                case LocalizationAuditIssueType.EmptyEnglish:
                case LocalizationAuditIssueType.CriticalChineseNotLocalized:
                case LocalizationAuditIssueType.SuspiciousChineseEncoding:
                case LocalizationAuditIssueType.InvalidFormatTemplate:
                    return true;
                default:
                    return false;
            }
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var builder = new StringBuilder(4096);
            builder.AppendLine("layer,module,key,issue_type,total,localized,status,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                builder
                    .Append(Escape(row.layer)).Append(',')
                    .Append(Escape(row.module)).Append(',')
                    .Append(Escape(row.key)).Append(',')
                    .Append(Escape(row.issueType)).Append(',')
                    .Append(row.total).Append(',')
                    .Append(row.localized).Append(',')
                    .Append(Escape(row.status)).Append(',')
                    .Append(Escape(row.note))
                    .AppendLine();
            }

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
    }
}
