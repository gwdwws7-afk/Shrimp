using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ThirdPersonController
{
// 连击配置项，用于驱动模块行为并保持可调性。
    public enum ComboTier
    {
        None, // 默认占位枚举值，表示当前未进入有效状态。
        Tier1, // 连击档位枚举值，用于驱动分段增益与表现反馈。
        Tier2, // 连击档位枚举值，用于驱动分段增益与表现反馈。
        Tier3, // 连击档位枚举值，用于驱动分段增益与表现反馈。
        Tier4 // 连击档位枚举值，用于驱动分段增益与表现反馈。
    }

    public class PlayerCombat : MonoBehaviour
    {
        private enum AttackInputType
        {
            Light,
            Heavy
        }
        [Header("Attack Settings")]
        public float attackRange = 2f;
        public float attackAngle = 120f;
        public float attackCooldown = 0.5f;
        public int attackDamage = 25;
        public float attackKnockback = 5f;
        public float attackSpeed = 1f;
        public float criticalRate = 0.05f;
        public float criticalDamage = 1.5f;

        [Header("Damage Curve")]
        public DamageCurveProfile damageCurveProfile;

        [Header("Combo Definition")]
        public AttackComboDefinition comboDefinition;
        public bool useAnimationEvents = true;
        public float inputBufferTime = 0.3f;
        public float hitStopDuration = 0.05f;
        public bool lockMovementDuringAttack = true;
        public bool lockRotationDuringAttack = false;

        [Header("Impact Tuning")]
        public float heavyDamageMultiplier = 1.15f;
        public float heavyKnockbackMultiplier = 1.45f;
        public float heavyRangeMultiplier = 1.1f;
        public float heavyRadiusMultiplier = 1.1f;
        public float heavyHitStopMultiplier = 1.2f;
        public float lightImpactScale = 0.3f;
        public float heavyImpactScale = 0.35f;
        public float berserkImpactMultiplier = 2f;

        [Header("Combo Settings")]
        public int maxComboCount = 999; // 阈值/边界参数，用于触发分段策略并防止越界。
        public float comboResetTime = 1.1f;
        public float comboWindowTime = 0.8f;

        [Header("Combo Unlocks")]
        [Tooltip("0 means no limit, otherwise limits combo step count.")]
        public int maxComboStepsUnlocked = 0;

        [Header("Heavy Opener")]
        public bool allowHeavyOpener = true;
        [Tooltip("Combo step index used when heavy attack starts from idle.")]
        public int heavyOpenerStepIndex = 0;
        
        [Header("Combo Tier Settings")]
        public float tier1DamageMultiplier = 1.1f; // 数值倍率参数，用于统一调控效果强度。
        public float tier2DamageMultiplier = 1.25f; // 数值倍率参数，用于统一调控效果强度。
        public float tier3DamageMultiplier = 1.5f; // 数值倍率参数，用于统一调控效果强度。
        public float tier3LifeStealPercent = 0.05f; // 运行时配置项，用于驱动模块行为并保持可调性。
        
        [Header("Berserk Mode Settings")]
        public int berserkThreshold = 50; // 阈值/边界参数，用于触发分段策略并防止越界。
        public float berserkDuration = 3f; // 时序参数，用于控制触发节奏并防止状态抖动。
        public float berserkAttackRangeMultiplier = 2f; // 判定范围参数，用于约束技能覆盖并保持平衡。
        public float berserkDamageMultiplier = 2f; // 数值倍率参数，用于统一调控效果强度。
        public bool berserkInvincible = true; // 运行时配置项，用于驱动模块行为并保持可调性。

        [Header("Hit Detection")]
        public Transform attackOrigin;
        public float attackRadius = 1f;
        public LayerMask enemyLayers;

        [Header("Visual Effects")]
        public ParticleSystem attackEffect;
        public TrailRenderer weaponTrail;
        public ParticleSystem berserkAuraEffect; // 运行时配置项，用于驱动模块行为并保持可调性。
        public AudioClip[] attackSounds;
        public AudioClip berserkStartSound; // 运行时配置项，用于驱动模块行为并保持可调性。

        [Header("Animation")]
        public string attackAnimTrigger = "Attack";
        public string comboAnimParam = "ComboCount";
        public string berserkAnimParam = "IsBerserk"; // 运行时配置项，用于驱动模块行为并保持可调性。

        private static readonly int AttackStateHash = Animator.StringToHash("Attack");
        private static readonly int Attack2StateHash = Animator.StringToHash("Attack_2");
        private static readonly int Attack3StateHash = Animator.StringToHash("Attack_3");
        private static readonly int AttackBStateHash = Animator.StringToHash("Attack_B");

        private PlayerInputHandler input;
        private PlayerMovement movement;
        private Animator animator;
        private AudioSource audioSource;
        private PlayerHealth playerHealth;
        private StaminaSystem staminaSystem;
        private BlockDodgeSystem blockDodgeSystem;
        private PlayerActionController actionController;
        private PlayerInputBuffer inputBuffer;
        private PlayerMusouSystem musouSystem;
        private PlayerStatsController statsController;

        private int currentCombo = 0;
        private float comboResetTimer;
        private bool canAttack = true;
        private bool isAttacking = false;
        private bool isBerserk = false; // 运行时状态标记，用于快速分支判定与流程保护。
        private float berserkTimer = 0f; // 时序参数，用于控制触发节奏并防止状态抖动。
        private float baseAttackRange; // 判定范围参数，用于约束技能覆盖并保持平衡。

        private int currentStepIndex = -1;
        private AttackStep currentStep;
        private float currentStepStartTime;
        private float currentStepEndTime;
        private bool attackHitTriggered;
        private bool attackBuffered;
        private float attackBufferTimer;
        private AttackInputType bufferedAttackType = AttackInputType.Light;
        private AttackInputType currentStepInputType = AttackInputType.Light;
        private AttackInputType queuedStepInputType = AttackInputType.Light;
        private bool queuedNextAttack;
        private int queuedStepIndex = -1;
        private Coroutine attackRoutine;
        private AttackStep fallbackStep;

        private List<Collider> hitEnemies = new List<Collider>();
        private readonly HashSet<EnemyHealth> processedHitTargets = new HashSet<EnemyHealth>();
        private readonly Dictionary<EnemyHealth, float> lastHitTimes = new Dictionary<EnemyHealth, float>();
        private float primaryHitTime = -1f;
        private float skillDamageBuffMultiplier = 1f;

        public bool IsAttacking => isAttacking;
        public int CurrentCombo => currentCombo;
        public ComboTier CurrentTier => GetCurrentTier();
        public bool IsBerserk => isBerserk;
        public float ComboResetNormalized => GetComboResetTime() <= 0f ? 0f : Mathf.Clamp01(comboResetTimer / GetComboResetTime());
        public float ComboResetRemaining => comboResetTimer;
        
// 连击配置项，用于驱动模块行为并保持可调性。
        public System.Action<int> OnComboChanged;
// 狂暴状态配置项，用于驱动模块行为并保持可调性。
        public System.Action<bool> OnBerserkStateChanged;

        /// <summary>
        /// External skill buffs can route their damage multiplier into the main combo damage path.
        /// </summary>
        public void SetSkillDamageBuffMultiplier(float multiplier)
        {
            skillDamageBuffMultiplier = Mathf.Max(0.1f, multiplier);
        }

        /// <summary>
        /// Clear temporary external damage buff and restore neutral scaling.
        /// </summary>
        public void ClearSkillDamageBuffMultiplier()
        {
            skillDamageBuffMultiplier = 1f;
        }

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
            musouSystem = GetComponent<PlayerMusouSystem>();
            statsController = GetComponent<PlayerStatsController>();

            if (damageCurveProfile == null)
            {
                damageCurveProfile = DamageCurveProfile.GetDefaultProfile();
            }

            EnsureAttackOrigin();
                
// 围绕 baseAttackRange 执行该步骤，用于保持上下文语义一致。
            baseAttackRange = attackRange;
        }

        private void Start()
        {
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
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
            OnComboChanged += (combo) => GameEvents.ComboCountChanged(combo);
            OnBerserkStateChanged += (active) => GameEvents.BerserkStateChanged(active);
        }

        private void UnsubscribeFromGameEvents()
        {
// 围绕 当前步骤 执行该步骤，用于保持上下文语义一致。
        }

        private void Update()
        {
            HandleCooldowns();
            HandleBerserkMode();
            HandleInput();
        }
        
// 获取 系统 相关状态，减少外部重复计算。
        private ComboTier GetCurrentTier()
        {
            if (currentCombo <= 0) return ComboTier.None;
            if (currentCombo < 11) return ComboTier.Tier1;
            if (currentCombo < 31) return ComboTier.Tier2;
            if (currentCombo < berserkThreshold) return ComboTier.Tier3;
            return ComboTier.Tier4;
        }
        
// 处理 狂暴 事件并执行对应业务分支。
        private void HandleBerserkMode()
        {
            if (isBerserk)
            {
                berserkTimer -= Time.deltaTime;
                
// 围绕 连击 执行该步骤，用于保证流程状态与后续分支一致。
                comboResetTimer = GetComboResetTime();
                
                if (berserkTimer <= 0f)
                {
                    ExitBerserkMode();
                }
            }
        }
        
// 执行 狂暴 相关流程，保证模块行为一致性。
        private void EnterBerserkMode()
        {
            if (isBerserk) return;
            
            isBerserk = true;
            berserkTimer = berserkDuration;
            
// 围绕 attackRange 执行该步骤，用于保持上下文语义一致。
            attackRange = baseAttackRange * berserkAttackRangeMultiplier;
            
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (berserkAuraEffect != null)
            {
                berserkAuraEffect.Play();
            }
            
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (berserkStartSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(berserkStartSound);
            }
            
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (animator != null && !string.IsNullOrEmpty(berserkAnimParam))
            {
                animator.SetBool(berserkAnimParam, true);
            }
            
// 围绕 狂暴状态 执行该步骤，用于保证流程状态与后续分支一致。
            OnBerserkStateChanged?.Invoke(true);
            
            Debug.Log($"Berserk mode started for {berserkDuration:F1}s.");
        }
        
// 执行 狂暴 相关流程，保证模块行为一致性。
        private void ExitBerserkMode()
        {
            if (!isBerserk) return;
            
            isBerserk = false;
            
// 围绕 attackRange 执行该步骤，用于保持上下文语义一致。
            attackRange = baseAttackRange;
            
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (berserkAuraEffect != null)
            {
                berserkAuraEffect.Stop();
            }
            
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (animator != null && !string.IsNullOrEmpty(berserkAnimParam))
            {
                animator.SetBool(berserkAnimParam, false);
            }
            
// 围绕 狂暴状态 执行该步骤，用于保证流程状态与后续分支一致。
            OnBerserkStateChanged?.Invoke(false);
            
            Debug.Log("?? 深渊狂暴模式结束");
        }
        
// 获取 伤害 相关状态，减少外部重复计算。
        private float GetDamageMultiplier()
        {
            float multiplier = GetCurrentTier() switch
            {
                ComboTier.Tier1 => tier1DamageMultiplier,
                ComboTier.Tier2 => tier2DamageMultiplier,
                ComboTier.Tier3 => tier3DamageMultiplier,
                ComboTier.Tier4 => berserkDamageMultiplier,
                _ => 1f
            };

            if (isBerserk)
            {
                multiplier = berserkDamageMultiplier;
            }

            if (musouSystem != null)
            {
                multiplier *= musouSystem.DamageMultiplier;
            }

            multiplier *= skillDamageBuffMultiplier;

            return multiplier;
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
                BufferAttack(AttackInputType.Light);
            }

            if (input.HeavyAttackPressed)
            {
                BufferAttack(AttackInputType.Heavy);
            }

            TryConsumeBufferedAttack();
        }

        private void BufferAttack(AttackInputType inputType)
        {
            if (!CanBufferAttack())
            {
                return;
            }

            if (inputBuffer != null)
            {
                inputBuffer.ClearAction(BufferedActionType.AttackLight);
                inputBuffer.ClearAction(BufferedActionType.AttackHeavy);
                inputBuffer.BufferAction(GetBufferedActionType(inputType), GetInputBufferTime());
                return;
            }

            attackBuffered = true;
            attackBufferTimer = GetInputBufferTime();
            bufferedAttackType = inputType;
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
            if (!HasBufferedAttack(out AttackInputType inputType))
            {
                return;
            }

            if (!isAttacking)
            {
                if (!CanStartAttack())
                {
                    return;
                }

                ConsumeBufferedAttack(inputType);
                PerformAttack(inputType);
                return;
            }

            if (IsWithinComboWindow())
            {
                int nextStepIndex = GetNextStepIndex(inputType);
                if (nextStepIndex >= 0)
                {
                    QueueNextAttack(nextStepIndex, inputType);
                    ConsumeBufferedAttack(inputType);
                }
            }
        }

        private bool HasBufferedAttack(out AttackInputType inputType)
        {
            if (inputBuffer != null)
            {
                if (inputBuffer.TryGet(BufferedActionType.AttackHeavy, out _))
                {
                    inputType = AttackInputType.Heavy;
                    return true;
                }

                if (inputBuffer.TryGet(BufferedActionType.AttackLight, out _))
                {
                    inputType = AttackInputType.Light;
                    return true;
                }

                inputType = AttackInputType.Light;
                return false;
            }

            if (attackBuffered)
            {
                inputType = bufferedAttackType;
                return true;
            }

            inputType = AttackInputType.Light;
            return false;
        }

        private void ConsumeBufferedAttack(AttackInputType inputType)
        {
            if (inputBuffer != null)
            {
                inputBuffer.TryConsume(GetBufferedActionType(inputType), out _);
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

        private void QueueNextAttack(int stepIndex, AttackInputType inputType)
        {
            queuedNextAttack = true;
            queuedStepIndex = stepIndex;
            queuedStepInputType = inputType;
        }

        private void PerformAttack(AttackInputType inputType)
        {
            int nextStepIndex = GetNextStepIndex(inputType);
            if (nextStepIndex < 0)
            {
                return;
            }

            StartAttackStep(nextStepIndex, inputType);
        }

        private void StartAttackStep(int stepIndex, AttackInputType inputType)
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
            currentStepInputType = inputType;
            currentStepStartTime = Time.time;
            float additionalHitDelay = GetMaxAdditionalHitDelay(step);
            currentStepEndTime = currentStepStartTime + step.hitDelay + additionalHitDelay + step.recoveryTime;
            attackHitTriggered = false;
            primaryHitTime = -1f;
            lastHitTimes.Clear();
            queuedNextAttack = false;
            queuedStepIndex = -1;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                int comboIndex = step.animationComboIndex;
                bool played = TryPlayAttackAnimation(comboIndex);
                animator.SetInteger(comboAnimParam, comboIndex);
                if (!played)
                {
                    animator.SetTrigger(attackAnimTrigger);
                }
            }

            PlayAttackEffects();

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }

            attackRoutine = StartCoroutine(AttackRoutine(step));
        }

        private bool TryPlayAttackAnimation(int comboIndex)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            int stateHash = comboIndex switch
            {
                2 => Attack2StateHash,
                3 => Attack3StateHash,
                4 => AttackBStateHash,
                _ => AttackStateHash
            };

            if (!animator.HasState(0, stateHash))
            {
                return false;
            }

            animator.Play(stateHash, 0, 0f);
            return true;
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
                StartAttackStep(queuedStepIndex, queuedStepInputType);
                return;
            }

            ClearBufferedAttackInput();
            isAttacking = false;
            canAttack = true;
            currentStepIndex = -1;
            currentStep = null;
            queuedNextAttack = false;
            queuedStepIndex = -1;
            queuedStepInputType = AttackInputType.Light;

            if (actionController != null)
            {
                actionController.EndAction(PlayerActionState.Attack);
            }
        }

        private void ClearBufferedAttackInput()
        {
            if (inputBuffer != null)
            {
                inputBuffer.ClearAction(BufferedActionType.AttackLight);
                inputBuffer.ClearAction(BufferedActionType.AttackHeavy);
            }

            attackBuffered = false;
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
            lastHitTimes.Clear();
            primaryHitTime = -1f;
            if (inputBuffer != null)
            {
                inputBuffer.ClearAction(BufferedActionType.AttackLight);
                inputBuffer.ClearAction(BufferedActionType.AttackHeavy);
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
        
// 执行 连击 相关流程，保证模块行为一致性。
        private void PlayComboSound()
        {
            if (attackSounds.Length == 0 || audioSource == null) return;
            
            int tier = (int)GetCurrentTier();
            int soundIndex = Mathf.Min(tier, attackSounds.Length - 1);
            AudioClip clip = attackSounds[soundIndex];
            
// 围绕 float 执行该步骤，用于保持上下文语义一致。
            float pitch = 1f + (tier * 0.1f);
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
            audioSource.pitch = 1f; // 围绕 audioSource 执行该步骤，用于保持上下文语义一致。
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

        public void SetMaxComboStepsUnlocked(int steps)
        {
            maxComboStepsUnlocked = steps;
            if (maxComboStepsUnlocked < 0)
            {
                maxComboStepsUnlocked = 0;
            }
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

        private int GetNextStepIndex(AttackInputType inputType)
        {
            if (comboDefinition == null)
            {
                return GetMaxComboStepsUnlocked() > 0 ? 0 : -1;
            }

            int allowedSteps = GetMaxComboStepsUnlocked();
            if (allowedSteps <= 0)
            {
                return -1;
            }

            if (currentStepIndex < 0)
            {
                if (inputType == AttackInputType.Heavy)
                {
                    if (!allowHeavyOpener)
                    {
                        return -1;
                    }

                    int heavyIndex = ResolveHeavyOpenerIndex();
                    if (heavyIndex < 0)
                    {
                        return -1;
                    }

                    int totalStepsOnOpener = comboDefinition.steps.Count;
                    bool allowHeavyFinisherOnOpener = heavyIndex == totalStepsOnOpener - 1
                        && heavyIndex == allowedSteps;
                    if (heavyIndex >= allowedSteps && !allowHeavyFinisherOnOpener)
                    {
                        return -1;
                    }

                    return heavyIndex;
                }
                return comboDefinition.HasStep(0) ? 0 : -1;
            }

            if (currentStep == null)
            {
                return -1;
            }

            int nextIndex = ResolveNextStepIndex(currentStep, inputType);
            int totalSteps = comboDefinition.steps.Count;
            bool allowHeavyFinisher = inputType == AttackInputType.Heavy
                && nextIndex == totalSteps - 1
                && nextIndex == allowedSteps;
            if (nextIndex >= allowedSteps && !allowHeavyFinisher)
            {
                return -1;
            }

            return comboDefinition.HasStep(nextIndex) ? nextIndex : -1;
        }

        private AttackStep GetStepDefinition(int stepIndex)
        {
            int allowedSteps = GetMaxComboStepsUnlocked();
            int totalSteps = comboDefinition != null ? comboDefinition.steps.Count : 1;
            bool allowFinisherStep = comboDefinition != null
                && stepIndex == totalSteps - 1
                && stepIndex == allowedSteps;
            if (allowedSteps <= 0 || (stepIndex >= allowedSteps && !allowFinisherStep))
            {
                return null;
            }

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
            fallbackStep.forwardOffset = 0f;
            fallbackStep.heightOffset = 0f;
            fallbackStep.perTargetHitCooldown = 0f;
            fallbackStep.nextStepOnLight = -1;
            fallbackStep.nextStepOnHeavy = -1;
            if (fallbackStep.additionalHitDelays != null)
            {
                fallbackStep.additionalHitDelays.Clear();
            }

            return fallbackStep;
        }

        private int ResolveNextStepIndex(AttackStep step, AttackInputType inputType)
        {
            if (step == null)
            {
                return -1;
            }

            if (inputType == AttackInputType.Heavy && step.nextStepOnHeavy >= 0)
            {
                return step.nextStepOnHeavy;
            }

            if (inputType == AttackInputType.Light && step.nextStepOnLight >= 0)
            {
                return step.nextStepOnLight;
            }

            if (inputType == AttackInputType.Light && step.nextStepIndex >= 0)
            {
                return step.nextStepIndex;
            }

            return -1;
        }

        private int ResolveHeavyOpenerIndex()
        {
            if (comboDefinition == null || comboDefinition.steps == null || comboDefinition.steps.Count == 0)
            {
                return -1;
            }

            int clampedIndex = Mathf.Clamp(heavyOpenerStepIndex, 0, comboDefinition.steps.Count - 1);
            if (comboDefinition.HasStep(clampedIndex))
            {
                return clampedIndex;
            }

            return comboDefinition.HasStep(0) ? 0 : -1;
        }

        private BufferedActionType GetBufferedActionType(AttackInputType inputType)
        {
            return inputType == AttackInputType.Heavy ? BufferedActionType.AttackHeavy : BufferedActionType.AttackLight;
        }

        private int GetMaxComboStepsUnlocked()
        {
            int totalSteps = comboDefinition != null ? comboDefinition.steps.Count : 1;
            if (maxComboStepsUnlocked <= 0)
            {
                return totalSteps;
            }

            return Mathf.Clamp(maxComboStepsUnlocked, 0, totalSteps);
        }

        private Vector3 GetAttackCenter(AttackStep step, out Vector3 forward)
        {
            Transform origin = GetAttackOrigin();
            Vector3 center = origin != null ? origin.position : transform.position;

            forward = origin != null ? origin.forward : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = transform.forward;
                forward.y = 0f;
            }
            forward = forward.normalized;

            float forwardOffset = step != null ? step.forwardOffset : 0f;
            float heightOffset = step != null ? step.heightOffset : 0f;

            center += forward * forwardOffset;
            center += Vector3.up * heightOffset;

            return center;
        }

        private float GetMaxAdditionalHitDelay(AttackStep step)
        {
            if (step == null || step.additionalHitDelays == null || step.additionalHitDelays.Count == 0)
            {
                return 0f;
            }

            float maxDelay = 0f;
            for (int i = 0; i < step.additionalHitDelays.Count; i++)
            {
                float delay = step.additionalHitDelays[i];
                if (delay > maxDelay)
                {
                    maxDelay = delay;
                }
            }

            return maxDelay;
        }

        private List<float> GetSortedAdditionalHitDelays(AttackStep step)
        {
            if (step == null || step.additionalHitDelays == null || step.additionalHitDelays.Count == 0)
            {
                return null;
            }

            List<float> delays = new List<float>(step.additionalHitDelays);
            delays.Sort();
            return delays;
        }

        private float GetRangeMultiplier()
        {
            float multiplier = 1f;
            if (isBerserk)
            {
                multiplier *= berserkAttackRangeMultiplier;
            }

            if (musouSystem != null)
            {
                multiplier *= musouSystem.RangeMultiplier;
            }

            return multiplier;
        }

        private float GetKnockbackMultiplier()
        {
            if (musouSystem == null)
            {
                return 1f;
            }

            return musouSystem.KnockbackMultiplier;
        }

        private void DoAttackHit(AttackStep step)
        {
            if (attackHitTriggered)
            {
                return;
            }

            attackHitTriggered = true;
            primaryHitTime = Time.time;
            DetectAndDamageEnemies(step);
        }

        private void TriggerAdditionalHit(AttackStep step)
        {
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

            yield return new WaitForSeconds(step.hitDelay);
            if (!attackHitTriggered)
            {
                DoAttackHit(step);
            }

            List<float> additionalHitDelays = GetSortedAdditionalHitDelays(step);
            if (additionalHitDelays != null)
            {
                float baseHitTime = primaryHitTime > 0f ? primaryHitTime : Time.time;
                for (int i = 0; i < additionalHitDelays.Count; i++)
                {
                    float delay = additionalHitDelays[i];
                    if (delay < 0f)
                    {
                        delay = 0f;
                    }

                    float targetTime = baseHitTime + delay;
                    float waitTime = targetTime - Time.time;
                    if (waitTime > 0f)
                    {
                        yield return new WaitForSeconds(waitTime);
                    }

                    TriggerAdditionalHit(step);
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

            Vector3 attackForward;
            Vector3 attackCenter = GetAttackCenter(step, out attackForward);

            float range = step != null && step.range > 0f ? step.range : attackRange;
            if (statsController != null)
            {
                range = statsController.ApplyAttackRange(range);
            }
            range *= GetRangeMultiplier();
            float angle = step != null && step.angle > 0f ? step.angle : attackAngle;
            if (statsController != null)
            {
                angle = statsController.ApplyAttackAngle(angle);
            }
            float hitRadius = step != null && step.radius > 0f ? step.radius : attackRadius;

            bool isHeavyImpact = currentStepInputType == AttackInputType.Heavy;
            if (isHeavyImpact)
            {
                range *= heavyRangeMultiplier;
                hitRadius *= heavyRadiusMultiplier;
            }

            // Find all enemies in range
            HitQuery.OverlapCone(attackCenter, attackForward, range, angle, hitRadius, enemyLayers, hitEnemies, 0);
            
// 围绕 伤害 执行该步骤，用于保证流程状态与后续分支一致。
            float damageMultiplier = GetDamageMultiplier();
            int baseDamage = step != null ? step.baseDamage : attackDamage;
            if (statsController != null)
            {
                baseDamage = statsController.ApplyAttackDamage(baseDamage);
            }

            float damageCurveMultiplier = 1f;
            if (damageCurveProfile != null)
            {
                damageCurveMultiplier = damageCurveProfile.GetDamageMultiplier(baseDamage);
            }
            float stepMultiplier = step != null ? step.damageMultiplier : 1f;
            int finalDamage = Mathf.RoundToInt(baseDamage * stepMultiplier * damageMultiplier * damageCurveMultiplier);
            if (isHeavyImpact)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * heavyDamageMultiplier);
            }
            float knockback = step != null ? step.knockback : attackKnockback;
            if (statsController != null)
            {
                knockback = statsController.ApplyAttackKnockback(knockback);
            }
            if (damageCurveProfile != null)
            {
                knockback *= damageCurveProfile.GetKnockbackMultiplier(baseDamage);
            }
            knockback *= GetKnockbackMultiplier();
            if (isHeavyImpact)
            {
                knockback *= heavyKnockbackMultiplier;
            }

            float appliedHitStop = hitStopDuration;
            if (isHeavyImpact)
            {
                appliedHitStop *= heavyHitStopMultiplier;
            }

            float perTargetCooldown = step != null ? Mathf.Max(0f, step.perTargetHitCooldown) : 0f;
            float now = Time.time;
            
// 围绕 伤害 执行该步骤，用于保持上下文语义一致。
            int totalDamageDealt = 0;
            processedHitTargets.Clear();

            for (int i = 0; i < hitEnemies.Count; i++)
            {
                Collider hitCollider = hitEnemies[i];
                if (hitCollider == null)
                {
                    continue;
                }

                EnemyHealth enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
                if (enemyHealth == null || enemyHealth.IsDead)
                {
                    continue;
                }

                if (!processedHitTargets.Add(enemyHealth))
                {
                    continue;
                }

                if (lastHitTimes.TryGetValue(enemyHealth, out float lastHitTime))
                {
                    if (perTargetCooldown <= 0f)
                    {
                        continue;
                    }

                    if (now - lastHitTime < perTargetCooldown)
                    {
                        continue;
                    }
                }

                // Apply damage
                if (enemyHealth != null)
                {
                    float impactScale = isHeavyImpact ? heavyImpactScale : lightImpactScale;
                    float appliedKnockback = knockback * impactScale;
                    if (berserkInvincible && isBerserk)
                    {
                        appliedKnockback *= berserkImpactMultiplier;
                    }
                    DamageElementType elementType = DamageElementType.Physical;
                    if (statsController != null)
                    {
                        elementType = statsController.GetAttackElementType();
                    }
                    DamageContext context = new DamageContext
                    {
                        source = transform,
                        sourceType = DamageSourceType.PlayerAttack,
                        damage = finalDamage,
                        elementType = elementType,
                        category = isHeavyImpact ? DamageCategory.Heavy : DamageCategory.Light,
                        knockback = appliedKnockback,
                        breakValue = appliedKnockback,
                        damageOrigin = transform.position,
                        hitPoint = hitCollider.bounds.center,
                        hasHitPoint = true,
                        isCritical = false,
                        showDamageText = true,
                        hitStopDuration = appliedHitStop,
                        isHeavyAttack = isHeavyImpact
                    };

                    if (DamageService.ApplyDamage(context, hitCollider))
                    {
                        totalDamageDealt += finalDamage;
                        lastHitTimes[enemyHealth] = now;
                    }
                }
            }
            
// 围绕 生命伤害 执行该步骤，用于保证流程状态与后续分支一致。
            if (playerHealth != null && totalDamageDealt > 0)
            {
                float lifeStealPercent = CurrentTier switch
                {
                    ComboTier.Tier3 => tier3LifeStealPercent,
                    ComboTier.Tier4 => tier3LifeStealPercent * 2f, // 围绕 连击 执行该步骤，用于保持上下文语义一致。
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
             
// 围绕 连击 执行该步骤，用于保证流程状态与后续分支一致。
            OnComboChanged?.Invoke(0);
            
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetInteger("ComboCount", 0);
            }
            
            // 记录连击中断前的数值，便于回放战斗时序问题。
            Debug.Log($"[Combat] Combo reset from {previousCombo} to 0.");
        }

        public void RegisterHit(int damage)
        {
            if (damage <= 0)
            {
                return;
            }

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
            Vector3 attackForward;
            Vector3 attackCenter = GetAttackCenter(currentStep, out attackForward);

            float range = currentStep != null && currentStep.range > 0f ? currentStep.range : attackRange;
            if (statsController != null)
            {
                range = statsController.ApplyAttackRange(range);
            }
            range *= GetRangeMultiplier();
            float angle = currentStep != null && currentStep.angle > 0f ? currentStep.angle : attackAngle;
            if (statsController != null)
            {
                angle = statsController.ApplyAttackAngle(angle);
            }

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackCenter, range);

            // Draw attack angle
            Vector3 leftBoundary = Quaternion.Euler(0, -angle * 0.5f, 0) * attackForward;
            Vector3 rightBoundary = Quaternion.Euler(0, angle * 0.5f, 0) * attackForward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(attackCenter, leftBoundary * range);
            Gizmos.DrawRay(attackCenter, rightBoundary * range);
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


