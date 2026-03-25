using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class BossAttackCsvTuningTool
    {
        private const string ExportMenuPath = "Tools/Boss/P2/Export Attack Tuning CSV Template";
        private const string ApplyMenuPath = "Tools/Boss/P2/Apply Attack Tuning CSV";
        private const string ValidateMenuPath = "Tools/Boss/P2/Validate Attack Tuning CSV (CI Gate)";
        private const string PrefabFolder = "Assets/Prefabs/Bosses";
        private const string TemplateCsvPath = "Assets/ThirdPersonController/Reports/boss_attack_tuning_round4_template.csv";
        private const string FillCsvPath = "Assets/ThirdPersonController/Reports/boss_attack_tuning_round4_fill.csv";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/boss_attack_tuning_round4_import_report.csv";
        private const string LogPrefix = "[BossAttackCsv]";

        private static readonly string[] RequiredColumns =
        {
            "prefab_path",
            "attack_id",
            "attack_name",
            "damage",
            "cooldown",
            "selection_weight",
            "windup_time",
            "active_time",
            "recovery_time",
            "range",
            "knockback_force",
            "is_special",
            "requires_phase2",
            "requires_phase3",
            "target_player",
            "aoe",
            "aoe_radius"
        };

        private struct CsvAttackRow
        {
            public string prefabPath;
            public string attackId;
            public string attackName;
            public float damage;
            public float cooldown;
            public float selectionWeight;
            public float windupTime;
            public float activeTime;
            public float recoveryTime;
            public float range;
            public float knockbackForce;
            public bool isSpecial;
            public bool requiresPhase2;
            public bool requiresPhase3;
            public bool targetPlayer;
            public bool aoe;
            public float aoeRadius;
            public string note;
        }

        private struct ReportRow
        {
            public string layer;
            public string source;
            public string attackId;
            public string status;
            public int fixedCount;
            public int gapCount;
            public string note;
        }

        private sealed class ParseResult
        {
            public readonly Dictionary<string, CsvAttackRow> rowByKey = new Dictionary<string, CsvAttackRow>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> consumedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public int errorCount;

            public bool HasErrors => errorCount > 0;
        }

        [MenuItem(ExportMenuPath)]
        public static void ExportTemplate()
        {
            List<CsvAttackRow> snapshot = CollectSnapshotRows();
            string templatePath = WriteAttackCsv(snapshot, TemplateCsvPath);
            AssetDatabase.Refresh();
            string message = $"Template CSV exported.\n\n{templatePath}\nRows: {snapshot.Count}";
            EditorUtility.DisplayDialog("Boss Attack CSV", message, "OK");
        }

        [MenuItem(ApplyMenuPath)]
        public static void Apply()
        {
            Run(applyValues: true, failOnBlocking: false, interactive: true);
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(applyValues: false, failOnBlocking: true, interactive: true);
        }

        public static void ApplyForBatch()
        {
            Run(applyValues: true, failOnBlocking: true, interactive: false);
        }

        public static void ValidateForBatch()
        {
            Run(applyValues: false, failOnBlocking: true, interactive: false);
        }

        private static void Run(bool applyValues, bool failOnBlocking, bool interactive)
        {
            if (interactive && !Application.isBatchMode)
            {
                bool allow = UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                if (!allow)
                {
                    return;
                }
            }

            var reportRows = new List<ReportRow>(128);

            List<CsvAttackRow> snapshot = CollectSnapshotRows();
            string templatePath = WriteAttackCsv(snapshot, TemplateCsvPath);
            EnsureFillCsvExists(templatePath, reportRows);

            ParseResult parsed = ParseFillCsv(reportRows);
            List<CsvAttackRow> normalizedRows = new List<CsvAttackRow>(snapshot.Count);
            if (!parsed.HasErrors)
            {
                ProcessBossPrefabs(applyValues, parsed, normalizedRows, reportRows);
            }
            else
            {
                reportRows.Add(new ReportRow
                {
                    layer = "CSV",
                    source = FillCsvPath,
                    attackId = string.Empty,
                    status = "Error",
                    fixedCount = 0,
                    gapCount = 1,
                    note = "csv-parse-aborted"
                });
            }

            Summarize(reportRows, out int fixedTotal, out int gapTotal, out int errorTotal, out int mismatchTotal);
            if (applyValues && !parsed.HasErrors)
            {
                bool hasBlocking = gapTotal > 0 || errorTotal > 0 || mismatchTotal > 0;
                if (!hasBlocking)
                {
                    bool fillChanged = WriteAttackCsvIfChanged(normalizedRows, FillCsvPath);
                    reportRows.Add(new ReportRow
                    {
                        layer = "CSV",
                        source = FillCsvPath,
                        attackId = string.Empty,
                        status = fillChanged ? "Fixed" : "Ok",
                        fixedCount = fillChanged ? 1 : 0,
                        gapCount = 0,
                        note = fillChanged ? "fill-csv-normalized" : "fill-csv-already-normalized"
                    });

                    if (fillChanged)
                    {
                        AssetDatabase.SaveAssets();
                    }
                }
                else
                {
                    reportRows.Add(new ReportRow
                    {
                        layer = "CSV",
                        source = FillCsvPath,
                        attackId = string.Empty,
                        status = "Skipped",
                        fixedCount = 0,
                        gapCount = 0,
                        note = "fill-csv-not-rewritten-due-to-blocking-status"
                    });
                }
            }

            Summarize(reportRows, out fixedTotal, out gapTotal, out errorTotal, out mismatchTotal);
            string reportPath = WriteReportCsv(reportRows);
            AssetDatabase.Refresh();

            string summary =
                $"mode={(applyValues ? "apply" : "validate")} rows={reportRows.Count} fixed={fixedTotal} " +
                $"gap={gapTotal} mismatch={mismatchTotal} error={errorTotal} report={reportPath}";
            Debug.Log($"{LogPrefix} complete | {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Boss Attack CSV", summary, "OK");
            }

            if (failOnBlocking && (gapTotal > 0 || errorTotal > 0 || mismatchTotal > 0))
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. gap={gapTotal} mismatch={mismatchTotal} error={errorTotal} report={reportPath}");
            }
        }

        private static void EnsureFillCsvExists(string templatePath, List<ReportRow> reportRows)
        {
            string fillAbsolute = Path.GetFullPath(FillCsvPath);
            if (File.Exists(fillAbsolute))
            {
                return;
            }

            string directory = Path.GetDirectoryName(fillAbsolute);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string templateAbsolute = Path.GetFullPath(templatePath);
            if (!File.Exists(templateAbsolute))
            {
                reportRows.Add(new ReportRow
                {
                    layer = "CSV",
                    source = FillCsvPath,
                    attackId = string.Empty,
                    status = "Error",
                    fixedCount = 0,
                    gapCount = 1,
                    note = "template-missing-cannot-create-fill"
                });
                return;
            }

            File.Copy(templateAbsolute, fillAbsolute, overwrite: true);
            reportRows.Add(new ReportRow
            {
                layer = "CSV",
                source = FillCsvPath,
                attackId = string.Empty,
                status = "Fixed",
                fixedCount = 1,
                gapCount = 0,
                note = "fill-csv-created-from-template"
            });
        }

        private static ParseResult ParseFillCsv(List<ReportRow> reportRows)
        {
            var result = new ParseResult();
            string fillAbsolute = Path.GetFullPath(FillCsvPath);
            if (!File.Exists(fillAbsolute))
            {
                result.errorCount++;
                reportRows.Add(new ReportRow
                {
                    layer = "CSV",
                    source = FillCsvPath,
                    attackId = string.Empty,
                    status = "Error",
                    fixedCount = 0,
                    gapCount = 1,
                    note = "fill-csv-missing"
                });
                return result;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(fillAbsolute, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                result.errorCount++;
                reportRows.Add(new ReportRow
                {
                    layer = "CSV",
                    source = FillCsvPath,
                    attackId = string.Empty,
                    status = "Error",
                    fixedCount = 0,
                    gapCount = 1,
                    note = $"fill-csv-read-failed:{ex.Message}"
                });
                return result;
            }

            if (lines.Length == 0)
            {
                result.errorCount++;
                reportRows.Add(new ReportRow
                {
                    layer = "CSV",
                    source = FillCsvPath,
                    attackId = string.Empty,
                    status = "Error",
                    fixedCount = 0,
                    gapCount = 1,
                    note = "fill-csv-empty"
                });
                return result;
            }

            Dictionary<string, int> header = BuildHeaderIndex(lines[0]);
            bool headerValid = true;
            for (int i = 0; i < RequiredColumns.Length; i++)
            {
                string required = RequiredColumns[i];
                if (!header.ContainsKey(required))
                {
                    headerValid = false;
                    result.errorCount++;
                    reportRows.Add(new ReportRow
                    {
                        layer = "CSV",
                        source = FillCsvPath,
                        attackId = string.Empty,
                        status = "Error",
                        fixedCount = 0,
                        gapCount = 1,
                        note = $"missing-column:{required}"
                    });
                }
            }

            if (!headerValid)
            {
                return result;
            }

            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                string rawLine = lines[lineIndex];
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                List<string> cells = SplitCsvLine(rawLine);
                string prefabPath = NormalizeAssetPath(GetCell(cells, header, "prefab_path"));
                string attackId = (GetCell(cells, header, "attack_id") ?? string.Empty).Trim();
                string attackName = (GetCell(cells, header, "attack_name") ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(prefabPath) || string.IsNullOrWhiteSpace(attackId))
                {
                    result.errorCount++;
                    reportRows.Add(new ReportRow
                    {
                        layer = "CSV",
                        source = FillCsvPath,
                        attackId = attackId,
                        status = "Error",
                        fixedCount = 0,
                        gapCount = 1,
                        note = $"line-{lineIndex + 1}-missing-key"
                    });
                    continue;
                }

                var parseErrors = new List<string>(8);
                if (!TryParseFloat(GetCell(cells, header, "damage"), out float damage)) parseErrors.Add("damage");
                if (!TryParseFloat(GetCell(cells, header, "cooldown"), out float cooldown)) parseErrors.Add("cooldown");
                if (!TryParseFloat(GetCell(cells, header, "selection_weight"), out float selectionWeight)) parseErrors.Add("selection_weight");
                if (!TryParseFloat(GetCell(cells, header, "windup_time"), out float windupTime)) parseErrors.Add("windup_time");
                if (!TryParseFloat(GetCell(cells, header, "active_time"), out float activeTime)) parseErrors.Add("active_time");
                if (!TryParseFloat(GetCell(cells, header, "recovery_time"), out float recoveryTime)) parseErrors.Add("recovery_time");
                if (!TryParseFloat(GetCell(cells, header, "range"), out float range)) parseErrors.Add("range");
                if (!TryParseFloat(GetCell(cells, header, "knockback_force"), out float knockbackForce)) parseErrors.Add("knockback_force");
                if (!TryParseFloat(GetCell(cells, header, "aoe_radius"), out float aoeRadius)) parseErrors.Add("aoe_radius");
                if (!TryParseBool(GetCell(cells, header, "is_special"), out bool isSpecial)) parseErrors.Add("is_special");
                if (!TryParseBool(GetCell(cells, header, "requires_phase2"), out bool requiresPhase2)) parseErrors.Add("requires_phase2");
                if (!TryParseBool(GetCell(cells, header, "requires_phase3"), out bool requiresPhase3)) parseErrors.Add("requires_phase3");
                if (!TryParseBool(GetCell(cells, header, "target_player"), out bool targetPlayer)) parseErrors.Add("target_player");
                if (!TryParseBool(GetCell(cells, header, "aoe"), out bool aoe)) parseErrors.Add("aoe");

                if (parseErrors.Count > 0)
                {
                    result.errorCount++;
                    reportRows.Add(new ReportRow
                    {
                        layer = "CSV",
                        source = prefabPath,
                        attackId = attackId,
                        status = "Error",
                        fixedCount = 0,
                        gapCount = 1,
                        note = $"line-{lineIndex + 1}-parse-failed:{string.Join("|", parseErrors)}"
                    });
                    continue;
                }

                string key = BuildKey(prefabPath, attackId);
                if (result.rowByKey.ContainsKey(key))
                {
                    result.errorCount++;
                    reportRows.Add(new ReportRow
                    {
                        layer = "CSV",
                        source = prefabPath,
                        attackId = attackId,
                        status = "Error",
                        fixedCount = 0,
                        gapCount = 1,
                        note = $"line-{lineIndex + 1}-duplicate-key"
                    });
                    continue;
                }

                result.rowByKey.Add(key, new CsvAttackRow
                {
                    prefabPath = prefabPath,
                    attackId = attackId,
                    attackName = attackName,
                    damage = damage,
                    cooldown = cooldown,
                    selectionWeight = selectionWeight,
                    windupTime = windupTime,
                    activeTime = activeTime,
                    recoveryTime = recoveryTime,
                    range = range,
                    knockbackForce = knockbackForce,
                    isSpecial = isSpecial,
                    requiresPhase2 = requiresPhase2,
                    requiresPhase3 = requiresPhase3,
                    targetPlayer = targetPlayer,
                    aoe = aoe,
                    aoeRadius = aoeRadius,
                    note = string.Empty
                });
            }

            return result;
        }

        private static void ProcessBossPrefabs(
            bool applyValues,
            ParseResult parsed,
            List<CsvAttackRow> normalizedRows,
            List<ReportRow> reportRows)
        {
            List<string> prefabPaths = GetBossPrefabPaths();
            for (int prefabIndex = 0; prefabIndex < prefabPaths.Count; prefabIndex++)
            {
                string prefabPath = prefabPaths[prefabIndex];
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (root == null)
                    {
                        reportRows.Add(CreateReport("Prefab", prefabPath, string.Empty, "Error", 0, 1, "load-prefab-failed"));
                        continue;
                    }

                    BossController boss = root.GetComponent<BossController>();
                    if (boss == null)
                    {
                        reportRows.Add(CreateReport("Prefab", prefabPath, string.Empty, "Gap", 0, 1, "boss-controller-missing"));
                        continue;
                    }

                    if (boss.attacks == null || boss.attacks.Count == 0)
                    {
                        reportRows.Add(CreateReport("Prefab", prefabPath, string.Empty, "Gap", 0, 1, "attacks-empty"));
                        continue;
                    }

                    bool prefabDirty = false;
                    for (int attackIndex = 0; attackIndex < boss.attacks.Count; attackIndex++)
                    {
                        BossAttack attack = boss.attacks[attackIndex];
                        if (attack == null)
                        {
                            reportRows.Add(CreateReport("PrefabAttack", prefabPath, $"<null-{attackIndex}>", "Gap", 0, 1, "attack-null"));
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(attack.attackId))
                        {
                            reportRows.Add(CreateReport("PrefabAttack", prefabPath, $"<empty-{attackIndex}>", "Gap", 0, 1, "attack-id-empty"));
                            continue;
                        }

                        string key = BuildKey(prefabPath, attack.attackId);
                        if (!parsed.rowByKey.TryGetValue(key, out CsvAttackRow csvRow))
                        {
                            if (applyValues)
                            {
                                normalizedRows.Add(CreateCsvFromAttack(prefabPath, attack));
                                reportRows.Add(CreateReport("PrefabAttack", prefabPath, attack.attackId, "Fixed", 1, 0, "fill-row-auto-added"));
                            }
                            else
                            {
                                reportRows.Add(CreateReport("PrefabAttack", prefabPath, attack.attackId, "Gap", 0, 1, "fill-row-missing"));
                            }

                            continue;
                        }

                        parsed.consumedKeys.Add(key);
                        int changeCount = 0;
                        int mismatchCount = 0;
                        var notes = new List<string>(12);

                        SyncStringValue("attack_name", csvRow.attackName, attack.attackName, applyValues, value => attack.attackName = value, ref changeCount, ref mismatchCount, notes);
                        SyncFloatValue("damage", csvRow.damage, attack.damage, applyValues, value => attack.damage = value, ref changeCount, ref mismatchCount, notes);
                        SyncFloatValue("cooldown", csvRow.cooldown, attack.cooldown, applyValues, value => attack.cooldown = value, ref changeCount, ref mismatchCount, notes);
                        SyncFloatValue("selection_weight", Mathf.Max(0.01f, csvRow.selectionWeight), attack.selectionWeight, applyValues, value => attack.selectionWeight = Mathf.Max(0.01f, value), ref changeCount, ref mismatchCount, notes);
                        SyncFloatValue("windup_time", csvRow.windupTime, attack.windupTime, applyValues, value => attack.windupTime = value, ref changeCount, ref mismatchCount, notes);
                        SyncFloatValue("active_time", csvRow.activeTime, attack.activeTime, applyValues, value => attack.activeTime = value, ref changeCount, ref mismatchCount, notes);
                        SyncFloatValue("recovery_time", csvRow.recoveryTime, attack.recoveryTime, applyValues, value => attack.recoveryTime = value, ref changeCount, ref mismatchCount, notes);
                        SyncFloatValue("range", csvRow.range, attack.range, applyValues, value => attack.range = value, ref changeCount, ref mismatchCount, notes);
                        SyncFloatValue("knockback_force", csvRow.knockbackForce, attack.knockbackForce, applyValues, value => attack.knockbackForce = value, ref changeCount, ref mismatchCount, notes);
                        SyncBoolValue("is_special", csvRow.isSpecial, attack.isSpecial, applyValues, value => attack.isSpecial = value, ref changeCount, ref mismatchCount, notes);
                        SyncBoolValue("requires_phase2", csvRow.requiresPhase2, attack.requiresPhase2, applyValues, value => attack.requiresPhase2 = value, ref changeCount, ref mismatchCount, notes);
                        SyncBoolValue("requires_phase3", csvRow.requiresPhase3, attack.requiresPhase3, applyValues, value => attack.requiresPhase3 = value, ref changeCount, ref mismatchCount, notes);
                        SyncBoolValue("target_player", csvRow.targetPlayer, attack.targetPlayer, applyValues, value => attack.targetPlayer = value, ref changeCount, ref mismatchCount, notes);
                        SyncBoolValue("aoe", csvRow.aoe, attack.aoe, applyValues, value => attack.aoe = value, ref changeCount, ref mismatchCount, notes);
                        SyncFloatValue("aoe_radius", csvRow.aoeRadius, attack.aoeRadius, applyValues, value => attack.aoeRadius = value, ref changeCount, ref mismatchCount, notes);

                        if (applyValues && changeCount > 0)
                        {
                            prefabDirty = true;
                        }

                        normalizedRows.Add(applyValues ? CreateCsvFromAttack(prefabPath, attack) : csvRow);

                        string status = applyValues
                            ? (changeCount > 0 ? "Fixed" : "Ok")
                            : (mismatchCount > 0 ? "Mismatch" : "Ok");
                        int gapCount = applyValues ? 0 : mismatchCount;
                        reportRows.Add(CreateReport("PrefabAttack", prefabPath, attack.attackId, status, changeCount, gapCount, notes.Count > 0 ? string.Join(";", notes) : string.Empty));
                    }

                    if (applyValues && prefabDirty)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    }
                }
                catch (Exception ex)
                {
                    reportRows.Add(CreateReport("Prefab", prefabPath, string.Empty, "Error", 0, 1, $"exception:{ex.Message}"));
                }
                finally
                {
                    if (root != null)
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }

            foreach (KeyValuePair<string, CsvAttackRow> kv in parsed.rowByKey)
            {
                if (parsed.consumedKeys.Contains(kv.Key))
                {
                    continue;
                }

                CsvAttackRow orphan = kv.Value;
                if (applyValues)
                {
                    reportRows.Add(CreateReport("CSV", orphan.prefabPath, orphan.attackId, "Fixed", 1, 0, "orphan-row-pruned"));
                }
                else
                {
                    reportRows.Add(CreateReport("CSV", orphan.prefabPath, orphan.attackId, "Gap", 0, 1, "orphan-row"));
                }
            }

            normalizedRows.Sort((a, b) =>
            {
                int prefabCompare = string.Compare(a.prefabPath, b.prefabPath, StringComparison.OrdinalIgnoreCase);
                if (prefabCompare != 0)
                {
                    return prefabCompare;
                }

                return string.Compare(a.attackId, b.attackId, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static List<CsvAttackRow> CollectSnapshotRows()
        {
            var rows = new List<CsvAttackRow>(64);
            List<string> prefabPaths = GetBossPrefabPaths();
            for (int prefabIndex = 0; prefabIndex < prefabPaths.Count; prefabIndex++)
            {
                string prefabPath = prefabPaths[prefabIndex];
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (root == null)
                    {
                        continue;
                    }

                    BossController boss = root.GetComponent<BossController>();
                    if (boss == null || boss.attacks == null || boss.attacks.Count == 0)
                    {
                        continue;
                    }

                    for (int attackIndex = 0; attackIndex < boss.attacks.Count; attackIndex++)
                    {
                        BossAttack attack = boss.attacks[attackIndex];
                        if (attack == null || string.IsNullOrWhiteSpace(attack.attackId))
                        {
                            continue;
                        }

                        rows.Add(CreateCsvFromAttack(prefabPath, attack));
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

            rows.Sort((a, b) =>
            {
                int prefabCompare = string.Compare(a.prefabPath, b.prefabPath, StringComparison.OrdinalIgnoreCase);
                if (prefabCompare != 0)
                {
                    return prefabCompare;
                }

                return string.Compare(a.attackId, b.attackId, StringComparison.OrdinalIgnoreCase);
            });
            return rows;
        }

        private static CsvAttackRow CreateCsvFromAttack(string prefabPath, BossAttack attack)
        {
            return new CsvAttackRow
            {
                prefabPath = NormalizeAssetPath(prefabPath),
                attackId = attack != null ? attack.attackId ?? string.Empty : string.Empty,
                attackName = attack != null ? attack.attackName ?? string.Empty : string.Empty,
                damage = attack != null ? attack.damage : 0f,
                cooldown = attack != null ? attack.cooldown : 0f,
                selectionWeight = attack != null ? attack.selectionWeight : 1f,
                windupTime = attack != null ? attack.windupTime : 0f,
                activeTime = attack != null ? attack.activeTime : 0f,
                recoveryTime = attack != null ? attack.recoveryTime : 0f,
                range = attack != null ? attack.range : 0f,
                knockbackForce = attack != null ? attack.knockbackForce : 0f,
                isSpecial = attack != null && attack.isSpecial,
                requiresPhase2 = attack != null && attack.requiresPhase2,
                requiresPhase3 = attack != null && attack.requiresPhase3,
                targetPlayer = attack == null || attack.targetPlayer,
                aoe = attack != null && attack.aoe,
                aoeRadius = attack != null ? attack.aoeRadius : 0f,
                note = string.Empty
            };
        }

        private static void SyncStringValue(
            string field,
            string expected,
            string actual,
            bool applyValues,
            Action<string> assign,
            ref int changeCount,
            ref int mismatchCount,
            List<string> notes)
        {
            string expectedSafe = expected ?? string.Empty;
            string actualSafe = actual ?? string.Empty;
            if (string.Equals(expectedSafe, actualSafe, StringComparison.Ordinal))
            {
                return;
            }

            if (applyValues)
            {
                assign(expectedSafe);
                changeCount++;
                notes.Add($"set-{field}");
            }
            else
            {
                mismatchCount++;
                notes.Add($"mismatch-{field}");
            }
        }

        private static void SyncFloatValue(
            string field,
            float expected,
            float actual,
            bool applyValues,
            Action<float> assign,
            ref int changeCount,
            ref int mismatchCount,
            List<string> notes)
        {
            if (NearlyEqual(expected, actual))
            {
                return;
            }

            if (applyValues)
            {
                assign(expected);
                changeCount++;
                notes.Add($"set-{field}");
            }
            else
            {
                mismatchCount++;
                notes.Add($"mismatch-{field}");
            }
        }

        private static void SyncBoolValue(
            string field,
            bool expected,
            bool actual,
            bool applyValues,
            Action<bool> assign,
            ref int changeCount,
            ref int mismatchCount,
            List<string> notes)
        {
            if (expected == actual)
            {
                return;
            }

            if (applyValues)
            {
                assign(expected);
                changeCount++;
                notes.Add($"set-{field}");
            }
            else
            {
                mismatchCount++;
                notes.Add($"mismatch-{field}");
            }
        }

        private static ReportRow CreateReport(
            string layer,
            string source,
            string attackId,
            string status,
            int fixedCount,
            int gapCount,
            string note)
        {
            return new ReportRow
            {
                layer = layer ?? string.Empty,
                source = source ?? string.Empty,
                attackId = attackId ?? string.Empty,
                status = status ?? string.Empty,
                fixedCount = fixedCount,
                gapCount = gapCount,
                note = note ?? string.Empty
            };
        }

        private static List<string> GetBossPrefabPaths()
        {
            var result = new List<string>(16);
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            Array.Sort(guids, StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (!path.EndsWith("_Controller.prefab", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(NormalizeAssetPath(path));
            }

            return result;
        }

        private static string WriteAttackCsv(List<CsvAttackRow> rows, string relativePath)
        {
            string absolutePath = Path.GetFullPath(relativePath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(absolutePath, BuildAttackCsvContent(rows), Encoding.UTF8);
            return relativePath;
        }

        private static bool WriteAttackCsvIfChanged(List<CsvAttackRow> rows, string relativePath)
        {
            string absolutePath = Path.GetFullPath(relativePath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string normalizedContent = BuildAttackCsvContent(rows);
            if (File.Exists(absolutePath))
            {
                string existing = File.ReadAllText(absolutePath, Encoding.UTF8);
                if (string.Equals(existing, normalizedContent, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            File.WriteAllText(absolutePath, normalizedContent, Encoding.UTF8);
            return true;
        }

        private static string BuildAttackCsvContent(List<CsvAttackRow> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine("prefab_path,attack_id,attack_name,damage,cooldown,selection_weight,windup_time,active_time,recovery_time,range,knockback_force,is_special,requires_phase2,requires_phase3,target_player,aoe,aoe_radius,note");
            for (int i = 0; i < rows.Count; i++)
            {
                CsvAttackRow row = rows[i];
                builder.Append(EscapeCsv(row.prefabPath)).Append(',')
                    .Append(EscapeCsv(row.attackId)).Append(',')
                    .Append(EscapeCsv(row.attackName)).Append(',')
                    .Append(FormatFloat(row.damage)).Append(',')
                    .Append(FormatFloat(row.cooldown)).Append(',')
                    .Append(FormatFloat(row.selectionWeight)).Append(',')
                    .Append(FormatFloat(row.windupTime)).Append(',')
                    .Append(FormatFloat(row.activeTime)).Append(',')
                    .Append(FormatFloat(row.recoveryTime)).Append(',')
                    .Append(FormatFloat(row.range)).Append(',')
                    .Append(FormatFloat(row.knockbackForce)).Append(',')
                    .Append(row.isSpecial ? "true" : "false").Append(',')
                    .Append(row.requiresPhase2 ? "true" : "false").Append(',')
                    .Append(row.requiresPhase3 ? "true" : "false").Append(',')
                    .Append(row.targetPlayer ? "true" : "false").Append(',')
                    .Append(row.aoe ? "true" : "false").Append(',')
                    .Append(FormatFloat(row.aoeRadius)).Append(',')
                    .Append(EscapeCsv(row.note))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static string WriteReportCsv(List<ReportRow> rows)
        {
            string absolutePath = Path.GetFullPath(ReportCsvPath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var builder = new StringBuilder();
            builder.AppendLine("layer,source,attack_id,status,fixed_count,gap_count,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ReportRow row = rows[i];
                builder.Append(EscapeCsv(row.layer)).Append(',')
                    .Append(EscapeCsv(row.source)).Append(',')
                    .Append(EscapeCsv(row.attackId)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(row.fixedCount).Append(',')
                    .Append(row.gapCount).Append(',')
                    .Append(EscapeCsv(row.note))
                    .AppendLine();
            }

            File.WriteAllText(absolutePath, builder.ToString(), Encoding.UTF8);
            return ReportCsvPath;
        }

        private static Dictionary<string, int> BuildHeaderIndex(string headerLine)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            List<string> columns = SplitCsvLine(headerLine ?? string.Empty);
            for (int i = 0; i < columns.Count; i++)
            {
                string name = (columns[i] ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(name) || map.ContainsKey(name))
                {
                    continue;
                }

                map.Add(name, i);
            }

            return map;
        }

        private static string GetCell(List<string> cells, Dictionary<string, int> header, string column)
        {
            if (!header.TryGetValue(column, out int columnIndex))
            {
                return string.Empty;
            }

            if (columnIndex < 0 || columnIndex >= cells.Count)
            {
                return string.Empty;
            }

            return cells[columnIndex] ?? string.Empty;
        }

        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>(16);
            if (line == null)
            {
                result.Add(string.Empty);
                return result;
            }

            var cell = new StringBuilder();
            bool inQuote = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQuote && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                        continue;
                    }

                    inQuote = !inQuote;
                    continue;
                }

                if (ch == ',' && !inQuote)
                {
                    result.Add(cell.ToString());
                    cell.Length = 0;
                    continue;
                }

                cell.Append(ch);
            }

            result.Add(cell.ToString());
            return result;
        }

        private static string BuildKey(string prefabPath, string attackId)
        {
            string safePrefab = NormalizeAssetPath(prefabPath);
            string safeAttack = (attackId ?? string.Empty).Trim();
            return safePrefab + "|" + safeAttack;
        }

        private static string NormalizeAssetPath(string path)
        {
            string normalized = (path ?? string.Empty).Trim();
            return normalized.Replace('\\', '/');
        }

        private static bool TryParseFloat(string text, out float value)
        {
            string raw = (text ?? string.Empty).Trim();
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            value = 0f;
            return false;
        }

        private static bool TryParseBool(string text, out bool value)
        {
            string raw = (text ?? string.Empty).Trim();
            if (bool.TryParse(raw, out value))
            {
                return true;
            }

            switch (raw.ToLowerInvariant())
            {
                case "1":
                case "y":
                case "yes":
                    value = true;
                    return true;
                case "0":
                case "n":
                case "no":
                    value = false;
                    return true;
                default:
                    value = false;
                    return false;
            }
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private static bool NearlyEqual(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.0001f;
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

        private static void Summarize(List<ReportRow> rows, out int fixedTotal, out int gapTotal, out int errorTotal, out int mismatchTotal)
        {
            fixedTotal = 0;
            gapTotal = 0;
            errorTotal = 0;
            mismatchTotal = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                string status = (rows[i].status ?? string.Empty).Trim();
                if (status.Equals("Fixed", StringComparison.OrdinalIgnoreCase))
                {
                    fixedTotal++;
                    continue;
                }

                if (status.Equals("Gap", StringComparison.OrdinalIgnoreCase))
                {
                    gapTotal++;
                    continue;
                }

                if (status.Equals("Error", StringComparison.OrdinalIgnoreCase))
                {
                    errorTotal++;
                    continue;
                }

                if (status.Equals("Mismatch", StringComparison.OrdinalIgnoreCase))
                {
                    mismatchTotal++;
                }
            }
        }
    }
}

