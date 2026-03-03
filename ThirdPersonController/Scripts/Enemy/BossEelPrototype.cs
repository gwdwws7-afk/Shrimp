using System.Collections;
using UnityEngine;

namespace ThirdPersonController
{
    public class BossEelPrototype : BossCombatTemplate
    {
        [Header("Prototype Defaults")]
        public bool autoSetupOnReset = true;

        private void Reset()
        {
            if (!autoSetupOnReset)
            {
                return;
            }

            if (phase1Skills.Count == 0)
            {
                phase1Skills.Add(new BossSkillDefinition
                {
                    id = "eel_charge",
                    name = "Piercing Charge",
                    weight = 1.2f,
                    cooldown = 6f,
                    windup = 0.4f,
                    active = 0.6f,
                    recovery = 0.7f,
                    animatorTrigger = "Charge"
                });
                phase1Skills.Add(new BossSkillDefinition
                {
                    id = "eel_tail",
                    name = "Tail Sweep",
                    weight = 1f,
                    cooldown = 5f,
                    windup = 0.3f,
                    active = 0.5f,
                    recovery = 0.6f,
                    animatorTrigger = "Tail"
                });
                phase1Skills.Add(new BossSkillDefinition
                {
                    id = "eel_vortex",
                    name = "Vortex Pull",
                    weight = 0.8f,
                    cooldown = 8f,
                    windup = 0.6f,
                    active = 0.8f,
                    recovery = 0.8f,
                    animatorTrigger = "Vortex"
                });
            }

            if (phase2Skills.Count == 0)
            {
                phase2Skills.Add(new BossSkillDefinition
                {
                    id = "eel_chain",
                    name = "Chain Rush",
                    weight = 1.2f,
                    cooldown = 7f,
                    windup = 0.4f,
                    active = 0.7f,
                    recovery = 0.7f,
                    animatorTrigger = "Chain"
                });
                phase2Skills.Add(new BossSkillDefinition
                {
                    id = "eel_devour",
                    name = "Abyss Devour",
                    weight = 0.9f,
                    cooldown = 9f,
                    windup = 0.8f,
                    active = 0.9f,
                    recovery = 0.9f,
                    animatorTrigger = "Devour"
                });
                phase2Skills.Add(new BossSkillDefinition
                {
                    id = "eel_tail_over",
                    name = "Overtail Sweep",
                    weight = 0.8f,
                    cooldown = 6f,
                    windup = 0.4f,
                    active = 0.6f,
                    recovery = 0.6f,
                    animatorTrigger = "Tail"
                });
            }

            phase2HealthThreshold = 0.5f;
            breakWindowDuration = 4f;
            staggerMax = 120f;
            staggerPerDamage = 1f;
        }

        protected override IEnumerator ExecuteSkill(BossSkillDefinition skill)
        {
            if (skill == null)
            {
                yield break;
            }

            BeginSkillExecution(skill);

            if (skill.windup > 0f)
            {
                yield return new WaitForSeconds(skill.windup);
            }

            int damage = GetSkillDamage(skill);
            float knockback = GetSkillKnockback();

            switch (skill.id)
            {
                case "eel_charge":
                    yield return DashForward(16f, skill.active, 2.2f, damage, knockback);
                    break;
                case "eel_chain":
                    yield return DashForward(18f, skill.active, 2.4f, damage, knockback);
                    break;
                case "eel_tail":
                case "eel_tail_over":
                {
                    PlayerHealth player = GetPlayer();
                    if (IsPlayerInCone(player, 4.5f, 120f))
                    {
                        ApplyDamageToPlayer(player, damage, knockback);
                    }
                    if (skill.active > 0f)
                    {
                        yield return new WaitForSeconds(skill.active);
                    }
                    break;
                }
                case "eel_vortex":
                {
                    PlayerHealth player = GetPlayer();
                    if (IsPlayerInRadius(player, 6f))
                    {
                        ApplyDamageToPlayer(player, damage, 0f);
                    }
                    if (skill.active > 0f)
                    {
                        yield return new WaitForSeconds(skill.active);
                    }
                    break;
                }
                case "eel_devour":
                {
                    PlayerHealth player = GetPlayer();
                    if (IsPlayerInRadius(player, 5.5f))
                    {
                        ApplyDamageToPlayer(player, damage, knockback * 1.3f);
                    }
                    if (skill.active > 0f)
                    {
                        yield return new WaitForSeconds(skill.active);
                    }
                    break;
                }
                default:
                    if (skill.active > 0f)
                    {
                        yield return new WaitForSeconds(skill.active);
                    }
                    break;
            }

            if (skill.recovery > 0f)
            {
                yield return new WaitForSeconds(skill.recovery);
            }

            EndSkillExecution(skill);
        }
    }
}
