using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class ConsumableInventory : MonoBehaviour
    {
        public static ConsumableInventory Instance { get; private set; }

        public ConsumableCatalog catalog;
        public List<ConsumableStack> stacks = new List<ConsumableStack>();

        public event System.Action OnInventoryChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LoadFromSave();
        }

        public static ConsumableInventory EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject inventoryObject = new GameObject("ConsumableInventory");
            return inventoryObject.AddComponent<ConsumableInventory>();
        }

        public int GetCount(string itemId)
        {
            ConsumableStack stack = FindStack(itemId);
            return stack != null ? stack.count : 0;
        }

        public bool Add(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
            {
                return false;
            }

            ConsumableDefinition item = ResolveItem(itemId);
            if (item == null)
            {
                return false;
            }

            ConsumableStack stack = FindOrCreateStack(itemId);
            int newCount = stack.count + amount;
            if (item.maxStack > 0)
            {
                newCount = Mathf.Min(newCount, item.maxStack);
            }

            int added = newCount - stack.count;
            if (added <= 0)
            {
                return false;
            }

            stack.count = newCount;
            SaveToData();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool Consume(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
            {
                return false;
            }

            ConsumableStack stack = FindStack(itemId);
            if (stack == null || stack.count < amount)
            {
                return false;
            }

            stack.count -= amount;
            if (stack.count <= 0)
            {
                stacks.Remove(stack);
            }

            SaveToData();
            OnInventoryChanged?.Invoke();
            return true;
        }

        private ConsumableDefinition ResolveItem(string itemId)
        {
            if (catalog == null)
            {
                catalog = Resources.Load<ConsumableCatalog>("ConsumableCatalog");
            }

            if (catalog == null)
            {
                catalog = ConsumableCatalog.CreateDefault();
            }

            return catalog != null ? catalog.GetById(itemId) : null;
        }

        private ConsumableStack FindStack(string itemId)
        {
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i] != null && stacks[i].itemId == itemId)
                {
                    return stacks[i];
                }
            }

            return null;
        }

        private ConsumableStack FindOrCreateStack(string itemId)
        {
            ConsumableStack stack = FindStack(itemId);
            if (stack != null)
            {
                return stack;
            }

            stack = new ConsumableStack { itemId = itemId, count = 0 };
            stacks.Add(stack);
            return stack;
        }

        private void LoadFromSave()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
            {
                return;
            }

            if (SaveManager.Instance.CurrentData.consumables != null)
            {
                stacks = new List<ConsumableStack>(SaveManager.Instance.CurrentData.consumables);
            }
        }

        private void SaveToData()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
            {
                return;
            }

            SaveManager.Instance.CurrentData.consumables = new List<ConsumableStack>(stacks);
        }
    }
}
