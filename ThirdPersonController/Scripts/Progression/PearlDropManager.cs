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
    public class ConsumableDropEntry
    {
        public string itemId = "";
        [Range(0f, 1f)]
        public float weight = 1f;
        public int minCount = 1;
        public int maxCount = 1;
        public EnemyType[] restrictedToEnemyTypes;
    }

    [System.Serializable]
    public class CreditDropEntry
    {
        public int minCredits = 4;
        public int maxCredits = 8;
        [Range(0f, 1f)]
        public float weight = 1f;
        public EnemyType[] restrictedToEnemyTypes;
    }

    [System.Serializable]
    public class EnemyDropChanceConfig
    {
        public EnemyType enemyType;
        [Range(0f, 1f)]
        public float dropChance = 0.2f;
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

        [Header("Consumable Drops")]
        public bool enableConsumableDrops = true;
        public bool useConsumableConfig = false;
        public ConsumableInventory consumableInventory;
        public ConsumableCatalog consumableCatalog;
        [Range(0f, 1f)]
        public float consumableDropChance = 0.12f;
        public float consumableDropMultiplier = 1f;
        public bool autoPopulateConsumableTable = true;
        public List<ConsumableDropEntry> consumableDropTable = new List<ConsumableDropEntry>();
        public List<EnemyDropChanceConfig> consumableDropConfigs = new List<EnemyDropChanceConfig>();

        [Header("Credit Drops")]
        public bool enableCreditDrops = true;
        public bool useCreditConfig = false;
        public CurrencyWallet wallet;
        [Range(0f, 1f)]
        public float creditDropChance = 0.35f;
        public float creditDropMultiplier = 1f;
        public float creditAmountMultiplier = 1f;
        public bool autoPopulateCreditTable = true;
        public List<CreditDropEntry> creditDropTable = new List<CreditDropEntry>();
        public List<EnemyDropChanceConfig> creditDropConfigs = new List<EnemyDropChanceConfig>();

        [Header("Pickup Spawn")]
        public float spawnHeightOffset = 0.35f;
        public float scatterRadius = 0.45f;

        [Header("Progression Caps")]
        public bool useProgressionCaps = true;
        public PearlRarity maxRarityCap = PearlRarity.Epic;
        public float dropChanceMultiplier = 1f;
        public float economyDropMultiplier = 1f;
        public float levelDropMultiplier = 1f;
        public int levelDifficulty = 1;

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

            if (consumableInventory == null)
            {
                consumableInventory = ConsumableInventory.EnsureInstance();
            }

            if (consumableCatalog == null)
            {
                consumableCatalog = Resources.Load<ConsumableCatalog>("ConsumableCatalog")
                    ?? ConsumableCatalog.CreateDefault();
            }

            if (wallet == null)
            {
                wallet = CurrencyWallet.EnsureInstance();
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
            float difficultyMultiplier = EconomyService.GetDropChanceMultiplier(levelDifficulty);
            if (inventory != null)
            {
                float actualDropChance = dropChance;
                actualDropChance *= Mathf.Max(0f, dropChanceMultiplier)
                    * Mathf.Max(0f, economyDropMultiplier)
                    * Mathf.Max(0f, levelDropMultiplier)
                    * Mathf.Max(0f, difficultyMultiplier);

                if (useEnemyConfig)
                {
                    EnemyDropConfig config = GetEnemyConfig(type);
                    if (config != null)
                    {
                        actualDropChance = config.dropChance * Mathf.Max(0f, dropChanceMultiplier)
                            * Mathf.Max(0f, economyDropMultiplier)
                            * Mathf.Max(0f, levelDropMultiplier)
                            * Mathf.Max(0f, difficultyMultiplier);

                        if (Random.value <= actualDropChance)
                        {
                            PearlItem pearl = PickPearlByRarity(config.minRarity, ClampRarity(config.maxRarity), config.legendaryBonus);
                            if (pearl != null)
                            {
                                SpawnPickup(pearl, position);
                            }
                        }
                    }
                }
                else
                {
                    if (dropTable != null && dropTable.Count > 0)
                    {
                        if (Random.value <= actualDropChance)
                        {
                            PearlItem pearl = PickRandomPearl();
                            if (pearl != null)
                            {
                                SpawnPickup(pearl, position);
                            }
                        }
                    }
                }
            }

            TryDropConsumable(type, position, difficultyMultiplier);
            TryDropCredits(type, position, difficultyMultiplier);
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

        private EnemyDropChanceConfig GetChanceConfig(List<EnemyDropChanceConfig> configs, EnemyType type)
        {
            if (configs == null)
            {
                return null;
            }

            for (int i = 0; i < configs.Count; i++)
            {
                if (configs[i] != null && configs[i].enemyType == type)
                {
                    return configs[i];
                }
            }

            return null;
        }

        private PearlItem PickPearlByRarity(PearlRarity min, PearlRarity max, float legendaryBonus)
        {
            if (useProgressionCaps)
            {
                max = ClampRarity(max);
            }
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
                GameEvents.PearlCollected(pearl.GetId());
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

        private void TryDropConsumable(EnemyType type, Vector3 position, float difficultyMultiplier)
        {
            if (!enableConsumableDrops)
            {
                return;
            }

            if (consumableInventory == null)
            {
                consumableInventory = ConsumableInventory.EnsureInstance();
            }

            if (consumableCatalog == null)
            {
                consumableCatalog = Resources.Load<ConsumableCatalog>("ConsumableCatalog")
                    ?? ConsumableCatalog.CreateDefault();
            }

            EnsureConsumableTable();

            float chance = consumableDropChance;
            if (useConsumableConfig)
            {
                EnemyDropChanceConfig config = GetChanceConfig(consumableDropConfigs, type);
                if (config == null)
                {
                    return;
                }
                chance = config.dropChance;
            }

            float multiplier = Mathf.Max(0f, consumableDropMultiplier)
                * Mathf.Max(0f, levelDropMultiplier)
                * Mathf.Max(0f, difficultyMultiplier);
            chance *= multiplier;

            if (Random.value > chance)
            {
                return;
            }

            ConsumableDropEntry entry = PickConsumableEntry(type);
            if (entry == null)
            {
                return;
            }

            int minCount = Mathf.Max(1, entry.minCount);
            int maxCount = Mathf.Max(minCount, entry.maxCount);
            int count = Random.Range(minCount, maxCount + 1);
            if (count <= 0)
            {
                return;
            }

            if (consumableInventory.Add(entry.itemId, count))
            {
                ConsumableDefinition item = consumableCatalog != null ? consumableCatalog.GetById(entry.itemId) : null;
                string label = item != null ? item.displayName : entry.itemId;
                GameEvents.ShowMessage($"+{count} {label}", 1.4f);
            }
        }

        private void TryDropCredits(EnemyType type, Vector3 position, float difficultyMultiplier)
        {
            if (!enableCreditDrops)
            {
                return;
            }

            if (wallet == null)
            {
                wallet = CurrencyWallet.EnsureInstance();
            }

            EnsureCreditTable();

            float chance = creditDropChance;
            if (useCreditConfig)
            {
                EnemyDropChanceConfig config = GetChanceConfig(creditDropConfigs, type);
                if (config == null)
                {
                    return;
                }
                chance = config.dropChance;
            }

            float multiplier = Mathf.Max(0f, creditDropMultiplier)
                * Mathf.Max(0f, levelDropMultiplier)
                * Mathf.Max(0f, difficultyMultiplier);
            chance *= multiplier;

            if (Random.value > chance)
            {
                return;
            }

            CreditDropEntry entry = PickCreditEntry(type);
            if (entry == null)
            {
                return;
            }

            int minCredits = Mathf.Max(0, entry.minCredits);
            int maxCredits = Mathf.Max(minCredits, entry.maxCredits);
            int amount = Random.Range(minCredits, maxCredits + 1);
            amount = Mathf.Max(0, Mathf.RoundToInt(amount * Mathf.Max(0f, creditAmountMultiplier)));
            if (amount <= 0)
            {
                return;
            }

            if (wallet != null)
            {
                wallet.AddCredits(amount);
            }
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
                    if (useProgressionCaps && dropTable[i].pearl.rarity > maxRarityCap)
                    {
                        continue;
                    }
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

                if (useProgressionCaps && entry.pearl.rarity > maxRarityCap)
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

        private void EnsureConsumableTable()
        {
            if (!autoPopulateConsumableTable)
            {
                return;
            }

            if (consumableDropTable != null && consumableDropTable.Count > 0)
            {
                return;
            }

            if (consumableCatalog == null || consumableCatalog.items == null)
            {
                return;
            }

            consumableDropTable = new List<ConsumableDropEntry>();
            for (int i = 0; i < consumableCatalog.items.Count; i++)
            {
                ConsumableDefinition item = consumableCatalog.items[i];
                if (item == null || string.IsNullOrEmpty(item.id))
                {
                    continue;
                }

                consumableDropTable.Add(new ConsumableDropEntry
                {
                    itemId = item.id,
                    weight = 1f,
                    minCount = 1,
                    maxCount = 1
                });
            }
        }

        private void EnsureCreditTable()
        {
            if (!autoPopulateCreditTable)
            {
                return;
            }

            if (creditDropTable != null && creditDropTable.Count > 0)
            {
                return;
            }

            creditDropTable = new List<CreditDropEntry>
            {
                new CreditDropEntry
                {
                    minCredits = 4,
                    maxCredits = 8,
                    weight = 1f
                }
            };
        }

        private ConsumableDropEntry PickConsumableEntry(EnemyType type)
        {
            if (consumableDropTable == null || consumableDropTable.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < consumableDropTable.Count; i++)
            {
                ConsumableDropEntry entry = consumableDropTable[i];
                if (entry == null || string.IsNullOrEmpty(entry.itemId))
                {
                    continue;
                }

                if (!IsDropAllowedForType(entry.restrictedToEnemyTypes, type))
                {
                    continue;
                }

                if (consumableCatalog != null && consumableCatalog.GetById(entry.itemId) == null)
                {
                    continue;
                }

                totalWeight += Mathf.Max(0f, entry.weight);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.value * totalWeight;
            float current = 0f;
            for (int i = 0; i < consumableDropTable.Count; i++)
            {
                ConsumableDropEntry entry = consumableDropTable[i];
                if (entry == null || string.IsNullOrEmpty(entry.itemId))
                {
                    continue;
                }

                if (!IsDropAllowedForType(entry.restrictedToEnemyTypes, type))
                {
                    continue;
                }

                if (consumableCatalog != null && consumableCatalog.GetById(entry.itemId) == null)
                {
                    continue;
                }

                current += Mathf.Max(0f, entry.weight);
                if (roll <= current)
                {
                    return entry;
                }
            }

            return null;
        }

        private CreditDropEntry PickCreditEntry(EnemyType type)
        {
            if (creditDropTable == null || creditDropTable.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < creditDropTable.Count; i++)
            {
                CreditDropEntry entry = creditDropTable[i];
                if (entry == null)
                {
                    continue;
                }

                if (!IsDropAllowedForType(entry.restrictedToEnemyTypes, type))
                {
                    continue;
                }

                totalWeight += Mathf.Max(0f, entry.weight);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.value * totalWeight;
            float current = 0f;
            for (int i = 0; i < creditDropTable.Count; i++)
            {
                CreditDropEntry entry = creditDropTable[i];
                if (entry == null)
                {
                    continue;
                }

                if (!IsDropAllowedForType(entry.restrictedToEnemyTypes, type))
                {
                    continue;
                }

                current += Mathf.Max(0f, entry.weight);
                if (roll <= current)
                {
                    return entry;
                }
            }

            return null;
        }

        private bool IsDropAllowedForType(EnemyType[] restrictions, EnemyType type)
        {
            if (restrictions == null || restrictions.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < restrictions.Length; i++)
            {
                if (restrictions[i] == type)
                {
                    return true;
                }
            }

            return false;
        }

        public void ApplyProgressionCaps(PearlRarity maxRarity, float dropMultiplier)
        {
            maxRarityCap = maxRarity;
            dropChanceMultiplier = Mathf.Max(0.1f, dropMultiplier);
        }

        public void ApplyEconomyMultiplier(float dropMultiplier)
        {
            economyDropMultiplier = Mathf.Max(0.1f, dropMultiplier);
        }

        public void ApplyLevelContext(LevelData levelData)
        {
            if (levelData == null)
            {
                return;
            }

            levelDropMultiplier = Mathf.Max(0f, levelData.dropChanceMultiplier);
            levelDifficulty = Mathf.Max(0, (int)levelData.difficulty);
        }

        private PearlRarity ClampRarity(PearlRarity rarity)
        {
            if (!useProgressionCaps)
            {
                return rarity;
            }

            int capped = Mathf.Min((int)rarity, (int)maxRarityCap);
            return (PearlRarity)capped;
        }
    }
}
