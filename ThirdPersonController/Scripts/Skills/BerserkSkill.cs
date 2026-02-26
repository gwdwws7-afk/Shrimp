using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能4: 狂暴化 - 攻速移速大幅提升
    /// 按键: R
    /// </summary>
    [CreateAssetMenu(fileName = "SKILL_Berserk", menuName = "Skills/Berserk")]
    public class BerserkSkill : SkillBase
    {
        [Header("狂暴效果")]
        public float duration = 8f;
        public float attackSpeedMultiplier = 1.5f;
        public float moveSpeedMultiplier = 1.3f;
        public float damageMultiplier = 1.3f;
        [FormerlySerializedAs("damageReduction")]
        public float berserkDamageReduction = 0.3f;
        
        [Header("持续回血")]
        public bool enableLifeRegen = true;
        public float lifeRegenPerSecond = 5f;

        private void OnEnable()
        {
            if (category == SkillCategory.None)
            {
                category = SkillCategory.Burst;
            }

            if (useAnimationEvents)
            {
                impactDelay = 0.16f;
                recoveryDelay = 0.25f;
                impactShakeDuration = 0.12f;
                impactShakeStrength = 0.18f;
            }

            if (damageReduction <= 0f)
            {
                damageReduction = berserkDamageReduction;
            }

            if (damageReductionTiming == SkillDefenseTiming.None)
            {
                damageReductionTiming = SkillDefenseTiming.OnCast;
            }

            if (damageReductionDuration <= 0f)
            {
                damageReductionDuration = duration;
            }
        }
        
        public override void Execute(Transform caster, Vector3 targetPosition)
        {
            StartSkillTimeline(caster, caster.position, caster.rotation, () =>
            {
                // 开始狂暴Coroutine
                caster.GetComponent<MonoBehaviour>().StartCoroutine(
                    BerserkCoroutine(caster));

                Debug.Log($"🔥 狂暴化启动！持续 {duration} 秒");
            });
        }
        
        private IEnumerator BerserkCoroutine(Transform caster)
        {
            PlayerCombat combat = caster.GetComponent<PlayerCombat>();
            PlayerMovement movement = caster.GetComponent<PlayerMovement>();
            PlayerHealth health = caster.GetComponent<PlayerHealth>();
            
            // 保存原始值
            float originalDamage = 0;
            if (combat != null) originalDamage = combat.attackDamage;
            
            // 应用增益
            if (combat != null)
            {
                combat.attackDamage = Mathf.RoundToInt(originalDamage * damageMultiplier);
            }
            
            // 特效循环
            float elapsed = 0f;
            float regenTimer = 0f;
            
            while (elapsed < duration)
            {
                // 持续回血
                if (enableLifeRegen && health != null)
                {
                    regenTimer += Time.deltaTime;
                    if (regenTimer >= 1f)
                    {
                        regenTimer = 0f;
                        health.Heal(Mathf.RoundToInt(lifeRegenPerSecond));
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 恢复原始值
            if (combat != null)
            {
                combat.attackDamage = (int)originalDamage;
            }
            
            Debug.Log("💨 狂暴化结束");
        }
    }
}
