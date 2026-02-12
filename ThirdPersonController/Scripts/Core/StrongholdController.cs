using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [System.Serializable]
    public class WaveSpawnGroup
    {
        public GameObject prefab;
        public int count = 5;
        public float spawnIntervalOverride = -1f;
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

    [System.Serializable]
    public class StrongholdWave
    {
        public string name = "Wave";
        public float startDelay = 0.4f;
        public float spawnInterval = 0.35f;
        public bool shuffleSpawnPoints = true;
        public List<WaveSpawnGroup> groups = new List<WaveSpawnGroup>();
        public WaveEliteTrigger eliteTrigger = new WaveEliteTrigger();
    }

    public class StrongholdController : MonoBehaviour
    {
        [Header("Activation")]
        public bool activeOnStart = true;
        public bool startOnPlayerEnter = true;
        public string playerTag = "Player";
        public Collider triggerArea;

        [Header("Spawn")]
        public Transform center;
        public List<Transform> spawnPoints = new List<Transform>();
        public float spawnRadius = 6f;
        public float spawnHeight = 0.5f;
        public float spawnPointJitter = 0.4f;
        public bool useGroundSnap = false;
        public LayerMask groundLayer = default;
        public bool facePlayerOnSpawn = true;
        public bool usePooling = true;

        [Header("Wave Timing")]
        public float waveCompleteDelay = 1f;

        [Header("Waves")]
        public List<StrongholdWave> waves = new List<StrongholdWave>();

        public event System.Action<StrongholdController> OnStrongholdStarted;
        public event System.Action<StrongholdController, int> OnWaveStarted;
        public event System.Action<StrongholdController, int> OnWaveCompleted;
        public event System.Action<StrongholdController> OnStrongholdCompleted;

        private class WaveRuntime
        {
            public int baseAlive;
            public int totalAlive;
            public bool spawnComplete;
            public bool eliteTriggered;
            public bool eliteSpawnPending;

            public void Reset()
            {
                baseAlive = 0;
                totalAlive = 0;
                spawnComplete = false;
                eliteTriggered = false;
                eliteSpawnPending = false;
            }
        }

        private readonly List<WaveRuntime> runtimes = new List<WaveRuntime>();
        private Transform player;
        private Coroutine strongholdRoutine;
        private int spawnPointCursor = 0;

        private bool isActive;
        private bool isRunning;
        private bool isCompleted;

        public bool IsActive => isActive;
        public bool IsRunning => isRunning;
        public bool IsCompleted => isCompleted;

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

            isRunning = false;
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
                runtimes.Add(new WaveRuntime());
            }
        }

        private IEnumerator StrongholdRoutine()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                OnWaveStarted?.Invoke(this, i);
                yield return StartCoroutine(SpawnWaveRoutine(i));

                yield return new WaitUntil(() => IsWaveComplete(i));
                OnWaveCompleted?.Invoke(this, i);

                if (waveCompleteDelay > 0f)
                {
                    yield return new WaitForSeconds(waveCompleteDelay);
                }
            }

            isRunning = false;
            isCompleted = true;
            OnStrongholdCompleted?.Invoke(this);
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

            if (wave.startDelay > 0f)
            {
                yield return new WaitForSeconds(wave.startDelay);
            }

            for (int g = 0; g < wave.groups.Count; g++)
            {
                WaveSpawnGroup group = wave.groups[g];
                if (group.prefab == null || group.count <= 0)
                {
                    continue;
                }

                float interval = group.spawnIntervalOverride > 0f ? group.spawnIntervalOverride : wave.spawnInterval;
                for (int i = 0; i < group.count; i++)
                {
                    SpawnEnemy(group.prefab, waveIndex, false);
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

            if (runtime.baseAlive <= wave.eliteTrigger.triggerOnRemaining)
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

                float groupInterval = group.spawnIntervalOverride > 0f ? group.spawnIntervalOverride : interval;
                for (int i = 0; i < group.count; i++)
                {
                    SpawnEnemy(group.prefab, waveIndex, true);
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
            return runtime.spawnComplete && runtime.totalAlive <= 0 && !runtime.eliteSpawnPending;
        }

        private void SpawnEnemy(GameObject prefab, int waveIndex, bool isElite)
        {
            if (prefab == null)
            {
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition(waves[waveIndex]);
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
