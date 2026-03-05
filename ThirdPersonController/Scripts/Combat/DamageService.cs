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

            EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
            if (enemyHealth == null || enemyHealth.IsDead)
            {
                return false;
            }

            BossCombatTemplate bossTemplate = enemyHealth.GetComponent<BossCombatTemplate>();
            if (bossTemplate != null && context.breakValue > 0f)
            {
                bossTemplate.RegisterBreakValue(context.breakValue);
            }

            int damageAfterElement = ApplyElementResistance(context.damage, context.elementType, enemyHealth);
            int damageWithBossModifiers = ApplyBossModifiers(damageAfterElement, context, enemyHealth);
            int finalDamage = ApplyDefense(damageWithBossModifiers, enemyHealth.defense, context.sourceType);
            if (finalDamage <= 0)
            {
                return false;
            }

            int beforeHealth = enemyHealth.CurrentHealth;
            enemyHealth.RegisterDamageSource(context.sourceType, context.isHeavyAttack);
            enemyHealth.TakeDamage(finalDamage, context.damageOrigin, context.knockback);
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

            BossCombatTemplate boss = enemyHealth.GetComponent<BossCombatTemplate>();
            if (boss == null)
            {
                return damage;
            }

            float multiplier = 1f;
            if (boss.IsBreakWindowActive)
            {
                multiplier *= boss.breakWindowDamageMultiplier;
            }

            if (context.hasHitPoint && boss.IsWeakPointHit(context.hitPoint))
            {
                multiplier *= boss.weakPointDamageMultiplier;
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

        private static bool IsPlayerSource(DamageSourceType sourceType)
        {
            return sourceType == DamageSourceType.PlayerAttack
                || sourceType == DamageSourceType.PlayerSkill;
        }
    }
}
