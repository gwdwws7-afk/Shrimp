using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class EnemyCrowdCoordinator : MonoBehaviour
    {
        [Header("Target")]
        public Transform player;

        [Header("Attack Slots")]
        public int maxActiveAttackers = 3;
        public int ringSlots = 8;
        public float ringRadius = 2.6f;
        public float ringJitter = 0.4f;

        [Header("Attack Token Scaling")]
        public bool scaleAttackersWithNearby = true;
        public int maxActiveAttackersCap = 8;
        public int attackersPerNearbyEnemy = 6;

        [Header("Crowd Sampling")]
        public float nearbyCountRadius = 12f;
        public float nearbyCountInterval = 0.5f;

        [Header("Debug (Runtime)")]
        [SerializeField] private int debugTokenRequests = 0;
        [SerializeField] private int debugTokenGranted = 0;
        [SerializeField] private int debugTokenRejected = 0;
        [SerializeField] private int debugTokenReleases = 0;

        private readonly HashSet<EnemyAI> activeAttackers = new HashSet<EnemyAI>();
        private readonly Dictionary<EnemyAI, int> slotMap = new Dictionary<EnemyAI, int>();
        private readonly List<EnemyAI> registeredEnemies = new List<EnemyAI>();
        private readonly List<EnemyAI> pruneBuffer = new List<EnemyAI>();
        private int nextSlotIndex = 0;
        private int nearbyEnemyCount = 0;
        private float nextCountTime = 0f;

        public int NearbyEnemyCount => nearbyEnemyCount;
        public int ActiveAttackersCount => activeAttackers.Count;
        public int EffectiveMaxAttackers => GetEffectiveMaxAttackers();
        public float TokenUtilization => EffectiveMaxAttackers > 0
            ? (float)activeAttackers.Count / EffectiveMaxAttackers
            : 0f;
        public int TokenRequestCount => debugTokenRequests;
        public int TokenGrantedCount => debugTokenGranted;
        public int TokenRejectedCount => debugTokenRejected;
        public int TokenReleaseCount => debugTokenReleases;

        private void Awake()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    player = playerObject.transform;
                }
            }
        }

        private void Update()
        {
            PruneInactiveReferences();
            UpdateNearbyCount();
        }

        public void Register(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (!slotMap.ContainsKey(enemy))
            {
                slotMap[enemy] = GetNextSlot();
            }

            if (!registeredEnemies.Contains(enemy))
            {
                registeredEnemies.Add(enemy);
            }
        }

        public void Unregister(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return;
            }

            activeAttackers.Remove(enemy);
            slotMap.Remove(enemy);
            registeredEnemies.Remove(enemy);
        }

        public bool RequestAttackToken(EnemyAI enemy)
        {
            debugTokenRequests++;
            if (enemy == null)
            {
                debugTokenRejected++;
                return false;
            }

            PruneInactiveReferences();
            if (activeAttackers.Contains(enemy))
            {
                debugTokenGranted++;
                return true;
            }

            int effectiveMax = GetEffectiveMaxAttackers();
            if (activeAttackers.Count >= effectiveMax)
            {
                debugTokenRejected++;
                return false;
            }

            activeAttackers.Add(enemy);
            debugTokenGranted++;
            return true;
        }

        public void ReleaseAttackToken(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (activeAttackers.Remove(enemy))
            {
                debugTokenReleases++;
            }
        }

        public Vector3 GetRingPosition(EnemyAI enemy)
        {
            if (player == null || enemy == null)
            {
                return enemy != null ? enemy.transform.position : Vector3.zero;
            }

            if (!slotMap.TryGetValue(enemy, out int slotIndex))
            {
                slotIndex = GetNextSlot();
                slotMap[enemy] = slotIndex;
            }

            float angleStep = ringSlots > 0 ? 360f / ringSlots : 360f;
            float angle = angleStep * slotIndex;
            float jitter = ringJitter > 0f ? Random.Range(-ringJitter, ringJitter) : 0f;
            Quaternion rotation = Quaternion.Euler(0f, angle + jitter, 0f);
            Vector3 offset = rotation * Vector3.forward * ringRadius;

            return player.position + offset;
        }

        private void UpdateNearbyCount()
        {
            if (nearbyCountInterval <= 0f)
            {
                return;
            }

            if (Time.time < nextCountTime)
            {
                return;
            }

            nextCountTime = Time.time + Mathf.Max(0.05f, nearbyCountInterval);

            if (player == null)
            {
                nearbyEnemyCount = 0;
                return;
            }

            float radiusSqr = nearbyCountRadius * nearbyCountRadius;
            int count = 0;
            for (int i = registeredEnemies.Count - 1; i >= 0; i--)
            {
                EnemyAI enemy = registeredEnemies[i];
                if (enemy == null)
                {
                    registeredEnemies.RemoveAt(i);
                    continue;
                }

                if (!enemy.isActiveAndEnabled)
                {
                    continue;
                }

                Vector3 diff = enemy.transform.position - player.position;
                if (diff.sqrMagnitude <= radiusSqr)
                {
                    count++;
                }
            }

            nearbyEnemyCount = count;
        }

        private int GetEffectiveMaxAttackers()
        {
            if (!scaleAttackersWithNearby)
            {
                return Mathf.Max(1, maxActiveAttackers);
            }

            int cap = maxActiveAttackersCap > 0 ? maxActiveAttackersCap : maxActiveAttackers;
            int baseMax = Mathf.Max(1, maxActiveAttackers);
            int extra = 0;
            if (attackersPerNearbyEnemy > 0 && nearbyEnemyCount > 0)
            {
                extra = nearbyEnemyCount / attackersPerNearbyEnemy;
            }

            return Mathf.Clamp(baseMax + extra, baseMax, Mathf.Max(baseMax, cap));
        }

        private void PruneInactiveReferences()
        {
            pruneBuffer.Clear();
            foreach (EnemyAI enemy in activeAttackers)
            {
                if (enemy == null || !enemy.isActiveAndEnabled)
                {
                    pruneBuffer.Add(enemy);
                }
            }

            for (int i = 0; i < pruneBuffer.Count; i++)
            {
                activeAttackers.Remove(pruneBuffer[i]);
            }

            for (int i = registeredEnemies.Count - 1; i >= 0; i--)
            {
                EnemyAI enemy = registeredEnemies[i];
                if (enemy == null)
                {
                    registeredEnemies.RemoveAt(i);
                    continue;
                }

                if (!enemy.isActiveAndEnabled)
                {
                    activeAttackers.Remove(enemy);
                }
            }
        }

        private int GetNextSlot()
        {
            int slot = nextSlotIndex;
            if (ringSlots > 0)
            {
                nextSlotIndex = (nextSlotIndex + 1) % ringSlots;
            }
            else
            {
                nextSlotIndex++;
            }

            return slot;
        }
    }
}
