using UnityEngine;
using DG.Tweening;

namespace ThirdPersonController
{
    /// <summary>
    /// ScreenEffectManager 模块的核心实现，负责统一管理关键运行流程与对外接口。
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
        public SpriteRenderer colorOverlay; // UI 引用，用于驱动界面表现与信息同步。
        public Material distortionMaterial; // 运行时配置项，用于驱动模块行为并保持可调性。
        
        [Header("连击颜色")]
        public Color normalColor = Color.white;
        public Color tier1Color = new Color(1f, 1f, 1f, 0.1f); // 运行时配置项，用于驱动模块行为并保持可调性。
        public Color tier2Color = new Color(1f, 0.9f, 0.2f, 0.15f); // 运行时配置项，用于驱动模块行为并保持可调性。
        public Color tier3Color = new Color(1f, 0.3f, 0.2f, 0.2f); // 运行时配置项，用于驱动模块行为并保持可调性。
        public Color berserkColor = new Color(0.8f, 0.1f, 0.1f, 0.3f); // 运行时配置项，用于驱动模块行为并保持可调性。
        
        [Header("受伤效果")]
        public float damageFlashDuration = 0.2f;
        public float damageShakeStrength = 0.3f;

        [Header("命中震动")]
        public float flinchShakeDuration = 0.08f;
        public float flinchShakeStrength = 0.12f;
        public float knockbackShakeDuration = 0.12f;
        public float knockbackShakeStrength = 0.2f;
        public float knockdownShakeDuration = 0.18f;
        public float knockdownShakeStrength = 0.28f;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
// 围绕 镜头 执行该步骤，用于保证流程状态与后续分支一致。
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            if (cameraTransform == null && mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (colorOverlay != null)
            {
                colorOverlay.color = Color.clear;
            }
        }
        
        private void Start()
        {
// 围绕 SubscribeToEvents 执行该步骤，用于保证流程状态与后续分支一致。
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
            GameEvents.OnEnemyHit += OnEnemyHit;
        }
        
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnPlayerDamaged -= OnPlayerDamaged;
            GameEvents.OnComboChanged -= OnComboChanged;
            GameEvents.OnBerserkStateChanged -= OnBerserkStateChanged;
            GameEvents.OnDamageDealt -= OnDamageDealt;
            GameEvents.OnEnemyHit -= OnEnemyHit;
        }
        
        #region 相机震动
        
        /// <summary>
        /// 执行 Shake Camera 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void ShakeCamera(float duration, float strength, int vibrato = 10)
        {
            if (cameraTransform == null) return;
            
            cameraTransform.DOShakePosition(duration, strength, vibrato, 90, false, true);
        }
        
        /// <summary>
        /// 执行 Shake Camera 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void ShakeCamera()
        {
            ShakeCamera(defaultShakeDuration, defaultShakeStrength, defaultShakeVibrato);
        }
        
        /// <summary>
        /// 执行 Shake On Damage 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void ShakeOnDamage(float damagePercent)
        {
            float strength = damageShakeStrength * damagePercent;
            ShakeCamera(damageFlashDuration, strength);
        }
        
        #endregion
        
        #region 颜色滤镜
        
        /// <summary>
        /// 设置Screen Color，统一写入入口，便于约束副作用。
        /// </summary>
        public void SetScreenColor(Color color, float duration = 0.2f)
        {
            if (colorOverlay == null) return;
            
            colorOverlay.DOColor(color, duration);
        }
        
        /// <summary>
        /// 执行 Fade Out Screen Color 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void FadeOutScreenColor(float duration = 0.5f)
        {
            if (colorOverlay == null) return;
            
            colorOverlay.DOColor(Color.clear, duration);
        }
        
        /// <summary>
        /// 设置Combo Color，统一写入入口，便于约束副作用。
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
        /// 执行 Damage Flash 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void DamageFlash()
        {
            if (colorOverlay == null) return;
            
// 围绕 colorOverlay 执行该步骤，用于保证流程状态与后续分支一致。
            colorOverlay.color = new Color(1f, 0, 0, 0.3f);
            colorOverlay.DOColor(Color.clear, damageFlashDuration);
            
// 围绕 镜头 执行该步骤，用于保证流程状态与后续分支一致。
            ShakeCamera(damageFlashDuration, damageShakeStrength);
        }
        
        /// <summary>
        /// 执行 Enter Berserk Mode 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void EnterBerserkMode(float duration)
        {
// 围绕 SetScreenColor 执行该步骤，用于保证流程状态与后续分支一致。
            SetScreenColor(berserkColor, 0.3f);
            
// 围绕 狂暴 执行该步骤，用于保证流程状态与后续分支一致。
            InvokeRepeating(nameof(BerserkShake), 0f, 0.1f);
            
// 围绕 狂暴 执行该步骤，用于保证流程状态与后续分支一致。
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
        /// 设置Time Scale，统一写入入口，便于约束副作用。
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
        /// 执行 Slow Motion 相关逻辑，并保证模块状态与外部调用约定一致。
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
                EnterBerserkMode(3f); // 围绕 狂暴 执行该步骤，用于保证流程状态与后续分支一致。
            }
        }
        
        private void OnDamageDealt(int damage, Vector3 position, bool isCritical)
        {
            if (isCritical)
            {
                ShakeCamera(0.1f, 0.1f, 5);
            }
        }

        private void OnEnemyHit(int damage, Vector3 position, EnemyHitReactionType reactionType)
        {
            switch (reactionType)
            {
                case EnemyHitReactionType.Knockdown:
                    ShakeCamera(knockdownShakeDuration, knockdownShakeStrength, 10);
                    break;
                case EnemyHitReactionType.Knockback:
                    ShakeCamera(knockbackShakeDuration, knockbackShakeStrength, 8);
                    break;
                default:
                    ShakeCamera(flinchShakeDuration, flinchShakeStrength, 6);
                    break;
            }
        }
        
        #endregion
    }
}
