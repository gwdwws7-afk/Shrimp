using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(fileName = "DamageCurveProfile", menuName = "Combat/Damage Curve Profile")]
    public class DamageCurveProfile : ScriptableObject
    {
        [Header("Damage Curve")]
        public float minDamageReference = 10f;
        public float maxDamageReference = 60f;
        public AnimationCurve damageMultiplierCurve = AnimationCurve.EaseInOut(0f, 0.85f, 1f, 1.2f);
        public float minDamageMultiplier = 0.7f;
        public float maxDamageMultiplier = 1.6f;

        [Header("Knockback Curve")]
        public AnimationCurve knockbackMultiplierCurve = AnimationCurve.EaseInOut(0f, 0.8f, 1f, 1.3f);
        public float minKnockbackMultiplier = 0.7f;
        public float maxKnockbackMultiplier = 1.5f;

        public float GetDamageMultiplier(int baseDamage)
        {
            float t = GetNormalizedDamage(baseDamage);
            float multiplier = damageMultiplierCurve.Evaluate(t);
            return Mathf.Clamp(multiplier, minDamageMultiplier, maxDamageMultiplier);
        }

        public float GetKnockbackMultiplier(int baseDamage)
        {
            float t = GetNormalizedDamage(baseDamage);
            float multiplier = knockbackMultiplierCurve.Evaluate(t);
            return Mathf.Clamp(multiplier, minKnockbackMultiplier, maxKnockbackMultiplier);
        }

        private float GetNormalizedDamage(int baseDamage)
        {
            if (maxDamageReference <= minDamageReference)
            {
                return 0f;
            }

            return Mathf.Clamp01(Mathf.InverseLerp(minDamageReference, maxDamageReference, baseDamage));
        }

        public static DamageCurveProfile GetDefaultProfile()
        {
            return Resources.Load<DamageCurveProfile>("DefaultDamageCurveProfile");
        }
    }
}
