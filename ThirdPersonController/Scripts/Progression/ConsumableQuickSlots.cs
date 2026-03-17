using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class ConsumableQuickSlots : MonoBehaviour
    {
        [Header("Slots")]
        public string[] slotItemIds = new string[3];
        public KeyCode[] slotKeys = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };
        public bool allowInput = true;

        [Header("References")]
        public ConsumableInventory inventory;
        public ConsumableCatalog catalog;
        public ConsumableUseSystem useSystem;
        public PlayerInputHandler inputHandler;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = ConsumableInventory.EnsureInstance();
            }

            if (catalog == null)
            {
                catalog = Resources.Load<ConsumableCatalog>("ConsumableCatalog") ?? ConsumableCatalog.CreateDefault();
            }

            if (useSystem == null)
            {
                useSystem = FindObjectOfType<ConsumableUseSystem>();
                if (useSystem == null)
                {
                    GameObject useObj = new GameObject("ConsumableUseSystem");
                    useSystem = useObj.AddComponent<ConsumableUseSystem>();
                }
            }

            if (inputHandler == null)
            {
                inputHandler = FindObjectOfType<PlayerInputHandler>();
            }

            EnsureSlotArray();
            LoadFromSave();
        }

        private void Update()
        {
            if (!allowInput || slotKeys == null)
            {
                return;
            }

            PlayerInputHandler handler = ResolveInputHandler();
            int count = Mathf.Min(slotKeys.Length, slotItemIds.Length);
            for (int i = 0; i < count; i++)
            {
                bool pressed = handler != null && handler.WasQuickSlotPressedThisFrame(i);

                if (pressed)
                {
                    UseSlot(i);
                }
            }
        }

        private PlayerInputHandler ResolveInputHandler()
        {
            if (inputHandler != null)
            {
                return inputHandler;
            }

            inputHandler = PlayerInputHandler.ResolveActiveInstance();
            return inputHandler;
        }

        public string GetSlotItemId(int index)
        {
            if (index < 0 || index >= slotItemIds.Length)
            {
                return string.Empty;
            }

            return slotItemIds[index] ?? string.Empty;
        }

        public string GetSlotDisplayName(int index)
        {
            string id = GetSlotItemId(index);
            if (string.IsNullOrEmpty(id) || catalog == null)
            {
                return "(空)";
            }

            ConsumableDefinition item = catalog.GetById(id);
            return item != null ? item.displayName : "(δ֪)";
        }

        public int GetSlotCount(int index)
        {
            if (inventory == null)
            {
                return 0;
            }

            string id = GetSlotItemId(index);
            return string.IsNullOrEmpty(id) ? 0 : inventory.GetCount(id);
        }

        public void SetSlot(int index, string itemId)
        {
            if (index < 0 || index >= slotItemIds.Length)
            {
                return;
            }

            slotItemIds[index] = itemId ?? string.Empty;
            SaveToData();
        }

        public void ClearSlot(int index)
        {
            SetSlot(index, string.Empty);
        }

        public bool UseSlot(int index)
        {
            if (useSystem == null)
            {
                return false;
            }

            string id = GetSlotItemId(index);
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            return useSystem.UseConsumable(id);
        }

        private void EnsureSlotArray()
        {
            if (slotItemIds == null || slotItemIds.Length != 3)
            {
                slotItemIds = new string[3];
            }
        }

        private void LoadFromSave()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
            {
                return;
            }

            List<string> slots = SaveManager.Instance.CurrentData.quickConsumableSlots;
            if (slots == null)
            {
                return;
            }

            EnsureSlotArray();
            for (int i = 0; i < slotItemIds.Length; i++)
            {
                slotItemIds[i] = i < slots.Count ? slots[i] : string.Empty;
            }
        }

        private void SaveToData()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
            {
                return;
            }

            SaveManager.Instance.CurrentData.quickConsumableSlots = new List<string>(slotItemIds);
        }
    }
}
