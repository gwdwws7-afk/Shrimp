using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class SaveMigrationMatrixGateValidator
    {
        private const string ValidateMenuPath = "Tools/Productization/P2/Validate Save Migration Matrix (CSV)";
        private const string ValidateGateMenuPath = "Tools/Productization/P2/Validate Save Migration Matrix (CI Gate)";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/save_migration_matrix_gate_report.csv";
        private const string SaveManagerPath = "Assets/ThirdPersonController/Scripts/Core/SaveManager.cs";
        private const string LogPrefix = "[SaveMigrationMatrixGate]";

        private struct ValidationRow
        {
            public string checkId;
            public string status;
            public int sourceVersion;
            public int targetVersion;
            public string note;
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
            var rows = new List<ValidationRow>(16);
            int gapTotal = 0;

            EvaluateSaveManagerWiring(rows, ref gapTotal);
            EvaluateLegacyV1Minimal(rows, ref gapTotal);
            EvaluateLegacyV1InvalidLanguage(rows, ref gapTotal);
            EvaluateAlreadyLatest(rows, ref gapTotal);

            rows.Add(new ValidationRow
            {
                checkId = "summary",
                status = gapTotal > 0 ? "Gap" : "Ok",
                sourceVersion = 0,
                targetVersion = SaveDataMigrationUtility.LatestSchemaVersion,
                note = $"rows={rows.Count}; gaps={gapTotal}"
            });

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();

            string summary = $"rows={rows.Count} gap={gapTotal} report={reportPath}";
            Debug.Log($"{LogPrefix} {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Save Migration Matrix Gate", summary, "OK");
            }

            if (failOnBlocking && gapTotal > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. gap={gapTotal} report={reportPath}");
            }
        }

        private static void EvaluateSaveManagerWiring(List<ValidationRow> rows, ref int gapTotal)
        {
            string fullPath = Path.GetFullPath(SaveManagerPath);
            bool exists = File.Exists(fullPath);
            bool hasMigrationCall = false;

            if (exists)
            {
                string source = File.ReadAllText(fullPath);
                hasMigrationCall = source.IndexOf("SaveDataMigrationUtility.TryMigrate", StringComparison.Ordinal) >= 0;
            }

            bool ok = exists && hasMigrationCall;
            if (!ok)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "save_manager.migration_wiring",
                status = ok ? "Ok" : "Gap",
                sourceVersion = 1,
                targetVersion = SaveDataMigrationUtility.LatestSchemaVersion,
                note = ok
                    ? "SaveManager load path invokes migration utility."
                    : "SaveManager migration wiring missing."
            });
        }

        private static void EvaluateLegacyV1Minimal(List<ValidationRow> rows, ref int gapTotal)
        {
            GameData data = new GameData
            {
                saveSchemaVersion = 0,
                quickConsumableSlots = null,
                activeProgressionRoute = string.Empty,
                consumables = null,
                questStates = null
            };

            bool changed = SaveDataMigrationUtility.TryMigrate(data, out string summary);
            bool ok = changed
                && data.saveSchemaVersion == SaveDataMigrationUtility.LatestSchemaVersion
                && data.quickConsumableSlots != null
                && data.quickConsumableSlots.Count >= 3
                && !string.IsNullOrWhiteSpace(data.activeProgressionRoute);

            if (!ok)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "matrix.v1_minimal_to_latest",
                status = ok ? "Ok" : "Gap",
                sourceVersion = 1,
                targetVersion = SaveDataMigrationUtility.LatestSchemaVersion,
                note = summary
            });
        }

        private static void EvaluateLegacyV1InvalidLanguage(List<ValidationRow> rows, ref int gapTotal)
        {
            GameData data = new GameData
            {
                saveSchemaVersion = 1,
                localizationLanguage = 999
            };

            bool changed = SaveDataMigrationUtility.TryMigrate(data, out string summary);
            bool languageValid = Enum.IsDefined(typeof(LocalizationLanguage), data.localizationLanguage);
            bool ok = changed
                && data.saveSchemaVersion == SaveDataMigrationUtility.LatestSchemaVersion
                && languageValid;

            if (!ok)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "matrix.v1_invalid_language_to_latest",
                status = ok ? "Ok" : "Gap",
                sourceVersion = 1,
                targetVersion = SaveDataMigrationUtility.LatestSchemaVersion,
                note = summary
            });
        }

        private static void EvaluateAlreadyLatest(List<ValidationRow> rows, ref int gapTotal)
        {
            GameData data = new GameData
            {
                saveSchemaVersion = SaveDataMigrationUtility.LatestSchemaVersion,
                activeProgressionRoute = "Control",
                quickConsumableSlots = new List<string> { string.Empty, string.Empty, string.Empty }
            };

            bool changed = SaveDataMigrationUtility.TryMigrate(data, out string summary);
            bool ok = !changed && data.saveSchemaVersion == SaveDataMigrationUtility.LatestSchemaVersion;
            if (!ok)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "matrix.latest_noop",
                status = ok ? "Ok" : "Gap",
                sourceVersion = SaveDataMigrationUtility.LatestSchemaVersion,
                targetVersion = SaveDataMigrationUtility.LatestSchemaVersion,
                note = summary
            });
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var sb = new StringBuilder(1024);
            sb.AppendLine("check_id,status,source_version,target_version,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                sb.Append(Escape(row.checkId)).Append(',')
                    .Append(Escape(row.status)).Append(',')
                    .Append(row.sourceVersion).Append(',')
                    .Append(row.targetVersion).Append(',')
                    .Append(Escape(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(false));
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
