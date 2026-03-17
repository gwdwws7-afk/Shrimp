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

        [Header("Attack Queue")]
        public bool useAttackQueue = true;
        [Min(1)] public int queuedAttackLimit = 3;
        [Min(1)] public int maxSameAttackQueued = 1;
        [Range(0f, 1f)] public float immediateRepeatPenalty = 0.35f;
        public bool prioritizeSpecialAttacksWhenEnraged = true;

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

        public bool IsBreakWindowActive => breakWindowActive;
        public int QueuedAttackCount => plannedAttacks.Count;
        public float CurrentStagger => currentStagger;

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

            CancelCurrentAttack(false);
            RestoreTimeScaleIfNeeded();
        }

        private void Update()
        {
            if (isDead)
            {
                return;
            }

            float delta = Time.deltaTime;
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

            UpdatePhase();
            UpdateAttackPlanning(delta);
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
            decisionTimer = Mathf.Max(decisionTimer, decisionInterval);

            OnPhaseChanged?.Invoke(currentPhase);
            GameEvents.ShowMessage($"PHASE {currentPhase}: {phase.phaseName}!", 2.5f);
        }

        private void ApplyPhaseStats(BossPhase phase)
        {
            if (phase == null)
            {
                return;
            }

            CacheBaseStatsIfNeeded();

            if (ai != null)
            {
                ai.chaseSpeed = Mathf.Max(0.1f, baseChaseSpeed * Mathf.Max(0.1f, phase.speedMultiplier));
                ai.attackDamage = Mathf.Max(1, Mathf.RoundToInt(baseAttackDamage * Mathf.Max(0.1f, phase.damageMultiplier)));
            }

            if (health != null)
            {
                health.defense = baseDefense * Mathf.Max(0f, phase.defenseMultiplier);
            }
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

            plannedAttacks.Clear();
            CancelCurrentAttack(false);

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
            if (decisionTimer < Mathf.Max(0.05f, decisionInterval))
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

            if (attackTimer < Mathf.Max(0f, attackInterval))
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

            isInAttack = true;
            isVulnerable = false;
            attackTimer = 0f;
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
                CancelCurrentAttack(false);
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
            runningAttackRoutine = null;
            isInAttack = false;
        }

        private void CancelCurrentAttack(bool applyCooldown)
        {
            if (runningAttackRoutine != null)
            {
                StopCoroutine(runningAttackRoutine);
                runningAttackRoutine = null;
            }

            if (applyCooldown && plannedAttacks.Count > 0)
            {
                QueuedBossAttack pending = plannedAttacks.Peek();
                if (pending.attack != null)
                {
                    SetAttackReadyTime(pending.key, pending.attack.cooldown);
                }
            }

            isInAttack = false;
            if (!breakWindowActive)
            {
                isVulnerable = false;
            }
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

        public void StunBoss(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            stunTimer = Mathf.Max(stunTimer, duration);
            CancelCurrentAttack(false);
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
            plannedAttacks.Clear();
            CancelCurrentAttack(false);
            RestoreTimeScaleIfNeeded();
            OnBossDefeated?.Invoke();
            GameEvents.ShowMessage("BOSS DEFEATED!", 5f);
            SyncDebugState();
        }

        private void SyncDebugState()
        {
            debugQueuedAttackCount = plannedAttacks.Count;
            debugStagger = currentStagger;
            debugBreakWindowActive = breakWindowActive;
        }
    }
}
