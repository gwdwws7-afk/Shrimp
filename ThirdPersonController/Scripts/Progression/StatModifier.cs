using System;
using UnityEngine;

namespace ThirdPersonController
{
    public enum StatType
    {
        AttackDamage,
        AttackRange,
        AttackAngle,
        AttackKnockback,
        MaxHealth,
        MaxStamina,
        MoveSpeed,
        MusouGain,
        SkillDamage,
        SkillCooldown,
        SkillRange,
        SkillKnockback,
        SkillStaminaCost
    }

    public enum ModifierType
    {
        Flat,
        Percent
    }

    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModifierType type;
        public float value;
    }
}
