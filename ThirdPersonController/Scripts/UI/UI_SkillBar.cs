using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能栏UI - 显示6个技能槽
    /// </summary>
    public class UI_SkillBar : MonoBehaviour
    {
        [System.Serializable]
        public class SkillSlot
        {
            public Image icon;               // 技能图标
            public Image cooldownOverlay;    // 冷却遮罩（黑色半透明）
            public Text cooldownText;        // 冷却时间文字
            public Text keyText;             // 按键提示（Q/W/E/R/T/F）
            public GameObject highlight;     // 高亮边框
        }
        
        [Header("技能槽")]
        public SkillSlot[] skillSlots = new SkillSlot[6];  // 6个技能槽
        
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

        public SkillManager skillManager;
        
        private void Start()
        {
            // 设置按键提示
            for (int i = 0; i < skillSlots.Length && i < keyBindings.Length; i++)
            {
                if (skillSlots[i].keyText != null)
                {
                    skillSlots[i].keyText.text = keyBindings[i];
                }
            }
            
            // 订阅事件
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
            // 取消订阅
            GameEvents.OnSkillUsed -= OnSkillUsed;
            GameEvents.OnSkillReady -= OnSkillReady;
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
            DrawLegendItem("聚怪", gatherColor, style);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 更新技能槽
        /// </summary>
        public void UpdateSkillSlot(int index, Sprite icon, float cooldown, float remainingCD)
        {
            if (index < 0 || index >= skillSlots.Length) return;
            
            var slot = skillSlots[index];
            
            // 更新图标
            if (slot.icon != null && icon != null)
            {
                slot.icon.sprite = icon;
                slot.icon.gameObject.SetActive(true);
            }
            
            // 更新冷却显示
            if (remainingCD > 0)
            {
                // 冷却中
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
                // 冷却完成
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
            UpdateSkillSlot(index, skill.icon, cooldownDuration, skill.cooldownTimer);

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
        /// 设置技能图标
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
        /// 高亮技能槽
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
            // 找到对应的技能槽并更新
            // 这里需要SkillManager的配合来知道是哪个技能
        }
        
        private void OnSkillReady(string skillName)
        {
            // 技能冷却完成
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
