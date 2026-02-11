using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(menuName = "Combat/Attack Combo Definition", fileName = "AttackComboDefinition")]
    public class AttackComboDefinition : ScriptableObject
    {
        [Header("Combo Settings")]
        public float comboResetTime = 1.5f;
        public int maxComboCount = 50;

        [Header("Input Buffer")]
        public float inputBufferTime = 0.2f;

        [Header("Attack Steps")]
        public List<AttackStep> steps = new List<AttackStep>();

        public bool HasStep(int index)
        {
            return index >= 0 && index < steps.Count;
        }

        public AttackStep GetStep(int index)
        {
            if (!HasStep(index))
            {
                return null;
            }

            return steps[index];
        }
    }

    [System.Serializable]
    public class AttackStep
    {
        public string name = "N1";
        [Tooltip("Animator combo index to set when this step starts.")]
        public int animationComboIndex = 1;

        [Header("Damage")]
        public int baseDamage = 25;
        public float damageMultiplier = 1f;
        public float knockback = 5f;

        [Header("Hit Shape")]
        public float range = 2f;
        public float angle = 120f;
        public float radius = 1f;

        [Header("Timing")]
        public float hitDelay = 0.15f;
        public float recoveryTime = 0.35f;
        public float comboWindowStart = 0.15f;
        public float comboWindowEnd = 0.6f;

        [Header("Stamina")]
        public float staminaCost = 0f;

        [Header("Cancel Rules")]
        public bool allowDodgeCancel = true;
        public bool allowBlockCancel = true;

        [Header("Requirements")]
        public bool requireGrounded = true;

        [Header("Combo")]
        public int nextStepIndex = -1;
    }
}
