using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController
{
    /// <summary>
    /// 血条UI - 显示玩家生命值
    /// </summary>
    public class UI_HPBar : MonoBehaviour
    {
        [Header("UI引用")]
        public Slider hpSlider;              // 血量滑动条
        public Image fillImage;              // 填充图片
        public Text hpText;                  // 血量文字 (可选)
        
        [Header("颜色设置")]
        public Color fullHealthColor = new Color(0.2f, 0.8f, 0.2f);      // 满血绿色
        public Color midHealthColor = new Color(0.9f, 0.9f, 0.2f);       // 中血黄色
        public Color lowHealthColor = new Color(0.9f, 0.2f, 0.2f);       // 低血红
        public float lowHealthThreshold = 0.3f;  // 低血阈值
        public float midHealthThreshold = 0.6f;  // 中血阈值
        
        [Header("动画设置")]
        public bool useSmoothFill = true;
        public float fillSpeed = 5f;         // 填充动画速度
        
        [Header("受伤效果")]
        public Image damageFlashImage;       // 受伤红屏图片
        public float flashDuration = 0.2f;   // 闪烁持续时间
        
        private float targetFillAmount = 1f;
        private float currentFillAmount = 1f;
        
        private void Start()
        {
            // 订阅事件
            GameEvents.OnPlayerDamaged += OnPlayerDamaged;
            GameEvents.OnPlayerHealed += OnPlayerHealed;
            
            // 查找玩家并订阅其血条变化事件
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += OnPlayerHealthChanged;
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅
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
            // 平滑更新血量条
            if (useSmoothFill && hpSlider != null)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * fillSpeed);
                hpSlider.value = currentFillAmount;
            }
        }
        
        /// <summary>
        /// 更新血量显示
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
            
            // 更新文字
            if (hpText != null)
            {
                hpText.text = $"{Mathf.Ceil(current)}/{max}";
            }
            
            // 更新颜色
            UpdateColor(targetFillAmount);
        }
        
        /// <summary>
        /// 根据血量更新颜色
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
            // 受伤闪烁效果
            if (damageFlashImage != null)
            {
                StartCoroutine(DamageFlash());
            }
        }
        
        private void OnPlayerHealed(int amount)
        {
            // 治疗效果（可选）
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
