using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(fileName = "EnemyHitReactionProfile", menuName = "Combat/Enemy Hit Reaction Profile")]
    public class EnemyHitReactionProfile : ScriptableObject
    {
        [Header("Thresholds")]
        public float knockbackThreshold = 2f;
        public float knockdownThreshold = 6f;

        [Header("Flinch")]
        public float flinchDuration = 0.2f;

        [Header("Knockback")]
        public float knockbackDuration = 0.25f;
        public float knockbackDistance = 1.2f;

        [Header("Knockdown")]
        public float knockdownDuration = 0.35f;
        public float knockdownDistance = 2.5f;
        public float knockdownRecoverTime = 0.6f;
        public float knockdownLift = 0f;

        public static EnemyHitReactionProfile GetDefaultProfile()
        {
            return Resources.Load<EnemyHitReactionProfile>("DefaultHitReactionProfile");
        }
    }
}
