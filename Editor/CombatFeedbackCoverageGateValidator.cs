using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class CombatFeedbackCoverageGateValidator
    {
        private const string ValidateMenuPath = "Tools/Productization/P0/Validate Combat Feedback Coverage (CSV)";
        private const string ValidateGateMenuPath = "Tools/Productization/P0/Validate Combat Feedback Coverage (CI Gate)";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/combat_feedback_coverage_gate_report.csv";

        private const string GameEventsPath = "Assets/ThirdPersonController/Scripts/Core/GameEvents.cs";
        private const string AudioManagerPath = "Assets/ThirdPersonController/Scripts/Core/AudioManager.cs";
        private const string ScreenEffectManagerPath = "Assets/ThirdPersonController/Scripts/Core/ScreenEffectManager.cs";

        private struct ValidationRow
        {
            public string checkId;
            public string status;
            public string value;
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
            var rows = new List<ValidationRow>(32);
            int gapTotal = 0;

            string gameEventsContent = ReadAssetText(GameEventsPath, rows, ref gapTotal);
            string audioContent = ReadAssetText(AudioManagerPath, rows, ref gapTotal);
            string screenEffectContent = ReadAssetText(ScreenEffectManagerPath, rows, ref gapTotal);

            EvaluateTokenSet(
                rows,
                ref gapTotal,
                "events.required_declared",
                GameEventsPath,
                gameEventsContent,
                new[]
                {
                    "public static event Action<int, Vector3, EnemyHitReactionType> OnEnemyHit;",
                    "public static event Action<EnemyType, Vector3, int> OnEnemyKilled;",
                    "public static event Action<bool> OnBerserkStateChanged;",
                    "public static event Action OnBossBreakWindowStart;"
                },
                "Core combat feedback events are declared.",
                "Missing required GameEvents declarations");

            EvaluateTokenSet(
                rows,
                ref gapTotal,
                "audio.required_subscriptions",
                AudioManagerPath,
                audioContent,
                new[]
                {
                    "GameEvents.OnEnemyHit += HandleEnemyHit;",
                    "GameEvents.OnEnemyKilled += HandleEnemyKilled;",
                    "GameEvents.OnBerserkStateChanged += HandleBerserkStateChanged;",
                    "GameEvents.OnBossBreakWindowStart += HandleBossBreakWindowStart;",
                    "GameEvents.OnEnemyHit -= HandleEnemyHit;",
                    "GameEvents.OnEnemyKilled -= HandleEnemyKilled;",
                    "GameEvents.OnBerserkStateChanged -= HandleBerserkStateChanged;",
                    "GameEvents.OnBossBreakWindowStart -= HandleBossBreakWindowStart;"
                },
                "AudioManager subscribes/unsubscribes all required combat feedback events.",
                "Missing required AudioManager event bindings");

            EvaluateTokenSet(
                rows,
                ref gapTotal,
                "audio.required_handlers",
                AudioManagerPath,
                audioContent,
                new[]
                {
                    "private void HandleEnemyHit(",
                    "private void HandleEnemyKilled(",
                    "private void HandleBerserkStateChanged(",
                    "private void HandleBossBreakWindowStart(",
                    "public void PlayBossBreakWindowSound()"
                },
                "AudioManager has handlers for all required feedback events.",
                "Missing required AudioManager handlers");

            EvaluateTokenSet(
                rows,
                ref gapTotal,
                "audio.priority_path",
                AudioManagerPath,
                audioContent,
                new[]
                {
                    "prioritySfxSource",
                    "AudioEventPriority.High",
                    "PlayBossBreakWindowSound"
                },
                "Audio high-priority playback path is available for key events.",
                "Missing high-priority audio playback path tokens");

            EvaluateTokenSet(
                rows,
                ref gapTotal,
                "vfx.required_subscriptions",
                ScreenEffectManagerPath,
                screenEffectContent,
                new[]
                {
                    "GameEvents.OnPlayerDamaged += OnPlayerDamaged;",
                    "GameEvents.OnComboChanged += OnComboChanged;",
                    "GameEvents.OnBerserkStateChanged += OnBerserkStateChanged;",
                    "GameEvents.OnDamageDealt += OnDamageDealt;",
                    "GameEvents.OnEnemyHit += OnEnemyHit;",
                    "GameEvents.OnPlayerDamaged -= OnPlayerDamaged;",
                    "GameEvents.OnComboChanged -= OnComboChanged;",
                    "GameEvents.OnBerserkStateChanged -= OnBerserkStateChanged;",
                    "GameEvents.OnDamageDealt -= OnDamageDealt;",
                    "GameEvents.OnEnemyHit -= OnEnemyHit;"
                },
                "ScreenEffectManager subscribes/unsubscribes all required visual feedback events.",
                "Missing required ScreenEffectManager event bindings");

            EvaluateTokenSet(
                rows,
                ref gapTotal,
                "vfx.required_handlers",
                ScreenEffectManagerPath,
                screenEffectContent,
                new[]
                {
                    "private void OnPlayerDamaged(",
                    "private void OnComboChanged(",
                    "private void OnBerserkStateChanged(",
                    "private void OnDamageDealt(",
                    "private void OnEnemyHit("
                },
                "ScreenEffectManager has handlers for required visual feedback events.",
                "Missing required ScreenEffectManager handlers");

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();

            string summary = $"rows={rows.Count} gap={gapTotal} report={reportPath}";
            Debug.Log($"[CombatFeedbackCoverageGate] {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Combat Feedback Coverage Gate", summary, "OK");
            }

            if (failOnBlocking && gapTotal > 0)
            {
                throw new InvalidOperationException($"[CombatFeedbackCoverageGate] gate failed. gap={gapTotal} report={reportPath}");
            }
        }

        private static string ReadAssetText(string assetPath, List<ValidationRow> rows, ref int gapTotal)
        {
            string fullPath = Path.GetFullPath(assetPath);
            bool exists = File.Exists(fullPath);
            if (!exists)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = $"file.exists.{Path.GetFileName(assetPath)}",
                status = exists ? "Ok" : "Gap",
                value = assetPath,
                note = exists ? "source_found" : "source_missing"
            });

            if (!exists)
            {
                return string.Empty;
            }

            return File.ReadAllText(fullPath);
        }

        private static void EvaluateTokenSet(
            List<ValidationRow> rows,
            ref int gapTotal,
            string checkId,
            string sourcePath,
            string content,
            string[] requiredTokens,
            string okNote,
            string gapPrefix)
        {
            var missing = new List<string>();
            if (string.IsNullOrEmpty(content))
            {
                missing.Add("<empty_source>");
            }
            else
            {
                for (int i = 0; i < requiredTokens.Length; i++)
                {
                    string token = requiredTokens[i];
                    if (string.IsNullOrEmpty(token))
                    {
                        continue;
                    }

                    if (content.IndexOf(token, StringComparison.Ordinal) < 0)
                    {
                        missing.Add(token);
                    }
                }
            }

            bool ok = missing.Count == 0;
            if (!ok)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = checkId,
                status = ok ? "Ok" : "Gap",
                value = sourcePath,
                note = ok ? okNote : $"{gapPrefix}: {string.Join(" | ", missing)}"
            });
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var builder = new StringBuilder(2048);
            builder.AppendLine("check_id,status,value,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                builder
                    .Append(Escape(row.checkId)).Append(',')
                    .Append(Escape(row.status)).Append(',')
                    .Append(Escape(row.value)).Append(',')
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
