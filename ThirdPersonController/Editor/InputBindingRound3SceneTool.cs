using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class InputBindingRound3SceneTool
    {
        private const string SceneReportAssetPath = "Assets/ThirdPersonController/Reports/input_binding_round3_scene_audit.csv";
        private const string FullGateReportAssetPath = "Assets/ThirdPersonController/Reports/input_binding_round3_full_gate_audit.csv";
        private const string InputHandlerScriptAssetPath = "Assets/ThirdPersonController/Scripts/Player/PlayerInputHandler.cs";

        private const string WhitelistPathMenuRetry = "<keyboard>/r";
        private const string WhitelistOwnerMenuRetry = "Gameplay.MenuRetry";
        private const string WhitelistOwnerSkill4 = "Gameplay.Skill4";

        private const string WhitelistPathEscape = "<keyboard>/escape";
        private const string WhitelistOwnerMenuCancel = "Gameplay.MenuCancel";
        private const string WhitelistOwnerQuitMenu = "Gameplay.QuitMenu";

        [MenuItem("Tools/Input/Round3/Validate Scene Bindings")]
        public static void ValidateSceneBindings()
        {
            List<AuditEntry> entries = new List<AuditEntry>();
            RunSceneAudit(applyFixes: false, entries, out int sceneCount, out int mismatchCount, out int fixedCount);
            WriteReport(entries, SceneReportAssetPath);
            AssetDatabase.Refresh();

            Debug.Log(
                $"[InputRound3] Scene validate complete | scenes={sceneCount} checks={entries.Count} " +
                $"mismatches={mismatchCount} fixed={fixedCount} report={SceneReportAssetPath}");
        }

        [MenuItem("Tools/Input/Round3/Apply Scene Bindings")]
        public static void ApplySceneBindings()
        {
            RunApplySceneBindings(promptUser: !Application.isBatchMode, failOnMismatch: false);
        }

        public static void ApplySceneBindingsForBatch()
        {
            RunApplySceneBindings(promptUser: false, failOnMismatch: true);
        }

        [MenuItem("Tools/Input/Round3/Validate Full Gate")]
        public static void ValidateFullGate()
        {
            RunFullGate(failOnMismatch: false);
        }

        public static void ValidateFullGateForBatch()
        {
            RunFullGate(failOnMismatch: true);
        }

        private static void RunFullGate(bool failOnMismatch)
        {
            List<AuditEntry> entries = new List<AuditEntry>();
            int mismatchCount = 0;
            int fixedCount = 0;

            AuditScriptDefaults(entries, ref mismatchCount);
            AuditInputHandlerSource(entries, ref mismatchCount);
            AuditWhitelistConsistency(entries, ref mismatchCount);

            RunSceneAudit(applyFixes: false, entries, out int sceneCount, out int sceneMismatchCount, out _);
            mismatchCount += sceneMismatchCount;

            WriteReport(entries, FullGateReportAssetPath);
            AssetDatabase.Refresh();

            Debug.Log(
                $"[InputRound3] Full gate complete | scenes={sceneCount} checks={entries.Count} " +
                $"mismatches={mismatchCount} fixed={fixedCount} report={FullGateReportAssetPath}");

            if (failOnMismatch && mismatchCount > 0)
            {
                throw new Exception($"InputRound3 full gate failed. mismatchCount={mismatchCount}");
            }
        }

        private static void RunApplySceneBindings(bool promptUser, bool failOnMismatch)
        {
            if (promptUser)
            {
                bool applyConfirmed = EditorUtility.DisplayDialog(
                    "Apply Round3 Input Binding Fixes",
                    "This will open each scene, apply the Round3 key defaults, and save changed scenes. Continue?",
                    "Apply",
                    "Cancel");
                if (!applyConfirmed)
                {
                    return;
                }
            }

            List<AuditEntry> entries = new List<AuditEntry>();
            RunSceneAudit(applyFixes: true, entries, out int sceneCount, out int mismatchCount, out int fixedCount);
            WriteReport(entries, SceneReportAssetPath);
            AssetDatabase.Refresh();

            int unresolved = CountEntriesByStatus(entries, "Mismatch");
            Debug.Log(
                $"[InputRound3] Scene apply complete | scenes={sceneCount} checks={entries.Count} " +
                $"mismatches={mismatchCount} fixed={fixedCount} unresolved={unresolved} report={SceneReportAssetPath}");

            if (failOnMismatch && unresolved > 0)
            {
                throw new Exception($"InputRound3 scene apply failed. unresolved={unresolved}");
            }
        }

        private static void RunSceneAudit(
            bool applyFixes,
            List<AuditEntry> entries,
            out int sceneCount,
            out int mismatchCount,
            out int fixedCount)
        {
            sceneCount = 0;
            mismatchCount = 0;
            fixedCount = 0;

            SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
                for (int i = 0; i < sceneGuids.Length; i++)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                    if (!IsSupportedScenePath(scenePath))
                    {
                        continue;
                    }

                    UnityEngine.SceneManagement.Scene scene;
                    try
                    {
                        scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    }
                    catch (Exception ex)
                    {
                        AddEntry(
                            entries,
                            scope: "Scene",
                            target: scenePath,
                            field: "OpenScene",
                            current: "Error",
                            expected: "Readable",
                            status: "Skipped",
                            note: ex.Message);
                        continue;
                    }

                    sceneCount++;
                    bool sceneDirty = false;
                    AuditScene(scene.path, applyFixes, ref sceneDirty, entries, ref mismatchCount, ref fixedCount);

                    if (applyFixes && sceneDirty)
                    {
                        EditorSceneManager.SaveScene(scene);
                    }
                }
            }
            finally
            {
                if (!Application.isBatchMode)
                {
                    try
                    {
                        EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[InputRound3] RestoreSceneManagerSetup skipped: {ex.Message}");
                    }
                }
            }
        }

        private static void AuditScriptDefaults(List<AuditEntry> entries, ref int mismatchCount)
        {
            GameObject temp = new GameObject("__InputRound3_DefaultAudit__");
            temp.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                QuickEnemySpawner spawner = temp.AddComponent<QuickEnemySpawner>();
                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(QuickEnemySpawner),
                    field: nameof(QuickEnemySpawner.spawnKey),
                    current: spawner.spawnKey.ToString(),
                    expected: KeyCode.G.ToString());

                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(QuickEnemySpawner),
                    field: nameof(QuickEnemySpawner.stressSpawnKey),
                    current: spawner.stressSpawnKey.ToString(),
                    expected: KeyCode.F8.ToString());

                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(QuickEnemySpawner),
                    field: nameof(QuickEnemySpawner.clearKey),
                    current: spawner.clearKey.ToString(),
                    expected: KeyCode.Delete.ToString());

                UI_TalentEquipmentOverlay talentOverlay = temp.AddComponent<UI_TalentEquipmentOverlay>();
                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(UI_TalentEquipmentOverlay),
                    field: nameof(UI_TalentEquipmentOverlay.toggleKey),
                    current: talentOverlay.toggleKey.ToString(),
                    expected: KeyCode.U.ToString());

                UI_HudHints hudHints = temp.AddComponent<UI_HudHints>();
                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(UI_HudHints),
                    field: nameof(UI_HudHints.toggleKey),
                    current: hudHints.toggleKey.ToString(),
                    expected: KeyCode.H.ToString());

                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(UI_HudHints),
                    field: nameof(UI_HudHints.economyKey),
                    current: hudHints.economyKey.ToString(),
                    expected: KeyCode.Y.ToString());

                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(UI_HudHints),
                    field: nameof(UI_HudHints.talentKey),
                    current: hudHints.talentKey.ToString(),
                    expected: KeyCode.U.ToString());

                ComboDebugger comboDebugger = temp.AddComponent<ComboDebugger>();
                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(ComboDebugger),
                    field: nameof(ComboDebugger.statusKey),
                    current: comboDebugger.statusKey.ToString(),
                    expected: KeyCode.Tab.ToString());

                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(ComboDebugger),
                    field: nameof(ComboDebugger.resetHintKey),
                    current: comboDebugger.resetHintKey.ToString(),
                    expected: KeyCode.F7.ToString());

                PlayerInputHandler inputHandler = temp.AddComponent<PlayerInputHandler>();
                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(PlayerInputHandler),
                    field: nameof(PlayerInputHandler.skill3Key),
                    current: inputHandler.skill3Key.ToString(),
                    expected: KeyCode.C.ToString());

                SkillManager skillManager = temp.AddComponent<SkillManager>();
                string skill3Default = GetArrayValue(skillManager.skillKeys, 2);
                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(SkillManager),
                    field: "skillKeys[2]",
                    current: skill3Default,
                    expected: KeyCode.C.ToString());

                UI_SkillBar skillBar = temp.AddComponent<UI_SkillBar>();
                string skillBarSlot3 = GetArrayValue(skillBar.keyBindings, 2);
                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "ScriptDefault",
                    target: nameof(UI_SkillBar),
                    field: "keyBindings[2]",
                    current: skillBarSlot3,
                    expected: "C");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temp);
            }
        }

        private static void AuditInputHandlerSource(List<AuditEntry> entries, ref int mismatchCount)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), InputHandlerScriptAssetPath);
            if (!File.Exists(fullPath))
            {
                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "Source",
                    target: InputHandlerScriptAssetPath,
                    field: "FileExists",
                    current: "Missing",
                    expected: "Present");
                return;
            }

            string source = File.ReadAllText(fullPath);

            AuditContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "Skill3KeyboardBinding",
                source,
                "skillActions[2].AddBinding(\"<Keyboard>/c\");");

            AuditContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "ToggleTalentKeyboardBinding",
                source,
                "toggleTalentAction.AddBinding(\"<Keyboard>/u\");");

            AuditContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "ToggleEconomyKeyboardBinding",
                source,
                "toggleEconomyAction.AddBinding(\"<Keyboard>/y\");");

            AuditContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "ToggleHintsKeyboardBinding",
                source,
                "toggleHintsAction.AddBinding(\"<Keyboard>/h\");");

            AuditContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "DebugComboStatusKeyboardBinding",
                source,
                "debugComboStatusAction.AddBinding(\"<Keyboard>/tab\");");

            AuditContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "DebugComboResetKeyboardBinding",
                source,
                "debugComboResetHintAction.AddBinding(\"<Keyboard>/f7\");");

            AuditContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "DebugSpawnerWaveKeyboardBinding",
                source,
                "debugSpawnerWaveAction.AddBinding(\"<Keyboard>/g\");");

            AuditContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "DebugSpawnerStressKeyboardBinding",
                source,
                "debugSpawnerStressAction.AddBinding(\"<Keyboard>/f8\");");

            AuditContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "DebugSpawnerClearKeyboardBinding",
                source,
                "debugSpawnerClearAction.AddBinding(\"<Keyboard>/delete\");");

            AuditContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "WhitelistMenuRetrySkill4",
                source,
                "BuildConflictWhitelistKey(\"<keyboard>/r\", \"Gameplay.MenuRetry\", \"Gameplay.Skill4\"),");

            AuditContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "WhitelistEscapeMenu",
                source,
                "BuildConflictWhitelistKey(\"<keyboard>/escape\", \"Gameplay.MenuCancel\", \"Gameplay.QuitMenu\"),");

            AuditNotContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "LegacySkill3KeyboardBinding",
                source,
                "skillActions[2].AddBinding(\"<Keyboard>/e\");");

            AuditNotContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "LegacyToggleTalentKeyboardBinding",
                source,
                "toggleTalentAction.AddBinding(\"<Keyboard>/t\");");

            AuditNotContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "LegacyDebugComboResetKeyboardBinding",
                source,
                "debugComboResetHintAction.AddBinding(\"<Keyboard>/r\");");

            AuditNotContains(
                entries,
                ref mismatchCount,
                target: InputHandlerScriptAssetPath,
                field: "LegacyDebugSpawnerStressKeyboardBinding",
                source,
                "debugSpawnerStressAction.AddBinding(\"<Keyboard>/h\");");
        }

        private static void AuditWhitelistConsistency(List<AuditEntry> entries, ref int mismatchCount)
        {
            Type type = typeof(PlayerInputHandler);
            FieldInfo field = type.GetField("keyboardBindingConflictWhitelist", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo keyBuilder = type.GetMethod("BuildConflictWhitelistKey", BindingFlags.NonPublic | BindingFlags.Static);

            if (field == null || keyBuilder == null)
            {
                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "Whitelist",
                    target: nameof(PlayerInputHandler),
                    field: "Reflection",
                    current: "Missing",
                    expected: "Available");
                return;
            }

            object raw = field.GetValue(null);
            HashSet<string> whitelist = raw as HashSet<string>;
            if (whitelist == null)
            {
                AuditCheck(
                    entries,
                    ref mismatchCount,
                    scope: "Whitelist",
                    target: nameof(PlayerInputHandler),
                    field: "keyboardBindingConflictWhitelist",
                    current: raw == null ? "null" : raw.GetType().Name,
                    expected: nameof(HashSet<string>));
                return;
            }

            string expectedRetry = (string)keyBuilder.Invoke(
                null,
                new object[] { WhitelistPathMenuRetry, WhitelistOwnerMenuRetry, WhitelistOwnerSkill4 });
            string expectedEscape = (string)keyBuilder.Invoke(
                null,
                new object[] { WhitelistPathEscape, WhitelistOwnerMenuCancel, WhitelistOwnerQuitMenu });

            AuditCheck(
                entries,
                ref mismatchCount,
                scope: "Whitelist",
                target: nameof(PlayerInputHandler),
                field: "Contains(MenuRetry,Skill4)",
                current: whitelist.Contains(expectedRetry).ToString(),
                expected: true.ToString());

            AuditCheck(
                entries,
                ref mismatchCount,
                scope: "Whitelist",
                target: nameof(PlayerInputHandler),
                field: "Contains(MenuCancel,QuitMenu)",
                current: whitelist.Contains(expectedEscape).ToString(),
                expected: true.ToString());

            AuditCheck(
                entries,
                ref mismatchCount,
                scope: "Whitelist",
                target: nameof(PlayerInputHandler),
                field: "WhitelistCount",
                current: whitelist.Count.ToString(),
                expected: "2");
        }

        private static bool IsSupportedScenePath(string scenePath)
        {
            return !string.IsNullOrEmpty(scenePath)
                && scenePath.StartsWith("Assets/Scenes/", StringComparison.OrdinalIgnoreCase);
        }

        private static void AuditScene(
            string scenePath,
            bool applyFixes,
            ref bool sceneDirty,
            List<AuditEntry> entries,
            ref int mismatchCount,
            ref int fixedCount)
        {
            QuickEnemySpawner[] spawners = UnityEngine.Object.FindObjectsOfType<QuickEnemySpawner>(true);
            for (int i = 0; i < spawners.Length; i++)
            {
                fixedCount += AuditSceneField(
                    scenePath,
                    spawners[i],
                    nameof(QuickEnemySpawner.spawnKey),
                    spawners[i].spawnKey,
                    KeyCode.G,
                    value => spawners[i].spawnKey = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);

                fixedCount += AuditSceneField(
                    scenePath,
                    spawners[i],
                    nameof(QuickEnemySpawner.stressSpawnKey),
                    spawners[i].stressSpawnKey,
                    KeyCode.F8,
                    value => spawners[i].stressSpawnKey = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);

                fixedCount += AuditSceneField(
                    scenePath,
                    spawners[i],
                    nameof(QuickEnemySpawner.clearKey),
                    spawners[i].clearKey,
                    KeyCode.Delete,
                    value => spawners[i].clearKey = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);
            }

            UI_TalentEquipmentOverlay[] overlays = UnityEngine.Object.FindObjectsOfType<UI_TalentEquipmentOverlay>(true);
            for (int i = 0; i < overlays.Length; i++)
            {
                fixedCount += AuditSceneField(
                    scenePath,
                    overlays[i],
                    nameof(UI_TalentEquipmentOverlay.toggleKey),
                    overlays[i].toggleKey,
                    KeyCode.U,
                    value => overlays[i].toggleKey = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);
            }

            UI_HudHints[] hintPanels = UnityEngine.Object.FindObjectsOfType<UI_HudHints>(true);
            for (int i = 0; i < hintPanels.Length; i++)
            {
                fixedCount += AuditSceneField(
                    scenePath,
                    hintPanels[i],
                    nameof(UI_HudHints.toggleKey),
                    hintPanels[i].toggleKey,
                    KeyCode.H,
                    value => hintPanels[i].toggleKey = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);

                fixedCount += AuditSceneField(
                    scenePath,
                    hintPanels[i],
                    nameof(UI_HudHints.economyKey),
                    hintPanels[i].economyKey,
                    KeyCode.Y,
                    value => hintPanels[i].economyKey = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);

                fixedCount += AuditSceneField(
                    scenePath,
                    hintPanels[i],
                    nameof(UI_HudHints.talentKey),
                    hintPanels[i].talentKey,
                    KeyCode.U,
                    value => hintPanels[i].talentKey = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);
            }

            ComboDebugger[] comboDebuggers = UnityEngine.Object.FindObjectsOfType<ComboDebugger>(true);
            for (int i = 0; i < comboDebuggers.Length; i++)
            {
                fixedCount += AuditSceneField(
                    scenePath,
                    comboDebuggers[i],
                    nameof(ComboDebugger.statusKey),
                    comboDebuggers[i].statusKey,
                    KeyCode.Tab,
                    value => comboDebuggers[i].statusKey = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);

                fixedCount += AuditSceneField(
                    scenePath,
                    comboDebuggers[i],
                    nameof(ComboDebugger.resetHintKey),
                    comboDebuggers[i].resetHintKey,
                    KeyCode.F7,
                    value => comboDebuggers[i].resetHintKey = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);
            }

            UI_EconomyOverlay[] economyOverlays = UnityEngine.Object.FindObjectsOfType<UI_EconomyOverlay>(true);
            for (int i = 0; i < economyOverlays.Length; i++)
            {
                fixedCount += AuditSceneField(
                    scenePath,
                    economyOverlays[i],
                    nameof(UI_EconomyOverlay.toggleKey),
                    economyOverlays[i].toggleKey,
                    KeyCode.Y,
                    value => economyOverlays[i].toggleKey = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);
            }

            PlayerInputHandler[] inputHandlers = UnityEngine.Object.FindObjectsOfType<PlayerInputHandler>(true);
            for (int i = 0; i < inputHandlers.Length; i++)
            {
                fixedCount += AuditSceneField(
                    scenePath,
                    inputHandlers[i],
                    nameof(PlayerInputHandler.skill3Key),
                    inputHandlers[i].skill3Key,
                    KeyCode.C,
                    value => inputHandlers[i].skill3Key = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);
            }

            SkillManager[] skillManagers = UnityEngine.Object.FindObjectsOfType<SkillManager>(true);
            for (int i = 0; i < skillManagers.Length; i++)
            {
                fixedCount += AuditSceneKeyArrayField(
                    scenePath,
                    skillManagers[i],
                    "skillKeys[2]",
                    skillManagers[i].skillKeys,
                    2,
                    KeyCode.C,
                    value => skillManagers[i].skillKeys = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);
            }

            UI_SkillBar[] skillBars = UnityEngine.Object.FindObjectsOfType<UI_SkillBar>(true);
            for (int i = 0; i < skillBars.Length; i++)
            {
                fixedCount += AuditSceneStringArrayField(
                    scenePath,
                    skillBars[i],
                    "keyBindings[2]",
                    skillBars[i].keyBindings,
                    2,
                    "C",
                    value => skillBars[i].keyBindings = value,
                    applyFixes,
                    ref sceneDirty,
                    entries,
                    ref mismatchCount);
            }
        }

        private static int AuditSceneField(
            string scenePath,
            MonoBehaviour component,
            string fieldName,
            KeyCode currentValue,
            KeyCode expectedValue,
            Action<KeyCode> applyValue,
            bool applyFixes,
            ref bool sceneDirty,
            List<AuditEntry> entries,
            ref int mismatchCount)
        {
            if (component == null)
            {
                return 0;
            }

            bool isMatch = currentValue == expectedValue;
            string status = "OK";
            int fixedCount = 0;

            if (!isMatch)
            {
                mismatchCount++;
                status = "Mismatch";

                if (applyFixes)
                {
                    applyValue?.Invoke(expectedValue);
                    EditorUtility.SetDirty(component);
                    sceneDirty = true;
                    status = "Fixed";
                    fixedCount = 1;
                }
            }

            AddEntry(
                entries,
                scope: "Scene",
                target: $"{scenePath}:{GetHierarchyPath(component.transform)}",
                field: $"{component.GetType().Name}.{fieldName}",
                current: currentValue.ToString(),
                expected: expectedValue.ToString(),
                status: status,
                note: string.Empty);

            return fixedCount;
        }

        private static int AuditSceneKeyArrayField(
            string scenePath,
            MonoBehaviour component,
            string fieldName,
            KeyCode[] currentArray,
            int index,
            KeyCode expectedValue,
            Action<KeyCode[]> applyArray,
            bool applyFixes,
            ref bool sceneDirty,
            List<AuditEntry> entries,
            ref int mismatchCount)
        {
            if (component == null)
            {
                return 0;
            }

            bool hasValue = currentArray != null && index >= 0 && index < currentArray.Length;
            KeyCode currentValue = hasValue ? currentArray[index] : KeyCode.None;
            string currentLabel = hasValue ? currentValue.ToString() : "<missing>";
            bool isMatch = hasValue && currentValue == expectedValue;
            string status = "OK";
            int fixedCount = 0;

            if (!isMatch)
            {
                mismatchCount++;
                status = "Mismatch";

                if (applyFixes)
                {
                    KeyCode[] next = EnsureKeyCodeArrayLength(currentArray, index + 1);
                    next[index] = expectedValue;
                    applyArray?.Invoke(next);
                    EditorUtility.SetDirty(component);
                    sceneDirty = true;
                    status = "Fixed";
                    fixedCount = 1;
                }
            }

            AddEntry(
                entries,
                scope: "Scene",
                target: $"{scenePath}:{GetHierarchyPath(component.transform)}",
                field: $"{component.GetType().Name}.{fieldName}",
                current: currentLabel,
                expected: expectedValue.ToString(),
                status: status,
                note: string.Empty);

            return fixedCount;
        }

        private static int AuditSceneStringArrayField(
            string scenePath,
            MonoBehaviour component,
            string fieldName,
            string[] currentArray,
            int index,
            string expectedValue,
            Action<string[]> applyArray,
            bool applyFixes,
            ref bool sceneDirty,
            List<AuditEntry> entries,
            ref int mismatchCount)
        {
            if (component == null)
            {
                return 0;
            }

            bool hasValue = currentArray != null && index >= 0 && index < currentArray.Length;
            string currentValue = hasValue ? currentArray[index] : "<missing>";
            bool isMatch = hasValue && string.Equals(currentArray[index], expectedValue, StringComparison.OrdinalIgnoreCase);
            string status = "OK";
            int fixedCount = 0;

            if (!isMatch)
            {
                mismatchCount++;
                status = "Mismatch";

                if (applyFixes)
                {
                    string[] next = EnsureStringArrayLength(currentArray, index + 1);
                    next[index] = expectedValue;
                    applyArray?.Invoke(next);
                    EditorUtility.SetDirty(component);
                    sceneDirty = true;
                    status = "Fixed";
                    fixedCount = 1;
                }
            }

            AddEntry(
                entries,
                scope: "Scene",
                target: $"{scenePath}:{GetHierarchyPath(component.transform)}",
                field: $"{component.GetType().Name}.{fieldName}",
                current: string.IsNullOrEmpty(currentValue) ? "<empty>" : currentValue,
                expected: expectedValue,
                status: status,
                note: string.Empty);

            return fixedCount;
        }

        private static KeyCode[] EnsureKeyCodeArrayLength(KeyCode[] source, int requiredLength)
        {
            int targetLength = Math.Max(requiredLength, 6);
            if (source != null && source.Length >= targetLength)
            {
                return source;
            }

            KeyCode[] next = new KeyCode[targetLength];
            if (source != null && source.Length > 0)
            {
                Array.Copy(source, next, source.Length);
            }

            return next;
        }

        private static string[] EnsureStringArrayLength(string[] source, int requiredLength)
        {
            int targetLength = Math.Max(requiredLength, 6);
            if (source != null && source.Length >= targetLength)
            {
                return source;
            }

            string[] next = new string[targetLength];
            if (source != null && source.Length > 0)
            {
                Array.Copy(source, next, source.Length);
            }

            return next;
        }

        private static void AuditContains(
            List<AuditEntry> entries,
            ref int mismatchCount,
            string target,
            string field,
            string source,
            string expectedSnippet)
        {
            bool contains = source.Contains(expectedSnippet, StringComparison.Ordinal);
            AuditCheck(
                entries,
                ref mismatchCount,
                scope: "Source",
                target: target,
                field: field,
                current: contains.ToString(),
                expected: true.ToString(),
                note: expectedSnippet);
        }

        private static void AuditNotContains(
            List<AuditEntry> entries,
            ref int mismatchCount,
            string target,
            string field,
            string source,
            string forbiddenSnippet)
        {
            bool contains = source.Contains(forbiddenSnippet, StringComparison.Ordinal);
            AuditCheck(
                entries,
                ref mismatchCount,
                scope: "Source",
                target: target,
                field: field,
                current: contains.ToString(),
                expected: false.ToString(),
                note: forbiddenSnippet);
        }

        private static void AuditCheck(
            List<AuditEntry> entries,
            ref int mismatchCount,
            string scope,
            string target,
            string field,
            string current,
            string expected,
            string note = "")
        {
            bool isMatch = string.Equals(current, expected, StringComparison.OrdinalIgnoreCase);
            if (!isMatch)
            {
                mismatchCount++;
            }

            AddEntry(
                entries,
                scope,
                target,
                field,
                current,
                expected,
                isMatch ? "OK" : "Mismatch",
                note);
        }

        private static string GetArrayValue<T>(IReadOnlyList<T> array, int index)
        {
            if (array == null || index < 0 || index >= array.Count)
            {
                return "<null>";
            }

            T value = array[index];
            object boxed = value;
            return boxed == null ? "<null>" : boxed.ToString();
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            Stack<string> names = new Stack<string>();
            Transform current = target;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names.ToArray());
        }

        private static void WriteReport(List<AuditEntry> entries, string reportAssetPath)
        {
            string reportFullPath = Path.Combine(Directory.GetCurrentDirectory(), reportAssetPath);
            string directory = Path.GetDirectoryName(reportFullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("scope,target,field,current,expected,status,note");
            for (int i = 0; i < entries.Count; i++)
            {
                AuditEntry entry = entries[i];
                builder.AppendLine(string.Join(",",
                    EscapeCsv(entry.scope),
                    EscapeCsv(entry.target),
                    EscapeCsv(entry.field),
                    EscapeCsv(entry.currentValue),
                    EscapeCsv(entry.expectedValue),
                    EscapeCsv(entry.status),
                    EscapeCsv(entry.note)));
            }

            File.WriteAllText(reportFullPath, builder.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsv(string text)
        {
            string value = text ?? string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }

        private static void AddEntry(
            List<AuditEntry> entries,
            string scope,
            string target,
            string field,
            string current,
            string expected,
            string status,
            string note)
        {
            entries.Add(new AuditEntry
            {
                scope = scope,
                target = target,
                field = field,
                currentValue = current,
                expectedValue = expected,
                status = status,
                note = note
            });
        }

        private static int CountEntriesByStatus(List<AuditEntry> entries, string status)
        {
            if (entries == null || entries.Count == 0 || string.IsNullOrEmpty(status))
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].status, status, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count;
        }

        private struct AuditEntry
        {
            public string scope;
            public string target;
            public string field;
            public string currentValue;
            public string expectedValue;
            public string status;
            public string note;
        }
    }
}
