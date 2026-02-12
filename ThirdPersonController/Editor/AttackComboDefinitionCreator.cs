using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class AttackComboDefinitionCreator
    {
        private const string DefaultFolder = "Assets/ThirdPersonController/Combat/Combos";

        [MenuItem("Tools/Combat/Create Default Attack Combo")]
        public static void CreateDefaultCombo()
        {
            string targetFolder = GetSelectedFolderOrDefault();
            EnsureFolderExists(targetFolder);

            AttackComboDefinition combo = ScriptableObject.CreateInstance<AttackComboDefinition>();
            combo.comboResetTime = 1.5f;
            combo.maxComboCount = 50;
            combo.inputBufferTime = 0.3f;
            combo.steps = CreateDefaultSteps();

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetFolder, "AttackComboDefinition.asset"));
            AssetDatabase.CreateAsset(combo, assetPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = combo;
            EditorGUIUtility.PingObject(combo);

            Debug.Log($"Created default attack combo at {assetPath}");
        }

        private static List<AttackStep> CreateDefaultSteps()
        {
            return new List<AttackStep>
            {
                new AttackStep
                {
                    name = "N1",
                    animationComboIndex = 1,
                    baseDamage = 25,
                    damageMultiplier = 1f,
                    knockback = 5f,
                    range = 2f,
                    angle = 120f,
                    radius = 1f,
                    hitDelay = 0.15f,
                    recoveryTime = 0.35f,
                    comboWindowStart = 0.1f,
                    comboWindowEnd = 0.55f,
                    staminaCost = 0f,
                    allowDodgeCancel = true,
                    allowBlockCancel = true,
                    requireGrounded = true,
                    nextStepIndex = 1
                },
                new AttackStep
                {
                    name = "N2",
                    animationComboIndex = 2,
                    baseDamage = 30,
                    damageMultiplier = 1.1f,
                    knockback = 6f,
                    range = 2.2f,
                    angle = 120f,
                    radius = 1.1f,
                    hitDelay = 0.17f,
                    recoveryTime = 0.4f,
                    comboWindowStart = 0.12f,
                    comboWindowEnd = 0.6f,
                    staminaCost = 0f,
                    allowDodgeCancel = true,
                    allowBlockCancel = true,
                    requireGrounded = true,
                    nextStepIndex = 2
                },
                new AttackStep
                {
                    name = "N3",
                    animationComboIndex = 3,
                    baseDamage = 40,
                    damageMultiplier = 1.2f,
                    knockback = 7f,
                    range = 2.4f,
                    angle = 120f,
                    radius = 1.2f,
                    hitDelay = 0.2f,
                    recoveryTime = 0.5f,
                    comboWindowStart = 0.15f,
                    comboWindowEnd = 0.7f,
                    staminaCost = 0f,
                    allowDodgeCancel = true,
                    allowBlockCancel = true,
                    requireGrounded = true,
                    nextStepIndex = -1
                }
            };
        }

        private static string GetSelectedFolderOrDefault()
        {
            string path = DefaultFolder;

            Object selected = Selection.activeObject;
            if (selected == null)
            {
                return path;
            }

            string selectedPath = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(selectedPath))
            {
                return path;
            }

            if (Directory.Exists(selectedPath))
            {
                return selectedPath;
            }

            string directory = Path.GetDirectoryName(selectedPath);
            return string.IsNullOrEmpty(directory) ? path : directory.Replace("\\", "/");
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string fullPath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            AssetDatabase.Refresh();
        }
    }
}
