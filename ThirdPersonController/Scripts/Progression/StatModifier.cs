using System;
using UnityEngine;

namespace ThirdPersonController
{
    public enum StatType
    {
// 围绕 伤害 执行该步骤，用于保持上下文语义一致。
        AttackDamage,
        AttackRange,
        AttackAngle,
        AttackKnockback,
        AttackSpeed,
        CriticalRate,
        CriticalDamage,
        
// 围绕 生命 执行该步骤，用于保持上下文语义一致。
        MaxHealth,
        MaxStamina,
        Defense,
        DamageReduction,
        DodgeRate,
        HealthRegen,
        
// 围绕 MoveSpeed 执行该步骤，用于保持上下文语义一致。
        MoveSpeed,
        SprintDistance,
        DodgeDistance,
        DodgeInvincibility,
        
// 围绕 无双 执行该步骤，用于保持上下文语义一致。
        MusouGain,
        
// 围绕 技能伤害 执行该步骤，用于保持上下文语义一致。
        SkillDamage,
        SkillCooldown,
        SkillRange,
        SkillKnockback,
        SkillStaminaCost,
        
// 围绕 LifeSteal 执行该步骤，用于保持上下文语义一致。
        LifeSteal,
        BossDamage,
        ComboDamage,
        BerserkDuration,
        PotionEffect,
        StatusResistance,
        ExtraLife,
        SecondWind,

// 围绕 AttackElementHeat 执行该步骤，用于保持上下文语义一致。
        AttackElementHeat,
        AttackElementElectric,
        AttackElementToxin,
        AttackElementCorrosion,
        SkillElementHeat,
        SkillElementElectric,
        SkillElementToxin,
        SkillElementCorrosion
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
