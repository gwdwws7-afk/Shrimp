using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能2: 震荡波 - 前方扇形冲击波
    /// 按键: W
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_Shockwave", menuName = "Skills/Shockwave")]
    public class ShockwaveSkill : SkillBase
    {
        [Header("冲击波设置")]
        public float coneAngle = 90f;       // 扇形角度
        public float coneRange = 8f;        // 扇形距离
        public float stunDuration = 2f;     // 眩晕时间
        public float knockbackForce = 12f;  // 击退力度

        private void OnEnable()
        {
            if (category == SkillCategory.None)
            {
                category = SkillCategory.CrowdControl;
            }

            if (useAnimationEvents)
            {
                impactDelay = 0.22f;
                recoveryDelay = 0.28f;
                impactShakeDuration = 0.12f;
                impactShakeStrength = 0.18f;
            }
        }
        
        public override void Execute(Transform caster, Vector3 targetPosition)
        {
            // 触发动画
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Shockwave");
            }

            Vector3 impactPosition = caster.position + caster.forward * 2f;
            StartSkillTimeline(caster, impactPosition, caster.rotation, () =>
            {
                // 检测扇形范围内敌人
                DetectAndDamage(caster);
            });
        }
        
        private void DetectAndDamage(Transform caster)
        {
            // 使用OverlapSphere获取所有敌人
            Collider[] hitColliders = Physics.OverlapSphere(caster.position, coneRange, LayerMask.GetMask("Enemy"));
            
            int hitCount = 0;
            
            foreach (var hitCollider in hitColliders)
            {
                // 检查是否在扇形范围内
                Vector3 directionToEnemy = (hitCollider.transform.position - caster.position).normalized;
                float angleToEnemy = Vector3.Angle(caster.forward, directionToEnemy);
                
                if (angleToEnemy <= coneAngle * 0.5f)
                {
                    // 射线检测确保没有墙壁阻挡
                    if (!Physics.Raycast(caster.position + Vector3.up, directionToEnemy, 
                        Vector3.Distance(caster.position, hitCollider.transform.position), 
                        LayerMask.GetMask("Default")))
                    {
                        // 造成伤害和击退
                        EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
                        if (enemyHealth != null)
                        {
                            Vector3 knockbackDir = directionToEnemy;
                            knockbackDir.y = 0.5f;
                            
                            enemyHealth.TakeDamage(damage, caster.position, knockbackForce);
                            
                            // 眩晕效果（如果敌人有AI）
                            EnemyAI ai = hitCollider.GetComponent<EnemyAI>();
                            if (ai != null)
                            {
                                // 这里可以添加眩晕逻辑
                            }
                            
                            hitCount++;
                        }
                    }
                }
            }
            
            if (hitCount > 0)
            {
                Debug.Log($"💥 震荡波命中 {hitCount} 个敌人！");
            }
        }
    }
}
