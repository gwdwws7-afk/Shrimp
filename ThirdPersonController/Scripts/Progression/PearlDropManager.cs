using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [System.Serializable]
    public class PearlDropEntry
    {
        public PearlItem pearl;
        [Range(0f, 1f)]
        public float weight = 1f;
    }

    public class PearlDropManager : MonoBehaviour
    {
        public PearlInventory inventory;
        [Range(0f, 1f)]
        public float dropChance = 0.25f;
        public List<PearlDropEntry> dropTable = new List<PearlDropEntry>();

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = FindObjectOfType<PearlInventory>();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            if (inventory == null || dropTable == null || dropTable.Count == 0)
            {
                return;
            }

            if (Random.value > dropChance)
            {
                return;
            }

            PearlItem pearl = PickRandomPearl();
            if (pearl == null)
            {
                return;
            }

            inventory.AddPearl(pearl);
            GameEvents.ShowMessage($"Pearl acquired: {pearl.pearlName}", 2f);
        }

        private PearlItem PickRandomPearl()
        {
            float totalWeight = 0f;
            for (int i = 0; i < dropTable.Count; i++)
            {
                if (dropTable[i] != null && dropTable[i].pearl != null)
                {
                    totalWeight += Mathf.Max(0f, dropTable[i].weight);
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.value * totalWeight;
            float current = 0f;
            for (int i = 0; i < dropTable.Count; i++)
            {
                PearlDropEntry entry = dropTable[i];
                if (entry == null || entry.pearl == null)
                {
                    continue;
                }

                current += Mathf.Max(0f, entry.weight);
                if (roll <= current)
                {
                    return entry.pearl;
                }
            }

            return null;
        }
    }
}
