using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class LocalizationKeyExtractionTool
    {
        private const string ApplyMenuPath = "Tools/Productization/P1/Extract Localization Keys (Apply, CSV)";
        private const string ValidateMenuPath = "Tools/Productization/P1/Validate Localization Key Extraction (CSV)";
        private const string ValidateGateMenuPath = "Tools/Productization/P1/Validate Localization Key Extraction (CI Gate)";
        private const string TableAssetPath = "Assets/ThirdPersonController/Resources/Localization/DefaultLocalizationTable.asset";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/localization_key_extraction_report.csv";
        private const string LogPrefix = "[LocalizationKeyExtraction]";

        private static readonly string[] ScriptScanRoots =
        {
            "Assets/ThirdPersonController/Scripts",
            "Assets/Editor"
        };

        private static readonly string[] AssetScanRoots =
        {
            "Assets/Scenes",
            "Assets/Prefabs",
            "Assets/ThirdPersonController"
        };

        private static readonly string[] ManagedPrefixes =
        {
            "ui.",
            "boss.",
            "quest.",
            "level.",
            "tutorial.",
            "input.",
            "system."
        };

        private static readonly Regex ScriptKeyRegex =
            new Regex(
                "(?<quote>[\"'])(?<key>[a-z][a-z0-9_]*(?:\\.[A-Za-z0-9_\\-]+){1,})\\k<quote>",
                RegexOptions.Compiled);

        private static readonly Regex SerializedKeyRegex =
            new Regex(
                "\\bkey\\s*:\\s*(?<key>[a-z][a-z0-9_]*(?:\\.[A-Za-z0-9_\\-]+){1,})\\b",
                RegexOptions.Compiled);

        private struct ValidationRow
        {
            public string layer;
            public string key;
            public string status;
            public int sourceCount;
            public string sources;
            public string zhCN;
            public string enUS;
            public string note;
        }

        private sealed class ReferenceInfo
        {
            public int count;
            public readonly HashSet<string> sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public void AddSource(string source)
            {
                count++;
                if (!string.IsNullOrWhiteSpace(source))
                {
                    sources.Add(source);
                }
            }
        }

        [MenuItem(ApplyMenuPath)]
        public static void ApplyInteractive()
        {
            Run(applyFix: true, failOnBlocking: false, interactive: true);
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
            var rows = new List<ValidationRow>(1024);
            int gapTotal = 0;
            int warningTotal = 0;
            int fixedTotal = 0;

            LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TableAssetPath);
            if (table == null)
            {
                rows.Add(new ValidationRow
                {
                    layer = "Summary",
                    key = string.Empty,
                    status = "Gap",
                    sourceCount = 0,
                    sources = string.Empty,
                    zhCN = string.Empty,
                    enUS = string.Empty,
                    note = $"Localization table missing: {TableAssetPath}"
                });
                gapTotal++;
                string missingReport = WriteCsv(rows);
                AssetDatabase.Refresh();
                string missingSummary = $"mode={(applyFix ? "apply" : "validate")} rows={rows.Count} gap={gapTotal} warnings={warningTotal} fixed={fixedTotal} report={missingReport}";
                Debug.Log($"{LogPrefix} {missingSummary}");
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Localization Key Extraction", missingSummary, "OK");
                }

                if (failOnBlocking)
                {
                    throw new InvalidOperationException($"{LogPrefix} gate failed. gap={gapTotal} report={missingReport}");
                }

                return;
            }

            if (table.entries == null)
            {
                table.entries = new List<LocalizationEntry>();
            }

            bool changed = NormalizeDuplicateEntries(table, applyFix, rows, ref gapTotal, ref fixedTotal);

            Dictionary<string, ReferenceInfo> references = CollectReferences();
            var orderedReferenceKeys = new List<string>(references.Keys);
            orderedReferenceKeys.Sort(StringComparer.Ordinal);

            var tableLookup = new Dictionary<string, LocalizationEntry>(StringComparer.Ordinal);
            for (int i = 0; i < table.entries.Count; i++)
            {
                LocalizationEntry entry = table.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                {
                    continue;
                }

                string key = entry.key.Trim();
                entry.key = key;
                if (!tableLookup.ContainsKey(key))
                {
                    tableLookup.Add(key, entry);
                }
            }

            for (int i = 0; i < orderedReferenceKeys.Count; i++)
            {
                string key = orderedReferenceKeys[i];
                ReferenceInfo info = references[key];
                string sourceSummary = BuildSourceSummary(info.sources, 4);

                if (!tableLookup.TryGetValue(key, out LocalizationEntry existing))
                {
                    if (applyFix)
                    {
                        LocalizationEntry added = CreateDefaultEntry(key);
                        table.entries.Add(added);
                        tableLookup.Add(key, added);
                        changed = true;
                        fixedTotal++;

                        rows.Add(new ValidationRow
                        {
                            layer = "Reference",
                            key = key,
                            status = "Fixed",
                            sourceCount = info.count,
                            sources = sourceSummary,
                            zhCN = added.zhCN,
                            enUS = added.enUS,
                            note = "Missing key auto-added to localization table."
                        });
                    }
                    else
                    {
                        gapTotal++;
                        rows.Add(new ValidationRow
                        {
                            layer = "Reference",
                            key = key,
                            status = "Gap",
                            sourceCount = info.count,
                            sources = sourceSummary,
                            zhCN = string.Empty,
                            enUS = string.Empty,
                            note = "Referenced key is missing from localization table."
                        });
                    }

                    continue;
                }

                rows.Add(new ValidationRow
                {
                    layer = "Reference",
                    key = key,
                    status = "Ok",
                    sourceCount = info.count,
                    sources = sourceSummary,
                    zhCN = existing.zhCN ?? string.Empty,
                    enUS = existing.enUS ?? string.Empty,
                    note = "referenced_and_resolved"
                });
            }

            var tableKeys = new List<string>(tableLookup.Keys);
            tableKeys.Sort(StringComparer.Ordinal);
            for (int i = 0; i < tableKeys.Count; i++)
            {
                string key = tableKeys[i];
                if (references.ContainsKey(key))
                {
                    continue;
                }

                warningTotal++;
                LocalizationEntry entry = tableLookup[key];
                rows.Add(new ValidationRow
                {
                    layer = "Table",
                    key = key,
                    status = "Warning",
                    sourceCount = 0,
                    sources = string.Empty,
                    zhCN = entry != null ? (entry.zhCN ?? string.Empty) : string.Empty,
                    enUS = entry != null ? (entry.enUS ?? string.Empty) : string.Empty,
                    note = "Table key is currently unreferenced (orphan candidate)."
                });
            }

            if (applyFix && changed)
            {
                table.entries.Sort((a, b) =>
                {
                    string ka = a != null && a.key != null ? a.key : string.Empty;
                    string kb = b != null && b.key != null ? b.key : string.Empty;
                    return string.Compare(ka, kb, StringComparison.Ordinal);
                });
                table.RebuildLookup();
                EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssets();

                LocalizationEncodingRepairTool.ApplyForBatch();
                rows.Add(new ValidationRow
                {
                    layer = "Pipeline",
                    key = "encoding_repair",
                    status = "Fixed",
                    sourceCount = 0,
                    sources = string.Empty,
                    zhCN = string.Empty,
                    enUS = string.Empty,
                    note = "Encoding repair pass completed after key extraction apply."
                });
            }

            rows.Add(new ValidationRow
            {
                layer = "Summary",
                key = string.Empty,
                status = gapTotal > 0 ? "Gap" : "Ok",
                sourceCount = references.Count,
                sources = string.Empty,
                zhCN = string.Empty,
                enUS = string.Empty,
                note = $"mode={(applyFix ? "apply" : "validate")}; refs={references.Count}; warnings={warningTotal}; fixed={fixedTotal}"
            });

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();

            string summary = $"mode={(applyFix ? "apply" : "validate")} rows={rows.Count} gap={gapTotal} warnings={warningTotal} fixed={fixedTotal} report={reportPath}";
            Debug.Log($"{LogPrefix} {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Localization Key Extraction", summary, "OK");
            }

            if (failOnBlocking && gapTotal > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. gap={gapTotal} report={reportPath}");
            }
        }

        private static bool NormalizeDuplicateEntries(
            LocalizationTable table,
            bool applyFix,
            List<ValidationRow> rows,
            ref int gapTotal,
            ref int fixedTotal)
        {
            bool changed = false;
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = table.entries.Count - 1; i >= 0; i--)
            {
                LocalizationEntry entry = table.entries[i];
                if (entry == null)
                {
                    if (applyFix)
                    {
                        table.entries.RemoveAt(i);
                        changed = true;
                        fixedTotal++;
                        rows.Add(new ValidationRow
                        {
                            layer = "Table",
                            key = "<null_entry>",
                            status = "Fixed",
                            sourceCount = 0,
                            sources = string.Empty,
                            zhCN = string.Empty,
                            enUS = string.Empty,
                            note = "Removed null localization entry."
                        });
                    }
                    else
                    {
                        gapTotal++;
                        rows.Add(new ValidationRow
                        {
                            layer = "Table",
                            key = "<null_entry>",
                            status = "Gap",
                            sourceCount = 0,
                            sources = string.Empty,
                            zhCN = string.Empty,
                            enUS = string.Empty,
                            note = "Found null localization entry."
                        });
                    }

                    continue;
                }

                string key = entry.key == null ? string.Empty : entry.key.Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    if (applyFix)
                    {
                        table.entries.RemoveAt(i);
                        changed = true;
                        fixedTotal++;
                        rows.Add(new ValidationRow
                        {
                            layer = "Table",
                            key = "<empty_key>",
                            status = "Fixed",
                            sourceCount = 0,
                            sources = string.Empty,
                            zhCN = entry.zhCN ?? string.Empty,
                            enUS = entry.enUS ?? string.Empty,
                            note = "Removed empty-key localization entry."
                        });
                    }
                    else
                    {
                        gapTotal++;
                        rows.Add(new ValidationRow
                        {
                            layer = "Table",
                            key = "<empty_key>",
                            status = "Gap",
                            sourceCount = 0,
                            sources = string.Empty,
                            zhCN = entry.zhCN ?? string.Empty,
                            enUS = entry.enUS ?? string.Empty,
                            note = "Localization entry has empty key."
                        });
                    }

                    continue;
                }

                entry.key = key;
                if (!seen.Add(key))
                {
                    if (applyFix)
                    {
                        table.entries.RemoveAt(i);
                        changed = true;
                        fixedTotal++;
                        rows.Add(new ValidationRow
                        {
                            layer = "Table",
                            key = key,
                            status = "Fixed",
                            sourceCount = 0,
                            sources = string.Empty,
                            zhCN = entry.zhCN ?? string.Empty,
                            enUS = entry.enUS ?? string.Empty,
                            note = "Removed duplicate localization key entry."
                        });
                    }
                    else
                    {
                        gapTotal++;
                        rows.Add(new ValidationRow
                        {
                            layer = "Table",
                            key = key,
                            status = "Gap",
                            sourceCount = 0,
                            sources = string.Empty,
                            zhCN = entry.zhCN ?? string.Empty,
                            enUS = entry.enUS ?? string.Empty,
                            note = "Duplicate localization key entry."
                        });
                    }
                }
            }

            return changed;
        }

        private static Dictionary<string, ReferenceInfo> CollectReferences()
        {
            var references = new Dictionary<string, ReferenceInfo>(StringComparer.Ordinal);

            for (int i = 0; i < ScriptScanRoots.Length; i++)
            {
                ScanDirectoryForScriptKeys(ScriptScanRoots[i], references);
            }

            for (int i = 0; i < AssetScanRoots.Length; i++)
            {
                ScanDirectoryForSerializedKeys(AssetScanRoots[i], references);
            }

            return references;
        }

        private static void ScanDirectoryForScriptKeys(string rootPath, Dictionary<string, ReferenceInfo> references)
        {
            string fullRoot = Path.GetFullPath(rootPath);
            if (!Directory.Exists(fullRoot))
            {
                return;
            }

            string[] files = Directory.GetFiles(fullRoot, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                string content;
                try
                {
                    content = File.ReadAllText(path);
                }
                catch
                {
                    continue;
                }

                MatchCollection matches = ScriptKeyRegex.Matches(content);
                for (int m = 0; m < matches.Count; m++)
                {
                    Match match = matches[m];
                    if (!match.Success)
                    {
                        continue;
                    }

                    string key = NormalizeLocalizationKey(match.Groups["key"].Value);
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    AddReference(references, key, NormalizeAssetPath(path));
                }
            }
        }

        private static void ScanDirectoryForSerializedKeys(string rootPath, Dictionary<string, ReferenceInfo> references)
        {
            string fullRoot = Path.GetFullPath(rootPath);
            if (!Directory.Exists(fullRoot))
            {
                return;
            }

            string[] patterns = { "*.prefab", "*.unity", "*.asset" };
            for (int p = 0; p < patterns.Length; p++)
            {
                string[] files = Directory.GetFiles(fullRoot, patterns[p], SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    string path = files[i];
                    string content;
                    try
                    {
                        content = File.ReadAllText(path);
                    }
                    catch
                    {
                        continue;
                    }

                    MatchCollection matches = SerializedKeyRegex.Matches(content);
                    for (int m = 0; m < matches.Count; m++)
                    {
                        Match match = matches[m];
                        if (!match.Success)
                        {
                            continue;
                        }

                        string key = NormalizeLocalizationKey(match.Groups["key"].Value);
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            continue;
                        }

                        AddReference(references, key, NormalizeAssetPath(path));
                    }
                }
            }
        }

        private static string NormalizeLocalizationKey(string rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
            {
                return string.Empty;
            }

            string key = rawKey.Trim();
            for (int i = 0; i < ManagedPrefixes.Length; i++)
            {
                if (key.StartsWith(ManagedPrefixes[i], StringComparison.Ordinal))
                {
                    return key;
                }
            }

            return string.Empty;
        }

        private static void AddReference(Dictionary<string, ReferenceInfo> references, string key, string source)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (!references.TryGetValue(key, out ReferenceInfo info))
            {
                info = new ReferenceInfo();
                references.Add(key, info);
            }

            info.AddSource(source);
        }

        private static LocalizationEntry CreateDefaultEntry(string key)
        {
            string english = BuildEnglishDefaultText(key);
            return new LocalizationEntry
            {
                key = key,
                zhCN = $"待翻译：{english}",
                enUS = english
            };
        }

        private static string BuildEnglishDefaultText(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return "Undefined";
            }

            string[] segments = key.Split('.');
            string token = segments.Length > 0 ? segments[segments.Length - 1] : key;
            if (string.IsNullOrWhiteSpace(token))
            {
                token = key;
            }

            token = token.Replace("_", " ").Replace("-", " ").Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return key;
            }

            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
            return textInfo.ToTitleCase(token.ToLowerInvariant());
        }

        private static string BuildSourceSummary(HashSet<string> sources, int maxCount)
        {
            if (sources == null || sources.Count == 0)
            {
                return string.Empty;
            }

            int take = Mathf.Max(1, maxCount);
            var ordered = new List<string>(sources);
            ordered.Sort(StringComparer.OrdinalIgnoreCase);

            if (ordered.Count <= take)
            {
                return string.Join(" | ", ordered);
            }

            List<string> head = ordered.GetRange(0, take);
            return string.Join(" | ", head) + $" | +{ordered.Count - take} more";
        }

        private static string NormalizeAssetPath(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return string.Empty;
            }

            string normalized = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets" + normalized.Substring(dataPath.Length);
            }

            return normalized;
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var builder = new StringBuilder(8192);
            builder.AppendLine("layer,key,status,source_count,sources,zh_cn,en_us,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                builder
                    .Append(Escape(row.layer)).Append(',')
                    .Append(Escape(row.key)).Append(',')
                    .Append(Escape(row.status)).Append(',')
                    .Append(row.sourceCount).Append(',')
                    .Append(Escape(row.sources)).Append(',')
                    .Append(Escape(row.zhCN)).Append(',')
                    .Append(Escape(row.enUS)).Append(',')
                    .Append(Escape(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, builder.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static string Escape(string value)
        {
            string text = value ?? string.Empty;
            bool needsQuote = text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (text.IndexOf('"') >= 0)
            {
                text = text.Replace("\"", "\"\"");
            }

            return needsQuote ? $"\"{text}\"" : text;
        }
    }
}
