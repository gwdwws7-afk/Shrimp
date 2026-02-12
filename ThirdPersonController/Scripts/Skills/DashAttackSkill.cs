using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能3: 深渊突袭 - 瞬间移动并攻击路径敌人
    /// 按键: E
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_DashAttack", menuName = "Skills/DashAttack")]
    public class DashAttackSkill : SkillBase
    {
        [Header("突袭设置")]
        public float dashDistance = 8f;
        public float dashSpeed = 20f;
        public float hitBoxWidth = 2f;
        public int pathDamage = 30;
        public float pathKnockback = 5f;
        
        [Header("无敌")]
        public bool invincibleDuringDash = true;
        public float invincibilityDuration = 0.5f;

        private void OnEnable()
        {
            if (category == SkillCategory.None)
            {
                category = SkillCategory.Mobility;
            }

            if (useAnimationEvents)
            {
                impactDelay = 0.08f;
                recoveryDelay = 0.22f;
                impactShakeDuration = 0.1f;
                impactShakeStrength = 0.2f;
            }
        }
        
        public override void Execute(Transform caster, Vector3 targetPosition)
        {
            StartSkillTimeline(caster, caster.position, caster.rotation, () =>
            {
                caster.GetComponent<MonoBehaviour>().StartCoroutine(
                    DashCoroutine(caster));
            });
        }
        
        private System.Collections.IEnumerator DashCoroutine(Transform caster)
        {
            // 触发动画
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Dash");
            }
            
            // 无敌状态
            PlayerHealth health = caster.GetComponent<PlayerHealth>();
            if (health != null && invincibleDuringDash)
            {
                // 这里可以添加无敌逻辑
            }
            
            // 禁用玩家控制
            PlayerMovement movement = caster.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;
            
            // 计算起点和终点
            Vector3 startPos = caster.position;
            Vector3 dashDirection = caster.forward;
            Vector3 endPos = startPos + dashDirection * dashDistance;
            
            // 检查终点是否有效（不穿透墙壁）
            if (Physics.Raycast(startPos + Vector3.up, dashDirection, out RaycastHit hit, dashDistance, LayerMask.GetMask("Default")))
            {
                endPos = hit.point - dashDirection * 0.5f;
            }
            
            // 突袭过程中检测敌人
            float traveled = 0f;
            Vector3 lastPos = startPos;
            
            while (traveled < dashDistance)
            {
                float moveDistance = dashSpeed * Time.deltaTime;
                caster.position += dashDirection * moveDistance;
                traveled += moveDistance;
                
                // 检测路径上的敌人
                DetectEnemiesInPath(lastPos, caster.position, hitBoxWidth, caster);
                
                lastPos = caster.position;
                yield return null;
            }
            
            // 确保到达终点
            caster.position = endPos;
            
            // 恢复控制
            if (movement != null) movement.enabled = true;
            
            // 播放结束特效
            SpawnEffect(endPos, caster.rotation);
        }
        
        private void DetectEnemiesInPath(Vector3 from, Vector3 to, float width, Transform caster)
        {
            Vector3 direction = (to - from).normalized;
            float distance = Vector3.Distance(from, to);
            
            // 使用BoxCast检测路径上的敌人
            RaycastHit[] hits = Physics.BoxCastAll(from, Vector3.one * width * 0.5f, 
                direction, Quaternion.LookRotation(direction), distance, 
                LayerMask.GetMask("Enemy"));
            
            foreach (var hit in hits)
            {
                EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    Vector3 knockbackDir = (hit.collider.transform.position - caster.position).normalized;
                    enemyHealth.TakeDamage(pathDamage, caster.position, pathKnockback);
                }
            }
        }
    }
}
