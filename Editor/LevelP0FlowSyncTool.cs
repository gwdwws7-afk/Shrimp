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
    /// <summary>
    /// P0 synchronization tool:
    /// 1) Force LEVEL_01~LEVEL_10 stronghold chain to S1->S2->S3.
    /// 2) Rebuild strongholdOverrides from 32B wave-event mapping.
    /// </summary>
    public static class LevelP0FlowSyncTool
    {
        private const string ApplyMenuPath = "Tools/Level/P0/Sync LevelData S3 Flow (32A+32B)";
        private const string ValidateMenuPath = "Tools/Level/P0/Validate LevelData S3 Flow (Report)";
        private const string LayoutCsvPath = "Assets/GameDesign/游戏设计/32A_LevelBeatSheet_FinalPolish.csv";
        private const string WaveCsvPath = "Assets/GameDesign/游戏设计/32B_LevelWaveEventAssetMapping_FinalPolish.csv";
        private const string LevelDataFolder = "Assets/GameDesign/Data";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/p0_leveldata_s3_sync_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/p0_leveldata_s3_sync_summary.md";

        [MenuItem(ApplyMenuPath)]
        public static void ApplyFromMenu()
        {
            Run(apply: true, failOnError: false);
        }

        [MenuItem(ValidateMenuPath)]
        public static void ValidateFromMenu()
        {
            Run(apply: false, failOnError: false);
        }

        // Batch mode entry:
        // Unity.exe -batchmode -projectPath <path> -executeMethod ThirdPersonController.Editor.LevelP0FlowSyncTool.ApplyForBatch -quit
        public static void ApplyForBatch()
        {
            Run(apply: true, failOnError: true);
        }

        public static void ValidateForBatch()
        {
            Run(apply: false, failOnError: true);
        }

        private static void Run(bool apply, bool failOnError)
        {
            List<Dictionary<string, string>> beatRows = ReadCsv(LayoutCsvPath);
            List<Dictionary<string, string>> waveRows = ReadCsv(WaveCsvPath);

            Dictionary<string, string> strongholdOrderByLevel = BuildStrongholdOrderMap(beatRows);
            Dictionary<string, Dictionary<string, Dictionary<int, List<CsvWaveRow>>>> wavesByLevel =
                BuildWaveMap(waveRows);

            Dictionary<string, EnemyArchetype> archetypeLookup = BuildArchetypeLookup();
            List<LevelTarget> targets = CollectLevelTargets();

            var reportRows = new List<ReportRow>(targets.Count);
            int errorCount = 0;
            int changedCount = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                ReportRow row = ProcessTarget(
                    targets[i],
                    apply,
                    strongholdOrderByLevel,
                    wavesByLevel,
                    archetypeLookup);
                reportRows.Add(row);

                if (row.changed)
                {
                    changedCount++;
                }

                if (string.Equals(row.status, "Error", StringComparison.Ordinal))
                {
                    errorCount++;
                }
            }

            if (apply)
            {
                AssetDatabase.SaveAssets();
            }

            WriteReport(reportRows, changedCount, errorCount, apply);
            AssetDatabase.Refresh();

            string mode = apply ? "apply" : "validate";
            string summary = $"[LevelP0FlowSync] mode={mode} targets={reportRows.Count} changed={changedCount} errors={errorCount}";
            Debug.Log(summary);

            if (failOnError && errorCount > 0)
            {
                throw new InvalidOperationException(summary);
            }
        }

        private static ReportRow ProcessTarget(
            LevelTarget target,
            bool apply,
            Dictionary<string, string> strongholdOrderByLevel,
            Dictionary<string, Dictionary<string, Dictionary<int, List<CsvWaveRow>>>> wavesByLevel,
            Dictionary<string, EnemyArchetype> archetypeLookup)
        {
            var row = new ReportRow
            {
                levelId = target.levelId,
                assetPath = target.assetPath,
                status = "Error",
                note = string.Empty
            };

            if (target.levelData == null)
            {
                row.note = "LevelData is null.";
                return row;
            }

            row.strongholdCountBefore = target.levelData.strongholds != null ? target.levelData.strongholds.Count : 0;
            row.overrideStrongholdsBefore = target.levelData.strongholdOverrides != null ? target.levelData.strongholdOverrides.Count : 0;

            if (!strongholdOrderByLevel.TryGetValue(target.levelId, out string strongholdOrderText))
            {
                row.note = "Missing 32A stronghold_order row.";
                return row;
            }

            if (!wavesByLevel.TryGetValue(target.levelId, out Dictionary<string, Dictionary<int, List<CsvWaveRow>>> byStronghold))
            {
                row.note = "Missing 32B rows for level.";
                return row;
            }

            if (!byStronghold.ContainsKey("Stronghold_01") ||
                !byStronghold.ContainsKey("Stronghold_02") ||
                !byStronghold.ContainsKey("Stronghold_03"))
            {
                row.note = "32B is missing one or more required strongholds (S1/S2/S3).";
                return row;
            }

            row.csvRowCount = CountWaveRows(byStronghold);
            row.strongholdOrderText = strongholdOrderText;

            int missingArchetypeRefs = 0;

            if (apply)
            {
                List<StrongholdConfig> rebuiltStrongholds = BuildStrongholdChain();
                List<StrongholdOverride> rebuiltOverrides =
                    BuildStrongholdOverrides(byStronghold, archetypeLookup, ref missingArchetypeRefs);

                target.levelData.strongholds = rebuiltStrongholds;
                target.levelData.strongholdOverrides = rebuiltOverrides;
                EditorUtility.SetDirty(target.levelData);
                row.changed = true;
            }
            else
            {
                row.changed = false;
            }

            row.missingArchetypeRefs = missingArchetypeRefs;
            row.strongholdCountAfter = 3;
            row.overrideStrongholdsAfter = 3;
            row.syncedWaveCount = CountWaves(byStronghold);

            if (missingArchetypeRefs > 0)
            {
                row.status = "Warn";
                row.note = $"Synced with missing archetype refs={missingArchetypeRefs}.";
            }
            else
            {
                row.status = "Ok";
                row.note = apply ? "Synced S1->S2->S3 chain and overrides." : "Validation passed.";
            }

            return row;
        }

        private static List<StrongholdConfig> BuildStrongholdChain()
        {
            return new List<StrongholdConfig>
            {
                new StrongholdConfig { strongholdId = "Stronghold_01", required = true, order = 0 },
                new StrongholdConfig { strongholdId = "Stronghold_02", required = true, order = 1 },
                new StrongholdConfig { strongholdId = "Stronghold_03", required = true, order = 2 }
            };
        }

        private static List<StrongholdOverride> BuildStrongholdOverrides(
            Dictionary<string, Dictionary<int, List<CsvWaveRow>>> byStronghold,
            Dictionary<string, EnemyArchetype> archetypeLookup,
            ref int missingArchetypeRefs)
        {
            var output = new List<StrongholdOverride>(3);
            string[] strongholdIds = { "Stronghold_01", "Stronghold_02", "Stronghold_03" };

            for (int i = 0; i < strongholdIds.Length; i++)
            {
                string strongholdId = strongholdIds[i];
                if (!byStronghold.TryGetValue(strongholdId, out Dictionary<int, List<CsvWaveRow>> wavesByIndex))
                {
                    continue;
                }

                var strongholdOverride = new StrongholdOverride
                {
                    strongholdId = strongholdId,
                    waves = new List<StrongholdWaveOverride>()
                };

                List<int> waveIndexes = new List<int>(wavesByIndex.Keys);
                waveIndexes.Sort();
                for (int w = 0; w < waveIndexes.Count; w++)
                {
                    int waveIndex = waveIndexes[w];
                    List<CsvWaveRow> eventRows = wavesByIndex[waveIndex];
                    var waveOverride = new StrongholdWaveOverride
                    {
                        waveIndex = waveIndex,
                        replaceEvents = true,
                        events = new List<WaveEventOverride>()
                    };

                    for (int e = 0; e < eventRows.Count; e++)
                    {
                        CsvWaveRow csv = eventRows[e];
                        var waveEvent = new WaveEventOverride
                        {
                            name = csv.eventType.ToString(),
                            eventType = csv.eventType,
                            triggerDelay = 0.4f,
                            triggerOnRemaining = csv.triggerOnRemaining,
                            spawnCount = Mathf.Max(0, csv.spawnCount),
                            useReinforcementPoints = true,
                            holdDecayRate = 1f,
                            showHoldMarker = true,
                            spawnDefenseTarget = csv.eventType == WaveEventType.ProtectTarget,
                            failOnTargetDestroyed = true,
                            assignTargetToSpawnedEnemies = true,
                            defenseTargetHealth = csv.defenseTargetHealth,
                            holdRadius = 5f,
                            holdDuration = csv.holdDuration,
                            duration = csv.duration,
                            spawnInterval = 0f,
                            spawnRadius = 0f
                        };

                        switch (csv.eventType)
                        {
                            case WaveEventType.Chase:
                                waveEvent.duration = csv.duration > 0f ? csv.duration : 8f;
                                break;
                            case WaveEventType.HoldPoint:
                                waveEvent.holdDuration = csv.holdDuration > 0f ? csv.holdDuration : 8f;
                                waveEvent.spawnCount = 0;
                                break;
                            case WaveEventType.ProtectTarget:
                                waveEvent.defenseTargetHealth = csv.defenseTargetHealth > 0 ? csv.defenseTargetHealth : 240;
                                break;
                            case WaveEventType.Reinforcement:
                            default:
                                break;
                        }

                        EnemyArchetype archetype = ResolveArchetype(csv.archetype, archetypeLookup);
                        if (archetype == null)
                        {
                            missingArchetypeRefs++;
                        }
                        waveEvent.archetypeOverride = archetype;
                        waveOverride.events.Add(waveEvent);
                    }

                    strongholdOverride.waves.Add(waveOverride);
                }

                output.Add(strongholdOverride);
            }

            return output;
        }

        private static EnemyArchetype ResolveArchetype(string archetypeKey, Dictionary<string, EnemyArchetype> lookup)
        {
            string key = Normalize(archetypeKey);
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (lookup.TryGetValue(key, out EnemyArchetype exact))
            {
                return exact;
            }

            // Compatibility aliases.
            if (string.Equals(key, "controller", StringComparison.OrdinalIgnoreCase))
            {
                if (lookup.TryGetValue("ranged", out EnemyArchetype ranged))
                {
                    return ranged;
                }
            }

            if (string.Equals(key, "ranged/controller", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "rangedcontroller", StringComparison.OrdinalIgnoreCase))
            {
                if (lookup.TryGetValue("ranged", out EnemyArchetype ranged))
                {
                    return ranged;
                }
            }

            return null;
        }

        private static Dictionary<string, EnemyArchetype> BuildArchetypeLookup()
        {
            string[] guids = AssetDatabase.FindAssets("t:EnemyArchetype", new[] { LevelDataFolder });
            var lookup = new Dictionary<string, EnemyArchetype>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EnemyArchetype archetype = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(path);
                if (archetype == null)
                {
                    continue;
                }

                AddLookup(lookup, archetype.archetypeId, archetype);
                AddLookup(lookup, archetype.name, archetype);
                AddLookup(lookup, archetype.displayName, archetype);
            }

            return lookup;
        }

        private static void AddLookup(Dictionary<string, EnemyArchetype> lookup, string key, EnemyArchetype archetype)
        {
            string normalized = Normalize(key);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            if (!lookup.ContainsKey(normalized))
            {
                lookup.Add(normalized, archetype);
            }
        }

        private static string Normalize(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            return key.Trim().ToLowerInvariant();
        }

        private static Dictionary<string, string> BuildStrongholdOrderMap(List<Dictionary<string, string>> rows)
        {
            var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rows.Count; i++)
            {
                string levelId = Get(rows[i], "level_id");
                if (string.IsNullOrWhiteSpace(levelId))
                {
                    continue;
                }

                output[levelId] = Get(rows[i], "stronghold_order");
            }

            return output;
        }

        private static Dictionary<string, Dictionary<string, Dictionary<int, List<CsvWaveRow>>>> BuildWaveMap(
            List<Dictionary<string, string>> rows)
        {
            var output = new Dictionary<string, Dictionary<string, Dictionary<int, List<CsvWaveRow>>>>(
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> raw = rows[i];
                string levelId = Get(raw, "level_id");
                string stronghold = Get(raw, "stronghold");
                if (string.IsNullOrWhiteSpace(levelId) || string.IsNullOrWhiteSpace(stronghold))
                {
                    continue;
                }

                int waveIndex = ParseInt(Get(raw, "wave_index"), 0);
                var parsed = new CsvWaveRow
                {
                    levelId = levelId,
                    stronghold = stronghold,
                    waveIndex = waveIndex,
                    eventType = ParseEventType(Get(raw, "event_type"), Get(raw, "design_intent")),
                    archetype = Get(raw, "archetype"),
                    spawnCount = ParseInt(Get(raw, "spawn_count"), 0),
                    triggerOnRemaining = ParseInt(Get(raw, "trigger_on_remaining"), -1),
                    duration = ParseFloat(Get(raw, "duration_sec"), 0f),
                    holdDuration = ParseFloat(Get(raw, "hold_duration_sec"), 0f),
                    defenseTargetHealth = ParseInt(Get(raw, "defense_target_hp"), 0),
                    designIntent = Get(raw, "design_intent")
                };

                if (!output.TryGetValue(levelId, out Dictionary<string, Dictionary<int, List<CsvWaveRow>>> byStronghold))
                {
                    byStronghold = new Dictionary<string, Dictionary<int, List<CsvWaveRow>>>(StringComparer.OrdinalIgnoreCase);
                    output[levelId] = byStronghold;
                }

                if (!byStronghold.TryGetValue(stronghold, out Dictionary<int, List<CsvWaveRow>> byWave))
                {
                    byWave = new Dictionary<int, List<CsvWaveRow>>();
                    byStronghold[stronghold] = byWave;
                }

                if (!byWave.TryGetValue(waveIndex, out List<CsvWaveRow> eventRows))
                {
                    eventRows = new List<CsvWaveRow>();
                    byWave[waveIndex] = eventRows;
                }

                eventRows.Add(parsed);
            }

            return output;
        }

        private static WaveEventType ParseEventType(string eventTypeText, string intentText)
        {
            string text = eventTypeText != null ? eventTypeText.Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (Enum.TryParse(text, true, out WaveEventType parsed))
                {
                    return parsed;
                }
            }

            string intent = intentText != null ? intentText : string.Empty;
            if (intent.Contains("追击"))
            {
                return WaveEventType.Chase;
            }
            if (intent.Contains("站位"))
            {
                return WaveEventType.HoldPoint;
            }
            if (intent.Contains("保护"))
            {
                return WaveEventType.ProtectTarget;
            }

            return WaveEventType.Reinforcement;
        }

        private static List<LevelTarget> CollectLevelTargets()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelDataFolder });
            var output = new List<LevelTarget>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(fileName) ||
                    !fileName.StartsWith("LevelData_Level", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (data == null)
                {
                    continue;
                }

                string levelId = data.levelId != null ? data.levelId.Trim() : string.Empty;
                int idx = ParseLevelIndex(levelId);
                if (idx < 1 || idx > 10)
                {
                    continue;
                }

                output.Add(new LevelTarget
                {
                    levelId = levelId,
                    levelIndex = idx,
                    assetPath = path,
                    levelData = data
                });
            }

            output.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));
            return output;
        }

        private static int ParseLevelIndex(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return -1;
            }

            int underscore = levelId.LastIndexOf('_');
            if (underscore >= 0 && underscore + 1 < levelId.Length)
            {
                string suffix = levelId.Substring(underscore + 1);
                if (int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                {
                    return parsed;
                }
            }

            if (levelId.StartsWith("LEVEL_", StringComparison.OrdinalIgnoreCase))
            {
                string suffix = levelId.Substring("LEVEL_".Length);
                if (int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                {
                    return parsed;
                }
            }

            return -1;
        }

        private static int CountWaveRows(Dictionary<string, Dictionary<int, List<CsvWaveRow>>> byStronghold)
        {
            int total = 0;
            foreach (KeyValuePair<string, Dictionary<int, List<CsvWaveRow>>> stronghold in byStronghold)
            {
                foreach (KeyValuePair<int, List<CsvWaveRow>> wave in stronghold.Value)
                {
                    total += wave.Value != null ? wave.Value.Count : 0;
                }
            }

            return total;
        }

        private static int CountWaves(Dictionary<string, Dictionary<int, List<CsvWaveRow>>> byStronghold)
        {
            int total = 0;
            foreach (KeyValuePair<string, Dictionary<int, List<CsvWaveRow>>> stronghold in byStronghold)
            {
                total += stronghold.Value != null ? stronghold.Value.Count : 0;
            }

            return total;
        }

        private static void WriteReport(List<ReportRow> rows, int changedCount, int errorCount, bool apply)
        {
            string reportAbs = ToAbsolutePath(ReportCsvPath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportAbs));

            var csv = new StringBuilder();
            csv.AppendLine("level_id,asset_path,stronghold_order_text,stronghold_count_before,stronghold_count_after,override_strongholds_before,override_strongholds_after,csv_rows,synced_waves,missing_archetype_refs,changed,status,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ReportRow row = rows[i];
                csv.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
                    CsvEscape(row.levelId),
                    CsvEscape(row.assetPath),
                    CsvEscape(row.strongholdOrderText),
                    row.strongholdCountBefore,
                    row.strongholdCountAfter,
                    row.overrideStrongholdsBefore,
                    row.overrideStrongholdsAfter,
                    row.csvRowCount,
                    row.syncedWaveCount,
                    row.missingArchetypeRefs,
                    row.changed ? "1" : "0",
                    CsvEscape(row.status),
                    CsvEscape(row.note)));
            }

            File.WriteAllText(reportAbs, csv.ToString(), Encoding.UTF8);

            string summaryAbs = ToAbsolutePath(SummaryMdPath);
            Directory.CreateDirectory(Path.GetDirectoryName(summaryAbs));
            var md = new StringBuilder();
            md.AppendLine("# P0 LevelData S3 Flow Sync Summary");
            md.AppendLine();
            md.AppendLine($"- Mode: {(apply ? "Apply" : "Validate")}");
            md.AppendLine($"- Targets: {rows.Count}");
            md.AppendLine($"- Changed: {changedCount}");
            md.AppendLine($"- Errors: {errorCount}");
            md.AppendLine($"- Report: `{ReportCsvPath}`");
            File.WriteAllText(summaryAbs, md.ToString(), Encoding.UTF8);
        }

        private static string CsvEscape(string value)
        {
            string text = value ?? string.Empty;
            text = text.Replace("\"", "\"\"");
            return "\"" + text + "\"";
        }

        private static string Get(Dictionary<string, string> row, string key)
        {
            if (row == null || !row.TryGetValue(key, out string value))
            {
                return string.Empty;
            }

            return value ?? string.Empty;
        }

        private static int ParseInt(string text, int fallback)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static float ParseFloat(string text, float fallback)
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static List<Dictionary<string, string>> ReadCsv(string assetRelativePath)
        {
            string absolutePath = ToAbsolutePath(assetRelativePath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"CSV not found: {absolutePath}");
            }

            string[] lines = File.ReadAllLines(absolutePath, Encoding.UTF8);
            if (lines.Length == 0)
            {
                return new List<Dictionary<string, string>>();
            }

            string[] headers = ParseCsvLine(lines[0]);
            var rows = new List<Dictionary<string, string>>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                string[] fields = ParseCsvLine(lines[i]);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int h = 0; h < headers.Length; h++)
                {
                    string value = h < fields.Length ? fields[h] : string.Empty;
                    row[headers[h]] = value;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(sb.ToString());
                    sb.Length = 0;
                }
                else
                {
                    sb.Append(c);
                }
            }

            fields.Add(sb.ToString());
            return fields.ToArray();
        }

        private static string ToAbsolutePath(string assetRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private struct CsvWaveRow
        {
            public string levelId;
            public string stronghold;
            public int waveIndex;
            public WaveEventType eventType;
            public string archetype;
            public int spawnCount;
            public int triggerOnRemaining;
            public float duration;
            public float holdDuration;
            public int defenseTargetHealth;
            public string designIntent;
        }

        private struct LevelTarget
        {
            public string levelId;
            public int levelIndex;
            public string assetPath;
            public LevelData levelData;
        }

        private struct ReportRow
        {
            public string levelId;
            public string assetPath;
            public string strongholdOrderText;
            public int strongholdCountBefore;
            public int strongholdCountAfter;
            public int overrideStrongholdsBefore;
            public int overrideStrongholdsAfter;
            public int csvRowCount;
            public int syncedWaveCount;
            public int missingArchetypeRefs;
            public bool changed;
            public string status;
            public string note;
        }
    }
}
