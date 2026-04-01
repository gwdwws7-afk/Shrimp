using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonController.Editor
{
    public static class Scene01ObjectRuntimeAuditTool
    {
        private const string ScenePath = "Assets/Scenes/Level_01_TrenchRift.unity";
        private const string ReportPath = "Assets/ThirdPersonController/Reports/scene01_object_runtime_audit.md";
        private const string LogPrefix = "[Scene01ObjectRuntimeAudit]";
        private const string MenuPath = "Tools/Level/P0/Validate Scene01 Object Runtime Audit";
        private const string FixMenuPath = "Tools/Level/P0/Fix Scene01 Missing Scripts";
        private const string FixWarningsMenuPath = "Tools/Level/P0/Fix Scene01 Warning Items";
        private const string FallbackEnemyControllerPath = "Assets/animator/SharkEnemyAnimator.controller";

        [MenuItem(MenuPath)]
        public static void ValidateInteractive()
        {
            Run(interactive: true, failOnBlocking: false);
        }

        public static void ValidateForBatch()
        {
            Run(interactive: false, failOnBlocking: true);
        }

        [MenuItem(FixMenuPath)]
        public static void FixInteractive()
        {
            FixMissingScripts(interactive: true);
        }

        public static void FixKnownIssuesForBatch()
        {
            FixMissingScripts(interactive: false);
        }

        [MenuItem(FixWarningsMenuPath)]
        public static void FixWarningsInteractive()
        {
            FixWarnings(interactive: true);
        }

        public static void FixWarningsForBatch()
        {
            FixWarnings(interactive: false);
        }

        private static void Run(bool interactive, bool failOnBlocking)
        {
            if (interactive && !Application.isBatchMode)
            {
                bool allow = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                if (!allow)
                {
                    return;
                }
            }

            if (!File.Exists(ScenePath))
            {
                string missingMessage = $"{LogPrefix} missing scene: {ScenePath}";
                Debug.LogError(missingMessage);
                if (failOnBlocking)
                {
                    throw new FileNotFoundException(missingMessage, ScenePath);
                }

                return;
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                string openFail = $"{LogPrefix} failed to open scene: {ScenePath} | {ex.Message}";
                Debug.LogError(openFail);
                if (failOnBlocking)
                {
                    throw;
                }

                return;
            }

            var gameObjects = CollectGameObjects(scene);
            var definedTags = new HashSet<string>(InternalEditorUtility.tags);
            var blocking = new List<AuditIssue>();
            var warnings = new List<AuditIssue>();

            int componentCount = 0;
            int monoCount = 0;
            int missingScriptCount = 0;
            int missingReferenceCount = 0;
            int unknownTagCount = 0;
            int animatorCount = 0;
            int animatorMissingControllerCritical = 0;
            int animatorMissingControllerNonCritical = 0;
            int animatorMissingAvatar = 0;
            int playerTaggedCount = 0;

            foreach (GameObject go in gameObjects)
            {
                if (go == null)
                {
                    continue;
                }

                string objectPath = GetHierarchyPath(go.transform);

                if (!string.IsNullOrEmpty(go.tag) && !string.Equals(go.tag, "Untagged", StringComparison.Ordinal) &&
                    !definedTags.Contains(go.tag))
                {
                    unknownTagCount++;
                    blocking.Add(new AuditIssue("UnknownTag", objectPath, $"Tag '{go.tag}' is not defined in TagManager."));
                }

                Component[] components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (component == null)
                    {
                        missingScriptCount++;
                        if (objectPath.Contains("WB_AutoLayout/Meta/", StringComparison.Ordinal))
                        {
                            warnings.Add(new AuditIssue(
                                "MissingScript(EditorMeta)",
                                objectPath,
                                "Editor-only metadata script is missing on whitebox meta object."));
                        }
                        else
                        {
                            blocking.Add(new AuditIssue(
                                "MissingScript",
                                objectPath,
                                "A script component is missing (null Component slot)."));
                        }
                        continue;
                    }

                    componentCount++;

                    MonoBehaviour mono = component as MonoBehaviour;
                    if (mono != null)
                    {
                        monoCount++;
                        missingReferenceCount += CollectMissingObjectReferences(mono, go.transform, blocking);
                    }

                    Animator animator = component as Animator;
                    if (animator != null)
                    {
                        animatorCount++;

                        if (animator.runtimeAnimatorController == null)
                        {
                            if (IsCriticalAnimatorHost(go))
                            {
                                animatorMissingControllerCritical++;
                                blocking.Add(new AuditIssue(
                                    "AnimatorMissingController",
                                    objectPath,
                                    "Animator.runtimeAnimatorController is null on gameplay-critical object."));
                            }
                            else
                            {
                                animatorMissingControllerNonCritical++;
                                warnings.Add(new AuditIssue(
                                    "AnimatorMissingController",
                                    objectPath,
                                    "Animator.runtimeAnimatorController is null on non-critical child object."));
                            }
                        }

                        if (animator.avatar == null)
                        {
                            animatorMissingAvatar++;
                            blocking.Add(new AuditIssue(
                                "AnimatorMissingAvatar",
                                objectPath,
                                "Animator.avatar is null."));
                        }
                    }
                }

                if (go.CompareTag("Player"))
                {
                    playerTaggedCount++;
                }
            }

            ValidatePlayerAnchor(gameObjects, blocking, warnings);
            ValidateEnemyAnchors(gameObjects, warnings);

            string reportPath = WriteReport(
                gameObjects.Count,
                componentCount,
                monoCount,
                missingScriptCount,
                missingReferenceCount,
                unknownTagCount,
                animatorCount,
                animatorMissingControllerCritical,
                animatorMissingControllerNonCritical,
                animatorMissingAvatar,
                playerTaggedCount,
                blocking,
                warnings);

            AssetDatabase.Refresh();

            string summary =
                $"scene={ScenePath} gameObjects={gameObjects.Count} components={componentCount} monoBehaviours={monoCount} " +
                $"missingScripts={missingScriptCount} missingRefs={missingReferenceCount} unknownTags={unknownTagCount} " +
                $"animators={animatorCount} animatorMissingControllerCritical={animatorMissingControllerCritical} " +
                $"animatorMissingControllerNonCritical={animatorMissingControllerNonCritical} animatorMissingAvatar={animatorMissingAvatar} " +
                $"playerTagged={playerTaggedCount} blocking={blocking.Count} warnings={warnings.Count} report={reportPath}";

            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Scene01 Object Runtime Audit", summary, "OK");
            }

            if (failOnBlocking && blocking.Count > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} blocking issues found: {blocking.Count}. report={reportPath}");
            }
        }

        private static void FixMissingScripts(bool interactive)
        {
            if (interactive && !Application.isBatchMode)
            {
                bool allow = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                if (!allow)
                {
                    return;
                }
            }

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"{LogPrefix} missing scene: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<GameObject> gameObjects = CollectGameObjects(scene);
            int removedCount = 0;

            for (int i = 0; i < gameObjects.Count; i++)
            {
                GameObject go = gameObjects[i];
                if (go == null)
                {
                    continue;
                }

                removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            }

            bool saved = false;
            if (removedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                saved = EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary = $"scene={ScenePath} removedMissingScripts={removedCount} saved={saved}";
            Debug.Log($"{LogPrefix} fix complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Scene01 Missing Script Fix", summary, "OK");
            }
        }

        private static void FixWarnings(bool interactive)
        {
            if (interactive && !Application.isBatchMode)
            {
                bool allow = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                if (!allow)
                {
                    return;
                }
            }

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"{LogPrefix} missing scene: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<GameObject> gameObjects = CollectGameObjects(scene);

            int removedMissingScripts = 0;
            int assignedControllers = 0;
            int removedNonCriticalAnimators = 0;

            for (int i = 0; i < gameObjects.Count; i++)
            {
                GameObject go = gameObjects[i];
                if (go == null)
                {
                    continue;
                }

                string objectPath = GetHierarchyPath(go.transform);
                if (objectPath.Contains("WB_AutoLayout/Meta/", StringComparison.Ordinal))
                {
                    removedMissingScripts += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                }

                Animator animator = go.GetComponent<Animator>();
                if (animator != null &&
                    animator.runtimeAnimatorController == null &&
                    !IsCriticalAnimatorHost(go))
                {
                    RuntimeAnimatorController controller = FindNearestParentController(go.transform);
                    if (controller == null)
                    {
                        controller = LoadFallbackController();
                    }

                    if (controller != null)
                    {
                        animator.runtimeAnimatorController = controller;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                        assignedControllers++;
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(animator);
                        removedNonCriticalAnimators++;
                    }
                }
            }

            bool saved = false;
            if (removedMissingScripts > 0 || removedNonCriticalAnimators > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                saved = EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary =
                $"scene={ScenePath} removedMissingScripts={removedMissingScripts} " +
                $"assignedControllers={assignedControllers} removedNonCriticalAnimators={removedNonCriticalAnimators} saved={saved}";
            Debug.Log($"{LogPrefix} warning-fix complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Scene01 Warning Fix", summary, "OK");
            }
        }

        private static RuntimeAnimatorController FindNearestParentController(Transform transform)
        {
            Transform current = transform != null ? transform.parent : null;
            while (current != null)
            {
                Animator animator = current.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    return animator.runtimeAnimatorController;
                }

                current = current.parent;
            }

            return null;
        }

        private static RuntimeAnimatorController LoadFallbackController()
        {
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(FallbackEnemyControllerPath);
        }

        private static int CollectMissingObjectReferences(MonoBehaviour mono, Transform owner, List<AuditIssue> blocking)
        {
            int found = 0;
            SerializedObject serializedObject;
            try
            {
                serializedObject = new SerializedObject(mono);
            }
            catch
            {
                return 0;
            }

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (iterator.objectReferenceValue == null && iterator.objectReferenceInstanceIDValue != 0)
                {
                    found++;
                    blocking.Add(new AuditIssue(
                        "MissingReference",
                        GetHierarchyPath(owner),
                        $"{mono.GetType().Name}.{iterator.propertyPath} points to a missing object reference."));
                }
            }

            return found;
        }

        private static void ValidatePlayerAnchor(
            List<GameObject> gameObjects,
            List<AuditIssue> blocking,
            List<AuditIssue> warnings)
        {
            var taggedPlayers = new List<GameObject>();
            for (int i = 0; i < gameObjects.Count; i++)
            {
                GameObject go = gameObjects[i];
                if (go == null || !go.CompareTag("Player"))
                {
                    continue;
                }

                taggedPlayers.Add(go);
            }

            if (taggedPlayers.Count == 0)
            {
                blocking.Add(new AuditIssue("PlayerTagMissing", "SceneRoot", "No GameObject with tag 'Player' found."));
                return;
            }

            if (taggedPlayers.Count > 1)
            {
                blocking.Add(new AuditIssue("PlayerTagDuplicated", "SceneRoot", $"Found {taggedPlayers.Count} objects tagged 'Player'."));
            }

            bool hasCompleteAnchor = false;
            bool hasCombatOrHealth = false;

            for (int i = 0; i < taggedPlayers.Count; i++)
            {
                GameObject taggedPlayer = taggedPlayers[i];
                string playerPath = GetHierarchyPath(taggedPlayer.transform);
                bool hasMovement = taggedPlayer.GetComponent<PlayerMovement>() != null;
                bool hasCombat = taggedPlayer.GetComponent<PlayerCombat>() != null;
                bool hasHealth = taggedPlayer.GetComponent<PlayerHealth>() != null;
                bool hasInput = taggedPlayer.GetComponent<PlayerInputHandler>() != null;
                bool hasAnimator = taggedPlayer.GetComponent<Animator>() != null;

                if (hasCombat || hasHealth)
                {
                    hasCombatOrHealth = true;
                }

                if (hasMovement && hasInput && hasAnimator && (hasCombat || hasHealth))
                {
                    hasCompleteAnchor = true;
                }
                else
                {
                    warnings.Add(new AuditIssue(
                        "PlayerTagPartial",
                        playerPath,
                        $"Tagged player components => movement={hasMovement}, input={hasInput}, animator={hasAnimator}, combat={hasCombat}, health={hasHealth}."));
                }
            }

            if (!hasCombatOrHealth)
            {
                blocking.Add(new AuditIssue(
                    "PlayerComponentMissing",
                    "SceneRoot",
                    "No tagged player has PlayerCombat or PlayerHealth."));
            }

            if (!hasCompleteAnchor)
            {
                blocking.Add(new AuditIssue(
                    "PlayerComponentMissing",
                    "SceneRoot",
                    "No tagged player has a complete runtime anchor (PlayerMovement + PlayerInputHandler + Animator + (PlayerCombat or PlayerHealth))."));
            }
        }

        private static void ValidateEnemyAnchors(List<GameObject> gameObjects, List<AuditIssue> warnings)
        {
            int enemyAiCount = 0;
            int enemyHealthCount = 0;

            for (int i = 0; i < gameObjects.Count; i++)
            {
                GameObject go = gameObjects[i];
                if (go == null)
                {
                    continue;
                }

                if (go.GetComponent<EnemyAI>() != null)
                {
                    enemyAiCount++;
                }

                if (go.GetComponent<EnemyHealth>() != null)
                {
                    enemyHealthCount++;
                }
            }

            if (enemyAiCount == 0)
            {
                warnings.Add(new AuditIssue("EnemyAnchorMissing", "SceneRoot", "No EnemyAI component found in scene."));
            }

            if (enemyHealthCount == 0)
            {
                warnings.Add(new AuditIssue("EnemyAnchorMissing", "SceneRoot", "No EnemyHealth component found in scene."));
            }
        }

        private static bool IsCriticalAnimatorHost(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            if (go.CompareTag("Player"))
            {
                return true;
            }

            if (go.GetComponent<PlayerMovement>() != null ||
                go.GetComponent<PlayerCombat>() != null ||
                go.GetComponent<PlayerHealth>() != null ||
                go.GetComponent<EnemyAI>() != null ||
                go.GetComponent<EnemyHealth>() != null ||
                go.GetComponent<EnemyArchetypeConfigurator>() != null)
            {
                return true;
            }

            return false;
        }

        private static List<GameObject> CollectGameObjects(Scene scene)
        {
            var result = new List<GameObject>();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return result;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                result.Add(root);
                CollectChildren(root.transform, result);
            }

            return result;
        }

        private static void CollectChildren(Transform parent, List<GameObject> result)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                result.Add(child.gameObject);
                CollectChildren(child, result);
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            var stack = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack.ToArray());
        }

        private static string WriteReport(
            int gameObjectCount,
            int componentCount,
            int monoCount,
            int missingScriptCount,
            int missingReferenceCount,
            int unknownTagCount,
            int animatorCount,
            int animatorMissingControllerCritical,
            int animatorMissingControllerNonCritical,
            int animatorMissingAvatar,
            int playerTaggedCount,
            List<AuditIssue> blocking,
            List<AuditIssue> warnings)
        {
            string absolutePath = Path.GetFullPath(ReportPath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var sb = new StringBuilder();
            sb.AppendLine("# Scene01 Object Runtime Audit");
            sb.AppendLine();
            sb.AppendLine($"- Scene: `{ScenePath}`");
            sb.AppendLine($"- GameObjects: `{gameObjectCount}`");
            sb.AppendLine($"- Components: `{componentCount}`");
            sb.AppendLine($"- MonoBehaviours: `{monoCount}`");
            sb.AppendLine($"- Missing Scripts: `{missingScriptCount}`");
            sb.AppendLine($"- Missing Object References: `{missingReferenceCount}`");
            sb.AppendLine($"- Unknown Tags: `{unknownTagCount}`");
            sb.AppendLine($"- Animators: `{animatorCount}`");
            sb.AppendLine($"- Animator Missing Controller (Critical): `{animatorMissingControllerCritical}`");
            sb.AppendLine($"- Animator Missing Controller (Non-Critical): `{animatorMissingControllerNonCritical}`");
            sb.AppendLine($"- Animator Missing Avatar: `{animatorMissingAvatar}`");
            sb.AppendLine($"- Player Tagged Objects: `{playerTaggedCount}`");
            sb.AppendLine($"- Blocking Issues: `{blocking.Count}`");
            sb.AppendLine($"- Warnings: `{warnings.Count}`");
            sb.AppendLine();

            sb.AppendLine("## Blocking Issues");
            if (blocking.Count == 0)
            {
                sb.AppendLine("- None");
            }
            else
            {
                for (int i = 0; i < blocking.Count; i++)
                {
                    AuditIssue issue = blocking[i];
                    sb.AppendLine($"- `{issue.kind}` | `{issue.path}` | {issue.message}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Warnings");
            if (warnings.Count == 0)
            {
                sb.AppendLine("- None");
            }
            else
            {
                for (int i = 0; i < warnings.Count; i++)
                {
                    AuditIssue issue = warnings[i];
                    sb.AppendLine($"- `{issue.kind}` | `{issue.path}` | {issue.message}");
                }
            }

            File.WriteAllText(absolutePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return ReportPath;
        }

        private readonly struct AuditIssue
        {
            public readonly string kind;
            public readonly string path;
            public readonly string message;

            public AuditIssue(string kind, string path, string message)
            {
                this.kind = kind;
                this.path = path;
                this.message = message;
            }
        }
    }
}
