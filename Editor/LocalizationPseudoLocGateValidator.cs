using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class LocalizationPseudoLocGateValidator
    {
        private const string ValidateMenuPath = "Tools/Productization/P2/Validate Localization Pseudo-Loc (CSV)";
        private const string ValidateGateMenuPath = "Tools/Productization/P2/Validate Localization Pseudo-Loc (CI Gate)";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/localization_pseudoloc_gate_report.csv";
        private const string TableAssetPath = "Assets/ThirdPersonController/Resources/Localization/DefaultLocalizationTable.asset";
        private const string LogPrefix = "[LocalizationPseudoLocGate]";

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
            public int pseudoReady;
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
                    pseudoReady = 0,
                    status = "Gap",
                    note = $"Localization table missing: {TableAssetPath}"
                });
                gapTotal++;
            }
            else
            {
                ValidateModuleCoverage(table, rows, ref gapTotal);
            }

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();

            string summary = $"rows={rows.Count} gap={gapTotal} report={reportPath}";
            Debug.Log($"{LogPrefix} {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Localization Pseudo-Loc Gate", summary, "OK");
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
                int pseudoReady = 0;

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
                    if (IsPseudoLocReady(entry.enUS, out string reason))
                    {
                        pseudoReady++;
                    }
                    else
                    {
                        gapTotal++;
                        rows.Add(new ValidationRow
                        {
                            layer = "AuditIssue",
                            module = rule.module,
                            key = entry.key,
                            issueType = "PseudoLocInvalid",
                            total = 0,
                            pseudoReady = 0,
                            status = "Gap",
                            note = reason
                        });
                    }
                }

                bool gap = total <= 0 || pseudoReady < total;
                if (gap)
                {
                    gapTotal++;
                }

                rows.Add(new ValidationRow
                {
                    layer = "Coverage",
                    module = rule.module,
                    key = rule.prefix,
                    issueType = "PseudoCoverage",
                    total = total,
                    pseudoReady = pseudoReady,
                    status = gap ? "Gap" : "Ok",
                    note = gap
                        ? (total <= 0
                            ? "No keys found for required prefix."
                            : $"Pseudo-loc incomplete: {pseudoReady}/{total}.")
                        : "pseudo_coverage_ok"
                });
            }
        }

        private static bool IsPseudoLocReady(string english, out string reason)
        {
            if (string.IsNullOrWhiteSpace(english))
            {
                reason = "enUS is empty.";
                return false;
            }

            string pseudo = LocalizationPseudoLocalizer.PseudoLocalize(english);
            if (string.IsNullOrWhiteSpace(pseudo))
            {
                reason = "pseudo result is empty.";
                return false;
            }

            if (string.Equals(pseudo, english, StringComparison.Ordinal))
            {
                reason = "pseudo result equals source text.";
                return false;
            }

            if (pseudo.Length <= english.Length)
            {
                reason = "pseudo result did not expand text length.";
                return false;
            }

            List<string> tokens = ExtractFormatTokens(english);
            for (int i = 0; i < tokens.Count; i++)
            {
                if (pseudo.IndexOf(tokens[i], StringComparison.Ordinal) < 0)
                {
                    reason = $"missing format token in pseudo result: {tokens[i]}";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private static List<string> ExtractFormatTokens(string value)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(value))
            {
                return tokens;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '{')
                {
                    continue;
                }

                int end = value.IndexOf('}', i + 1);
                if (end <= i)
                {
                    continue;
                }

                string token = value.Substring(i, end - i + 1);
                if (token.Length > 2)
                {
                    tokens.Add(token);
                }

                i = end;
            }

            return tokens;
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
            builder.AppendLine("layer,module,key,issue_type,total,pseudo_ready,status,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                builder
                    .Append(Escape(row.layer)).Append(',')
                    .Append(Escape(row.module)).Append(',')
                    .Append(Escape(row.key)).Append(',')
                    .Append(Escape(row.issueType)).Append(',')
                    .Append(row.total).Append(',')
                    .Append(row.pseudoReady).Append(',')
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
