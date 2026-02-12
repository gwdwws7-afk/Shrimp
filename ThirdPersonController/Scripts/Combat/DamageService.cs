using UnityEngine;

namespace ThirdPersonController
{
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
        public float knockback;
        public Vector3 damageOrigin;
        public Vector3 hitPoint;
        public bool hasHitPoint;
        public bool isCritical;
        public bool showDamageText;
        public float hitStopDuration;
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

            int beforeHealth = enemyHealth.CurrentHealth;
            enemyHealth.TakeDamage(context.damage, context.damageOrigin, context.knockback);
            if (enemyHealth.CurrentHealth >= beforeHealth)
            {
                return false;
            }

            if (IsPlayerSource(context.sourceType))
            {
                Vector3 position = context.hasHitPoint ? context.hitPoint : target.bounds.center;
                GameEvents.DamageDealt(context.damage, position, context.isCritical);
                if (context.showDamageText)
                {
                    GameEvents.ShowDamageText(context.damage, position, context.isCritical);
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
                        combat.RegisterHit(context.damage);
                    }
                }
            }

            if (context.hitStopDuration > 0f)
            {
                HitStopManager.Trigger(context.hitStopDuration);
            }

            return true;
        }

        private static bool IsPlayerSource(DamageSourceType sourceType)
        {
            return sourceType == DamageSourceType.PlayerAttack
                || sourceType == DamageSourceType.PlayerSkill;
        }
    }
}
