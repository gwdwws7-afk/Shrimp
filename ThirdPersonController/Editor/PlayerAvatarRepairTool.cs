using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class PlayerAvatarRepairTool
    {
        private const string PlayerPrefabPath = "Assets/Character/Meshy_AI_Animation_Walking_frame_rate_60.prefab";
        private const string AvatarSourceModelPath = "Assets/fbx/Characters/Meshy_AI_biped/Meshy_AI_Animation_Walking_frame_rate_60.fbx";

        [MenuItem("Tools/ThirdPersonController/Player/Repair Avatar Binding")]
        public static void RepairMenu()
        {
            if (RepairInternal(logSuccess: true))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        // Batch entry for CI or command-line execution.
        public static void RepairForBatch()
        {
            RepairInternal(logSuccess: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/ThirdPersonController/Player/Validate Avatar Binding")]
        public static void ValidateMenu()
        {
            Animator prefabAnimator = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath)?.GetComponent<Animator>();
            if (prefabAnimator == null)
            {
                Debug.LogError($"[PlayerAvatarRepairTool] Player prefab missing Animator: {PlayerPrefabPath}");
                return;
            }

            bool valid = prefabAnimator.avatar != null && prefabAnimator.avatar.isValid;
            Debug.Log(valid
                ? $"[PlayerAvatarRepairTool] Avatar valid on {PlayerPrefabPath}."
                : $"[PlayerAvatarRepairTool] Avatar missing/invalid on {PlayerPrefabPath}.");
        }

        private static bool RepairInternal(bool logSuccess)
        {
            if (!EnsureHumanoidImporter(AvatarSourceModelPath))
            {
                return false;
            }

            Avatar avatar = FindValidAvatar(AvatarSourceModelPath);
            if (avatar == null)
            {
                Debug.LogError($"[PlayerAvatarRepairTool] No valid Avatar found in model: {AvatarSourceModelPath}");
                return false;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[PlayerAvatarRepairTool] Failed to load prefab: {PlayerPrefabPath}");
                return false;
            }

            bool changed = false;
            try
            {
                Animator animator = prefabRoot.GetComponent<Animator>();
                if (animator == null)
                {
                    Debug.LogError($"[PlayerAvatarRepairTool] Animator not found on prefab root: {PlayerPrefabPath}");
                    return false;
                }

                if (animator.avatar != avatar)
                {
                    animator.avatar = avatar;
                    changed = true;
                }

                if (animator.applyRootMotion)
                {
                    animator.applyRootMotion = false;
                    changed = true;
                }

                if (!string.Equals(prefabRoot.tag, "Player", System.StringComparison.Ordinal))
                {
                    prefabRoot.tag = "Player";
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            if (logSuccess)
            {
                Debug.Log(changed
                    ? $"[PlayerAvatarRepairTool] Repaired avatar binding: {PlayerPrefabPath}"
                    : $"[PlayerAvatarRepairTool] Avatar binding already valid: {PlayerPrefabPath}");
            }

            return true;
        }

        private static bool EnsureHumanoidImporter(string modelPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[PlayerAvatarRepairTool] ModelImporter not found: {modelPath}");
                return false;
            }

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            return true;
        }

        private static Avatar FindValidAvatar(string modelPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
            if (assets == null || assets.Length == 0)
            {
                return null;
            }

            return assets
                .OfType<Avatar>()
                .FirstOrDefault(a => a != null && a.isValid);
        }
    }
}
