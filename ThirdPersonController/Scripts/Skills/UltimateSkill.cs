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
        
        public override void Execute(Transform caster, Vector3 targetPosition)
        {
            // 慢动作效果
            Time.timeScale = slowMotionScale;
            caster.GetComponent<MonoBehaviour>().Invoke(nameof(RestoreTimeScale), slowMotionDuration);
            
            // 播放特效
            SpawnEffect(caster.position, caster.rotation);
            PlaySound(castSound, caster.position);
            
            // 触发动画
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Ultimate");
            }
            
            // 执行全屏攻击
            ExecuteUltimate(caster);
        }
        
        private void RestoreTimeScale()
        {
            Time.timeScale = 1f;
        }
        
        private System.Collections.IEnumerator RestoreAIAfterDelay(EnemyAI enemyAI, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (enemyAI != null)
            {
                enemyAI.enabled = true;
            }
        }
        
        private void ExecuteUltimate(Transform caster)
        {
            // 全屏范围检测
            Collider[] hitColliders = Physics.OverlapSphere(caster.position, effectRadius, LayerMask.GetMask("Enemy"));
            
            List<EnemyHealth> killedEnemies = new List<EnemyHealth>();
            int hitCount = 0;
            
            foreach (var hitCollider in hitColliders)
            {
                EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
                EnemyAI enemyAI = hitCollider.GetComponent<EnemyAI>();
                
                if (enemyHealth != null)
                {
                    int previousHealth = enemyHealth.CurrentHealth;
                    
                    // 造成伤害
                    Vector3 knockbackDir = (hitCollider.transform.position - caster.position).normalized;
                    knockbackDir.y = 0.5f;
                    enemyHealth.TakeDamage(damage, caster.position, knockbackForce);
                    
                    hitCount++;
                    
                    // 检查是否击杀
                    if (enemyHealth.IsDead && previousHealth > 0)
                    {
                        killedEnemies.Add(enemyHealth);
                    }
                    
                    // 眩晕
                    if (enemyAI != null)
                    {
                        enemyAI.enabled = false;
                        caster.GetComponent<MonoBehaviour>().StartCoroutine(
                            RestoreAIAfterDelay(enemyAI, stunDuration));
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
