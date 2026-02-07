using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController
{
    /// <summary>
    /// 耐力条UI - 显示玩家耐力值
    /// </summary>
    public class UI_StaminaBar : MonoBehaviour
    {
        [Header("UI引用")]
        public Slider staminaSlider;         // 耐力滑动条
        public Image fillImage;              // 填充图片
        public Text staminaText;             // 耐力文字 (可选)
        
        [Header("颜色设置")]
        public Color normalColor = new Color(0.9f, 0.7f, 0.2f);      // 正常黄色
        public Color lowColor = new Color(0.9f, 0.3f, 0.1f);         // 低耐力橙红
        public Color exhaustedColor = new Color(0.3f, 0.3f, 0.3f);   // 力竭灰色
        public float lowThreshold = 0.3f;    // 低耐力阈值
        
        [Header("动画设置")]
        public bool useSmoothFill = true;
        public float fillSpeed = 8f;         // 填充动画速度
        
        [Header("力竭效果")]
        public Image exhaustedOverlay;       // 力竭遮罩
        public float pulseSpeed = 2f;        // 闪烁速度
        
        private float targetFillAmount = 1f;
        private float currentFillAmount = 1f;
        private bool isExhausted = false;
        
        private void Start()
        {
            // 订阅事件
            GameEvents.OnStaminaChanged += OnStaminaChanged;
            GameEvents.OnStaminaDepleted += OnStaminaDepleted;
        }
        
        private void OnDestroy()
        {
            // 取消订阅
            GameEvents.OnStaminaChanged -= OnStaminaChanged;
            GameEvents.OnStaminaDepleted -= OnStaminaDepleted;
        }
        
        private void Update()
        {
            // 平滑更新耐力条
            if (useSmoothFill && staminaSlider != null)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * fillSpeed);
                staminaSlider.value = currentFillAmount;
            }
            
            // 力竭闪烁效果
            if (isExhausted && exhaustedOverlay != null)
            {
                float alpha = Mathf.PingPong(Time.time * pulseSpeed, 0.3f);
                Color color = exhaustedOverlay.color;
                color.a = alpha;
                exhaustedOverlay.color = color;
            }
        }
        
        /// <summary>
        /// 更新耐力显示
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
            
            // 更新文字
            if (staminaText != null)
            {
                staminaText.text = $"{Mathf.Ceil(current)}/{max}";
            }
            
            // 更新颜色
            UpdateColor(targetFillAmount);
        }
        
        /// <summary>
        /// 根据耐力更新颜色
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
            
            // 显示力竭效果
            if (exhaustedOverlay != null)
            {
                exhaustedOverlay.gameObject.SetActive(true);
            }
            
            // 订阅力竭结束事件
            StaminaSystem stamina = FindObjectOfType<StaminaSystem>();
            if (stamina != null)
            {
                stamina.OnExhaustionEnd += OnExhaustionEnd;
            }
        }
        
        private void OnExhaustionEnd()
        {
            isExhausted = false;
            
            // 隐藏力竭效果
            if (exhaustedOverlay != null)
            {
                exhaustedOverlay.gameObject.SetActive(false);
            }
            
            // 取消订阅
            StaminaSystem stamina = FindObjectOfType<StaminaSystem>();
            if (stamina != null)
            {
                stamina.OnExhaustionEnd -= OnExhaustionEnd;
            }
        }
        
        #endregion
    }
}
