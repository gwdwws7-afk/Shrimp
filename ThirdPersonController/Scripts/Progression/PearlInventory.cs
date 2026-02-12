using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class PearlInventory : MonoBehaviour
    {
        public List<PearlItem> ownedPearls = new List<PearlItem>();

        public event System.Action OnInventoryChanged;

        public bool AddPearl(PearlItem pearl)
        {
            if (pearl == null)
            {
                return false;
            }

            ownedPearls.Add(pearl);
            NotifyChanged();
            return true;
        }

        public bool RemovePearl(PearlItem pearl)
        {
            if (pearl == null)
            {
                return false;
            }

            bool removed = ownedPearls.Remove(pearl);
            if (removed)
            {
                NotifyChanged();
            }

            return removed;
        }

        public bool HasPearl(PearlItem pearl)
        {
            return pearl != null && ownedPearls.Contains(pearl);
        }

        public void NotifyChanged()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
