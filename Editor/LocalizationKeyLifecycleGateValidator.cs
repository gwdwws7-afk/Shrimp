using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class LocalizationKeyLifecycleGateValidator
    {
        private const string ValidateMenuPath = "Tools/Productization/P1/Validate Localization Key Lifecycle (CSV)";
        private const string ValidateGateMenuPath = "Tools/Productization/P1/Validate Localization Key Lifecycle (CI Gate)";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/localization_key_lifecycle_gate_report.csv";
        private const string TableAssetPath = "Assets/ThirdPersonController/Resources/Localization/DefaultLocalizationTable.asset";
        private const string LogPrefix = "[LocalizationKeyLifecycleGate]";

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

        private static readonly Regex ScriptKeyRegex =
            new Regex(
                "(?<quote>[\"'])(?<key>(ui|boss)\\.[A-Za-z0-9_\\-]+(?:\\.[A-Za-z0-9_\\-]+)*)\\k<quote>",
                RegexOptions.Compiled);

        private static readonly Regex SerializedKeyRegex =
            new Regex(
                "\\bkey\\s*:\\s*(?<key>(ui|boss)\\.[A-Za-z0-9_\\-]+(?:\\.[A-Za-z0-9_\\-]+)*)\\b",
                RegexOptions.Compiled);

        private struct ValidationRow
        {
            public string layer;
            public string key;
            public string status;
            public int refCount;
            public string note;
            public string sources;
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

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(failOnBlocking: false, interactive: true);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(failOnBlocking: true, interactive: false);
        }

        public static void ValidateForBatch()
        {
            Run(failOnBlocking: true, interactive: false);
        }

        private static void Run(bool failOnBlocking, bool interactive)
        {
            var rows = new List<ValidationRow>(512);
            int gapTotal = 0;
            int warningTotal = 0;

            LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TableAssetPath);
            if (table == null)
            {
                rows.Add(new ValidationRow
                {
                    layer = "Summary",
                    key = string.Empty,
                    status = "Gap",
                    refCount = 0,
                    note = $"Localization table missing: {TableAssetPath}",
                    sources = string.Empty
                });
                gapTotal++;
            }
            else
            {
                EvaluateLifecycle(table, rows, ref gapTotal, ref warningTotal);
            }

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();

            string summary = $"rows={rows.Count} gap={gapTotal} warnings={warningTotal} report={reportPath}";
            Debug.Log($"{LogPrefix} {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Localization Key Lifecycle Gate", summary, "OK");
            }

            if (failOnBlocking && gapTotal > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. gap={gapTotal} report={reportPath}");
            }
        }

        private static void EvaluateLifecycle(
            LocalizationTable table,
            List<ValidationRow> rows,
            ref int gapTotal,
            ref int warningTotal)
        {
            var tableKeys = new HashSet<string>(StringComparer.Ordinal);
            List<LocalizationEntry> entries = table.entries ?? new List<LocalizationEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                LocalizationEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                {
                    continue;
                }

                tableKeys.Add(entry.key.Trim());
            }

            Dictionary<string, ReferenceInfo> references = CollectReferences();

            foreach (KeyValuePair<string, ReferenceInfo> pair in references)
            {
                string key = pair.Key;
                ReferenceInfo info = pair.Value;
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                bool existsInTable = tableKeys.Contains(key);
                bool deprecated = IsDeprecatedKey(key);
                string sourceSummary = BuildSourceSummary(info.sources, 3);

                if (!existsInTable)
                {
                    gapTotal++;
                    rows.Add(new ValidationRow
                    {
                        layer = "Reference",
                        key = key,
                        status = "Gap",
                        refCount = info.count,
                        note = "Referenced key is missing from localization table.",
                        sources = sourceSummary
                    });
                    continue;
                }

                if (deprecated)
                {
                    gapTotal++;
                    rows.Add(new ValidationRow
                    {
                        layer = "Reference",
                        key = key,
                        status = "Gap",
                        refCount = info.count,
                        note = "Deprecated key is still referenced in runtime content.",
                        sources = sourceSummary
                    });
                    continue;
                }

                rows.Add(new ValidationRow
                {
                    layer = "Reference",
                    key = key,
                    status = "Ok",
                    refCount = info.count,
                    note = "referenced_and_resolved",
                    sources = sourceSummary
                });
            }

            foreach (string key in tableKeys)
            {
                bool referenced = references.ContainsKey(key);
                bool deprecated = IsDeprecatedKey(key);

                if (deprecated && !referenced)
                {
                    rows.Add(new ValidationRow
                    {
                        layer = "Table",
                        key = key,
                        status = "Ok",
                        refCount = 0,
                        note = "deprecated_key_unreferenced",
                        sources = string.Empty
                    });
                    continue;
                }

                if (!referenced)
                {
                    warningTotal++;
                    rows.Add(new ValidationRow
                    {
                        layer = "Table",
                        key = key,
                        status = "Warning",
                        refCount = 0,
                        note = "Table key appears unused (orphan candidate).",
                        sources = string.Empty
                    });
                }
            }

            rows.Add(new ValidationRow
            {
                layer = "Summary",
                key = string.Empty,
                status = gapTotal > 0 ? "Gap" : "Ok",
                refCount = references.Count,
                note = $"table_keys={tableKeys.Count}; referenced_keys={references.Count}; warnings={warningTotal}",
                sources = string.Empty
            });
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

                    string key = match.Groups["key"].Value;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    AddReference(references, key.Trim(), NormalizeAssetPath(path));
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

                        string key = match.Groups["key"].Value;
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            continue;
                        }

                        AddReference(references, key.Trim(), NormalizeAssetPath(path));
                    }
                }
            }
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

        private static bool IsDeprecatedKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return key.StartsWith("deprecated.", StringComparison.OrdinalIgnoreCase)
                || key.EndsWith(".deprecated", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildSourceSummary(HashSet<string> sources, int maxCount)
        {
            if (sources == null || sources.Count == 0)
            {
                return string.Empty;
            }

            int take = Mathf.Max(1, maxCount);
            var sorted = new List<string>(sources);
            sorted.Sort(StringComparer.OrdinalIgnoreCase);

            if (sorted.Count <= take)
            {
                return string.Join(" | ", sorted);
            }

            List<string> head = sorted.GetRange(0, take);
            return string.Join(" | ", head) + $" | +{sorted.Count - take} more";
        }

        private static string NormalizeAssetPath(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return string.Empty;
            }

            string normalized = absolutePath.Replace('\\', '/');
            string dataRoot = Application.dataPath.Replace('\\', '/');
            if (normalized.StartsWith(dataRoot, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets" + normalized.Substring(dataRoot.Length);
            }

            return normalized;
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var builder = new StringBuilder(8192);
            builder.AppendLine("layer,key,status,ref_count,note,sources");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                builder
                    .Append(Escape(row.layer)).Append(',')
                    .Append(Escape(row.key)).Append(',')
                    .Append(Escape(row.status)).Append(',')
                    .Append(row.refCount).Append(',')
                    .Append(Escape(row.note)).Append(',')
                    .Append(Escape(row.sources))
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
