using System.Collections;
using UnityEngine;

namespace ThirdPersonController
{
    public class BossEelPrototype : BossCombatTemplate
    {
        [Header("Prototype Defaults")]
        public bool autoSetupOnReset = true;

        [Header("Eel Special")]
        public bool enableRiptideCombo = true;
        [Range(0f, 1f)] public float chainRushFollowupChance = 0.65f;
        [Min(0f)] public float chainRushFollowupDelay = 0.2f;
        [Min(0f)] public float vortexPullDistance = 1.6f;
        [Min(0f)] public float vortexPullMaxRange = 9f;
        [Min(1f)] public float devourBreakBonusMultiplier = 1.25f;

        [Header("Debug")]
        [SerializeField] private bool debugLastChainFollowupTriggered = false;
        public bool DebugLastChainFollowupTriggered => debugLastChainFollowupTriggered;

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
                    animatorTrigger = "Charge",
                    usePreferredRange = true,
                    preferredMinRange = 3.5f,
                    preferredMaxRange = 14f
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
                    animatorTrigger = "Tail",
                    usePreferredRange = true,
                    preferredMinRange = 0f,
                    preferredMaxRange = 4.5f
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
                    animatorTrigger = "Vortex",
                    usePreferredRange = true,
                    preferredMinRange = 2.5f,
                    preferredMaxRange = 8f,
                    phase2WeightMultiplier = 1.25f
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
                    animatorTrigger = "Chain",
                    usePreferredRange = true,
                    preferredMinRange = 3.5f,
                    preferredMaxRange = 14f,
                    phase2WeightMultiplier = 1.35f
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
                    animatorTrigger = "Devour",
                    usePreferredRange = true,
                    preferredMinRange = 0f,
                    preferredMaxRange = 5.5f,
                    phase2WeightMultiplier = 1.2f
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
                    animatorTrigger = "Tail",
                    usePreferredRange = true,
                    preferredMinRange = 0f,
                    preferredMaxRange = 5f,
                    phase2WeightMultiplier = 1.1f
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

            debugLastChainFollowupTriggered = false;
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
                    yield return DashForward(19f, skill.active, 2.4f, damage, knockback * 1.1f);
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
                    ApplyVortexPull(player);
                    if (IsPlayerInRadius(player, 6f))
                    {
                        ApplyDamageToPlayer(player, damage, 0f);
                    }
                    if (skill.active > 0f)
                    {
                        yield return new WaitForSeconds(skill.active);
                    }

                    yield return TryChainRushFollowup(damage, knockback);
                    break;
                }
                case "eel_devour":
                {
                    PlayerHealth player = GetPlayer();
                    if (IsBreakWindowActive)
                    {
                        damage = Mathf.RoundToInt(damage * Mathf.Max(1f, devourBreakBonusMultiplier));
                    }

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

        private void ApplyVortexPull(PlayerHealth player)
        {
            if (player == null)
            {
                return;
            }

            Vector3 toBoss = transform.position - player.transform.position;
            toBoss.y = 0f;
            float distance = toBoss.magnitude;
            if (distance <= 0.01f || distance > Mathf.Max(0.1f, vortexPullMaxRange))
            {
                return;
            }

            float pullAmount = Mathf.Min(Mathf.Max(0f, vortexPullDistance), Mathf.Max(0f, distance - 1.2f));
            if (pullAmount <= 0f)
            {
                return;
            }

            Vector3 pullDirection = toBoss / distance;
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null && !playerRb.isKinematic)
            {
                playerRb.AddForce(pullDirection * pullAmount, ForceMode.VelocityChange);
                return;
            }

            player.transform.position += pullDirection * pullAmount;
        }

        private IEnumerator TryChainRushFollowup(int baseDamage, float knockback)
        {
            if (!enableRiptideCombo || currentPhase != BossCombatPhase.Phase2)
            {
                yield break;
            }

            if (Random.value > Mathf.Clamp01(chainRushFollowupChance))
            {
                yield break;
            }

            BossSkillDefinition followup = FindSkillById("eel_chain");
            if (followup == null || !IsSkillReady(followup))
            {
                yield break;
            }

            debugLastChainFollowupTriggered = true;
            if (chainRushFollowupDelay > 0f)
            {
                yield return new WaitForSeconds(chainRushFollowupDelay);
            }

            if (animator != null && !string.IsNullOrEmpty(followup.animatorTrigger))
            {
                animator.SetTrigger(followup.animatorTrigger);
            }

            int followupDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.85f));
            yield return DashForward(19f, Mathf.Max(0.25f, followup.active), 2.4f, followupDamage, knockback * 1.1f);
            SetSkillCooldown(followup);
        }

        private BossSkillDefinition FindSkillById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < phase1Skills.Count; i++)
            {
                BossSkillDefinition skill = phase1Skills[i];
                if (skill != null && skill.id == id)
                {
                    return skill;
                }
            }

            for (int i = 0; i < phase2Skills.Count; i++)
            {
                BossSkillDefinition skill = phase2Skills[i];
                if (skill != null && skill.id == id)
                {
                    return skill;
                }
            }

            return null;
        }
    }
}
