using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class BossEncounterRound3TuningTool
    {
        private const string ValidateMenuPath = "Tools/Boss/P1/Validate Round3 Encounter Tuning (CSV)";
        private const string ValidateGateMenuPath = "Tools/Boss/P1/Validate Round3 Encounter Tuning (CI Gate)";
        private const string FixMenuPath = "Tools/Boss/P1/Apply Round3 Encounter Tuning";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/boss_encounter_round3_tuning_report.csv";
        private const string LogPrefix = "[BossEncounterRound3]";
        private const float Epsilon = 0.0001f;

        private struct TuningTarget
        {
            public float attackInterval;
            public float decisionInterval;
            public int queuedAttackLimit;
            public float immediateRepeatPenalty;
            public bool enablePostBreakPunishWindow;
            public float postBreakPunishDuration;
            public float postBreakAttackIntervalMultiplier;
            public float postBreakDecisionIntervalMultiplier;
            public float postBreakChaseSpeedMultiplier;
            public bool enablePhaseComboChain;
            public float phase2ComboChance;
            public float phase3ComboChance;
            public float comboStartDelay;
            public float comboRepeatPenalty;
            public bool enableInterruptRecoveryGate;
            public float interruptRecoveryDuration;
            public float interruptedAttackCooldownScale;
            public bool enableTimePressure;
            public float timePressureDelay;
            public float timePressureRampDuration;
            public float maxTimePressureDamageMultiplier;
            public float maxTimePressureSpeedMultiplier;
        }

        private struct ValidationRow
        {
            public string levelId;
            public string assetPath;
            public int levelIndex;
            public string prototype;
            public string status;
            public int fixedCount;
            public int gapCount;
            public string note;
            public float expectedAttackInterval;
            public float actualAttackInterval;
            public float expectedDecisionInterval;
            public float actualDecisionInterval;
            public int expectedQueuedAttackLimit;
            public int actualQueuedAttackLimit;
            public float expectedImmediateRepeatPenalty;
            public float actualImmediateRepeatPenalty;
            public float expectedPostBreakDuration;
            public float actualPostBreakDuration;
            public float expectedPostBreakAttackMul;
            public float actualPostBreakAttackMul;
            public float expectedPostBreakDecisionMul;
            public float actualPostBreakDecisionMul;
            public float expectedPostBreakChaseMul;
            public float actualPostBreakChaseMul;
            public bool expectedEnablePhaseComboChain;
            public bool actualEnablePhaseComboChain;
            public float expectedPhase2ComboChance;
            public float actualPhase2ComboChance;
            public float expectedPhase3ComboChance;
            public float actualPhase3ComboChance;
            public float expectedComboStartDelay;
            public float actualComboStartDelay;
            public float expectedComboRepeatPenalty;
            public float actualComboRepeatPenalty;
            public bool expectedEnableInterruptRecoveryGate;
            public bool actualEnableInterruptRecoveryGate;
            public float expectedInterruptRecoveryDuration;
            public float actualInterruptRecoveryDuration;
            public float expectedInterruptedAttackCooldownScale;
            public float actualInterruptedAttackCooldownScale;
            public float expectedPressureDelay;
            public float actualPressureDelay;
            public float expectedPressureRamp;
            public float actualPressureRamp;
            public float expectedPressureDamageMul;
            public float actualPressureDamageMul;
            public float expectedPressureSpeedMul;
            public float actualPressureSpeedMul;
        }

        private sealed class Entry
        {
            public LevelData levelData;
            public string assetPath;
            public int levelIndex;
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(applyFix: false, failOnGap: false, interactive: true);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(applyFix: false, failOnGap: true, interactive: false);
        }

        [MenuItem(FixMenuPath)]
        public static void Fix()
        {
            Run(applyFix: true, failOnGap: false, interactive: true);
        }

        public static void ApplyForBatch()
        {
            Run(applyFix: true, failOnGap: true, interactive: false);
        }

        public static void ValidateForBatch()
        {
            Run(applyFix: false, failOnGap: true, interactive: false);
        }

        private static void Run(bool applyFix, bool failOnGap, bool interactive)
        {
            List<Entry> entries = CollectEntries();
            if (entries.Count == 0)
            {
                string none = $"{LogPrefix} no eligible LevelData assets for boss round3 tuning.";
                Debug.LogWarning(none);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Boss Round3 Tuning", none, "OK");
                }

                return;
            }

            var rows = new List<ValidationRow>(entries.Count);
            int fixedCount = 0;
            int gapCount = 0;
            int errorCount = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                ValidationRow row = ProcessEntry(entries[i], applyFix);
                rows.Add(row);

                fixedCount += Mathf.Max(0, row.fixedCount);
                gapCount += Mathf.Max(0, row.gapCount);
                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorCount++;
                }
            }

            if (applyFix)
            {
                AssetDatabase.SaveAssets();
            }

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();

            string summary =
                $"mode={(applyFix ? "fix" : "validate")} targets={rows.Count} fixed={fixedCount} " +
                $"gap={gapCount} error={errorCount} report={reportPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Boss Round3 Tuning", summary, "OK");
            }

            if (failOnGap && (gapCount > 0 || errorCount > 0))
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. gap={gapCount} error={errorCount} report={reportPath}");
            }
        }

        private static ValidationRow ProcessEntry(Entry entry, bool applyFix)
        {
            var row = new ValidationRow
            {
                levelId = entry.levelData != null ? entry.levelData.levelId : string.Empty,
                assetPath = entry.assetPath ?? string.Empty,
                levelIndex = entry.levelIndex,
                prototype = entry.levelData != null ? entry.levelData.bossPrototype.ToString() : string.Empty,
                status = "Error",
                fixedCount = 0,
                gapCount = 0,
                note = string.Empty
            };

            if (entry.levelData == null)
            {
                row.note = "LevelData is null.";
                row.gapCount = 1;
                return row;
            }

            TuningTarget target = BuildTarget(entry.levelIndex, entry.levelData.bossPrototype);
            var gaps = new List<string>();
            int localFixed = 0;
            bool dirty = false;

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossAttackInterval,
                target.attackInterval,
                applyFix,
                value => entry.levelData.bossAttackInterval = value,
                "bossAttackInterval",
                gaps,
                ref dirty);

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossDecisionInterval,
                target.decisionInterval,
                applyFix,
                value => entry.levelData.bossDecisionInterval = value,
                "bossDecisionInterval",
                gaps,
                ref dirty);

            if (entry.levelData.bossQueuedAttackLimit != target.queuedAttackLimit)
            {
                if (applyFix)
                {
                    entry.levelData.bossQueuedAttackLimit = target.queuedAttackLimit;
                    localFixed++;
                    dirty = true;
                }
                else
                {
                    gaps.Add("bossQueuedAttackLimit");
                }
            }

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossImmediateRepeatPenalty,
                target.immediateRepeatPenalty,
                applyFix,
                value => entry.levelData.bossImmediateRepeatPenalty = value,
                "bossImmediateRepeatPenalty",
                gaps,
                ref dirty);

            if (entry.levelData.bossEnablePostBreakPunishWindow != target.enablePostBreakPunishWindow)
            {
                if (applyFix)
                {
                    entry.levelData.bossEnablePostBreakPunishWindow = target.enablePostBreakPunishWindow;
                    localFixed++;
                    dirty = true;
                }
                else
                {
                    gaps.Add("bossEnablePostBreakPunishWindow");
                }
            }

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossPostBreakPunishDuration,
                target.postBreakPunishDuration,
                applyFix,
                value => entry.levelData.bossPostBreakPunishDuration = value,
                "bossPostBreakPunishDuration",
                gaps,
                ref dirty);

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossPostBreakAttackIntervalMultiplier,
                target.postBreakAttackIntervalMultiplier,
                applyFix,
                value => entry.levelData.bossPostBreakAttackIntervalMultiplier = value,
                "bossPostBreakAttackIntervalMultiplier",
                gaps,
                ref dirty);

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossPostBreakDecisionIntervalMultiplier,
                target.postBreakDecisionIntervalMultiplier,
                applyFix,
                value => entry.levelData.bossPostBreakDecisionIntervalMultiplier = value,
                "bossPostBreakDecisionIntervalMultiplier",
                gaps,
                ref dirty);

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossPostBreakChaseSpeedMultiplier,
                target.postBreakChaseSpeedMultiplier,
                applyFix,
                value => entry.levelData.bossPostBreakChaseSpeedMultiplier = value,
                "bossPostBreakChaseSpeedMultiplier",
                gaps,
                ref dirty);

            if (entry.levelData.bossEnablePhaseComboChain != target.enablePhaseComboChain)
            {
                if (applyFix)
                {
                    entry.levelData.bossEnablePhaseComboChain = target.enablePhaseComboChain;
                    localFixed++;
                    dirty = true;
                }
                else
                {
                    gaps.Add("bossEnablePhaseComboChain");
                }
            }

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossPhase2ComboChance,
                target.phase2ComboChance,
                applyFix,
                value => entry.levelData.bossPhase2ComboChance = value,
                "bossPhase2ComboChance",
                gaps,
                ref dirty);

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossPhase3ComboChance,
                target.phase3ComboChance,
                applyFix,
                value => entry.levelData.bossPhase3ComboChance = value,
                "bossPhase3ComboChance",
                gaps,
                ref dirty);

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossComboStartDelay,
                target.comboStartDelay,
                applyFix,
                value => entry.levelData.bossComboStartDelay = value,
                "bossComboStartDelay",
                gaps,
                ref dirty);

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossComboRepeatPenalty,
                target.comboRepeatPenalty,
                applyFix,
                value => entry.levelData.bossComboRepeatPenalty = value,
                "bossComboRepeatPenalty",
                gaps,
                ref dirty);

            if (entry.levelData.bossEnableInterruptRecoveryGate != target.enableInterruptRecoveryGate)
            {
                if (applyFix)
                {
                    entry.levelData.bossEnableInterruptRecoveryGate = target.enableInterruptRecoveryGate;
                    localFixed++;
                    dirty = true;
                }
                else
                {
                    gaps.Add("bossEnableInterruptRecoveryGate");
                }
            }

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossInterruptRecoveryDuration,
                target.interruptRecoveryDuration,
                applyFix,
                value => entry.levelData.bossInterruptRecoveryDuration = value,
                "bossInterruptRecoveryDuration",
                gaps,
                ref dirty);

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossInterruptedAttackCooldownScale,
                target.interruptedAttackCooldownScale,
                applyFix,
                value => entry.levelData.bossInterruptedAttackCooldownScale = value,
                "bossInterruptedAttackCooldownScale",
                gaps,
                ref dirty);

            if (entry.levelData.bossEnableTimePressure != target.enableTimePressure)
            {
                if (applyFix)
                {
                    entry.levelData.bossEnableTimePressure = target.enableTimePressure;
                    localFixed++;
                    dirty = true;
                }
                else
                {
                    gaps.Add("bossEnableTimePressure");
                }
            }

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossTimePressureDelay,
                target.timePressureDelay,
                applyFix,
                value => entry.levelData.bossTimePressureDelay = value,
                "bossTimePressureDelay",
                gaps,
                ref dirty);

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossTimePressureRampDuration,
                target.timePressureRampDuration,
                applyFix,
                value => entry.levelData.bossTimePressureRampDuration = value,
                "bossTimePressureRampDuration",
                gaps,
                ref dirty);

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossMaxTimePressureDamageMultiplier,
                target.maxTimePressureDamageMultiplier,
                applyFix,
                value => entry.levelData.bossMaxTimePressureDamageMultiplier = value,
                "bossMaxTimePressureDamageMultiplier",
                gaps,
                ref dirty);

            localFixed += ApplyOrValidateFloat(
                entry.levelData.bossMaxTimePressureSpeedMultiplier,
                target.maxTimePressureSpeedMultiplier,
                applyFix,
                value => entry.levelData.bossMaxTimePressureSpeedMultiplier = value,
                "bossMaxTimePressureSpeedMultiplier",
                gaps,
                ref dirty);

            if (dirty)
            {
                EditorUtility.SetDirty(entry.levelData);
            }

            row.expectedAttackInterval = target.attackInterval;
            row.actualAttackInterval = entry.levelData.bossAttackInterval;
            row.expectedDecisionInterval = target.decisionInterval;
            row.actualDecisionInterval = entry.levelData.bossDecisionInterval;
            row.expectedQueuedAttackLimit = target.queuedAttackLimit;
            row.actualQueuedAttackLimit = entry.levelData.bossQueuedAttackLimit;
            row.expectedImmediateRepeatPenalty = target.immediateRepeatPenalty;
            row.actualImmediateRepeatPenalty = entry.levelData.bossImmediateRepeatPenalty;
            row.expectedPostBreakDuration = target.postBreakPunishDuration;
            row.actualPostBreakDuration = entry.levelData.bossPostBreakPunishDuration;
            row.expectedPostBreakAttackMul = target.postBreakAttackIntervalMultiplier;
            row.actualPostBreakAttackMul = entry.levelData.bossPostBreakAttackIntervalMultiplier;
            row.expectedPostBreakDecisionMul = target.postBreakDecisionIntervalMultiplier;
            row.actualPostBreakDecisionMul = entry.levelData.bossPostBreakDecisionIntervalMultiplier;
            row.expectedPostBreakChaseMul = target.postBreakChaseSpeedMultiplier;
            row.actualPostBreakChaseMul = entry.levelData.bossPostBreakChaseSpeedMultiplier;
            row.expectedEnablePhaseComboChain = target.enablePhaseComboChain;
            row.actualEnablePhaseComboChain = entry.levelData.bossEnablePhaseComboChain;
            row.expectedPhase2ComboChance = target.phase2ComboChance;
            row.actualPhase2ComboChance = entry.levelData.bossPhase2ComboChance;
            row.expectedPhase3ComboChance = target.phase3ComboChance;
            row.actualPhase3ComboChance = entry.levelData.bossPhase3ComboChance;
            row.expectedComboStartDelay = target.comboStartDelay;
            row.actualComboStartDelay = entry.levelData.bossComboStartDelay;
            row.expectedComboRepeatPenalty = target.comboRepeatPenalty;
            row.actualComboRepeatPenalty = entry.levelData.bossComboRepeatPenalty;
            row.expectedEnableInterruptRecoveryGate = target.enableInterruptRecoveryGate;
            row.actualEnableInterruptRecoveryGate = entry.levelData.bossEnableInterruptRecoveryGate;
            row.expectedInterruptRecoveryDuration = target.interruptRecoveryDuration;
            row.actualInterruptRecoveryDuration = entry.levelData.bossInterruptRecoveryDuration;
            row.expectedInterruptedAttackCooldownScale = target.interruptedAttackCooldownScale;
            row.actualInterruptedAttackCooldownScale = entry.levelData.bossInterruptedAttackCooldownScale;
            row.expectedPressureDelay = target.timePressureDelay;
            row.actualPressureDelay = entry.levelData.bossTimePressureDelay;
            row.expectedPressureRamp = target.timePressureRampDuration;
            row.actualPressureRamp = entry.levelData.bossTimePressureRampDuration;
            row.expectedPressureDamageMul = target.maxTimePressureDamageMultiplier;
            row.actualPressureDamageMul = entry.levelData.bossMaxTimePressureDamageMultiplier;
            row.expectedPressureSpeedMul = target.maxTimePressureSpeedMultiplier;
            row.actualPressureSpeedMul = entry.levelData.bossMaxTimePressureSpeedMultiplier;

            row.fixedCount = localFixed;
            row.gapCount = gaps.Count;
            row.note = gaps.Count > 0 ? string.Join(";", gaps) : string.Empty;
            if (gaps.Count > 0)
            {
                row.status = applyFix ? "Partial" : "Gap";
            }
            else
            {
                row.status = localFixed > 0 ? "Fixed" : "Ok";
            }

            return row;
        }

        private static int ApplyOrValidateFloat(
            float actual,
            float expected,
            bool applyFix,
            Action<float> setter,
            string fieldName,
            List<string> gaps,
            ref bool dirty)
        {
            if (Mathf.Abs(actual - expected) <= Epsilon)
            {
                return 0;
            }

            if (!applyFix)
            {
                gaps.Add(fieldName);
                return 0;
            }

            setter(expected);
            dirty = true;
            return 1;
        }

        private static List<Entry> CollectEntries()
        {
            var result = new List<Entry>();
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (data == null || !data.overrideBossSettings || !data.overrideBossEncounterTuning)
                {
                    continue;
                }

                int levelIndex = ParseLevelIndex(data.levelId);
                if (levelIndex <= 0)
                {
                    continue;
                }

                result.Add(new Entry
                {
                    levelData = data,
                    assetPath = path,
                    levelIndex = levelIndex
                });
            }

            result.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));
            return result;
        }

        private static int ParseLevelIndex(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return -1;
            }

            if (!levelId.StartsWith("LEVEL_", StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            string value = levelId.Substring("LEVEL_".Length);
            if (int.TryParse(value, out int index))
            {
                return index;
            }

            return -1;
        }

        private static TuningTarget BuildTarget(int levelIndex, BossPrototypeType prototype)
        {
            float t = Mathf.InverseLerp(3f, 10f, levelIndex);
            bool isEel = prototype == BossPrototypeType.Eel;
            bool isGuardian = prototype == BossPrototypeType.Guardian;

            float attackInterval = Mathf.Max(0f, Round2(Mathf.Lerp(3.35f, 2.42f, t) + (isGuardian ? 0.06f : -0.08f)));
            float decisionInterval = Mathf.Max(0.05f, Round2(Mathf.Lerp(0.80f, 0.56f, t) + (isGuardian ? 0.02f : -0.02f)));
            int queuedAttackLimit = levelIndex >= 8 ? 4 : 3;
            float immediateRepeatPenalty = Mathf.Clamp01(Round2(Mathf.Lerp(0.34f, 0.24f, t) + (isGuardian ? 0.01f : -0.01f)));
            float punishDuration = Round2(Mathf.Lerp(4.8f, 6.2f, t) + (isGuardian ? 0.2f : 0f));
            float punishAttackMul = Mathf.Clamp(Round2(Mathf.Lerp(0.80f, 0.62f, t) + (isEel ? -0.02f : 0.01f)), 0.55f, 0.9f);
            float punishDecisionMul = Mathf.Clamp(Round2(Mathf.Lerp(0.86f, 0.72f, t) + (isEel ? -0.01f : 0.01f)), 0.55f, 0.95f);
            float punishChaseMul = Mathf.Clamp(Round2(Mathf.Lerp(1.10f, 1.28f, t) + (isEel ? 0.03f : 0f)), 1f, 1.4f);
            float phase2ComboChance = Mathf.Clamp01(Round2(Mathf.Lerp(0.42f, 0.58f, t) + (isEel ? 0.03f : -0.01f)));
            float phase3ComboChance = Mathf.Clamp01(Round2(Mathf.Lerp(0.58f, 0.78f, t) + (isEel ? 0.03f : 0f)));
            float comboStartDelay = Mathf.Max(0f, Round2(Mathf.Lerp(0.14f, 0.06f, t) + (isGuardian ? 0.01f : -0.01f)));
            float comboRepeatPenalty = Mathf.Clamp01(Round2(Mathf.Lerp(0.36f, 0.22f, t) + (isGuardian ? 0.02f : -0.01f)));
            float interruptRecoveryDuration = Mathf.Max(0f, Round2(Mathf.Lerp(0.26f, 0.14f, t) + (isGuardian ? 0.01f : -0.01f)));
            float interruptedAttackCooldownScale = Mathf.Clamp01(Round2(Mathf.Lerp(0.55f, 0.35f, t) + (isEel ? -0.02f : 0.02f)));
            float pressureDelay = Mathf.Max(0f, Round2(Mathf.Lerp(74f, 45f, t) + (isGuardian ? 3f : -2f)));
            float pressureRamp = Mathf.Max(1f, Round2(Mathf.Lerp(62f, 34f, t) + (isGuardian ? 2f : 0f)));
            float pressureDamageMul = Mathf.Max(1f, Round2(Mathf.Lerp(1.30f, 1.50f, t) + (isGuardian ? 0.03f : 0f)));
            float pressureSpeedMul = Mathf.Max(1f, Round2(Mathf.Lerp(1.16f, 1.28f, t) + (isEel ? 0.02f : 0f)));

            return new TuningTarget
            {
                attackInterval = attackInterval,
                decisionInterval = decisionInterval,
                queuedAttackLimit = queuedAttackLimit,
                immediateRepeatPenalty = immediateRepeatPenalty,
                enablePostBreakPunishWindow = true,
                postBreakPunishDuration = punishDuration,
                postBreakAttackIntervalMultiplier = punishAttackMul,
                postBreakDecisionIntervalMultiplier = punishDecisionMul,
                postBreakChaseSpeedMultiplier = punishChaseMul,
                enablePhaseComboChain = true,
                phase2ComboChance = phase2ComboChance,
                phase3ComboChance = phase3ComboChance,
                comboStartDelay = comboStartDelay,
                comboRepeatPenalty = comboRepeatPenalty,
                enableInterruptRecoveryGate = true,
                interruptRecoveryDuration = interruptRecoveryDuration,
                interruptedAttackCooldownScale = interruptedAttackCooldownScale,
                enableTimePressure = true,
                timePressureDelay = pressureDelay,
                timePressureRampDuration = pressureRamp,
                maxTimePressureDamageMultiplier = pressureDamageMul,
                maxTimePressureSpeedMultiplier = pressureSpeedMul
            };
        }

        private static float Round2(float value)
        {
            return (float)Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string absolutePath = Path.GetFullPath(ReportCsvPath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var builder = new StringBuilder();
            builder.AppendLine("level_id,level_index,prototype,status,fixed_count,gap_count,expected_attack_interval,actual_attack_interval,expected_decision_interval,actual_decision_interval,expected_queued_attack_limit,actual_queued_attack_limit,expected_immediate_repeat_penalty,actual_immediate_repeat_penalty,expected_post_break_duration,actual_post_break_duration,expected_post_break_attack_mul,actual_post_break_attack_mul,expected_post_break_decision_mul,actual_post_break_decision_mul,expected_post_break_chase_mul,actual_post_break_chase_mul,expected_enable_phase_combo_chain,actual_enable_phase_combo_chain,expected_phase2_combo_chance,actual_phase2_combo_chance,expected_phase3_combo_chance,actual_phase3_combo_chance,expected_combo_start_delay,actual_combo_start_delay,expected_combo_repeat_penalty,actual_combo_repeat_penalty,expected_enable_interrupt_recovery_gate,actual_enable_interrupt_recovery_gate,expected_interrupt_recovery_duration,actual_interrupt_recovery_duration,expected_interrupted_attack_cooldown_scale,actual_interrupted_attack_cooldown_scale,expected_pressure_delay,actual_pressure_delay,expected_pressure_ramp,actual_pressure_ramp,expected_pressure_damage_mul,actual_pressure_damage_mul,expected_pressure_speed_mul,actual_pressure_speed_mul,asset_path,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                builder.Append(EscapeCsv(row.levelId)).Append(',')
                    .Append(row.levelIndex).Append(',')
                    .Append(EscapeCsv(row.prototype)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.fixedCount).Append(',')
                    .Append(row.gapCount).Append(',')
                    .Append(row.expectedAttackInterval.ToString("0.##")).Append(',')
                    .Append(row.actualAttackInterval.ToString("0.##")).Append(',')
                    .Append(row.expectedDecisionInterval.ToString("0.##")).Append(',')
                    .Append(row.actualDecisionInterval.ToString("0.##")).Append(',')
                    .Append(row.expectedQueuedAttackLimit).Append(',')
                    .Append(row.actualQueuedAttackLimit).Append(',')
                    .Append(row.expectedImmediateRepeatPenalty.ToString("0.##")).Append(',')
                    .Append(row.actualImmediateRepeatPenalty.ToString("0.##")).Append(',')
                    .Append(row.expectedPostBreakDuration.ToString("0.##")).Append(',')
                    .Append(row.actualPostBreakDuration.ToString("0.##")).Append(',')
                    .Append(row.expectedPostBreakAttackMul.ToString("0.##")).Append(',')
                    .Append(row.actualPostBreakAttackMul.ToString("0.##")).Append(',')
                    .Append(row.expectedPostBreakDecisionMul.ToString("0.##")).Append(',')
                    .Append(row.actualPostBreakDecisionMul.ToString("0.##")).Append(',')
                    .Append(row.expectedPostBreakChaseMul.ToString("0.##")).Append(',')
                    .Append(row.actualPostBreakChaseMul.ToString("0.##")).Append(',')
                    .Append(row.expectedEnablePhaseComboChain).Append(',')
                    .Append(row.actualEnablePhaseComboChain).Append(',')
                    .Append(row.expectedPhase2ComboChance.ToString("0.##")).Append(',')
                    .Append(row.actualPhase2ComboChance.ToString("0.##")).Append(',')
                    .Append(row.expectedPhase3ComboChance.ToString("0.##")).Append(',')
                    .Append(row.actualPhase3ComboChance.ToString("0.##")).Append(',')
                    .Append(row.expectedComboStartDelay.ToString("0.##")).Append(',')
                    .Append(row.actualComboStartDelay.ToString("0.##")).Append(',')
                    .Append(row.expectedComboRepeatPenalty.ToString("0.##")).Append(',')
                    .Append(row.actualComboRepeatPenalty.ToString("0.##")).Append(',')
                    .Append(row.expectedEnableInterruptRecoveryGate).Append(',')
                    .Append(row.actualEnableInterruptRecoveryGate).Append(',')
                    .Append(row.expectedInterruptRecoveryDuration.ToString("0.##")).Append(',')
                    .Append(row.actualInterruptRecoveryDuration.ToString("0.##")).Append(',')
                    .Append(row.expectedInterruptedAttackCooldownScale.ToString("0.##")).Append(',')
                    .Append(row.actualInterruptedAttackCooldownScale.ToString("0.##")).Append(',')
                    .Append(row.expectedPressureDelay.ToString("0.##")).Append(',')
                    .Append(row.actualPressureDelay.ToString("0.##")).Append(',')
                    .Append(row.expectedPressureRamp.ToString("0.##")).Append(',')
                    .Append(row.actualPressureRamp.ToString("0.##")).Append(',')
                    .Append(row.expectedPressureDamageMul.ToString("0.##")).Append(',')
                    .Append(row.actualPressureDamageMul.ToString("0.##")).Append(',')
                    .Append(row.expectedPressureSpeedMul.ToString("0.##")).Append(',')
                    .Append(row.actualPressureSpeedMul.ToString("0.##")).Append(',')
                    .Append(EscapeCsv(row.assetPath)).Append(',')
                    .Append(EscapeCsv(row.note)).AppendLine();
            }

            File.WriteAllText(absolutePath, builder.ToString(), Encoding.UTF8);
            return ReportCsvPath;
        }

        private static string EscapeCsv(string value)
        {
            string text = value ?? string.Empty;
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }

            return text;
        }
    }
}
