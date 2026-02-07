using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能5: 异种之握 - 将周围敌人拉向自己并浮空
    /// 按键: T
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
        
        public override void Execute(Transform caster, Vector3 targetPosition)
        {
            // 播放特效
            SpawnEffect(caster.position, caster.rotation);
            PlaySound(castSound, caster.position);
            
            // 触发动画
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Pull");
            }
            
            // 牵引敌人
            PullEnemies(caster);
            
            Debug.Log($"🌀 异种之握！牵引 {pullRadius}m 内的敌人");
        }
        
        private void PullEnemies(Transform caster)
        {
            Collider[] hitColliders = Physics.OverlapSphere(caster.position, pullRadius, LayerMask.GetMask("Enemy"));
            
            foreach (var hitCollider in hitColliders)
            {
                EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
                Rigidbody enemyRb = hitCollider.GetComponent<Rigidbody>();
                EnemyAI enemyAI = hitCollider.GetComponent<EnemyAI>();
                
                if (enemyHealth != null && enemyRb != null)
                {
                    // 暂时禁用AI
                    if (enemyAI != null) enemyAI.enabled = false;
                    
                    // 计算牵引方向和力度
                    Vector3 pullDirection = (caster.position - hitCollider.transform.position).normalized;
                    float distance = Vector3.Distance(caster.position, hitCollider.transform.position);
                    float forceMultiplier = 1f - (distance / pullRadius); // 越近拉力越大
                    
                    // 应用牵引力
                    enemyRb.AddForce(pullDirection * pullForce * forceMultiplier + Vector3.up * liftHeight, ForceMode.Impulse);
                    
                    // 启动协程处理落地伤害
                    caster.GetComponent<MonoBehaviour>().StartCoroutine(
                        HandleLanding(hitCollider.gameObject, enemyHealth, enemyRb, enemyAI, caster));
                }
            }
            
            if (hitColliders.Length > 0)
            {
                Debug.Log($"🎯 牵引了 {hitColliders.Length} 个敌人");
            }
        }
        
        private System.Collections.IEnumerator HandleLanding(GameObject enemy, EnemyHealth health, Rigidbody rb, EnemyAI ai, Transform caster)
        {
            // 等待浮空时间
            yield return new WaitForSeconds(floatDuration);
            
            // 检查是否落地
            float checkTimer = 0f;
            bool hasLanded = false;
            
            while (checkTimer < 3f && !hasLanded)
            {
                // 简单检测是否着地（距离地面高度）
                if (Physics.Raycast(enemy.transform.position, Vector3.down, 0.5f, LayerMask.GetMask("Default")))
                {
                    hasLanded = true;
                    
                    // 造成落地伤害
                    health.TakeDamage(landingDamage, caster.position, landingKnockback);
                    
                    // 恢复AI
                    if (ai != null) ai.enabled = true;
                    
                    break;
                }
                
                checkTimer += Time.deltaTime;
                yield return null;
            }
            
            // 超时恢复AI
            if (!hasLanded && ai != null)
            {
                ai.enabled = true;
            }
        }
    }
}
