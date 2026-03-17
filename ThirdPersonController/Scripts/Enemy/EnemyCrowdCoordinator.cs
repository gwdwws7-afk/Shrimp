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

        [Header("Layered Ring (P2)")]
        public bool separateMeleeAndRangedLayers = true;
        [Min(0.25f)] public float meleeRingRadiusMultiplier = 0.9f;
        [Min(0.5f)] public float rangedRingRadiusMultiplier = 1.3f;
        [Min(0f)] public float overflowRadiusStep = 0.45f;

        [Header("Slot Conflict Avoidance (P2)")]
        public bool avoidSlotConflicts = true;

        [Header("Attack Token Scaling")]
        public bool scaleAttackersWithNearby = true;
        public int maxActiveAttackersCap = 8;
        public int attackersPerNearbyEnemy = 6;

        [Header("Fair Token Queue (P2)")]
        public bool useFairTokenQueue = true;
        [Min(1)] public int maxQueueGrantsPerTick = 8;
        [Min(0f)] public float queueRequestFreshWindow = 0.75f;

        [Header("Crowd Sampling")]
        public float nearbyCountRadius = 12f;
        public float nearbyCountInterval = 0.5f;

        [Header("Debug (Runtime)")]
        [SerializeField] private int debugTokenRequests = 0;
        [SerializeField] private int debugTokenGranted = 0;
        [SerializeField] private int debugTokenRejected = 0;
        [SerializeField] private int debugTokenReleases = 0;
        [SerializeField] private int debugQueuedAttackers = 0;

        private readonly HashSet<EnemyAI> activeAttackers = new HashSet<EnemyAI>();
        private readonly Dictionary<EnemyAI, int> slotMap = new Dictionary<EnemyAI, int>();
        private readonly List<EnemyAI> registeredEnemies = new List<EnemyAI>();
        private readonly List<EnemyAI> pruneBuffer = new List<EnemyAI>();
        private readonly HashSet<int> occupiedSlotBuffer = new HashSet<int>();

        private readonly Queue<EnemyAI> tokenWaitQueue = new Queue<EnemyAI>();
        private readonly HashSet<EnemyAI> queuedAttackers = new HashSet<EnemyAI>();
        private readonly Dictionary<EnemyAI, float> queuedRequestTimes = new Dictionary<EnemyAI, float>();

        private int nextSlotIndex = 0;
        private int nearbyEnemyCount = 0;
        private float nextCountTime = 0f;

        public int NearbyEnemyCount => nearbyEnemyCount;
        public int ActiveAttackersCount => activeAttackers.Count;
        public int EffectiveMaxAttackers => GetEffectiveMaxAttackers();
        public int WaitingAttackersCount => queuedAttackers.Count;
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
            FillTokensFromQueue();
            debugQueuedAttackers = queuedAttackers.Count;
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
            RemoveFromQueue(enemy);
            queuedRequestTimes.Remove(enemy);
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
            Register(enemy);
            if (useFairTokenQueue)
            {
                queuedRequestTimes[enemy] = Time.time;
            }
            else
            {
                queuedRequestTimes.Remove(enemy);
            }

            if (activeAttackers.Contains(enemy))
            {
                debugTokenGranted++;
                return true;
            }

            if (useFairTokenQueue)
            {
                EnqueueForToken(enemy);
                FillTokensFromQueue();
                if (activeAttackers.Contains(enemy))
                {
                    return true;
                }

                debugTokenRejected++;
                return false;
            }

            int effectiveMax = GetEffectiveMaxAttackers();
            if (activeAttackers.Count >= effectiveMax)
            {
                debugTokenRejected++;
                return false;
            }

            GrantToken(enemy);
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
                queuedRequestTimes.Remove(enemy);
                FillTokensFromQueue();
            }
        }

        public Vector3 GetRingPosition(EnemyAI enemy)
        {
            if (player == null || enemy == null)
            {
                return enemy != null ? enemy.transform.position : Vector3.zero;
            }

            int slotIndex = GetEnemySlot(enemy);
            bool rangedLayer = separateMeleeAndRangedLayers && IsRangedCombatant(enemy);
            int resolvedSlot = ResolveSlot(enemy, slotIndex, rangedLayer);
            slotMap[enemy] = resolvedSlot;

            float angle = GetSlotAngle(enemy, resolvedSlot, rangedLayer);
            float radius = GetRingRadiusForEnemy(enemy, rangedLayer);

            int occupancyOrder = GetOccupancyOrder(enemy, resolvedSlot, rangedLayer);
            if (occupancyOrder > 0)
            {
                radius += Mathf.Max(0f, overflowRadiusStep) * occupancyOrder;
            }

            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 offset = rotation * Vector3.forward * radius;
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
                if (!IsEnemyValid(enemy))
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
                    RemoveFromQueue(enemy);
                    queuedRequestTimes.Remove(enemy);
                }
            }

            PruneWaitingQueue();
        }

        private void FillTokensFromQueue()
        {
            if (!useFairTokenQueue || tokenWaitQueue.Count == 0)
            {
                return;
            }

            PruneWaitingQueue();

            int grantedThisTick = 0;
            int grantBudget = Mathf.Max(1, maxQueueGrantsPerTick);
            while (tokenWaitQueue.Count > 0 && grantedThisTick < grantBudget)
            {
                if (activeAttackers.Count >= GetEffectiveMaxAttackers())
                {
                    break;
                }

                EnemyAI candidate = tokenWaitQueue.Dequeue();
                if (candidate == null)
                {
                    continue;
                }

                queuedAttackers.Remove(candidate);

                if (!IsEnemyValid(candidate) || activeAttackers.Contains(candidate) || !IsQueueRequestFresh(candidate))
                {
                    queuedRequestTimes.Remove(candidate);
                    continue;
                }

                GrantToken(candidate);
                grantedThisTick++;
            }
        }

        private void GrantToken(EnemyAI enemy)
        {
            if (enemy == null || activeAttackers.Contains(enemy))
            {
                return;
            }

            activeAttackers.Add(enemy);
            RemoveFromQueue(enemy);
            queuedRequestTimes.Remove(enemy);
            debugTokenGranted++;
        }

        private void EnqueueForToken(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (!queuedAttackers.Add(enemy))
            {
                return;
            }

            queuedRequestTimes[enemy] = Time.time;
            tokenWaitQueue.Enqueue(enemy);
        }

        private void RemoveFromQueue(EnemyAI enemy)
        {
            if (enemy == null || !queuedAttackers.Remove(enemy))
            {
                return;
            }

            queuedRequestTimes.Remove(enemy);
            if (tokenWaitQueue.Count == 0)
            {
                return;
            }

            int count = tokenWaitQueue.Count;
            for (int i = 0; i < count; i++)
            {
                EnemyAI queuedEnemy = tokenWaitQueue.Dequeue();
                if (queuedEnemy == null || queuedEnemy == enemy)
                {
                    continue;
                }

                if (queuedAttackers.Contains(queuedEnemy))
                {
                    tokenWaitQueue.Enqueue(queuedEnemy);
                }
            }
        }

        private void PruneWaitingQueue()
        {
            if (tokenWaitQueue.Count == 0)
            {
                queuedAttackers.Clear();
                queuedRequestTimes.Clear();
                return;
            }

            int count = tokenWaitQueue.Count;
            for (int i = 0; i < count; i++)
            {
                EnemyAI queuedEnemy = tokenWaitQueue.Dequeue();
                if (queuedEnemy == null)
                {
                    continue;
                }

                if (!queuedAttackers.Contains(queuedEnemy))
                {
                    continue;
                }

                if (!IsEnemyValid(queuedEnemy) || activeAttackers.Contains(queuedEnemy) || !IsQueueRequestFresh(queuedEnemy))
                {
                    queuedAttackers.Remove(queuedEnemy);
                    queuedRequestTimes.Remove(queuedEnemy);
                    continue;
                }

                tokenWaitQueue.Enqueue(queuedEnemy);
            }
        }

        private int ResolveSlot(EnemyAI enemy, int preferredSlot, bool rangedLayer)
        {
            int normalizedPreferred = NormalizeSlot(preferredSlot);
            if (!avoidSlotConflicts || ringSlots <= 1)
            {
                return normalizedPreferred;
            }

            BuildOccupiedSlots(enemy, rangedLayer);
            if (!occupiedSlotBuffer.Contains(normalizedPreferred))
            {
                return normalizedPreferred;
            }

            for (int step = 1; step < ringSlots; step++)
            {
                int plus = (normalizedPreferred + step) % ringSlots;
                if (!occupiedSlotBuffer.Contains(plus))
                {
                    return plus;
                }

                int minus = (normalizedPreferred - step + ringSlots) % ringSlots;
                if (!occupiedSlotBuffer.Contains(minus))
                {
                    return minus;
                }
            }

            return normalizedPreferred;
        }

        private void BuildOccupiedSlots(EnemyAI excludeEnemy, bool rangedLayer)
        {
            occupiedSlotBuffer.Clear();
            for (int i = 0; i < registeredEnemies.Count; i++)
            {
                EnemyAI other = registeredEnemies[i];
                if (other == null || other == excludeEnemy || !other.isActiveAndEnabled)
                {
                    continue;
                }

                if (separateMeleeAndRangedLayers && IsRangedCombatant(other) != rangedLayer)
                {
                    continue;
                }

                occupiedSlotBuffer.Add(GetEnemySlot(other));
            }
        }

        private int GetEnemySlot(EnemyAI enemy)
        {
            if (!slotMap.TryGetValue(enemy, out int slotIndex))
            {
                slotIndex = GetNextSlot();
                slotMap[enemy] = slotIndex;
            }

            return NormalizeSlot(slotIndex);
        }

        private int NormalizeSlot(int slot)
        {
            if (ringSlots <= 0)
            {
                return slot;
            }

            int normalized = slot % ringSlots;
            if (normalized < 0)
            {
                normalized += ringSlots;
            }

            return normalized;
        }

        private float GetSlotAngle(EnemyAI enemy, int slotIndex, bool rangedLayer)
        {
            float angle;
            if (ringSlots > 0)
            {
                float angleStep = 360f / Mathf.Max(1, ringSlots);
                angle = angleStep * NormalizeSlot(slotIndex);
                if (separateMeleeAndRangedLayers && rangedLayer && ringSlots > 1)
                {
                    angle += angleStep * 0.5f;
                }
            }
            else
            {
                angle = Mathf.Abs(enemy.GetInstanceID()) % 360;
            }

            if (ringJitter > 0f)
            {
                angle += ringJitter * GetStableSignedJitter(enemy);
            }

            return angle;
        }

        private float GetRingRadiusForEnemy(EnemyAI enemy, bool rangedLayer)
        {
            float radius = Mathf.Max(0.6f, ringRadius);
            if (!separateMeleeAndRangedLayers)
            {
                return radius;
            }

            if (rangedLayer)
            {
                radius *= Mathf.Max(1f, rangedRingRadiusMultiplier);
                if (enemy != null)
                {
                    radius = Mathf.Max(radius, enemy.attackRange + 0.4f);
                }
            }
            else
            {
                radius *= Mathf.Max(0.25f, meleeRingRadiusMultiplier);
                if (enemy != null)
                {
                    radius = Mathf.Min(radius, Mathf.Max(0.8f, enemy.attackRange + 0.2f));
                }
            }

            return Mathf.Max(0.6f, radius);
        }

        private int GetOccupancyOrder(EnemyAI enemy, int slotIndex, bool rangedLayer)
        {
            if (enemy == null || ringSlots <= 0)
            {
                return 0;
            }

            int normalized = NormalizeSlot(slotIndex);
            int enemyId = enemy.GetInstanceID();
            int order = 0;
            bool foundSelf = false;

            for (int i = 0; i < registeredEnemies.Count; i++)
            {
                EnemyAI other = registeredEnemies[i];
                if (!IsEnemyValid(other))
                {
                    continue;
                }

                if (separateMeleeAndRangedLayers && IsRangedCombatant(other) != rangedLayer)
                {
                    continue;
                }

                if (GetEnemySlot(other) != normalized)
                {
                    continue;
                }

                if (other == enemy)
                {
                    foundSelf = true;
                    continue;
                }

                if (other.GetInstanceID() < enemyId)
                {
                    order++;
                }
            }

            return foundSelf ? Mathf.Max(0, order) : 0;
        }

        private bool IsRangedCombatant(EnemyAI enemy)
        {
            return enemy != null && enemy.PrefersRangedRingLayer();
        }

        private bool IsEnemyValid(EnemyAI enemy)
        {
            return enemy != null && enemy.isActiveAndEnabled;
        }

        private bool IsQueueRequestFresh(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            if (queueRequestFreshWindow <= 0f)
            {
                return true;
            }

            if (!queuedRequestTimes.TryGetValue(enemy, out float requestedAt))
            {
                return false;
            }

            return Time.time - requestedAt <= queueRequestFreshWindow;
        }

        private static float GetStableSignedJitter(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return 0f;
            }

            int hash = enemy.GetInstanceID();
            hash = (hash ^ 61) ^ (hash >> 16);
            hash += hash << 3;
            hash ^= hash >> 4;
            hash *= 668265261;
            hash ^= hash >> 15;

            float normalized = (hash & 0x7fffffff) / (float)int.MaxValue;
            return normalized * 2f - 1f;
        }

        private int GetNextSlot()
        {
            int slot = nextSlotIndex;
            if (ringSlots > 0)
            {
                nextSlotIndex = (nextSlotIndex + 1) % ringSlots;
                return NormalizeSlot(slot);
            }

            nextSlotIndex++;
            return slot;
        }
    }
}
