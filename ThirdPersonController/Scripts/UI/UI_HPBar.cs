using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController
{
    /// <summary>
    /// UI_HPBar 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    public class UI_HPBar : MonoBehaviour
    {
        [Header("UI引用")]
        public Slider hpSlider;              // 生命条 Slider
        public Image fillImage; // 运行时配置项，用于驱动模块行为并保持可调性。
        public Text hpText; // 生命数值文本，用于维持信息可读性与界面层次。
        
        [Header("颜色设置")]
        public Color fullHealthColor = new Color(0.2f, 0.8f, 0.2f); // 运行时配置项，用于驱动模块行为并保持可调性。
        public Color midHealthColor = new Color(0.9f, 0.9f, 0.2f); // 运行时配置项，用于驱动模块行为并保持可调性。
        public Color lowHealthColor = new Color(0.9f, 0.2f, 0.2f); // 运行时配置项，用于驱动模块行为并保持可调性。
        public float lowHealthThreshold = 0.3f; // 低血量阈值（<=），用于控制分段逻辑并防止越界。
        public float midHealthThreshold = 0.6f; // 中血量阈值（<=），用于控制分段逻辑并防止越界。
        
        [Header("动画设置")]
        public bool useSmoothFill = true;
        public float fillSpeed = 5f; // 速度参数，用于调节手感与反馈节奏。
        
        [Header("受伤效果")]
        public Image damageFlashImage; // 运行时配置项，用于驱动模块行为并保持可调性。
        public float flashDuration = 0.2f; // 闪烁持续时间，用于定义效果生效窗口。
        
        private float targetFillAmount = 1f;
        private float currentFillAmount = 1f;
        
        private void Start()
        {
            // 订阅受伤与治疗事件。
            GameEvents.OnPlayerDamaged += OnPlayerDamaged;
            GameEvents.OnPlayerHealed += OnPlayerHealed;
            
            // 绑定血量变化回调，支持实时刷新显示。
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += OnPlayerHealthChanged;
            }
        }
        
        private void OnDestroy()
        {
            // 反注册全局事件，避免销毁后仍被回调。
            GameEvents.OnPlayerDamaged -= OnPlayerDamaged;
            GameEvents.OnPlayerHealed -= OnPlayerHealed;
            
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= OnPlayerHealthChanged;
            }
        }
        
        private void Update()
        {
            // 平滑模式下逐帧向目标值插值，减少跳变感。
            if (useSmoothFill && hpSlider != null)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * fillSpeed);
                hpSlider.value = currentFillAmount;
            }
        }
        
        /// <summary>
        /// 更新HP，保持显示与运行数据一致。
        /// </summary>
        public void UpdateHP(float current, float max)
        {
            if (hpSlider != null)
            {
                targetFillAmount = current / max;
                
                if (!useSmoothFill)
                {
                    hpSlider.value = targetFillAmount;
                    currentFillAmount = targetFillAmount;
                }
            }
            
// 刷新生命数值文本，用于维持信息可读性与界面层次。
            if (hpText != null)
            {
                hpText.text = $"{Mathf.Ceil(current)}/{max}";
            }
            
            // 按当前生命百分比刷新条体颜色。
            UpdateColor(targetFillAmount);
        }
        
        /// <summary>
        /// 按血量区间更新颜色。
        /// </summary>
        private void UpdateColor(float percent)
        {
            if (fillImage == null) return;
            
            if (percent <= lowHealthThreshold)
            {
                fillImage.color = lowHealthColor;
            }
            else if (percent <= midHealthThreshold)
            {
                fillImage.color = midHealthColor;
            }
            else
            {
                fillImage.color = fullHealthColor;
            }
        }
        
        #region 事件处理
        
        private void OnPlayerDamaged(float damage, Vector3 source)
        {
            // 受击时触发短暂闪烁反馈。
            if (damageFlashImage != null)
            {
                StartCoroutine(DamageFlash());
            }
        }
        
        private void OnPlayerHealed(int amount)
        {
            // 治疗后的数值刷新由 OnPlayerHealthChanged 统一处理。
        }
        
        private void OnPlayerHealthChanged(int current, int max)
        {
            UpdateHP(current, max);
        }
        
        #endregion
        
        #region 受伤闪烁
        
        private System.Collections.IEnumerator DamageFlash()
        {
            if (damageFlashImage == null) yield break;
            
            damageFlashImage.gameObject.SetActive(true);
            
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                float alpha = Mathf.Lerp(0.5f, 0f, elapsed / flashDuration);
                Color color = damageFlashImage.color;
                color.a = alpha;
                damageFlashImage.color = color;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            damageFlashImage.gameObject.SetActive(false);
        }
        
        #endregion
    }
}
