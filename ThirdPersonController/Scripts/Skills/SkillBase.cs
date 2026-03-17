using UnityEngine;

namespace ThirdPersonController
{
    public enum SkillCategory
    {
        None,
        CrowdControl,
        Burst,
        Mobility,
        Gather
    }

    public enum SkillDefenseTiming
    {
        None,
        OnCast,
        OnImpact,
        OnRecovery
    }

    /// <summary>
    /// 技能基类（ScriptableObject）。
    /// 提供统一的释放、冷却、特效与防御节奏流程。
    /// </summary>
    public abstract class SkillBase : ScriptableObject
    {
        [Header("基础信息")]
        public string skillName = "Skill Name";
        public string description = "Skill Description";
        public Sprite icon;
        public KeyCode keyBinding;

        [Header("设置")]
        public SkillCategory category = SkillCategory.None;
        
        [Header("设置")]
        public float cooldown = 10f;
        public float staminaCost = 20f;
        
        [Header("设置")]
        public int damage = 50;
        public float range = 5f;
        public float effectDuration = 2f;

        [Header("伤害类型")]
        public DamageElementType elementType = DamageElementType.Physical;
        public DamageCategory damageCategory = DamageCategory.Skill;
        public float breakValue = 0f;
        public float breakValueMultiplier = 1f;
        public float impactScale = 0.35f;

        [Header("动作控制")]
        public float castDuration = 0.5f;
        public bool lockMovement = true;
        public bool lockRotation = true;
        public bool interruptible = false;

        [Header("防御节奏")]
        public SkillDefenseTiming invincibilityTiming = SkillDefenseTiming.None;
        public float invincibilityDuration = 0f;
        [Range(0f, 0.95f)]
        public float damageReduction = 0f;
        public float damageReductionDuration = 0f;
        public SkillDefenseTiming damageReductionTiming = SkillDefenseTiming.None;
        public bool endsOnRecovery = true;

        [Header("设置")]
        public bool useAnimationEvents = true;
        public float impactDelay = 0.15f;
        public float recoveryDelay = 0.2f;
        public float impactShakeDuration = 0.1f;
        public float impactShakeStrength = 0.12f;

        [Header("默认节奏特效")]
        public bool useCategoryTint = true;
        public Color castTint = Color.white;
        public Color impactTint = Color.white;
        public float fallbackCastSize = 0.35f;
        public float fallbackImpactSize = 0.6f;
        
        [Header("视觉效果")]
        public GameObject effectPrefab;
        public GameObject castEffectPrefab;
        public GameObject impactEffectPrefab;
        public AudioClip castSound;
        public AudioClip hitSound;
        public AudioClip impactSound;
        
        // 运行时状态（不参与资源配置序列化）
        [System.NonSerialized]
        public float cooldownTimer = 0f;
        [System.NonSerialized]
        public bool isReady = true;

        [System.NonSerialized]
        public float cooldownDuration = 0f;

        [System.NonSerialized]
        protected SkillTimelineController timelineController;

        protected virtual void OnEnable()
        {
            if (impactScale <= 0f)
            {
                impactScale = GetDefaultImpactScale();
            }
        }
        
        /// <summary>
        /// 执行技能主逻辑（由子类实现）。
        /// </summary>
        /// <param name="caster">施法者</param>
        /// <param name="targetPosition">目标位置</param>
        public abstract void Execute(Transform caster, Vector3 targetPosition);

        public void ExecuteWithTimeline(Transform caster, Vector3 targetPosition, SkillTimelineController timelineController)
        {
            this.timelineController = timelineController;
            Execute(caster, targetPosition);
            this.timelineController = null;
        }

        public virtual float GetActionDuration()
        {
            return Mathf.Max(castDuration, GetTimelineDuration());
        }

        public virtual void OnInterrupted(Transform caster)
        {
        }
        
        /// <summary>
        /// 检查技能是否允许释放（冷却、耐力等）。
        /// </summary>
        public virtual bool CanExecute(Transform caster, StaminaSystem stamina)
        {
            // 先检查冷却就绪状态
            if (!isReady) return false;
            
            // 再检查耐力是否足够
            float cost = GetModifiedStaminaCost(caster);
            if (stamina != null && !stamina.HasEnoughStamina(cost))
            {
                GameEvents.ShowMessage("提示", 1f);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 启动技能冷却并广播事件。
        /// </summary>
        public virtual void StartCooldown(Transform caster)
        {
            cooldownDuration = GetModifiedCooldown(caster);
            cooldownTimer = cooldownDuration;
            isReady = false;
            
            // 通知 UI/系统进入冷却
            GameEvents.SkillUsed(skillName, cooldownDuration);
        }
        
        /// <summary>
        /// 更新Cooldown，保持显示与运行数据一致。
        /// </summary>
        public virtual void UpdateCooldown(float deltaTime)
        {
            if (!isReady && cooldownTimer > 0)
            {
                cooldownTimer -= deltaTime;
                if (cooldownTimer <= 0)
                {
                    cooldownTimer = 0;
                    isReady = true;
                    
                    // 冷却结束，触发就绪事件
                    GameEvents.SkillReady(skillName);
                }
            }
        }
        
        /// <summary>
        /// 获取冷却进度（0 = 就绪，1 = 刚进入冷却）。
        /// </summary>
        public float GetCooldownProgress()
        {
            if (isReady) return 0f;
            float duration = cooldownDuration > 0f ? cooldownDuration : cooldown;
            return duration <= 0f ? 0f : cooldownTimer / duration;
        }

        public float GetTimelineDuration()
        {
            float timeline = impactDelay + recoveryDelay;
            return Mathf.Max(castDuration, timeline);
        }
        
        /// <summary>
        /// 生成通用技能特效（旧接口兼容）。
        /// </summary>
        protected void SpawnEffect(Vector3 position, Quaternion rotation)
        {
            if (effectPrefab != null)
            {
                EffectPoolManager.SpawnEffect(effectPrefab, position, rotation, effectDuration);
            }
        }
        
        /// <summary>
        /// 播放技能音效（优先走 AudioManager）。
        /// </summary>
        protected void PlaySound(AudioClip clip, Vector3 position)
        {
            if (clip != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFXAtPosition(clip, position);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(clip, position);
                }
            }
        }

        protected void PlayCastFX(Vector3 position, Quaternion rotation)
        {
            if (castEffectPrefab != null)
            {
                EffectPoolManager.SpawnEffect(castEffectPrefab, position, rotation, effectDuration);
            }
            else
            {
                SpawnFallbackBurst(position, rotation, GetTint(castTint), fallbackCastSize);
            }

            PlaySound(castSound, position);
        }

        protected void PlayImpactFX(Vector3 position, Quaternion rotation)
        {
            GameObject prefab = impactEffectPrefab != null ? impactEffectPrefab : effectPrefab;
            if (prefab != null)
            {
                EffectPoolManager.SpawnEffect(prefab, position, rotation, effectDuration);
            }
            else
            {
                SpawnFallbackBurst(position, rotation, GetTint(impactTint), fallbackImpactSize);
            }

            AudioClip clip = impactSound != null ? impactSound : hitSound;
            PlaySound(clip, position);

            if (impactShakeStrength > 0f && ScreenEffectManager.Instance != null)
            {
                ScreenEffectManager.Instance.ShakeCamera(impactShakeDuration, impactShakeStrength, 10);
            }
        }

        private void SpawnFallbackBurst(Vector3 position, Quaternion rotation, Color tint, float size)
        {
            GameObject effect = new GameObject("SkillBurstFX");
            effect.transform.position = position;
            effect.transform.rotation = rotation;

            ParticleSystem particleSystem = effect.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.startLifetime = 0.4f;
            main.startSpeed = 2f;
            main.startSize = size;
            main.startColor = tint;
            main.loop = false;

            var emission = particleSystem.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 18)
            });

            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.2f;

            particleSystem.Play();
            Destroy(effect, effectDuration);
        }

        private Color GetTint(Color fallback)
        {
            if (!useCategoryTint)
            {
                return fallback;
            }

            switch (category)
            {
                case SkillCategory.CrowdControl:
                    return new Color(0.4f, 0.7f, 1f, 0.85f);
                case SkillCategory.Burst:
                    return new Color(1f, 0.5f, 0.4f, 0.85f);
                case SkillCategory.Mobility:
                    return new Color(0.5f, 1f, 0.6f, 0.85f);
                case SkillCategory.Gather:
                    return new Color(0.8f, 0.6f, 1f, 0.85f);
                default:
                    return new Color(fallback.r, fallback.g, fallback.b, 0.85f);
            }
        }

        protected void StartSkillTimeline(Transform caster, Vector3 impactPosition, Quaternion impactRotation,
            System.Action onImpact, System.Action onRecovery = null)
        {
            if (caster == null)
            {
                onImpact?.Invoke();
                onRecovery?.Invoke();
                return;
            }

            ApplyDefense(caster, SkillDefenseTiming.OnCast);

            MonoBehaviour runner = caster.GetComponent<MonoBehaviour>();
            if (runner == null)
            {
                PlayCastFX(caster.position, caster.rotation);
                ApplyDefense(caster, SkillDefenseTiming.OnImpact);
                onImpact?.Invoke();
                PlayImpactFX(impactPosition, impactRotation);
                ApplyDefense(caster, SkillDefenseTiming.OnRecovery);
                onRecovery?.Invoke();
                if (endsOnRecovery)
                {
                    NotifySkillEnded(caster);
                }
                return;
            }

            System.Action impactWrapper = () =>
            {
                ApplyDefense(caster, SkillDefenseTiming.OnImpact);
                onImpact?.Invoke();
            };

            System.Action recoveryWrapper = () =>
            {
                ApplyDefense(caster, SkillDefenseTiming.OnRecovery);
                onRecovery?.Invoke();
                if (endsOnRecovery)
                {
                    NotifySkillEnded(caster);
                }
            };

            if (useAnimationEvents && timelineController != null)
            {
                PlayCastFX(caster.position, caster.rotation);
                timelineController.BeginTimeline(
                    impactDelay,
                    recoveryDelay,
                    () =>
                    {
                        impactWrapper?.Invoke();
                        PlayImpactFX(impactPosition, impactRotation);
                    },
                    recoveryWrapper);
                return;
            }

            runner.StartCoroutine(SkillTimelineRoutine(caster, impactPosition, impactRotation, impactWrapper, recoveryWrapper));
        }

        private System.Collections.IEnumerator SkillTimelineRoutine(Transform caster, Vector3 impactPosition, Quaternion impactRotation,
            System.Action onImpact, System.Action onRecovery)
        {
            PlayCastFX(caster.position, caster.rotation);

            if (impactDelay > 0f)
            {
                yield return new WaitForSeconds(impactDelay);
            }

            onImpact?.Invoke();
            PlayImpactFX(impactPosition, impactRotation);

            if (recoveryDelay > 0f)
            {
                yield return new WaitForSeconds(recoveryDelay);
            }

            onRecovery?.Invoke();
        }
        
        /// <summary>
        /// 统一消耗耐力（含属性修正）。
        /// </summary>
        public bool ConsumeStamina(StaminaSystem stamina, Transform caster)
        {
            if (stamina == null) return true;
            float cost = GetModifiedStaminaCost(caster);
            return stamina.ConsumeStamina(cost);
        }

        protected void NotifySkillEnded(Transform caster)
        {
            if (caster == null)
            {
                return;
            }

            SkillManager manager = caster.GetComponent<SkillManager>();
            if (manager != null)
            {
                manager.NotifySkillEnded(this);
            }
        }

        private void ApplyDefense(Transform caster, SkillDefenseTiming timing)
        {
            if (caster == null)
            {
                return;
            }

            PlayerHealth health = caster.GetComponent<PlayerHealth>();
            if (health == null)
            {
                return;
            }

            if (invincibilityTiming == timing && invincibilityDuration > 0f)
            {
                health.ApplyInvincibility(invincibilityDuration);
            }

            if (damageReductionTiming == timing && damageReduction > 0f && damageReductionDuration > 0f)
            {
                health.ApplyDamageReduction(damageReduction, damageReductionDuration);
            }
        }

        protected int GetModifiedDamage(Transform caster, int baseDamage)
        {
            PlayerStatsController stats = GetStatsController(caster);
            if (stats == null)
            {
                return baseDamage;
            }

            return stats.ApplySkillDamage(baseDamage);
        }

        protected float GetModifiedCooldown(Transform caster)
        {
            PlayerStatsController stats = GetStatsController(caster);
            if (stats == null)
            {
                return cooldown;
            }

            return stats.ApplySkillCooldown(cooldown);
        }

        protected float GetModifiedRange(Transform caster, float baseRange)
        {
            PlayerStatsController stats = GetStatsController(caster);
            if (stats == null)
            {
                return baseRange;
            }

            return stats.ApplySkillRange(baseRange);
        }

        protected float GetModifiedKnockback(Transform caster, float baseKnockback)
        {
            PlayerStatsController stats = GetStatsController(caster);
            if (stats == null)
            {
                return baseKnockback * GetImpactScale();
            }

            float value = stats.ApplySkillKnockback(baseKnockback);
            value *= GetImpactScale();
            return value;
        }

        protected float GetImpactScale()
        {
            float scale = impactScale > 0f ? impactScale : GetDefaultImpactScale();
            return Mathf.Max(0.1f, scale);
        }

        protected float GetDefaultImpactScale()
        {
            if (damageCategory == DamageCategory.Ultimate)
            {
                return 0.6f;
            }

            switch (category)
            {
                case SkillCategory.Burst:
                    return 0.45f;
                case SkillCategory.CrowdControl:
                    return 0.4f;
                case SkillCategory.Mobility:
                    return 0.25f;
                case SkillCategory.Gather:
                    return 0.3f;
                default:
                    return 0.35f;
            }
        }

        protected float GetModifiedBreakValue(Transform caster, float fallbackValue)
        {
            float value = breakValue > 0f ? breakValue : fallbackValue;
            value *= Mathf.Max(0f, breakValueMultiplier);
            return Mathf.Max(0f, value);
        }

        protected DamageElementType ResolveSkillElement(Transform caster)
        {
            PlayerStatsController stats = GetStatsController(caster);
            if (stats == null)
            {
                return elementType;
            }

            DamageElementType overrideType = stats.GetSkillElementType();
            return overrideType != DamageElementType.Physical ? overrideType : elementType;
        }

        protected float GetModifiedStaminaCost(Transform caster)
        {
            PlayerStatsController stats = GetStatsController(caster);
            if (stats == null)
            {
                return staminaCost;
            }

            return stats.ApplySkillStaminaCost(staminaCost);
        }

        protected EnemyHealth ResolveEnemyHealth(Collider target)
        {
            if (target == null)
            {
                return null;
            }

            EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                return enemyHealth;
            }

            enemyHealth = target.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                return enemyHealth;
            }

            if (target.attachedRigidbody != null)
            {
                enemyHealth = target.attachedRigidbody.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    return enemyHealth;
                }
            }

            return null;
        }

        protected EnemyAI ResolveEnemyAI(Collider target)
        {
            if (target == null)
            {
                return null;
            }

            EnemyAI enemyAI = target.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                return enemyAI;
            }

            enemyAI = target.GetComponentInParent<EnemyAI>();
            if (enemyAI != null)
            {
                return enemyAI;
            }

            if (target.attachedRigidbody != null)
            {
                enemyAI = target.attachedRigidbody.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    return enemyAI;
                }
            }

            return null;
        }

        protected float GetEnemyControlMultiplier(EnemyHealth enemyHealth)
        {
            if (enemyHealth == null)
            {
                return 1f;
            }

            return SkillEnemyInteractionTuning.GetControlMultiplier(category, enemyHealth.enemyType);
        }

        protected float GetEnemyDisplacementMultiplier(EnemyHealth enemyHealth)
        {
            if (enemyHealth == null)
            {
                return 1f;
            }

            return SkillEnemyInteractionTuning.GetDisplacementMultiplier(category, enemyHealth.enemyType);
        }

        private PlayerStatsController GetStatsController(Transform caster)
        {
            if (caster == null)
            {
                return null;
            }

            PlayerStatsController stats = caster.GetComponent<PlayerStatsController>();
            if (stats == null)
            {
                stats = caster.GetComponentInParent<PlayerStatsController>();
            }

            return stats;
        }
    }
}
