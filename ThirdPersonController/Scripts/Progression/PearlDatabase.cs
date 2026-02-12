using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(fileName = "PearlDatabase", menuName = "Progression/Pearl Database")]
    public class PearlDatabase : ScriptableObject
    {
        public List<PearlItem> pearls = new List<PearlItem>();

        private Dictionary<string, PearlItem> lookup;

        public PearlItem GetPearlById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            BuildLookup();
            if (lookup != null && lookup.TryGetValue(id, out PearlItem item))
            {
                return item;
            }

            return null;
        }

        private void BuildLookup()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, PearlItem>();
            if (pearls == null)
            {
                return;
            }

            for (int i = 0; i < pearls.Count; i++)
            {
                PearlItem item = pearls[i];
                if (item == null)
                {
                    continue;
                }

                string id = item.GetId();
                if (!lookup.ContainsKey(id))
                {
                    lookup.Add(id, item);
                }
            }
        }
    }
}
