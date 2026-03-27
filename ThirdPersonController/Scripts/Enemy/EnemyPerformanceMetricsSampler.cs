using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// Runtime performance sampler for enemy stress scenarios.
    /// Captures frame-time distribution, GC allocation pressure and AI decision throughput.
    /// </summary>
    public class EnemyPerformanceMetricsSampler : MonoBehaviour
    {
        [Header("Sampling")]
        public float aiSampleInterval = 0.25f;
        public int maxFrameSamples = 30000;
        public int maxAiSamples = 4000;
        public bool preferMainThreadManagedAllocCounter = true;

        [Header("Output")]
        public bool writeToAssetsReports = true;
        public string outputFileName = "enemy_ai_p3_stress_metrics.csv";
        public bool logStepSummary = true;

        [Header("Debug (Runtime)")]
        [SerializeField] private bool isRecording = false;
        [SerializeField] private string currentStepLabel = "";
        [SerializeField] private int currentTargetCount = 0;
        [SerializeField] private string lastOutputPath = "";
        [TextArea(6, 16)]
        public string lastSummary = "";
        public bool IsRecording => isRecording;
        public int RecordedStepCount => recordedSteps.Count;
        public string LastOutputPath => lastOutputPath;

        private readonly List<float> frameTimeSamplesMs = new List<float>(4096);
        private readonly List<float> aiDecisionRateSamples = new List<float>(512);
        private readonly List<int> aiActiveEnemySamples = new List<int>(512);
        private readonly List<int> projectileActiveSamples = new List<int>(512);
        private readonly List<int> uiDamageTextActiveSamples = new List<int>(512);
        private readonly List<int> activeParticleSamples = new List<int>(512);
        private readonly List<long> gcAllocSamplesBytes = new List<long>(4096);
        private readonly List<StepRecord> recordedSteps = new List<StepRecord>(16);
        private readonly Dictionary<int, int> previousDecisionCounts = new Dictionary<int, int>(512);
        private readonly Dictionary<int, int> latestDecisionCounts = new Dictionary<int, int>(512);

        private float stepStartTime = 0f;
        private float nextAiSampleTime = 0f;
        private float lastAiSampleTime = 0f;
        private long lastGcTotalMemory = 0;
        private int gcCollectionStartGen0 = 0;
        private int gcCollectionStartGen1 = 0;
        private int gcCollectionStartGen2 = 0;
        private long lastThreadAllocatedBytes = 0L;
        private bool threadAllocCounterAvailable = false;

        private ProfilerRecorder gcAllocatedInFrameRecorder;
        private bool gcRecorderReady = false;

        [Serializable]
        private struct StepRecord
        {
            public string label;
            public int targetCount;
            public float durationSeconds;
            public int frameSamples;
            public float avgFrameMs;
            public float p95FrameMs;
            public float p99FrameMs;
            public float maxFrameMs;
            public float avgFps;
            public float p1Fps;
            public long avgGcAllocBytesPerFrame;
            public long p95GcAllocBytesPerFrame;
            public long maxGcAllocBytesPerFrame;
            public long totalGcAllocBytes;
            public int gcCollectionsGen0;
            public int gcCollectionsGen1;
            public int gcCollectionsGen2;
            public float avgAiDecisionsPerSecond;
            public float p95AiDecisionsPerSecond;
            public float peakAiDecisionsPerSecond;
            public float avgActiveEnemies;
            public float avgActiveProjectiles;
            public int p95ActiveProjectiles;
            public float avgActiveDamageTexts;
            public int p95ActiveDamageTexts;
            public float avgActiveParticles;
            public int p95ActiveParticles;
        }

        private void OnEnable()
        {
            StartGcRecorder();
            ProbeThreadAllocCounter();
        }

        private void OnDisable()
        {
            StopRecordingIfNeeded();
            DisposeGcRecorder();
        }

        private void Update()
        {
            if (!isRecording)
            {
                return;
            }

            RecordFrameSample();
            if (Time.unscaledTime >= nextAiSampleTime)
            {
                RecordAiSample();
                nextAiSampleTime = Time.unscaledTime + Mathf.Max(0.05f, aiSampleInterval);
            }
        }

        public void BeginStep(string stepLabel, int targetEnemyCount)
        {
            if (isRecording)
            {
                EndStep();
            }

            currentStepLabel = string.IsNullOrEmpty(stepLabel) ? "UnnamedStep" : stepLabel;
            currentTargetCount = Mathf.Max(0, targetEnemyCount);
            isRecording = true;

            frameTimeSamplesMs.Clear();
            aiDecisionRateSamples.Clear();
            aiActiveEnemySamples.Clear();
            projectileActiveSamples.Clear();
            uiDamageTextActiveSamples.Clear();
            activeParticleSamples.Clear();
            gcAllocSamplesBytes.Clear();
            previousDecisionCounts.Clear();

            stepStartTime = Time.unscaledTime;
            nextAiSampleTime = stepStartTime;
            lastAiSampleTime = stepStartTime;
            lastGcTotalMemory = GC.GetTotalMemory(false);

            gcCollectionStartGen0 = GC.CollectionCount(0);
            gcCollectionStartGen1 = GC.CollectionCount(1);
            gcCollectionStartGen2 = GC.CollectionCount(2);
            if (threadAllocCounterAvailable)
            {
                lastThreadAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
            }

            PrimeAiDecisionBaseline();
        }

        public void EndStep()
        {
            if (!isRecording)
            {
                return;
            }

            isRecording = false;
            StepRecord record = BuildStepRecord();
            recordedSteps.Add(record);
            lastSummary = BuildRecordSummary(record);

            if (logStepSummary)
            {
                Debug.Log(lastSummary);
            }
        }

        [ContextMenu("Export CSV")]
        public string ExportCsv()
        {
            string csv = BuildCsv();
            string outputPath = ResolveOutputPath();
            if (string.IsNullOrEmpty(outputPath))
            {
                return string.Empty;
            }

            try
            {
                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(outputPath, csv, Encoding.UTF8);
                lastOutputPath = outputPath;
                Debug.Log($"[EnemyPerf] CSV exported: {outputPath}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[EnemyPerf] Failed to write CSV: {exception.Message}");
            }

            return outputPath;
        }

        [ContextMenu("Clear Recorded Steps")]
        public void ClearRecordedSteps()
        {
            recordedSteps.Clear();
            lastSummary = "";
        }

        private void StopRecordingIfNeeded()
        {
            if (!isRecording)
            {
                return;
            }

            EndStep();
        }

        private void StartGcRecorder()
        {
            DisposeGcRecorder();

            try
            {
                gcAllocatedInFrameRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
                gcRecorderReady = gcAllocatedInFrameRecorder.Valid;
            }
            catch
            {
                gcRecorderReady = false;
            }
        }

        private void DisposeGcRecorder()
        {
            if (gcAllocatedInFrameRecorder.Valid)
            {
                gcAllocatedInFrameRecorder.Dispose();
            }

            gcRecorderReady = false;
        }

        private void RecordFrameSample()
        {
            if (frameTimeSamplesMs.Count < maxFrameSamples)
            {
                frameTimeSamplesMs.Add(Time.unscaledDeltaTime * 1000f);
            }

            if (gcAllocSamplesBytes.Count >= maxFrameSamples)
            {
                return;
            }

            long gcAllocThisFrame = GetGcAllocInFrameBytes();
            gcAllocSamplesBytes.Add(Math.Max(0L, gcAllocThisFrame));
        }

        private long GetGcAllocInFrameBytes()
        {
            if (preferMainThreadManagedAllocCounter && threadAllocCounterAvailable)
            {
                long current = GC.GetAllocatedBytesForCurrentThread();
                long delta = current - lastThreadAllocatedBytes;
                lastThreadAllocatedBytes = current;
                return delta > 0 ? delta : 0;
            }

            if (gcRecorderReady && gcAllocatedInFrameRecorder.Valid)
            {
                return gcAllocatedInFrameRecorder.LastValue;
            }

            long totalMemory = GC.GetTotalMemory(false);
            long memoryDelta = totalMemory - lastGcTotalMemory;
            lastGcTotalMemory = totalMemory;
            return memoryDelta > 0 ? memoryDelta : 0;
        }

        private void ProbeThreadAllocCounter()
        {
            try
            {
                lastThreadAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
                threadAllocCounterAvailable = true;
            }
            catch
            {
                threadAllocCounterAvailable = false;
            }
        }

        private void PrimeAiDecisionBaseline()
        {
            IReadOnlyList<EnemyAI> enemies = EnemyAI.ActiveInstances;
            for (int i = 0; i < enemies.Count; i++)
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

                EnemyAI.EnemyAIDebugSnapshot snapshot = enemy.GetDebugSnapshot();
                previousDecisionCounts[enemy.GetInstanceID()] = snapshot.decisionCount;
            }
        }

        private void RecordAiSample()
        {
            float now = Time.unscaledTime;
            float elapsed = Mathf.Max(0.0001f, now - lastAiSampleTime);
            lastAiSampleTime = now;

            int activeEnemyCount = 0;
            int decisionDelta = 0;
            latestDecisionCounts.Clear();
            IReadOnlyList<EnemyAI> enemies = EnemyAI.ActiveInstances;

            for (int i = 0; i < enemies.Count; i++)
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

                EnemyAI.EnemyAIDebugSnapshot snapshot = enemy.GetDebugSnapshot();
                int enemyId = enemy.GetInstanceID();
                latestDecisionCounts[enemyId] = snapshot.decisionCount;
                activeEnemyCount++;

                if (previousDecisionCounts.TryGetValue(enemyId, out int previousDecisionCount))
                {
                    decisionDelta += Mathf.Max(0, snapshot.decisionCount - previousDecisionCount);
                }
            }

            previousDecisionCounts.Clear();
            foreach (KeyValuePair<int, int> pair in latestDecisionCounts)
            {
                previousDecisionCounts[pair.Key] = pair.Value;
            }

            if (aiDecisionRateSamples.Count < maxAiSamples)
            {
                aiDecisionRateSamples.Add(decisionDelta / elapsed);
            }

            if (aiActiveEnemySamples.Count < maxAiSamples)
            {
                aiActiveEnemySamples.Add(activeEnemyCount);
            }

            if (projectileActiveSamples.Count < maxAiSamples)
            {
                projectileActiveSamples.Add(CountActiveComponents<EnemyProjectile>());
            }

            if (uiDamageTextActiveSamples.Count < maxAiSamples)
            {
                uiDamageTextActiveSamples.Add(CountActiveDamageTexts());
            }

            if (activeParticleSamples.Count < maxAiSamples)
            {
                activeParticleSamples.Add(CountActiveParticles());
            }
        }

        private StepRecord BuildStepRecord()
        {
            StepRecord record = new StepRecord();
            record.label = currentStepLabel;
            record.targetCount = currentTargetCount;
            record.durationSeconds = Mathf.Max(0f, Time.unscaledTime - stepStartTime);
            record.frameSamples = frameTimeSamplesMs.Count;

            record.avgFrameMs = ComputeAverage(frameTimeSamplesMs);
            record.p95FrameMs = ComputePercentile(frameTimeSamplesMs, 0.95f);
            record.p99FrameMs = ComputePercentile(frameTimeSamplesMs, 0.99f);
            record.maxFrameMs = ComputeMax(frameTimeSamplesMs);

            float avgFrameSeconds = record.avgFrameMs / 1000f;
            record.avgFps = avgFrameSeconds > 0.0001f ? 1f / avgFrameSeconds : 0f;
            float p99FrameSeconds = record.p99FrameMs / 1000f;
            record.p1Fps = p99FrameSeconds > 0.0001f ? 1f / p99FrameSeconds : 0f;

            record.avgGcAllocBytesPerFrame = ComputeAverageLong(gcAllocSamplesBytes);
            record.p95GcAllocBytesPerFrame = ComputePercentileLong(gcAllocSamplesBytes, 0.95f);
            record.maxGcAllocBytesPerFrame = ComputeMaxLong(gcAllocSamplesBytes);
            record.totalGcAllocBytes = ComputeSum(gcAllocSamplesBytes);

            record.gcCollectionsGen0 = Mathf.Max(0, GC.CollectionCount(0) - gcCollectionStartGen0);
            record.gcCollectionsGen1 = Mathf.Max(0, GC.CollectionCount(1) - gcCollectionStartGen1);
            record.gcCollectionsGen2 = Mathf.Max(0, GC.CollectionCount(2) - gcCollectionStartGen2);

            record.avgAiDecisionsPerSecond = ComputeAverage(aiDecisionRateSamples);
            record.p95AiDecisionsPerSecond = ComputePercentile(aiDecisionRateSamples, 0.95f);
            record.peakAiDecisionsPerSecond = ComputeMax(aiDecisionRateSamples);
            record.avgActiveEnemies = ComputeAverageInt(aiActiveEnemySamples);
            record.avgActiveProjectiles = ComputeAverageInt(projectileActiveSamples);
            record.p95ActiveProjectiles = ComputePercentileInt(projectileActiveSamples, 0.95f);
            record.avgActiveDamageTexts = ComputeAverageInt(uiDamageTextActiveSamples);
            record.p95ActiveDamageTexts = ComputePercentileInt(uiDamageTextActiveSamples, 0.95f);
            record.avgActiveParticles = ComputeAverageInt(activeParticleSamples);
            record.p95ActiveParticles = ComputePercentileInt(activeParticleSamples, 0.95f);
            return record;
        }

        private string BuildCsv()
        {
            StringBuilder sb = new StringBuilder(4096);
            sb.AppendLine("step_label,target_count,duration_s,frame_samples,avg_frame_ms,p95_frame_ms,p99_frame_ms,max_frame_ms,avg_fps,p1_fps,avg_gc_alloc_bytes_per_frame,p95_gc_alloc_bytes_per_frame,max_gc_alloc_bytes_per_frame,total_gc_alloc_bytes,gc_collections_gen0,gc_collections_gen1,gc_collections_gen2,avg_ai_decisions_per_s,p95_ai_decisions_per_s,peak_ai_decisions_per_s,avg_active_enemies,avg_active_projectiles,p95_active_projectiles,avg_active_damage_texts,p95_active_damage_texts,avg_active_particles,p95_active_particles");

            for (int i = 0; i < recordedSteps.Count; i++)
            {
                StepRecord record = recordedSteps[i];
                sb.Append(EscapeCsv(record.label)).Append(',');
                sb.Append(record.targetCount).Append(',');
                sb.Append(FormatFloat(record.durationSeconds)).Append(',');
                sb.Append(record.frameSamples).Append(',');
                sb.Append(FormatFloat(record.avgFrameMs)).Append(',');
                sb.Append(FormatFloat(record.p95FrameMs)).Append(',');
                sb.Append(FormatFloat(record.p99FrameMs)).Append(',');
                sb.Append(FormatFloat(record.maxFrameMs)).Append(',');
                sb.Append(FormatFloat(record.avgFps)).Append(',');
                sb.Append(FormatFloat(record.p1Fps)).Append(',');
                sb.Append(record.avgGcAllocBytesPerFrame).Append(',');
                sb.Append(record.p95GcAllocBytesPerFrame).Append(',');
                sb.Append(record.maxGcAllocBytesPerFrame).Append(',');
                sb.Append(record.totalGcAllocBytes).Append(',');
                sb.Append(record.gcCollectionsGen0).Append(',');
                sb.Append(record.gcCollectionsGen1).Append(',');
                sb.Append(record.gcCollectionsGen2).Append(',');
                sb.Append(FormatFloat(record.avgAiDecisionsPerSecond)).Append(',');
                sb.Append(FormatFloat(record.p95AiDecisionsPerSecond)).Append(',');
                sb.Append(FormatFloat(record.peakAiDecisionsPerSecond)).Append(',');
                sb.Append(FormatFloat(record.avgActiveEnemies)).Append(',');
                sb.Append(FormatFloat(record.avgActiveProjectiles)).Append(',');
                sb.Append(record.p95ActiveProjectiles).Append(',');
                sb.Append(FormatFloat(record.avgActiveDamageTexts)).Append(',');
                sb.Append(record.p95ActiveDamageTexts).Append(',');
                sb.Append(FormatFloat(record.avgActiveParticles)).Append(',');
                sb.Append(record.p95ActiveParticles);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string ResolveOutputPath()
        {
            string safeFileName = string.IsNullOrEmpty(outputFileName)
                ? "enemy_ai_p3_stress_metrics.csv"
                : outputFileName.Trim();

            if (writeToAssetsReports)
            {
                return Path.Combine(Application.dataPath, "ThirdPersonController", "Reports", safeFileName);
            }

            return Path.Combine(Application.persistentDataPath, safeFileName);
        }

        private string BuildRecordSummary(StepRecord record)
        {
            StringBuilder sb = new StringBuilder(256);
            sb.Append("[EnemyPerf] ").Append(record.label)
                .Append(" target=").Append(record.targetCount)
                .Append(" avgFrame=").Append(FormatFloat(record.avgFrameMs)).Append("ms")
                .Append(" p95=").Append(FormatFloat(record.p95FrameMs)).Append("ms")
                .Append(" p99=").Append(FormatFloat(record.p99FrameMs)).Append("ms")
                .Append(" avgFPS=").Append(FormatFloat(record.avgFps))
                .Append(" gcAvg=").Append(record.avgGcAllocBytesPerFrame)
                .Append("B/frame")
                .Append(" aiAvg=").Append(FormatFloat(record.avgAiDecisionsPerSecond))
                .Append("/s")
                .Append(" enemiesAvg=").Append(FormatFloat(record.avgActiveEnemies))
                .Append(" projectilesAvg=").Append(FormatFloat(record.avgActiveProjectiles))
                .Append(" uiDamageAvg=").Append(FormatFloat(record.avgActiveDamageTexts))
                .Append(" particlesAvg=").Append(FormatFloat(record.avgActiveParticles));
            return sb.ToString();
        }

        private static int CountActiveComponents<T>() where T : Component
        {
            T[] components = FindObjectsOfType<T>();
            int count = 0;
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component == null)
                {
                    continue;
                }

                if (component is Behaviour behaviour)
                {
                    if (behaviour.isActiveAndEnabled)
                    {
                        count++;
                    }

                    continue;
                }

                if (component.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveDamageTexts()
        {
            UI_DamageText[] damageTexts = FindObjectsOfType<UI_DamageText>();
            int count = 0;
            for (int i = 0; i < damageTexts.Length; i++)
            {
                UI_DamageText damageText = damageTexts[i];
                if (damageText != null && damageText.IsPlaying && damageText.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveParticles()
        {
            ParticleSystem[] particleSystems = FindObjectsOfType<ParticleSystem>();
            int count = 0;
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];
                if (particleSystem == null || !particleSystem.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (particleSystem.isPlaying || particleSystem.particleCount > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static float ComputeAverage(List<float> samples)
        {
            if (samples == null || samples.Count == 0)
            {
                return 0f;
            }

            double total = 0d;
            for (int i = 0; i < samples.Count; i++)
            {
                total += samples[i];
            }

            return (float)(total / samples.Count);
        }

        private static float ComputeAverageInt(List<int> samples)
        {
            if (samples == null || samples.Count == 0)
            {
                return 0f;
            }

            long total = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                total += samples[i];
            }

            return (float)total / samples.Count;
        }

        private static long ComputeAverageLong(List<long> samples)
        {
            if (samples == null || samples.Count == 0)
            {
                return 0;
            }

            long total = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                total += samples[i];
            }

            return total / samples.Count;
        }

        private static long ComputeSum(List<long> samples)
        {
            if (samples == null || samples.Count == 0)
            {
                return 0;
            }

            long total = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                total += samples[i];
            }

            return total;
        }

        private static float ComputePercentile(List<float> samples, float percentile)
        {
            if (samples == null || samples.Count == 0)
            {
                return 0f;
            }

            float[] buffer = samples.ToArray();
            Array.Sort(buffer);
            int index = Mathf.Clamp(Mathf.CeilToInt((buffer.Length - 1) * percentile), 0, buffer.Length - 1);
            return buffer[index];
        }

        private static long ComputePercentileLong(List<long> samples, float percentile)
        {
            if (samples == null || samples.Count == 0)
            {
                return 0;
            }

            long[] buffer = samples.ToArray();
            Array.Sort(buffer);
            int index = Mathf.Clamp(Mathf.CeilToInt((buffer.Length - 1) * percentile), 0, buffer.Length - 1);
            return buffer[index];
        }

        private static int ComputePercentileInt(List<int> samples, float percentile)
        {
            if (samples == null || samples.Count == 0)
            {
                return 0;
            }

            int[] buffer = samples.ToArray();
            Array.Sort(buffer);
            int index = Mathf.Clamp(Mathf.CeilToInt((buffer.Length - 1) * percentile), 0, buffer.Length - 1);
            return buffer[index];
        }

        private static float ComputeMax(List<float> samples)
        {
            if (samples == null || samples.Count == 0)
            {
                return 0f;
            }

            float maxValue = float.MinValue;
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i] > maxValue)
                {
                    maxValue = samples[i];
                }
            }

            return maxValue;
        }

        private static long ComputeMaxLong(List<long> samples)
        {
            if (samples == null || samples.Count == 0)
            {
                return 0;
            }

            long maxValue = long.MinValue;
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i] > maxValue)
                {
                    maxValue = samples[i];
                }
            }

            return maxValue;
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (!value.Contains(",") && !value.Contains("\"") && !value.Contains("\n"))
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
