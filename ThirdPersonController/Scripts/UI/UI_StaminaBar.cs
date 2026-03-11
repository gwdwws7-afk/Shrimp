using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController
{
    /// <summary>
    /// UI_StaminaBar 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    public class UI_StaminaBar : MonoBehaviour
    {
        [Header("UI引用")]
        public Slider staminaSlider;         // 耐力条 Slider
        public Image fillImage; // 运行时配置项，用于驱动模块行为并保持可调性。
        public Text staminaText; // 耐力数值文本，用于维持信息可读性与界面层次。
        
        [Header("颜色设置")]
        public Color normalColor = new Color(0.9f, 0.7f, 0.2f); // 运行时配置项，用于驱动模块行为并保持可调性。
        public Color lowColor = new Color(0.9f, 0.3f, 0.1f); // 运行时配置项，用于驱动模块行为并保持可调性。
        public Color exhaustedColor = new Color(0.3f, 0.3f, 0.3f); // 力竭状态颜色，用于对外暴露当前流程阶段。
        public float lowThreshold = 0.3f; // 低耐力阈值（<=），用于控制分段逻辑并防止越界。
        
        [Header("动画设置")]
        public bool useSmoothFill = true;
        public float fillSpeed = 8f;         // 耐力条平滑插值速度
        
        [Header("力竭效果")]
        public Image exhaustedOverlay;       // 力竭时显示的叠加遮罩
        public float pulseSpeed = 2f;        // 力竭遮罩呼吸闪烁速度
        
        private float targetFillAmount = 1f;
        private float currentFillAmount = 1f;
        private bool isExhausted = false;
        
        private void Start()
        {
            // 订阅耐力变化与力竭事件。
            GameEvents.OnStaminaChanged += OnStaminaChanged;
            GameEvents.OnStaminaDepleted += OnStaminaDepleted;
        }
        
        private void OnDestroy()
        {
            // 反注册事件，避免重复回调。
            GameEvents.OnStaminaChanged -= OnStaminaChanged;
            GameEvents.OnStaminaDepleted -= OnStaminaDepleted;
        }
        
        private void Update()
        {
            // 平滑模式下逐帧插值更新耐力条。
            if (useSmoothFill && staminaSlider != null)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * fillSpeed);
                staminaSlider.value = currentFillAmount;
            }
            
            // 力竭状态下让遮罩做呼吸闪烁提示。
            if (isExhausted && exhaustedOverlay != null)
            {
                float alpha = Mathf.PingPong(Time.time * pulseSpeed, 0.3f);
                Color color = exhaustedOverlay.color;
                color.a = alpha;
                exhaustedOverlay.color = color;
            }
        }
        
        /// <summary>
        /// 更新Stamina，保持显示与运行数据一致。
        /// </summary>
        public void UpdateStamina(float current, float max)
        {
            if (staminaSlider != null)
            {
                targetFillAmount = current / max;
                
                if (!useSmoothFill)
                {
                    staminaSlider.value = targetFillAmount;
                    currentFillAmount = targetFillAmount;
                }
            }
            
// 刷新耐力数值文本，用于维持信息可读性与界面层次。
            if (staminaText != null)
            {
                staminaText.text = $"{Mathf.Ceil(current)}/{max}";
            }
            
            // 按阈值和力竭状态刷新颜色。
            UpdateColor(targetFillAmount);
        }
        
        /// <summary>
        /// 根据状态更新条颜色。
        /// </summary>
        private void UpdateColor(float percent)
        {
            if (fillImage == null) return;
            
            if (isExhausted)
            {
                fillImage.color = exhaustedColor;
            }
            else if (percent <= lowThreshold)
            {
                fillImage.color = lowColor;
            }
            else
            {
                fillImage.color = normalColor;
            }
        }
        
        #region 事件处理
        
        private void OnStaminaChanged(float current, float max)
        {
            UpdateStamina(current, max);
        }
        
        private void OnStaminaDepleted()
        {
            isExhausted = true;
            
            // 力竭时显示遮罩并强化提示。
            if (exhaustedOverlay != null)
            {
                exhaustedOverlay.gameObject.SetActive(true);
            }
            
            // 订阅力竭结束回调，用于恢复 UI 状态。
            StaminaSystem stamina = FindObjectOfType<StaminaSystem>();
            if (stamina != null)
            {
                stamina.OnExhaustionEnd += OnExhaustionEnd;
            }
        }
        
        private void OnExhaustionEnd()
        {
            isExhausted = false;
            
            // 力竭结束后关闭遮罩。
            if (exhaustedOverlay != null)
            {
                exhaustedOverlay.gameObject.SetActive(false);
            }
            
            // 解除力竭结束监听，避免重复绑定。
            StaminaSystem stamina = FindObjectOfType<StaminaSystem>();
            if (stamina != null)
            {
                stamina.OnExhaustionEnd -= OnExhaustionEnd;
            }
        }
        
        #endregion
    }
}
