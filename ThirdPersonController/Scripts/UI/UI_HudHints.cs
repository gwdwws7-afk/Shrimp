using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class UI_HudHints : MonoBehaviour
    {
        public enum Corner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        [Header("Display")]
        public bool show = true;
        public bool allowToggle = true;
        public string toggleHintsActionName = "ToggleHints";
        public string toggleEconomyActionName = "ToggleEconomy";
        public string toggleTalentActionName = "ToggleTalent";
        public KeyCode toggleKey = KeyCode.H;
        public KeyCode economyKey = KeyCode.Y;
        public KeyCode talentKey = KeyCode.U;
        public bool useDynamicInputHints = true;
        public PlayerInputHandler inputHandler;
        public Corner anchor = Corner.TopRight;
        public Vector2 offset = new Vector2(16f, 16f);
        public float width = 240f;
        public float padding = 10f;
        public float lineHeight = 18f;
        public float hintRefreshInterval = 0.25f;

        [Header("Hints")]
        public string title = "操作提示";
        public List<string> hints = new List<string>();

        private readonly List<string> cachedDisplayHints = new List<string>();
        private bool hintsDirty = true;
        private float nextHintRefreshTime = 0f;
        private PlayerInputHandler subscribedInputHandler;

        private void OnEnable()
        {
            BindInputHandlerEvents();
            MarkHintsDirty();
            RebuildDisplayHints();
        }

        private void OnDisable()
        {
            UnbindInputHandlerEvents();
        }

        private void Update()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            bool togglePressed = handler != null && handler.WasActionPressedThisFrame(toggleHintsActionName, toggleKey);

            if (allowToggle && togglePressed)
            {
                show = !show;
            }

            BindInputHandlerEvents();
            if (hintsDirty || Time.unscaledTime >= nextHintRefreshTime)
            {
                RebuildDisplayHints();
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

        private void OnGUI()
        {
            if (!show)
            {
                return;
            }

            if (hintsDirty)
            {
                RebuildDisplayHints();
            }

            int count = cachedDisplayHints.Count;
            float height = padding * 2f + lineHeight * Mathf.Max(1, count + 1);
            Rect panel = BuildPanelRect(width, height);

            GUI.Box(panel, string.Empty);
            GUILayout.BeginArea(panel);
            GUILayout.Space(padding * 0.5f);
            GUILayout.Label(Localize("ui.hud_hints.title", title), HeaderStyle());
            for (int i = 0; i < cachedDisplayHints.Count; i++)
            {
                GUILayout.Label(cachedDisplayHints[i], HintStyle());
            }
            GUILayout.EndArea();
        }

        private void RebuildDisplayHints()
        {
            cachedDisplayHints.Clear();
            List<string> displayHints = BuildDisplayHints();
            if (displayHints != null && displayHints.Count > 0)
            {
                cachedDisplayHints.AddRange(displayHints);
            }

            hintsDirty = false;
            nextHintRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, hintRefreshInterval);
        }

        private List<string> BuildDisplayHints()
        {
            var displayHints = new List<string>();
            if (useDynamicInputHints)
            {
                string economyLabel = ResolveActionLabel(toggleEconomyActionName, economyKey);
                string talentLabel = ResolveActionLabel(toggleTalentActionName, talentKey);
                string slot1Label = ResolveActionLabel("QuickSlot1", KeyCode.Alpha1, includeGamepad: false);
                string slot2Label = ResolveActionLabel("QuickSlot2", KeyCode.Alpha2, includeGamepad: false);
                string slot3Label = ResolveActionLabel("QuickSlot3", KeyCode.Alpha3, includeGamepad: false);

                displayHints.Add(string.Format(
                    Localize("ui.hud_hints.economy_format", "{0} 补给/商店"),
                    economyLabel));
                displayHints.Add(string.Format(
                    Localize("ui.hud_hints.quick_slots_format", "{0}/{1}/{2} 快捷使用"),
                    slot1Label,
                    slot2Label,
                    slot3Label));
                displayHints.Add(string.Format(
                    Localize("ui.hud_hints.talent_format", "{0} 天赋/装备"),
                    talentLabel));
            }

            if (hints != null)
            {
                for (int i = 0; i < hints.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(hints[i]))
                    {
                        displayHints.Add(hints[i]);
                    }
                }
            }

            if (displayHints.Count == 0)
            {
                displayHints.Add(Localize("ui.hud_hints.none", "暂无提示"));
            }

            return displayHints;
        }

        private void BindInputHandlerEvents()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            if (subscribedInputHandler == handler)
            {
                return;
            }

            if (subscribedInputHandler != null)
            {
                subscribedInputHandler.OnPromptDeviceChanged -= HandlePromptDeviceChanged;
            }

            subscribedInputHandler = handler;
            if (subscribedInputHandler != null)
            {
                subscribedInputHandler.OnPromptDeviceChanged += HandlePromptDeviceChanged;
            }

            MarkHintsDirty();
        }

        private void UnbindInputHandlerEvents()
        {
            if (subscribedInputHandler != null)
            {
                subscribedInputHandler.OnPromptDeviceChanged -= HandlePromptDeviceChanged;
            }

            subscribedInputHandler = null;
        }

        private void HandlePromptDeviceChanged(InputPromptDevice _)
        {
            MarkHintsDirty();
            RebuildDisplayHints();
        }

        private void MarkHintsDirty()
        {
            hintsDirty = true;
        }

        private string ResolveActionLabel(string actionName, KeyCode fallbackKey, bool includeGamepad = true)
        {
            PlayerInputHandler handler = ResolveInputHandler();
            if (handler == null)
            {
                return PlayerInputHandler.GetFriendlyKeyLabel(fallbackKey);
            }

            string binding = handler.GetActionBindingLabel(actionName, fallbackKey, includeGamepad);
            if (!string.IsNullOrEmpty(binding))
            {
                return binding;
            }

            return PlayerInputHandler.GetFriendlyKeyLabel(fallbackKey);
        }

        private Rect BuildPanelRect(float panelWidth, float panelHeight)
        {
            float x = offset.x;
            float y = offset.y;

            switch (anchor)
            {
                case Corner.TopRight:
                    x = Screen.width - panelWidth - offset.x;
                    break;
                case Corner.BottomLeft:
                    y = Screen.height - panelHeight - offset.y;
                    break;
                case Corner.BottomRight:
                    x = Screen.width - panelWidth - offset.x;
                    y = Screen.height - panelHeight - offset.y;
                    break;
            }

            return new Rect(x, y, panelWidth, panelHeight);
        }

        private static GUIStyle HeaderStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
        }

        private static GUIStyle HintStyle()
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
