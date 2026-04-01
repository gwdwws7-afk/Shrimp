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
        private const string CombatFeedbackRoutingPath = "Assets/ThirdPersonController/Scripts/Core/CombatFeedbackRouting.cs";

        private struct ValidationRow
        {
            public string checkId;
            public string status;
            public string value;
            public string note;
        }

        private struct FeedbackMappingDefinition
        {
            public string id;
            public string description;
            public string eventToken;
            public string audioToken;
            public string vfxToken;
        }

        private static readonly FeedbackMappingDefinition[] RequiredMappings =
        {
            new FeedbackMappingDefinition
            {
                id = "enemy_hit_flinch",
                description = "Enemy flinch hit should route to base hit feedback.",
                eventToken = "OnEnemyHit",
                audioToken = "CombatFeedbackEventId.EnemyHitFlinch",
                vfxToken = "CombatFeedbackEventId.EnemyHitFlinch"
            },
            new FeedbackMappingDefinition
            {
                id = "enemy_hit_knockback",
                description = "Enemy knockback hit should route to heavy-hit feedback.",
                eventToken = "OnEnemyHit",
                audioToken = "CombatFeedbackEventId.EnemyHitKnockback",
                vfxToken = "CombatFeedbackEventId.EnemyHitKnockback"
            },
            new FeedbackMappingDefinition
            {
                id = "enemy_hit_knockdown",
                description = "Enemy knockdown hit should route to high-priority audio and screen shake.",
                eventToken = "OnEnemyHit",
                audioToken = "CombatFeedbackEventId.EnemyHitKnockdown",
                vfxToken = "CombatFeedbackEventId.EnemyHitKnockdown"
            },
            new FeedbackMappingDefinition
            {
                id = "enemy_killed",
                description = "Enemy kill should route to death audio feedback path.",
                eventToken = "OnEnemyKilled",
                audioToken = "CombatFeedbackEventId.EnemyKilled",
                vfxToken = "CombatFeedbackEventId.EnemyKilled"
            },
            new FeedbackMappingDefinition
            {
                id = "berserk_state_changed",
                description = "Berserk state should route to audio cue + berserk screen treatment.",
                eventToken = "OnBerserkStateChanged",
                audioToken = "CombatFeedbackEventId.BerserkStart",
                vfxToken = "CombatFeedbackEventId.BerserkStart"
            },
            new FeedbackMappingDefinition
            {
                id = "boss_break_window",
                description = "Boss break window should route to dedicated high-priority audio cue.",
                eventToken = "OnBossBreakWindowStart",
                audioToken = "CombatFeedbackEventId.BossBreakWindowStart",
                vfxToken = "CombatFeedbackEventId.BossBreakWindowStart"
            },
            new FeedbackMappingDefinition
            {
                id = "skill_used",
                description = "Skill cast should route into unified combat feedback bus.",
                eventToken = "OnSkillUsed",
                audioToken = "CombatFeedbackEventId.SkillUsed",
                vfxToken = "CombatFeedbackEventId.SkillUsed"
            },
            new FeedbackMappingDefinition
            {
                id = "stamina_depleted",
                description = "Stamina depleted should route into pressure feedback path.",
                eventToken = "OnStaminaDepleted",
                audioToken = "CombatFeedbackEventId.StaminaDepleted",
                vfxToken = "CombatFeedbackEventId.StaminaDepleted"
            }
        };

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
            string routingContent = ReadAssetText(CombatFeedbackRoutingPath, rows, ref gapTotal);

            EvaluateTokenSet(
                rows,
                ref gapTotal,
                "routing.model_declared",
                CombatFeedbackRoutingPath,
                routingContent,
                new[]
                {
                    "public enum CombatFeedbackEventId",
                    "public class CombatFeedbackAudioRoute",
                    "public class CombatFeedbackVfxRoute",
                    "EnemyHitFlinch",
                    "EnemyHitKnockback",
                    "EnemyHitKnockdown",
                    "EnemyKilled",
                    "BerserkStart",
                    "BossBreakWindowStart",
                    "SkillUsed",
                    "StaminaDepleted"
                },
                "Combat feedback routing model declares all required event ids and route payloads.",
                "Missing routing model tokens");

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
                    "public static event Action OnBossBreakWindowStart;",
                    "public static event Action<string, float> OnSkillUsed;",
                    "public static event Action OnStaminaDepleted;"
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
                    "GameEvents.OnSkillUsed += HandleSkillUsed;",
                    "GameEvents.OnStaminaDepleted += HandleStaminaDepleted;",
                    "GameEvents.OnEnemyHit -= HandleEnemyHit;",
                    "GameEvents.OnEnemyKilled -= HandleEnemyKilled;",
                    "GameEvents.OnBerserkStateChanged -= HandleBerserkStateChanged;",
                    "GameEvents.OnBossBreakWindowStart -= HandleBossBreakWindowStart;",
                    "GameEvents.OnSkillUsed -= HandleSkillUsed;",
                    "GameEvents.OnStaminaDepleted -= HandleStaminaDepleted;"
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
                    "private void HandleSkillUsed(",
                    "private void HandleStaminaDepleted(",
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
                "audio.routing_bootstrap",
                AudioManagerPath,
                audioContent,
                new[]
                {
                    "EnsureAudioRoute(CombatFeedbackEventId.EnemyHitFlinch",
                    "EnsureAudioRoute(CombatFeedbackEventId.EnemyHitKnockback",
                    "EnsureAudioRoute(CombatFeedbackEventId.EnemyHitKnockdown",
                    "EnsureAudioRoute(CombatFeedbackEventId.EnemyKilled",
                    "EnsureAudioRoute(CombatFeedbackEventId.BerserkStart",
                    "EnsureAudioRoute(CombatFeedbackEventId.BossBreakWindowStart",
                    "EnsureAudioRoute(CombatFeedbackEventId.SkillUsed",
                    "EnsureAudioRoute(CombatFeedbackEventId.StaminaDepleted",
                    "TryPlayMappedRoute("
                },
                "Audio routing table covers all required combat feedback events.",
                "Missing audio routing bootstrap tokens");

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
                    "GameEvents.OnEnemyKilled += OnEnemyKilled;",
                    "GameEvents.OnBossBreakWindowStart += OnBossBreakWindowStart;",
                    "GameEvents.OnSkillUsed += OnSkillUsed;",
                    "GameEvents.OnStaminaDepleted += OnStaminaDepleted;",
                    "GameEvents.OnPlayerDamaged -= OnPlayerDamaged;",
                    "GameEvents.OnComboChanged -= OnComboChanged;",
                    "GameEvents.OnBerserkStateChanged -= OnBerserkStateChanged;",
                    "GameEvents.OnDamageDealt -= OnDamageDealt;",
                    "GameEvents.OnEnemyHit -= OnEnemyHit;",
                    "GameEvents.OnEnemyKilled -= OnEnemyKilled;",
                    "GameEvents.OnBossBreakWindowStart -= OnBossBreakWindowStart;",
                    "GameEvents.OnSkillUsed -= OnSkillUsed;",
                    "GameEvents.OnStaminaDepleted -= OnStaminaDepleted;"
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
                    "private void OnEnemyHit(",
                    "private void OnEnemyKilled(",
                    "private void OnBossBreakWindowStart(",
                    "private void OnSkillUsed(",
                    "private void OnStaminaDepleted("
                },
                "ScreenEffectManager has handlers for required visual feedback events.",
                "Missing required ScreenEffectManager handlers");

            EvaluateTokenSet(
                rows,
                ref gapTotal,
                "vfx.routing_bootstrap",
                ScreenEffectManagerPath,
                screenEffectContent,
                new[]
                {
                    "EnsureVfxRoute(CombatFeedbackEventId.EnemyHitFlinch",
                    "EnsureVfxRoute(CombatFeedbackEventId.EnemyHitKnockback",
                    "EnsureVfxRoute(CombatFeedbackEventId.EnemyHitKnockdown",
                    "EnsureVfxRoute(CombatFeedbackEventId.EnemyKilled",
                    "EnsureVfxRoute(CombatFeedbackEventId.BerserkStart",
                    "EnsureVfxRoute(CombatFeedbackEventId.BossBreakWindowStart",
                    "EnsureVfxRoute(CombatFeedbackEventId.SkillUsed",
                    "EnsureVfxRoute(CombatFeedbackEventId.StaminaDepleted",
                    "TryApplyMappedVfx("
                },
                "VFX routing table covers all required combat feedback events.",
                "Missing vfx routing bootstrap tokens");

            EvaluateMappings(rows, ref gapTotal, gameEventsContent, audioContent, screenEffectContent);

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

        private static void EvaluateMappings(
            List<ValidationRow> rows,
            ref int gapTotal,
            string gameEventsContent,
            string audioContent,
            string screenEffectContent)
        {
            for (int i = 0; i < RequiredMappings.Length; i++)
            {
                FeedbackMappingDefinition mapping = RequiredMappings[i];
                var missing = new List<string>(4);

                if (!ContainsToken(gameEventsContent, mapping.eventToken))
                {
                    missing.Add($"event:{mapping.eventToken}");
                }

                if (!ContainsToken(audioContent, mapping.audioToken))
                {
                    missing.Add($"audio:{mapping.audioToken}");
                }

                if (!ContainsToken(screenEffectContent, mapping.vfxToken))
                {
                    missing.Add($"vfx:{mapping.vfxToken}");
                }

                bool ok = missing.Count == 0;
                if (!ok)
                {
                    gapTotal++;
                }

                rows.Add(new ValidationRow
                {
                    checkId = $"mapping.{mapping.id}",
                    status = ok ? "Ok" : "Gap",
                    value = mapping.description,
                    note = ok ? "mapping_ok" : $"Missing tokens: {string.Join(" | ", missing)}"
                });
            }
        }

        private static bool ContainsToken(string content, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return true;
            }

            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            return content.IndexOf(token, StringComparison.Ordinal) >= 0;
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
