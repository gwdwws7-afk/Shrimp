using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyTypeBindingValidationMenu
{
    private const string ValidateMenuPath = "Tools/AI/Validate Enemy Type Bindings (P1)";
    private const string ValidateSceneMenuPath = "Tools/AI/Validate Enemy Type Bindings In Scenes (P1)";
    private const string ValidateSceneGateMenuPath = "Tools/AI/Validate Enemy Type Bindings In Scenes (P1 CI Gate)";
    private const string ExportCsvMenuPath = "Tools/AI/Export Enemy Type Binding Report (P1 CSV)";
    private const string FixMenuPath = "Tools/AI/Fix Enemy Type Bindings (P1)";
    private const string FixSceneMenuPath = "Tools/AI/Fix Enemy Type Bindings In Scenes (P1)";
    private const string DefaultCsvPath = "Assets/ThirdPersonController/Reports/enemy_type_p1_binding_report.csv";
    private const string DefaultSceneChecklistCsvPath = "Assets/ThirdPersonController/Reports/enemy_type_scene_missing_prefab_checklist.csv";
    private const string DefaultFallbackConfigPath = "Assets/GameDesign/Data/EnemyWavePrefabFallbackConfig.asset";
    private const string LogPrefix = "[EnemyTypeBindingValidation]";

    [MenuItem(ValidateMenuPath)]
    public static void ValidateBindings()
    {
        int scannedPrefabs;
        int scannedWaveGroups;
        List<EnemyTypeBindingIssue> issues = CollectIssues(out scannedPrefabs, out scannedWaveGroups);

        int errorCount;
        int warningCount;
        LogIssuesToConsole(issues, out errorCount, out warningCount);

        var summary = new StringBuilder();
        summary.AppendLine($"Scanned enemy prefabs: {scannedPrefabs}");
        summary.AppendLine($"Scanned stronghold wave groups: {scannedWaveGroups}");
        summary.AppendLine($"Errors: {errorCount}");
        summary.AppendLine($"Warnings: {warningCount}");
        summary.AppendLine("See Console for details.");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Enemy Type Binding Validation", summary.ToString(), "OK");
        }
        else
        {
            Debug.Log($"{LogPrefix} {summary.ToString().Replace('\n', ' ')}");
        }
    }

    [MenuItem(ValidateSceneMenuPath)]
    public static void ValidateBindingsInScenes()
    {
        ValidateBindingsInScenesInternal(failOnError: false, showDialogWhenInteractive: true);
    }

    [MenuItem(ValidateSceneGateMenuPath)]
    public static void ValidateBindingsInScenesCiGate()
    {
        ValidateBindingsInScenesInternal(failOnError: true, showDialogWhenInteractive: false);
    }

    private static void ValidateBindingsInScenesInternal(bool failOnError, bool showDialogWhenInteractive)
    {
        if (!Application.isBatchMode)
        {
            bool allow = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            if (!allow)
            {
                return;
            }
        }

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            int scannedScenes;
            int scannedSceneEnemies;
            int scannedStrongholds;
            int scannedWaveGroups;
            List<EnemyTypeBindingIssue> issues = CollectSceneIssues(
                out scannedScenes,
                out scannedSceneEnemies,
                out scannedStrongholds,
                out scannedWaveGroups);

            int errorCount;
            int warningCount;
            LogIssuesToConsole(issues, out errorCount, out warningCount);

            var summary = new StringBuilder();
            summary.AppendLine($"Scanned scenes: {scannedScenes}");
            summary.AppendLine($"Scanned scene enemies: {scannedSceneEnemies}");
            summary.AppendLine($"Scanned scene strongholds: {scannedStrongholds}");
            summary.AppendLine($"Scanned scene wave groups: {scannedWaveGroups}");
            summary.AppendLine($"Errors: {errorCount}");
            summary.AppendLine($"Warnings: {warningCount}");
            summary.AppendLine("See Console for details.");

            if (!Application.isBatchMode && showDialogWhenInteractive)
            {
                EditorUtility.DisplayDialog("Enemy Type Scene Binding Validation", summary.ToString(), "OK");
            }
            else
            {
                Debug.Log($"{LogPrefix} {summary.ToString().Replace('\n', ' ')}");
            }

            if (failOnError && errorCount > 0)
            {
                string message = $"{LogPrefix} CI gate failed. Errors: {errorCount}, Warnings: {warningCount}.";
                Debug.LogError(message);
                throw new System.InvalidOperationException(message);
            }
        }
        finally
        {
            if (originalSetup != null && originalSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }
    }

    [MenuItem(FixSceneMenuPath)]
    public static void FixBindingsInScenes()
    {
        if (!Application.isBatchMode)
        {
            bool allow = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            if (!allow)
            {
                return;
            }
        }

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EnemyArchetypeLibrary archetypes = BuildArchetypeLibrary();
            if (archetypes.byId.Count == 0)
            {
                Debug.LogError($"{LogPrefix} no EnemyArchetype assets found. Aborting scene auto-fix.");
                return;
            }

            PrefabArchetypeIndex prefabIndex = BuildPrefabArchetypeIndex();
            EnemyWavePrefabFallbackConfig fallbackConfig = LoadOrCreateWavePrefabFallbackConfig();
            var checklistRows = new List<SceneMissingPrefabChecklistRow>();

            int scannedScenes;
            int savedScenes;
            int scannedSceneEnemies;
            int scannedStrongholds;
            int scannedWaveGroups;
            int addedConfigurators;
            int alignedEnemyTypes;
            int assignedMissingArchetypes;
            int missingPrefabReferences;
            int autoAssignedPrefabReferences;

            ExecuteSceneFixes(
                archetypes,
                prefabIndex,
                fallbackConfig,
                checklistRows,
                out scannedScenes,
                out savedScenes,
                out scannedSceneEnemies,
                out scannedStrongholds,
                out scannedWaveGroups,
                out addedConfigurators,
                out alignedEnemyTypes,
                out assignedMissingArchetypes,
                out missingPrefabReferences,
                out autoAssignedPrefabReferences);

            string checklistPath = ExportSceneChecklistCsv(checklistRows);

            var summary = new StringBuilder();
            summary.AppendLine($"Scanned scenes: {scannedScenes}");
            summary.AppendLine($"Saved scenes: {savedScenes}");
            summary.AppendLine($"Scanned scene enemies: {scannedSceneEnemies}");
            summary.AppendLine($"Scanned scene strongholds: {scannedStrongholds}");
            summary.AppendLine($"Scanned scene wave groups: {scannedWaveGroups}");
            summary.AppendLine($"Added configurators: {addedConfigurators}");
            summary.AppendLine($"Aligned EnemyHealth.enemyType: {alignedEnemyTypes}");
            summary.AppendLine($"Assigned missing archetypes: {assignedMissingArchetypes}");
            summary.AppendLine($"Missing prefab references found: {missingPrefabReferences}");
            summary.AppendLine($"Auto-assigned prefab references: {autoAssignedPrefabReferences}");
            summary.AppendLine($"Unresolved prefab references: {Mathf.Max(0, missingPrefabReferences - autoAssignedPrefabReferences)}");
            summary.AppendLine($"Checklist CSV: {checklistPath}");

            Debug.Log($"{LogPrefix} scene auto-fix complete.\n{summary}");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Enemy Type Scene Binding Fix", summary.ToString(), "OK");
            }
        }
        finally
        {
            if (originalSetup != null && originalSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }
    }

    [MenuItem(FixMenuPath)]
    public static void FixBindings()
    {
        EnemyArchetypeLibrary archetypes = BuildArchetypeLibrary();
        if (archetypes.byId.Count == 0)
        {
            Debug.LogError($"{LogPrefix} no EnemyArchetype assets found. Aborting auto-fix.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int scannedPrefabs = 0;
        int savedPrefabs = 0;
        int addedConfigurators = 0;
        int alignedEnemyTypes = 0;
        int assignedMissingArchetypes = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (sourcePrefab == null)
            {
                continue;
            }

            EnemyAI sourceAi = sourcePrefab.GetComponentInChildren<EnemyAI>(true);
            EnemyHealth sourceHealth = sourcePrefab.GetComponentInChildren<EnemyHealth>(true);
            if (sourceAi == null && sourceHealth == null)
            {
                continue;
            }

            scannedPrefabs++;
            GameObject root = null;
            bool changed = false;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                if (root == null || EnemyTypeBindingValidation.IsBossLikePrefab(root))
                {
                    continue;
                }

                EnemyHealth health = root.GetComponentInChildren<EnemyHealth>(true);
                EnemyArchetypeConfigurator configurator = root.GetComponentInChildren<EnemyArchetypeConfigurator>(true);

                if (configurator == null)
                {
                    GameObject owner = health != null ? health.gameObject : root;
                    configurator = owner.AddComponent<EnemyArchetypeConfigurator>();
                    addedConfigurators++;
                    changed = true;
                }

                if (configurator != null && configurator.archetype == null)
                {
                    EnemyArchetype fallback = ResolveArchetypeForHealth(archetypes, health);
                    if (fallback != null)
                    {
                        configurator.archetype = fallback;
                        assignedMissingArchetypes++;
                        changed = true;
                    }
                }

                if (configurator != null && configurator.archetype != null && health != null)
                {
                    if (health.enemyType != configurator.archetype.enemyType)
                    {
                        health.enemyType = configurator.archetype.enemyType;
                        alignedEnemyTypes++;
                        changed = true;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    savedPrefabs++;
                }
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var summary = new StringBuilder();
        summary.AppendLine($"Scanned enemy prefabs: {scannedPrefabs}");
        summary.AppendLine($"Saved prefabs: {savedPrefabs}");
        summary.AppendLine($"Added configurators: {addedConfigurators}");
        summary.AppendLine($"Aligned EnemyHealth.enemyType: {alignedEnemyTypes}");
        summary.AppendLine($"Assigned missing archetypes: {assignedMissingArchetypes}");

        Debug.Log($"{LogPrefix} auto-fix complete.\n{summary}");
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Enemy Type Binding Fix", summary.ToString(), "OK");
        }
    }

    [MenuItem(ExportCsvMenuPath)]
    public static void ExportCsvReport()
    {
        int scannedPrefabs;
        int scannedWaveGroups;
        List<EnemyTypeBindingIssue> issues = CollectIssues(out scannedPrefabs, out scannedWaveGroups);

        string path = EditorUtility.SaveFilePanel(
            "Export Enemy Type Binding Report",
            Path.GetFullPath(Path.GetDirectoryName(DefaultCsvPath) ?? "Assets"),
            Path.GetFileName(DefaultCsvPath),
            "csv");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var csv = new StringBuilder();
        csv.AppendLine("severity,code,prefab,context,archetype_id,message");
        for (int i = 0; i < issues.Count; i++)
        {
            EnemyTypeBindingIssue issue = issues[i];
            string prefabName = issue.prefab != null ? issue.prefab.name : string.Empty;
            csv.Append(Escape(issue.severity.ToString())).Append(',');
            csv.Append(Escape(issue.code.ToString())).Append(',');
            csv.Append(Escape(prefabName)).Append(',');
            csv.Append(Escape(issue.context)).Append(',');
            csv.Append(Escape(issue.normalizedArchetypeId)).Append(',');
            csv.Append(Escape(issue.message)).AppendLine();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
        File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/');
        string normalizedPath = path.Replace('\\', '/');
        if (normalizedPath.StartsWith(projectRoot + "/"))
        {
            normalizedPath = "Assets/" + normalizedPath.Substring(projectRoot.Length + 1);
        }

        Debug.Log($"[EnemyTypeBindingValidation] Exported report: {normalizedPath}");
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Enemy Type Binding Validation",
                $"CSV exported.\nScanned enemy prefabs: {scannedPrefabs}\nScanned stronghold wave groups: {scannedWaveGroups}\nIssue count: {issues.Count}",
                "OK");
        }
    }

    private static List<EnemyTypeBindingIssue> CollectIssues(out int scannedPrefabs, out int scannedWaveGroups)
    {
        var issues = new List<EnemyTypeBindingIssue>();
        scannedPrefabs = 0;
        scannedWaveGroups = 0;

        issues.AddRange(CollectEnemyPrefabIssues(ref scannedPrefabs));
        issues.AddRange(CollectStrongholdWaveGroupIssues(ref scannedWaveGroups));
        return issues;
    }

    private static List<EnemyTypeBindingIssue> CollectSceneIssues(
        out int scannedScenes,
        out int scannedSceneEnemies,
        out int scannedStrongholds,
        out int scannedWaveGroups)
    {
        var issues = new List<EnemyTypeBindingIssue>();
        scannedScenes = 0;
        scannedSceneEnemies = 0;
        scannedStrongholds = 0;
        scannedWaveGroups = 0;

        string[] guids = AssetDatabase.FindAssets("t:Scene");
        for (int i = 0; i < guids.Length; i++)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(scenePath) || !scenePath.StartsWith("Assets/"))
            {
                continue;
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"{LogPrefix} skip scene '{scenePath}' due to open failure: {ex.Message}");
                continue;
            }

            if (!scene.IsValid())
            {
                continue;
            }

            scannedScenes++;
            issues.AddRange(CollectSceneEnemyIssues(scene, scenePath, ref scannedSceneEnemies));
            issues.AddRange(CollectSceneStrongholdIssues(scene, scenePath, ref scannedStrongholds, ref scannedWaveGroups));
        }

        return issues;
    }

    private static List<EnemyTypeBindingIssue> CollectSceneEnemyIssues(
        Scene scene,
        string scenePath,
        ref int scannedSceneEnemies)
    {
        var issues = new List<EnemyTypeBindingIssue>();
        var visitedOwners = new HashSet<int>();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int r = 0; r < roots.Length; r++)
        {
            GameObject root = roots[r];
            if (root == null)
            {
                continue;
            }

            EnemyAI[] ais = root.GetComponentsInChildren<EnemyAI>(true);
            for (int i = 0; i < ais.Length; i++)
            {
                EnemyAI ai = ais[i];
                if (ai == null)
                {
                    continue;
                }

                AddSceneEnemyIssueCandidates(issues, ai, scenePath, visitedOwners, ref scannedSceneEnemies);
            }

            EnemyHealth[] healths = root.GetComponentsInChildren<EnemyHealth>(true);
            for (int i = 0; i < healths.Length; i++)
            {
                EnemyHealth health = healths[i];
                if (health == null)
                {
                    continue;
                }

                AddSceneEnemyIssueCandidates(issues, health, scenePath, visitedOwners, ref scannedSceneEnemies);
            }
        }

        return issues;
    }

    private static void AddSceneEnemyIssueCandidates(
        List<EnemyTypeBindingIssue> issues,
        Component candidate,
        string scenePath,
        HashSet<int> visitedOwners,
        ref int scannedSceneEnemies)
    {
        GameObject owner = ResolveEnemyOwner(candidate);
        if (owner == null)
        {
            return;
        }

        int key = owner.GetInstanceID();
        if (!visitedOwners.Add(key))
        {
            return;
        }

        scannedSceneEnemies++;
        string hierarchyPath = GetHierarchyPath(owner.transform);
        string context = $"Scene:{scenePath}:Enemy:{hierarchyPath}";
        issues.AddRange(EnemyTypeBindingValidation.ValidatePrefabBinding(owner, context, ignoreBossPrefabs: true));
    }

    private static List<EnemyTypeBindingIssue> CollectSceneStrongholdIssues(
        Scene scene,
        string scenePath,
        ref int scannedStrongholds,
        ref int scannedWaveGroups)
    {
        var issues = new List<EnemyTypeBindingIssue>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            GameObject root = roots[r];
            if (root == null)
            {
                continue;
            }

            StrongholdController[] strongholds = root.GetComponentsInChildren<StrongholdController>(true);
            for (int s = 0; s < strongholds.Length; s++)
            {
                StrongholdController stronghold = strongholds[s];
                if (stronghold == null || stronghold.waves == null)
                {
                    continue;
                }

                scannedStrongholds++;
                string strongholdPath = GetHierarchyPath(stronghold.transform);
                string contextPrefix =
                    $"Scene:{scenePath}:Stronghold:{strongholdPath}[{stronghold.StrongholdId}]";

                for (int waveIndex = 0; waveIndex < stronghold.waves.Count; waveIndex++)
                {
                    StrongholdWave wave = stronghold.waves[waveIndex];
                    if (wave == null)
                    {
                        continue;
                    }

                    scannedWaveGroups += ValidateWaveGroups(issues, contextPrefix, wave.groups, waveIndex, "Wave");
                    if (wave.eliteTrigger != null)
                    {
                        scannedWaveGroups += ValidateWaveGroups(
                            issues,
                            contextPrefix,
                            wave.eliteTrigger.eliteGroups,
                            waveIndex,
                            "Elite");
                    }

                    if (wave.events == null)
                    {
                        continue;
                    }

                    for (int eventIndex = 0; eventIndex < wave.events.Count; eventIndex++)
                    {
                        WaveEvent waveEvent = wave.events[eventIndex];
                        if (waveEvent == null || waveEvent.groups == null)
                        {
                            continue;
                        }

                        string eventLabel = $"Event:{waveEvent.eventType}:{eventIndex}";
                        scannedWaveGroups += ValidateWaveGroups(
                            issues,
                            contextPrefix,
                            waveEvent.groups,
                            waveIndex,
                            eventLabel);
                    }
                }
            }
        }

        return issues;
    }

    private static void ExecuteSceneFixes(
        EnemyArchetypeLibrary archetypes,
        PrefabArchetypeIndex prefabIndex,
        EnemyWavePrefabFallbackConfig fallbackConfig,
        List<SceneMissingPrefabChecklistRow> checklistRows,
        out int scannedScenes,
        out int savedScenes,
        out int scannedSceneEnemies,
        out int scannedStrongholds,
        out int scannedWaveGroups,
        out int addedConfigurators,
        out int alignedEnemyTypes,
        out int assignedMissingArchetypes,
        out int missingPrefabReferences,
        out int autoAssignedPrefabReferences)
    {
        scannedScenes = 0;
        savedScenes = 0;
        scannedSceneEnemies = 0;
        scannedStrongholds = 0;
        scannedWaveGroups = 0;
        addedConfigurators = 0;
        alignedEnemyTypes = 0;
        assignedMissingArchetypes = 0;
        missingPrefabReferences = 0;
        autoAssignedPrefabReferences = 0;

        string[] guids = AssetDatabase.FindAssets("t:Scene");
        for (int i = 0; i < guids.Length; i++)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(scenePath) || !scenePath.StartsWith("Assets/"))
            {
                continue;
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"{LogPrefix} skip scene '{scenePath}' due to open failure: {ex.Message}");
                continue;
            }

            if (!scene.IsValid())
            {
                continue;
            }

            scannedScenes++;
            bool sceneDirty = false;

            FixSceneEnemies(
                scene,
                archetypes,
                ref scannedSceneEnemies,
                ref addedConfigurators,
                ref alignedEnemyTypes,
                ref assignedMissingArchetypes,
                ref sceneDirty);

            FixSceneStrongholds(
                scene,
                scenePath,
                prefabIndex,
                fallbackConfig,
                checklistRows,
                ref scannedStrongholds,
                ref scannedWaveGroups,
                ref missingPrefabReferences,
                ref autoAssignedPrefabReferences,
                ref sceneDirty);

            if (sceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (EditorSceneManager.SaveScene(scene))
                {
                    savedScenes++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void FixSceneEnemies(
        Scene scene,
        EnemyArchetypeLibrary archetypes,
        ref int scannedSceneEnemies,
        ref int addedConfigurators,
        ref int alignedEnemyTypes,
        ref int assignedMissingArchetypes,
        ref bool sceneDirty)
    {
        var visitedOwners = new HashSet<int>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            GameObject root = roots[r];
            if (root == null)
            {
                continue;
            }

            EnemyAI[] ais = root.GetComponentsInChildren<EnemyAI>(true);
            for (int i = 0; i < ais.Length; i++)
            {
                EnemyAI ai = ais[i];
                if (ai == null)
                {
                    continue;
                }

                FixSceneEnemyCandidate(
                    ai,
                    archetypes,
                    visitedOwners,
                    ref scannedSceneEnemies,
                    ref addedConfigurators,
                    ref alignedEnemyTypes,
                    ref assignedMissingArchetypes,
                    ref sceneDirty);
            }

            EnemyHealth[] healths = root.GetComponentsInChildren<EnemyHealth>(true);
            for (int i = 0; i < healths.Length; i++)
            {
                EnemyHealth health = healths[i];
                if (health == null)
                {
                    continue;
                }

                FixSceneEnemyCandidate(
                    health,
                    archetypes,
                    visitedOwners,
                    ref scannedSceneEnemies,
                    ref addedConfigurators,
                    ref alignedEnemyTypes,
                    ref assignedMissingArchetypes,
                    ref sceneDirty);
            }
        }
    }

    private static void FixSceneEnemyCandidate(
        Component candidate,
        EnemyArchetypeLibrary archetypes,
        HashSet<int> visitedOwners,
        ref int scannedSceneEnemies,
        ref int addedConfigurators,
        ref int alignedEnemyTypes,
        ref int assignedMissingArchetypes,
        ref bool sceneDirty)
    {
        GameObject owner = ResolveEnemyOwner(candidate);
        if (owner == null)
        {
            return;
        }

        int key = owner.GetInstanceID();
        if (!visitedOwners.Add(key))
        {
            return;
        }

        if (EnemyTypeBindingValidation.IsBossLikePrefab(owner))
        {
            return;
        }

        EnemyAI ai = owner.GetComponentInChildren<EnemyAI>(true);
        EnemyHealth health = owner.GetComponentInChildren<EnemyHealth>(true);
        if (ai == null && health == null)
        {
            return;
        }

        scannedSceneEnemies++;
        EnemyArchetypeConfigurator configurator = owner.GetComponentInChildren<EnemyArchetypeConfigurator>(true);
        if (configurator == null)
        {
            GameObject configuratorOwner = health != null ? health.gameObject : owner;
            configurator = configuratorOwner.AddComponent<EnemyArchetypeConfigurator>();
            EditorUtility.SetDirty(configurator);
            addedConfigurators++;
            sceneDirty = true;
        }

        if (configurator != null && configurator.archetype == null)
        {
            EnemyArchetype fallback = ResolveArchetypeForHealth(archetypes, health);
            if (fallback != null)
            {
                bool assigned = false;

                if (configurator.archetype != fallback)
                {
                    configurator.archetype = fallback;
                    assigned = true;
                }

                var serialized = new SerializedObject(configurator);
                SerializedProperty archetypeProp = serialized.FindProperty("archetype");
                if (archetypeProp != null && archetypeProp.objectReferenceValue == null)
                {
                    archetypeProp.objectReferenceValue = fallback;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    assigned = true;
                }

                if (assigned)
                {
                    EditorUtility.SetDirty(configurator);
                    assignedMissingArchetypes++;
                    sceneDirty = true;
                }
            }
            else
            {
                Debug.LogWarning(
                    $"{LogPrefix} fallback archetype not found for scene enemy '{GetHierarchyPath(owner.transform)}'.");
            }
        }

        if (configurator != null && configurator.archetype != null && health != null)
        {
            if (health.enemyType != configurator.archetype.enemyType)
            {
                health.enemyType = configurator.archetype.enemyType;
                EditorUtility.SetDirty(health);
                alignedEnemyTypes++;
                sceneDirty = true;
            }
        }
    }

    private static void FixSceneStrongholds(
        Scene scene,
        string scenePath,
        PrefabArchetypeIndex prefabIndex,
        EnemyWavePrefabFallbackConfig fallbackConfig,
        List<SceneMissingPrefabChecklistRow> checklistRows,
        ref int scannedStrongholds,
        ref int scannedWaveGroups,
        ref int missingPrefabReferences,
        ref int autoAssignedPrefabReferences,
        ref bool sceneDirty)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            GameObject root = roots[r];
            if (root == null)
            {
                continue;
            }

            StrongholdController[] strongholds = root.GetComponentsInChildren<StrongholdController>(true);
            for (int s = 0; s < strongholds.Length; s++)
            {
                StrongholdController stronghold = strongholds[s];
                if (stronghold == null || stronghold.waves == null)
                {
                    continue;
                }

                scannedStrongholds++;
                string hierarchyPath = GetHierarchyPath(stronghold.transform);
                string strongholdId = stronghold.StrongholdId;

                for (int waveIndex = 0; waveIndex < stronghold.waves.Count; waveIndex++)
                {
                    StrongholdWave wave = stronghold.waves[waveIndex];
                    if (wave == null)
                    {
                        continue;
                    }

                    FixSceneWaveGroups(
                        scenePath,
                        hierarchyPath,
                        strongholdId,
                        "Wave",
                        waveIndex,
                        wave.groups,
                        prefabIndex,
                        fallbackConfig,
                        checklistRows,
                        ref scannedWaveGroups,
                        ref missingPrefabReferences,
                        ref autoAssignedPrefabReferences,
                        ref sceneDirty);

                    if (wave.eliteTrigger != null)
                    {
                        FixSceneWaveGroups(
                            scenePath,
                            hierarchyPath,
                            strongholdId,
                            "Elite",
                            waveIndex,
                            wave.eliteTrigger.eliteGroups,
                            prefabIndex,
                            fallbackConfig,
                            checklistRows,
                            ref scannedWaveGroups,
                            ref missingPrefabReferences,
                            ref autoAssignedPrefabReferences,
                            ref sceneDirty);
                    }

                    if (wave.events == null)
                    {
                        continue;
                    }

                    for (int eventIndex = 0; eventIndex < wave.events.Count; eventIndex++)
                    {
                        WaveEvent waveEvent = wave.events[eventIndex];
                        if (waveEvent == null || waveEvent.groups == null)
                        {
                            continue;
                        }

                        string stageLabel = $"Event:{waveEvent.eventType}:{eventIndex}";
                        FixSceneWaveGroups(
                            scenePath,
                            hierarchyPath,
                            strongholdId,
                            stageLabel,
                            waveIndex,
                            waveEvent.groups,
                            prefabIndex,
                            fallbackConfig,
                            checklistRows,
                            ref scannedWaveGroups,
                            ref missingPrefabReferences,
                            ref autoAssignedPrefabReferences,
                            ref sceneDirty);
                    }
                }
            }
        }
    }

    private static void FixSceneWaveGroups(
        string scenePath,
        string strongholdPath,
        string strongholdId,
        string stageLabel,
        int waveIndex,
        List<WaveSpawnGroup> groups,
        PrefabArchetypeIndex prefabIndex,
        EnemyWavePrefabFallbackConfig fallbackConfig,
        List<SceneMissingPrefabChecklistRow> checklistRows,
        ref int scannedWaveGroups,
        ref int missingPrefabReferences,
        ref int autoAssignedPrefabReferences,
        ref bool sceneDirty)
    {
        if (groups == null || groups.Count == 0)
        {
            return;
        }

        for (int i = 0; i < groups.Count; i++)
        {
            WaveSpawnGroup group = groups[i];
            if (group == null)
            {
                continue;
            }

            scannedWaveGroups++;
            if (group.prefab != null)
            {
                continue;
            }

            missingPrefabReferences++;
            string archetypeId = group.archetypeOverride != null
                ? EnemyArchetypeValidation.NormalizeArchetypeId(group.archetypeOverride.archetypeId)
                : string.Empty;

            List<PrefabCandidate> candidates = ResolvePrefabCandidatesForGroup(
                group,
                prefabIndex,
                fallbackConfig,
                stageLabel);
            bool autoAssigned = false;
            string status;
            string note;
            string suggested = JoinCandidatePaths(candidates);

            if (candidates.Count == 1)
            {
                group.prefab = candidates[0].prefab;
                if (!string.IsNullOrEmpty(strongholdId))
                {
                    StrongholdController[] strongholds = Object.FindObjectsOfType<StrongholdController>(true);
                    for (int s = 0; s < strongholds.Length; s++)
                    {
                        StrongholdController stronghold = strongholds[s];
                        if (stronghold != null && stronghold.StrongholdId == strongholdId)
                        {
                            EditorUtility.SetDirty(stronghold);
                            break;
                        }
                    }
                }
                autoAssigned = true;
                autoAssignedPrefabReferences++;
                sceneDirty = true;
                status = "AutoAssigned";
                note = $"Assigned unique candidate: {candidates[0].assetPath}";
            }
            else if (candidates.Count > 1)
            {
                status = "NeedsManualPick";
                note = $"Multiple candidates ({candidates.Count}) for archetype.";
            }
            else
            {
                status = "MissingNoCandidate";
                note = string.IsNullOrEmpty(archetypeId)
                    ? "No archetypeOverride and no deterministic fallback."
                    : $"No prefab candidate found for archetypeId '{archetypeId}'.";
            }

            checklistRows.Add(new SceneMissingPrefabChecklistRow
            {
                scenePath = scenePath,
                strongholdPath = strongholdPath,
                strongholdId = strongholdId,
                stageLabel = stageLabel,
                waveIndex = waveIndex,
                groupIndex = i,
                archetypeId = archetypeId,
                status = status,
                suggestedPrefabs = suggested,
                note = note,
                autoAssigned = autoAssigned
            });
        }
    }

    private static List<PrefabCandidate> ResolvePrefabCandidatesForGroup(
        WaveSpawnGroup group,
        PrefabArchetypeIndex prefabIndex,
        EnemyWavePrefabFallbackConfig fallbackConfig,
        string stageLabel)
    {
        var candidates = new List<PrefabCandidate>();
        if (group == null || prefabIndex == null)
        {
            return candidates;
        }

        if (group.archetypeOverride != null)
        {
            string archetypeId = EnemyArchetypeValidation.NormalizeArchetypeId(group.archetypeOverride.archetypeId);
            if (!string.IsNullOrEmpty(archetypeId)
                && prefabIndex.byArchetypeId.TryGetValue(archetypeId, out List<PrefabCandidate> byId))
            {
                candidates.AddRange(byId);
            }

            if (candidates.Count == 0
                && prefabIndex.byEnemyType.TryGetValue(group.archetypeOverride.enemyType, out List<PrefabCandidate> byType))
            {
                candidates.AddRange(byType);
            }
        }
        else
        {
            PrefabCandidate fallback = PickDeterministicStageFallbackCandidate(
                prefabIndex,
                fallbackConfig,
                stageLabel);
            if (fallback != null)
            {
                candidates.Add(fallback);
            }
        }

        return DeduplicateCandidates(candidates);
    }

    private static PrefabCandidate PickDeterministicStageFallbackCandidate(
        PrefabArchetypeIndex prefabIndex,
        EnemyWavePrefabFallbackConfig fallbackConfig,
        string stageLabel)
    {
        if (prefabIndex == null)
        {
            return null;
        }

        string[] preferredArchetypeIds = GetPreferredFallbackArchetypeIds(fallbackConfig, stageLabel);
        for (int i = 0; i < preferredArchetypeIds.Length; i++)
        {
            string archetypeId = preferredArchetypeIds[i];
            if (string.IsNullOrEmpty(archetypeId))
            {
                continue;
            }

            if (!prefabIndex.byArchetypeId.TryGetValue(archetypeId, out List<PrefabCandidate> candidatesById))
            {
                continue;
            }

            PrefabCandidate picked = SelectBestCandidate(candidatesById);
            if (picked != null)
            {
                return picked;
            }
        }

        var allCandidates = new List<PrefabCandidate>();
        foreach (KeyValuePair<string, List<PrefabCandidate>> kv in prefabIndex.byArchetypeId)
        {
            List<PrefabCandidate> candidates = kv.Value;
            if (candidates == null || candidates.Count == 0)
            {
                continue;
            }

            allCandidates.AddRange(candidates);
        }

        return SelectBestCandidate(allCandidates);
    }

    private static string[] GetPreferredFallbackArchetypeIds(
        EnemyWavePrefabFallbackConfig fallbackConfig,
        string stageLabel)
    {
        if (fallbackConfig != null)
        {
            IReadOnlyList<string> configured = fallbackConfig.GetPreferredArchetypeIds(stageLabel);
            if (configured != null && configured.Count > 0)
            {
                var ids = new string[configured.Count];
                for (int i = 0; i < configured.Count; i++)
                {
                    ids[i] = EnemyArchetypeValidation.NormalizeArchetypeId(configured[i]);
                }

                return ids;
            }
        }

        return GetBuiltInPreferredFallbackArchetypeIds(stageLabel);
    }

    private static string[] GetBuiltInPreferredFallbackArchetypeIds(string stageLabel)
    {
        if (string.IsNullOrEmpty(stageLabel))
        {
            return new[] { "grunt", "rusher", "ranged", "tank", "controller", "suicide", "elite" };
        }

        if (stageLabel.StartsWith("Elite", System.StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "elite", "tank", "controller", "rusher", "grunt", "ranged", "suicide" };
        }

        if (stageLabel.StartsWith("Event:Chase", System.StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "rusher", "grunt", "ranged", "tank", "controller", "suicide", "elite" };
        }

        if (stageLabel.StartsWith("Event:Reinforcement", System.StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "rusher", "grunt", "ranged", "tank", "controller", "suicide", "elite" };
        }

        if (stageLabel.StartsWith("Event:ProtectTarget", System.StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "tank", "controller", "grunt", "ranged", "rusher", "suicide", "elite" };
        }

        if (stageLabel.StartsWith("Event:HoldPoint", System.StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "controller", "tank", "grunt", "ranged", "rusher", "suicide", "elite" };
        }

        return new[] { "grunt", "rusher", "ranged", "tank", "controller", "suicide", "elite" };
    }

    private static EnemyWavePrefabFallbackConfig LoadOrCreateWavePrefabFallbackConfig()
    {
        EnemyWavePrefabFallbackConfig fallbackConfig =
            AssetDatabase.LoadAssetAtPath<EnemyWavePrefabFallbackConfig>(DefaultFallbackConfigPath);

        if (fallbackConfig == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:EnemyWavePrefabFallbackConfig");
            if (guids.Length > 0)
            {
                string foundPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                fallbackConfig = AssetDatabase.LoadAssetAtPath<EnemyWavePrefabFallbackConfig>(foundPath);
            }
        }

        bool created = false;
        if (fallbackConfig == null)
        {
            fallbackConfig = ScriptableObject.CreateInstance<EnemyWavePrefabFallbackConfig>();
            string fullPath = Path.GetFullPath(DefaultFallbackConfigPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(fallbackConfig, DefaultFallbackConfigPath);
            created = true;
        }

        bool changed = EnsureFallbackConfigDefaults(fallbackConfig);
        if (created || changed)
        {
            EditorUtility.SetDirty(fallbackConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        if (created)
        {
            Debug.Log($"{LogPrefix} created default fallback config: {DefaultFallbackConfigPath}");
        }

        return fallbackConfig;
    }

    private static bool EnsureFallbackConfigDefaults(EnemyWavePrefabFallbackConfig fallbackConfig)
    {
        if (fallbackConfig == null)
        {
            return false;
        }

        bool changed = false;
        if (fallbackConfig.defaultArchetypeIds == null)
        {
            fallbackConfig.defaultArchetypeIds = new List<string>();
            changed = true;
        }

        if (fallbackConfig.defaultArchetypeIds.Count == 0)
        {
            fallbackConfig.defaultArchetypeIds.AddRange(GetBuiltInPreferredFallbackArchetypeIds(string.Empty));
            changed = true;
        }

        if (fallbackConfig.stageRules == null)
        {
            fallbackConfig.stageRules = new List<EnemyWavePrefabFallbackConfig.StageFallbackRule>();
            changed = true;
        }

        if (fallbackConfig.stageRules.Count == 0)
        {
            fallbackConfig.stageRules.Add(
                CreateStageFallbackRule("Elite", GetBuiltInPreferredFallbackArchetypeIds("Elite")));
            fallbackConfig.stageRules.Add(
                CreateStageFallbackRule("Event:Chase", GetBuiltInPreferredFallbackArchetypeIds("Event:Chase")));
            fallbackConfig.stageRules.Add(
                CreateStageFallbackRule(
                    "Event:Reinforcement",
                    GetBuiltInPreferredFallbackArchetypeIds("Event:Reinforcement")));
            fallbackConfig.stageRules.Add(
                CreateStageFallbackRule(
                    "Event:ProtectTarget",
                    GetBuiltInPreferredFallbackArchetypeIds("Event:ProtectTarget")));
            fallbackConfig.stageRules.Add(
                CreateStageFallbackRule("Event:HoldPoint", GetBuiltInPreferredFallbackArchetypeIds("Event:HoldPoint")));
            changed = true;
        }

        return changed;
    }

    private static EnemyWavePrefabFallbackConfig.StageFallbackRule CreateStageFallbackRule(
        string stagePrefix,
        string[] archetypeIds)
    {
        var rule = new EnemyWavePrefabFallbackConfig.StageFallbackRule
        {
            stagePrefix = stagePrefix,
            archetypeIds = new List<string>()
        };

        if (archetypeIds == null || archetypeIds.Length == 0)
        {
            return rule;
        }

        var seen = new HashSet<string>();
        for (int i = 0; i < archetypeIds.Length; i++)
        {
            string normalized = EnemyArchetypeValidation.NormalizeArchetypeId(archetypeIds[i]);
            if (string.IsNullOrEmpty(normalized) || !seen.Add(normalized))
            {
                continue;
            }

            rule.archetypeIds.Add(normalized);
        }

        return rule;
    }

    private static PrefabCandidate SelectBestCandidate(List<PrefabCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        PrefabCandidate best = null;
        int bestScore = int.MinValue;
        string bestPath = string.Empty;

        for (int i = 0; i < candidates.Count; i++)
        {
            PrefabCandidate candidate = candidates[i];
            if (candidate == null || candidate.prefab == null || string.IsNullOrEmpty(candidate.assetPath))
            {
                continue;
            }

            string path = candidate.assetPath;
            int score = 0;
            if (path.IndexOf("Assets/Prefabs/Enemies/", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 100;
            }

            if (path.IndexOf("Assets/fbx/", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score -= 10;
            }

            if (best == null
                || score > bestScore
                || (score == bestScore
                    && string.Compare(path, bestPath, System.StringComparison.OrdinalIgnoreCase) < 0))
            {
                best = candidate;
                bestScore = score;
                bestPath = path;
            }
        }

        return best;
    }

    private static List<PrefabCandidate> DeduplicateCandidates(List<PrefabCandidate> candidates)
    {
        if (candidates == null || candidates.Count <= 1)
        {
            return candidates ?? new List<PrefabCandidate>();
        }

        var unique = new List<PrefabCandidate>(candidates.Count);
        var seen = new HashSet<string>();
        for (int i = 0; i < candidates.Count; i++)
        {
            PrefabCandidate candidate = candidates[i];
            if (candidate == null || string.IsNullOrEmpty(candidate.assetPath))
            {
                continue;
            }

            if (seen.Add(candidate.assetPath))
            {
                unique.Add(candidate);
            }
        }

        return unique;
    }

    private static PrefabArchetypeIndex BuildPrefabArchetypeIndex()
    {
        var index = new PrefabArchetypeIndex();
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            EnemyAI ai = prefab.GetComponentInChildren<EnemyAI>(true);
            EnemyHealth health = prefab.GetComponentInChildren<EnemyHealth>(true);
            if (ai == null && health == null)
            {
                continue;
            }

            if (EnemyTypeBindingValidation.IsBossLikePrefab(prefab))
            {
                continue;
            }

            EnemyArchetypeConfigurator configurator = prefab.GetComponentInChildren<EnemyArchetypeConfigurator>(true);
            if (configurator == null || configurator.archetype == null)
            {
                continue;
            }

            string archetypeId = EnemyArchetypeValidation.NormalizeArchetypeId(configurator.archetype.archetypeId);
            var candidate = new PrefabCandidate
            {
                prefab = prefab,
                assetPath = path,
                archetypeId = archetypeId,
                enemyType = configurator.archetype.enemyType
            };

            if (!string.IsNullOrEmpty(archetypeId))
            {
                if (!index.byArchetypeId.TryGetValue(archetypeId, out List<PrefabCandidate> byId))
                {
                    byId = new List<PrefabCandidate>();
                    index.byArchetypeId.Add(archetypeId, byId);
                }
                byId.Add(candidate);
            }

            if (!index.byEnemyType.TryGetValue(candidate.enemyType, out List<PrefabCandidate> byType))
            {
                byType = new List<PrefabCandidate>();
                index.byEnemyType.Add(candidate.enemyType, byType);
            }
            byType.Add(candidate);
        }

        return index;
    }

    private static string JoinCandidatePaths(List<PrefabCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(" | ");
            }

            builder.Append(candidates[i].assetPath);
        }

        return builder.ToString();
    }

    private static string ExportSceneChecklistCsv(List<SceneMissingPrefabChecklistRow> rows)
    {
        string assetPath = DefaultSceneChecklistCsvPath;
        string fullPath = Path.GetFullPath(assetPath);
        string directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var csv = new StringBuilder();
        csv.AppendLine("scene,stronghold_path,stronghold_id,stage,wave,group,archetype_id,status,auto_assigned,suggested_prefabs,note");
        if (rows != null)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                SceneMissingPrefabChecklistRow row = rows[i];
                csv.Append(Escape(row.scenePath)).Append(',');
                csv.Append(Escape(row.strongholdPath)).Append(',');
                csv.Append(Escape(row.strongholdId)).Append(',');
                csv.Append(Escape(row.stageLabel)).Append(',');
                csv.Append(Escape(row.waveIndex.ToString())).Append(',');
                csv.Append(Escape(row.groupIndex.ToString())).Append(',');
                csv.Append(Escape(row.archetypeId)).Append(',');
                csv.Append(Escape(row.status)).Append(',');
                csv.Append(Escape(row.autoAssigned ? "true" : "false")).Append(',');
                csv.Append(Escape(row.suggestedPrefabs)).Append(',');
                csv.Append(Escape(row.note)).AppendLine();
            }
        }

        File.WriteAllText(fullPath, csv.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        return assetPath;
    }

    private static List<EnemyTypeBindingIssue> CollectEnemyPrefabIssues(ref int scannedPrefabs)
    {
        var issues = new List<EnemyTypeBindingIssue>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            EnemyAI ai = prefab.GetComponentInChildren<EnemyAI>(true);
            EnemyHealth health = prefab.GetComponentInChildren<EnemyHealth>(true);
            if (ai == null && health == null)
            {
                continue;
            }

            scannedPrefabs++;
            string context = $"Prefab:{path}";
            issues.AddRange(EnemyTypeBindingValidation.ValidatePrefabBinding(prefab, context, ignoreBossPrefabs: true));
        }

        return issues;
    }

    private static List<EnemyTypeBindingIssue> CollectStrongholdWaveGroupIssues(ref int scannedWaveGroups)
    {
        var issues = new List<EnemyTypeBindingIssue>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            StrongholdController stronghold = prefab.GetComponentInChildren<StrongholdController>(true);
            if (stronghold == null || stronghold.waves == null)
            {
                continue;
            }

            for (int waveIndex = 0; waveIndex < stronghold.waves.Count; waveIndex++)
            {
                StrongholdWave wave = stronghold.waves[waveIndex];
                if (wave == null)
                {
                    continue;
                }

                string contextPrefix = $"StrongholdPrefab:{path}";
                scannedWaveGroups += ValidateWaveGroups(issues, contextPrefix, wave.groups, waveIndex, "Wave");
                if (wave.eliteTrigger != null)
                {
                    scannedWaveGroups += ValidateWaveGroups(
                        issues,
                        contextPrefix,
                        wave.eliteTrigger.eliteGroups,
                        waveIndex,
                        "Elite");
                }

                if (wave.events == null)
                {
                    continue;
                }

                for (int eventIndex = 0; eventIndex < wave.events.Count; eventIndex++)
                {
                    WaveEvent waveEvent = wave.events[eventIndex];
                    if (waveEvent == null || waveEvent.groups == null)
                    {
                        continue;
                    }

                        string eventLabel = $"Event:{waveEvent.eventType}:{eventIndex}";
                        scannedWaveGroups += ValidateWaveGroups(
                            issues,
                            contextPrefix,
                            waveEvent.groups,
                            waveIndex,
                            eventLabel);
                }
            }
        }

        return issues;
    }

    private static int ValidateWaveGroups(
        List<EnemyTypeBindingIssue> issues,
        string contextPrefix,
        List<WaveSpawnGroup> groups,
        int waveIndex,
        string stageLabel)
    {
        if (groups == null || groups.Count == 0)
        {
            return 0;
        }

        int scanned = 0;
        for (int i = 0; i < groups.Count; i++)
        {
            WaveSpawnGroup group = groups[i];
            if (group == null)
            {
                continue;
            }

            scanned++;
            string context = $"{contextPrefix}::{stageLabel}:Wave{waveIndex}:Group{i}";
            issues.AddRange(EnemyTypeBindingValidation.ValidateWaveGroupBinding(group, context, ignoreBossPrefabs: true));
        }

        return scanned;
    }

    private static GameObject ResolveEnemyOwner(Component component)
    {
        if (component == null)
        {
            return null;
        }

        Transform current = component.transform;
        while (current != null && current.parent != null)
        {
            Transform parent = current.parent;
            if (parent.GetComponent<EnemyAI>() == null && parent.GetComponent<EnemyHealth>() == null)
            {
                break;
            }

            current = parent;
        }

        return current != null ? current.gameObject : null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        var builder = new StringBuilder(transform.name);
        Transform current = transform.parent;
        while (current != null)
        {
            builder.Insert(0, '/');
            builder.Insert(0, current.name);
            current = current.parent;
        }

        return builder.ToString();
    }

    private static void LogIssuesToConsole(
        IReadOnlyList<EnemyTypeBindingIssue> issues,
        out int errorCount,
        out int warningCount)
    {
        errorCount = 0;
        warningCount = 0;

        for (int i = 0; i < issues.Count; i++)
        {
            EnemyTypeBindingIssue issue = issues[i];
            string prefix = issue.severity == EnemyTypeBindingIssueSeverity.Error ? "ERROR" : "WARN";
            string ownerName = issue.prefab != null ? issue.prefab.name : "<null>";
            string message =
                $"[EnemyTypeBindingValidation] [{prefix}] [{issue.code}] {ownerName} ({issue.context}): {issue.message}";

            if (issue.severity == EnemyTypeBindingIssueSeverity.Error)
            {
                errorCount++;
                Debug.LogError(message, issue.prefab);
            }
            else
            {
                warningCount++;
                Debug.LogWarning(message, issue.prefab);
            }
        }
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        string escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private sealed class EnemyArchetypeLibrary
    {
        public readonly Dictionary<string, EnemyArchetype> byId = new Dictionary<string, EnemyArchetype>();
        public readonly Dictionary<EnemyType, EnemyArchetype> byEnemyType = new Dictionary<EnemyType, EnemyArchetype>();
    }

    private sealed class PrefabArchetypeIndex
    {
        public readonly Dictionary<string, List<PrefabCandidate>> byArchetypeId =
            new Dictionary<string, List<PrefabCandidate>>();

        public readonly Dictionary<EnemyType, List<PrefabCandidate>> byEnemyType =
            new Dictionary<EnemyType, List<PrefabCandidate>>();
    }

    private sealed class PrefabCandidate
    {
        public GameObject prefab;
        public string assetPath;
        public string archetypeId;
        public EnemyType enemyType;
    }

    private struct SceneMissingPrefabChecklistRow
    {
        public string scenePath;
        public string strongholdPath;
        public string strongholdId;
        public string stageLabel;
        public int waveIndex;
        public int groupIndex;
        public string archetypeId;
        public string status;
        public bool autoAssigned;
        public string suggestedPrefabs;
        public string note;
    }

    private static EnemyArchetypeLibrary BuildArchetypeLibrary()
    {
        var library = new EnemyArchetypeLibrary();
        string[] guids = AssetDatabase.FindAssets("t:EnemyArchetype");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EnemyArchetype archetype = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(path);
            if (archetype == null)
            {
                continue;
            }

            string id = EnemyArchetypeValidation.NormalizeArchetypeId(archetype.archetypeId);
            if (!string.IsNullOrEmpty(id) && !library.byId.ContainsKey(id))
            {
                library.byId.Add(id, archetype);
            }

            if (!library.byEnemyType.ContainsKey(archetype.enemyType))
            {
                library.byEnemyType.Add(archetype.enemyType, archetype);
            }
        }

        return library;
    }

    private static EnemyArchetype ResolveArchetypeForHealth(EnemyArchetypeLibrary library, EnemyHealth health)
    {
        if (library == null)
        {
            return null;
        }

        if (health != null
            && library.byEnemyType.TryGetValue(health.enemyType, out EnemyArchetype direct)
            && direct != null)
        {
            return direct;
        }

        // Fallback preference order for non-direct mappings.
        if (library.byId.TryGetValue("grunt", out EnemyArchetype grunt)
            && grunt != null)
        {
            return grunt;
        }

        EnemyArchetype explicitGrunt =
            AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/GameDesign/Data/EnemyArchetype_Grunt.asset");
        if (explicitGrunt != null)
        {
            return explicitGrunt;
        }

        if (library.byId.Count > 0)
        {
            var keyDump = new StringBuilder();
            foreach (KeyValuePair<string, EnemyArchetype> kv in library.byId)
            {
                if (keyDump.Length > 0)
                {
                    keyDump.Append(',');
                }

                keyDump.Append(kv.Key);
            }

            Debug.LogWarning(
                $"{LogPrefix} could not resolve grunt fallback archetype. Known ids: {keyDump}");
        }

        foreach (KeyValuePair<string, EnemyArchetype> kv in library.byId)
        {
            return kv.Value;
        }

        return null;
    }
}
