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
        private int baseMaxHealth;
        private float baseMaxStamina;
        private float baseWalkSpeed;
        private float baseSprintSpeed;
        private float baseCrouchSpeed;

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
            }

            if (health != null)
            {
                baseMaxHealth = health.maxHealth;
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
