using NUnit.Framework;
using System.IO;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class LocalizationServiceRegressionTests
    {
        [Test]
        public void LocalizationService_SwitchLanguage_AndFallback_Works()
        {
            LocalizationService service = LocalizationService.Instance;
            Assert.NotNull(service);

            LocalizationLanguage oldLanguage = service.CurrentLanguage;
            LocalizationTable oldTable = service.table;

            LocalizationTable table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.entries.Add(new LocalizationEntry
            {
                key = "ui.start",
                zhCN = "开始",
                enUS = "Start"
            });
            table.RebuildLookup();

            service.SetTable(table);

            service.SetLanguage(LocalizationLanguage.English);
            Assert.AreEqual("Start", service.Get("ui.start", "fallback"));

            service.SetLanguage(LocalizationLanguage.SimplifiedChinese);
            Assert.AreEqual("开始", service.Get("ui.start", "fallback"));

            Assert.AreEqual("fallback", service.Get("ui.missing", "fallback"));

            service.SetTable(oldTable);
            service.SetLanguage(oldLanguage);
            Object.DestroyImmediate(table);
        }

        [Test]
        public void LocalizationService_MissingCurrentLanguage_FallsBackToEnglish_AndRaisesAudit()
        {
            LocalizationService service = LocalizationService.Instance;
            Assert.NotNull(service);

            LocalizationLanguage oldLanguage = service.CurrentLanguage;
            LocalizationTable oldTable = service.table;
            bool oldLogSetting = service.logMissingLocalization;

            LocalizationTable table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.entries.Add(new LocalizationEntry
            {
                key = "skill.dash.name",
                zhCN = string.Empty,
                enUS = "Dash Strike"
            });
            table.RebuildLookup();

            string capturedKey = string.Empty;
            LocalizationLanguage capturedLanguage = LocalizationLanguage.English;
            string capturedReason = string.Empty;
            int missingCount = 0;

            service.logMissingLocalization = false;
            service.ClearMissingLocalizationAudit();
            service.SetTable(table);
            service.OnMissingLocalization += HandleMissing;

            try
            {
                service.SetLanguage(LocalizationLanguage.SimplifiedChinese);
                string value = service.Get("skill.dash.name", "fallback");

                Assert.AreEqual("Dash Strike", value, "zhCN missing text should fallback to enUS.");
                Assert.AreEqual(1, missingCount, "Fallback should emit one missing localization audit event.");
                Assert.AreEqual("skill.dash.name", capturedKey);
                Assert.AreEqual(LocalizationLanguage.SimplifiedChinese, capturedLanguage);
                Assert.AreEqual("MissingCurrentLanguage_UseEnglishFallback", capturedReason);
            }
            finally
            {
                service.OnMissingLocalization -= HandleMissing;
                service.SetTable(oldTable);
                service.SetLanguage(oldLanguage);
                service.logMissingLocalization = oldLogSetting;
                Object.DestroyImmediate(table);
            }

            void HandleMissing(string key, LocalizationLanguage language, string reason)
            {
                missingCount++;
                capturedKey = key;
                capturedLanguage = language;
                capturedReason = reason;
            }
        }

        [Test]
        public void LocalizationService_DefaultTable_Contains_CoreUiKeys()
        {
            LocalizationService service = LocalizationService.Instance;
            Assert.NotNull(service);

            LocalizationTable oldTable = service.table;
            service.SetTable(null);

            string sentinel = "__missing__";
            string[] keys =
            {
                "ui.main_menu.start_game_button",
                "ui.main_menu.press_to_start_format",
                "ui.main_menu.language.label",
                "ui.skill_bar.legend.crowd_control",
                "ui.skill_bar.attack_hint_format",
                "ui.quest.title",
                "ui.quest.type.kill_enemies",
                "ui.talent.title",
                "ui.talent.inventory.equip",
                "ui.level_flow.prep.start_button",
                "ui.level_flow.result.rewards_title",
                "ui.hud_hints.title",
                "ui.economy_overlay.title",
                "ui.level.complete_title",
                "ui.wave.announcer.wave_format",
                "boss.defeated"
            };

            for (int i = 0; i < keys.Length; i++)
            {
                string value = service.Get(keys[i], sentinel);
                Assert.AreNotEqual(sentinel, value, $"Missing localization key: {keys[i]}");
                Assert.IsFalse(string.IsNullOrEmpty(value), $"Empty localization value: {keys[i]}");
            }

            service.SetTable(oldTable);
        }

        [Test]
        public void SaveManager_Settings_PersistLocalizationLanguage()
        {
            SaveManager saveManager = SaveManager.Instance;
            LocalizationService service = LocalizationService.Instance;
            Assert.NotNull(saveManager);
            Assert.NotNull(service);

            string settingsPath = saveManager.SettingsFilePath;
            bool hadSettingsFile = File.Exists(settingsPath);
            string oldSettingsJson = hadSettingsFile ? File.ReadAllText(settingsPath) : string.Empty;

            LocalizationLanguage oldLanguage = service.CurrentLanguage;
            int oldSavedLanguage = saveManager.CurrentData != null
                ? saveManager.CurrentData.localizationLanguage
                : (int)oldLanguage;

            try
            {
                service.SetLanguage(LocalizationLanguage.English);
                saveManager.SaveSettings();

                service.SetLanguage(LocalizationLanguage.SimplifiedChinese);
                Assert.AreEqual(LocalizationLanguage.SimplifiedChinese, service.CurrentLanguage);

                saveManager.LoadSettings();

                Assert.AreEqual(LocalizationLanguage.English, service.CurrentLanguage);
                Assert.NotNull(saveManager.CurrentData);
                Assert.AreEqual((int)LocalizationLanguage.English, saveManager.CurrentData.localizationLanguage);
            }
            finally
            {
                if (hadSettingsFile)
                {
                    File.WriteAllText(settingsPath, oldSettingsJson);
                }
                else if (File.Exists(settingsPath))
                {
                    File.Delete(settingsPath);
                }

                if (saveManager.CurrentData != null)
                {
                    saveManager.CurrentData.localizationLanguage = oldSavedLanguage;
                }

                service.SetLanguage(oldLanguage);
            }
        }

        [Test]
        public void LocalizationService_PseudoLanguage_UsesPseudoLocalizerOnEnglishSource()
        {
            LocalizationService service = LocalizationService.Instance;
            Assert.NotNull(service);

            LocalizationLanguage oldLanguage = service.CurrentLanguage;
            LocalizationTable oldTable = service.table;

            LocalizationTable table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.entries.Add(new LocalizationEntry
            {
                key = "ui.test.pseudo",
                zhCN = "中文占位",
                enUS = "Press {0} To Start"
            });

            try
            {
                service.SetTable(table);
                service.SetLanguage(LocalizationLanguage.Pseudo);
                string pseudo = service.Get("ui.test.pseudo", "fallback");
                Assert.IsTrue(pseudo.Contains("{0}"), "Pseudo result should preserve format placeholder.");
                Assert.AreNotEqual("Press {0} To Start", pseudo);
                Assert.Greater(pseudo.Length, "Press {0} To Start".Length);
            }
            finally
            {
                service.SetTable(oldTable);
                service.SetLanguage(oldLanguage);
            }
        }

        [Test]
        public void LocalizationService_PseudoLanguage_FallsBackToChineseWhenEnglishMissing()
        {
            LocalizationService service = LocalizationService.Instance;
            Assert.NotNull(service);

            LocalizationLanguage oldLanguage = service.CurrentLanguage;
            LocalizationTable oldTable = service.table;

            LocalizationTable table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.entries.Add(new LocalizationEntry
            {
                key = "ui.test.pseudo.zh_only",
                zhCN = "中文按钮",
                enUS = string.Empty
            });

            try
            {
                service.SetTable(table);
                service.SetLanguage(LocalizationLanguage.Pseudo);
                string pseudo = service.Get("ui.test.pseudo.zh_only", "fallback");
                Assert.IsTrue(pseudo.Contains("中文按钮"), "Pseudo source should fallback to zhCN when enUS is empty.");
                Assert.Greater(pseudo.Length, "中文按钮".Length);
            }
            finally
            {
                service.SetTable(oldTable);
                service.SetLanguage(oldLanguage);
            }
        }
    }
}
