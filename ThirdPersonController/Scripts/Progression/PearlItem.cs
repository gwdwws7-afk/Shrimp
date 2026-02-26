using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum PearlRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum PearlType
    {
        Attack,
        Defense,
        Utility,
        Special
    }

    [CreateAssetMenu(fileName = "Pearl_", menuName = "Progression/Pearl")]
    public class PearlItem : ScriptableObject
    {
        public string id = "";
        public string pearlName = "Pearl";
        [TextArea(2, 4)]
        public string description;
        public PearlRarity rarity = PearlRarity.Common;
        public PearlType pearlType = PearlType.Attack;
        public Sprite icon;
        public List<StatModifier> modifiers = new List<StatModifier>();
        
        [Header("Enhancement")]
        public int maxEnhanceLevel = 5;
        public float[] enhanceBonusPerLevel = new float[] { 0.02f, 0.04f, 0.06f, 0.08f, 0.1f };
        
        [Header("Crafting")]
        public int scrapValue = 1;
        public PearlItem[] craftRecipe;
        
        [Header("Drop Settings")]
        public float baseDropWeight = 1f;
        public EnemyType[] canDropFrom;

        public string GetId()
        {
            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }

            return name;
        }
        
        public float GetEnhanceBonus(int enhanceLevel)
        {
            if (enhanceLevel <= 0 || enhanceLevel > maxEnhanceLevel)
            {
                return 0f;
            }
            
            int index = Mathf.Min(enhanceLevel - 1, enhanceBonusPerLevel.Length - 1);
            return enhanceBonusPerLevel[index];
        }
        
        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case PearlRarity.Common:
                    return Color.white;
                case PearlRarity.Uncommon:
                    return Color.green;
                case PearlRarity.Rare:
                    return Color.blue;
                case PearlRarity.Epic:
                    return new Color(0.6f, 0.2f, 1f);
                case PearlRarity.Legendary:
                    return new Color(1f, 0.84f, 0f);
                default:
                    return Color.white;
            }
        }
    }
}
