using UnityEngine;

namespace ThirdPersonController
{
    public enum DamageElementType
    {
        Physical,
        Heat,
        Electric,
        Toxin,
        Corrosion
    }

    public enum DamageCategory
    {
        Light,
        Heavy,
        Skill,
        Ultimate
    }

    public enum DamageSourceType
    {
        PlayerAttack,
        PlayerSkill,
        Enemy,
        Environment
    }

    public struct DamageContext
    {
        public Transform source;
        public DamageSourceType sourceType;
        public int damage;
        public SkillCategory skillCategory;
        public DamageElementType elementType;
        public DamageCategory category;
        public float knockback;
        public float breakValue;
        public Vector3 damageOrigin;
        public Vector3 hitPoint;
        public bool hasHitPoint;
        public bool isCritical;
        public bool showDamageText;
        public float hitStopDuration;
        public bool isHeavyAttack;
    }

    public static class DamageService
    {
        public static bool ApplyDamage(DamageContext context, Collider target)
        {
            if (target == null || context.damage <= 0)
            {
                return false;
            }

            EnemyHealth enemyHealth = ResolveEnemyHealth(target);
            if (enemyHealth == null || enemyHealth.IsDead)
            {
                return false;
            }

            BossCombatTemplate bossTemplate = enemyHealth.GetComponent<BossCombatTemplate>();
            if (bossTemplate != null && context.breakValue > 0f)
            {
                bossTemplate.RegisterBreakValue(context.breakValue);
            }

            BossController bossController = enemyHealth.GetComponent<BossController>();
            if (bossController != null && context.breakValue > 0f)
            {
                bossController.RegisterBreakValue(context.breakValue);
            }

            int damageAfterElement = ApplyElementResistance(context.damage, context.elementType, enemyHealth);
            int damageAfterSkillTuning = ApplySkillVsEnemyDamageTuning(damageAfterElement, context, enemyHealth);
            int damageWithBossModifiers = ApplyBossModifiers(damageAfterSkillTuning, context, enemyHealth);
            int finalDamage = ApplyDefense(damageWithBossModifiers, enemyHealth.defense, context.sourceType);
            if (finalDamage <= 0)
            {
                return false;
            }

            float finalKnockback = ApplySkillVsEnemyKnockbackTuning(context.knockback, context, enemyHealth);
            int beforeHealth = enemyHealth.CurrentHealth;
            enemyHealth.RegisterDamageSource(context.sourceType, context.isHeavyAttack);
            enemyHealth.TakeDamage(finalDamage, context.damageOrigin, finalKnockback);
            if (enemyHealth.CurrentHealth >= beforeHealth)
            {
                return false;
            }

            if (IsPlayerSource(context.sourceType))
            {
                Vector3 position = context.hasHitPoint ? context.hitPoint : target.bounds.center;
                GameEvents.DamageDealt(finalDamage, position, context.isCritical);
                if (context.showDamageText)
                {
                    GameEvents.ShowDamageText(finalDamage, position, context.isCritical);
                }

                if (context.source != null)
                {
                    PlayerCombat combat = context.source.GetComponent<PlayerCombat>();
                    if (combat == null)
                    {
                        combat = context.source.GetComponentInParent<PlayerCombat>();
                    }

                    if (combat != null)
                    {
                        combat.RegisterHit(finalDamage);
                    }
                }
            }

            if (context.hitStopDuration > 0f)
            {
                HitStopManager.Trigger(context.hitStopDuration);
            }

            return true;
        }

        private static int ApplySkillVsEnemyDamageTuning(int damage, DamageContext context, EnemyHealth enemyHealth)
        {
            if (damage <= 0 || enemyHealth == null || context.sourceType != DamageSourceType.PlayerSkill)
            {
                return damage;
            }

            float multiplier = SkillEnemyInteractionTuning.GetDamageMultiplier(
                context.skillCategory,
                context.category,
                enemyHealth.enemyType);

            if (Mathf.Approximately(multiplier, 1f))
            {
                return damage;
            }

            return Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
        }

        private static float ApplySkillVsEnemyKnockbackTuning(float knockback, DamageContext context, EnemyHealth enemyHealth)
        {
            if (knockback <= 0f || enemyHealth == null || context.sourceType != DamageSourceType.PlayerSkill)
            {
                return knockback;
            }

            float multiplier = SkillEnemyInteractionTuning.GetKnockbackMultiplier(
                context.skillCategory,
                enemyHealth.enemyType);

            return Mathf.Max(0f, knockback * multiplier);
        }

        private static int ApplyElementResistance(int damage, DamageElementType elementType, EnemyHealth enemyHealth)
        {
            if (damage <= 0 || enemyHealth == null)
            {
                return damage;
            }

            float resistance = enemyHealth.GetResistance(elementType);
            resistance = Mathf.Clamp(resistance, -1f, 1f);
            float multiplier = 1f - resistance;
            return Mathf.RoundToInt(damage * multiplier);
        }

        private static int ApplyBossModifiers(int damage, DamageContext context, EnemyHealth enemyHealth)
        {
            if (enemyHealth == null)
            {
                return damage;
            }

            float multiplier = 1f;
            BossCombatTemplate boss = enemyHealth.GetComponent<BossCombatTemplate>();
            if (boss != null)
            {
                if (boss.IsBreakWindowActive)
                {
                    multiplier *= boss.breakWindowDamageMultiplier;
                }

                if (context.hasHitPoint && boss.IsWeakPointHit(context.hitPoint))
                {
                    multiplier *= boss.weakPointDamageMultiplier;
                }
            }

            BossController bossController = enemyHealth.GetComponent<BossController>();
            if (bossController != null)
            {
                if (bossController.IsBreakWindowActive)
                {
                    multiplier *= bossController.breakWindowDamageMultiplier;
                }

                if (bossController.IsWeaknessElement(context.elementType))
                {
                    multiplier *= bossController.GetWeaknessMultiplier();
                }
            }

            if (multiplier <= 1f)
            {
                return damage;
            }

            return Mathf.RoundToInt(damage * multiplier);
        }

        private static int ApplyDefense(int damage, float defense, DamageSourceType sourceType)
        {
            if (damage <= 0)
            {
                return damage;
            }

            if (defense <= 0f)
            {
                return damage;
            }

            int mitigated = Mathf.RoundToInt(damage - Mathf.Max(0f, defense));
            if (mitigated <= 0 && IsPlayerSource(sourceType))
            {
                return 1;
            }

            return Mathf.Max(0, mitigated);
        }

        private static EnemyHealth ResolveEnemyHealth(Collider target)
        {
            if (target == null)
            {
                return null;
            }

            EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                return enemyHealth;
            }

            enemyHealth = target.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                return enemyHealth;
            }

            if (target.attachedRigidbody != null)
            {
                return target.attachedRigidbody.GetComponent<EnemyHealth>();
            }

            return null;
        }

        private static bool IsPlayerSource(DamageSourceType sourceType)
        {
            return sourceType == DamageSourceType.PlayerAttack
                || sourceType == DamageSourceType.PlayerSkill;
        }
    }
}
