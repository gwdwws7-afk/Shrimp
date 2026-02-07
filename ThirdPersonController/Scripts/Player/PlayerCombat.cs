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

        private int currentCombo = 0;
        private float comboResetTimer;
        private float comboWindowTimer;
        private float attackCooldownTimer;
        private bool canAttack = true;
        private bool isAttacking = false;
        private bool isBerserk = false;             // 是否在狂暴状态
        private float berserkTimer = 0f;            // 狂暴倒计时
        private float baseAttackRange;              // 记录基础攻击范围

        private List<Collider> hitEnemies = new List<Collider>();

        public bool IsAttacking => isAttacking;
        public int CurrentCombo => currentCombo;
        public ComboTier CurrentTier => GetCurrentTier();
        public bool IsBerserk => isBerserk;
        
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

            if (attackOrigin == null)
                attackOrigin = transform;
                
            // 保存基础攻击范围
            baseAttackRange = attackRange;
        }

        private void Start()
        {
            // 订阅事件到全局事件系统
            SubscribeToGameEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameEvents();
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
                comboResetTimer = comboResetTime;
                
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
            // Attack cooldown
            if (!canAttack)
            {
                attackCooldownTimer -= Time.deltaTime;
                if (attackCooldownTimer <= 0f)
                {
                    canAttack = true;
                }
            }

            // Combo reset timer
            if (currentCombo > 0)
            {
                comboResetTimer -= Time.deltaTime;
                if (comboResetTimer <= 0f)
                {
                    ResetCombo();
                }
            }

            // Combo window timer
            if (comboWindowTimer > 0f)
            {
                comboWindowTimer -= Time.deltaTime;
            }
        }

        private void HandleInput()
        {
            // 检查是否在格挡或闪避
            if (blockDodgeSystem != null && (blockDodgeSystem.IsBlocking || blockDodgeSystem.IsDodging))
                return;

            if (input.AttackPressed && canAttack && !isAttacking)
            {
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            isAttacking = true;
            canAttack = false;
            attackCooldownTimer = attackCooldown;

            // Increment combo
            if (comboWindowTimer > 0f && currentCombo < maxComboCount)
            {
                currentCombo++;
            }
            else
            {
                currentCombo = 1;
            }

            comboResetTimer = comboResetTime;
            comboWindowTimer = comboWindowTime;
            
            // 检查是否触发狂暴模式
            if (currentCombo >= berserkThreshold && !isBerserk)
            {
                EnterBerserkMode();
            }
            
            // 触发连击变化事件
            OnComboChanged?.Invoke(currentCombo);
            
            // 根据连击等级播放不同音效
            PlayComboSound();

            // Trigger animation
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Attack");
                animator.SetInteger("ComboCount", Mathf.Min(currentCombo, 3)); // 动画最多3段
            }

            // Play effects
            PlayAttackEffects();

            // Start attack sequence
            StartCoroutine(AttackSequence());
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

        private IEnumerator AttackSequence()
        {
            // Enable weapon trail
            if (weaponTrail != null)
                weaponTrail.emitting = true;

            // Wait for hit frame (adjust based on animation)
            yield return new WaitForSeconds(0.15f);

            // Detect and damage enemies
            DetectAndDamageEnemies();

            // Wait for rest of attack animation
            yield return new WaitForSeconds(0.35f);

            // Disable weapon trail
            if (weaponTrail != null)
                weaponTrail.emitting = false;

            isAttacking = false;
        }

        private void DetectAndDamageEnemies()
        {
            hitEnemies.Clear();

            // Find all enemies in range
            Collider[] hitColliders = Physics.OverlapSphere(attackOrigin.position, attackRange, enemyLayers);
            
            // 计算伤害倍率
            float damageMultiplier = GetDamageMultiplier();
            int finalDamage = Mathf.RoundToInt(attackDamage * damageMultiplier);
            
            // 计算治疗量（Tier3以上吸血）
            int totalDamageDealt = 0;

            foreach (var hitCollider in hitColliders)
            {
                // Check angle
                Vector3 directionToEnemy = (hitCollider.transform.position - transform.position).normalized;
                float angleToEnemy = Vector3.Angle(transform.forward, directionToEnemy);

                if (angleToEnemy <= attackAngle * 0.5f)
                {
                    // Apply damage
                    EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        // 检查狂暴无敌状态
                        if (berserkInvincible && isBerserk)
                        {
                            // 狂暴模式下可以穿透敌人防御
                            enemyHealth.TakeDamage(finalDamage, transform.position, attackKnockback * 2f);
                        }
                        else
                        {
                            enemyHealth.TakeDamage(finalDamage, transform.position, attackKnockback);
                        }
                        
                        totalDamageDealt += finalDamage;
                        hitEnemies.Add(hitCollider);
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
            // Can be used to enable hit detection
        }

        public void OnAttackEnd()
        {
            // Can be used to disable hit detection
        }

        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackOrigin.position, attackRange);

            // Draw attack angle
            Vector3 leftBoundary = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * transform.forward;
            Vector3 rightBoundary = Quaternion.Euler(0, attackAngle * 0.5f, 0) * transform.forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, leftBoundary * attackRange);
            Gizmos.DrawRay(transform.position, rightBoundary * attackRange);
        }
    }
}
