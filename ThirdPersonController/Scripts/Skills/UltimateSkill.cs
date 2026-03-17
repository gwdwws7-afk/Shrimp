using UnityEngine;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// UltimateSkill 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// 对大范围敌人造成高额伤害，并可在击杀后刷新小技能冷却。
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_Ultimate", menuName = "Skills/Ultimate")]
    public class UltimateSkill : SkillBase
    {
        [Header("设置")]
        public float effectRadius = 20f;
        public float stunDuration = 3f;
        public float knockbackForce = 15f;
        public bool refreshCooldownsOnKill = true;
        
        [Header("特效")]
        public float slowMotionDuration = 1f;
        public float slowMotionScale = 0.3f;

        private readonly List<Collider> hitTargets = new List<Collider>();
        [System.NonSerialized] private Coroutine slowMotionRoutine;
        [System.NonSerialized] private MonoBehaviour activeRunner;
        [System.NonSerialized] private bool slowMotionActive;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (category == SkillCategory.None)
            {
                category = SkillCategory.Burst;
            }

            if (useAnimationEvents)
            {
                impactDelay = 0.26f;
                recoveryDelay = 0.38f;
                impactShakeDuration = 0.18f;
                impactShakeStrength = 0.3f;
            }
        }
        
        public override void Execute(Transform caster, Vector3 targetPosition)
        {
// 触发终极技能动画，用于强化动作反馈并统一表现节奏。
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Ultimate");
            }

            StartSkillTimeline(caster, caster.position, caster.rotation, () =>
            {
                // 进入慢动作表现，强调终结技反馈
                if (slowMotionDuration > 0f)
                {
                    activeRunner = caster.GetComponent<MonoBehaviour>();
                    if (activeRunner != null)
                    {
                        if (slowMotionRoutine != null)
                        {
                            activeRunner.StopCoroutine(slowMotionRoutine);
                        }
                        slowMotionRoutine = activeRunner.StartCoroutine(SlowMotionRoutine());
                    }
                    else
                    {
                        ApplySlowMotion();
                        RestoreTimeScale();
                    }
                }

                // 执行终极伤害与控制结算
                ExecuteUltimate(caster);
            });
        }

        public override void OnInterrupted(Transform caster)
        {
            RestoreTimeScale();
            if (activeRunner != null && slowMotionRoutine != null)
            {
                activeRunner.StopCoroutine(slowMotionRoutine);
            }

            slowMotionRoutine = null;
            activeRunner = null;
        }

        private System.Collections.IEnumerator SlowMotionRoutine()
        {
            ApplySlowMotion();
            yield return new WaitForSecondsRealtime(slowMotionDuration);
            RestoreTimeScale();
            slowMotionRoutine = null;
            activeRunner = null;
        }

        private void ApplySlowMotion()
        {
            slowMotionActive = true;
            Time.timeScale = slowMotionScale;
        }

        private void RestoreTimeScale()
        {
            if (!slowMotionActive)
            {
                return;
            }

            slowMotionActive = false;
            Time.timeScale = 1f;
        }
        
        private void ExecuteUltimate(Transform caster)
        {
            // 计算当前释放的修正参数
            float adjustedRadius = GetModifiedRange(caster, effectRadius);
            int adjustedDamage = GetModifiedDamage(caster, damage);
            float adjustedKnockback = GetModifiedKnockback(caster, knockbackForce);

            HitQuery.OverlapSphere(caster.position, adjustedRadius, LayerMask.GetMask("Enemy"), hitTargets);
            
            List<EnemyHealth> killedEnemies = new List<EnemyHealth>();
            int hitCount = 0;

            for (int i = 0; i < hitTargets.Count; i++)
            {
                Collider hitCollider = hitTargets[i];
                if (hitCollider == null)
                {
                    continue;
                }

                EnemyHealth enemyHealth = ResolveEnemyHealth(hitCollider);
                EnemyAI enemyAI = ResolveEnemyAI(hitCollider);

                if (enemyHealth != null)
                {
                    int previousHealth = enemyHealth.CurrentHealth;

                    DamageContext context = new DamageContext
                    {
                        source = caster,
                        sourceType = DamageSourceType.PlayerSkill,
                        damage = adjustedDamage,
                        skillCategory = category,
                        elementType = ResolveSkillElement(caster),
                        category = damageCategory,
                        knockback = adjustedKnockback,
                        breakValue = GetModifiedBreakValue(caster, adjustedKnockback),
                        damageOrigin = caster.position,
                        hitPoint = hitCollider.bounds.center,
                        hasHitPoint = true,
                        isCritical = false,
                        showDamageText = true,
                        hitStopDuration = 0f,
                        isHeavyAttack = false
                    };

                    if (DamageService.ApplyDamage(context, hitCollider))
                    {
                        hitCount++;

                        if (enemyHealth.IsDead && previousHealth > 0)
                        {
                            killedEnemies.Add(enemyHealth);
                        }

                        // 命中后追加眩晕控制
                        if (enemyAI != null)
                        {
                            float scaledStunDuration = stunDuration * GetEnemyControlMultiplier(enemyHealth);
                            if (scaledStunDuration > 0.01f)
                            {
                                enemyAI.ApplyStun(scaledStunDuration);
                            }
                        }
                    }
                }
            }
            
            // 命中后播放打击音效
            if (hitCount > 0 && hitSound != null)
            {
                PlaySound(hitSound, caster.position);
            }
            
            // 满足条件时刷新小技能冷却
            if (refreshCooldownsOnKill && killedEnemies.Count > 0)
            {
                RefreshSkillCooldowns(caster);
                Debug.Log($"[Skill] Ultimate killed {killedEnemies.Count} enemies, refreshed minor cooldowns.");
            }
            
            Debug.Log($"[Skill] Ultimate hit {hitCount} enemies.");
        }
        
        private void RefreshSkillCooldowns(Transform caster)
        {
            SkillManager skillManager = caster.GetComponent<SkillManager>();
            if (skillManager != null)
            {
                // 仅刷新 Q/W/E/R 四个小技能
                for (int i = 0; i < 4 && i < skillManager.skills.Length; i++)
                {
                    skillManager.RefreshSkill(i);
                }
            }
        }
    }
}
