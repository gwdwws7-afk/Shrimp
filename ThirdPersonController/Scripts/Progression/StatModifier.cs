using System;
using UnityEngine;

namespace ThirdPersonController
{
    public enum StatType
    {
        // 攻击相关
        AttackDamage,
        AttackRange,
        AttackAngle,
        AttackKnockback,
        AttackSpeed,
        CriticalRate,
        CriticalDamage,
        
        // 防御相关
        MaxHealth,
        MaxStamina,
        Defense,
        DamageReduction,
        DodgeRate,
        HealthRegen,
        
        // 移动相关
        MoveSpeed,
        SprintDistance,
        DodgeDistance,
        DodgeInvincibility,
        
        // 无双相关
        MusouGain,
        
        // 技能相关
        SkillDamage,
        SkillCooldown,
        SkillRange,
        SkillKnockback,
        SkillStaminaCost,
        
        // 特殊效果
        LifeSteal,
        BossDamage,
        ComboDamage,
        BerserkDuration,
        PotionEffect,
        StatusResistance,
        ExtraLife,
        SecondWind,

        // 元素倾向（用于攻击/技能元素）
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
