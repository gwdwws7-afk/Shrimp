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
        public EnemyType[] restrictedToEnemyTypes;
    }

    [System.Serializable]
    public class EnemyDropConfig
    {
        public EnemyType enemyType;
        public float dropChance = 0.3f;
        public PearlRarity minRarity = PearlRarity.Common;
        public PearlRarity maxRarity = PearlRarity.Epic;
        public float legendaryBonus = 0.05f;
    }

    public class PearlDropManager : MonoBehaviour
    {
        public PearlInventory inventory;
        public PearlDatabase pearlDatabase;
        public GameObject pickupPrefab;
        [Range(0f, 1f)]
        public float dropChance = 0.25f;
        public List<PearlDropEntry> dropTable = new List<PearlDropEntry>();
        
        [Header("Enemy-specific Drops")]
        public bool useEnemyConfig = true;
        public List<EnemyDropConfig> enemyDropConfigs = new List<EnemyDropConfig>();

        [Header("Pickup Spawn")]
        public float spawnHeightOffset = 0.35f;
        public float scatterRadius = 0.45f;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = FindObjectOfType<PearlInventory>();
            }
            
            if (pearlDatabase == null)
            {
                pearlDatabase = Resources.Load<PearlDatabase>("PearlDatabase");
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
            if (inventory == null)
            {
                return;
            }

            float actualDropChance = dropChance;
            
            if (useEnemyConfig)
            {
                EnemyDropConfig config = GetEnemyConfig(type);
                if (config == null)
                {
                    return;
                }
                
                actualDropChance = config.dropChance;
                
                if (Random.value > actualDropChance)
                {
                    return;
                }
                
                PearlItem pearl = PickPearlByRarity(config.minRarity, config.maxRarity, config.legendaryBonus);
                if (pearl != null)
                {
                    SpawnPickup(pearl, position);
                }
            }
            else
            {
                if (dropTable == null || dropTable.Count == 0)
                {
                    return;
                }

                if (Random.value > actualDropChance)
                {
                    return;
                }

                PearlItem pearl = PickRandomPearl();
                if (pearl == null)
                {
                    return;
                }

                SpawnPickup(pearl, position);
            }
        }

        private EnemyDropConfig GetEnemyConfig(EnemyType type)
        {
            if (enemyDropConfigs == null)
            {
                return null;
            }
            
            for (int i = 0; i < enemyDropConfigs.Count; i++)
            {
                if (enemyDropConfigs[i] != null && enemyDropConfigs[i].enemyType == type)
                {
                    return enemyDropConfigs[i];
                }
            }
            
            return null;
        }

        private PearlItem PickPearlByRarity(PearlRarity min, PearlRarity max, float legendaryBonus)
        {
            if (pearlDatabase == null || pearlDatabase.pearls == null)
            {
                return PickRandomPearl();
            }
            
            List<PearlItem> validPearls = new List<PearlItem>();
            
            for (int i = 0; i < pearlDatabase.pearls.Count; i++)
            {
                PearlItem pearl = pearlDatabase.pearls[i];
                if (pearl == null) continue;
                
                if (pearl.rarity >= min && pearl.rarity <= max)
                {
                    validPearls.Add(pearl);
                }
            }
            
            if (validPearls.Count == 0)
            {
                return PickRandomPearl();
            }
            
            float totalWeight = 0f;
            for (int i = 0; i < validPearls.Count; i++)
            {
                float weight = validPearls[i].baseDropWeight;
                if (validPearls[i].rarity == PearlRarity.Legendary)
                {
                    weight *= legendaryBonus > 0 ? legendaryBonus * 10 : 1f;
                }
                totalWeight += weight;
            }
            
            float roll = Random.Range(0f, totalWeight);
            float current = 0f;
            
            for (int i = 0; i < validPearls.Count; i++)
            {
                float weight = validPearls[i].baseDropWeight;
                if (validPearls[i].rarity == PearlRarity.Legendary)
                {
                    weight *= legendaryBonus > 0 ? legendaryBonus * 10 : 1f;
                }
                
                current += weight;
                if (roll <= current)
                {
                    return validPearls[i];
                }
            }
            
            return validPearls[Random.Range(0, validPearls.Count)];
        }

        private void SpawnPickup(PearlItem pearl, Vector3 position)
        {
            Vector3 spawnPosition = position + Vector3.up * spawnHeightOffset;
            if (scatterRadius > 0f)
            {
                Vector2 scatter = Random.insideUnitCircle * scatterRadius;
                spawnPosition += new Vector3(scatter.x, 0f, scatter.y);
            }

            GameObject pickupObject = null;
            if (pickupPrefab != null)
            {
                pickupObject = Object.Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                pickupObject = CreateRuntimePickup(spawnPosition);
            }

            if (pickupObject == null)
            {
                if (inventory != null)
                {
                    inventory.AddPearl(pearl);
                }
                GameEvents.ShowMessage($"Pearl acquired: {pearl.pearlName}", 2f);
                return;
            }

            PearlPickup pickup = pickupObject.GetComponent<PearlPickup>();
            if (pickup == null)
            {
                pickup = pickupObject.AddComponent<PearlPickup>();
            }

            pickup.Initialize(pearl, inventory);
        }

        private GameObject CreateRuntimePickup(Vector3 position)
        {
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickupObject.name = "PearlPickup";
            pickupObject.transform.position = position;
            pickupObject.transform.localScale = Vector3.one * 0.35f;

            SphereCollider collider = pickupObject.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Rigidbody rb = pickupObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            return pickupObject;
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
