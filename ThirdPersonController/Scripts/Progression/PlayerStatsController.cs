using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class PlayerStatsController : MonoBehaviour
    {
        private PlayerCombat combat;
        private PlayerHealth health;
        private StaminaSystem stamina;
        private PlayerMovement movement;
        private PlayerMusouSystem musou;
        private PearlEquipment equipment;
        private TalentTree talentTree;

        private int baseAttackDamage;
        private float baseAttackRange;
        private float baseAttackAngle;
        private float baseAttackKnockback;
        private float baseAttackSpeed;
        private float baseCriticalRate;
        private float baseCriticalDamage;
        private int baseMaxHealth;
        private float baseMaxStamina;
        private float baseDefense;
        private float baseDodgeRate;
        private float baseHealthRegen;
        private float baseWalkSpeed;
        private float baseSprintSpeed;
        private float baseCrouchSpeed;
        private float baseDodgeDistance;
        private float baseDodgeInvincibility;
        private float baseMusouGain;
        private float baseLifeSteal;
        private float baseBossDamage;
        private float baseBerserkDuration;

        private readonly List<StatModifier> cachedModifiers = new List<StatModifier>();

        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<PlayerHealth>();
            stamina = GetComponent<StaminaSystem>();
            movement = GetComponent<PlayerMovement>();
            musou = GetComponent<PlayerMusouSystem>();
            equipment = GetComponent<PearlEquipment>();
            talentTree = GetComponent<TalentTree>();

            CacheBaseStats();
        }

        private void OnEnable()
        {
            if (equipment != null)
            {
                equipment.OnEquipmentChanged += Recalculate;
            }

            if (talentTree != null)
            {
                talentTree.OnTalentChanged += Recalculate;
            }

            Recalculate();
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.OnEquipmentChanged -= Recalculate;
            }

            if (talentTree != null)
            {
                talentTree.OnTalentChanged -= Recalculate;
            }
        }

        private void CacheBaseStats()
        {
            if (combat != null)
            {
                baseAttackDamage = combat.attackDamage;
                baseAttackRange = combat.attackRange;
                baseAttackAngle = combat.attackAngle;
                baseAttackKnockback = combat.attackKnockback;
                baseAttackSpeed = combat.attackSpeed;
                baseCriticalRate = combat.criticalRate;
                baseCriticalDamage = combat.criticalDamage;
            }

            if (health != null)
            {
                baseMaxHealth = health.maxHealth;
                baseDefense = 0;
                baseDodgeRate = 0;
                baseHealthRegen = 0;
            }

            if (stamina != null)
            {
                baseMaxStamina = stamina.maxStamina;
            }

            if (movement != null)
            {
                baseWalkSpeed = movement.walkSpeed;
                baseSprintSpeed = movement.sprintSpeed;
                baseCrouchSpeed = movement.crouchSpeed;
                baseDodgeDistance = 3f;
                baseDodgeInvincibility = 0.2f;
            }
        }

        public void Recalculate()
        {
            cachedModifiers.Clear();
            if (equipment != null)
            {
                cachedModifiers.AddRange(equipment.GetModifiers());
            }

            if (talentTree != null)
            {
                cachedModifiers.AddRange(talentTree.GetModifiers());
            }

            ApplyVitalStats();
            ApplyMovementStats();
            ApplyCombatStats();
        }

        private void ApplyVitalStats()
        {
            if (health != null)
            {
                int newMaxHealth = Mathf.RoundToInt(ApplyModifiers(baseMaxHealth, StatType.MaxHealth));
                health.ApplyMaxHealth(newMaxHealth, true);
            }

            if (stamina != null)
            {
                float newMaxStamina = ApplyModifiers(baseMaxStamina, StatType.MaxStamina);
                stamina.ApplyMaxStamina(newMaxStamina, true);
            }
        }

        private void ApplyMovementStats()
        {
            if (movement == null)
            {
                return;
            }

            float moveMultiplier = ApplyModifiers(1f, StatType.MoveSpeed);
            movement.walkSpeed = baseWalkSpeed * moveMultiplier;
            movement.sprintSpeed = baseSprintSpeed * moveMultiplier;
            movement.crouchSpeed = baseCrouchSpeed * moveMultiplier;
        }

        private void ApplyCombatStats()
        {
            if (combat != null)
            {
                combat.attackSpeed = Mathf.Max(0.1f, ApplyModifiers(baseAttackSpeed, StatType.AttackSpeed));
                combat.criticalRate = Mathf.Clamp01(ApplyModifiers(baseCriticalRate, StatType.CriticalRate));
                combat.criticalDamage = Mathf.Max(1f, ApplyModifiers(baseCriticalDamage, StatType.CriticalDamage));
            }

            if (health != null)
            {
                health.defense = Mathf.Max(0, ApplyModifiers(baseDefense, StatType.Defense));
                health.damageReduction = Mathf.Clamp01(ApplyModifiers(0, StatType.DamageReduction));
                health.healthRegen = ApplyModifiers(baseHealthRegen, StatType.HealthRegen);
                health.dodgeRate = Mathf.Clamp01(ApplyModifiers(baseDodgeRate, StatType.DodgeRate));
            }
        }

        public int ApplyAttackDamage(int baseDamage)
        {
            float value = ApplyModifiers(baseDamage, StatType.AttackDamage);
            return Mathf.Max(1, Mathf.RoundToInt(value));
        }

        public float ApplyAttackRange(float baseRange)
        {
            return Mathf.Max(0.1f, ApplyModifiers(baseRange, StatType.AttackRange));
        }

        public float ApplyAttackAngle(float baseAngle)
        {
            return Mathf.Clamp(ApplyModifiers(baseAngle, StatType.AttackAngle), 1f, 360f);
        }

        public float ApplyAttackKnockback(float baseKnockback)
        {
            return Mathf.Max(0f, ApplyModifiers(baseKnockback, StatType.AttackKnockback));
        }

        public float GetMusouGainMultiplier()
        {
            return Mathf.Max(0f, ApplyModifiers(1f, StatType.MusouGain));
        }

        public int ApplySkillDamage(int baseDamage)
        {
            float value = ApplyModifiers(baseDamage, StatType.SkillDamage);
            return Mathf.Max(1, Mathf.RoundToInt(value));
        }

        public float ApplySkillCooldown(float baseCooldown)
        {
            float value = ApplyModifiers(baseCooldown, StatType.SkillCooldown);
            return Mathf.Max(0.1f, value);
        }

        public float ApplySkillRange(float baseRange)
        {
            return Mathf.Max(0.1f, ApplyModifiers(baseRange, StatType.SkillRange));
        }

        public float ApplySkillKnockback(float baseKnockback)
        {
            return Mathf.Max(0f, ApplyModifiers(baseKnockback, StatType.SkillKnockback));
        }

        public float ApplySkillStaminaCost(float baseCost)
        {
            return Mathf.Max(0f, ApplyModifiers(baseCost, StatType.SkillStaminaCost));
        }

        public float ApplyLifeSteal()
        {
            return Mathf.Max(0f, ApplyModifiers(0f, StatType.LifeSteal));
        }

        public float ApplyBossDamage()
        {
            return Mathf.Max(1f, ApplyModifiers(1f, StatType.BossDamage));
        }

        public float ApplyComboDamage()
        {
            return Mathf.Max(1f, ApplyModifiers(1f, StatType.ComboDamage));
        }

        public float ApplyBerserkDuration(float baseDuration)
        {
            return Mathf.Max(0.1f, ApplyModifiers(baseDuration, StatType.BerserkDuration));
        }

        public float ApplyPotionEffect()
        {
            return Mathf.Max(1f, ApplyModifiers(1f, StatType.PotionEffect));
        }

        public float ApplyStatusResistance()
        {
            return Mathf.Max(0f, ApplyModifiers(0f, StatType.StatusResistance));
        }

        public int GetExtraLives()
        {
            return Mathf.Max(0, Mathf.RoundToInt(ApplyModifiers(0f, StatType.ExtraLife)));
        }

        public bool HasSecondWind()
        {
            return ApplyModifiers(0f, StatType.SecondWind) > 0.5f;
        }

        public float ApplySprintDistance()
        {
            return Mathf.Max(1f, ApplyModifiers(1f, StatType.SprintDistance));
        }

        public float ApplyDodgeDistance()
        {
            return Mathf.Max(1f, ApplyModifiers(baseDodgeDistance, StatType.DodgeDistance));
        }

        public float ApplyDodgeInvincibility()
        {
            return Mathf.Max(0.05f, ApplyModifiers(baseDodgeInvincibility, StatType.DodgeInvincibility));
        }

        private float ApplyModifiers(float baseValue, StatType stat)
        {
            float flat = 0f;
            float percent = 0f;

            for (int i = 0; i < cachedModifiers.Count; i++)
            {
                StatModifier modifier = cachedModifiers[i];
                if (modifier.stat != stat)
                {
                    continue;
                }

                if (modifier.type == ModifierType.Flat)
                {
                    flat += modifier.value;
                }
                else
                {
                    percent += modifier.value;
                }
            }

            float value = baseValue + flat;
            value *= 1f + percent;
            return value;
        }
    }
}
