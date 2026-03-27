using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class SaveMigrationRegressionTests
    {
        [Test]
        public void SaveDataMigrationUtility_LegacySnapshot_MigratesToLatestSchema()
        {
            GameData legacy = new GameData
            {
                saveSchemaVersion = 0,
                quickConsumableSlots = null,
                localizationLanguage = 999,
                activeProgressionRoute = string.Empty,
                consumables = null,
                questStates = null
            };

            bool changed = SaveDataMigrationUtility.TryMigrate(legacy, out string summary);

            Assert.IsTrue(changed, "Legacy snapshot should require migration.");
            Assert.AreEqual(SaveDataMigrationUtility.LatestSchemaVersion, legacy.saveSchemaVersion);
            Assert.NotNull(legacy.quickConsumableSlots);
            Assert.GreaterOrEqual(legacy.quickConsumableSlots.Count, 3);
            Assert.IsTrue(Enum.IsDefined(typeof(LocalizationLanguage), legacy.localizationLanguage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(legacy.activeProgressionRoute));
            Assert.IsFalse(string.IsNullOrWhiteSpace(summary));
        }

        [Test]
        public void SaveManager_LoadGame_MigratesLegacyFileToLatestSchema()
        {
            RunWithIsolatedSaveStorage((saveManager, savePath, _) =>
            {
                saveManager.encryptSave = false;
                string legacyJson =
                    "{\"playerLevel\":5,\"currentLevel\":2,\"localizationLanguage\":999,\"activeProgressionRoute\":\"\",\"quickConsumableSlots\":[]}";
                File.WriteAllText(savePath, legacyJson);

                bool loaded = saveManager.LoadGame();

                Assert.IsTrue(loaded, "Legacy save payload should still load and migrate.");
                Assert.NotNull(saveManager.CurrentData);
                Assert.AreEqual(SaveManager.CurrentSaveSchemaVersion, saveManager.CurrentData.saveSchemaVersion);
                Assert.NotNull(saveManager.CurrentData.quickConsumableSlots);
                Assert.GreaterOrEqual(saveManager.CurrentData.quickConsumableSlots.Count, 3);
                Assert.IsTrue(Enum.IsDefined(typeof(LocalizationLanguage), saveManager.CurrentData.localizationLanguage));
                Assert.IsFalse(string.IsNullOrWhiteSpace(saveManager.CurrentData.activeProgressionRoute));
            });
        }

        [Test]
        public void SaveManager_LoadSettings_MigratesLegacySettingsAndAppliesLanguage()
        {
            RunWithIsolatedSaveStorage((saveManager, _, settingsPath) =>
            {
                saveManager.encryptSave = false;
                LocalizationService localization = LocalizationService.Instance;
                Assert.NotNull(localization);
                localization.SetLanguage(LocalizationLanguage.SimplifiedChinese);

                string legacySettingsJson =
                    "{\"musicVolume\":0.42,\"sfxVolume\":0.77,\"localizationLanguage\":1,\"saveSchemaVersion\":0}";
                File.WriteAllText(settingsPath, legacySettingsJson);

                saveManager.LoadSettings();

                Assert.NotNull(saveManager.CurrentData);
                Assert.AreEqual(SaveManager.CurrentSaveSchemaVersion, saveManager.CurrentData.saveSchemaVersion);
                Assert.AreEqual((int)LocalizationLanguage.English, saveManager.CurrentData.localizationLanguage);
                Assert.AreEqual(LocalizationLanguage.English, localization.CurrentLanguage);
                Assert.AreEqual(0.42f, saveManager.CurrentData.musicVolume, 0.0001f);
            });
        }

        [Test]
        public void SaveManager_LoadGame_PrimaryCorrupted_UsesBackupAndMigrates()
        {
            RunWithIsolatedSaveStorage((saveManager, savePath, _) =>
            {
                saveManager.encryptSave = false;
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                File.WriteAllText(savePath, "corrupted-primary-save");

                string backupPath = savePath + ".bak";
                string legacyBackupJson =
                    "{\"playerLevel\":7,\"currentLevel\":4,\"saveSchemaVersion\":0,\"localizationLanguage\":999,\"activeProgressionRoute\":\"\",\"quickConsumableSlots\":[]}";
                File.WriteAllText(backupPath, legacyBackupJson);

                bool loaded = saveManager.LoadGame();

                Assert.IsTrue(loaded, "Load should recover from backup when primary save is corrupted.");
                Assert.NotNull(saveManager.CurrentData);
                Assert.AreEqual(SaveManager.CurrentSaveSchemaVersion, saveManager.CurrentData.saveSchemaVersion);
                Assert.NotNull(saveManager.CurrentData.quickConsumableSlots);
                Assert.GreaterOrEqual(saveManager.CurrentData.quickConsumableSlots.Count, 3);
                Assert.IsTrue(Enum.IsDefined(typeof(LocalizationLanguage), saveManager.CurrentData.localizationLanguage));
                Assert.IsFalse(string.IsNullOrWhiteSpace(saveManager.CurrentData.activeProgressionRoute));
            });
        }

        [Test]
        public void SaveManager_LoadSettings_PrimaryCorrupted_UsesBackupAndMigrates()
        {
            RunWithIsolatedSaveStorage((saveManager, _, settingsPath) =>
            {
                saveManager.encryptSave = false;
                LocalizationService localization = LocalizationService.Instance;
                Assert.NotNull(localization);
                localization.SetLanguage(LocalizationLanguage.SimplifiedChinese);

                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                File.WriteAllText(settingsPath, "corrupted-primary-settings");

                string backupPath = settingsPath + ".bak";
                string legacyBackupJson =
                    "{\"musicVolume\":0.31,\"sfxVolume\":0.66,\"localizationLanguage\":1,\"saveSchemaVersion\":0}";
                File.WriteAllText(backupPath, legacyBackupJson);

                saveManager.LoadSettings();

                Assert.NotNull(saveManager.CurrentData);
                Assert.AreEqual(SaveManager.CurrentSaveSchemaVersion, saveManager.CurrentData.saveSchemaVersion);
                Assert.AreEqual((int)LocalizationLanguage.English, saveManager.CurrentData.localizationLanguage);
                Assert.AreEqual(LocalizationLanguage.English, localization.CurrentLanguage);
                Assert.AreEqual(0.31f, saveManager.CurrentData.musicVolume, 0.0001f);
                Assert.AreEqual(0.66f, saveManager.CurrentData.sfxVolume, 0.0001f);
            });
        }

        private static void RunWithIsolatedSaveStorage(Action<SaveManager, string, string> body)
        {
            SaveManager saveManager = SaveManager.Instance;
            Assert.NotNull(saveManager);

            LocalizationService localizationService = LocalizationService.Instance;
            LocalizationLanguage oldLanguage = localizationService != null
                ? localizationService.CurrentLanguage
                : LocalizationLanguage.SimplifiedChinese;

            string oldSavePath = saveManager.overrideSavePathForTests;
            string oldSettingsPath = saveManager.overrideSettingsPathForTests;
            bool oldEncryptSave = saveManager.encryptSave;

            string tempRoot = Path.Combine(Path.GetTempPath(), $"shrimp-save-migration-{Guid.NewGuid():N}");
            string tempSavePath = Path.Combine(tempRoot, "savegame.dat");
            string tempSettingsPath = Path.Combine(tempRoot, "settings.dat");
            Directory.CreateDirectory(tempRoot);

            try
            {
                saveManager.ConfigureTestStoragePaths(tempSavePath, tempSettingsPath);
                body(saveManager, tempSavePath, tempSettingsPath);
            }
            finally
            {
                saveManager.encryptSave = oldEncryptSave;
                saveManager.ConfigureTestStoragePaths(oldSavePath, oldSettingsPath);

                if (localizationService != null)
                {
                    localizationService.SetLanguage(oldLanguage);
                }

                if (Directory.Exists(tempRoot))
                {
                    try
                    {
                        Directory.Delete(tempRoot, true);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
