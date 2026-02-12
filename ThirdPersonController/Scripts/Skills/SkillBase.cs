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

    /// <summary>
    /// 技能基类 - ScriptableObject
    /// 所有技能都继承此类
    /// </summary>
    public abstract class SkillBase : ScriptableObject
    {
        [Header("基础信息")]
        public string skillName = "新技能";
        public string description = "技能描述";
        public Sprite icon;
        public KeyCode keyBinding;

        [Header("技能分类")]
        public SkillCategory category = SkillCategory.None;
        
        [Header("冷却与消耗")]
        public float cooldown = 10f;
        public float staminaCost = 20f;
        
        [Header("伤害与效果")]
        public int damage = 50;
        public float range = 5f;
        public float effectDuration = 2f;

        [Header("动作控制")]
        public float castDuration = 0.5f;
        public bool lockMovement = true;
        public bool lockRotation = true;
        public bool interruptible = false;

        [Header("节奏点")]
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
        
        // 运行时数据（不保存到ScriptableObject）
        [System.NonSerialized]
        public float cooldownTimer = 0f;
        [System.NonSerialized]
        public bool isReady = true;

        [System.NonSerialized]
        protected SkillTimelineController timelineController;
        
        /// <summary>
        /// 执行技能
        /// </summary>
        /// <param name="caster">施法者（玩家）</param>
        /// <param name="targetPosition">目标位置</param>
        public abstract void Execute(Transform caster, Vector3 targetPosition);

        public void ExecuteWithTimeline(Transform caster, Vector3 targetPosition, SkillTimelineController timelineController)
        {
            this.timelineController = timelineController;
            Execute(caster, targetPosition);
            this.timelineController = null;
        }
        
        /// <summary>
        /// 检查是否可以释放技能
        /// </summary>
        public virtual bool CanExecute(Transform caster, StaminaSystem stamina)
        {
            // 检查冷却
            if (!isReady) return false;
            
            // 检查耐力
            if (stamina != null && !stamina.HasEnoughStamina(staminaCost))
            {
                GameEvents.ShowMessage("耐力不足！", 1f);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 开始冷却
        /// </summary>
        public virtual void StartCooldown()
        {
            cooldownTimer = cooldown;
            isReady = false;
            
            // 触发技能使用事件
            GameEvents.SkillUsed(skillName, cooldown);
        }
        
        /// <summary>
        /// 更新冷却（每帧调用）
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
                    
                    // 触发冷却完成事件
                    GameEvents.SkillReady(skillName);
                }
            }
        }
        
        /// <summary>
        /// 获取冷却进度（0-1）
        /// </summary>
        public float GetCooldownProgress()
        {
            if (isReady) return 0f;
            return cooldownTimer / cooldown;
        }

        public float GetTimelineDuration()
        {
            float timeline = impactDelay + recoveryDelay;
            return Mathf.Max(castDuration, timeline);
        }
        
        /// <summary>
        /// 播放特效
        /// </summary>
        protected void SpawnEffect(Vector3 position, Quaternion rotation)
        {
            if (effectPrefab != null)
            {
                EffectPoolManager.SpawnEffect(effectPrefab, position, rotation, effectDuration);
            }
        }
        
        /// <summary>
        /// 播放音效
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

            MonoBehaviour runner = caster.GetComponent<MonoBehaviour>();
            if (runner == null)
            {
                PlayCastFX(caster.position, caster.rotation);
                onImpact?.Invoke();
                PlayImpactFX(impactPosition, impactRotation);
                onRecovery?.Invoke();
                return;
            }

            if (useAnimationEvents && timelineController != null)
            {
                PlayCastFX(caster.position, caster.rotation);
                timelineController.BeginTimeline(
                    impactDelay,
                    recoveryDelay,
                    () =>
                    {
                        onImpact?.Invoke();
                        PlayImpactFX(impactPosition, impactRotation);
                    },
                    onRecovery);
                return;
            }

            runner.StartCoroutine(SkillTimelineRoutine(caster, impactPosition, impactRotation, onImpact, onRecovery));
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
        /// 消耗耐力
        /// </summary>
        public bool ConsumeStamina(StaminaSystem stamina)
        {
            if (stamina == null) return true;
            return stamina.ConsumeStamina(staminaCost);
        }
    }
}
