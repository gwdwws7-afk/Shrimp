using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class SaveManagerAtomicRegressionTests
    {
        [Test]
        public void SaveManager_SaveGame_SecondWrite_CreatesBackupFile()
        {
            RunWithIsolatedSaveStorage((saveManager, savePath, _) =>
            {
                saveManager.encryptSave = false;
                Assert.NotNull(saveManager.CurrentData);

                saveManager.CurrentData.currentLevel = 3;
                saveManager.SaveGame();

                string backupPath = $"{savePath}.bak";
                Assert.IsTrue(File.Exists(savePath), "Primary save file should exist after first save.");
                Assert.IsFalse(File.Exists(backupPath), "Backup file should not exist before any overwrite.");

                saveManager.CurrentData.currentLevel = 7;
                saveManager.SaveGame();

                Assert.IsTrue(File.Exists(backupPath), "Second save should keep previous snapshot as backup.");
                string primaryJson = File.ReadAllText(savePath);
                string backupJson = File.ReadAllText(backupPath);
                Assert.AreNotEqual(primaryJson, backupJson, "Backup content should preserve previous snapshot.");
            });
        }

        [Test]
        public void SaveManager_LoadSettings_FallsBackToBackup_WhenPrimaryCorrupted()
        {
            RunWithIsolatedSaveStorage((saveManager, _, settingsPath) =>
            {
                LocalizationService service = LocalizationService.Instance;
                Assert.NotNull(service);

                saveManager.encryptSave = false;
                Assert.NotNull(saveManager.CurrentData);

                service.SetLanguage(LocalizationLanguage.English);
                AudioManager audioManager = AudioManager.Instance;
                Assert.NotNull(audioManager);
                audioManager.SetMusicVolume(0.35f);
                saveManager.CurrentData.localizationLanguage = (int)LocalizationLanguage.English;
                saveManager.SaveSettings();

                string backupPath = $"{settingsPath}.bak";
                File.Copy(settingsPath, backupPath, true);
                File.WriteAllText(settingsPath, "{bad_json");

                saveManager.CurrentData.localizationLanguage = (int)LocalizationLanguage.SimplifiedChinese;
                saveManager.CurrentData.musicVolume = 0.8f;
                service.SetLanguage(LocalizationLanguage.SimplifiedChinese);

                saveManager.LoadSettings();

                Assert.AreEqual((int)LocalizationLanguage.English, saveManager.CurrentData.localizationLanguage);
                Assert.AreEqual(LocalizationLanguage.English, service.CurrentLanguage);
                Assert.AreEqual(0.35f, saveManager.CurrentData.musicVolume, 0.0001f);
            });
        }

        private static void RunWithIsolatedSaveStorage(Action<SaveManager, string, string> body)
        {
            SaveManager saveManager = SaveManager.Instance;
            Assert.NotNull(saveManager, "SaveManager singleton should be available.");

            LocalizationService localizationService = LocalizationService.Instance;
            LocalizationLanguage oldLanguage = localizationService != null
                ? localizationService.CurrentLanguage
                : LocalizationLanguage.SimplifiedChinese;

            string oldSavePath = saveManager.overrideSavePathForTests;
            string oldSettingsPath = saveManager.overrideSettingsPathForTests;
            bool oldEncryptSave = saveManager.encryptSave;

            string tempRoot = Path.Combine(Path.GetTempPath(), $"shrimp-save-atomic-{Guid.NewGuid():N}");
            string tempSavePath = Path.Combine(tempRoot, "savegame.dat");
            string tempSettingsPath = Path.Combine(tempRoot, "settings.dat");
            Directory.CreateDirectory(tempRoot);

            try
            {
                saveManager.ConfigureTestStoragePaths(tempSavePath, tempSettingsPath);
                if (saveManager.CurrentData == null)
                {
                    saveManager.LoadGame();
                }

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
