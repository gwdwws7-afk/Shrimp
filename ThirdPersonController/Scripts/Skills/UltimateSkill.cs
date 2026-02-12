using UnityEngine;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能6: 终极审判 - 全屏高伤害，击杀刷新小技能CD
    /// 按键: F
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_Ultimate", menuName = "Skills/Ultimate")]
    public class UltimateSkill : SkillBase
    {
        [Header("终极技能设置")]
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

        private void OnEnable()
        {
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
            // 触发动画
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Ultimate");
            }

            StartSkillTimeline(caster, caster.position, caster.rotation, () =>
            {
                // 慢动作效果
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

                // 执行全屏攻击
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
            // 全屏范围检测
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

                EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
                EnemyAI enemyAI = hitCollider.GetComponent<EnemyAI>();

                if (enemyHealth != null)
                {
                    int previousHealth = enemyHealth.CurrentHealth;

                    DamageContext context = new DamageContext
                    {
                        source = caster,
                        sourceType = DamageSourceType.PlayerSkill,
                        damage = adjustedDamage,
                        knockback = adjustedKnockback,
                        damageOrigin = caster.position,
                        hitPoint = hitCollider.bounds.center,
                        hasHitPoint = true,
                        isCritical = false,
                        showDamageText = true,
                        hitStopDuration = 0f
                    };

                    if (DamageService.ApplyDamage(context, hitCollider))
                    {
                        hitCount++;

                        if (enemyHealth.IsDead && previousHealth > 0)
                        {
                            killedEnemies.Add(enemyHealth);
                        }

                        // 眩晕
                        if (enemyAI != null)
                        {
                            enemyAI.ApplyStun(stunDuration);
                        }
                    }
                }
            }
            
            // 播放命中音效
            if (hitCount > 0 && hitSound != null)
            {
                PlaySound(hitSound, caster.position);
            }
            
            // 刷新小技能CD
            if (refreshCooldownsOnKill && killedEnemies.Count > 0)
            {
                RefreshSkillCooldowns(caster);
                Debug.Log($"⚡ 终极审判击杀 {killedEnemies.Count} 个敌人，小技能CD已刷新！");
            }
            
            Debug.Log($"💥 终极审判命中 {hitCount} 个敌人！");
        }
        
        private void RefreshSkillCooldowns(Transform caster)
        {
            SkillManager skillManager = caster.GetComponent<SkillManager>();
            if (skillManager != null)
            {
                // 刷新QWER技能（不包括终极技能自己）
                for (int i = 0; i < 4 && i < skillManager.skills.Length; i++)
                {
                    skillManager.RefreshSkill(i);
                }
            }
        }
    }
}
