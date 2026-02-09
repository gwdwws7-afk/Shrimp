using UnityEngine;
using DG.Tweening;

namespace ThirdPersonController
{
    /// <summary>
    /// 屏幕特效管理器 - 管理屏幕震动、颜色滤镜、后处理效果
    /// </summary>
    public class ScreenEffectManager : MonoBehaviour
    {
        public static ScreenEffectManager Instance { get; private set; }
        
        [Header("相机引用")]
        public Camera mainCamera;
        public Transform cameraTransform;
        
        [Header("震动设置")]
        public float defaultShakeDuration = 0.3f;
        public float defaultShakeStrength = 0.5f;
        public int defaultShakeVibrato = 10;
        
        [Header("颜色滤镜")]
        public SpriteRenderer colorOverlay;      // 全屏颜色覆盖
        public Material distortionMaterial;      // 扭曲材质（可选）
        
        [Header("连击颜色")]
        public Color normalColor = Color.white;
        public Color tier1Color = new Color(1f, 1f, 1f, 0.1f);      // 轻微白
        public Color tier2Color = new Color(1f, 0.9f, 0.2f, 0.15f); // 淡黄
        public Color tier3Color = new Color(1f, 0.3f, 0.2f, 0.2f);  // 淡红
        public Color berserkColor = new Color(0.8f, 0.1f, 0.1f, 0.3f); // 深红
        
        [Header("受伤效果")]
        public float damageFlashDuration = 0.2f;
        public float damageShakeStrength = 0.3f;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            // 自动查找相机
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            if (cameraTransform == null && mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            
            // 初始状态
            if (colorOverlay != null)
            {
                colorOverlay.color = Color.clear;
            }
        }
        
        private void Start()
        {
            // 订阅事件
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void SubscribeToEvents()
        {
            GameEvents.OnPlayerDamaged += OnPlayerDamaged;
            GameEvents.OnComboChanged += OnComboChanged;
            GameEvents.OnBerserkStateChanged += OnBerserkStateChanged;
            GameEvents.OnDamageDealt += OnDamageDealt;
        }
        
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnPlayerDamaged -= OnPlayerDamaged;
            GameEvents.OnComboChanged -= OnComboChanged;
            GameEvents.OnBerserkStateChanged -= OnBerserkStateChanged;
            GameEvents.OnDamageDealt -= OnDamageDealt;
        }
        
        #region 相机震动
        
        /// <summary>
        /// 震动相机
        /// </summary>
        public void ShakeCamera(float duration, float strength, int vibrato = 10)
        {
            if (cameraTransform == null) return;
            
            cameraTransform.DOShakePosition(duration, strength, vibrato, 90, false, true);
        }
        
        /// <summary>
        /// 震动相机（使用默认参数）
        /// </summary>
        public void ShakeCamera()
        {
            ShakeCamera(defaultShakeDuration, defaultShakeStrength, defaultShakeVibrato);
        }
        
        /// <summary>
        /// 根据伤害震动
        /// </summary>
        public void ShakeOnDamage(float damagePercent)
        {
            float strength = damageShakeStrength * damagePercent;
            ShakeCamera(damageFlashDuration, strength);
        }
        
        #endregion
        
        #region 颜色滤镜
        
        /// <summary>
        /// 设置屏幕颜色
        /// </summary>
        public void SetScreenColor(Color color, float duration = 0.2f)
        {
            if (colorOverlay == null) return;
            
            colorOverlay.DOColor(color, duration);
        }
        
        /// <summary>
        /// 淡出屏幕颜色
        /// </summary>
        public void FadeOutScreenColor(float duration = 0.5f)
        {
            if (colorOverlay == null) return;
            
            colorOverlay.DOColor(Color.clear, duration);
        }
        
        /// <summary>
        /// 根据连击等级设置颜色
        /// </summary>
        public void SetComboColor(int combo)
        {
            Color targetColor;
            
            if (combo >= 50)
            {
                targetColor = berserkColor;
            }
            else if (combo >= 31)
            {
                targetColor = tier3Color;
            }
            else if (combo >= 11)
            {
                targetColor = tier2Color;
            }
            else if (combo >= 1)
            {
                targetColor = tier1Color;
            }
            else
            {
                targetColor = Color.clear;
            }
            
            SetScreenColor(targetColor);
        }
        
        /// <summary>
        /// 受伤闪烁
        /// </summary>
        public void DamageFlash()
        {
            if (colorOverlay == null) return;
            
            // 红屏闪烁
            colorOverlay.color = new Color(1f, 0, 0, 0.3f);
            colorOverlay.DOColor(Color.clear, damageFlashDuration);
            
            // 震动
            ShakeCamera(damageFlashDuration, damageShakeStrength);
        }
        
        /// <summary>
        /// 狂暴模式特效
        /// </summary>
        public void EnterBerserkMode(float duration)
        {
            // 深红滤镜
            SetScreenColor(berserkColor, 0.3f);
            
            // 持续震动
            InvokeRepeating(nameof(BerserkShake), 0f, 0.1f);
            
            // 持续时间后恢复
            Invoke(nameof(ExitBerserkEffects), duration);
        }
        
        private void BerserkShake()
        {
            ShakeCamera(0.1f, 0.2f, 5);
        }
        
        private void ExitBerserkEffects()
        {
            CancelInvoke(nameof(BerserkShake));
            FadeOutScreenColor(0.5f);
        }
        
        #endregion
        
        #region 后处理效果（如使用Post Processing）
        
        /// <summary>
        /// 设置时间缩放（慢动作效果）
        /// </summary>
        public void SetTimeScale(float scale, float duration = 0.5f)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * scale;
            
            if (duration > 0)
            {
                Invoke(nameof(ResetTimeScale), duration);
            }
        }
        
        private void ResetTimeScale()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
        
        /// <summary>
        /// 慢动作效果
        /// </summary>
        public void SlowMotion(float targetScale = 0.3f, float duration = 1f)
        {
            SetTimeScale(targetScale, duration);
        }
        
        #endregion
        
        #region 事件处理
        
        private void OnPlayerDamaged(float damage, Vector3 source)
        {
            DamageFlash();
        }
        
        private void OnComboChanged(int combo)
        {
            SetComboColor(combo);
        }
        
        private void OnBerserkStateChanged(bool isActive)
        {
            if (isActive)
            {
                EnterBerserkMode(3f);  // 3秒狂暴
            }
        }
        
        private void OnDamageDealt(int damage, Vector3 position, bool isCritical)
        {
            if (isCritical)
            {
                ShakeCamera(0.1f, 0.1f, 5);
            }
        }
        
        #endregion
    }
}
