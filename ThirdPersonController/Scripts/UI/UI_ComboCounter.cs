using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ThirdPersonController
{
    /// <summary>
    /// 连击计数器UI - 显示当前连击数和等级
    /// </summary>
    public class UI_ComboCounter : MonoBehaviour
    {
        [Header("UI引用")]
        public Text comboText;               // 连击数字文本
        public Image comboGauge;             // 连击进度条（倒计时）
        public CanvasGroup canvasGroup;      // 用于淡入淡出
        public PlayerCombat combat;
        
        [Header("等级颜色")]
        public Color tier1Color = Color.white;                    // Tier 1: 白色
        public Color tier2Color = new Color(1f, 0.9f, 0.2f);     // Tier 2: 黄色
        public Color tier3Color = new Color(1f, 0.3f, 0.2f);     // Tier 3: 红色
        public Color tier4Color = new Color(0.8f, 0.2f, 1f);     // Tier 4: 紫色（狂暴）
        
        [Header("动画设置")]
        public float punchScale = 1.3f;      // 跳动缩放
        public float punchDuration = 0.2f;   // 跳动持续时间
        public float displayDuration = 2f;   // 显示持续时间（连击结束后）
        public float fadeDuration = 0.5f;    // 淡出时间
        
        [Header("狂暴特效")]
        public GameObject berserkEffect;     // 狂暴特效物体
        public ParticleSystem berserkParticles;  // 狂暴粒子
        
        private int currentCombo = 0;
        private float displayTimer = 0f;
        private bool isBerserk = false;
        
        private void Start()
        {
            // 初始隐藏
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            
            if (berserkEffect != null)
            {
                berserkEffect.SetActive(false);
            }

            if (combat == null)
            {
                combat = FindObjectOfType<PlayerCombat>();
            }
            
            // 订阅事件
            GameEvents.OnComboChanged += OnComboChanged;
            GameEvents.OnBerserkStateChanged += OnBerserkStateChanged;
        }
        
        private void OnDestroy()
        {
            // 取消订阅
            GameEvents.OnComboChanged -= OnComboChanged;
            GameEvents.OnBerserkStateChanged -= OnBerserkStateChanged;
        }
        
        private void Update()
        {
            // 处理显示计时器
            if (displayTimer > 0 && currentCombo == 0)
            {
                displayTimer -= Time.deltaTime;
                if (displayTimer <= 0)
                {
                    FadeOut();
                }
            }
            
            // 更新进度条（连击倒计时）
            UpdateGauge();
        }
        
        /// <summary>
        /// 更新连击显示
        /// </summary>
        public void UpdateCombo(int combo)
        {
            if (combo == 0)
            {
                // 连击重置，开始淡出计时
                displayTimer = displayDuration;
                return;
            }
            
            currentCombo = combo;
            
            // 显示UI
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.DOKill();
            }
            
            // 更新文本
            if (comboText != null)
            {
                comboText.text = combo.ToString();
                
                // 根据等级更新颜色
                comboText.color = GetTierColor(combo);
            }
            
            // 跳动动画
            if (comboText != null)
            {
                comboText.transform.DOKill();
                comboText.transform.localScale = Vector3.one;
                comboText.transform.DOPunchScale(Vector3.one * punchScale, punchDuration, 0, 0);
            }
            
            // 重置淡出计时器
            displayTimer = 0f;
        }
        
        /// <summary>
        /// 获取连击等级颜色
        /// </summary>
        private Color GetTierColor(int combo)
        {
            if (combo >= 50) return tier4Color;
            if (combo >= 31) return tier3Color;
            if (combo >= 11) return tier2Color;
            return tier1Color;
        }
        
        /// <summary>
        /// 更新进度条
        /// </summary>
        private void UpdateGauge()
        {
            if (comboGauge == null) return;

            if (combat == null)
            {
                comboGauge.fillAmount = 0f;
                return;
            }

            comboGauge.fillAmount = combat.ComboResetNormalized;
        }
        
        /// <summary>
        /// 淡出UI
        /// </summary>
        private void FadeOut()
        {
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, fadeDuration);
            }
        }
        
        #region 事件处理
        
        private void OnComboChanged(int combo)
        {
            UpdateCombo(combo);
        }
        
        private void OnBerserkStateChanged(bool isActive)
        {
            isBerserk = isActive;
            
            if (isActive)
            {
                // 狂暴模式启动
                if (berserkEffect != null)
                {
                    berserkEffect.SetActive(true);
                }
                
                if (berserkParticles != null)
                {
                    berserkParticles.Play();
                }
                
                // 改变文字颜色为紫色
                if (comboText != null)
                {
                    comboText.color = tier4Color;
                    comboText.transform.DOScale(1.5f, 0.3f).SetLoops(2, LoopType.Yoyo);
                }
            }
            else
            {
                // 狂暴模式结束
                if (berserkEffect != null)
                {
                    berserkEffect.SetActive(false);
                }
                
                if (berserkParticles != null)
                {
                    berserkParticles.Stop();
                }
            }
        }
        
        #endregion
    }
}
