using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ThirdPersonController
{
    /// <summary>
    /// UI_ComboCounter 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    public class UI_ComboCounter : MonoBehaviour
    {
        [Header("UI引用")]
        public Text comboText; // 连击数值文本，用于维持信息可读性与界面层次。
        public Image comboGauge; // 运行时配置项，用于驱动模块行为并保持可调性。
        public CanvasGroup canvasGroup; // UI 透明度组件，用于缓存依赖并减少重复查找。
        public PlayerCombat combat;
        
        [Header("等级颜色")]
        public Color tier1Color = Color.white; // 运行时配置项，用于驱动模块行为并保持可调性。
        public Color tier2Color = new Color(1f, 0.9f, 0.2f); // 运行时配置项，用于驱动模块行为并保持可调性。
        public Color tier3Color = new Color(1f, 0.3f, 0.2f); // 运行时配置项，用于驱动模块行为并保持可调性。
        public Color tier4Color = new Color(0.8f, 0.2f, 1f);     // 50+ 连击颜色（狂暴主色）
        
        [Header("动画设置")]
        public float punchScale = 1.3f;      // 连击增长时的文本放大倍率
        public float punchDuration = 0.2f;   // 连击增长时的文本放大持续时间
        public float displayDuration = 2f;   // 连击归零后保留显示时长
        public float fadeDuration = 0.5f; // 面板淡出时长，用于定义效果生效窗口。
        
        [Header("狂暴特效")]
        public GameObject berserkEffect;     // 狂暴状态附加特效物体
        public ParticleSystem berserkParticles; // 狂暴状态粒子系统，用于对外暴露当前流程阶段。
        
        private int currentCombo = 0;
        private float displayTimer = 0f;
        private bool isBerserk = false;
        
        private void Start()
        {
            // 开局隐藏连击面板，避免未进入战斗时出现 UI 闪烁。
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
            
            // 监听连击变化与狂暴状态切换。
            GameEvents.OnComboChanged += OnComboChanged;
            GameEvents.OnBerserkStateChanged += OnBerserkStateChanged;
        }
        
        private void OnDestroy()
        {
            // 销毁时解除事件监听，避免空引用回调。
            GameEvents.OnComboChanged -= OnComboChanged;
            GameEvents.OnBerserkStateChanged -= OnBerserkStateChanged;
        }
        
        private void Update()
        {
            // 连击归零后开始倒计时，到期后淡出。
            if (displayTimer > 0 && currentCombo == 0)
            {
                displayTimer -= Time.deltaTime;
                if (displayTimer <= 0)
                {
                    FadeOut();
                }
            }
            
            // 每帧刷新连击重置进度条。
            UpdateGauge();
        }
        
        /// <summary>
        /// 刷新连击文本、颜色与动效，并控制显示时机。
        /// </summary>
        public void UpdateCombo(int combo)
        {
            if (combo == 0)
            {
                // 归零时不立即隐藏，给玩家短暂观察窗口。
                displayTimer = displayDuration;
                return;
            }
            
            currentCombo = combo;
            
            // 有连击时立即显示并终止历史淡出补间。
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.DOKill();
            }
            
// 同步连击数字文本，用于维持信息可读性与界面层次。
            if (comboText != null)
            {
                comboText.text = combo.ToString();
                
                // 按连击档位切换颜色。
                comboText.color = GetTierColor(combo);
            }
            
            // 播放一次文字弹跳，强化增长反馈。
            if (comboText != null)
            {
                comboText.transform.DOKill();
                comboText.transform.localScale = Vector3.one;
                comboText.transform.DOPunchScale(Vector3.one * punchScale, punchDuration, 0, 0);
            }
            
            // 非零连击期间取消隐藏倒计时。
            displayTimer = 0f;
        }
        
        /// <summary>
        /// 根据连击档位返回颜色。
        /// </summary>
        private Color GetTierColor(int combo)
        {
            if (combo >= 50) return tier4Color;
            if (combo >= 31) return tier3Color;
            if (combo >= 11) return tier2Color;
            return tier1Color;
        }
        
        /// <summary>
        /// 更新连击倒计时进度条。
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
        /// 执行 Fade Out 相关逻辑，并保证模块状态与外部调用约定一致。
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
                // 进入狂暴时开启额外特效。
                if (berserkEffect != null)
                {
                    berserkEffect.SetActive(true);
                }
                
                if (berserkParticles != null)
                {
                    berserkParticles.Play();
                }
                
                // 狂暴期间将文本提升到最高档色并播放脉冲动画。
                if (comboText != null)
                {
                    comboText.color = tier4Color;
                    comboText.transform.DOScale(1.5f, 0.3f).SetLoops(2, LoopType.Yoyo);
                }
            }
            else
            {
                // 退出狂暴时关闭额外特效，恢复常态表现。
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
