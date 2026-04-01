using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class HumanoidAnimationPipelineTool
    {
        private static readonly HashSet<string> ActionStopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "meshy", "ai", "animation", "frame", "rate", "fps", "armature", "base", "baselayer", "layer", "scene", "take"
        };

        private const string FbxRoot = "Assets/fbx";
        private const string CharactersRoot = "Assets/fbx/Characters";
        private const string AvatarOutputRoot = "Assets/ThirdPersonController/Animations/Avatars";
        private const string AnimationOutputRoot = "Assets/ThirdPersonController/Animations/Humanoid";
        private const string MasterAvatarModelPath = "Assets/fbx/Characters/Meshy_AI_biped/Meshy_AI_Animation_Walking_frame_rate_60.fbx";
        private const string LogPrefix = "[HumanoidAnimationPipeline]";

        [MenuItem("Tools/Animation/Humanoid Pipeline/Run Full Pipeline")]
        public static void RunFullPipelineMenu()
        {
            RunPipeline(logToConsole: true);
        }

        [MenuItem("Tools/Animation/Humanoid Pipeline/Rebind Animator Controllers")]
        public static void RebindAnimatorControllersMenu()
        {
            int changedControllers = RebindAllAnimatorControllers();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"{LogPrefix} Rebind done. Changed controllers: {changedControllers}.");
        }

        // Batch entry point for -executeMethod
        public static void RunForBatch()
        {
            RunPipeline(logToConsole: true);
        }

        // Batch entry point for -executeMethod
        public static void RebindForBatch()
        {
            int changedControllers = RebindAllAnimatorControllers();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"{LogPrefix} Rebind done. Changed controllers: {changedControllers}.");
        }

        private static void RunPipeline(bool logToConsole)
        {
            try
            {
                if (!AssetDatabase.IsValidFolder("Assets/ThirdPersonController/Animations"))
                {
                    EnsureFolderPath("Assets/ThirdPersonController/Animations");
                }

                RecreateFolder(AvatarOutputRoot);
                RecreateFolder(AnimationOutputRoot);

                if (!EnsureModelImporter(MasterAvatarModelPath, createFromThisModel: true, sourceAvatar: null))
                {
                    Debug.LogError($"{LogPrefix} Master avatar model importer invalid: {MasterAvatarModelPath}");
                    return;
                }

                Avatar masterAvatar = FindValidAvatar(MasterAvatarModelPath);
                if (masterAvatar == null)
                {
                    Debug.LogError($"{LogPrefix} Cannot find a valid master avatar from: {MasterAvatarModelPath}");
                    return;
                }

                List<string> allFbx = DiscoverAllFbxPaths();
                List<string> allCharacterFbx = DiscoverCharacterFbxPaths();
                List<string> modelSources = DiscoverModelSourceFbxPaths(allCharacterFbx);

                int avatarAssetCount = ExtractModelAvatars(modelSources);
                int humanoidConfiguredCount = AlignCharacterFbxToHumanoid(allCharacterFbx, modelSources, masterAvatar);
                int extractedAnimCount = ExtractAnimationClips(allFbx);
                int reboundControllers = RebindAllAnimatorControllers();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (logToConsole)
                {
                    Debug.Log(
                        $"{LogPrefix} Done. Avatars: {avatarAssetCount}, Humanoid-aligned FBX: {humanoidConfiguredCount}, Extracted animations: {extractedAnimCount}, Rebound controllers: {reboundControllers}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} Pipeline failed: {ex}");
                throw;
            }
        }

        private static List<string> DiscoverCharacterFbxPaths()
        {
            string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { CharactersRoot });
            return fbxGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> DiscoverAllFbxPaths()
        {
            string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { FbxRoot });
            return fbxGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> DiscoverModelSourceFbxPaths(List<string> allCharacterFbx)
        {
            Dictionary<string, List<string>> byCharacter = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string fbxPath in allCharacterFbx)
            {
                string characterKey = GetCharacterKey(fbxPath);
                if (!byCharacter.TryGetValue(characterKey, out List<string> list))
                {
                    list = new List<string>();
                    byCharacter[characterKey] = list;
                }

                list.Add(fbxPath);
            }

            List<string> modelPaths = new List<string>();
            foreach (KeyValuePair<string, List<string>> pair in byCharacter)
            {
                string characterKey = pair.Key;
                List<string> candidates = pair.Value.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();

                if (string.Equals(characterKey, "Idle", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string selected = candidates.FirstOrDefault(path =>
                    Path.GetFileName(path).IndexOf("Character_output", StringComparison.OrdinalIgnoreCase) >= 0);

                if (string.IsNullOrEmpty(selected))
                {
                    selected = candidates.FirstOrDefault(path =>
                        Path.GetFileName(path).IndexOf("Animation_Walking", StringComparison.OrdinalIgnoreCase) >= 0);
                }

                if (string.IsNullOrEmpty(selected))
                {
                    selected = candidates[0];
                }

                modelPaths.Add(selected);
            }

            if (!modelPaths.Contains(MasterAvatarModelPath, StringComparer.OrdinalIgnoreCase))
            {
                modelPaths.Insert(0, MasterAvatarModelPath);
            }

            return modelPaths
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int ExtractModelAvatars(List<string> modelSourcePaths)
        {
            int avatarAssetCount = 0;

            foreach (string modelPath in modelSourcePaths)
            {
                if (!EnsureModelImporter(modelPath, createFromThisModel: true, sourceAvatar: null))
                {
                    Debug.LogWarning($"{LogPrefix} Skip avatar extraction (importer invalid): {modelPath}");
                    continue;
                }

                Avatar avatar = FindValidAvatar(modelPath);
                if (avatar == null)
                {
                    Debug.LogWarning($"{LogPrefix} Skip avatar extraction (no valid avatar): {modelPath}");
                    continue;
                }

                string characterKey = GetCharacterKey(modelPath);
                string avatarAssetPath = $"{AvatarOutputRoot}/{characterKey}_Avatar.asset";

                if (TryCreateAvatarAsset(avatar, avatarAssetPath))
                {
                    avatarAssetCount++;
                }
            }

            return avatarAssetCount;
        }

        private static int AlignCharacterFbxToHumanoid(List<string> allCharacterFbx, List<string> modelSourcePaths, Avatar masterAvatar)
        {
            int configuredCount = 0;
            HashSet<string> modelSourceSet = new HashSet<string>(modelSourcePaths, StringComparer.OrdinalIgnoreCase);

            foreach (string fbxPath in allCharacterFbx)
            {
                bool isModelSource = modelSourceSet.Contains(fbxPath);
                bool ok = EnsureModelImporter(fbxPath, createFromThisModel: isModelSource, sourceAvatar: isModelSource ? null : masterAvatar);
                if (ok)
                {
                    configuredCount++;
                }
            }

            return configuredCount;
        }

        private static int ExtractAnimationClips(List<string> allFbxPaths)
        {
            int extractedCount = 0;
            HashSet<string> usedOutputPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string fbxPath in allFbxPaths)
            {
                UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                if (subAssets == null || subAssets.Length == 0)
                {
                    continue;
                }

                List<AnimationClip> clips = subAssets
                    .OfType<AnimationClip>()
                    .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (clips.Count == 0)
                {
                    continue;
                }

                string characterKey = GetAssetGroupKey(fbxPath);
                string characterFolder = $"{AnimationOutputRoot}/{characterKey}";
                EnsureFolderPath(characterFolder);

                foreach (AnimationClip clip in clips)
                {
                    string actionKey = BuildActionKey(clip.name, fbxPath);
                    string fileName = $"{characterKey}__{actionKey}.anim";
                    string outputPath = AssetDatabase.GenerateUniqueAssetPath($"{characterFolder}/{fileName}");
                    int dedupeIndex = 2;
                    while (usedOutputPaths.Contains(outputPath))
                    {
                        string deduped = $"{characterFolder}/{characterKey}__{actionKey}_{dedupeIndex}.anim";
                        outputPath = AssetDatabase.GenerateUniqueAssetPath(deduped);
                        dedupeIndex++;
                    }

                    AnimationClip clipCopy = UnityEngine.Object.Instantiate(clip);
                    clipCopy.name = Path.GetFileNameWithoutExtension(outputPath);
                    AssetDatabase.CreateAsset(clipCopy, outputPath);

                    usedOutputPaths.Add(outputPath);
                    extractedCount++;
                }
            }

            return extractedCount;
        }

        private static string BuildActionKey(string clipName, string fbxPath)
        {
            string candidate = clipName ?? string.Empty;
            int lastPipe = candidate.LastIndexOf('|');
            if (lastPipe >= 0 && lastPipe < candidate.Length - 1)
            {
                candidate = candidate.Substring(lastPipe + 1);
            }

            string normalized = NormalizeActionCandidate(candidate);
            if (IsGenericActionKey(normalized))
            {
                string fbxStem = Path.GetFileNameWithoutExtension(fbxPath);
                normalized = NormalizeActionCandidate(fbxStem);
            }

            if (IsGenericActionKey(normalized))
            {
                normalized = "Clip";
            }

            return ToPascalCase(normalized);
        }

        private static bool IsGenericActionKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return true;
            }

            string lower = key.ToLowerInvariant();
            if (lower == "clip")
            {
                return true;
            }

            return Regex.IsMatch(lower, "^(bas|base|basel|basela|baselay|baselaye|baselayer|layer|scene|take\\d*)$");
        }

        private static string NormalizeActionCandidate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string cleaned = Regex.Replace(value, "[^A-Za-z0-9]+", "_");
            cleaned = Regex.Replace(cleaned, "_+", "_").Trim('_');
            if (string.IsNullOrEmpty(cleaned))
            {
                return string.Empty;
            }

            string[] parts = cleaned.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> kept = new List<string>(parts.Length);
            foreach (string part in parts)
            {
                string token = part.Trim();
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (ActionStopWords.Contains(token))
                {
                    continue;
                }

                if (Regex.IsMatch(token, "^\\d+$"))
                {
                    continue;
                }

                kept.Add(token);
            }

            if (kept.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("_", kept);
        }

        private static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "Clip";
            }

            string[] parts = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return "Clip";
            }

            List<string> transformed = new List<string>();
            foreach (string part in parts)
            {
                if (part.Length == 0)
                {
                    continue;
                }

                string lower = part.ToLowerInvariant();
                string converted = char.ToUpperInvariant(lower[0]) + lower.Substring(1);
                transformed.Add(converted);
            }

            return transformed.Count == 0 ? "Clip" : string.Join(string.Empty, transformed);
        }

        private static string GetCharacterKey(string assetPath)
        {
            string relative = assetPath.Replace('\\', '/');
            string prefix = CharactersRoot + "/";
            if (!relative.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return "Unknown";
            }

            string afterRoot = relative.Substring(prefix.Length);
            string[] segments = afterRoot.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return "Unknown";
            }

            string raw = segments[0];
            if (string.Equals(raw, "Meshy_AI_biped", StringComparison.OrdinalIgnoreCase))
            {
                raw = "PlayerBase";
            }

            raw = Regex.Replace(raw, "[^A-Za-z0-9]+", "_");
            raw = Regex.Replace(raw, "_+", "_").Trim('_');
            return ToPascalCase(raw);
        }

        private static string GetAssetGroupKey(string assetPath)
        {
            string relative = assetPath.Replace('\\', '/');
            string characterPrefix = CharactersRoot + "/";
            if (relative.StartsWith(characterPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return GetCharacterKey(assetPath);
            }

            string fbxPrefix = FbxRoot + "/";
            if (relative.StartsWith(fbxPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string afterRoot = relative.Substring(fbxPrefix.Length);
                string[] segments = afterRoot.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    string raw = Regex.Replace(segments[0], "[^A-Za-z0-9]+", "_");
                    raw = Regex.Replace(raw, "_+", "_").Trim('_');
                    return ToPascalCase(raw);
                }
            }

            return "Unknown";
        }

        private static bool TryCreateAvatarAsset(Avatar sourceAvatar, string avatarAssetPath)
        {
            try
            {
                EnsureFolderPath(Path.GetDirectoryName(avatarAssetPath).Replace('\\', '/'));

                Avatar clone = UnityEngine.Object.Instantiate(sourceAvatar);
                clone.name = Path.GetFileNameWithoutExtension(avatarAssetPath);
                AssetDatabase.CreateAsset(clone, avatarAssetPath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{LogPrefix} Failed to create avatar asset at {avatarAssetPath}: {ex.Message}");
                return false;
            }
        }

        private static bool EnsureModelImporter(string modelPath, bool createFromThisModel, Avatar sourceAvatar)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                return false;
            }

            bool changed = false;
            if (importer.importAnimation != true)
            {
                importer.importAnimation = true;
                changed = true;
            }

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (createFromThisModel)
            {
                if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    changed = true;
                }
            }
            else
            {
                if (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther)
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    changed = true;
                }

                if (sourceAvatar != null && importer.sourceAvatar != sourceAvatar)
                {
                    importer.sourceAvatar = sourceAvatar;
                    changed = true;
                }
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            return true;
        }

        private static Avatar FindValidAvatar(string modelPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
            if (assets == null || assets.Length == 0)
            {
                return null;
            }

            return assets
                .OfType<Avatar>()
                .FirstOrDefault(avatar => avatar != null && avatar.isValid);
        }

        private static void RecreateFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            EnsureFolderPath(assetPath);
        }

        private static void EnsureFolderPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            string normalized = folderPath.Replace('\\', '/');
            string[] segments = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return;
            }

            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static int RebindAllAnimatorControllers()
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets" });
            int changedControllers = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (controller == null)
                {
                    continue;
                }

                int changedMotions;
                if (RebindController(controller, out changedMotions))
                {
                    EditorUtility.SetDirty(controller);
                    changedControllers++;
                    Debug.Log($"{LogPrefix} Rebound {changedMotions} motions in controller: {path}");
                }
            }

            return changedControllers;
        }

        private static bool RebindController(AnimatorController controller, out int changedMotions)
        {
            changedMotions = 0;
            if (controller.layers == null || controller.layers.Length == 0)
            {
                return false;
            }

            bool changed = false;
            for (int i = 0; i < controller.layers.Length; i++)
            {
                AnimatorStateMachine stateMachine = controller.layers[i].stateMachine;
                if (stateMachine == null)
                {
                    continue;
                }

                changed |= RebindStateMachine(stateMachine, ref changedMotions);
            }

            return changed;
        }

        private static bool RebindStateMachine(AnimatorStateMachine stateMachine, ref int changedMotions)
        {
            bool changed = false;

            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null || state.motion == null)
                {
                    continue;
                }

                Motion rebound;
                if (TryRebindMotion(state.motion, out rebound))
                {
                    state.motion = rebound;
                    changed = true;
                    changedMotions++;
                }
            }

            ChildAnimatorStateMachine[] subMachines = stateMachine.stateMachines;
            for (int i = 0; i < subMachines.Length; i++)
            {
                AnimatorStateMachine child = subMachines[i].stateMachine;
                if (child == null)
                {
                    continue;
                }

                changed |= RebindStateMachine(child, ref changedMotions);
            }

            return changed;
        }

        private static bool TryRebindMotion(Motion motion, out Motion reboundMotion)
        {
            reboundMotion = motion;
            if (motion == null)
            {
                return false;
            }

            AnimationClip clip = motion as AnimationClip;
            if (clip != null)
            {
                AnimationClip reboundClip = FindExtractedClipEquivalent(clip);
                if (reboundClip != null && reboundClip != clip)
                {
                    reboundMotion = reboundClip;
                    return true;
                }

                return false;
            }

            BlendTree blendTree = motion as BlendTree;
            if (blendTree != null)
            {
                return RebindBlendTree(blendTree);
            }

            return false;
        }

        private static bool RebindBlendTree(BlendTree blendTree)
        {
            bool changed = false;
            ChildMotion[] children = blendTree.children;
            for (int i = 0; i < children.Length; i++)
            {
                Motion childMotion = children[i].motion;
                if (childMotion == null)
                {
                    continue;
                }

                Motion rebound;
                if (TryRebindMotion(childMotion, out rebound))
                {
                    children[i].motion = rebound;
                    changed = true;
                }
            }

            if (changed)
            {
                blendTree.children = children;
                EditorUtility.SetDirty(blendTree);
            }

            return changed;
        }

        private static AnimationClip FindExtractedClipEquivalent(AnimationClip sourceClip)
        {
            string sourcePath = AssetDatabase.GetAssetPath(sourceClip);
            if (string.IsNullOrEmpty(sourcePath))
            {
                return null;
            }

            sourcePath = sourcePath.Replace('\\', '/');
            if (sourcePath.StartsWith(AnimationOutputRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string group = GetAssetGroupKey(sourcePath);
            string action = BuildActionKey(sourceClip.name, sourcePath);
            string groupFolder = $"{AnimationOutputRoot}/{group}";
            string exactPath = $"{groupFolder}/{group}__{action}.anim";
            AnimationClip exact = AssetDatabase.LoadAssetAtPath<AnimationClip>(exactPath);
            if (exact != null)
            {
                return exact;
            }

            if (!AssetDatabase.IsValidFolder(groupFolder))
            {
                return null;
            }

            string[] candidates = AssetDatabase.FindAssets($"{group}__{action} t:AnimationClip", new[] { groupFolder });
            if (candidates == null || candidates.Length == 0)
            {
                return null;
            }

            string candidatePath = AssetDatabase.GUIDToAssetPath(candidates[0]);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(candidatePath);
        }
    }
}
