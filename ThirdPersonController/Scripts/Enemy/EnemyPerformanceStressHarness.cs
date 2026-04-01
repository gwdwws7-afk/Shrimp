using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ThirdPersonController
{
    /// <summary>
    /// Executes repeatable P3 stress rounds (spawn -> warmup -> sample -> export).
    /// </summary>
    public class EnemyPerformanceStressHarness : MonoBehaviour
    {
        [Serializable]
        public struct StressStepConfig
        {
            public string label;
            [Min(1)] public int targetEnemyCount;
            [Min(0f)] public float warmupSeconds;
            [Min(0.1f)] public float sampleSeconds;
            [Min(1)] public int spawnBurstPerFrame;
        }

        [Header("References")]
        public GameObject enemyPrefab;
        public Transform player;
        public PlayerInputHandler inputHandler;
        public EnemyPerformanceMetricsSampler metricsSampler;

        [Header("Control")]
        public bool runOnStart = false;
        public string runActionName = "DebugStressRun";
        public string clearActionName = "DebugStressClear";
        public KeyCode runKey = KeyCode.F9;
        public KeyCode clearKey = KeyCode.F10;
        public bool logControlHintsOnStart = true;
        public bool autoExportCsv = true;
        [Min(1)] public int repeatPerStep = 1;
        public bool appendRepeatIndexToStepLabel = true;

        [Header("Stress Target")]
        public bool forceOverrideTargetToPlayer = true;

        [Header("Coordinator (Stress)")]
        public bool ensureCrowdCoordinator = true;
        public bool tuneCrowdCoordinatorForStress = true;
        public float stressCoordinatorNearbyCountRadius = 18f;
        public float stressCoordinatorNearbyCountInterval = 0.25f;

        [Header("Spawn")]
        public float spawnRadius = 15f;
        public float spawnHeight = 0.3f;
        public float minSpawnDistance = 2f;
        public float ringSpacing = 1.4f;
        public float angularJitter = 6f;

        [Header("Spawn NavMesh")]
        public bool projectSpawnToNavMesh = true;
        public float navMeshSampleRadius = 5f;
        [Min(1)] public int navMeshSampleAttempts = 6;

        [Header("P3 Steps")]
        public List<StressStepConfig> steps = new List<StressStepConfig>();

        [Header("P4 Long-Run Preset")]
        [Min(0f)] public float longRunWarmupSeconds = 15f;
        [Min(10f)] public float longRunSampleSecondsFor100 = 120f;
        [Min(10f)] public float longRunSampleSecondsFor150 = 180f;
        [Min(1)] public int longRunRepeatCount = 2;
        [Min(1)] public int longRunBurstPerFrame100 = 14;
        [Min(1)] public int longRunBurstPerFrame150 = 16;

        [Header("Population Maintenance")]
        public bool maintainPopulationDuringSampling = true;
        [Min(0.1f)] public float maintainPopulationInterval = 0.5f;
        [Range(0f, 1f)] public float maintainPopulationTopUpRatio = 0.98f;
        [Min(1)] public int maintainPopulationMaxTopUpPerTick = 48;

        [Header("Debug (Runtime)")]
        [SerializeField] private bool isRunning = false;
        [SerializeField] private int activeEnemyCount = 0;
        [SerializeField] private string currentStep = "";
        [SerializeField] private string lastExportPath = "";
        [SerializeField] private int lastSpawnedCount = 0;

        private Coroutine runRoutine;
        private EnemyCrowdCoordinator stressCoordinator;

        public bool IsRunning => isRunning;
        public int ActiveEnemyCount => activeEnemyCount;
        public string CurrentStep => currentStep;
        public string LastExportPath => lastExportPath;

        private void Start()
        {
            TryResolveReferences();
            EnsureDefaultSteps();

            if (logControlHintsOnStart)
            {
                string runBinding = ResolveBindingLabel(runActionName, runKey);
                string clearBinding = ResolveBindingLabel(clearActionName, clearKey);
                Debug.Log($"[EnemyPerfHarness] {runBinding}=run stress steps, {clearBinding}=clear enemies.");
            }

            if (runOnStart)
            {
                StartStressRun();
            }
        }

        private void Update()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            bool runPressed = handler != null && handler.WasActionPressedThisFrame(runActionName, runKey);
            if (runPressed)
            {
                StartStressRun();
            }

            bool clearPressed = handler != null && handler.WasActionPressedThisFrame(clearActionName, clearKey);
            if (clearPressed)
            {
                ClearEnemiesNow();
            }
        }

        [ContextMenu("Run Stress Steps")]
        public void StartStressRun()
        {
            if (isRunning)
            {
                return;
            }

            TryResolveReferences();
            EnsureDefaultSteps();

            if (enemyPrefab == null)
            {
                Debug.LogWarning("[EnemyPerfHarness] enemyPrefab is missing.");
                return;
            }

            if (metricsSampler == null)
            {
                Debug.LogWarning("[EnemyPerfHarness] metricsSampler is missing.");
                return;
            }

            if (steps == null || steps.Count == 0)
            {
                Debug.LogWarning("[EnemyPerfHarness] no stress steps configured.");
                return;
            }

            runRoutine = StartCoroutine(RunStepsCoroutine());
        }

        [ContextMenu("Stop Stress Run")]
        public void StopStressRun()
        {
            if (runRoutine != null)
            {
                StopCoroutine(runRoutine);
                runRoutine = null;
            }

            isRunning = false;
            currentStep = "";

            if (metricsSampler != null && metricsSampler.IsRecording)
            {
                metricsSampler.EndStep();
            }
        }

        [ContextMenu("Clear Enemies")]
        public void ClearEnemiesNow()
        {
            EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth enemy = enemies[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy)
                {
                    continue;
                }

                ObjectPoolManager.Despawn(enemy.gameObject);
            }

            activeEnemyCount = 0;
        }

        [ContextMenu("Reset Default P3 Steps")]
        public void ResetDefaultSteps()
        {
            steps.Clear();
            EnsureDefaultSteps();
        }

        [ContextMenu("Reset Long-Run P4 Steps")]
        public void ResetLongRunSteps()
        {
            ApplyLongRunPreset(true);
        }

        public void EnsureDefaultSteps()
        {
            if (steps == null)
            {
                steps = new List<StressStepConfig>();
            }

            if (steps.Count > 0)
            {
                return;
            }

            steps.Add(CreateStep("P3_100", 100, 8f, 20f, 12));
            steps.Add(CreateStep("P3_150", 150, 10f, 24f, 14));
        }

        public void ApplyLongRunPreset(bool replaceExistingSteps)
        {
            if (steps == null)
            {
                steps = new List<StressStepConfig>();
            }

            if (replaceExistingSteps)
            {
                steps.Clear();
            }

            steps.Add(CreateStep(
                "P4_LONG_100",
                100,
                Mathf.Max(0f, longRunWarmupSeconds),
                Mathf.Max(10f, longRunSampleSecondsFor100),
                Mathf.Max(1, longRunBurstPerFrame100)));

            steps.Add(CreateStep(
                "P4_LONG_150",
                150,
                Mathf.Max(0f, longRunWarmupSeconds),
                Mathf.Max(10f, longRunSampleSecondsFor150),
                Mathf.Max(1, longRunBurstPerFrame150)));

            repeatPerStep = Mathf.Max(1, longRunRepeatCount);
            appendRepeatIndexToStepLabel = repeatPerStep > 1;
        }

        public void SpawnToTargetImmediate(int targetCount)
        {
            if (enemyPrefab == null)
            {
                return;
            }

            if (targetCount <= 0)
            {
                activeEnemyCount = CountActiveEnemies();
                return;
            }

            TryResolveReferences();
            int currentCount = CountActiveEnemies();
            int spawnCount = Mathf.Max(0, targetCount - currentCount);
            SpawnBurst(spawnCount, currentCount, targetCount);
            activeEnemyCount = CountActiveEnemies();
            lastSpawnedCount = spawnCount;
        }

        public int GetActiveEnemyCount()
        {
            activeEnemyCount = CountActiveEnemies();
            return activeEnemyCount;
        }

        private IEnumerator RunStepsCoroutine()
        {
            isRunning = true;
            currentStep = "";
            lastExportPath = "";
            TryResolveReferences();

            int stepRepeatCount = Mathf.Max(1, repeatPerStep);
            for (int i = 0; i < steps.Count; i++)
            {
                StressStepConfig step = SanitizeStep(steps[i], i);
                for (int repeatIndex = 0; repeatIndex < stepRepeatCount; repeatIndex++)
                {
                    string stepLabel = BuildStepLabel(step.label, repeatIndex, stepRepeatCount);
                    currentStep = stepLabel;
                    ClearEnemiesNow();
                    yield return null;

                    yield return SpawnToTargetCoroutine(step.targetEnemyCount, step.spawnBurstPerFrame);
                    activeEnemyCount = CountActiveEnemies();
                    Debug.Log($"[EnemyPerfHarness] step={stepLabel} spawned={lastSpawnedCount}, active={activeEnemyCount}");

                    if (step.warmupSeconds > 0f)
                    {
                        yield return WaitUnscaled(step.warmupSeconds);
                    }

                    metricsSampler.BeginStep(stepLabel, step.targetEnemyCount);
                    if (maintainPopulationDuringSampling)
                    {
                        yield return SampleAndMaintainPopulation(step);
                    }
                    else
                    {
                        yield return WaitUnscaled(step.sampleSeconds);
                    }
                    metricsSampler.EndStep();
                }
            }

            if (autoExportCsv && metricsSampler != null)
            {
                lastExportPath = metricsSampler.ExportCsv();
            }

            currentStep = "";
            isRunning = false;
            runRoutine = null;
        }

        private IEnumerator SpawnToTargetCoroutine(int targetCount, int burstPerFrame)
        {
            int desiredCount = Mathf.Max(0, targetCount);
            int burst = Mathf.Max(1, burstPerFrame);
            int guard = 0;

            while (CountActiveEnemies() < desiredCount && guard < desiredCount * 4)
            {
                int current = CountActiveEnemies();
                int remaining = desiredCount - current;
                int spawnNow = Mathf.Min(burst, remaining);

                SpawnBurst(spawnNow, current, desiredCount);
                guard += spawnNow;
                yield return null;
            }

            activeEnemyCount = CountActiveEnemies();
        }

        private IEnumerator SampleAndMaintainPopulation(StressStepConfig step)
        {
            float remaining = Mathf.Max(0f, step.sampleSeconds);
            float interval = Mathf.Max(0.1f, maintainPopulationInterval);
            int topUpBurst = Mathf.Max(1, Mathf.Max(maintainPopulationMaxTopUpPerTick, step.spawnBurstPerFrame));
            float topUpRatio = Mathf.Clamp01(maintainPopulationTopUpRatio);
            int desiredCount = Mathf.Max(1, step.targetEnemyCount);

            while (remaining > 0f)
            {
                float waitTime = Mathf.Min(interval, remaining);
                yield return WaitUnscaled(waitTime);
                remaining -= waitTime;

                int activeCount = CountActiveEnemies();
                int threshold = Mathf.CeilToInt(desiredCount * topUpRatio);
                if (activeCount >= threshold)
                {
                    continue;
                }

                int deficit = Mathf.Max(0, desiredCount - activeCount);
                int spawnNow = Mathf.Min(deficit, topUpBurst);
                if (spawnNow <= 0)
                {
                    continue;
                }

                SpawnBurst(spawnNow, activeCount, desiredCount);
                activeEnemyCount = CountActiveEnemies();
            }
        }

        private void SpawnBurst(int count, int existingCount, int targetCount)
        {
            if (count <= 0 || enemyPrefab == null)
            {
                lastSpawnedCount = 0;
                return;
            }

            TryResolveReferences();
            Vector3 center = player != null ? player.position : transform.position;
            int spawned = 0;
            int slotsPerRing = Mathf.Max(10, Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(1, targetCount)) * 6f));

            for (int i = 0; i < count; i++)
            {
                int index = existingCount + i;
                int ring = index / slotsPerRing;
                int slot = index % slotsPerRing;
                float angle = (slot / (float)slotsPerRing) * 360f;
                angle += UnityEngine.Random.Range(-angularJitter, angularJitter);

                float radius = spawnRadius + ring * Mathf.Max(0.4f, ringSpacing);
                radius = Mathf.Max(minSpawnDistance, radius);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                Vector3 desiredPos = center + new Vector3(offset.x, 0f, offset.z);
                if (!TryResolveSpawnPosition(center, desiredPos, radius, out Vector3 resolvedPos))
                {
                    continue;
                }

                Vector3 spawnPos = resolvedPos + Vector3.up * Mathf.Max(0f, spawnHeight);

                GameObject enemy = ObjectPoolManager.Spawn(enemyPrefab, spawnPos, Quaternion.identity);
                if (enemy == null)
                {
                    continue;
                }

                if (player != null)
                {
                    Vector3 lookPoint = player.position;
                    lookPoint.y = enemy.transform.position.y;
                    enemy.transform.LookAt(lookPoint);
                }

                if (forceOverrideTargetToPlayer && player != null)
                {
                    EnemyAI enemyAi = enemy.GetComponent<EnemyAI>();
                    if (enemyAi != null)
                    {
                        enemyAi.SetOverrideTarget(player, true);
                    }
                }

                spawned++;
            }

            lastSpawnedCount = spawned;
        }

        private bool TryResolveSpawnPosition(Vector3 center, Vector3 desiredPos, float fallbackRadius, out Vector3 resolvedPos)
        {
            resolvedPos = desiredPos;
            if (!projectSpawnToNavMesh)
            {
                return true;
            }

            float sampleRadius = Mathf.Max(0.5f, navMeshSampleRadius);
            if (NavMesh.SamplePosition(desiredPos, out NavMeshHit directHit, sampleRadius, NavMesh.AllAreas))
            {
                resolvedPos = directHit.position;
                return true;
            }

            int attempts = Mathf.Max(1, navMeshSampleAttempts);
            float probeRadius = Mathf.Max(sampleRadius, fallbackRadius);
            for (int i = 0; i < attempts; i++)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * probeRadius;
                Vector3 probe = center + new Vector3(randomCircle.x, 0f, randomCircle.y);
                if (NavMesh.SamplePosition(probe, out NavMeshHit probeHit, sampleRadius, NavMesh.AllAreas))
                {
                    resolvedPos = probeHit.position;
                    return true;
                }
            }

            // Stress gate prefers stable population over strict navmesh-only placement.
            resolvedPos = desiredPos;
            return true;
        }

        private static StressStepConfig CreateStep(string label, int targetCount, float warmupSeconds, float sampleSeconds, int burstPerFrame)
        {
            StressStepConfig step = new StressStepConfig
            {
                label = label,
                targetEnemyCount = Mathf.Max(1, targetCount),
                warmupSeconds = Mathf.Max(0f, warmupSeconds),
                sampleSeconds = Mathf.Max(0.1f, sampleSeconds),
                spawnBurstPerFrame = Mathf.Max(1, burstPerFrame)
            };
            return step;
        }

        private static StressStepConfig SanitizeStep(StressStepConfig raw, int index)
        {
            StressStepConfig step = raw;
            if (string.IsNullOrEmpty(step.label))
            {
                step.label = $"P3_Step_{index + 1}";
            }

            step.targetEnemyCount = Mathf.Max(1, step.targetEnemyCount);
            step.warmupSeconds = Mathf.Max(0f, step.warmupSeconds);
            step.sampleSeconds = Mathf.Max(0.1f, step.sampleSeconds);
            step.spawnBurstPerFrame = Mathf.Max(1, step.spawnBurstPerFrame);
            return step;
        }

        private string BuildStepLabel(string baseLabel, int repeatIndex, int repeatCount)
        {
            string label = string.IsNullOrEmpty(baseLabel) ? "P3_Step" : baseLabel;
            if (!appendRepeatIndexToStepLabel || repeatCount <= 1)
            {
                return label;
            }

            return $"{label}_R{repeatIndex + 1}";
        }

        private int CountActiveEnemies()
        {
            EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>(true);
            int count = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth enemy = enemies[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private void TryResolveReferences()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }

            if (inputHandler == null)
            {
                inputHandler = FindObjectOfType<PlayerInputHandler>();
            }

            if (metricsSampler == null)
            {
                metricsSampler = FindObjectOfType<EnemyPerformanceMetricsSampler>();
            }

            if (ensureCrowdCoordinator)
            {
                EnsureCrowdCoordinator();
            }
        }

        private PlayerInputHandler ResolveInputHandler()
        {
            if (inputHandler != null)
            {
                return inputHandler;
            }

            inputHandler = PlayerInputHandler.ResolveActiveInstance();
            return inputHandler;
        }

        private string ResolveBindingLabel(string actionName, KeyCode fallbackKey)
        {
            PlayerInputHandler handler = ResolveInputHandler();
            if (handler == null)
            {
                return PlayerInputHandler.GetFriendlyKeyLabel(fallbackKey);
            }

            if (!handler.AreDebugHotkeysEnabled())
            {
                return "Disabled";
            }

            string binding = handler.GetActionBindingLabel(actionName, fallbackKey, includeGamepad: false);
            return string.IsNullOrEmpty(binding)
                ? PlayerInputHandler.GetFriendlyKeyLabel(fallbackKey)
                : binding;
        }

        private void EnsureCrowdCoordinator()
        {
            stressCoordinator = FindObjectOfType<EnemyCrowdCoordinator>();
            if (stressCoordinator == null)
            {
                GameObject go = new GameObject("EnemyCrowdCoordinator_Stress");
                go.transform.SetParent(transform);
                stressCoordinator = go.AddComponent<EnemyCrowdCoordinator>();
            }

            if (stressCoordinator == null)
            {
                return;
            }

            if (player != null)
            {
                stressCoordinator.player = player;
            }

            if (tuneCrowdCoordinatorForStress)
            {
                stressCoordinator.nearbyCountRadius = Mathf.Max(8f, stressCoordinatorNearbyCountRadius);
                stressCoordinator.nearbyCountInterval = Mathf.Max(0.05f, stressCoordinatorNearbyCountInterval);
            }
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float duration = Mathf.Max(0f, seconds);
            float endTime = Time.unscaledTime + duration;
            while (Time.unscaledTime < endTime)
            {
                yield return null;
            }
        }
    }
}
