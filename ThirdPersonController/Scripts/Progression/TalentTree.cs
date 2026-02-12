using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class TalentTree : MonoBehaviour
    {
        public TalentTreeData data;
        public int availablePoints = 0;
        public List<string> unlockedNodes = new List<string>();

        public event System.Action OnTalentChanged;
        public event System.Action<string> OnTalentUnlocked;

        private Dictionary<string, TalentNodeData> nodeLookup;

        private void Awake()
        {
            BuildLookup();
        }

        private void OnValidate()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            nodeLookup = new Dictionary<string, TalentNodeData>();
            if (data == null || data.nodes == null)
            {
                return;
            }

            for (int i = 0; i < data.nodes.Count; i++)
            {
                TalentNodeData node = data.nodes[i];
                if (node == null || string.IsNullOrEmpty(node.id))
                {
                    continue;
                }

                if (!nodeLookup.ContainsKey(node.id))
                {
                    nodeLookup.Add(node.id, node);
                }
            }
        }

        public bool IsUnlocked(string nodeId)
        {
            return !string.IsNullOrEmpty(nodeId) && unlockedNodes.Contains(nodeId);
        }

        public bool CanUnlock(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return false;
            }

            if (IsUnlocked(nodeId))
            {
                return false;
            }

            if (!TryGetNode(nodeId, out TalentNodeData node))
            {
                return false;
            }

            if (availablePoints < node.cost)
            {
                return false;
            }

            if (node.prerequisites != null)
            {
                for (int i = 0; i < node.prerequisites.Count; i++)
                {
                    if (!IsUnlocked(node.prerequisites[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool Unlock(string nodeId)
        {
            if (!CanUnlock(nodeId))
            {
                return false;
            }

            if (!TryGetNode(nodeId, out TalentNodeData node))
            {
                return false;
            }

            availablePoints = Mathf.Max(0, availablePoints - node.cost);
            unlockedNodes.Add(nodeId);
            NotifyChanged();
            OnTalentUnlocked?.Invoke(nodeId);
            GameEvents.ShowMessage($"Talent unlocked: {node.title}", 2f);
            return true;
        }

        public void ResetAll()
        {
            unlockedNodes.Clear();
            NotifyChanged();
        }

        public void NotifyChanged()
        {
            OnTalentChanged?.Invoke();
        }

        public List<StatModifier> GetModifiers()
        {
            List<StatModifier> modifiers = new List<StatModifier>();
            if (data == null || data.nodes == null)
            {
                return modifiers;
            }

            for (int i = 0; i < unlockedNodes.Count; i++)
            {
                if (TryGetNode(unlockedNodes[i], out TalentNodeData node))
                {
                    if (node.modifiers != null)
                    {
                        modifiers.AddRange(node.modifiers);
                    }
                }
            }

            return modifiers;
        }

        private bool TryGetNode(string nodeId, out TalentNodeData node)
        {
            if (nodeLookup == null)
            {
                BuildLookup();
            }

            if (nodeLookup != null && nodeLookup.TryGetValue(nodeId, out node))
            {
                return true;
            }

            node = null;
            return false;
        }
    }
}
