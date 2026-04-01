using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(
        fileName = "BossEncounterProfile",
        menuName = "ThirdPersonController/Boss/Boss Encounter Profile")]
    public class BossEncounterProfile : ScriptableObject
    {
        [Header("Identity")]
        public string bossDisplayName = string.Empty;
        public string bossIdentityId = string.Empty;
        [TextArea(2, 4)] public string roleFantasy = string.Empty;
        [TextArea(2, 4)] public string counterPlayHint = string.Empty;
        [TextArea(2, 4)] public string failLearningHint = string.Empty;
        [Min(1)] public int expectedPhaseCount = 2;
        [Min(1)] public int expectedSkillCount = 3;
        public bool definesBreakRule = true;
        public bool requireDistinctPhaseIntents = true;
        [TextArea(2, 4)] public string phase1Intent = "Establish baseline pressure and telegraph core threats.";
        [TextArea(2, 4)] public string phase2Intent = "Escalate pressure with tighter punish windows and combo threat.";
        [TextArea(2, 4)] public string phase3Intent = "Force high-clarity clutch decisions under time pressure.";
        [TextArea(2, 4)] public string signaturePunishWindow = "Punish after guarded opener recovery or interrupted chain end.";
        [TextArea(2, 4)] public string antiPatternHint = "Do not over-commit during armored startup or chain continuation.";

        [Header("Spawn Stats")]
        public bool overrideSpawnStats = true;
        public int maxHealth = 3000;
        public int expReward = 300;
        public int baseDamage = 25;
        public float knockback = 6f;

        [Header("Encounter Tuning")]
        public bool overrideEncounterTuning = true;
        public float phase2HealthThreshold = 0.66f;
        public float phase3HealthThreshold = 0.33f;
        public float breakWindowDuration = 4f;
        public float breakWindowCooldown = 12f;
        public float breakWindowDamageMultiplier = 1.6f;
        public float staggerMax = 120f;
        public float staggerPerDamage = 1f;
        public float attackInterval = 3.2f;
        public float decisionInterval = 0.78f;
        public int queuedAttackLimit = 3;
        public float immediateRepeatPenalty = 0.32f;
        public bool enablePostBreakPunishWindow = true;
        public float postBreakPunishDuration = 5f;
        public float postBreakAttackIntervalMultiplier = 0.75f;
        public float postBreakDecisionIntervalMultiplier = 0.82f;
        public float postBreakChaseSpeedMultiplier = 1.15f;
        public bool enablePhaseComboChain = true;
        public float phase2ComboChance = 0.45f;
        public float phase3ComboChance = 0.65f;
        public float comboStartDelay = 0.08f;
        public float comboRepeatPenalty = 0.35f;
        public bool enableInterruptRecoveryGate = true;
        public float interruptRecoveryDuration = 0.2f;
        public float interruptedAttackCooldownScale = 0.45f;
        public bool enableTimePressure = true;
        public float timePressureDelay = 75f;
        public float timePressureRampDuration = 60f;
        public float maxTimePressureDamageMultiplier = 1.35f;
        public float maxTimePressureSpeedMultiplier = 1.2f;
        public bool enablePhaseTransitionOpeners = true;
        public string phase2TransitionOpenerId = "";
        public string phase3TransitionOpenerId = "";
        public bool enablePhaseTransitionOpenerRetry = true;
        public float phaseTransitionOpenerRetryDelay = 0.12f;
        public int phaseTransitionOpenerMaxRetries = 3;
        public bool enablePhaseTransitionFollowupChain = false;
        public string phase2TransitionFollowupId = "";
        public string phase3TransitionFollowupId = "";
        public bool enablePhaseTransitionFollowupRetry = true;
        public float phaseTransitionFollowupRetryDelay = 0.12f;
        public int phaseTransitionFollowupMaxRetries = 2;
        public bool enablePhase3SpecialPriorityWindow = true;
        public float phase3SpecialPriorityDuration = 6f;
        public float phase3SpecialPriorityWeightMultiplier = 1.7f;
        public bool forceSpecialQueueDuringPhase3Priority = true;
        public bool enablePhaseIntentStyle = true;
        public BossPhaseIntentStyle phase1IntentStyle = BossPhaseIntentStyle.Balanced;
        public BossPhaseIntentStyle phase2IntentStyle = BossPhaseIntentStyle.PressureClose;
        public BossPhaseIntentStyle phase3IntentStyle = BossPhaseIntentStyle.SpecialBurst;
        public float closeRangeIntentThreshold = 4f;
        public float intentCloseWeightBoost = 1.45f;
        public float intentRangedWeightBoost = 1.35f;
        public float intentAoeWeightBoost = 1.25f;
        public float intentSpecialWeightBoost = 1.55f;
        public float intentFastDecisionMultiplier = 0.88f;
        public float intentSlowDecisionMultiplier = 1.14f;

        public void ApplyTo(BossSpawnPoint spawnPoint)
        {
            if (spawnPoint == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(bossDisplayName))
            {
                spawnPoint.bossName = bossDisplayName;
            }

            if (overrideSpawnStats)
            {
                spawnPoint.maxHealth = Mathf.Max(1, maxHealth);
                spawnPoint.expReward = Mathf.Max(0, expReward);
                spawnPoint.baseDamage = Mathf.Max(1, baseDamage);
                spawnPoint.knockback = Mathf.Max(0f, knockback);
            }

            spawnPoint.overrideEncounterTuning = overrideEncounterTuning;
            if (!overrideEncounterTuning)
            {
                return;
            }

            spawnPoint.phase2HealthThreshold = phase2HealthThreshold;
            spawnPoint.phase3HealthThreshold = phase3HealthThreshold;
            spawnPoint.breakWindowDuration = breakWindowDuration;
            spawnPoint.breakWindowCooldown = breakWindowCooldown;
            spawnPoint.breakWindowDamageMultiplier = breakWindowDamageMultiplier;
            spawnPoint.staggerMax = staggerMax;
            spawnPoint.staggerPerDamage = staggerPerDamage;
            spawnPoint.attackInterval = attackInterval;
            spawnPoint.decisionInterval = decisionInterval;
            spawnPoint.queuedAttackLimit = queuedAttackLimit;
            spawnPoint.immediateRepeatPenalty = immediateRepeatPenalty;
            spawnPoint.enablePostBreakPunishWindow = enablePostBreakPunishWindow;
            spawnPoint.postBreakPunishDuration = postBreakPunishDuration;
            spawnPoint.postBreakAttackIntervalMultiplier = postBreakAttackIntervalMultiplier;
            spawnPoint.postBreakDecisionIntervalMultiplier = postBreakDecisionIntervalMultiplier;
            spawnPoint.postBreakChaseSpeedMultiplier = postBreakChaseSpeedMultiplier;
            spawnPoint.enablePhaseComboChain = enablePhaseComboChain;
            spawnPoint.phase2ComboChance = phase2ComboChance;
            spawnPoint.phase3ComboChance = phase3ComboChance;
            spawnPoint.comboStartDelay = comboStartDelay;
            spawnPoint.comboRepeatPenalty = comboRepeatPenalty;
            spawnPoint.enableInterruptRecoveryGate = enableInterruptRecoveryGate;
            spawnPoint.interruptRecoveryDuration = interruptRecoveryDuration;
            spawnPoint.interruptedAttackCooldownScale = interruptedAttackCooldownScale;
            spawnPoint.enableTimePressure = enableTimePressure;
            spawnPoint.timePressureDelay = timePressureDelay;
            spawnPoint.timePressureRampDuration = timePressureRampDuration;
            spawnPoint.maxTimePressureDamageMultiplier = maxTimePressureDamageMultiplier;
            spawnPoint.maxTimePressureSpeedMultiplier = maxTimePressureSpeedMultiplier;
            spawnPoint.enablePhaseTransitionOpeners = enablePhaseTransitionOpeners;
            spawnPoint.phase2TransitionOpenerId = phase2TransitionOpenerId ?? string.Empty;
            spawnPoint.phase3TransitionOpenerId = phase3TransitionOpenerId ?? string.Empty;
            spawnPoint.enablePhaseTransitionOpenerRetry = enablePhaseTransitionOpenerRetry;
            spawnPoint.phaseTransitionOpenerRetryDelay = phaseTransitionOpenerRetryDelay;
            spawnPoint.phaseTransitionOpenerMaxRetries = phaseTransitionOpenerMaxRetries;
            spawnPoint.enablePhaseTransitionFollowupChain = enablePhaseTransitionFollowupChain;
            spawnPoint.phase2TransitionFollowupId = phase2TransitionFollowupId ?? string.Empty;
            spawnPoint.phase3TransitionFollowupId = phase3TransitionFollowupId ?? string.Empty;
            spawnPoint.enablePhaseTransitionFollowupRetry = enablePhaseTransitionFollowupRetry;
            spawnPoint.phaseTransitionFollowupRetryDelay = phaseTransitionFollowupRetryDelay;
            spawnPoint.phaseTransitionFollowupMaxRetries = phaseTransitionFollowupMaxRetries;
            spawnPoint.enablePhase3SpecialPriorityWindow = enablePhase3SpecialPriorityWindow;
            spawnPoint.phase3SpecialPriorityDuration = phase3SpecialPriorityDuration;
            spawnPoint.phase3SpecialPriorityWeightMultiplier = phase3SpecialPriorityWeightMultiplier;
            spawnPoint.forceSpecialQueueDuringPhase3Priority = forceSpecialQueueDuringPhase3Priority;
            spawnPoint.enablePhaseIntentStyle = enablePhaseIntentStyle;
            spawnPoint.phase1IntentStyle = phase1IntentStyle;
            spawnPoint.phase2IntentStyle = phase2IntentStyle;
            spawnPoint.phase3IntentStyle = phase3IntentStyle;
            spawnPoint.closeRangeIntentThreshold = closeRangeIntentThreshold;
            spawnPoint.intentCloseWeightBoost = intentCloseWeightBoost;
            spawnPoint.intentRangedWeightBoost = intentRangedWeightBoost;
            spawnPoint.intentAoeWeightBoost = intentAoeWeightBoost;
            spawnPoint.intentSpecialWeightBoost = intentSpecialWeightBoost;
            spawnPoint.intentFastDecisionMultiplier = intentFastDecisionMultiplier;
            spawnPoint.intentSlowDecisionMultiplier = intentSlowDecisionMultiplier;
        }
    }
}
