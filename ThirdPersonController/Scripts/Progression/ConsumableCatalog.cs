using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum ConsumableEffectType
    {
        HealFlat,
        HealPercent,
        RestoreStaminaFlat,
        RestoreStaminaPercent,
        DamageReduction,
        Invincibility
    }

    [System.Serializable]
    public class ConsumableDefinition
    {
        public string id = "";
        public string displayName = "Consumable";
        [TextArea(2, 3)]
        public string description;
        public ConsumableEffectType effectType = ConsumableEffectType.HealFlat;
        public float amount = 20f;
        public float duration = 0f;
        public int price = 50;
        public int maxStack = 99;
    }

    [CreateAssetMenu(fileName = "ConsumableCatalog", menuName = "Progression/Consumable Catalog")]
    public class ConsumableCatalog : ScriptableObject
    {
        public List<ConsumableDefinition> items = new List<ConsumableDefinition>();

        private Dictionary<string, ConsumableDefinition> lookup;

        public ConsumableDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            BuildLookup();
            if (lookup != null && lookup.TryGetValue(id, out ConsumableDefinition item))
            {
                return item;
            }

            return null;
        }

        public static ConsumableCatalog CreateDefault()
        {
            ConsumableCatalog catalog = CreateInstance<ConsumableCatalog>();
            catalog.items = new List<ConsumableDefinition>
            {
                new ConsumableDefinition
                {
                    id = "consumable_medkit",
                    displayName = "Medkit",
                    description = "Restore 40 HP.",
                    effectType = ConsumableEffectType.HealFlat,
                    amount = 40f,
                    price = 60,
                    maxStack = 20
                },
                new ConsumableDefinition
                {
                    id = "consumable_stamina",
                    displayName = "Stamina Gel",
                    description = "Restore 40 stamina.",
                    effectType = ConsumableEffectType.RestoreStaminaFlat,
                    amount = 40f,
                    price = 50,
                    maxStack = 20
                },
                new ConsumableDefinition
                {
                    id = "consumable_aegis",
                    displayName = "Aegis Shell",
                    description = "Reduce damage by 30% for 6 seconds.",
                    effectType = ConsumableEffectType.DamageReduction,
                    amount = 0.3f,
                    duration = 6f,
                    price = 90,
                    maxStack = 10
                }
            };
            return catalog;
        }

        private void BuildLookup()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, ConsumableDefinition>();
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                ConsumableDefinition item = items[i];
                if (item == null || string.IsNullOrEmpty(item.id))
                {
                    continue;
                }

                if (!lookup.ContainsKey(item.id))
                {
                    lookup.Add(item.id, item);
                }
            }
        }
    }
}
