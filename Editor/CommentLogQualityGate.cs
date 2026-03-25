using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class CommentLogQualityGate
    {
        private const string ValidateMenuPath = "Tools/Quality/P0/Validate Comment Log Quality";
        private const string ValidateGateMenuPath = "Tools/Quality/P0/Validate Comment Log Quality (CI Gate)";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/comment_log_quality_gate_report.csv";
        private const string SummaryMdPath = "Assets/ThirdPersonController/Reports/comment_log_quality_gate_summary.md";

        private static readonly string[] ScanRoots =
        {
            "Assets/ThirdPersonController/Scripts",
            "Assets/ThirdPersonController/Editor",
            "Assets/Editor",
            "Assets/GameDesign/Scripts"
        };

        private static readonly string[] PlaceholderTokens =
        {
            "TODO",
            "TBD",
            "FIXME",
            "XXX",
            "placeholder",
            "\u5360\u4F4D",
            "\u5F85\u5B9E\u73B0",
            "\u4E34\u65F6",
            "\u8C03\u8BD5\u4FE1\u606F",
            "stub",
            "mock"
        };

        private struct IssueRow
        {
            public string file;
            public int line;
            public string severity;
            public string category;
            public string status;
            public string message;
            public string snippet;
        }

        [MenuItem(ValidateMenuPath)]
        public static void ValidateFromMenu()
        {
            Run(logToConsole: true, failOnError: false);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(logToConsole: true, failOnError: true);
        }

        public static void ValidateForBatch()
        {
            Run(logToConsole: true, failOnError: true);
        }

        private static void Run(bool logToConsole, bool failOnError)
        {
            List<IssueRow> rows = ScanScripts(out int warningCount, out int errorCount, out int scannedFileCount);
            string csvPath = WriteCsv(rows);
            string summaryPath = WriteSummary(rows, scannedFileCount, warningCount, errorCount);
            AssetDatabase.Refresh();

            if (logToConsole)
            {
                Debug.Log(
                    $"[CommentLogQualityGate] files={scannedFileCount} warnings={warningCount} errors={errorCount} csv={csvPath} summary={summaryPath}");
            }

            if (failOnError && errorCount > 0)
            {
                throw new InvalidOperationException(
                    $"[CommentLogQualityGate] blocking corruption detected: errors={errorCount}. See {ReportCsvPath}");
            }
        }

        private static List<IssueRow> ScanScripts(out int warningCount, out int errorCount, out int scannedFileCount)
        {
            var rows = new List<IssueRow>();
            warningCount = 0;
            errorCount = 0;
            scannedFileCount = 0;

            List<string> paths = CollectScriptPaths();
            for (int i = 0; i < paths.Count; i++)
            {
                string assetPath = paths[i];
                string absolutePath = ToAbsolutePath(assetPath);
                if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                {
                    continue;
                }

                scannedFileCount++;
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(absolutePath);
                }
                catch (Exception ex)
                {
                    rows.Add(new IssueRow
                    {
                        file = assetPath,
                        line = 0,
                        severity = "Error",
                        category = "ReadFailure",
                        status = "Error",
                        message = $"Failed to read file: {ex.Message}",
                        snippet = string.Empty
                    });
                    errorCount++;
                    continue;
                }

                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    string line = lines[lineIndex] ?? string.Empty;
                    if (!IsCommentOrLogLine(line))
                    {
                        continue;
                    }

                    if (line.IndexOf('\uFFFD') >= 0)
                    {
                        rows.Add(new IssueRow
                        {
                            file = assetPath,
                            line = lineIndex + 1,
                            severity = "Error",
                            category = "Mojibake",
                            status = "Error",
                            message = "Replacement character found in comment/log line.",
                            snippet = line.Trim()
                        });
                        errorCount++;
                        continue;
                    }

                    if (ContainsMojibakeLikeToken(line))
                    {
                        rows.Add(new IssueRow
                        {
                            file = assetPath,
                            line = lineIndex + 1,
                            severity = "Warning",
                            category = "MojibakeSuspect",
                            status = "Warning",
                            message = "Suspicious mojibake signature found in comment/log line.",
                            snippet = line.Trim()
                        });
                        warningCount++;
                    }

                    if (ContainsToken(line, PlaceholderTokens))
                    {
                        rows.Add(new IssueRow
                        {
                            file = assetPath,
                            line = lineIndex + 1,
                            severity = "Warning",
                            category = "Placeholder",
                            status = "Warning",
                            message = "Placeholder or debug marker found in comment/log line.",
                            snippet = line.Trim()
                        });
                        warningCount++;
                    }
                }
            }

            if (rows.Count == 0)
            {
                rows.Add(new IssueRow
                {
                    file = string.Empty,
                    line = 0,
                    severity = "Info",
                    category = "None",
                    status = "Ok",
                    message = "No issues found.",
                    snippet = string.Empty
                });
            }

            return rows;
        }

        private static List<string> CollectScriptPaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ScanRoots.Length; i++)
            {
                string root = ScanRoots[i];
                if (string.IsNullOrEmpty(root) || !AssetDatabase.IsValidFolder(root))
                {
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:Script", new[] { root });
                for (int g = 0; g < guids.Length; g++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                    if (string.IsNullOrEmpty(path) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    paths.Add(path);
                }
            }

            var sorted = new List<string>(paths);
            sorted.Sort(StringComparer.OrdinalIgnoreCase);
            return sorted;
        }

        private static bool IsCommentOrLogLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal)
                || trimmed.StartsWith("/*", StringComparison.Ordinal)
                || trimmed.StartsWith("*", StringComparison.Ordinal)
                || trimmed.StartsWith("///", StringComparison.Ordinal))
            {
                return true;
            }

            return line.IndexOf("Debug.Log", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsToken(string line, string[] tokens)
        {
            if (string.IsNullOrEmpty(line) || tokens == null || tokens.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (line.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsMojibakeLikeToken(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            if (line.IndexOf('\u951F') >= 0)
            {
                return true;
            }

            return line.Contains("\u59AF\u2033\u6F61")
                   || line.Contains("\u7487\u8D1F\u8B97")
                   || line.Contains("\u7F01\u7199\u7AF4");
        }

        private static string WriteCsv(List<IssueRow> rows)
        {
            string absolutePath = ToAbsolutePath(ReportCsvPath);
            EnsureParentDirectory(absolutePath);

            var builder = new StringBuilder();
            builder.AppendLine("file,line,severity,category,status,message,snippet");
            for (int i = 0; i < rows.Count; i++)
            {
                IssueRow row = rows[i];
                builder.Append(EscapeCsv(row.file)).Append(',')
                    .Append(row.line).Append(',')
                    .Append(EscapeCsv(row.severity)).Append(',')
                    .Append(EscapeCsv(row.category)).Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(EscapeCsv(row.message)).Append(',')
                    .Append(EscapeCsv(row.snippet)).AppendLine();
            }

            File.WriteAllText(absolutePath, builder.ToString(), Encoding.UTF8);
            return ReportCsvPath;
        }

        private static string WriteSummary(List<IssueRow> rows, int scannedFileCount, int warningCount, int errorCount)
        {
            string absolutePath = ToAbsolutePath(SummaryMdPath);
            EnsureParentDirectory(absolutePath);

            var builder = new StringBuilder();
            builder.AppendLine("# Comment Log Quality Gate Summary");
            builder.AppendLine();
            builder.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            builder.AppendLine($"- Scanned Files: {scannedFileCount}");
            builder.AppendLine($"- Warnings: {warningCount}");
            builder.AppendLine($"- Errors: {errorCount}");
            builder.AppendLine($"- CSV: {ReportCsvPath}");
            builder.AppendLine();
            builder.AppendLine("| file | line | severity | category | message |");
            builder.AppendLine("|---|---:|---|---|---|");

            int maxRows = Mathf.Min(rows.Count, 80);
            for (int i = 0; i < maxRows; i++)
            {
                IssueRow row = rows[i];
                builder.Append('|')
                    .Append(row.file).Append('|')
                    .Append(row.line).Append('|')
                    .Append(row.severity).Append('|')
                    .Append(row.category).Append('|')
                    .Append(row.message.Replace("|", "\\|")).Append('|')
                    .AppendLine();
            }

            File.WriteAllText(absolutePath, builder.ToString(), Encoding.UTF8);
            return SummaryMdPath;
        }

        private static string ToAbsolutePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return string.Empty;
            }

            string projectRoot = Directory.GetCurrentDirectory();
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void EnsureParentDirectory(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return;
            }

            string dir = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string escaped = value.Replace("\"", "\"\"");
            if (escaped.IndexOf(',') >= 0 || escaped.IndexOf('\n') >= 0 || escaped.IndexOf('\r') >= 0)
            {
                return $"\"{escaped}\"";
            }

            return escaped;
        }
    }
}
