using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController
{
    /// <summary>
    /// UI_SkillBar 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    public class UI_SkillBar : MonoBehaviour
    {
        [System.Serializable]
        public class SkillSlot
        {
            public Image icon; // UI 引用，用于驱动界面表现与信息同步。
            public Image cooldownOverlay; // 冷却遮罩，用于限制触发频率并平衡节奏。
            public Text cooldownText; // 冷却计时文本，用于限制触发频率并平衡节奏。
            public Text keyText; // 按键提示文本，用于输入映射并支持后续重绑定。
            public GameObject highlight; // 运行时配置项，用于驱动模块行为并保持可调性。
        }
        
        [Header("技能槽位")]
        public SkillSlot[] skillSlots = new SkillSlot[6];  // 六个主动技能槽位（与按键一一对应）
        
        [Header("按键绑定")]
        public string[] keyBindings = new string[6] { "Q", "W", "E", "R", "T", "F" };
        
        [Header("视觉效果")]
        public Color normalColor = Color.white;
        public Color cooldownColor = Color.gray;
        public Color readyColor = new Color(0.5f, 1f, 0.5f);

        [Header("分类颜色")]
        public Color crowdControlColor = new Color(0.4f, 0.7f, 1f);
        public Color burstColor = new Color(1f, 0.5f, 0.4f);
        public Color mobilityColor = new Color(0.5f, 1f, 0.6f);
        public Color gatherColor = new Color(0.8f, 0.6f, 1f);

        [Header("Legend")]
        public bool showLegend = true;
        public float legendOffsetY = -90f;

        [Header("Attack Input")]
        public Text attackInputHintText;
        public string attackInputHintLabel = "A: 左键  B: 右键";

        public SkillManager skillManager;
        private Texture2D fallbackSkillIconTexture;
        private Sprite fallbackSkillIcon;
        
        private void Start()
        {
            // 初始化每个槽位的按键提示文本。
            for (int i = 0; i < skillSlots.Length && i < keyBindings.Length; i++)
            {
                if (skillSlots[i].keyText != null)
                {
                    skillSlots[i].keyText.text = keyBindings[i];
                }
            }

            EnsureAttackInputHint();
            if (attackInputHintText != null)
            {
                attackInputHintText.text = attackInputHintLabel;
            }
            
            // 监听技能释放与冷却完成事件。
            GameEvents.OnSkillUsed += OnSkillUsed;
            GameEvents.OnSkillReady += OnSkillReady;

            if (skillManager == null)
            {
                skillManager = FindObjectOfType<SkillManager>();
            }
        }

        private void Update()
        {
            UpdateFromManager();
        }
        
        private void OnDestroy()
        {
            // 销毁时解除事件监听。
            GameEvents.OnSkillUsed -= OnSkillUsed;
            GameEvents.OnSkillReady -= OnSkillReady;

            if (fallbackSkillIcon != null)
            {
                Destroy(fallbackSkillIcon);
                fallbackSkillIcon = null;
            }

            if (fallbackSkillIconTexture != null)
            {
                Destroy(fallbackSkillIconTexture);
                fallbackSkillIconTexture = null;
            }
        }

        private void OnGUI()
        {
            if (!showLegend)
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            float width = 380f;
            float height = 20f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height + legendOffsetY;

            GUILayout.BeginArea(new Rect(x, y, width, height));
            GUILayout.BeginHorizontal();

            DrawLegendItem("群控", crowdControlColor, style);
            DrawLegendItem("爆发", burstColor, style);
            DrawLegendItem("位移", mobilityColor, style);
            DrawLegendItem("Gather", gatherColor, style);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 更新技能槽位 UI 显示。
        /// </summary>
        public void UpdateSkillSlot(int index, Sprite icon, float cooldown, float remainingCD)
        {
            if (index < 0 || index >= skillSlots.Length) return;
            
            var slot = skillSlots[index];
            
            // 刷新槽位图标，并确保图标处于可见状态。
            if (slot.icon != null && icon != null)
            {
                slot.icon.sprite = icon;
                slot.icon.gameObject.SetActive(true);
            }
            
            // 按剩余冷却切换冷却态与就绪态视觉。
            if (remainingCD > 0)
            {
                // 冷却中：显示遮罩比例与倒计时文本。
                if (slot.cooldownOverlay != null)
                {
                    slot.cooldownOverlay.fillAmount = remainingCD / cooldown;
                    slot.cooldownOverlay.gameObject.SetActive(true);
                }
                
                if (slot.cooldownText != null)
                {
                    slot.cooldownText.text = remainingCD.ToString("F1");
                    slot.cooldownText.gameObject.SetActive(true);
                }
                
                if (slot.icon != null)
                {
                    slot.icon.color = cooldownColor;
                }
            }
            else
            {
                // 冷却结束：隐藏遮罩与倒计时，并恢复就绪颜色。
                if (slot.cooldownOverlay != null)
                {
                    slot.cooldownOverlay.fillAmount = 0;
                    slot.cooldownOverlay.gameObject.SetActive(false);
                }
                
                if (slot.cooldownText != null)
                {
                    slot.cooldownText.gameObject.SetActive(false);
                }
                
                if (slot.icon != null)
                {
                    slot.icon.color = readyColor;
                }
            }
        }

        public void UpdateSkillSlot(int index, SkillBase skill)
        {
            if (skill == null)
            {
                return;
            }

            float cooldownDuration = skill.cooldownDuration > 0f ? skill.cooldownDuration : skill.cooldown;
            Sprite displayIcon = skill.icon != null ? skill.icon : GetFallbackSkillIcon();
            UpdateSkillSlot(index, displayIcon, cooldownDuration, skill.cooldownTimer);

            if (skillSlots[index].icon != null)
            {
                if (skill.cooldownTimer > 0f)
                {
                    skillSlots[index].icon.color = cooldownColor;
                }
                else
                {
                    skillSlots[index].icon.color = GetCategoryColor(skill.category);
                }
            }

            if (skillSlots[index].keyText != null)
            {
                skillSlots[index].keyText.color = GetCategoryColor(skill.category);
            }
        }
        
        /// <summary>
        /// 设置Skill Icon，统一写入入口，便于约束副作用。
        /// </summary>
        public void SetSkillIcon(int index, Sprite icon)
        {
            if (index < 0 || index >= skillSlots.Length) return;
            
            if (skillSlots[index].icon != null)
            {
                skillSlots[index].icon.sprite = icon;
            }
        }
        
        /// <summary>
        /// 执行 Highlight Slot 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void HighlightSlot(int index, bool highlight)
        {
            if (index < 0 || index >= skillSlots.Length) return;
            
            if (skillSlots[index].highlight != null)
            {
                skillSlots[index].highlight.SetActive(highlight);
            }
        }
        
        #region 事件处理
        
        private void OnSkillUsed(string skillName, float cooldown)
        {
            // 当前由 UpdateFromManager 轮询刷新 UI；保留事件接口兼容旧链路。
            // 这里不直接改 UI，避免事件顺序与轮询刷新冲突。
        }
        
        private void OnSkillReady(string skillName)
        {
            // 同样保留兼容入口，冷却完成由轮询逻辑统一反映。
        }

        private void UpdateFromManager()
        {
            if (skillManager == null || skillManager.skills == null)
            {
                return;
            }

            for (int i = 0; i < skillSlots.Length && i < skillManager.skills.Length; i++)
            {
                SkillBase skill = skillManager.skills[i];
                if (skill == null)
                {
                    continue;
                }

                UpdateSkillSlot(i, skill);
            }
        }

        private void EnsureAttackInputHint()
        {
            if (attackInputHintText != null)
            {
                return;
            }

            Transform existing = transform.Find("AttackInputHint");
            if (existing != null)
            {
                attackInputHintText = existing.GetComponent<Text>();
            }

            if (attackInputHintText != null)
            {
                return;
            }

            GameObject hintObj = new GameObject("AttackInputHint");
            hintObj.transform.SetParent(transform, false);
            Text hintText = hintObj.AddComponent<Text>();
            hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintText.fontSize = 14;
            hintText.color = new Color(1f, 1f, 1f, 0.8f);
            hintText.alignment = TextAnchor.MiddleCenter;

            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 1f);
            hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 6f);
            hintRect.sizeDelta = new Vector2(240f, 20f);

            attackInputHintText = hintText;
        }

        private Color GetCategoryColor(SkillCategory category)
        {
            switch (category)
            {
                case SkillCategory.CrowdControl:
                    return crowdControlColor;
                case SkillCategory.Burst:
                    return burstColor;
                case SkillCategory.Mobility:
                    return mobilityColor;
                case SkillCategory.Gather:
                    return gatherColor;
                default:
                    return readyColor;
            }
        }

        private Sprite GetFallbackSkillIcon()
        {
            if (fallbackSkillIcon != null)
            {
                return fallbackSkillIcon;
            }

            fallbackSkillIconTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            fallbackSkillIconTexture.name = "FallbackSkillIconTexture";
            fallbackSkillIconTexture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            fallbackSkillIconTexture.SetPixels(pixels);
            fallbackSkillIconTexture.Apply(false, false);

            fallbackSkillIcon = Sprite.Create(
                fallbackSkillIconTexture,
                new Rect(0f, 0f, 32f, 32f),
                new Vector2(0.5f, 0.5f),
                32f);
            fallbackSkillIcon.name = "FallbackSkillIcon";

            return fallbackSkillIcon;
        }

        private void DrawLegendItem(string label, Color color, GUIStyle style)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUILayout.Label(label, style, GUILayout.Width(80));
            GUI.color = previous;
        }
        
        #endregion
    }
}
