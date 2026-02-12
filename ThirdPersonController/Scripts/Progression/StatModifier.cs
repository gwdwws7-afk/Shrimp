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
        MusouGain
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
