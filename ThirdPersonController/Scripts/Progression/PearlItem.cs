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

    [CreateAssetMenu(fileName = "Pearl_", menuName = "Progression/Pearl")]
    public class PearlItem : ScriptableObject
    {
        public string id = "";
        public string pearlName = "Pearl";
        [TextArea(2, 4)]
        public string description;
        public PearlRarity rarity = PearlRarity.Common;
        public Sprite icon;
        public List<StatModifier> modifiers = new List<StatModifier>();

        public string GetId()
        {
            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }

            return name;
        }
    }
}
