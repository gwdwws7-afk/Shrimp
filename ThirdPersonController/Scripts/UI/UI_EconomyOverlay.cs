using UnityEngine;

namespace ThirdPersonController
{
    public class UI_EconomyOverlay : MonoBehaviour
    {
        [Header("Input")]
        public string toggleActionName = "ToggleEconomy";
        public KeyCode toggleKey = KeyCode.Y;
        public bool allowToggle = true;
        public bool pauseGameWhenOpen = false;
        public bool startOpen = true;

        [Header("Layout")]
        public float panelWidth = 520f;
        public float panelHeight = 540f;
        public float panelPadding = 12f;
        public float listHeight = 280f;

        [Header("References")]
        public CurrencyWallet wallet;
        public ShopManager shopManager;
        public ConsumableInventory inventory;
        public ConsumableCatalog catalog;
        public ConsumableUseSystem useSystem;
        public ConsumableQuickSlots quickSlots;
        public PlayerInputHandler inputHandler;

        private bool isOpen = true;
        private Vector2 scroll;

        private void Awake()
        {
            if (wallet == null)
            {
                wallet = CurrencyWallet.EnsureInstance();
            }

            if (shopManager == null)
            {
                shopManager = FindObjectOfType<ShopManager>();
            }

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
            }

            if (quickSlots == null)
            {
                quickSlots = FindObjectOfType<ConsumableQuickSlots>();
            }

            if (inputHandler == null)
            {
                inputHandler = FindObjectOfType<PlayerInputHandler>();
            }

            SetOpen(startOpen, false);
        }

        private void Update()
        {
            if (!allowToggle)
            {
                return;
            }

            PlayerInputHandler handler = ResolveInputHandler();
            bool togglePressed = handler != null && handler.WasActionPressedThisFrame(toggleActionName, toggleKey);

            if (togglePressed)
            {
                Toggle();
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

        private void Toggle()
        {
            SetOpen(!isOpen, pauseGameWhenOpen);
        }

        public void SetOpen(bool open, bool controlTimeAndCursor)
        {
            isOpen = open;
            if (quickSlots != null)
            {
                quickSlots.allowInput = !isOpen;
            }

            if (controlTimeAndCursor)
            {
                Time.timeScale = isOpen ? 0f : 1f;
                Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = isOpen;
            }
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            float width = panelWidth;
            float height = panelHeight;
            Rect panel = new Rect(20f, 20f, width, height);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(panel);
            GUILayout.Space(panelPadding);

            GUILayout.Label(Localize("ui.economy_overlay.title", "信息"), HeaderStyle());
            GUILayout.Space(6f);

            int credits = wallet != null ? wallet.Credits : 0;
            GUILayout.Label(
                string.Format(Localize("ui.economy_overlay.credits_format", "货币: {0}"), credits),
                SectionStyle());
            GUILayout.Space(8f);

            DrawQuickSlots();
            GUILayout.Space(8f);

            GUILayout.Label(Localize("ui.economy_overlay.catalog_title", "物资列表"), SectionStyle());
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(listHeight));
            DrawCatalogList();
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            GUILayout.Label(
                string.Format(
                    Localize("ui.economy_overlay.close_hint_format", "按 {0} 关闭"),
                    GetToggleBindingLabel()),
                SmallStyle());
            GUILayout.EndArea();
        }

        private void DrawQuickSlots()
        {
            GUILayout.Label(
                string.Format(
                    Localize("ui.economy_overlay.quick_slots_title_format", "快捷使用 ({0})"),
                    GetQuickSlotHint()),
                SectionStyle());
            if (quickSlots == null)
            {
                GUILayout.Label(Localize("ui.economy_overlay.quick_slots_missing", "未找到快捷栏组件"), SmallStyle());
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                string name = quickSlots.GetSlotDisplayName(i);
                int count = quickSlots.GetSlotCount(i);
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    string.Format(
                        Localize("ui.economy_overlay.quick_slot_entry_format", "{0}: {1} x{2}"),
                        i + 1,
                        name,
                        count),
                    GUILayout.Width(220));
                if (GUILayout.Button(Localize("ui.economy_overlay.use_button", "使用"), GUILayout.Width(60)))
                {
                    quickSlots.UseSlot(i);
                }
                if (GUILayout.Button(Localize("ui.economy_overlay.clear_button", "清空"), GUILayout.Width(60)))
                {
                    quickSlots.ClearSlot(i);
                }
                GUILayout.EndHorizontal();
            }
        }

        private string GetToggleBindingLabel()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            if (handler == null)
            {
                return PlayerInputHandler.GetFriendlyKeyLabel(toggleKey);
            }

            string binding = handler.GetActionBindingLabel(toggleActionName, toggleKey);
            return string.IsNullOrEmpty(binding)
                ? PlayerInputHandler.GetFriendlyKeyLabel(toggleKey)
                : binding;
        }

        private string GetQuickSlotHint()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            if (handler == null)
            {
                return Localize("ui.economy_overlay.quick_slot_hint_default", "1/2/3");
            }

            string slot1 = handler.GetActionBindingLabel("QuickSlot1", KeyCode.Alpha1, includeGamepad: false);
            string slot2 = handler.GetActionBindingLabel("QuickSlot2", KeyCode.Alpha2, includeGamepad: false);
            string slot3 = handler.GetActionBindingLabel("QuickSlot3", KeyCode.Alpha3, includeGamepad: false);
            if (string.IsNullOrEmpty(slot1) || string.IsNullOrEmpty(slot2) || string.IsNullOrEmpty(slot3))
            {
                return Localize("ui.economy_overlay.quick_slot_hint_default", "1/2/3");
            }

            return $"{slot1}/{slot2}/{slot3}";
        }

        private void DrawCatalogList()
        {
            if (catalog == null || catalog.items == null || catalog.items.Count == 0)
            {
                GUILayout.Label(Localize("ui.economy_overlay.catalog_empty", "暂无可用物资。"));
                return;
            }

            for (int i = 0; i < catalog.items.Count; i++)
            {
                ConsumableDefinition item = catalog.items[i];
                if (item == null)
                {
                    continue;
                }

                int count = inventory != null ? inventory.GetCount(item.id) : 0;
                int price = shopManager != null ? shopManager.GetPrice(item.id) : item.price;

                GUILayout.BeginVertical("box");
                GUILayout.Label($"{item.displayName}  x{count}", SectionStyle());
                GUILayout.Label(item.description, SmallStyle());
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(
                    string.Format(Localize("ui.economy_overlay.buy_button_format", "购买 ({0})"), price),
                    GUILayout.Width(100)))
                {
                    shopManager?.Purchase(item.id, 1);
                }
                if (GUILayout.Button(Localize("ui.economy_overlay.use_button", "使用"), GUILayout.Width(60)))
                {
                    useSystem?.UseConsumable(item.id);
                }
                if (GUILayout.Button(
                    string.Format(Localize("ui.economy_overlay.bind_slot_format", "绑定{0}"), 1),
                    GUILayout.Width(60)))
                {
                    quickSlots?.SetSlot(0, item.id);
                }
                if (GUILayout.Button(
                    string.Format(Localize("ui.economy_overlay.bind_slot_format", "绑定{0}"), 2),
                    GUILayout.Width(60)))
                {
                    quickSlots?.SetSlot(1, item.id);
                }
                if (GUILayout.Button(
                    string.Format(Localize("ui.economy_overlay.bind_slot_format", "绑定{0}"), 3),
                    GUILayout.Width(60)))
                {
                    quickSlots?.SetSlot(2, item.id);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        private static GUIStyle HeaderStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.UpperLeft,
                fontStyle = FontStyle.Bold
            };
        }

        private static GUIStyle SectionStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.UpperLeft,
                fontStyle = FontStyle.Bold
            };
        }

        private static GUIStyle SmallStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperLeft
            };
        }

        private static string Localize(string key, string fallback)
        {
            LocalizationService service = LocalizationService.Instance;
            if (service != null)
            {
                return service.Get(key, fallback);
            }

            return fallback;
        }
    }
}
