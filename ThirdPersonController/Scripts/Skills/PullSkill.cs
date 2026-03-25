using UnityEngine;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// PullSkill 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// 将范围内敌人拉向玩家并在落地时结算伤害。
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_Pull", menuName = "Skills/Pull")]
    public class PullSkill : SkillBase
    {
        [Header("牵引设置")]
        public float pullRadius = 10f;
        public float pullForce = 15f;
        public float liftHeight = 3f;
        public float floatDuration = 1.5f;
        
        [Header("伤害")]
        public int landingDamage = 40;
        public float landingKnockback = 8f;

        private readonly List<Collider> hitTargets = new List<Collider>();

        protected override void OnEnable()
        {
            base.OnEnable();
            if (category == SkillCategory.None)
            {
                category = SkillCategory.Gather;
            }

            if (useAnimationEvents)
            {
                impactDelay = 0.24f;
                recoveryDelay = 0.32f;
                impactShakeDuration = 0.12f;
                impactShakeStrength = 0.22f;
            }
        }
        
        public override void Execute(Transform caster, Vector3 targetPosition)
        {
// 触发施法动画，用于强化动作反馈并统一表现节奏。
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Pull");
            }

            StartSkillTimeline(caster, caster.position, caster.rotation, () =>
            {
                // 执行牵引与后续落地处理
                PullEnemies(caster);

                Debug.Log($"[Skill] Pull cast at radius {pullRadius:0.0}m.");
            });
        }
        
        private void PullEnemies(Transform caster)
        {
            float adjustedRadius = GetModifiedRange(caster, pullRadius);
            HitQuery.OverlapSphere(caster.position, adjustedRadius, LayerMask.GetMask("Enemy"), hitTargets);

            for (int i = 0; i < hitTargets.Count; i++)
            {
                Collider hitCollider = hitTargets[i];
                if (hitCollider == null)
                {
                    continue;
                }

                EnemyHealth enemyHealth = ResolveEnemyHealth(hitCollider);
                Rigidbody enemyRb = hitCollider.attachedRigidbody != null
                    ? hitCollider.attachedRigidbody
                    : hitCollider.GetComponentInParent<Rigidbody>();
                EnemyAI enemyAI = ResolveEnemyAI(hitCollider);
                
                if (enemyHealth != null && enemyRb != null)
                {
                    float displacementScale = GetEnemyDisplacementMultiplier(enemyHealth);
                    if (displacementScale <= 0.01f)
                    {
                        continue;
                    }

                    // 牵引期间关闭敌人 AI，避免反向寻路干扰位移
                    if (enemyAI != null) enemyAI.enabled = false;
                    
                    // 计算牵引方向与距离衰减
                    Vector3 pullDirection = (caster.position - hitCollider.transform.position).normalized;
                    float distance = Vector3.Distance(caster.position, hitCollider.transform.position);
                    float forceMultiplier = 1f - (distance / adjustedRadius); // 距离越近，牵引力越强
                    
                    // 施加牵引与上抛冲量
                    enemyRb.AddForce(
                        pullDirection * pullForce * forceMultiplier * displacementScale
                        + Vector3.up * liftHeight * displacementScale,
                        ForceMode.Impulse);
                    
                    // 开始监听落地并结算二段伤害
                    MonoBehaviour runner = caster.GetComponent<MonoBehaviour>();
                    if (runner != null)
                    {
                        runner.StartCoroutine(HandleLanding(hitCollider.gameObject, enemyHealth, enemyAI, caster));
                    }
                }
            }
            
            if (hitTargets.Count > 0)
            {
                Debug.Log($"[Skill] Pull affected {hitTargets.Count} enemies.");
            }
        }
        
        private System.Collections.IEnumerator HandleLanding(GameObject enemy, EnemyHealth health, EnemyAI ai, Transform caster)
        {
            // 等待浮空结束后再检测落地
            yield return new WaitForSeconds(floatDuration);

            if (health == null || health.IsDead)
            {
                if (ai != null)
                {
                    ai.enabled = true;
                }
                yield break;
            }
            
            // 最长检测 3 秒，避免协程卡死
            float checkTimer = 0f;
            bool hasLanded = false;
            
            while (checkTimer < 3f && !hasLanded)
            {
                // 命中地面，触发落地伤害
                if (Physics.Raycast(enemy.transform.position, Vector3.down, 0.5f, LayerMask.GetMask("Default")))
                {
                    hasLanded = true;
                    
                    // 根据属性修正落地伤害与击退
                    int adjustedDamage = GetModifiedDamage(caster, landingDamage);
                    float adjustedKnockback = GetModifiedKnockback(caster, landingKnockback);

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
                        hitPoint = enemy.transform.position,
                        hasHitPoint = true,
                        isCritical = false,
                        showDamageText = true,
                        hitStopDuration = 0f,
                        isHeavyAttack = false
                    };

                    Collider targetCollider = health.GetComponent<Collider>();
                    if (targetCollider == null)
                    {
                        targetCollider = enemy.GetComponentInChildren<Collider>();
                    }

                    DamageService.ApplyDamage(context, targetCollider);
                    
                    // 落地结算后恢复敌人 AI
                    if (ai != null) ai.enabled = true;
                    
                    break;
                }
                
                checkTimer += Time.deltaTime;
                yield return null;
            }
            
            // 超时未落地时也恢复 AI，避免敌人失活
            if (!hasLanded && ai != null)
            {
                ai.enabled = true;
            }
        }
    }
}
