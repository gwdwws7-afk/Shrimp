using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class UIInputLocalizationProductizationValidator
    {
        private const string ValidateMenuPath = "Tools/Productization/P2/Validate UI Input+Localization Productization (CSV)";
        private const string ValidateGateMenuPath = "Tools/Productization/P2/Validate UI Input+Localization Productization (CI Gate)";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/ui_input_localization_productization_report.csv";
        private const string LogPrefix = "[UIInputLocalizationProductizationGate]";

        private struct ValidationRow
        {
            public string checkId;
            public string status;
            public string value;
            public string note;
        }

        private readonly struct ScriptRule
        {
            public readonly string id;
            public readonly string path;
            public readonly string[] requiredTokens;

            public ScriptRule(string id, string path, params string[] requiredTokens)
            {
                this.id = id;
                this.path = path;
                this.requiredTokens = requiredTokens;
            }
        }

        private static readonly ScriptRule[] Rules =
        {
            new ScriptRule(
                "ui_hud_hints_dynamic",
                "Assets/ThirdPersonController/Scripts/UI/UI_HudHints.cs",
                "OnPromptDeviceChanged",
                "GetActionBindingLabel(",
                "Localize(\"ui.hud_hints."),
            new ScriptRule(
                "ui_skill_bar_dynamic",
                "Assets/ThirdPersonController/Scripts/UI/UI_SkillBar.cs",
                "GetActionBindingLabel(",
                "Localize(\"ui.skill_bar."),
            new ScriptRule(
                "ui_economy_overlay_dynamic",
                "Assets/ThirdPersonController/Scripts/UI/UI_EconomyOverlay.cs",
                "GetActionBindingLabel(",
                "Localize(\"ui.economy_overlay."),
            new ScriptRule(
                "ui_talent_overlay_dynamic",
                "Assets/ThirdPersonController/Scripts/UI/UI_TalentEquipmentOverlay.cs",
                "GetActionBindingLabel(",
                "Localize(\"ui.talent."),
            new ScriptRule(
                "ui_level_flow_dynamic",
                "Assets/ThirdPersonController/Scripts/Core/LevelFlowUIController.cs",
                "GetActionBindingLabel(",
                "Localize(\"ui.level_flow."),
            new ScriptRule(
                "ui_main_menu_dynamic",
                "Assets/ThirdPersonController/Scripts/Core/MainMenuController.cs",
                "GetActionBindingLabel(",
                "Localize(\"ui.main_menu.")
        };

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
            var rows = new List<ValidationRow>(Rules.Length + 4);
            int gapTotal = 0;

            for (int i = 0; i < Rules.Length; i++)
            {
                EvaluateRule(Rules[i], rows, ref gapTotal);
            }

            rows.Add(new ValidationRow
            {
                checkId = "summary",
                status = gapTotal > 0 ? "Gap" : "Ok",
                value = $"rules={Rules.Length}",
                note = $"gap={gapTotal}"
            });

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();

            string summary = $"rows={rows.Count} gap={gapTotal} report={reportPath}";
            Debug.Log($"{LogPrefix} {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("UI Input/Localization Productization Gate", summary, "OK");
            }

            if (failOnBlocking && gapTotal > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. gap={gapTotal} report={reportPath}");
            }
        }

        private static void EvaluateRule(ScriptRule rule, List<ValidationRow> rows, ref int gapTotal)
        {
            string fullPath = Path.GetFullPath(rule.path);
            bool exists = File.Exists(fullPath);
            if (!exists)
            {
                gapTotal++;
                rows.Add(new ValidationRow
                {
                    checkId = $"script.exists.{rule.id}",
                    status = "Gap",
                    value = rule.path,
                    note = "script_missing"
                });
                return;
            }

            string source = File.ReadAllText(fullPath);
            var missing = new List<string>(4);
            for (int i = 0; i < rule.requiredTokens.Length; i++)
            {
                string token = rule.requiredTokens[i];
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (source.IndexOf(token, StringComparison.Ordinal) < 0)
                {
                    missing.Add(token);
                }
            }

            bool ok = missing.Count == 0;
            if (!ok)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = $"script.tokens.{rule.id}",
                status = ok ? "Ok" : "Gap",
                value = rule.path,
                note = ok ? "token_set_present" : $"missing: {string.Join(" | ", missing)}"
            });
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var sb = new StringBuilder(1024);
            sb.AppendLine("check_id,status,value,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                sb.Append(Escape(row.checkId)).Append(',')
                    .Append(Escape(row.status)).Append(',')
                    .Append(Escape(row.value)).Append(',')
                    .Append(Escape(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static string Escape(string value)
        {
            string text = value ?? string.Empty;
            bool needsQuote = text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (text.IndexOf('"') >= 0)
            {
                text = text.Replace("\"", "\"\"");
            }

            return needsQuote ? $"\"{text}\"" : text;
        }
    }
}
