using System.Collections;
using UnityEngine;

namespace ThirdPersonController
{
    public class BossGuardianPrototype : BossCombatTemplate
    {
        [Header("Prototype Defaults")]
        public bool autoSetupOnReset = true;

        [Header("Guardian Special")]
        public bool enableShockAftershock = true;
        [Min(0f)] public float aftershockDelay = 0.2f;
        [Range(0f, 1f)] public float aftershockDamageMultiplier = 0.55f;
        [Min(1f)] public float overloadSecondPulseRadiusMultiplier = 1.15f;

        [Header("Debug")]
        [SerializeField] private int debugLastPulseCount = 0;
        public int DebugLastPulseCount => debugLastPulseCount;

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
                    animatorTrigger = "Spray",
                    usePreferredRange = true,
                    preferredMinRange = 2.5f,
                    preferredMaxRange = 8f
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
                    animatorTrigger = "Slam",
                    usePreferredRange = true,
                    preferredMinRange = 0f,
                    preferredMaxRange = 4.5f
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
                    animatorTrigger = "Shock",
                    usePreferredRange = true,
                    preferredMinRange = 1.5f,
                    preferredMaxRange = 7f,
                    phase2WeightMultiplier = 1.2f
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
                    animatorTrigger = "Overload",
                    usePreferredRange = true,
                    preferredMinRange = 2f,
                    preferredMaxRange = 8f,
                    phase2WeightMultiplier = 1.4f
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
                    animatorTrigger = "Sweep",
                    usePreferredRange = true,
                    preferredMinRange = 0f,
                    preferredMaxRange = 5f,
                    phase2WeightMultiplier = 1.25f
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
                    animatorTrigger = "Spray",
                    usePreferredRange = true,
                    preferredMinRange = 2.5f,
                    preferredMaxRange = 8f,
                    phase2WeightMultiplier = 1.25f
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

            debugLastPulseCount = 0;
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
                    ApplyConePulse(5.5f, 140f, damage, knockback * 0.6f);
                    if (skill.active > 0f)
                    {
                        yield return new WaitForSeconds(skill.active);
                    }
                    break;
                }
                case "guard_shield":
                case "guard_sweep":
                {
                    float range = skill.id == "guard_sweep" ? 5f : 4.8f;
                    if (ApplyConePulse(range, 110f, damage, knockback))
                    {
                        if (currentPhase == BossCombatPhase.Phase2 && skill.id == "guard_sweep")
                        {
                            yield return new WaitForSeconds(0.12f);
                            ApplyConePulse(range, 130f, Mathf.RoundToInt(damage * 0.65f), knockback * 0.8f);
                        }
                    }
                    if (skill.active > 0f)
                    {
                        yield return new WaitForSeconds(skill.active);
                    }
                    break;
                }
                case "guard_shock":
                {
                    ApplyRadiusPulse(5.5f, damage, knockback * 1.1f);
                    if (enableShockAftershock && currentPhase == BossCombatPhase.Phase2)
                    {
                        if (aftershockDelay > 0f)
                        {
                            yield return new WaitForSeconds(aftershockDelay);
                        }

                        int aftershockDamage = Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Clamp01(aftershockDamageMultiplier)));
                        ApplyRadiusPulse(5.8f, aftershockDamage, knockback * 0.85f);
                    }
                    if (skill.active > 0f)
                    {
                        yield return new WaitForSeconds(skill.active);
                    }
                    break;
                }
                case "guard_overload":
                {
                    ApplyRadiusPulse(6.5f, damage, knockback * 1.2f);
                    if (aftershockDelay > 0f)
                    {
                        yield return new WaitForSeconds(aftershockDelay);
                    }

                    float secondRadius = 6.5f * Mathf.Max(1f, overloadSecondPulseRadiusMultiplier);
                    int secondPulseDamage = Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Max(0.2f, aftershockDamageMultiplier)));
                    ApplyRadiusPulse(secondRadius, secondPulseDamage, knockback);
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

        private bool ApplyConePulse(float range, float angle, int damage, float knockback)
        {
            debugLastPulseCount++;
            PlayerHealth player = GetPlayer();
            if (!IsPlayerInCone(player, range, angle))
            {
                return false;
            }

            ApplyDamageToPlayer(player, damage, knockback);
            return true;
        }

        private bool ApplyRadiusPulse(float radius, int damage, float knockback)
        {
            debugLastPulseCount++;
            PlayerHealth player = GetPlayer();
            if (!IsPlayerInRadius(player, radius))
            {
                return false;
            }

            ApplyDamageToPlayer(player, damage, knockback);
            return true;
        }
    }
}
