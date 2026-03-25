using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class SkillResourceGapValidator
    {
        private const string ValidateMenuPath = "Tools/Productization/P2/Validate Skill Resource Gap (CSV)";
        private const string ValidateGateMenuPath = "Tools/Productization/P2/Validate Skill Resource Gap (CI Gate)";
        private const string FixMenuPath = "Tools/Productization/P2/Fix Skill Resource Gap Fallback Flags";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/skill_resource_gap_report.csv";
        private const string LogPrefix = "[SkillResourceGapValidator]";

        private static readonly string[] SearchRoots =
        {
            "Assets/Resources/Skills",
            "Assets/ThirdPersonController/Resources/Skills"
        };

        private struct ValidationRow
        {
            public string layer;
            public string source;
            public string slot;
            public string skillKey;
            public string iconSource;
            public string audioSource;
            public string fxSource;
            public string status;
            public int fixedCount;
            public int gapCount;
            public string note;
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(applyFix: false, failOnBlocking: false, interactive: true);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(applyFix: false, failOnBlocking: true, interactive: false);
        }

        [MenuItem(FixMenuPath)]
        public static void Fix()
        {
            Run(applyFix: true, failOnBlocking: false, interactive: true);
        }

        public static void ApplyForBatch()
        {
            Run(applyFix: true, failOnBlocking: true, interactive: false);
        }

        public static void ValidateForBatch()
        {
            Run(applyFix: false, failOnBlocking: true, interactive: false);
        }

        private static void Run(bool applyFix, bool failOnBlocking, bool interactive)
        {
            var rows = new List<ValidationRow>(128);
            int fixedTotal = 0;
            int gapTotal = 0;

            Dictionary<string, SkillBase> lookup = CollectSkills(rows, applyFix, ref fixedTotal, ref gapTotal);
            ValidateLoadouts(rows, lookup, ref gapTotal);

            if (applyFix)
            {
                AssetDatabase.SaveAssets();
            }

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();

            int skillRows = 0;
            int loadoutRows = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i].layer, "SkillAsset", StringComparison.Ordinal))
                {
                    skillRows++;
                }
                else if (string.Equals(rows[i].layer, "Loadout", StringComparison.Ordinal))
                {
                    loadoutRows++;
                }
            }

            string summary =
                $"mode={(applyFix ? "fix" : "validate")} skills={skillRows} loadoutSlots={loadoutRows} fixed={fixedTotal} gap={gapTotal} report={reportPath}";
            Debug.Log($"{LogPrefix} {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Skill Resource Gap", summary, "OK");
            }

            if (failOnBlocking && gapTotal > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. gap={gapTotal} report={reportPath}");
            }
        }

        private static Dictionary<string, SkillBase> CollectSkills(
            List<ValidationRow> rows,
            bool applyFix,
            ref int fixedTotal,
            ref int gapTotal)
        {
            var lookup = new Dictionary<string, SkillBase>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> skillAssetPaths = CollectSkillAssetPaths();
            var sortedPaths = new List<string>(skillAssetPaths);
            sortedPaths.Sort(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < sortedPaths.Count; i++)
            {
                string path = sortedPaths[i];
                SkillBase skill = AssetDatabase.LoadAssetAtPath<SkillBase>(path);
                if (skill == null)
                {
                    rows.Add(new ValidationRow
                    {
                        layer = "SkillAsset",
                        source = path ?? string.Empty,
                        slot = string.Empty,
                        skillKey = string.Empty,
                        iconSource = "Unknown",
                        audioSource = "Unknown",
                        fxSource = "Unknown",
                        status = "Error",
                        fixedCount = 0,
                        gapCount = 1,
                        note = "Skill asset failed to load."
                    });
                    gapTotal++;
                    continue;
                }

                string resourceKey = GetResourceKey(path);
                if (!string.IsNullOrWhiteSpace(resourceKey) && !lookup.ContainsKey(resourceKey))
                {
                    lookup.Add(resourceKey, skill);
                }

                string fallbackKey = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrWhiteSpace(fallbackKey) && !lookup.ContainsKey(fallbackKey))
                {
                    lookup.Add(fallbackKey, skill);
                }

                ValidationRow row = ValidateSkillAsset(path, skill, applyFix);
                rows.Add(row);
                fixedTotal += Mathf.Max(0, row.fixedCount);
                gapTotal += Mathf.Max(0, row.gapCount);
            }

            return lookup;
        }

        private static ValidationRow ValidateSkillAsset(string path, SkillBase skill, bool applyFix)
        {
            var row = new ValidationRow
            {
                layer = "SkillAsset",
                source = path ?? string.Empty,
                slot = string.Empty,
                skillKey = GetResourceKey(path),
                iconSource = "Unknown",
                audioSource = "Unknown",
                fxSource = "Unknown",
                status = "Error",
                fixedCount = 0,
                gapCount = 0,
                note = string.Empty
            };

            if (skill == null)
            {
                row.status = "Error";
                row.gapCount = 1;
                row.note = "Skill asset is null.";
                return row;
            }

            bool hasIcon = skill.icon != null;
            bool hasAudio = skill.castSound != null || skill.hitSound != null || skill.impactSound != null;
            bool hasFx = skill.effectPrefab != null || skill.castEffectPrefab != null || skill.impactEffectPrefab != null;

            bool changed = false;
            if (!hasAudio && !skill.useFallbackAudioWhenMissing && applyFix)
            {
                skill.useFallbackAudioWhenMissing = true;
                EditorUtility.SetDirty(skill);
                changed = true;
                row.fixedCount++;
            }

            bool canFallbackAudio = hasAudio || skill.useFallbackAudioWhenMissing;
            row.iconSource = hasIcon ? "Asset" : "Fallback(SkillManagerIcon)";
            row.audioSource = hasAudio
                ? "Asset"
                : (skill.useFallbackAudioWhenMissing ? "Fallback(SkillBaseTone)" : "Missing");
            row.fxSource = hasFx ? "Asset" : "Fallback(SkillBaseBurst)";

            if (!canFallbackAudio)
            {
                row.status = applyFix && changed ? "Partial" : "Gap";
                row.gapCount = 1;
                row.note = "All audio clips missing and fallback audio disabled.";
                return row;
            }

            row.status = row.fixedCount > 0 ? "Fixed" : "Ok";
            var notes = new List<string>(4);
            if (!hasIcon)
            {
                notes.Add("icon->fallback");
            }

            if (!hasAudio)
            {
                notes.Add("audio->fallback");
            }

            if (!hasFx)
            {
                notes.Add("fx->fallback");
            }

            row.note = notes.Count > 0 ? string.Join(";", notes) : "fully_bound";
            return row;
        }

        private static void ValidateLoadouts(List<ValidationRow> rows, Dictionary<string, SkillBase> lookup, ref int gapTotal)
        {
            HashSet<string> loadoutPaths = CollectLoadoutPaths();
            var sorted = new List<string>(loadoutPaths);
            sorted.Sort(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < sorted.Count; i++)
            {
                string path = sorted[i];
                SkillLoadoutConfig loadout = AssetDatabase.LoadAssetAtPath<SkillLoadoutConfig>(path);
                if (loadout == null)
                {
                    rows.Add(new ValidationRow
                    {
                        layer = "Loadout",
                        source = path ?? string.Empty,
                        slot = "N/A",
                        skillKey = string.Empty,
                        iconSource = "N/A",
                        audioSource = "N/A",
                        fxSource = "N/A",
                        status = "Error",
                        fixedCount = 0,
                        gapCount = 1,
                        note = "SkillLoadoutConfig failed to load."
                    });
                    gapTotal++;
                    continue;
                }

                string folder = string.IsNullOrWhiteSpace(loadout.resourcesFolder) ? "Skills" : loadout.resourcesFolder.Trim().Trim('/');
                int slotCount = loadout.skillResourceNames != null ? loadout.skillResourceNames.Length : 0;
                if (slotCount <= 0)
                {
                    rows.Add(new ValidationRow
                    {
                        layer = "Loadout",
                        source = path,
                        slot = "N/A",
                        skillKey = string.Empty,
                        iconSource = "N/A",
                        audioSource = "N/A",
                        fxSource = "N/A",
                        status = "Gap",
                        fixedCount = 0,
                        gapCount = 1,
                        note = "Loadout has no skill slots."
                    });
                    gapTotal++;
                    continue;
                }

                for (int slot = 0; slot < slotCount; slot++)
                {
                    string name = loadout.skillResourceNames[slot];
                    string key = string.IsNullOrWhiteSpace(name) ? string.Empty : $"{folder}/{name.Trim()}";
                    bool missingName = string.IsNullOrWhiteSpace(name);
                    bool resolved = !missingName && ResolveSkillFromLoadoutKey(key, name.Trim(), lookup) != null;

                    string status = "Ok";
                    int gapCount = 0;
                    string note = "bound";
                    if (missingName)
                    {
                        status = "Gap";
                        gapCount = 1;
                        note = "skillResourceNames slot empty.";
                    }
                    else if (!resolved)
                    {
                        status = "Gap";
                        gapCount = 1;
                        note = $"Missing skill asset for key '{key}'.";
                    }

                    rows.Add(new ValidationRow
                    {
                        layer = "Loadout",
                        source = path,
                        slot = slot.ToString(),
                        skillKey = key,
                        iconSource = "N/A",
                        audioSource = "N/A",
                        fxSource = "N/A",
                        status = status,
                        fixedCount = 0,
                        gapCount = gapCount,
                        note = note
                    });

                    gapTotal += gapCount;
                }
            }
        }

        private static SkillBase ResolveSkillFromLoadoutKey(string combinedKey, string fallbackName, Dictionary<string, SkillBase> lookup)
        {
            if (!string.IsNullOrWhiteSpace(combinedKey) && lookup.TryGetValue(combinedKey, out SkillBase byCombined))
            {
                return byCombined;
            }

            if (!string.IsNullOrWhiteSpace(fallbackName) && lookup.TryGetValue(fallbackName, out SkillBase byName))
            {
                return byName;
            }

            return null;
        }

        private static HashSet<string> CollectSkillAssetPaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < SearchRoots.Length; i++)
            {
                string root = SearchRoots[i];
                if (!AssetDatabase.IsValidFolder(root))
                {
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { root });
                for (int g = 0; g < guids.Length; g++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                    if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    SkillBase skill = AssetDatabase.LoadAssetAtPath<SkillBase>(path);
                    if (skill == null)
                    {
                        continue;
                    }

                    paths.Add(path);
                }
            }

            return paths;
        }

        private static HashSet<string> CollectLoadoutPaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < SearchRoots.Length; i++)
            {
                string root = SearchRoots[i];
                if (!AssetDatabase.IsValidFolder(root))
                {
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:SkillLoadoutConfig", new[] { root });
                for (int g = 0; g < guids.Length; g++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                    if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    paths.Add(path);
                }
            }

            return paths;
        }

        private static string GetResourceKey(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return string.Empty;
            }

            const string marker = "/Resources/";
            int markerIndex = assetPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return Path.GetFileNameWithoutExtension(assetPath);
            }

            int start = markerIndex + marker.Length;
            if (start >= assetPath.Length)
            {
                return Path.GetFileNameWithoutExtension(assetPath);
            }

            string relative = assetPath.Substring(start);
            if (relative.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                relative = relative.Substring(0, relative.Length - ".asset".Length);
            }

            return relative.Replace('\\', '/');
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var builder = new StringBuilder(4096);
            builder.AppendLine("layer,source,slot,skill_key,icon_source,audio_source,fx_source,status,fixed_count,gap_count,note");

            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                builder
                    .Append(Escape(row.layer)).Append(',')
                    .Append(Escape(row.source)).Append(',')
                    .Append(Escape(row.slot)).Append(',')
                    .Append(Escape(row.skillKey)).Append(',')
                    .Append(Escape(row.iconSource)).Append(',')
                    .Append(Escape(row.audioSource)).Append(',')
                    .Append(Escape(row.fxSource)).Append(',')
                    .Append(Escape(row.status)).Append(',')
                    .Append(row.fixedCount).Append(',')
                    .Append(row.gapCount).Append(',')
                    .Append(Escape(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, builder.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            bool needsQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (value.IndexOf('"') >= 0)
            {
                value = value.Replace("\"", "\"\"");
            }

            return needsQuote ? $"\"{value}\"" : value;
        }
    }
}
