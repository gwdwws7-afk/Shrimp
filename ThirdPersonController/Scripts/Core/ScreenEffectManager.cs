using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ThirdPersonController
{
    public class ScreenEffectManager : MonoBehaviour
    {
        public static ScreenEffectManager Instance { get; private set; }

        [Header("Camera")]
        public Camera mainCamera;
        public Transform cameraTransform;

        [Header("Shake")]
        public float defaultShakeDuration = 0.3f;
        public float defaultShakeStrength = 0.5f;
        public int defaultShakeVibrato = 10;

        [Header("Overlay")]
        public SpriteRenderer colorOverlay;
        public Material distortionMaterial;

        [Header("Combo Colors")]
        public Color normalColor = Color.white;
        public Color tier1Color = new Color(1f, 1f, 1f, 0.1f);
        public Color tier2Color = new Color(1f, 0.9f, 0.2f, 0.15f);
        public Color tier3Color = new Color(1f, 0.3f, 0.2f, 0.2f);
        public Color berserkColor = new Color(0.8f, 0.1f, 0.1f, 0.3f);

        [Header("Damage")]
        public float damageFlashDuration = 0.2f;
        public float damageShakeStrength = 0.3f;

        [Header("Hit Shake")]
        public float flinchShakeDuration = 0.08f;
        public float flinchShakeStrength = 0.12f;
        public float knockbackShakeDuration = 0.12f;
        public float knockbackShakeStrength = 0.2f;
        public float knockdownShakeDuration = 0.18f;
        public float knockdownShakeStrength = 0.28f;

        [Header("Feedback Event Routing")]
        public bool useVfxEventRouting = true;
        public bool autoPopulateVfxRoutes = true;
        public List<CombatFeedbackVfxRoute> vfxEventRoutes = new List<CombatFeedbackVfxRoute>();

        [SerializeField] private bool debugLastVfxRouteApplied = false;
        [SerializeField] private CombatFeedbackEventId debugLastVfxEvent = CombatFeedbackEventId.EnemyHitFlinch;
        public bool LastVfxRouteApplied => debugLastVfxRouteApplied;
        public CombatFeedbackEventId LastVfxEvent => debugLastVfxEvent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (cameraTransform == null && mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }

            if (colorOverlay != null)
            {
                colorOverlay.color = Color.clear;
            }

            EnsureDefaultVfxRoutes();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
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
            GameEvents.OnEnemyKilled += OnEnemyKilled;
            GameEvents.OnBossBreakWindowStart += OnBossBreakWindowStart;
            GameEvents.OnSkillUsed += OnSkillUsed;
            GameEvents.OnStaminaDepleted += OnStaminaDepleted;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnPlayerDamaged -= OnPlayerDamaged;
            GameEvents.OnComboChanged -= OnComboChanged;
            GameEvents.OnBerserkStateChanged -= OnBerserkStateChanged;
            GameEvents.OnDamageDealt -= OnDamageDealt;
            GameEvents.OnEnemyHit -= OnEnemyHit;
            GameEvents.OnEnemyKilled -= OnEnemyKilled;
            GameEvents.OnBossBreakWindowStart -= OnBossBreakWindowStart;
            GameEvents.OnSkillUsed -= OnSkillUsed;
            GameEvents.OnStaminaDepleted -= OnStaminaDepleted;
        }

        public void ShakeCamera(float duration, float strength, int vibrato = 10)
        {
            if (cameraTransform == null)
            {
                return;
            }

            cameraTransform.DOShakePosition(duration, strength, vibrato, 90, false, true);
        }

        public void ShakeCamera()
        {
            ShakeCamera(defaultShakeDuration, defaultShakeStrength, defaultShakeVibrato);
        }

        public void ShakeOnDamage(float damagePercent)
        {
            float strength = damageShakeStrength * damagePercent;
            ShakeCamera(damageFlashDuration, strength);
        }

        public void SetScreenColor(Color color, float duration = 0.2f)
        {
            if (colorOverlay == null)
            {
                return;
            }

            colorOverlay.DOColor(color, duration);
        }

        public void FadeOutScreenColor(float duration = 0.5f)
        {
            if (colorOverlay == null)
            {
                return;
            }

            colorOverlay.DOColor(Color.clear, duration);
        }

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

        public void DamageFlash()
        {
            if (colorOverlay != null)
            {
                colorOverlay.color = new Color(1f, 0f, 0f, 0.3f);
                colorOverlay.DOColor(Color.clear, damageFlashDuration);
            }

            ShakeCamera(damageFlashDuration, damageShakeStrength);
        }

        public void EnterBerserkMode(float duration)
        {
            SetScreenColor(berserkColor, 0.3f);
            InvokeRepeating(nameof(BerserkShake), 0f, 0.1f);
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

        public void SetTimeScale(float scale, float duration = 0.5f)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * scale;
            if (duration > 0f)
            {
                Invoke(nameof(ResetTimeScale), duration);
            }
        }

        private void ResetTimeScale()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        public void SlowMotion(float targetScale = 0.3f, float duration = 1f)
        {
            SetTimeScale(targetScale, duration);
        }

        private void EnsureDefaultVfxRoutes()
        {
            if (!autoPopulateVfxRoutes)
            {
                return;
            }

            EnsureVfxRoute(CombatFeedbackEventId.EnemyHitFlinch, true, flinchShakeDuration, flinchShakeStrength, 6, false, Color.clear, 0f);
            EnsureVfxRoute(CombatFeedbackEventId.EnemyHitKnockback, true, knockbackShakeDuration, knockbackShakeStrength, 8, false, Color.clear, 0f);
            EnsureVfxRoute(CombatFeedbackEventId.EnemyHitKnockdown, true, knockdownShakeDuration, knockdownShakeStrength, 10, false, Color.clear, 0f);
            EnsureVfxRoute(CombatFeedbackEventId.EnemyKilled, true, 0.09f, 0.14f, 6, false, Color.clear, 0f);
            EnsureVfxRoute(CombatFeedbackEventId.BerserkStart, true, 0.12f, 0.2f, 5, true, berserkColor, 0.2f);
            EnsureVfxRoute(CombatFeedbackEventId.BossBreakWindowStart, true, 0.18f, 0.34f, 10, true, new Color(1f, 0.9f, 0.35f, 0.28f), 0.16f);
            EnsureVfxRoute(CombatFeedbackEventId.SkillUsed, true, 0.08f, 0.1f, 5, false, Color.clear, 0f);
            EnsureVfxRoute(CombatFeedbackEventId.StaminaDepleted, true, 0.1f, 0.16f, 6, true, new Color(0.75f, 0.95f, 1f, 0.15f), 0.15f);
        }

        private void EnsureVfxRoute(
            CombatFeedbackEventId eventId,
            bool shakeCamera,
            float shakeDuration,
            float shakeStrength,
            int shakeVibrato,
            bool flashOverlay,
            Color flashColor,
            float flashDuration)
        {
            if (vfxEventRoutes == null)
            {
                vfxEventRoutes = new List<CombatFeedbackVfxRoute>();
            }

            for (int i = 0; i < vfxEventRoutes.Count; i++)
            {
                CombatFeedbackVfxRoute route = vfxEventRoutes[i];
                if (route != null && route.eventId == eventId)
                {
                    return;
                }
            }

            vfxEventRoutes.Add(new CombatFeedbackVfxRoute
            {
                eventId = eventId,
                shakeCamera = shakeCamera,
                shakeDuration = Mathf.Max(0f, shakeDuration),
                shakeStrength = Mathf.Max(0f, shakeStrength),
                shakeVibrato = Mathf.Max(0, shakeVibrato),
                flashOverlay = flashOverlay,
                flashColor = flashColor,
                flashDuration = Mathf.Max(0f, flashDuration)
            });
        }

        private bool TryApplyMappedVfx(CombatFeedbackEventId eventId)
        {
            debugLastVfxRouteApplied = false;
            if (!useVfxEventRouting || vfxEventRoutes == null || vfxEventRoutes.Count == 0)
            {
                return false;
            }

            CombatFeedbackVfxRoute route = null;
            for (int i = 0; i < vfxEventRoutes.Count; i++)
            {
                CombatFeedbackVfxRoute candidate = vfxEventRoutes[i];
                if (candidate != null && candidate.eventId == eventId)
                {
                    route = candidate;
                    break;
                }
            }

            if (route == null)
            {
                return false;
            }

            bool applied = false;
            if (route.shakeCamera)
            {
                ShakeCamera(route.shakeDuration, route.shakeStrength, route.shakeVibrato);
                applied = true;
            }

            if (route.flashOverlay && colorOverlay != null)
            {
                colorOverlay.color = route.flashColor;
                colorOverlay.DOColor(Color.clear, Mathf.Max(0.01f, route.flashDuration));
                applied = true;
            }

            if (applied)
            {
                debugLastVfxRouteApplied = true;
                debugLastVfxEvent = eventId;
            }

            return applied;
        }

        private static CombatFeedbackEventId ResolveEnemyHitEvent(EnemyHitReactionType reactionType)
        {
            switch (reactionType)
            {
                case EnemyHitReactionType.Knockback:
                    return CombatFeedbackEventId.EnemyHitKnockback;
                case EnemyHitReactionType.Knockdown:
                    return CombatFeedbackEventId.EnemyHitKnockdown;
                default:
                    return CombatFeedbackEventId.EnemyHitFlinch;
            }
        }

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
            if (!isActive)
            {
                return;
            }

            if (TryApplyMappedVfx(CombatFeedbackEventId.BerserkStart))
            {
                return;
            }

            EnterBerserkMode(3f);
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
            if (TryApplyMappedVfx(ResolveEnemyHitEvent(reactionType)))
            {
                return;
            }

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

        private void OnEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            TryApplyMappedVfx(CombatFeedbackEventId.EnemyKilled);
        }

        private void OnBossBreakWindowStart()
        {
            TryApplyMappedVfx(CombatFeedbackEventId.BossBreakWindowStart);
        }

        private void OnSkillUsed(string skillName, float cooldown)
        {
            TryApplyMappedVfx(CombatFeedbackEventId.SkillUsed);
        }

        private void OnStaminaDepleted()
        {
            TryApplyMappedVfx(CombatFeedbackEventId.StaminaDepleted);
        }
    }
}
