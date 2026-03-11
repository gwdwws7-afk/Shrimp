using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// BerserkSkill 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// 提升输出并在持续期间提供周期回血。
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
        [System.NonSerialized] private readonly Dictionary<int, Coroutine> activeCoroutines = new Dictionary<int, Coroutine>();
        [System.NonSerialized] private readonly Dictionary<int, MonoBehaviour> activeRunners = new Dictionary<int, MonoBehaviour>();

        protected override void OnEnable()
        {
            base.OnEnable();
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
                MonoBehaviour runner = caster.GetComponent<MonoBehaviour>();
                if (runner == null)
                {
                    return;
                }

                int casterId = caster.GetInstanceID();
                StopActiveBerserk(caster, casterId);
                Coroutine routine = runner.StartCoroutine(BerserkCoroutine(caster, casterId));
                activeCoroutines[casterId] = routine;
                activeRunners[casterId] = runner;

                Debug.Log($"[Skill] Berserk started: duration={duration:0.##}s");
            });
        }
        
        private IEnumerator BerserkCoroutine(Transform caster, int casterId)
        {
            PlayerCombat combat = caster.GetComponent<PlayerCombat>();
            PlayerHealth health = caster.GetComponent<PlayerHealth>();

            if (combat != null)
            {
                combat.SetSkillDamageBuffMultiplier(damageMultiplier);
            }
            
// 狂暴持续计时，用于定义效果生效窗口。
            float elapsed = 0f;
            float regenTimer = 0f;
            
            while (elapsed < duration)
            {
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
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
            
            // 狂暴结束后恢复额外伤害倍率
            if (combat != null)
            {
                combat.ClearSkillDamageBuffMultiplier();
            }

            activeCoroutines.Remove(casterId);
            activeRunners.Remove(casterId);
            
            Debug.Log("[Skill] Berserk ended.");
        }

        private void StopActiveBerserk(Transform caster, int casterId)
        {
            if (activeCoroutines.TryGetValue(casterId, out Coroutine running))
            {
                if (activeRunners.TryGetValue(casterId, out MonoBehaviour runner) && runner != null)
                {
                    runner.StopCoroutine(running);
                }
            }

            PlayerCombat combat = caster.GetComponent<PlayerCombat>();
            if (combat != null)
            {
                combat.ClearSkillDamageBuffMultiplier();
            }

            activeCoroutines.Remove(casterId);
            activeRunners.Remove(casterId);
        }
    }
}
