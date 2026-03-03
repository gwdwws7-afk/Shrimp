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
        public KeyCode toggleKey = KeyCode.H;
        public Corner anchor = Corner.TopRight;
        public Vector2 offset = new Vector2(16f, 16f);
        public float width = 240f;
        public float padding = 10f;
        public float lineHeight = 18f;

        [Header("Hints")]
        public string title = "操作提示";
        public List<string> hints = new List<string>
        {
            "Y 补给/商店",
            "1-3 快捷使用",
            "T 天赋/装备"
        };

        private void Update()
        {
            if (allowToggle && Input.GetKeyDown(toggleKey))
            {
                show = !show;
            }
        }

        private void OnGUI()
        {
            if (!show)
            {
                return;
            }

            int count = hints != null ? hints.Count : 0;
            float height = padding * 2f + lineHeight * Mathf.Max(1, count + 1);
            Rect panel = BuildPanelRect(width, height);

            GUI.Box(panel, string.Empty);
            GUILayout.BeginArea(panel);
            GUILayout.Space(padding * 0.5f);
            GUILayout.Label(title, HeaderStyle());
            if (hints != null)
            {
                for (int i = 0; i < hints.Count; i++)
                {
                    GUILayout.Label(hints[i], HintStyle());
                }
            }
            GUILayout.EndArea();
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
