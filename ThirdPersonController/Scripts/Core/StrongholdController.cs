using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum WaveObjectiveType
    {
        KillAll,
        Survival,
        TimeAttack,
        NoDamage,
        Combo
    }

    [System.Serializable]
    public class WaveObjective
    {
        public WaveObjectiveType objectiveType = WaveObjectiveType.KillAll;
        public int targetValue = 0;
        public float timeLimit = 0f;
        public bool isOptional = false;
    }

    [System.Serializable]
    public class WaveSpawnGroup
    {
        public GameObject prefab;
        public int count = 5;
        public float spawnIntervalOverride = -1f;
        public EnemyArchetype archetypeOverride;
    }

    [System.Serializable]
    public class WaveEliteTrigger
    {
        public bool enabled = false;
        public int triggerOnRemaining = 3;
        public float triggerDelay = 0f;
        public float spawnInterval = 0.4f;
        public List<WaveSpawnGroup> eliteGroups = new List<WaveSpawnGroup>();
    }

    public enum WaveEventType
    {
        Reinforcement,
        Chase,
        HoldPoint,
        ProtectTarget
    }

    [System.Serializable]
    public class WaveEvent
    {
        public string name = "Event";
        public WaveEventType eventType = WaveEventType.Reinforcement;
        public bool enabled = true;
        public float triggerDelay = 0f;
        public int triggerOnRemaining = -1;

        [Header("Spawn")]
        public float duration = 6f;
        public float spawnInterval = 0.45f;
        public float spawnRadius = 5f;
        public bool useReinforcementPoints = true;
        public List<WaveSpawnGroup> groups = new List<WaveSpawnGroup>();

        [Header("Hold Point")]
        public Transform holdPoint;
        public float holdRadius = 3f;
        public float holdDuration = 6f;
        public float holdDecayRate = 1f;
        public bool showHoldMarker = true;

        [Header("Defense Target")]
        public DefenseTarget defenseTarget;
        public GameObject defenseTargetPrefab;
        public int defenseTargetHealth = 200;
        public bool spawnDefenseTarget = true;
        public bool failOnTargetDestroyed = true;
        public bool assignTargetToSpawnedEnemies = true;
        public string defenseTargetId = "";
    }

    [System.Serializable]
    public class StrongholdWave
    {
        public string name = "Wave";
        public float startDelay = 0.4f;
        public float spawnInterval = 0.35f;
        public bool shuffleSpawnPoints = true;
        public List<WaveSpawnGroup> groups = new List<WaveSpawnGroup>();
        public WaveEliteTrigger eliteTrigger = new WaveEliteTrigger();

        [Header("Events")]
        public List<WaveEvent> events = new List<WaveEvent>();
        
        [Header("Objectives")]
        public WaveObjective objective = new WaveObjective();
        public bool useTimeLimit = false;
        public float waveTimeLimit = 120f;
    }

    public class StrongholdController : MonoBehaviour
    {
        [Header("Activation")]
        public string strongholdId = "";
        public bool activeOnStart = true;
        public bool startOnPlayerEnter = true;
        public string playerTag = "Player";
        public Collider triggerArea;

        [Header("Spawn")]
        public Transform center;
        public List<Transform> spawnPoints = new List<Transform>();
        public List<Transform> reinforcementPoints = new List<Transform>();
        public float spawnRadius = 6f;
        public float spawnHeight = 0.5f;
        public float spawnPointJitter = 0.4f;
        public bool useGroundSnap = false;
        public LayerMask groundLayer = default;
        public bool facePlayerOnSpawn = true;
        public bool usePooling = true;

        [Header("Wave Timing")]
        public float waveCompleteDelay = 1f;

        [Header("Spawn Director")]
        public bool autoFindDirector = true;
        public WaveSpawnDirector spawnDirector;

        [Header("Waves")]
        public List<StrongholdWave> waves = new List<StrongholdWave>();
        
        [Header("Objectives")]
        public bool useObjectives = true;
        public WaveObjective strongholdObjective = new WaveObjective();
        public int comboTarget = 50;
        public bool requireNoDamage = false;
        
        [Header("Events")]
        public System.Action<StrongholdController> OnStrongholdStarted;
        public event System.Action<StrongholdController, int> OnWaveStarted;
        public event System.Action<StrongholdController, int> OnWaveCompleted;
        public event System.Action<StrongholdController> OnStrongholdCompleted;
        public event System.Action<StrongholdController> OnStrongholdFailed;

        private class WaveRuntime
        {
            public int baseAlive;
            public int totalAlive;
            public bool spawnComplete;
            public bool eliteTriggered;
            public bool eliteSpawnPending;
            public List<EventRuntime> eventRuntimes = new List<EventRuntime>();

            public void Reset()
            {
                baseAlive = 0;
                totalAlive = 0;
                spawnComplete = false;
                eliteTriggered = false;
                eliteSpawnPending = false;
                eventRuntimes.Clear();
            }
        }

        private class EventRuntime
        {
            public bool triggered;
            public bool completed;
            public GameObject holdMarker;
            public float holdProgress;
            public DefenseTarget defenseTarget;
        }

        private readonly List<WaveRuntime> runtimes = new List<WaveRuntime>();
        private Transform player;
        private Coroutine strongholdRoutine;
        private int spawnPointCursor = 0;
        private int reinforcementPointCursor = 0;
        private int currentWaveIndex = -1;

        private static WaveSpawnDirector sharedDirector;

        private bool isActive;
        private bool isRunning;
        private bool isCompleted;

        public bool IsActive => isActive;
        public bool IsRunning => isRunning;
        public bool IsCompleted => isCompleted;
        public int CurrentWaveIndex => currentWaveIndex;
        public int TotalWaves => waves != null ? waves.Count : 0;
        public string StrongholdId => string.IsNullOrEmpty(strongholdId) ? name : strongholdId;

        private void Awake()
        {
            if (center == null)
            {
                center = transform;
            }

            if (triggerArea == null)
            {
                triggerArea = GetComponent<Collider>();
            }

            if (triggerArea != null)
            {
                triggerArea.isTrigger = true;
            }

            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
                if (playerObject != null)
                {
                    player = playerObject.transform;
                }
            }

            ResolveSpawnDirector();
        }

        private void Start()
        {
            SetActive(activeOnStart);
            if (activeOnStart && !startOnPlayerEnter)
            {
                BeginStronghold();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!startOnPlayerEnter || !isActive || isRunning || isCompleted)
            {
                return;
            }

            if (other.CompareTag(playerTag) || other.GetComponentInParent<PlayerCombat>() != null)
            {
                BeginStronghold();
            }
        }

        public void SetActive(bool active)
        {
            isActive = active;
            if (triggerArea != null)
            {
                triggerArea.enabled = active;
            }
        }

        public void BeginStronghold()
        {
            if (isRunning || isCompleted)
            {
                return;
            }

            if (!isActive)
            {
                return;
            }

            PrepareRuntime();

            isRunning = true;
            strongholdRoutine = StartCoroutine(StrongholdRoutine());
            OnStrongholdStarted?.Invoke(this);
        }

        public void CancelStronghold()
        {
            if (!isRunning)
            {
                return;
            }

            if (strongholdRoutine != null)
            {
                StopCoroutine(strongholdRoutine);
                strongholdRoutine = null;
            }

            CleanupAllWaveEvents();

            isRunning = false;
            currentWaveIndex = -1;
        }

        public void FailStronghold(string reason)
        {
            if (!isRunning || isCompleted)
            {
                return;
            }

            if (strongholdRoutine != null)
            {
                StopCoroutine(strongholdRoutine);
                strongholdRoutine = null;
            }

            CleanupAllWaveEvents();
            isRunning = false;
            currentWaveIndex = -1;
            SetActive(false);
            if (!string.IsNullOrEmpty(reason))
            {
                GameEvents.ShowMessage(reason, 2f);
            }

            OnStrongholdFailed?.Invoke(this);
            GameEvents.GameOver(false);
        }

        public void NotifyEnemyDestroyed(int waveIndex, bool isElite)
        {
            if (!isRunning)
            {
                return;
            }

            if (waveIndex < 0 || waveIndex >= runtimes.Count)
            {
                return;
            }

            WaveRuntime runtime = runtimes[waveIndex];
            runtime.totalAlive = Mathf.Max(0, runtime.totalAlive - 1);
            if (!isElite)
            {
                runtime.baseAlive = Mathf.Max(0, runtime.baseAlive - 1);
            }

            CheckEliteTrigger(waveIndex);
        }

        private void PrepareRuntime()
        {
            runtimes.Clear();
            for (int i = 0; i < waves.Count; i++)
            {
                WaveRuntime runtime = new WaveRuntime();
                StrongholdWave wave = waves[i];
                if (wave != null && wave.events != null && wave.events.Count > 0)
                {
                    for (int e = 0; e < wave.events.Count; e++)
                    {
                        runtime.eventRuntimes.Add(new EventRuntime());
                    }
                }
                runtimes.Add(runtime);
            }
            currentWaveIndex = -1;
        }

        private void InitializeEventRuntime(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waves.Count || waveIndex >= runtimes.Count)
            {
                return;
            }

            StrongholdWave wave = waves[waveIndex];
            WaveRuntime runtime = runtimes[waveIndex];
            runtime.eventRuntimes.Clear();
            if (wave == null || wave.events == null || wave.events.Count == 0)
            {
                return;
            }

            for (int i = 0; i < wave.events.Count; i++)
            {
                runtime.eventRuntimes.Add(new EventRuntime());
            }
        }

        private IEnumerator StrongholdRoutine()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                currentWaveIndex = i;
                OnWaveStarted?.Invoke(this, i);
                yield return StartCoroutine(SpawnWaveRoutine(i));

                yield return new WaitUntil(() => IsWaveComplete(i));
                OnWaveCompleted?.Invoke(this, i);
                CleanupWaveEvents(i);

                if (waveCompleteDelay > 0f)
                {
                    yield return new WaitForSeconds(waveCompleteDelay);
                }
            }

            isRunning = false;
            isCompleted = true;
            currentWaveIndex = -1;
            CleanupAllWaveEvents();
            OnStrongholdCompleted?.Invoke(this);
        }

        public bool TryGetWaveStatus(out int waveIndex, out int totalWaves, out int remaining, out int plannedTotal)
        {
            totalWaves = TotalWaves;
            waveIndex = currentWaveIndex;
            remaining = 0;
            plannedTotal = 0;

            if (!isRunning || waveIndex < 0 || waveIndex >= runtimes.Count)
            {
                return false;
            }

            WaveRuntime runtime = runtimes[waveIndex];
            remaining = runtime.totalAlive;
            plannedTotal = GetPlannedWaveTotal(waveIndex);
            return true;
        }

        public string GetWaveDisplayName(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waves.Count)
            {
                return string.Empty;
            }

            string name = waves[waveIndex].name;
            if (string.IsNullOrEmpty(name))
            {
                return $"Wave {waveIndex + 1}";
            }

            return name;
        }

        private int GetPlannedWaveTotal(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waves.Count)
            {
                return 0;
            }

            StrongholdWave wave = waves[waveIndex];
            int total = 0;
            if (wave.groups != null)
            {
                for (int i = 0; i < wave.groups.Count; i++)
                {
                    WaveSpawnGroup group = wave.groups[i];
                    if (group == null)
                    {
                        continue;
                    }
                    int baseCount = Mathf.Max(0, group.count);
                    int adjusted = AdjustSpawnCount(wave, group, waveIndex, false, baseCount);
                    total += Mathf.Max(0, adjusted);
                }
            }

            if (wave.eliteTrigger != null && wave.eliteTrigger.enabled && wave.eliteTrigger.eliteGroups != null)
            {
                for (int i = 0; i < wave.eliteTrigger.eliteGroups.Count; i++)
                {
                    WaveSpawnGroup group = wave.eliteTrigger.eliteGroups[i];
                    if (group == null)
                    {
                        continue;
                    }
                    int baseCount = Mathf.Max(0, group.count);
                    int adjusted = AdjustSpawnCount(wave, group, waveIndex, true, baseCount);
                    total += Mathf.Max(0, adjusted);
                }
            }

            return total;
        }

        private IEnumerator SpawnWaveRoutine(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waves.Count)
            {
                yield break;
            }

            StrongholdWave wave = waves[waveIndex];
            WaveRuntime runtime = runtimes[waveIndex];
            runtime.Reset();
            InitializeEventRuntime(waveIndex);

            if (wave.startDelay > 0f)
            {
                yield return new WaitForSeconds(wave.startDelay);
            }

            StartWaveEvents(waveIndex);

            for (int g = 0; g < wave.groups.Count; g++)
            {
                WaveSpawnGroup group = wave.groups[g];
                if (group.prefab == null || group.count <= 0)
                {
                    continue;
                }

                int spawnCount = AdjustSpawnCount(wave, group, waveIndex, false, group.count);
                float baseInterval = group.spawnIntervalOverride > 0f ? group.spawnIntervalOverride : wave.spawnInterval;
                float interval = AdjustSpawnInterval(wave, group, waveIndex, false, baseInterval);
                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnEnemy(group.prefab, waveIndex, false, group.archetypeOverride);
                    runtime.baseAlive++;
                    runtime.totalAlive++;

                    if (interval > 0f)
                    {
                        yield return new WaitForSeconds(interval);
                    }
                }
            }

            runtime.spawnComplete = true;
            CheckEliteTrigger(waveIndex);
        }

        private void StartWaveEvents(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waves.Count || waveIndex >= runtimes.Count)
            {
                return;
            }

            StrongholdWave wave = waves[waveIndex];
            if (wave == null || wave.events == null || wave.events.Count == 0)
            {
                return;
            }

            for (int i = 0; i < wave.events.Count; i++)
            {
                WaveEvent waveEvent = wave.events[i];
                if (waveEvent == null || !waveEvent.enabled)
                {
                    continue;
                }

                switch (waveEvent.eventType)
                {
                    case WaveEventType.Reinforcement:
                        StartCoroutine(ReinforcementEventRoutine(waveIndex, i));
                        break;
                    case WaveEventType.Chase:
                        StartCoroutine(ChaseEventRoutine(waveIndex, i));
                        break;
                    case WaveEventType.HoldPoint:
                        StartCoroutine(HoldPointEventRoutine(waveIndex, i));
                        break;
                    case WaveEventType.ProtectTarget:
                        StartCoroutine(ProtectTargetEventRoutine(waveIndex, i));
                        break;
                }
            }
        }

        private IEnumerator ReinforcementEventRoutine(int waveIndex, int eventIndex)
        {
            StrongholdWave wave = waves[waveIndex];
            WaveRuntime runtime = runtimes[waveIndex];
            WaveEvent waveEvent = wave.events[eventIndex];
            EventRuntime eventRuntime = runtime.eventRuntimes[eventIndex];

            if (waveEvent.triggerDelay > 0f)
            {
                yield return new WaitForSeconds(waveEvent.triggerDelay);
            }

            if (waveEvent.triggerOnRemaining >= 0)
            {
                yield return new WaitUntil(() => runtime.spawnComplete && runtime.baseAlive <= waveEvent.triggerOnRemaining);
            }

            if (!IsWaveActive(waveIndex))
            {
                yield break;
            }

            eventRuntime.triggered = true;
            if (waveEvent.groups != null)
            {
                float interval = waveEvent.spawnInterval > 0f ? waveEvent.spawnInterval : wave.spawnInterval;
                for (int g = 0; g < waveEvent.groups.Count; g++)
                {
                    WaveSpawnGroup group = waveEvent.groups[g];
                    if (group == null || group.prefab == null || group.count <= 0)
                    {
                        continue;
                    }

                    int spawnCount = AdjustSpawnCount(wave, group, waveIndex, false, group.count);
                    float baseInterval = group.spawnIntervalOverride > 0f ? group.spawnIntervalOverride : interval;
                    float groupInterval = AdjustSpawnInterval(wave, group, waveIndex, false, baseInterval);
                    for (int i = 0; i < spawnCount; i++)
                    {
                        SpawnEnemyAtPosition(group.prefab, waveIndex, false, GetReinforcementPosition(wave, waveEvent), null, group.archetypeOverride);
                        runtime.baseAlive++;
                        runtime.totalAlive++;

                        if (groupInterval > 0f)
                        {
                            yield return new WaitForSeconds(groupInterval);
                        }
                    }
                }
            }

            eventRuntime.completed = true;
        }

        private IEnumerator ChaseEventRoutine(int waveIndex, int eventIndex)
        {
            StrongholdWave wave = waves[waveIndex];
            WaveRuntime runtime = runtimes[waveIndex];
            WaveEvent waveEvent = wave.events[eventIndex];
            EventRuntime eventRuntime = runtime.eventRuntimes[eventIndex];

            if (waveEvent.triggerDelay > 0f)
            {
                yield return new WaitForSeconds(waveEvent.triggerDelay);
            }

            eventRuntime.triggered = true;
            float duration = Mathf.Max(0.1f, waveEvent.duration);
            float interval = Mathf.Max(0.05f, waveEvent.spawnInterval);
            float timer = 0f;

            while (timer < duration && IsWaveActive(waveIndex))
            {
                if (waveEvent.groups != null)
                {
                    for (int g = 0; g < waveEvent.groups.Count; g++)
                    {
                        WaveSpawnGroup group = waveEvent.groups[g];
                        if (group == null || group.prefab == null || group.count <= 0)
                        {
                            continue;
                        }

                        int spawnCount = AdjustSpawnCount(wave, group, waveIndex, false, group.count);
                        for (int i = 0; i < spawnCount; i++)
                        {
                            SpawnEnemyAtPosition(group.prefab, waveIndex, false, GetChaseSpawnPosition(waveEvent), null, group.archetypeOverride);
                            runtime.baseAlive++;
                            runtime.totalAlive++;
                        }
                    }
                }

                yield return new WaitForSeconds(interval);
                timer += interval;
            }

            eventRuntime.completed = true;
        }

        private IEnumerator HoldPointEventRoutine(int waveIndex, int eventIndex)
        {
            StrongholdWave wave = waves[waveIndex];
            WaveRuntime runtime = runtimes[waveIndex];
            WaveEvent waveEvent = wave.events[eventIndex];
            EventRuntime eventRuntime = runtime.eventRuntimes[eventIndex];

            if (waveEvent.triggerDelay > 0f)
            {
                yield return new WaitForSeconds(waveEvent.triggerDelay);
            }

            eventRuntime.triggered = true;
            Vector3 holdCenter = waveEvent.holdPoint != null
                ? waveEvent.holdPoint.position
                : (center != null ? center.position : transform.position);

            if (waveEvent.showHoldMarker)
            {
                eventRuntime.holdMarker = CreateHoldMarker(holdCenter, waveEvent.holdRadius);
            }

            while (eventRuntime.holdProgress < waveEvent.holdDuration && IsWaveActive(waveIndex))
            {
                float delta = Time.deltaTime;
                if (player != null)
                {
                    float distance = Vector3.Distance(player.position, holdCenter);
                    if (distance <= waveEvent.holdRadius)
                    {
                        eventRuntime.holdProgress += delta;
                    }
                    else if (waveEvent.holdDecayRate > 0f)
                    {
                        eventRuntime.holdProgress = Mathf.Max(0f, eventRuntime.holdProgress - waveEvent.holdDecayRate * delta);
                    }
                }

                yield return null;
            }

            if (eventRuntime.holdMarker != null)
            {
                Destroy(eventRuntime.holdMarker);
                eventRuntime.holdMarker = null;
            }

            eventRuntime.completed = true;
        }

        private IEnumerator ProtectTargetEventRoutine(int waveIndex, int eventIndex)
        {
            StrongholdWave wave = waves[waveIndex];
            WaveRuntime runtime = runtimes[waveIndex];
            WaveEvent waveEvent = wave.events[eventIndex];
            EventRuntime eventRuntime = runtime.eventRuntimes[eventIndex];

            if (waveEvent.triggerDelay > 0f)
            {
                yield return new WaitForSeconds(waveEvent.triggerDelay);
            }

            if (!IsWaveActive(waveIndex))
            {
                yield break;
            }

            eventRuntime.triggered = true;
            DefenseTarget target = ResolveDefenseTarget(waveIndex, waveEvent, eventRuntime);
            if (target == null)
            {
                eventRuntime.completed = true;
                yield break;
            }

            if (waveEvent.failOnTargetDestroyed)
            {
                target.OnDestroyed += HandleDefenseTargetDestroyed;
            }

            if (waveEvent.groups != null && waveEvent.groups.Count > 0)
            {
                float interval = waveEvent.spawnInterval > 0f ? waveEvent.spawnInterval : wave.spawnInterval;
                for (int g = 0; g < waveEvent.groups.Count; g++)
                {
                    WaveSpawnGroup group = waveEvent.groups[g];
                    if (group == null || group.prefab == null || group.count <= 0)
                    {
                        continue;
                    }

                    int spawnCount = AdjustSpawnCount(wave, group, waveIndex, false, group.count);
                    float baseInterval = group.spawnIntervalOverride > 0f ? group.spawnIntervalOverride : interval;
                    float groupInterval = AdjustSpawnInterval(wave, group, waveIndex, false, baseInterval);
                    for (int i = 0; i < spawnCount; i++)
                    {
                        SpawnEnemyAtPosition(group.prefab, waveIndex, false, GetReinforcementPosition(wave, waveEvent),
                            waveEvent.assignTargetToSpawnedEnemies ? target.transform : null, group.archetypeOverride);
                        runtime.baseAlive++;
                        runtime.totalAlive++;

                        if (groupInterval > 0f)
                        {
                            yield return new WaitForSeconds(groupInterval);
                        }
                    }
                }
            }

            float surviveDuration = waveEvent.holdDuration > 0f ? waveEvent.holdDuration : waveEvent.duration;
            if (surviveDuration <= 0f)
            {
                surviveDuration = 6f;
            }

            float timer = 0f;
            while (timer < surviveDuration && IsWaveActive(waveIndex))
            {
                if (target.IsDestroyed)
                {
                    if (waveEvent.failOnTargetDestroyed)
                    {
                        FailStronghold("Defense target destroyed!");
                    }
                    break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            if (waveEvent.failOnTargetDestroyed)
            {
                target.OnDestroyed -= HandleDefenseTargetDestroyed;
            }

            eventRuntime.completed = true;
        }

        private void CheckEliteTrigger(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waves.Count)
            {
                return;
            }

            StrongholdWave wave = waves[waveIndex];
            WaveRuntime runtime = runtimes[waveIndex];

            if (!wave.eliteTrigger.enabled || runtime.eliteTriggered)
            {
                return;
            }

            int triggerRemaining = wave.eliteTrigger.triggerOnRemaining;
            if (spawnDirector != null)
            {
                triggerRemaining = spawnDirector.AdjustEliteTriggerRemaining(this, wave, waveIndex, triggerRemaining);
            }

            if (runtime.baseAlive <= triggerRemaining)
            {
                runtime.eliteTriggered = true;
                if (wave.eliteTrigger.eliteGroups == null || wave.eliteTrigger.eliteGroups.Count == 0)
                {
                    return;
                }

                StartCoroutine(SpawnEliteRoutine(waveIndex));
            }
        }

        private IEnumerator SpawnEliteRoutine(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waves.Count)
            {
                yield break;
            }

            StrongholdWave wave = waves[waveIndex];
            WaveRuntime runtime = runtimes[waveIndex];
            runtime.eliteSpawnPending = true;

            if (wave.eliteTrigger.triggerDelay > 0f)
            {
                yield return new WaitForSeconds(wave.eliteTrigger.triggerDelay);
            }

            float interval = wave.eliteTrigger.spawnInterval > 0f ? wave.eliteTrigger.spawnInterval : wave.spawnInterval;
            for (int g = 0; g < wave.eliteTrigger.eliteGroups.Count; g++)
            {
                WaveSpawnGroup group = wave.eliteTrigger.eliteGroups[g];
                if (group.prefab == null || group.count <= 0)
                {
                    continue;
                }

                int spawnCount = AdjustSpawnCount(wave, group, waveIndex, true, group.count);
                float baseInterval = group.spawnIntervalOverride > 0f ? group.spawnIntervalOverride : interval;
                float groupInterval = AdjustSpawnInterval(wave, group, waveIndex, true, baseInterval);
                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnEnemy(group.prefab, waveIndex, true, group.archetypeOverride);
                    runtime.totalAlive++;

                    if (groupInterval > 0f)
                    {
                        yield return new WaitForSeconds(groupInterval);
                    }
                }
            }

            runtime.eliteSpawnPending = false;
        }

        private bool IsWaveComplete(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= runtimes.Count)
            {
                return true;
            }

            WaveRuntime runtime = runtimes[waveIndex];
            return runtime.spawnComplete
                && runtime.totalAlive <= 0
                && !runtime.eliteSpawnPending
                && AreWaveEventsComplete(waveIndex);
        }

        private bool AreWaveEventsComplete(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= runtimes.Count || waveIndex >= waves.Count)
            {
                return true;
            }

            StrongholdWave wave = waves[waveIndex];
            WaveRuntime runtime = runtimes[waveIndex];
            if (wave == null || wave.events == null || wave.events.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < wave.events.Count; i++)
            {
                WaveEvent waveEvent = wave.events[i];
                if (waveEvent == null || !waveEvent.enabled)
                {
                    continue;
                }

                if (i >= runtime.eventRuntimes.Count)
                {
                    return false;
                }

                if (!runtime.eventRuntimes[i].completed)
                {
                    return false;
                }
            }

            return true;
        }

        private void CleanupWaveEvents(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= runtimes.Count)
            {
                return;
            }

            WaveRuntime runtime = runtimes[waveIndex];
            if (runtime.eventRuntimes == null)
            {
                return;
            }

            StrongholdWave wave = waveIndex >= 0 && waveIndex < waves.Count ? waves[waveIndex] : null;

            for (int i = 0; i < runtime.eventRuntimes.Count; i++)
            {
                EventRuntime eventRuntime = runtime.eventRuntimes[i];
                if (eventRuntime != null && eventRuntime.holdMarker != null)
                {
                    Destroy(eventRuntime.holdMarker);
                    eventRuntime.holdMarker = null;
                }

                if (eventRuntime != null && eventRuntime.defenseTarget != null)
                {
                    eventRuntime.defenseTarget.OnDestroyed -= HandleDefenseTargetDestroyed;
                    if (wave != null && wave.events != null && i < wave.events.Count)
                    {
                        WaveEvent waveEvent = wave.events[i];
                        if (waveEvent != null && waveEvent.spawnDefenseTarget && waveEvent.defenseTarget == null)
                        {
                            Destroy(eventRuntime.defenseTarget.gameObject);
                        }
                    }
                    eventRuntime.defenseTarget = null;
                }
            }
        }

        private void CleanupAllWaveEvents()
        {
            for (int i = 0; i < runtimes.Count; i++)
            {
                CleanupWaveEvents(i);
            }
        }

        private void SpawnEnemy(GameObject prefab, int waveIndex, bool isElite, EnemyArchetype archetypeOverride)
        {
            if (prefab == null)
            {
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition(waves[waveIndex]);
            SpawnEnemyAtPosition(prefab, waveIndex, isElite, spawnPosition, null, archetypeOverride);
        }

        private void SpawnEnemyAtPosition(GameObject prefab, int waveIndex, bool isElite, Vector3 spawnPosition, Transform targetOverride, EnemyArchetype archetypeOverride)
        {
            if (prefab == null)
            {
                return;
            }
            Quaternion rotation = Quaternion.identity;
            if (facePlayerOnSpawn && player != null)
            {
                Vector3 direction = player.position - spawnPosition;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    rotation = Quaternion.LookRotation(direction);
                }
            }

            GameObject enemy = usePooling
                ? ObjectPoolManager.Spawn(prefab, spawnPosition, rotation)
                : Instantiate(prefab, spawnPosition, rotation);
            EnemyWaveMember member = enemy.GetComponent<EnemyWaveMember>();
            if (member == null)
            {
                member = enemy.AddComponent<EnemyWaveMember>();
            }
            member.Initialize(this, waveIndex, isElite);

            if (archetypeOverride != null)
            {
                EnemyArchetypeConfigurator configurator = enemy.GetComponent<EnemyArchetypeConfigurator>();
                if (configurator == null)
                {
                    configurator = enemy.AddComponent<EnemyArchetypeConfigurator>();
                }
                configurator.ApplyArchetype(archetypeOverride);
            }

            if (targetOverride != null)
            {
                EnemyAI ai = enemy.GetComponent<EnemyAI>();
                if (ai != null)
                {
                    ai.SetOverrideTarget(targetOverride, true);
                }
            }
        }

        private bool IsWaveActive(int waveIndex)
        {
            return isRunning && currentWaveIndex == waveIndex;
        }

        private DefenseTarget ResolveDefenseTarget(int waveIndex, WaveEvent waveEvent, EventRuntime runtime)
        {
            if (waveEvent.defenseTarget != null)
            {
                DefenseTarget existing = waveEvent.defenseTarget;
                if (!string.IsNullOrEmpty(waveEvent.defenseTargetId))
                {
                    existing.defenseTargetId = waveEvent.defenseTargetId;
                }
                existing.ResetHealth(waveEvent.defenseTargetHealth);
                runtime.defenseTarget = existing;
                return existing;
            }

            if (!waveEvent.spawnDefenseTarget)
            {
                return null;
            }

            Vector3 position = waveEvent.holdPoint != null
                ? waveEvent.holdPoint.position
                : (center != null ? center.position : transform.position);

            DefenseTarget target = null;
            if (waveEvent.defenseTargetPrefab != null)
            {
                GameObject instance = Instantiate(waveEvent.defenseTargetPrefab, position, Quaternion.identity);
                target = instance.GetComponent<DefenseTarget>();
                if (target == null)
                {
                    target = instance.AddComponent<DefenseTarget>();
                }
            }
            else
            {
                GameObject instance = CreateDefenseTargetPrimitive(position, waveEvent.holdRadius);
                target = instance.GetComponent<DefenseTarget>();
            }

            if (target != null)
            {
                if (!string.IsNullOrEmpty(waveEvent.defenseTargetId))
                {
                    target.defenseTargetId = waveEvent.defenseTargetId;
                }
                else
                {
                    target.defenseTargetId = $"{StrongholdId}_W{waveIndex + 1}_{waveEvent.name}";
                }
                target.ResetHealth(waveEvent.defenseTargetHealth);
                runtime.defenseTarget = target;
            }

            return target;
        }

        private GameObject CreateDefenseTargetPrimitive(Vector3 position, float radius)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            target.name = "DefenseTarget";
            target.transform.position = position;
            target.transform.localScale = new Vector3(1.2f, 1.4f, 1.2f);

            Rigidbody rb = target.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            DefenseTarget defenseTarget = target.AddComponent<DefenseTarget>();
            defenseTarget.maxHealth = 200;
            defenseTarget.currentHealth = 200;

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.35f, 0.25f, 0.9f);
            }

            if (radius > 0f)
            {
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "DefenseTarget_Ring";
                ring.transform.SetParent(target.transform, false);
                ring.transform.localPosition = Vector3.zero;
                ring.transform.localScale = new Vector3(radius * 1.6f, 0.05f, radius * 1.6f);
                Collider ringCollider = ring.GetComponent<Collider>();
                if (ringCollider != null)
                {
                    Destroy(ringCollider);
                }
                Renderer ringRenderer = ring.GetComponent<Renderer>();
                if (ringRenderer != null)
                {
                    ringRenderer.material.color = new Color(1f, 0.2f, 0.2f, 0.25f);
                }
            }

            return target;
        }

        private void HandleDefenseTargetDestroyed(DefenseTarget target)
        {
            if (target != null)
            {
                target.OnDestroyed -= HandleDefenseTargetDestroyed;
            }

            if (isRunning)
            {
                FailStronghold("Defense target destroyed!");
            }
        }

        private void ResolveSpawnDirector()
        {
            if (!autoFindDirector || spawnDirector != null)
            {
                return;
            }

            if (sharedDirector == null)
            {
                sharedDirector = FindObjectOfType<WaveSpawnDirector>();
            }

            if (sharedDirector == null)
            {
                GameObject directorObject = new GameObject("WaveSpawnDirector");
                sharedDirector = directorObject.AddComponent<IntensityWaveDirector>();
            }

            spawnDirector = sharedDirector;
        }

        private int AdjustSpawnCount(StrongholdWave wave, WaveSpawnGroup group, int waveIndex, bool isElite, int baseCount)
        {
            if (spawnDirector == null)
            {
                return baseCount;
            }

            return spawnDirector.AdjustSpawnCount(this, wave, group, waveIndex, isElite, baseCount);
        }

        private float AdjustSpawnInterval(StrongholdWave wave, WaveSpawnGroup group, int waveIndex, bool isElite, float baseInterval)
        {
            if (spawnDirector == null)
            {
                return baseInterval;
            }

            return spawnDirector.AdjustSpawnInterval(this, wave, group, waveIndex, isElite, baseInterval);
        }

        private Vector3 GetSpawnPosition(StrongholdWave wave)
        {
            Vector3 basePosition = center != null ? center.position : transform.position;
            if (spawnPoints != null && spawnPoints.Count > 0)
            {
                Transform point = SelectSpawnPoint(wave);
                if (point != null)
                {
                    basePosition = point.position;
                }
            }
            else
            {
                Vector2 circle = Random.insideUnitCircle * spawnRadius;
                basePosition += new Vector3(circle.x, 0f, circle.y);
            }

            basePosition.y += spawnHeight;
            if (spawnPointJitter > 0f)
            {
                Vector2 jitter = Random.insideUnitCircle * spawnPointJitter;
                basePosition += new Vector3(jitter.x, 0f, jitter.y);
            }

            if (useGroundSnap)
            {
                if (Physics.Raycast(basePosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
                {
                    basePosition.y = hit.point.y + spawnHeight;
                }
            }

            return basePosition;
        }

        private Vector3 GetReinforcementPosition(StrongholdWave wave, WaveEvent waveEvent)
        {
            Vector3 basePosition = center != null ? center.position : transform.position;
            if (waveEvent.useReinforcementPoints && reinforcementPoints != null && reinforcementPoints.Count > 0)
            {
                Transform point = SelectReinforcementPoint(wave);
                if (point != null)
                {
                    basePosition = point.position;
                }
            }
            else if (spawnPoints != null && spawnPoints.Count > 0)
            {
                Transform point = SelectSpawnPoint(wave);
                if (point != null)
                {
                    basePosition = point.position;
                }
            }
            else
            {
                Vector2 circle = Random.insideUnitCircle * spawnRadius;
                basePosition += new Vector3(circle.x, 0f, circle.y);
            }

            basePosition = ApplySpawnOffset(basePosition);
            return basePosition;
        }

        private Vector3 GetChaseSpawnPosition(WaveEvent waveEvent)
        {
            Vector3 basePosition = player != null ? player.position : (center != null ? center.position : transform.position);
            float radius = Mathf.Max(1f, waveEvent.spawnRadius);
            Vector2 circle = Random.insideUnitCircle.normalized * radius;
            basePosition += new Vector3(circle.x, 0f, circle.y);
            basePosition = ApplySpawnOffset(basePosition);
            return basePosition;
        }

        private Vector3 ApplySpawnOffset(Vector3 basePosition)
        {
            basePosition.y += spawnHeight;
            if (spawnPointJitter > 0f)
            {
                Vector2 jitter = Random.insideUnitCircle * spawnPointJitter;
                basePosition += new Vector3(jitter.x, 0f, jitter.y);
            }

            if (useGroundSnap)
            {
                if (Physics.Raycast(basePosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
                {
                    basePosition.y = hit.point.y + spawnHeight;
                }
            }

            return basePosition;
        }

        private Transform SelectReinforcementPoint(StrongholdWave wave)
        {
            if (reinforcementPoints == null || reinforcementPoints.Count == 0)
            {
                return null;
            }

            if (wave != null && wave.shuffleSpawnPoints)
            {
                int index = Random.Range(0, reinforcementPoints.Count);
                return reinforcementPoints[index];
            }

            Transform point = reinforcementPoints[reinforcementPointCursor % reinforcementPoints.Count];
            reinforcementPointCursor++;
            return point;
        }

        private GameObject CreateHoldMarker(Vector3 position, float radius)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "HoldPoint";
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(radius * 2f, 0.15f, radius * 2f);

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 0.7f, 1f, 0.3f);
            }

            return marker;
        }

        private Transform SelectSpawnPoint(StrongholdWave wave)
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                return null;
            }

            if (wave.shuffleSpawnPoints)
            {
                int index = Random.Range(0, spawnPoints.Count);
                return spawnPoints[index];
            }

            Transform point = spawnPoints[spawnPointCursor % spawnPoints.Count];
            spawnPointCursor++;
            return point;
        }

        private void OnDrawGizmosSelected()
        {
            if (center == null)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.2f);
            Gizmos.DrawWireSphere(center.position, spawnRadius);
        }
    }
}
