using UnityEngine;

namespace ThirdPersonController
{
    public static class SkillEnemyInteractionTuning
    {
        public static float GetDamageMultiplier(
            SkillCategory skillCategory,
            DamageCategory damageCategory,
            EnemyType enemyType)
        {
            float multiplier = 1f;
            switch (skillCategory)
            {
                case SkillCategory.CrowdControl:
                    multiplier = enemyType switch
                    {
                        EnemyType.Grunt => 1.08f,
                        EnemyType.Rusher => 1.15f,
                        EnemyType.Tank => 0.82f,
                        EnemyType.Elite => 0.92f,
                        EnemyType.Mutant => 1.0f,
                        EnemyType.Boss => 0.7f,
                        _ => 1f
                    };
                    break;
                case SkillCategory.Burst:
                    multiplier = enemyType switch
                    {
                        EnemyType.Grunt => 1.0f,
                        EnemyType.Rusher => 1.05f,
                        EnemyType.Tank => 1.1f,
                        EnemyType.Elite => 1.08f,
                        EnemyType.Mutant => 1.0f,
                        EnemyType.Boss => 0.9f,
                        _ => 1f
                    };
                    break;
                case SkillCategory.Mobility:
                    multiplier = enemyType switch
                    {
                        EnemyType.Grunt => 1.0f,
                        EnemyType.Rusher => 1.12f,
                        EnemyType.Tank => 0.88f,
                        EnemyType.Elite => 0.95f,
                        EnemyType.Mutant => 1.0f,
                        EnemyType.Boss => 0.8f,
                        _ => 1f
                    };
                    break;
                case SkillCategory.Gather:
                    multiplier = enemyType switch
                    {
                        EnemyType.Grunt => 1.05f,
                        EnemyType.Rusher => 1.1f,
                        EnemyType.Tank => 0.9f,
                        EnemyType.Elite => 0.94f,
                        EnemyType.Mutant => 1.0f,
                        EnemyType.Boss => 0.75f,
                        _ => 1f
                    };
                    break;
            }

            if (damageCategory == DamageCategory.Ultimate)
            {
                float ultimateScale = enemyType switch
                {
                    EnemyType.Grunt => 1.02f,
                    EnemyType.Rusher => 1.02f,
                    EnemyType.Tank => 1.06f,
                    EnemyType.Elite => 1.06f,
                    EnemyType.Mutant => 1.04f,
                    EnemyType.Boss => 1f,
                    _ => 1f
                };
                multiplier *= ultimateScale;
            }

            return Mathf.Max(0.1f, multiplier);
        }

        public static float GetKnockbackMultiplier(SkillCategory skillCategory, EnemyType enemyType)
        {
            return skillCategory switch
            {
                SkillCategory.CrowdControl => enemyType switch
                {
                    EnemyType.Grunt => 1.1f,
                    EnemyType.Rusher => 1.2f,
                    EnemyType.Tank => 0.55f,
                    EnemyType.Elite => 0.75f,
                    EnemyType.Mutant => 0.85f,
                    EnemyType.Boss => 0.2f,
                    _ => 1f
                },
                SkillCategory.Burst => enemyType switch
                {
                    EnemyType.Grunt => 1f,
                    EnemyType.Rusher => 1.05f,
                    EnemyType.Tank => 0.7f,
                    EnemyType.Elite => 0.82f,
                    EnemyType.Mutant => 0.9f,
                    EnemyType.Boss => 0.25f,
                    _ => 1f
                },
                SkillCategory.Mobility => enemyType switch
                {
                    EnemyType.Grunt => 1f,
                    EnemyType.Rusher => 1.12f,
                    EnemyType.Tank => 0.66f,
                    EnemyType.Elite => 0.8f,
                    EnemyType.Mutant => 0.9f,
                    EnemyType.Boss => 0.22f,
                    _ => 1f
                },
                SkillCategory.Gather => enemyType switch
                {
                    EnemyType.Grunt => 1.12f,
                    EnemyType.Rusher => 1.2f,
                    EnemyType.Tank => 0.5f,
                    EnemyType.Elite => 0.68f,
                    EnemyType.Mutant => 0.82f,
                    EnemyType.Boss => 0.15f,
                    _ => 1f
                },
                _ => 1f
            };
        }

        public static float GetControlMultiplier(SkillCategory skillCategory, EnemyType enemyType)
        {
            return skillCategory switch
            {
                SkillCategory.CrowdControl => enemyType switch
                {
                    EnemyType.Grunt => 1.15f,
                    EnemyType.Rusher => 1.25f,
                    EnemyType.Tank => 0.55f,
                    EnemyType.Elite => 0.75f,
                    EnemyType.Mutant => 0.85f,
                    EnemyType.Boss => 0f,
                    _ => 1f
                },
                SkillCategory.Burst => enemyType switch
                {
                    EnemyType.Grunt => 1.0f,
                    EnemyType.Rusher => 1.05f,
                    EnemyType.Tank => 0.72f,
                    EnemyType.Elite => 0.86f,
                    EnemyType.Mutant => 0.9f,
                    EnemyType.Boss => 0f,
                    _ => 1f
                },
                SkillCategory.Gather => enemyType switch
                {
                    EnemyType.Grunt => 1.0f,
                    EnemyType.Rusher => 1.1f,
                    EnemyType.Tank => 0.65f,
                    EnemyType.Elite => 0.8f,
                    EnemyType.Mutant => 0.9f,
                    EnemyType.Boss => 0f,
                    _ => 1f
                },
                _ => 1f
            };
        }

        public static float GetDisplacementMultiplier(SkillCategory skillCategory, EnemyType enemyType)
        {
            if (skillCategory != SkillCategory.Gather)
            {
                return 1f;
            }

            return enemyType switch
            {
                EnemyType.Grunt => 1.15f,
                EnemyType.Rusher => 1.25f,
                EnemyType.Tank => 0.45f,
                EnemyType.Elite => 0.68f,
                EnemyType.Mutant => 0.8f,
                EnemyType.Boss => 0f,
                _ => 1f
            };
        }
    }
}
