using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ThirdPersonController
{
    // 连击等级枚举
    public enum ComboTier
    {
        None,       // 0连击
        Tier1,      // 1-10连击: +10%伤害
        Tier2,      // 11-30连击: +25%伤害
        Tier3,      // 31-50连击: +50%伤害, 吸血5%
        Tier4       // 50+连击: 深渊狂暴模式
    }

    public class PlayerCombat : MonoBehaviour
    {
        [Header("Attack Settings")]
        public float attackRange = 2f;
        public float attackAngle = 120f;
        public float attackCooldown = 0.5f;
        public int attackDamage = 25;
        public float attackKnockback = 5f;

        [Header("Combo Definition")]
        public AttackComboDefinition comboDefinition;
        public bool useAnimationEvents = true;
        public float inputBufferTime = 0.3f;
        public float hitStopDuration = 0.05f;
        public bool lockMovementDuringAttack = true;
        public bool lockRotationDuringAttack = false;

        [Header("Combo Settings")]
        public int maxComboCount = 50;              // 最大50连击
        public float comboResetTime = 1.5f;
        public float comboWindowTime = 0.8f;
        
        [Header("Combo Tier Settings")]
        public float tier1DamageMultiplier = 1.1f;  // 1-10连击
        public float tier2DamageMultiplier = 1.25f; // 11-30连击
        public float tier3DamageMultiplier = 1.5f;  // 31-50连击
        public float tier3LifeStealPercent = 0.05f; // Tier3吸血5%
        
        [Header("Berserk Mode Settings")]
        public int berserkThreshold = 50;           // 狂暴阈值
        public float berserkDuration = 3f;          // 狂暴持续时间
        public float berserkAttackRangeMultiplier = 2f; // 攻击范围翻倍
        public float berserkDamageMultiplier = 2f;  // 伤害翻倍
        public bool berserkInvincible = true;       // 无敌状态

        [Header("Hit Detection")]
        public Transform attackOrigin;
        public float attackRadius = 1f;
        public LayerMask enemyLayers;

        [Header("Visual Effects")]
        public ParticleSystem attackEffect;
        public TrailRenderer weaponTrail;
        public ParticleSystem berserkAuraEffect;    // 狂暴光环特效
        public AudioClip[] attackSounds;
        public AudioClip berserkStartSound;         // 狂暴启动音效

        [Header("Animation")]
        public string attackAnimTrigger = "Attack";
        public string comboAnimParam = "ComboCount";
        public string berserkAnimParam = "IsBerserk"; // 狂暴动画参数

        private PlayerInputHandler input;
        private PlayerMovement movement;
        private Animator animator;
        private AudioSource audioSource;
        private PlayerHealth playerHealth;
        private StaminaSystem staminaSystem;
        private BlockDodgeSystem blockDodgeSystem;
        private PlayerActionController actionController;
        private PlayerInputBuffer inputBuffer;

        private int currentCombo = 0;
        private float comboResetTimer;
        private bool canAttack = true;
        private bool isAttacking = false;
        private bool isBerserk = false;             // 是否在狂暴状态
        private float berserkTimer = 0f;            // 狂暴倒计时
        private float baseAttackRange;              // 记录基础攻击范围

        private int currentStepIndex = -1;
        private AttackStep currentStep;
        private float currentStepStartTime;
        private float currentStepEndTime;
        private bool attackHitTriggered;
        private bool attackBuffered;
        private float attackBufferTimer;
        private bool queuedNextAttack;
        private int queuedStepIndex = -1;
        private Coroutine attackRoutine;
        private AttackStep fallbackStep;

        private List<Collider> hitEnemies = new List<Collider>();

        public bool IsAttacking => isAttacking;
        public int CurrentCombo => currentCombo;
        public ComboTier CurrentTier => GetCurrentTier();
        public bool IsBerserk => isBerserk;
        public float ComboResetNormalized => GetComboResetTime() <= 0f ? 0f : Mathf.Clamp01(comboResetTimer / GetComboResetTime());
        public float ComboResetRemaining => comboResetTimer;
        
        // 事件：连击变化
        public System.Action<int> OnComboChanged;
        // 事件：狂暴模式启动/结束
        public System.Action<bool> OnBerserkStateChanged;

        private void Awake()
        {
            input = GetComponent<PlayerInputHandler>();
            movement = GetComponent<PlayerMovement>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            playerHealth = GetComponent<PlayerHealth>();
            staminaSystem = GetComponent<StaminaSystem>();
            blockDodgeSystem = GetComponent<BlockDodgeSystem>();
            actionController = GetComponent<PlayerActionController>();
            inputBuffer = GetComponent<PlayerInputBuffer>();

            EnsureAttackOrigin();
                
            // 保存基础攻击范围
            baseAttackRange = attackRange;
        }

        private void Start()
        {
            // 订阅事件到全局事件系统
            SubscribeToGameEvents();

            if (actionController != null)
            {
                actionController.OnActionInterrupted += HandleActionInterrupted;
            }
        }

        private void Reset()
        {
            EnsureAttackOrigin();
        }

        private void OnValidate()
        {
            EnsureAttackOrigin();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameEvents();

            if (actionController != null)
            {
                actionController.OnActionInterrupted -= HandleActionInterrupted;
            }
        }

        private void SubscribeToGameEvents()
        {
            OnComboChanged += (combo) => GameEvents.ComboChanged(combo);
            OnBerserkStateChanged += (active) => GameEvents.BerserkStateChanged(active);
        }

        private void UnsubscribeFromGameEvents()
        {
            // 清理事件订阅
        }

        private void Update()
        {
            HandleCooldowns();
            HandleBerserkMode();
            HandleInput();
        }
        
        // 获取当前连击等级
        private ComboTier GetCurrentTier()
        {
            if (currentCombo <= 0) return ComboTier.None;
            if (currentCombo < 11) return ComboTier.Tier1;
            if (currentCombo < 31) return ComboTier.Tier2;
            if (currentCombo < berserkThreshold) return ComboTier.Tier3;
            return ComboTier.Tier4;
        }
        
        // 处理狂暴模式
        private void HandleBerserkMode()
        {
            if (isBerserk)
            {
                berserkTimer -= Time.deltaTime;
                
                // 狂暴期间保持连击计时器刷新，防止连击中断
                comboResetTimer = GetComboResetTime();
                
                if (berserkTimer <= 0f)
                {
                    ExitBerserkMode();
                }
            }
        }
        
        // 进入狂暴模式
        private void EnterBerserkMode()
        {
            if (isBerserk) return;
            
            isBerserk = true;
            berserkTimer = berserkDuration;
            
            // 攻击范围翻倍
            attackRange = baseAttackRange * berserkAttackRangeMultiplier;
            
            // 播放特效
            if (berserkAuraEffect != null)
            {
                berserkAuraEffect.Play();
            }
            
            // 播放音效
            if (berserkStartSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(berserkStartSound);
            }
            
            // 动画参数
            if (animator != null && !string.IsNullOrEmpty(berserkAnimParam))
            {
                animator.SetBool(berserkAnimParam, true);
            }
            
            // 触发事件
            OnBerserkStateChanged?.Invoke(true);
            
            Debug.Log("🔥 深渊狂暴模式启动！持续 " + berserkDuration + " 秒");
        }
        
        // 退出狂暴模式
        private void ExitBerserkMode()
        {
            if (!isBerserk) return;
            
            isBerserk = false;
            
            // 恢复攻击范围
            attackRange = baseAttackRange;
            
            // 停止特效
            if (berserkAuraEffect != null)
            {
                berserkAuraEffect.Stop();
            }
            
            // 动画参数
            if (animator != null && !string.IsNullOrEmpty(berserkAnimParam))
            {
                animator.SetBool(berserkAnimParam, false);
            }
            
            // 触发事件
            OnBerserkStateChanged?.Invoke(false);
            
            Debug.Log("💨 深渊狂暴模式结束");
        }
        
        // 获取当前伤害倍率
        private float GetDamageMultiplier()
        {
            if (isBerserk) return berserkDamageMultiplier;
            
            return GetCurrentTier() switch
            {
                ComboTier.Tier1 => tier1DamageMultiplier,
                ComboTier.Tier2 => tier2DamageMultiplier,
                ComboTier.Tier3 => tier3DamageMultiplier,
                ComboTier.Tier4 => berserkDamageMultiplier,
                _ => 1f
            };
        }

        private void HandleCooldowns()
        {
            if (inputBuffer == null && attackBuffered)
            {
                attackBufferTimer -= Time.deltaTime;
                if (attackBufferTimer <= 0f)
                {
                    attackBuffered = false;
                }
            }

            if (currentCombo > 0)
            {
                comboResetTimer -= Time.deltaTime;
                if (comboResetTimer <= 0f)
                {
                    ResetCombo();
                }
            }

            if (isAttacking && currentStep != null && Time.time >= currentStepEndTime)
            {
                FinishAttackStep();
            }
        }

        private void HandleInput()
        {
            if (input.AttackPressed)
            {
                BufferAttack();
            }

            TryConsumeBufferedAttack();
        }

        private void BufferAttack()
        {
            if (!CanBufferAttack())
            {
                return;
            }

            if (inputBuffer != null)
            {
                inputBuffer.BufferAction(BufferedActionType.Attack, GetInputBufferTime());
                return;
            }

            attackBuffered = true;
            attackBufferTimer = GetInputBufferTime();
        }

        private bool CanBufferAttack()
        {
            if (blockDodgeSystem != null && (blockDodgeSystem.IsBlocking || blockDodgeSystem.IsDodging))
            {
                return false;
            }

            if (movement != null && movement.IsJumping)
            {
                return false;
            }

            return true;
        }

        private void TryConsumeBufferedAttack()
        {
            if (!HasBufferedAttack())
            {
                return;
            }

            if (!isAttacking)
            {
                if (!CanStartAttack())
                {
                    return;
                }

                ConsumeBufferedAttack();
                PerformAttack();
                return;
            }

            if (IsWithinComboWindow())
            {
                int nextStepIndex = GetNextStepIndex();
                if (nextStepIndex >= 0)
                {
                    QueueNextAttack(nextStepIndex);
                    ConsumeBufferedAttack();
                }
            }
        }

        private bool HasBufferedAttack()
        {
            if (inputBuffer != null)
            {
                return inputBuffer.HasAction(BufferedActionType.Attack);
            }

            return attackBuffered;
        }

        private void ConsumeBufferedAttack()
        {
            if (inputBuffer != null)
            {
                inputBuffer.TryConsume(BufferedActionType.Attack, out _);
                return;
            }

            attackBuffered = false;
        }

        private bool CanStartAttack()
        {
            if (!canAttack)
            {
                return false;
            }

            if (movement != null && movement.IsJumping)
            {
                return false;
            }

            if (actionController != null && !actionController.CanStartAction(PlayerActionState.Attack))
            {
                return false;
            }

            return true;
        }

        private void QueueNextAttack(int stepIndex)
        {
            queuedNextAttack = true;
            queuedStepIndex = stepIndex;
        }

        private void PerformAttack()
        {
            int nextStepIndex = GetNextStepIndex();
            if (nextStepIndex < 0)
            {
                return;
            }

            StartAttackStep(nextStepIndex);
        }

        private void StartAttackStep(int stepIndex)
        {
            AttackStep step = GetStepDefinition(stepIndex);
            if (step == null)
            {
                return;
            }

            if (step.requireGrounded && movement != null && !movement.IsGrounded)
            {
                return;
            }

            if (staminaSystem != null && step.staminaCost > 0f)
            {
                if (!staminaSystem.ConsumeStamina(step.staminaCost))
                {
                    return;
                }
            }

            bool allowInterrupt = step.allowDodgeCancel || step.allowBlockCancel;
            ActionInterruptMask interruptMask = GetAttackInterruptMask(step);
            if (actionController != null)
            {
                bool started = actionController.TryStartAction(
                    PlayerActionState.Attack,
                    ActionPriority.Attack,
                    step.hitDelay + step.recoveryTime,
                    lockMovementDuringAttack,
                    lockRotationDuringAttack,
                    true,
                    allowInterrupt,
                    interruptMask);

                if (!started)
                {
                    return;
                }
            }

            isAttacking = true;
            canAttack = false;
            currentStepIndex = stepIndex;
            currentStep = step;
            currentStepStartTime = Time.time;
            currentStepEndTime = currentStepStartTime + step.hitDelay + step.recoveryTime;
            attackHitTriggered = false;
            queuedNextAttack = false;
            queuedStepIndex = -1;

            int maxCombo = GetMaxComboCount();
            if (currentCombo <= 0)
            {
                currentCombo = 1;
            }
            else
            {
                currentCombo = Mathf.Min(currentCombo + 1, maxCombo);
            }

            comboResetTimer = GetComboResetTime();

            if (currentCombo >= berserkThreshold && !isBerserk)
            {
                EnterBerserkMode();
            }

            OnComboChanged?.Invoke(currentCombo);
            PlayComboSound();

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(attackAnimTrigger);
                animator.SetInteger(comboAnimParam, step.animationComboIndex);
            }

            PlayAttackEffects();

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }

            attackRoutine = StartCoroutine(AttackRoutine(step));
        }

        private void FinishAttackStep()
        {
            if (!isAttacking)
            {
                return;
            }

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            if (queuedNextAttack && queuedStepIndex >= 0)
            {
                StartAttackStep(queuedStepIndex);
                return;
            }

            isAttacking = false;
            canAttack = true;
            currentStepIndex = -1;
            currentStep = null;
            queuedNextAttack = false;
            queuedStepIndex = -1;

            if (actionController != null)
            {
                actionController.EndAction(PlayerActionState.Attack);
            }
        }

        private void CancelAttack()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            isAttacking = false;
            canAttack = true;
            currentStepIndex = -1;
            currentStep = null;
            queuedNextAttack = false;
            queuedStepIndex = -1;
            attackBuffered = false;
            if (inputBuffer != null)
            {
                inputBuffer.ClearAction(BufferedActionType.Attack);
            }
            ResetCombo();

            if (actionController != null)
            {
                actionController.EndAction(PlayerActionState.Attack);
            }
        }

        private ActionInterruptMask GetAttackInterruptMask(AttackStep step)
        {
            ActionInterruptMask mask = ActionInterruptMask.None;

            if (step != null)
            {
                if (step.allowDodgeCancel)
                {
                    mask |= ActionInterruptMask.Dodge;
                }

                if (step.allowBlockCancel)
                {
                    mask |= ActionInterruptMask.Block;
                }

                if (step.allowDodgeCancel || step.allowBlockCancel)
                {
                    mask |= ActionInterruptMask.Skill;
                }
            }

            return mask;
        }
        
        // 根据连击等级播放音效
        private void PlayComboSound()
        {
            if (attackSounds.Length == 0 || audioSource == null) return;
            
            int tier = (int)GetCurrentTier();
            int soundIndex = Mathf.Min(tier, attackSounds.Length - 1);
            AudioClip clip = attackSounds[soundIndex];
            
            // 高连击时音调更高
            float pitch = 1f + (tier * 0.1f);
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
            audioSource.pitch = 1f; // 恢复默认音调
        }

        private float GetComboResetTime()
        {
            if (comboDefinition != null)
            {
                return comboDefinition.comboResetTime;
            }

            return comboResetTime;
        }

        private float GetInputBufferTime()
        {
            if (comboDefinition != null && comboDefinition.inputBufferTime > 0f)
            {
                return comboDefinition.inputBufferTime;
            }

            return inputBufferTime;
        }

        private int GetMaxComboCount()
        {
            if (comboDefinition != null)
            {
                return comboDefinition.maxComboCount;
            }

            return maxComboCount;
        }

        private bool IsWithinComboWindow()
        {
            if (currentStep == null)
            {
                return false;
            }

            float elapsed = Time.time - currentStepStartTime;
            return elapsed >= currentStep.comboWindowStart && elapsed <= currentStep.comboWindowEnd;
        }

        private int GetNextStepIndex()
        {
            if (comboDefinition == null)
            {
                return 0;
            }

            if (currentStepIndex < 0)
            {
                return comboDefinition.HasStep(0) ? 0 : -1;
            }

            if (currentStep == null)
            {
                return -1;
            }

            int nextIndex = currentStep.nextStepIndex >= 0 ? currentStep.nextStepIndex : currentStepIndex + 1;
            return comboDefinition.HasStep(nextIndex) ? nextIndex : -1;
        }

        private AttackStep GetStepDefinition(int stepIndex)
        {
            if (comboDefinition != null && comboDefinition.HasStep(stepIndex))
            {
                return comboDefinition.GetStep(stepIndex);
            }

            if (fallbackStep == null)
            {
                fallbackStep = new AttackStep
                {
                    name = "Fallback",
                    animationComboIndex = Mathf.Clamp(stepIndex + 1, 1, 3),
                    baseDamage = attackDamage,
                    damageMultiplier = 1f,
                    knockback = attackKnockback,
                    range = attackRange,
                    angle = attackAngle,
                    radius = attackRadius,
                    hitDelay = 0.15f,
                    recoveryTime = 0.35f,
                    comboWindowStart = 0f,
                    comboWindowEnd = comboWindowTime,
                    staminaCost = 0f,
                    allowDodgeCancel = true,
                    allowBlockCancel = true,
                    requireGrounded = true,
                    nextStepIndex = -1
                };
            }

            fallbackStep.baseDamage = attackDamage;
            fallbackStep.knockback = attackKnockback;
            fallbackStep.range = attackRange;
            fallbackStep.angle = attackAngle;
            fallbackStep.radius = attackRadius;
            fallbackStep.comboWindowEnd = comboWindowTime;
            fallbackStep.animationComboIndex = Mathf.Clamp(stepIndex + 1, 1, 3);

            return fallbackStep;
        }

        private void DoAttackHit(AttackStep step)
        {
            if (attackHitTriggered)
            {
                return;
            }

            attackHitTriggered = true;
            DetectAndDamageEnemies(step);
        }

        private void HandleActionInterrupted(PlayerActionState interrupted, PlayerActionState byState)
        {
            if (interrupted == PlayerActionState.Attack)
            {
                CancelAttack();
            }
        }

        private IEnumerator AttackRoutine(AttackStep step)
        {
            if (weaponTrail != null)
            {
                weaponTrail.emitting = true;
            }

            if (!useAnimationEvents)
            {
                yield return new WaitForSeconds(step.hitDelay);
                DoAttackHit(step);
            }
            else
            {
                yield return new WaitForSeconds(step.hitDelay);
                if (!attackHitTriggered)
                {
                    DoAttackHit(step);
                }
            }

            yield return new WaitForSeconds(step.recoveryTime);

            if (weaponTrail != null)
            {
                weaponTrail.emitting = false;
            }

            FinishAttackStep();
        }

        private void DetectAndDamageEnemies(AttackStep step)
        {
            hitEnemies.Clear();

            Transform origin = GetAttackOrigin();
            if (origin == null)
            {
                return;
            }

            float range = step != null && step.range > 0f ? step.range : attackRange;
            if (isBerserk && step != null && step.range > 0f)
            {
                range *= berserkAttackRangeMultiplier;
            }
            float angle = step != null && step.angle > 0f ? step.angle : attackAngle;
            float hitRadius = step != null && step.radius > 0f ? step.radius : attackRadius;

            // Find all enemies in range
            Collider[] hitColliders = Physics.OverlapSphere(origin.position, Mathf.Max(range, hitRadius), enemyLayers);
            
            // 计算伤害倍率
            float damageMultiplier = GetDamageMultiplier();
            int baseDamage = step != null ? step.baseDamage : attackDamage;
            float stepMultiplier = step != null ? step.damageMultiplier : 1f;
            int finalDamage = Mathf.RoundToInt(baseDamage * stepMultiplier * damageMultiplier);
            float knockback = step != null ? step.knockback : attackKnockback;
            
            // 计算治疗量（Tier3以上吸血）
            int totalDamageDealt = 0;

            foreach (var hitCollider in hitColliders)
            {
                if (hitEnemies.Contains(hitCollider))
                {
                    continue;
                }

                // Check angle
                Vector3 directionToEnemy = (hitCollider.transform.position - transform.position).normalized;
                float angleToEnemy = Vector3.Angle(transform.forward, directionToEnemy);
                float distanceToEnemy = Vector3.Distance(origin.position, hitCollider.transform.position);

                if (distanceToEnemy <= range && angleToEnemy <= angle * 0.5f)
                {
                    // Apply damage
                    EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        // 检查狂暴无敌状态
                        if (berserkInvincible && isBerserk)
                        {
                            // 狂暴模式下可以穿透敌人防御
                            enemyHealth.TakeDamage(finalDamage, transform.position, knockback * 2f);
                        }
                        else
                        {
                            enemyHealth.TakeDamage(finalDamage, transform.position, knockback);
                        }
                        
                        totalDamageDealt += finalDamage;
                        hitEnemies.Add(hitCollider);
                        GameEvents.DamageDealt(finalDamage, hitCollider.transform.position, false);
                        GameEvents.ShowDamageText(finalDamage, hitCollider.transform.position, false);
                        if (hitStopDuration > 0f)
                        {
                            HitStopManager.Trigger(hitStopDuration);
                        }
                    }
                }
            }
            
            // 应用吸血效果 (Tier3: 5%, Tier4/狂暴: 10%)
            if (playerHealth != null && totalDamageDealt > 0)
            {
                float lifeStealPercent = CurrentTier switch
                {
                    ComboTier.Tier3 => tier3LifeStealPercent,
                    ComboTier.Tier4 => tier3LifeStealPercent * 2f, // 狂暴双倍吸血
                    _ => 0f
                };
                
                if (lifeStealPercent > 0)
                {
                    int healAmount = Mathf.RoundToInt(totalDamageDealt * lifeStealPercent);
                    playerHealth.Heal(healAmount);
                }
            }
        }

        private void PlayAttackEffects()
        {
            // Particle effect
            if (attackEffect != null)
            {
                attackEffect.Play();
            }

            // Sound effect
            if (attackSounds.Length > 0 && audioSource != null)
            {
                AudioClip clip = attackSounds[Random.Range(0, attackSounds.Length)];
                audioSource.PlayOneShot(clip);
            }
        }

        private void ResetCombo()
        {
            int previousCombo = currentCombo;
            currentCombo = 0;
            comboResetTimer = 0f;
            attackBuffered = false;
            queuedNextAttack = false;
            queuedStepIndex = -1;
             
            // 触发连击变化事件
            OnComboChanged?.Invoke(0);
            
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetInteger("ComboCount", 0);
            }
            
            // 如果在狂暴状态且狂暴时间未到，不重置狂暴（狂暴自然结束）
            // 如果不在狂暴状态，正常重置
            Debug.Log($"连击重置！最高连击: {previousCombo}");
        }

        // Animation events - called from animation clips
        public void OnAttackStart()
        {
            if (weaponTrail != null)
            {
                weaponTrail.emitting = true;
            }
        }

        public void OnAttackEnd()
        {
            if (weaponTrail != null)
            {
                weaponTrail.emitting = false;
            }

            FinishAttackStep();
        }

        public void AnimEvent_AttackHit()
        {
            if (currentStep != null)
            {
                DoAttackHit(currentStep);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = GetAttackOrigin();
            if (origin == null)
            {
                return;
            }

            float range = currentStep != null && currentStep.range > 0f ? currentStep.range : attackRange;
            float angle = currentStep != null && currentStep.angle > 0f ? currentStep.angle : attackAngle;

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin.position, range);

            // Draw attack angle
            Vector3 leftBoundary = Quaternion.Euler(0, -angle * 0.5f, 0) * transform.forward;
            Vector3 rightBoundary = Quaternion.Euler(0, angle * 0.5f, 0) * transform.forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, leftBoundary * range);
            Gizmos.DrawRay(transform.position, rightBoundary * range);
        }

        private Transform GetAttackOrigin()
        {
            if (attackOrigin == null)
            {
                EnsureAttackOrigin();
            }

            return attackOrigin;
        }

        private void EnsureAttackOrigin()
        {
            if (attackOrigin == null)
            {
                attackOrigin = transform;
            }
        }
    }
}
