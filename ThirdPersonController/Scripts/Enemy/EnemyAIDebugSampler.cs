using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// Runtime sampler for EnemyAI behavior distribution and token pressure.
    /// Attach to a scene object during AI tuning sessions.
    /// </summary>
    public class EnemyAIDebugSampler : MonoBehaviour
    {
        [Header("Sampling")]
        public float sampleInterval = 0.25f;
        public float reportInterval = 5f;
        public bool resetAfterReport = true;

        [Header("Output")]
        public bool logToConsole = true;
        [TextArea(8, 20)]
        public string lastReport = "";

        [Header("Coordinator")]
        public bool autoFindCoordinator = true;
        public EnemyCrowdCoordinator crowdCoordinator;

        private float nextSampleTime = 0f;
        private float nextReportTime = 0f;

        private readonly Dictionary<string, ArchetypeAggregate> aggregates = new Dictionary<string, ArchetypeAggregate>();

        private class ArchetypeAggregate
        {
            public int sampleFrames = 0;
            public int enemySamples = 0;
            public int decisions = 0;
            public int hitCount = 0;
            public int tokenSuccess = 0;
            public int tokenFail = 0;
            public readonly Dictionary<string, int> stateCounts = new Dictionary<string, int>();
            public readonly Dictionary<string, int> lodCounts = new Dictionary<string, int>();
        }

        private void OnEnable()
        {
            nextSampleTime = Time.time;
            nextReportTime = Time.time + Mathf.Max(1f, reportInterval);
            if (autoFindCoordinator && crowdCoordinator == null)
            {
                crowdCoordinator = FindObjectOfType<EnemyCrowdCoordinator>();
            }
        }

        private void Update()
        {
            if (Time.time >= nextSampleTime)
            {
                SampleNow();
                nextSampleTime = Time.time + Mathf.Max(0.05f, sampleInterval);
            }

            if (Time.time >= nextReportTime)
            {
                BuildReport();
                if (logToConsole && !string.IsNullOrEmpty(lastReport))
                {
                    Debug.Log(lastReport);
                }

                if (resetAfterReport)
                {
                    aggregates.Clear();
                }

                nextReportTime = Time.time + Mathf.Max(1f, reportInterval);
            }
        }

        [ContextMenu("Sample Now")]
        public void SampleNow()
        {
            EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyAI enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled)
                {
                    continue;
                }

                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health != null && health.IsDead)
                {
                    continue;
                }

                string key = ResolveArchetypeKey(enemy);
                if (!aggregates.TryGetValue(key, out ArchetypeAggregate aggregate))
                {
                    aggregate = new ArchetypeAggregate();
                    aggregates[key] = aggregate;
                }

                EnemyAI.EnemyAIDebugSnapshot snapshot = enemy.GetDebugSnapshot();
                aggregate.sampleFrames++;
                aggregate.enemySamples++;
                aggregate.decisions += snapshot.decisionCount;
                aggregate.hitCount += snapshot.hitsAppliedCount;
                aggregate.tokenSuccess += snapshot.tokenAcquireSuccessCount;
                aggregate.tokenFail += snapshot.tokenAcquireFailCount;

                string state = string.IsNullOrEmpty(snapshot.state) ? "Unknown" : snapshot.state;
                if (!aggregate.stateCounts.ContainsKey(state))
                {
                    aggregate.stateCounts[state] = 0;
                }
                aggregate.stateCounts[state]++;

                string lod = string.IsNullOrEmpty(snapshot.updateLod) ? "Full" : snapshot.updateLod;
                if (!aggregate.lodCounts.ContainsKey(lod))
                {
                    aggregate.lodCounts[lod] = 0;
                }
                aggregate.lodCounts[lod]++;
            }
        }

        [ContextMenu("Build Report")]
        public void BuildReport()
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.AppendLine("[EnemyAI P1 Sampler]");
            sb.AppendLine($"time={System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"archetypes={aggregates.Count}");

            if (crowdCoordinator == null && autoFindCoordinator)
            {
                crowdCoordinator = FindObjectOfType<EnemyCrowdCoordinator>();
            }

            if (crowdCoordinator != null)
            {
                sb.AppendLine(
                    $"token active={crowdCoordinator.ActiveAttackersCount}/{crowdCoordinator.EffectiveMaxAttackers}, util={crowdCoordinator.TokenUtilization:F2}, req/grant/reject/release={crowdCoordinator.TokenRequestCount}/{crowdCoordinator.TokenGrantedCount}/{crowdCoordinator.TokenRejectedCount}/{crowdCoordinator.TokenReleaseCount}");
            }

            foreach (KeyValuePair<string, ArchetypeAggregate> pair in aggregates)
            {
                ArchetypeAggregate agg = pair.Value;
                int totalStates = 0;
                string dominantState = "N/A";
                int dominantCount = -1;
                foreach (KeyValuePair<string, int> kv in agg.stateCounts)
                {
                    totalStates += kv.Value;
                    if (kv.Value > dominantCount)
                    {
                        dominantCount = kv.Value;
                        dominantState = kv.Key;
                    }
                }

                float dominantRatio = totalStates > 0 ? (float)dominantCount / totalStates : 0f;
                string lodSummary = BuildLodSummary(agg.lodCounts);
                sb.AppendLine(
                    $"{pair.Key}: samples={agg.enemySamples}, dominant={dominantState}({dominantRatio:P0}), lod={lodSummary}, decisions={agg.decisions}, hits={agg.hitCount}, tokenOk/Fail={agg.tokenSuccess}/{agg.tokenFail}");
            }

            lastReport = sb.ToString();
        }

        private static string BuildLodSummary(Dictionary<string, int> lodCounts)
        {
            if (lodCounts == null || lodCounts.Count == 0)
            {
                return "n/a";
            }

            int total = 0;
            foreach (KeyValuePair<string, int> kv in lodCounts)
            {
                total += kv.Value;
            }

            if (total <= 0)
            {
                return "n/a";
            }

            StringBuilder sb = new StringBuilder(48);
            AppendLodRatio(sb, lodCounts, "Full", total);
            AppendLodRatio(sb, lodCounts, "Simplified", total);
            AppendLodRatio(sb, lodCounts, "Minimal", total);
            return sb.ToString();
        }

        private static void AppendLodRatio(StringBuilder sb, Dictionary<string, int> lodCounts, string key, int total)
        {
            if (!lodCounts.TryGetValue(key, out int count))
            {
                return;
            }

            if (sb.Length > 0)
            {
                sb.Append(',');
            }

            float ratio = (float)count / total;
            sb.Append(key);
            sb.Append(':');
            sb.Append(ratio.ToString("P0"));
        }

        private static string ResolveArchetypeKey(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return "unknown";
            }

            EnemyArchetypeConfigurator configurator = enemy.GetComponent<EnemyArchetypeConfigurator>();
            if (configurator != null && configurator.archetype != null)
            {
                string id = configurator.archetype.archetypeId;
                if (!string.IsNullOrEmpty(id))
                {
                    return id.Trim().ToLowerInvariant();
                }
            }

            return "unknown";
        }
    }
}
