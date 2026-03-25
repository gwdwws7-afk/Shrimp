using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum BossPhaseType
    {
        Normal,
        Enraged,
        Desperate,
        Final
    }

    [Serializable]
    public class BossPhase
    {
        public string phaseName = "Phase";
        public float healthPercentThreshold = 1f;
        public float timeScale = 1f;

        [Header("Stats Multiplier")]
        public float damageMultiplier = 1f;
        public float speedMultiplier = 1f;
        public float defenseMultiplier = 1f;

        [Header("Abilities")]
        public bool unlockSpecialAttacks = false;
        public List<string> unlockedAttacks = new List<string>();

        [Header("Visual")]
        public Color phaseColor = Color.white;
        public ParticleSystem phaseEnterEffect;
        public AudioClip phaseEnterSound;
    }

    [Serializable]
    public class BossAttack
    {
        public string attackId = "";
        public string attackName = "Attack";
        public float damage = 50f;
        public float range = 5f;
        public float cooldown = 3f;
        public float windupTime = 0.5f;
        public float activeTime = 0.3f;
        public float recoveryTime = 0.5f;
        public float knockbackForce = 10f;
        [Min(0.01f)] public float selectionWeight = 1f;

        [Header("Targeting")]
        public bool targetPlayer = true;
        public bool aoe = false;
        public float aoeRadius = 3f;

        [Header("Special")]
        public bool isSpecial = false;
        public bool requiresPhase2 = false;
        public bool requiresPhase3 = false;
    }

    public class BossController : MonoBehaviour
    {
        private struct QueuedBossAttack
        {
            public BossAttack attack;
            public string key;
        }

        [Header("Configuration")]
        public int maxHealth = 5000;
        public int currentPhase = 1;
        public bool usePhases = true;

        [Header("Phases")]
        public List<BossPhase> phases = new List<BossPhase>();

        [Header("Attacks")]
        public List<BossAttack> attacks = new List<BossAttack>();
        public float attackInterval = 3.5f;
        public float decisionInterval = 0.8f;

        [Header("Attack Selection")]
        public bool weightAttacksByDistance = true;
        [Range(0f, 2f)] public float inRangeWeightMultiplier = 1.15f;
        [Range(0f, 1f)] public float outOfRangeWeightMultiplier = 0.35f;

        [Header("Post Break Punish")]
        public bool enablePostBreakPunishWindow = true;
        [Min(0f)] public float postBreakPunishDuration = 5f;
        [Range(0.3f, 1f)] public float postBreakAttackIntervalMultiplier = 0.75f;
        [Range(0.3f, 1f)] public float postBreakDecisionIntervalMultiplier = 0.82f;
        [Min(1f)] public float postBreakChaseSpeedMultiplier = 1.15f;

        [Header("Attack Queue")]
        public bool useAttackQueue = true;
        [Min(1)] public int queuedAttackLimit = 3;
        [Min(1)] public int maxSameAttackQueued = 1;
        [Range(0f, 1f)] public float immediateRepeatPenalty = 0.35f;
        public bool prioritizeSpecialAttacksWhenEnraged = true;
        public bool scaleDecisionIntervalWithTimePressure = true;
        [Range(0.3f, 1f)] public float minDecisionIntervalMultiplierAtMaxPressure = 0.72f;

        [Header("Phase Combo Chain")]
        public bool enablePhaseComboChain = true;
        [Range(0f, 1f)] public float phase2ComboChance = 0.45f;
        [Range(0f, 1f)] public float phase3ComboChance = 0.65f;
        [Min(0f)] public float comboStartDelay = 0.08f;
        [Range(0f, 1f)] public float comboRepeatPenalty = 0.35f;

        [Header("Interrupt Recovery")]
        public bool enableInterruptRecoveryGate = true;
        [Min(0f)] public float interruptRecoveryDuration = 0.2f;
        [Range(0f, 1f)] public float interruptedAttackCooldownScale = 0.45f;

        [Header("Encounter Choreography")]
        public bool enablePhaseTransitionOpeners = true;
        public string phase2TransitionOpenerId = "";
        public string phase3TransitionOpenerId = "";
        public bool enablePhaseTransitionOpenerRetry = true;
        [Min(0f)] public float phaseTransitionOpenerRetryDelay = 0.12f;
        [Min(0)] public int phaseTransitionOpenerMaxRetries = 3;
        public bool enablePhaseTransitionFollowupChain = false;
        public string phase2TransitionFollowupId = "";
        public string phase3TransitionFollowupId = "";
        public bool enablePhaseTransitionFollowupRetry = true;
        [Min(0f)] public float phaseTransitionFollowupRetryDelay = 0.12f;
        [Min(0)] public int phaseTransitionFollowupMaxRetries = 2;
        public bool enablePhase3SpecialPriorityWindow = true;
        [Min(0f)] public float phase3SpecialPriorityDuration = 6f;
        [Min(1f)] public float phase3SpecialPriorityWeightMultiplier = 1.7f;
        public bool forceSpecialQueueDuringPhase3Priority = true;

        [Header("Break Window")]
        public bool enableBreakWindow = true;
        [Min(1f)] public float staggerMax = 120f;
        [Min(0f)] public float staggerPerDamage = 1f;
        [Min(0f)] public float breakWindowDuration = 4f;
        [Min(0f)] public float breakWindowCooldown = 12f;
        [Min(1f)] public float breakWindowDamageMultiplier = 1.6f;
        public bool forceKnockdownDuringBreak = true;
        public bool allowHeavyKnockdownOutsideBreak = false;
        public string breakTrigger = "Break";

        [Header("Weakness")]
        public bool hasWeakness = false;
        public string weaknessElement = "";
        public float weaknessMultiplier = 2f;

        [Header("Time Pressure")]
        public bool enableTimePressure = true;
        [Min(0f)] public float timePressureDelay = 75f;
        [Min(1f)] public float timePressureRampDuration = 60f;
        [Min(1f)] public float maxTimePressureDamageMultiplier = 1.35f;
        [Min(1f)] public float maxTimePressureSpeedMultiplier = 1.2f;

        [Header("State")]
        public bool isEnraged = false;
        public bool isInAttack = false;
        public bool isVulnerable = false;

        [Header("References")]
        public EnemyHealth health;
        public EnemyAI ai;
        public Animator animator;

        [Header("Events")]
        public Action<int> OnPhaseChanged;
        public Action<BossAttack> OnAttackStarted;
        public Action OnBreakWindowStart;
        public Action OnBreakWindowEnd;
        public Action OnBossDefeated;

        [Header("Debug (Runtime)")]
        [SerializeField] private int debugQueuedAttackCount = 0;
        [SerializeField] private float debugStagger = 0f;
        [SerializeField] private bool debugBreakWindowActive = false;
        [SerializeField] private float debugTimePressure = 0f;
        [SerializeField] private float debugEffectiveDecisionInterval = 0f;
        [SerializeField] private float debugLastPlanningDistance = 0f;
        [SerializeField] private float debugPostBreakPunishFactor = 0f;
        [SerializeField] private bool debugLastComboTriggered = false;
        [SerializeField] private float debugInterruptRecoveryTimer = 0f;
        [SerializeField] private float debugComboStartDelayTimer = 0f;
        [SerializeField] private bool debugLastPhaseOpenerQueued = false;
        [SerializeField] private bool debugLastPhaseFollowupQueued = false;
        [SerializeField] private float debugPhase3SpecialPriorityTimer = 0f;
        [SerializeField] private float debugPhaseTransitionFollowupRetryTimer = 0f;

        public bool IsBreakWindowActive => breakWindowActive;
        public int QueuedAttackCount => plannedAttacks.Count;
        public float CurrentStagger => currentStagger;
        public bool DebugLastComboTriggered => debugLastComboTriggered;
        public float DebugInterruptRecoveryTimer => interruptRecoveryTimer;
        public bool DebugLastPhaseOpenerQueued => debugLastPhaseOpenerQueued;
        public bool DebugLastPhaseFollowupQueued => debugLastPhaseFollowupQueued;

        private readonly Queue<QueuedBossAttack> plannedAttacks = new Queue<QueuedBossAttack>();
        private readonly Dictionary<string, float> nextAttackReadyTime = new Dictionary<string, float>();

        private int currentPhaseIndex = 0;
        private float attackTimer = 0f;
        private float decisionTimer = 0f;
        private bool isDead = false;

        private float currentStagger = 0f;
        private float breakCooldownTimer = 0f;
        private float breakTimer = 0f;
        private bool breakWindowActive = false;
        private float stunTimer = 0f;

        private float baseChaseSpeed = 0f;
        private int baseAttackDamage = 0;
        private float baseDefense = 0f;
        private bool baseStatsCached = false;
        private float prePhaseTimeScale = 1f;
        private bool hasOverriddenTimeScale = false;
        private float currentPhaseDamageMultiplier = 1f;
        private float currentPhaseSpeedMultiplier = 1f;
        private float currentPhaseDefenseMultiplier = 1f;
        private float encounterElapsed = 0f;
        private float lastAppliedTimePressure = -1f;
        private float postBreakPunishTimer = 0f;
        private float interruptRecoveryTimer = 0f;
        private float comboStartDelayTimer = 0f;
        private float phase3SpecialPriorityTimer = 0f;
        private string activeAttackKey = string.Empty;
        private float activeAttackCooldown = 0f;
        private string pendingPhaseTransitionOpenerId = string.Empty;
        private float pendingPhaseTransitionOpenerRetryTimer = 0f;
        private int pendingPhaseTransitionOpenerRetriesLeft = 0;
        private int pendingPhaseTransitionOpenerPhaseIndex = -1;
        private string pendingPhaseTransitionFollowupId = string.Empty;
        private float pendingPhaseTransitionFollowupRetryTimer = 0f;
        private int pendingPhaseTransitionFollowupRetriesLeft = 0;

        private string lastQueuedAttackKey = string.Empty;
        private Coroutine runningAttackRoutine;

        private void Awake()
        {
            if (health == null) health = GetComponent<EnemyHealth>();
            if (ai == null) ai = GetComponent<EnemyAI>();
            if (animator == null) animator = GetComponent<Animator>();

            InitializePhases();
            CacheBaseStatsIfNeeded();
            currentPhaseIndex = Mathf.Clamp(currentPhase - 1, 0, Mathf.Max(0, phases.Count - 1));
            currentPhase = currentPhaseIndex + 1;
            ApplyPhaseStats(GetCurrentPhase());
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamageTaken += HandleDamageTaken;
                health.OnDeath += HandleDeath;
            }

            ResetRuntimeState();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamageTaken -= HandleDamageTaken;
                health.OnDeath -= HandleDeath;
            }

            CancelCurrentAttack(false, false);
            RestoreTimeScaleIfNeeded();
        }

        private void Update()
        {
            if (isDead)
            {
                return;
            }

            float delta = Time.deltaTime;
            encounterElapsed += delta;
            UpdateTimePressure();
            interruptRecoveryTimer = Mathf.Max(0f, interruptRecoveryTimer - Mathf.Max(0f, delta));
            comboStartDelayTimer = Mathf.Max(0f, comboStartDelayTimer - Mathf.Max(0f, delta));
            phase3SpecialPriorityTimer = Mathf.Max(0f, phase3SpecialPriorityTimer - Mathf.Max(0f, delta));
            if (!isInAttack)
            {
                attackTimer += delta;
            }

            if (breakCooldownTimer > 0f)
            {
                breakCooldownTimer -= delta;
            }

            UpdateBreakWindow(delta);
            if (breakWindowActive)
            {
                return;
            }

            UpdateStun(delta);
            if (stunTimer > 0f)
            {
                return;
            }

            UpdatePostBreakPunish(delta);
            UpdatePhase();
            UpdateAttackPlanning(delta);
            UpdatePhaseTransitionOpenerRetry(delta);
            UpdatePhaseTransitionFollowupRetry(delta);
            TryStartPlannedAttack();
            SyncDebugState();
        }

        private void InitializePhases()
        {
            if (phases.Count == 0)
            {
                phases.Add(new BossPhase { phaseName = "Normal", healthPercentThreshold = 1f });
                phases.Add(new BossPhase { phaseName = "Enraged", healthPercentThreshold = 0.66f, unlockSpecialAttacks = true, damageMultiplier = 1.1f });
                phases.Add(new BossPhase { phaseName = "Desperate", healthPercentThreshold = 0.33f, unlockSpecialAttacks = true, damageMultiplier = 1.2f });
            }

            phases.Sort((a, b) => b.healthPercentThreshold.CompareTo(a.healthPercentThreshold));
        }

        private void CacheBaseStatsIfNeeded()
        {
            if (baseStatsCached)
            {
                return;
            }

            baseChaseSpeed = ai != null ? ai.chaseSpeed : 0f;
            baseAttackDamage = ai != null ? Mathf.Max(1, ai.attackDamage) : 1;
            baseDefense = health != null ? health.defense : 0f;
            baseStatsCached = true;
        }

        private void ResetRuntimeState()
        {
            isDead = false;
            isEnraged = currentPhaseIndex > 0;
            isInAttack = false;
            isVulnerable = false;
            breakWindowActive = false;
            currentStagger = 0f;
            breakTimer = 0f;
            breakCooldownTimer = 0f;
            stunTimer = 0f;
            attackTimer = Mathf.Max(attackInterval, 0f);
            decisionTimer = 0f;
            plannedAttacks.Clear();
            nextAttackReadyTime.Clear();
            lastQueuedAttackKey = string.Empty;
            runningAttackRoutine = null;
            encounterElapsed = 0f;
            lastAppliedTimePressure = -1f;
            postBreakPunishTimer = 0f;
            interruptRecoveryTimer = 0f;
            comboStartDelayTimer = 0f;
            phase3SpecialPriorityTimer = 0f;
            activeAttackKey = string.Empty;
            activeAttackCooldown = 0f;
            ClearPendingPhaseTransitionOpener();
            ClearPendingPhaseTransitionFollowup();
            debugLastComboTriggered = false;
            debugLastPhaseOpenerQueued = false;
            debugLastPhaseFollowupQueued = false;
            ApplyEffectiveStats(0f);
            SyncDebugState();
        }

        private void UpdatePhase()
        {
            if (!usePhases || health == null || phases.Count == 0)
            {
                return;
            }

            CheckPhaseTransition();
        }

        private void CheckPhaseTransition()
        {
            if (health == null || phases.Count == 0)
            {
                return;
            }

            float healthPercent = (float)health.CurrentHealth / Mathf.Max(1, health.MaxHealth);
            int targetPhaseIndex = currentPhaseIndex;
            for (int i = currentPhaseIndex + 1; i < phases.Count; i++)
            {
                if (healthPercent <= phases[i].healthPercentThreshold + 0.0001f)
                {
                    targetPhaseIndex = i;
                }
            }

            if (targetPhaseIndex > currentPhaseIndex)
            {
                TransitionToPhase(targetPhaseIndex);
            }
        }

        private void TransitionToPhase(int newPhaseIndex)
        {
            if (newPhaseIndex < 0 || newPhaseIndex >= phases.Count || newPhaseIndex == currentPhaseIndex)
            {
                return;
            }

            currentPhaseIndex = newPhaseIndex;
            currentPhase = newPhaseIndex + 1;
            isEnraged = currentPhaseIndex > 0;

            BossPhase phase = phases[newPhaseIndex];
            ApplyPhaseStats(phase);
            ApplyPhasePresentation(phase);

            plannedAttacks.Clear();
            ClearPendingPhaseTransitionOpener();
            ClearPendingPhaseTransitionFollowup();
            debugLastPhaseOpenerQueued = false;
            debugLastPhaseFollowupQueued = false;
            if (enablePhaseTransitionOpeners)
            {
                string openerId = ResolvePhaseTransitionOpenerId(newPhaseIndex);
                debugLastPhaseOpenerQueued = TryQueuePhaseTransitionOpenerById(openerId);
                if (debugLastPhaseOpenerQueued)
                {
                    debugLastPhaseFollowupQueued = TryQueuePhaseTransitionFollowupByPhaseIndex(newPhaseIndex);
                    if (!debugLastPhaseFollowupQueued)
                    {
                        SchedulePhaseTransitionFollowupRetry(newPhaseIndex);
                    }
                }
                else
                {
                    SchedulePhaseTransitionOpenerRetry(openerId, newPhaseIndex);
                }
            }

            if (enablePhase3SpecialPriorityWindow && newPhaseIndex >= 2)
            {
                phase3SpecialPriorityTimer = Mathf.Max(phase3SpecialPriorityTimer, Mathf.Max(0f, phase3SpecialPriorityDuration));
            }

            decisionTimer = Mathf.Max(decisionTimer, GetEffectiveDecisionInterval());
            if (debugLastPhaseOpenerQueued || debugLastPhaseFollowupQueued)
            {
                attackTimer = Mathf.Max(attackTimer, GetEffectiveAttackInterval());
            }

            OnPhaseChanged?.Invoke(currentPhase);
            GameEvents.ShowMessage(string.Format(Localize("boss.phase_format", "PHASE {0}: {1}!"), currentPhase, phase.phaseName), 2.5f);
        }

        private bool TryQueuePhaseTransitionOpener(int newPhaseIndex)
        {
            return TryQueuePhaseTransitionOpenerById(ResolvePhaseTransitionOpenerId(newPhaseIndex));
        }

        private string ResolvePhaseTransitionOpenerId(int newPhaseIndex)
        {
            string openerId = string.Empty;
            if (newPhaseIndex == 1)
            {
                openerId = phase2TransitionOpenerId;
            }
            else if (newPhaseIndex >= 2)
            {
                openerId = phase3TransitionOpenerId;
            }

            return openerId;
        }

        private bool TryQueuePhaseTransitionFollowupByPhaseIndex(int phaseIndex)
        {
            if (!enablePhaseTransitionFollowupChain)
            {
                return false;
            }

            return TryQueuePhaseTransitionFollowupById(ResolvePhaseTransitionFollowupId(phaseIndex));
        }

        private string ResolvePhaseTransitionFollowupId(int phaseIndex)
        {
            if (phaseIndex == 1)
            {
                return phase2TransitionFollowupId;
            }

            if (phaseIndex >= 2)
            {
                return phase3TransitionFollowupId;
            }

            return string.Empty;
        }

        private void SchedulePhaseTransitionOpenerRetry(string openerId, int phaseIndex)
        {
            if (!enablePhaseTransitionOpeners || !enablePhaseTransitionOpenerRetry || string.IsNullOrWhiteSpace(openerId))
            {
                return;
            }

            int retries = Mathf.Max(0, phaseTransitionOpenerMaxRetries);
            if (retries <= 0)
            {
                return;
            }

            pendingPhaseTransitionOpenerId = openerId;
            pendingPhaseTransitionOpenerRetriesLeft = retries;
            pendingPhaseTransitionOpenerRetryTimer = ResolvePhaseTransitionOpenerRetryDelay();
            pendingPhaseTransitionOpenerPhaseIndex = phaseIndex;
        }

        private void UpdatePhaseTransitionOpenerRetry(float deltaTime)
        {
            if (!enablePhaseTransitionOpeners || !enablePhaseTransitionOpenerRetry)
            {
                ClearPendingPhaseTransitionOpener();
                return;
            }

            if (string.IsNullOrWhiteSpace(pendingPhaseTransitionOpenerId) || pendingPhaseTransitionOpenerRetriesLeft <= 0)
            {
                return;
            }

            pendingPhaseTransitionOpenerRetryTimer -= Mathf.Max(0f, deltaTime);
            if (pendingPhaseTransitionOpenerRetryTimer > 0f)
            {
                return;
            }

            bool queued = TryQueuePhaseTransitionOpenerById(pendingPhaseTransitionOpenerId);
            if (queued)
            {
                int queuedPhaseIndex = pendingPhaseTransitionOpenerPhaseIndex;
                debugLastPhaseOpenerQueued = true;
                debugLastPhaseFollowupQueued = false;
                attackTimer = Mathf.Max(attackTimer, GetEffectiveAttackInterval());
                ClearPendingPhaseTransitionOpener();
                debugLastPhaseFollowupQueued = TryQueuePhaseTransitionFollowupByPhaseIndex(queuedPhaseIndex);
                if (!debugLastPhaseFollowupQueued)
                {
                    SchedulePhaseTransitionFollowupRetry(queuedPhaseIndex);
                }
                SyncDebugState();
                return;
            }

            pendingPhaseTransitionOpenerRetriesLeft--;
            if (pendingPhaseTransitionOpenerRetriesLeft <= 0)
            {
                ClearPendingPhaseTransitionOpener();
                return;
            }

            pendingPhaseTransitionOpenerRetryTimer = ResolvePhaseTransitionOpenerRetryDelay();
        }

        private float ResolvePhaseTransitionOpenerRetryDelay()
        {
            if (phaseTransitionOpenerRetryDelay > 0f)
            {
                return phaseTransitionOpenerRetryDelay;
            }

            return Mathf.Max(0.05f, GetEffectiveDecisionInterval() * 0.5f);
        }

        private void ClearPendingPhaseTransitionOpener()
        {
            pendingPhaseTransitionOpenerId = string.Empty;
            pendingPhaseTransitionOpenerRetryTimer = 0f;
            pendingPhaseTransitionOpenerRetriesLeft = 0;
            pendingPhaseTransitionOpenerPhaseIndex = -1;
        }

        private bool TryQueuePhaseTransitionOpenerById(string openerId)
        {
            if (string.IsNullOrWhiteSpace(openerId))
            {
                return false;
            }

            BossPhase phase = GetCurrentPhase();
            bool lockSpecialByList = phase != null && phase.unlockSpecialAttacks && phase.unlockedAttacks != null && phase.unlockedAttacks.Count > 0;
            for (int i = 0; i < attacks.Count; i++)
            {
                BossAttack attack = attacks[i];
                if (attack == null || !string.Equals(attack.attackId, openerId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!CanUseAttackForCurrentPhase(attack, phase, lockSpecialByList))
                {
                    return false;
                }

                string key = GetAttackKey(attack, i);
                if (!IsAttackReady(key) || CountQueuedEntriesByKey(key) >= Mathf.Max(1, maxSameAttackQueued))
                {
                    return false;
                }

                if (plannedAttacks.Count >= Mathf.Max(1, queuedAttackLimit))
                {
                    return false;
                }

                plannedAttacks.Enqueue(new QueuedBossAttack
                {
                    attack = attack,
                    key = key
                });
                lastQueuedAttackKey = key;
                return true;
            }

            return false;
        }

        private bool TryQueuePhaseTransitionFollowupById(string followupId)
        {
            if (!enablePhaseTransitionFollowupChain || string.IsNullOrWhiteSpace(followupId))
            {
                return false;
            }

            BossPhase phase = GetCurrentPhase();
            bool lockSpecialByList = phase != null && phase.unlockSpecialAttacks && phase.unlockedAttacks != null && phase.unlockedAttacks.Count > 0;
            for (int i = 0; i < attacks.Count; i++)
            {
                BossAttack attack = attacks[i];
                if (attack == null || !string.Equals(attack.attackId, followupId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!CanUseAttackForCurrentPhase(attack, phase, lockSpecialByList))
                {
                    return false;
                }

                string key = GetAttackKey(attack, i);
                if (!IsAttackReady(key) || CountQueuedEntriesByKey(key) >= Mathf.Max(1, maxSameAttackQueued))
                {
                    return false;
                }

                if (plannedAttacks.Count >= Mathf.Max(1, queuedAttackLimit))
                {
                    return false;
                }

                plannedAttacks.Enqueue(new QueuedBossAttack
                {
                    attack = attack,
                    key = key
                });
                lastQueuedAttackKey = key;
                return true;
            }

            return false;
        }

        private void SchedulePhaseTransitionFollowupRetry(int phaseIndex)
        {
            if (!enablePhaseTransitionFollowupChain || !enablePhaseTransitionFollowupRetry)
            {
                return;
            }

            string followupId = ResolvePhaseTransitionFollowupId(phaseIndex);
            if (string.IsNullOrWhiteSpace(followupId))
            {
                return;
            }

            int retries = Mathf.Max(0, phaseTransitionFollowupMaxRetries);
            if (retries <= 0)
            {
                return;
            }

            pendingPhaseTransitionFollowupId = followupId;
            pendingPhaseTransitionFollowupRetriesLeft = retries;
            pendingPhaseTransitionFollowupRetryTimer = ResolvePhaseTransitionFollowupRetryDelay();
        }

        private void UpdatePhaseTransitionFollowupRetry(float deltaTime)
        {
            if (!enablePhaseTransitionFollowupChain || !enablePhaseTransitionFollowupRetry)
            {
                ClearPendingPhaseTransitionFollowup();
                return;
            }

            if (string.IsNullOrWhiteSpace(pendingPhaseTransitionFollowupId) || pendingPhaseTransitionFollowupRetriesLeft <= 0)
            {
                return;
            }

            pendingPhaseTransitionFollowupRetryTimer -= Mathf.Max(0f, deltaTime);
            if (pendingPhaseTransitionFollowupRetryTimer > 0f)
            {
                return;
            }

            bool queued = TryQueuePhaseTransitionFollowupById(pendingPhaseTransitionFollowupId);
            if (queued)
            {
                debugLastPhaseFollowupQueued = true;
                attackTimer = Mathf.Max(attackTimer, GetEffectiveAttackInterval());
                ClearPendingPhaseTransitionFollowup();
                SyncDebugState();
                return;
            }

            pendingPhaseTransitionFollowupRetriesLeft--;
            if (pendingPhaseTransitionFollowupRetriesLeft <= 0)
            {
                ClearPendingPhaseTransitionFollowup();
                return;
            }

            pendingPhaseTransitionFollowupRetryTimer = ResolvePhaseTransitionFollowupRetryDelay();
        }

        private float ResolvePhaseTransitionFollowupRetryDelay()
        {
            if (phaseTransitionFollowupRetryDelay > 0f)
            {
                return phaseTransitionFollowupRetryDelay;
            }

            return Mathf.Max(0.05f, GetEffectiveDecisionInterval() * 0.5f);
        }

        private void ClearPendingPhaseTransitionFollowup()
        {
            pendingPhaseTransitionFollowupId = string.Empty;
            pendingPhaseTransitionFollowupRetryTimer = 0f;
            pendingPhaseTransitionFollowupRetriesLeft = 0;
        }

        private bool TryQueueForcedSpecialDuringPhase3Priority(List<int> candidateIndices)
        {
            if (!forceSpecialQueueDuringPhase3Priority || phase3SpecialPriorityTimer <= 0f)
            {
                return false;
            }

            if (candidateIndices == null || candidateIndices.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < candidateIndices.Count; i++)
            {
                int attackIndex = candidateIndices[i];
                BossAttack attack = attacks[attackIndex];
                if (attack == null || !attack.isSpecial)
                {
                    continue;
                }

                string key = GetAttackKey(attack, attackIndex);
                if (CountQueuedEntriesByKey(key) >= Mathf.Max(1, maxSameAttackQueued))
                {
                    continue;
                }

                plannedAttacks.Enqueue(new QueuedBossAttack
                {
                    attack = attack,
                    key = key
                });
                lastQueuedAttackKey = key;
                return true;
            }

            return false;
        }

        private void ApplyPhaseStats(BossPhase phase)
        {
            if (phase == null)
            {
                return;
            }

            CacheBaseStatsIfNeeded();
            currentPhaseDamageMultiplier = Mathf.Max(0.1f, phase.damageMultiplier);
            currentPhaseSpeedMultiplier = Mathf.Max(0.1f, phase.speedMultiplier);
            currentPhaseDefenseMultiplier = Mathf.Max(0f, phase.defenseMultiplier);
            ApplyEffectiveStats(GetTimePressureFactor());
            lastAppliedTimePressure = GetTimePressureFactor();
        }

        private void ApplyEffectiveStats(float timePressureFactor)
        {
            CacheBaseStatsIfNeeded();

            float clampedPressure = Mathf.Clamp01(timePressureFactor);
            float pressureDamage = Mathf.Lerp(1f, Mathf.Max(1f, maxTimePressureDamageMultiplier), clampedPressure);
            float pressureSpeed = Mathf.Lerp(1f, Mathf.Max(1f, maxTimePressureSpeedMultiplier), clampedPressure);
            float punishFactor = GetPostBreakPunishFactor();
            float punishSpeed = Mathf.Lerp(1f, Mathf.Max(1f, postBreakChaseSpeedMultiplier), punishFactor);

            if (ai != null)
            {
                ai.chaseSpeed = Mathf.Max(0.1f, baseChaseSpeed * currentPhaseSpeedMultiplier * pressureSpeed * punishSpeed);
                ai.attackDamage = Mathf.Max(1, Mathf.RoundToInt(baseAttackDamage * currentPhaseDamageMultiplier * pressureDamage));
            }

            if (health != null)
            {
                health.defense = baseDefense * currentPhaseDefenseMultiplier;
            }
        }

        private void UpdateTimePressure()
        {
            float factor = GetTimePressureFactor();
            if (Mathf.Abs(factor - lastAppliedTimePressure) < 0.01f)
            {
                return;
            }

            ApplyEffectiveStats(factor);
            lastAppliedTimePressure = factor;
        }

        private void UpdatePostBreakPunish(float deltaTime)
        {
            if (postBreakPunishTimer <= 0f)
            {
                return;
            }

            float beforeFactor = GetPostBreakPunishFactor();
            postBreakPunishTimer = Mathf.Max(0f, postBreakPunishTimer - Mathf.Max(0f, deltaTime));
            float afterFactor = GetPostBreakPunishFactor();
            if (Mathf.Abs(afterFactor - beforeFactor) >= 0.01f)
            {
                ApplyEffectiveStats(GetTimePressureFactor());
            }
        }

        private float GetPostBreakPunishFactor()
        {
            if (!enablePostBreakPunishWindow || postBreakPunishTimer <= 0f)
            {
                return 0f;
            }

            float duration = Mathf.Max(0.01f, postBreakPunishDuration);
            return Mathf.Clamp01(postBreakPunishTimer / duration);
        }

        private float GetTimePressureFactor()
        {
            if (!enableTimePressure)
            {
                return 0f;
            }

            float delay = Mathf.Max(0f, timePressureDelay);
            if (encounterElapsed <= delay)
            {
                return 0f;
            }

            float ramp = Mathf.Max(1f, timePressureRampDuration);
            return Mathf.Clamp01((encounterElapsed - delay) / ramp);
        }

        private float GetEffectiveDecisionInterval()
        {
            float interval = Mathf.Max(0.05f, decisionInterval);
            float result = interval;

            if (scaleDecisionIntervalWithTimePressure)
            {
                float pressureFactor = GetTimePressureFactor();
                float minMultiplier = Mathf.Clamp(minDecisionIntervalMultiplierAtMaxPressure, 0.3f, 1f);
                float pressureMultiplier = Mathf.Lerp(1f, minMultiplier, pressureFactor);
                result *= pressureMultiplier;
            }

            float punishFactor = GetPostBreakPunishFactor();
            float punishMultiplier = Mathf.Lerp(1f, Mathf.Clamp(postBreakDecisionIntervalMultiplier, 0.3f, 1f), punishFactor);
            return Mathf.Max(0.05f, result * punishMultiplier);
        }

        private float GetEffectiveAttackInterval()
        {
            float interval = Mathf.Max(0f, attackInterval);
            float punishFactor = GetPostBreakPunishFactor();
            if (punishFactor <= 0f)
            {
                return interval;
            }

            float punishMultiplier = Mathf.Lerp(1f, Mathf.Clamp(postBreakAttackIntervalMultiplier, 0.3f, 1f), punishFactor);
            return Mathf.Max(0f, interval * punishMultiplier);
        }

        private void ApplyPhasePresentation(BossPhase phase)
        {
            if (phase == null)
            {
                return;
            }

            if (phase.phaseEnterEffect != null)
            {
                phase.phaseEnterEffect.Play();
            }

            if (phase.phaseEnterSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(phase.phaseEnterSound, transform.position);
            }

            if (phase.timeScale > 0f && !Mathf.Approximately(phase.timeScale, Time.timeScale))
            {
                if (!hasOverriddenTimeScale)
                {
                    prePhaseTimeScale = Time.timeScale;
                    hasOverriddenTimeScale = true;
                }

                Time.timeScale = phase.timeScale;
            }
        }

        private void RestoreTimeScaleIfNeeded()
        {
            if (!hasOverriddenTimeScale)
            {
                return;
            }

            Time.timeScale = prePhaseTimeScale;
            hasOverriddenTimeScale = false;
        }

        private void HandleDamageTaken(int damage, Vector3 source)
        {
            if (usePhases)
            {
                CheckPhaseTransition();
            }

            if (enableBreakWindow && !breakWindowActive && breakCooldownTimer <= 0f && damage > 0)
            {
                AddStagger(damage * Mathf.Max(0f, staggerPerDamage));
            }
        }

        public void RegisterBreakValue(float breakValue)
        {
            if (!enableBreakWindow || breakWindowActive || breakCooldownTimer > 0f || breakValue <= 0f)
            {
                return;
            }

            AddStagger(breakValue);
        }

        private void AddStagger(float value)
        {
            if (!enableBreakWindow || value <= 0f)
            {
                return;
            }

            currentStagger = Mathf.Min(Mathf.Max(1f, staggerMax), currentStagger + value);
            if (currentStagger >= Mathf.Max(1f, staggerMax))
            {
                TriggerBreakWindow();
            }
        }

        private void TriggerBreakWindow()
        {
            if (breakWindowActive || !enableBreakWindow)
            {
                return;
            }

            breakWindowActive = true;
            breakTimer = 0f;
            breakCooldownTimer = Mathf.Max(0f, breakWindowCooldown);
            currentStagger = 0f;
            isVulnerable = true;
            postBreakPunishTimer = 0f;

            plannedAttacks.Clear();
            CancelCurrentAttack(false, true);

            if (animator != null && !string.IsNullOrEmpty(breakTrigger))
            {
                animator.SetTrigger(breakTrigger);
            }

            if (ai != null)
            {
                ai.SetStunned(true);
                ai.enabled = false;
            }

            OnBreakWindowStart?.Invoke();
            GameEvents.BossBreakWindowStart();
            SyncDebugState();
        }

        private void UpdateBreakWindow(float deltaTime)
        {
            if (!breakWindowActive)
            {
                return;
            }

            breakTimer += deltaTime;
            if (breakTimer < Mathf.Max(0.05f, breakWindowDuration))
            {
                return;
            }

            breakWindowActive = false;
            breakTimer = 0f;
            isVulnerable = false;

            if (ai != null)
            {
                ai.enabled = true;
                ai.SetStunned(false);
            }

            if (enablePostBreakPunishWindow && postBreakPunishDuration > 0f)
            {
                postBreakPunishTimer = Mathf.Max(postBreakPunishTimer, postBreakPunishDuration);
                ApplyEffectiveStats(GetTimePressureFactor());
                decisionTimer = Mathf.Max(decisionTimer, GetEffectiveDecisionInterval());
                attackTimer = Mathf.Max(attackTimer, GetEffectiveAttackInterval());
            }

            OnBreakWindowEnd?.Invoke();
            SyncDebugState();
        }

        private void UpdateStun(float deltaTime)
        {
            if (stunTimer <= 0f)
            {
                return;
            }

            stunTimer -= deltaTime;
            if (stunTimer > 0f)
            {
                return;
            }

            stunTimer = 0f;
            if (ai != null)
            {
                ai.SetStunned(false);
            }
        }

        private void UpdateAttackPlanning(float deltaTime)
        {
            if (attacks.Count == 0)
            {
                return;
            }

            decisionTimer += deltaTime;
            if (decisionTimer < GetEffectiveDecisionInterval())
            {
                return;
            }

            decisionTimer = 0f;
            if (plannedAttacks.Count >= Mathf.Max(1, queuedAttackLimit))
            {
                return;
            }

            if (useAttackQueue)
            {
                TryEnqueueWeightedAttack();
                return;
            }

            plannedAttacks.Clear();
            TryEnqueueWeightedAttack();
        }

        private void TryEnqueueWeightedAttack()
        {
            List<int> candidateIndices = GetAvailableAttackIndices();
            if (candidateIndices.Count == 0)
            {
                return;
            }

            if (TryQueueForcedSpecialDuringPhase3Priority(candidateIndices))
            {
                SyncDebugState();
                return;
            }

            float distanceToPlayer = GetDistanceToPlayer();
            debugLastPlanningDistance = distanceToPlayer;
            float totalWeight = 0f;
            List<float> dynamicWeights = new List<float>(candidateIndices.Count);
            for (int i = 0; i < candidateIndices.Count; i++)
            {
                int attackIndex = candidateIndices[i];
                BossAttack attack = attacks[attackIndex];
                string key = GetAttackKey(attack, attackIndex);

                if (CountQueuedEntriesByKey(key) >= Mathf.Max(1, maxSameAttackQueued))
                {
                    dynamicWeights.Add(0f);
                    continue;
                }

                float weight = Mathf.Max(0.01f, attack.selectionWeight);
                if (!string.IsNullOrEmpty(lastQueuedAttackKey) && string.Equals(lastQueuedAttackKey, key, StringComparison.Ordinal))
                {
                    weight *= Mathf.Clamp01(immediateRepeatPenalty);
                }

                if (prioritizeSpecialAttacksWhenEnraged && isEnraged && attack.isSpecial)
                {
                    weight *= 1.25f;
                }

                if (phase3SpecialPriorityTimer > 0f && attack.isSpecial)
                {
                    weight *= Mathf.Max(1f, phase3SpecialPriorityWeightMultiplier);
                }

                if (weightAttacksByDistance)
                {
                    weight *= GetDistanceWeightMultiplier(attack, distanceToPlayer);
                }

                dynamicWeights.Add(weight);
                totalWeight += weight;
            }

            if (totalWeight <= 0.001f)
            {
                return;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float accumulated = 0f;
            for (int i = 0; i < candidateIndices.Count; i++)
            {
                float weight = dynamicWeights[i];
                if (weight <= 0f)
                {
                    continue;
                }

                accumulated += weight;
                if (roll > accumulated)
                {
                    continue;
                }

                int selectedIndex = candidateIndices[i];
                BossAttack selectedAttack = attacks[selectedIndex];
                string selectedKey = GetAttackKey(selectedAttack, selectedIndex);
                plannedAttacks.Enqueue(new QueuedBossAttack
                {
                    attack = selectedAttack,
                    key = selectedKey
                });
                lastQueuedAttackKey = selectedKey;
                SyncDebugState();
                return;
            }
        }

        private List<int> GetAvailableAttackIndices()
        {
            List<int> available = new List<int>();
            BossPhase phase = GetCurrentPhase();
            bool lockSpecialByList = phase != null && phase.unlockSpecialAttacks && phase.unlockedAttacks != null && phase.unlockedAttacks.Count > 0;

            for (int i = 0; i < attacks.Count; i++)
            {
                BossAttack attack = attacks[i];
                if (attack == null)
                {
                    continue;
                }

                if (!CanUseAttackForCurrentPhase(attack, phase, lockSpecialByList))
                {
                    continue;
                }

                string key = GetAttackKey(attack, i);
                if (!IsAttackReady(key))
                {
                    continue;
                }

                available.Add(i);
            }

            return available;
        }

        private bool CanUseAttackForCurrentPhase(BossAttack attack, BossPhase phase, bool lockSpecialByList)
        {
            if (attack == null)
            {
                return false;
            }

            if (attack.requiresPhase3 && currentPhaseIndex < 2)
            {
                return false;
            }

            if (attack.requiresPhase2 && currentPhaseIndex < 1)
            {
                return false;
            }

            if (attack.isSpecial && phase != null && !phase.unlockSpecialAttacks)
            {
                return false;
            }

            if (attack.isSpecial && lockSpecialByList)
            {
                string attackId = attack.attackId ?? string.Empty;
                for (int i = 0; i < phase.unlockedAttacks.Count; i++)
                {
                    if (string.Equals(phase.unlockedAttacks[i], attackId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }

        private float GetDistanceWeightMultiplier(BossAttack attack, float distanceToPlayer)
        {
            if (attack == null)
            {
                return 0f;
            }

            if (attack.aoe)
            {
                return 1f;
            }

            float range = Mathf.Max(0.1f, attack.range);
            if (distanceToPlayer <= range)
            {
                return Mathf.Max(0f, inRangeWeightMultiplier);
            }

            float overRangeRatio = (distanceToPlayer - range) / range;
            float t = Mathf.Clamp01(overRangeRatio);
            float multiplier = Mathf.Lerp(1f, Mathf.Clamp01(outOfRangeWeightMultiplier), t);
            return Mathf.Max(0f, multiplier);
        }

        private bool IsAttackReady(string key)
        {
            if (!nextAttackReadyTime.TryGetValue(key, out float readyTime))
            {
                return true;
            }

            return Time.time >= readyTime;
        }

        private void SetAttackReadyTime(string key, float cooldown)
        {
            nextAttackReadyTime[key] = Time.time + Mathf.Max(0f, cooldown);
        }

        private void TryStartPlannedAttack()
        {
            if (isInAttack || breakWindowActive || plannedAttacks.Count == 0)
            {
                return;
            }

            if (interruptRecoveryTimer > 0f || comboStartDelayTimer > 0f)
            {
                return;
            }

            if (attackTimer < GetEffectiveAttackInterval())
            {
                return;
            }

            QueuedBossAttack queuedAttack = plannedAttacks.Dequeue();
            if (queuedAttack.attack == null || !IsAttackReady(queuedAttack.key))
            {
                SyncDebugState();
                return;
            }

            runningAttackRoutine = StartCoroutine(ExecuteAttack(queuedAttack));
            SyncDebugState();
        }

        private IEnumerator ExecuteAttack(QueuedBossAttack queuedAttack)
        {
            BossAttack attack = queuedAttack.attack;
            if (attack == null)
            {
                yield break;
            }

            debugLastComboTriggered = false;
            isInAttack = true;
            isVulnerable = false;
            attackTimer = 0f;
            activeAttackKey = queuedAttack.key;
            activeAttackCooldown = Mathf.Max(0f, attack.cooldown);
            OnAttackStarted?.Invoke(attack);

            if (animator != null && !string.IsNullOrEmpty(attack.attackId) && HasAnimatorTrigger(attack.attackId))
            {
                animator.SetTrigger(attack.attackId);
            }

            if (attack.windupTime > 0f)
            {
                isVulnerable = true;
                yield return new WaitForSeconds(attack.windupTime);
            }

            if (breakWindowActive || isDead)
            {
                CancelCurrentAttack(false, breakWindowActive && !isDead);
                yield break;
            }

            if (attack.aoe)
            {
                ExecuteAOEAttack(attack);
            }
            else if (attack.targetPlayer)
            {
                ExecuteTargetedAttack(attack);
            }

            if (attack.activeTime > 0f)
            {
                yield return new WaitForSeconds(attack.activeTime);
            }

            isVulnerable = false;

            if (attack.recoveryTime > 0f)
            {
                yield return new WaitForSeconds(attack.recoveryTime);
            }

            SetAttackReadyTime(queuedAttack.key, attack.cooldown);
            bool comboQueued = TryQueuePhaseComboFollowup(queuedAttack.key);
            if (comboQueued)
            {
                comboStartDelayTimer = Mathf.Max(comboStartDelayTimer, Mathf.Max(0f, comboStartDelay));
                attackTimer = Mathf.Max(attackTimer, GetEffectiveAttackInterval());
            }

            runningAttackRoutine = null;
            isInAttack = false;
            activeAttackKey = string.Empty;
            activeAttackCooldown = 0f;
            SyncDebugState();
        }

        private void CancelCurrentAttack(bool applyCooldown, bool markInterrupted = false)
        {
            bool hadActiveAttack = isInAttack || runningAttackRoutine != null || !string.IsNullOrEmpty(activeAttackKey);
            if (runningAttackRoutine != null)
            {
                StopCoroutine(runningAttackRoutine);
                runningAttackRoutine = null;
            }

            if (applyCooldown)
            {
                if (!string.IsNullOrEmpty(activeAttackKey))
                {
                    SetAttackReadyTime(activeAttackKey, activeAttackCooldown);
                }
                else if (plannedAttacks.Count > 0)
                {
                    QueuedBossAttack pending = plannedAttacks.Peek();
                    if (pending.attack != null)
                    {
                        SetAttackReadyTime(pending.key, pending.attack.cooldown);
                    }
                }
            }

            if (markInterrupted && hadActiveAttack)
            {
                HandleInterruptedAttackRecovery();
            }

            isInAttack = false;
            activeAttackKey = string.Empty;
            activeAttackCooldown = 0f;
            if (!breakWindowActive)
            {
                isVulnerable = false;
            }

            SyncDebugState();
        }

        private void HandleInterruptedAttackRecovery()
        {
            if (!enableInterruptRecoveryGate)
            {
                return;
            }

            float recoveryDelay = Mathf.Max(0f, interruptRecoveryDuration);
            interruptRecoveryTimer = Mathf.Max(interruptRecoveryTimer, recoveryDelay);
            if (!string.IsNullOrEmpty(activeAttackKey))
            {
                float cooldown = activeAttackCooldown * Mathf.Clamp01(interruptedAttackCooldownScale);
                SetAttackReadyTime(activeAttackKey, cooldown);
            }

            decisionTimer = Mathf.Max(decisionTimer, GetEffectiveDecisionInterval());
        }

        private bool TryQueuePhaseComboFollowup(string completedAttackKey)
        {
            if (!enablePhaseComboChain || currentPhaseIndex < 1)
            {
                return false;
            }

            if (plannedAttacks.Count >= Mathf.Max(1, queuedAttackLimit))
            {
                return false;
            }

            float comboChance = currentPhaseIndex >= 2
                ? Mathf.Clamp01(phase3ComboChance)
                : Mathf.Clamp01(phase2ComboChance);
            if (comboChance <= 0f || UnityEngine.Random.value > comboChance)
            {
                return false;
            }

            List<int> candidateIndices = GetAvailableAttackIndices();
            if (candidateIndices.Count == 0)
            {
                return false;
            }

            float distanceToPlayer = GetDistanceToPlayer();
            float totalWeight = 0f;
            List<int> weightedIndices = new List<int>(candidateIndices.Count);
            List<float> dynamicWeights = new List<float>(candidateIndices.Count);
            for (int i = 0; i < candidateIndices.Count; i++)
            {
                int attackIndex = candidateIndices[i];
                BossAttack attack = attacks[attackIndex];
                string key = GetAttackKey(attack, attackIndex);

                if (CountQueuedEntriesByKey(key) >= Mathf.Max(1, maxSameAttackQueued))
                {
                    continue;
                }

                float weight = Mathf.Max(0.01f, attack.selectionWeight);
                if (!string.IsNullOrEmpty(completedAttackKey)
                    && string.Equals(key, completedAttackKey, StringComparison.Ordinal))
                {
                    weight *= Mathf.Clamp01(comboRepeatPenalty);
                }

                if (prioritizeSpecialAttacksWhenEnraged && isEnraged && attack.isSpecial)
                {
                    weight *= 1.15f;
                }

                if (weightAttacksByDistance)
                {
                    weight *= GetDistanceWeightMultiplier(attack, distanceToPlayer);
                }

                if (weight <= 0.001f)
                {
                    continue;
                }

                weightedIndices.Add(attackIndex);
                dynamicWeights.Add(weight);
                totalWeight += weight;
            }

            if (totalWeight <= 0.001f || weightedIndices.Count == 0)
            {
                return false;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float accumulated = 0f;
            for (int i = 0; i < weightedIndices.Count; i++)
            {
                accumulated += dynamicWeights[i];
                if (roll > accumulated)
                {
                    continue;
                }

                int selectedIndex = weightedIndices[i];
                BossAttack selectedAttack = attacks[selectedIndex];
                string selectedKey = GetAttackKey(selectedAttack, selectedIndex);
                plannedAttacks.Enqueue(new QueuedBossAttack
                {
                    attack = selectedAttack,
                    key = selectedKey
                });
                lastQueuedAttackKey = selectedKey;
                debugLastComboTriggered = true;
                SyncDebugState();
                return true;
            }

            return false;
        }

        private void ExecuteAOEAttack(BossAttack attack)
        {
            if (attack == null)
            {
                return;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, attack.aoeRadius, LayerMask.GetMask("Player"));
            int finalDamage = GetFinalAttackDamage(attack);
            float finalKnockback = Mathf.Max(0f, attack.knockbackForce);
            for (int i = 0; i < hits.Length; i++)
            {
                PlayerHealth playerHealth = hits[i] != null ? hits[i].GetComponent<PlayerHealth>() : null;
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(finalDamage, transform.position, finalKnockback);
                }
            }
        }

        private void ExecuteTargetedAttack(BossAttack attack)
        {
            if (attack == null)
            {
                return;
            }

            Transform playerTransform = ResolvePlayerTransform();
            if (playerTransform == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > attack.range)
            {
                return;
            }

            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                return;
            }

            int finalDamage = GetFinalAttackDamage(attack);
            float finalKnockback = Mathf.Max(0f, attack.knockbackForce);
            playerHealth.TakeDamage(finalDamage, transform.position, finalKnockback);
        }

        private Transform ResolvePlayerTransform()
        {
            if (ai != null && ai.Player != null)
            {
                return ai.Player;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            return playerObject != null ? playerObject.transform : null;
        }

        private float GetDistanceToPlayer()
        {
            Transform playerTransform = ResolvePlayerTransform();
            if (playerTransform == null)
            {
                return 0f;
            }

            return Vector3.Distance(transform.position, playerTransform.position);
        }

        private bool HasAnimatorTrigger(string triggerName)
        {
            if (animator == null || string.IsNullOrEmpty(triggerName))
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Trigger
                    && string.Equals(parameter.name, triggerName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private int GetFinalAttackDamage(BossAttack attack)
        {
            float phaseDamageMultiplier = 1f;
            BossPhase phase = GetCurrentPhase();
            if (phase != null)
            {
                phaseDamageMultiplier = Mathf.Max(0.1f, phase.damageMultiplier);
            }

            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1f, attack.damage) * phaseDamageMultiplier));
        }

        private int CountQueuedEntriesByKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return 0;
            }

            int count = 0;
            foreach (QueuedBossAttack queued in plannedAttacks)
            {
                if (string.Equals(queued.key, key, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetAttackKey(BossAttack attack, int index)
        {
            if (attack == null)
            {
                return $"attack_{index}";
            }

            if (!string.IsNullOrEmpty(attack.attackId))
            {
                return attack.attackId;
            }

            if (!string.IsNullOrEmpty(attack.attackName))
            {
                return attack.attackName;
            }

            return $"attack_{index}";
        }

        private static string Localize(string key, string fallback)
        {
            LocalizationService service = LocalizationService.Instance;
            if (service != null)
            {
                return service.Get(key, fallback);
            }

            return fallback;
        }

        public void StunBoss(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            stunTimer = Mathf.Max(stunTimer, duration);
            CancelCurrentAttack(false, true);
            if (ai != null)
            {
                ai.SetStunned(true);
            }
        }

        public float GetWeaknessMultiplier()
        {
            return hasWeakness ? Mathf.Max(1f, weaknessMultiplier) : 1f;
        }

        public bool IsWeaknessElement(DamageElementType elementType)
        {
            if (!hasWeakness || string.IsNullOrEmpty(weaknessElement))
            {
                return false;
            }

            string value = weaknessElement.Trim().ToLowerInvariant();
            switch (value)
            {
                case "physical":
                    return elementType == DamageElementType.Physical;
                case "heat":
                case "fire":
                    return elementType == DamageElementType.Heat;
                case "electric":
                case "electricity":
                case "lightning":
                    return elementType == DamageElementType.Electric;
                case "toxin":
                case "poison":
                    return elementType == DamageElementType.Toxin;
                case "corrosion":
                case "acid":
                    return elementType == DamageElementType.Corrosion;
                default:
                    return false;
            }
        }

        public BossPhase GetCurrentPhase()
        {
            if (currentPhaseIndex >= 0 && currentPhaseIndex < phases.Count)
            {
                return phases[currentPhaseIndex];
            }

            return null;
        }

        private void HandleDeath()
        {
            isDead = true;
            postBreakPunishTimer = 0f;
            plannedAttacks.Clear();
            ClearPendingPhaseTransitionOpener();
            ClearPendingPhaseTransitionFollowup();
            CancelCurrentAttack(false, false);
            RestoreTimeScaleIfNeeded();
            OnBossDefeated?.Invoke();
            GameEvents.ShowMessage(Localize("boss.defeated", "BOSS DEFEATED!"), 5f);
            SyncDebugState();
        }

        private void SyncDebugState()
        {
            debugQueuedAttackCount = plannedAttacks.Count;
            debugStagger = currentStagger;
            debugBreakWindowActive = breakWindowActive;
            debugTimePressure = GetTimePressureFactor();
            debugEffectiveDecisionInterval = GetEffectiveDecisionInterval();
            debugPostBreakPunishFactor = GetPostBreakPunishFactor();
            debugInterruptRecoveryTimer = interruptRecoveryTimer;
            debugComboStartDelayTimer = comboStartDelayTimer;
            debugPhase3SpecialPriorityTimer = phase3SpecialPriorityTimer;
            debugPhaseTransitionFollowupRetryTimer = pendingPhaseTransitionFollowupRetryTimer;
        }
    }
}
