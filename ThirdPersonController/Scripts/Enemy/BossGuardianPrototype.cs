using System.Collections;
using UnityEngine;

namespace ThirdPersonController
{
    public class BossGuardianPrototype : BossCombatTemplate
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
                    id = "guard_spray",
                    name = "Corrosion Spray",
                    weight = 1f,
                    cooldown = 6f,
                    windup = 0.5f,
                    active = 0.7f,
                    recovery = 0.8f,
                    animatorTrigger = "Spray"
                });
                phase1Skills.Add(new BossSkillDefinition
                {
                    id = "guard_shield",
                    name = "Shield Slam",
                    weight = 1.1f,
                    cooldown = 5f,
                    windup = 0.4f,
                    active = 0.6f,
                    recovery = 0.7f,
                    animatorTrigger = "Slam"
                });
                phase1Skills.Add(new BossSkillDefinition
                {
                    id = "guard_shock",
                    name = "Ground Shock",
                    weight = 0.9f,
                    cooldown = 7f,
                    windup = 0.6f,
                    active = 0.8f,
                    recovery = 0.8f,
                    animatorTrigger = "Shock"
                });
            }

            if (phase2Skills.Count == 0)
            {
                phase2Skills.Add(new BossSkillDefinition
                {
                    id = "guard_overload",
                    name = "Overload Burst",
                    weight = 1.1f,
                    cooldown = 8f,
                    windup = 0.7f,
                    active = 0.8f,
                    recovery = 0.9f,
                    animatorTrigger = "Overload"
                });
                phase2Skills.Add(new BossSkillDefinition
                {
                    id = "guard_sweep",
                    name = "Blade Sweep",
                    weight = 1f,
                    cooldown = 6f,
                    windup = 0.4f,
                    active = 0.6f,
                    recovery = 0.7f,
                    animatorTrigger = "Sweep"
                });
                phase2Skills.Add(new BossSkillDefinition
                {
                    id = "guard_spray_p2",
                    name = "Corrosion Wave",
                    weight = 0.9f,
                    cooldown = 7f,
                    windup = 0.6f,
                    active = 0.7f,
                    recovery = 0.8f,
                    animatorTrigger = "Spray"
                });
            }

            phase2HealthThreshold = 0.5f;
            breakWindowDuration = 4f;
            staggerMax = 140f;
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
                case "guard_spray":
                case "guard_spray_p2":
                {
                    PlayerHealth player = GetPlayer();
                    if (IsPlayerInCone(player, 5.5f, 140f))
                    {
                        ApplyDamageToPlayer(player, damage, knockback * 0.6f);
                    }
                    if (skill.active > 0f)
                    {
                        yield return new WaitForSeconds(skill.active);
                    }
                    break;
                }
                case "guard_shield":
                case "guard_sweep":
                {
                    PlayerHealth player = GetPlayer();
                    if (IsPlayerInCone(player, 4.8f, 110f))
                    {
                        ApplyDamageToPlayer(player, damage, knockback);
                    }
                    if (skill.active > 0f)
                    {
                        yield return new WaitForSeconds(skill.active);
                    }
                    break;
                }
                case "guard_shock":
                case "guard_overload":
                {
                    PlayerHealth player = GetPlayer();
                    float radius = skill.id == "guard_overload" ? 6.5f : 5.5f;
                    if (IsPlayerInRadius(player, radius))
                    {
                        ApplyDamageToPlayer(player, damage, knockback * 1.1f);
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
