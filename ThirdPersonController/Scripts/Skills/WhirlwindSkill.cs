using UnityEngine;
using System.Collections;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能1: 深海旋风 - 360度旋转攻击
    /// 按键: Q
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_Whirlwind", menuName = "Skills/Whirlwind")]
    public class WhirlwindSkill : SkillBase
    {
        [Header("旋风设置")]
        public float duration = 2f;
        public float tickRate = 0.3f;
        public float radius = 3f;
        public float rotationSpeed = 720f; // 每秒旋转角度
        public int tickDamage = 15;
        
        [Header("击退")]
        public float knockbackForce = 8f;
        
        public override void Execute(Transform caster, Vector3 targetPosition)
        {
            // 播放特效
            SpawnEffect(caster.position, caster.rotation);
            PlaySound(castSound, caster.position);
            
            // 开始旋风Coroutine
            caster.GetComponent<MonoBehaviour>().StartCoroutine(
                WhirlwindCoroutine(caster));
            
            // 触发动画
            Animator animator = caster.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Whirlwind");
            }
        }
        
        private IEnumerator WhirlwindCoroutine(Transform caster)
        {
            float elapsed = 0f;
            float tickTimer = 0f;
            
            // 禁用玩家移动
            PlayerMovement movement = caster.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;
            
            while (elapsed < duration)
            {
                // 旋转
                caster.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                
                // 定时伤害
                tickTimer += Time.deltaTime;
                if (tickTimer >= tickRate)
                {
                    tickTimer = 0f;
                    DealDamage(caster);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 恢复移动
            if (movement != null) movement.enabled = true;
        }
        
        private void DealDamage(Transform caster)
        {
            // 查找范围内敌人
            Collider[] hitColliders = Physics.OverlapSphere(caster.position, radius, LayerMask.GetMask("Enemy"));
            
            foreach (var hitCollider in hitColliders)
            {
                EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    // 计算击退方向
                    Vector3 knockbackDir = (hitCollider.transform.position - caster.position).normalized;
                    knockbackDir.y = 0.3f;
                    
                    enemyHealth.TakeDamage(tickDamage, caster.position, knockbackForce);
                }
            }
            
            // 播放命中音效
            if (hitColliders.Length > 0 && hitSound != null)
            {
                PlaySound(hitSound, caster.position);
            }
        }
    }
}
