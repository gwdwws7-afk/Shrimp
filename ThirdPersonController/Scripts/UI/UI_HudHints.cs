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

        [Header("Hints")]
        public string title = "操作提示";
        public List<string> hints = new List<string>();

        private void Update()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            bool togglePressed = handler != null && handler.WasActionPressedThisFrame(toggleHintsActionName, toggleKey);

            if (allowToggle && togglePressed)
            {
                show = !show;
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

            List<string> displayHints = BuildDisplayHints();
            int count = displayHints.Count;
            float height = padding * 2f + lineHeight * Mathf.Max(1, count + 1);
            Rect panel = BuildPanelRect(width, height);

            GUI.Box(panel, string.Empty);
            GUILayout.BeginArea(panel);
            GUILayout.Space(padding * 0.5f);
            GUILayout.Label(title, HeaderStyle());
            for (int i = 0; i < displayHints.Count; i++)
            {
                GUILayout.Label(displayHints[i], HintStyle());
            }
            GUILayout.EndArea();
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

                displayHints.Add($"{economyLabel} 补给/商店");
                displayHints.Add($"{slot1Label}/{slot2Label}/{slot3Label} 快捷使用");
                displayHints.Add($"{talentLabel} 天赋/装备");
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
                displayHints.Add("暂无提示");
            }

            return displayHints;
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
    }
}
