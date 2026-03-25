using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class LocalizationQualityGateTests
    {
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

        private static readonly HashSet<string> AllowSameKeys = new HashSet<string>
        {
            "ui.economy_overlay.quick_slot_hint_default"
        };

        [Test]
        public void LocalizationTableAudit_DefaultTable_HasNoBlockingIssues()
        {
            LocalizationTable table = Resources.Load<LocalizationTable>("Localization/DefaultLocalizationTable");
            Assert.NotNull(table, "Default localization table should exist under Resources/Localization.");

            List<LocalizationAuditIssue> issues = LocalizationTableAudit.Run(table, CriticalPrefixes, AllowSameKeys);
            if (issues.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"Localization audit found {issues.Count} issue(s):");
                int count = Mathf.Min(issues.Count, 30);
                for (int i = 0; i < count; i++)
                {
                    LocalizationAuditIssue issue = issues[i];
                    builder.Append("- ");
                    builder.Append(issue.type);
                    builder.Append(" | key=");
                    builder.Append(issue.key);
                    builder.Append(" | ");
                    builder.AppendLine(issue.detail);
                }

                Assert.Fail(builder.ToString());
            }
        }

        [Test]
        public void LocalizationTableAudit_DefaultTable_CoreZhDiffersFromEnglish()
        {
            LocalizationTable table = Resources.Load<LocalizationTable>("Localization/DefaultLocalizationTable");
            Assert.NotNull(table);

            string[] keys =
            {
                "ui.main_menu.start_game_button",
                "ui.main_menu.quit_button",
                "ui.hud_hints.title",
                "ui.skill_bar.legend.crowd_control",
                "ui.skill_bar.legend.burst",
                "ui.skill_bar.legend.mobility",
                "ui.skill_bar.legend.gather",
                "ui.skill_bar.attack_hint_default",
                "ui.skill_bar.attack_hint_format",
                "ui.level_flow.prep.start_button",
                "ui.level_flow.result.victory_title",
                "ui.quest.title",
                "ui.quest.type.kill_enemies",
                "ui.economy_overlay.title",
                "ui.talent.title"
            };

            for (int i = 0; i < keys.Length; i++)
            {
                Assert.IsTrue(table.TryGet(keys[i], out LocalizationEntry entry), $"Missing key: {keys[i]}");
                Assert.NotNull(entry, $"Null entry: {keys[i]}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.zhCN), $"zhCN is empty: {keys[i]}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.enUS), $"enUS is empty: {keys[i]}");
                Assert.AreNotEqual(entry.enUS.Trim(), entry.zhCN.Trim(), $"zhCN still equals enUS for key: {keys[i]}");

                if (keys[i].StartsWith("ui.skill_bar.", System.StringComparison.Ordinal))
                {
                    Assert.IsFalse(ContainsMojibake(entry.zhCN), $"zhCN contains suspicious mojibake token: {keys[i]} | {entry.zhCN}");
                }
            }
        }

        [Test]
        public void LocalizationTableAudit_MojibakeSequence_IsReportedAsSuspiciousChineseEncoding()
        {
            LocalizationTable table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.entries.Add(new LocalizationEntry
            {
                key = "ui.test.mojibake",
                zhCN = "缇ゆ测试",
                enUS = "Level Complete"
            });
            table.RebuildLookup();

            List<LocalizationAuditIssue> issues = LocalizationTableAudit.Run(table, CriticalPrefixes, AllowSameKeys);
            bool found = false;
            for (int i = 0; i < issues.Count; i++)
            {
                LocalizationAuditIssue issue = issues[i];
                if (issue == null)
                {
                    continue;
                }

                if (issue.type == LocalizationAuditIssueType.SuspiciousChineseEncoding
                    && issue.key == "ui.test.mojibake")
                {
                    found = true;
                    break;
                }
            }

            Object.DestroyImmediate(table);
            Assert.IsTrue(found, "Mojibake zhCN should be reported as SuspiciousChineseEncoding.");
        }

        [Test]
        public void LocalizationPseudoLocalizer_PreservesFormatTokens_AndExpandsText()
        {
            const string source = "Press {0} to Continue";
            string pseudo = LocalizationPseudoLocalizer.PseudoLocalize(source);

            Assert.IsFalse(string.IsNullOrWhiteSpace(pseudo));
            Assert.AreNotEqual(source, pseudo);
            Assert.Greater(pseudo.Length, source.Length);
            Assert.IsTrue(pseudo.Contains("{0}"), "Pseudo-localized output must preserve format placeholders.");
        }

        [Test]
        public void LocalizationPseudoLocalizer_DefaultTable_CoreUiKeys_ArePseudoLocReady()
        {
            LocalizationTable table = Resources.Load<LocalizationTable>("Localization/DefaultLocalizationTable");
            Assert.NotNull(table);

            string[] keys =
            {
                "ui.main_menu.start_game_button",
                "ui.level_flow.prep.start_button",
                "ui.hud_hints.title",
                "ui.skill_bar.attack_hint_format",
                "ui.quest.type.kill_enemies",
                "ui.talent.title",
                "ui.economy_overlay.title",
                "boss.break_window"
            };

            for (int i = 0; i < keys.Length; i++)
            {
                Assert.IsTrue(table.TryGet(keys[i], out LocalizationEntry entry), $"Missing key: {keys[i]}");
                Assert.NotNull(entry);
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.enUS), $"enUS is empty: {keys[i]}");

                string pseudo = LocalizationPseudoLocalizer.PseudoLocalize(entry.enUS);
                Assert.IsFalse(string.IsNullOrWhiteSpace(pseudo), $"Pseudo result is empty: {keys[i]}");
                Assert.AreNotEqual(entry.enUS, pseudo, $"Pseudo result equals source: {keys[i]}");
                Assert.Greater(pseudo.Length, entry.enUS.Length, $"Pseudo result should expand text: {keys[i]}");

                if (entry.enUS.Contains("{0}"))
                {
                    Assert.IsTrue(pseudo.Contains("{0}"), $"Pseudo result should preserve '{{0}}': {keys[i]}");
                }
            }
        }

        private static bool ContainsMojibake(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return text.Contains("\uFFFD")
                || text.Contains("锟")
                || text.Contains("閿")
                || text.Contains("闁")
                || text.Contains("鏉")
                || text.Contains("缁")
                || text.Contains("鐖")
                || text.Contains("鍙")
                || text.Contains("杞")
                || text.Contains("纭");
        }
    }
}
