using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class PearlEquipment : MonoBehaviour
    {
        public int slotCount = 3;
        public List<PearlItem> equippedPearls = new List<PearlItem>();

        public event System.Action OnEquipmentChanged;

        private void Awake()
        {
            EnsureSlotCount();
        }

        public void EnsureSlotCount()
        {
            if (slotCount < 0)
            {
                slotCount = 0;
            }

            if (equippedPearls == null)
            {
                equippedPearls = new List<PearlItem>();
            }

            if (equippedPearls.Count > slotCount)
            {
                equippedPearls.RemoveRange(slotCount, equippedPearls.Count - slotCount);
            }
            else
            {
                while (equippedPearls.Count < slotCount)
                {
                    equippedPearls.Add(null);
                }
            }
        }

        public bool Equip(PearlItem pearl, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotCount)
            {
                return false;
            }

            EnsureSlotCount();
            equippedPearls[slotIndex] = pearl;
            NotifyChanged();
            return true;
        }

        public bool Unequip(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotCount)
            {
                return false;
            }

            EnsureSlotCount();
            if (equippedPearls[slotIndex] == null)
            {
                return false;
            }

            equippedPearls[slotIndex] = null;
            NotifyChanged();
            return true;
        }

        public List<StatModifier> GetModifiers()
        {
            List<StatModifier> modifiers = new List<StatModifier>();
            if (equippedPearls == null)
            {
                return modifiers;
            }

            for (int i = 0; i < equippedPearls.Count; i++)
            {
                PearlItem pearl = equippedPearls[i];
                if (pearl == null || pearl.modifiers == null)
                {
                    continue;
                }

                modifiers.AddRange(pearl.modifiers);
            }

            return modifiers;
        }

        public void NotifyChanged()
        {
            OnEquipmentChanged?.Invoke();
        }
    }
}
