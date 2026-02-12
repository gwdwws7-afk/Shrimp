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

        private readonly HashSet<EnemyAI> activeAttackers = new HashSet<EnemyAI>();
        private readonly Dictionary<EnemyAI, int> slotMap = new Dictionary<EnemyAI, int>();
        private int nextSlotIndex = 0;

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
        }

        public void Unregister(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return;
            }

            activeAttackers.Remove(enemy);
            slotMap.Remove(enemy);
        }

        public bool RequestAttackToken(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            if (activeAttackers.Contains(enemy))
            {
                return true;
            }

            if (activeAttackers.Count >= maxActiveAttackers)
            {
                return false;
            }

            activeAttackers.Add(enemy);
            return true;
        }

        public void ReleaseAttackToken(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return;
            }

            activeAttackers.Remove(enemy);
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
