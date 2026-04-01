using System;
using UnityEngine;

namespace ThirdPersonController
{
    public enum CombatFeedbackEventId
    {
        EnemyHitFlinch,
        EnemyHitKnockback,
        EnemyHitKnockdown,
        EnemyKilled,
        BerserkStart,
        BossBreakWindowStart,
        SkillUsed,
        StaminaDepleted
    }

    [Serializable]
    public class CombatFeedbackAudioRoute
    {
        public CombatFeedbackEventId eventId = CombatFeedbackEventId.EnemyHitFlinch;
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 2f)] public float volume = 1f;
        [Range(0.5f, 2f)] public float pitch = 1f;
        public AudioEventPriority priority = AudioEventPriority.Normal;
        public bool playAtEventPosition = true;
    }

    [Serializable]
    public class CombatFeedbackVfxRoute
    {
        public CombatFeedbackEventId eventId = CombatFeedbackEventId.EnemyHitFlinch;
        public bool shakeCamera = true;
        [Min(0f)] public float shakeDuration = 0.1f;
        [Min(0f)] public float shakeStrength = 0.1f;
        [Min(0)] public int shakeVibrato = 8;
        public bool flashOverlay = false;
        public Color flashColor = new Color(1f, 1f, 1f, 0.15f);
        [Min(0f)] public float flashDuration = 0.12f;
    }
}
