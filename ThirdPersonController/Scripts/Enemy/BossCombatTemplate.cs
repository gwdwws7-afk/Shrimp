using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum BossCombatPhase
    {
        Phase1,
        Phase2
    }

    [System.Serializable]
    public class BossSkillDefinition
    {
        public string id = "";
        public string name = "Skill";
        public float weight = 1f;
        public float cooldown = 6f;
        public float windup = 0.5f;
        public float active = 0.6f;
        public float recovery = 0.8f;
        public float damageMultiplier = 1f;
        public string animatorTrigger = "";

        [Header("Selection")]
        public bool usePreferredRange = false;
        [Min(0f)] public float preferredMinRange = 0f;
        [Min(0f)] public float preferredMaxRange = 5f;
        [Min(0f)] public float phase2WeightMultiplier = 1f;
    }

    public class BossCombatTemplate : MonoBehaviour
    {
        [Header("Core")]
        public EnemyHealth health;
        public EnemyAI ai;
        public Animator animator;
        public bool enableDecisions = true;
        public float decisionInterval = 0.8f;
        public int baseDamage = 25;
        public float baseKnockback = 6f;

        [Header("Phase")]
        public BossCombatPhase currentPhase = BossCombatPhase.Phase1;
        [Range(0.1f, 0.9f)]
        public float phase2HealthThreshold = 0.5f;
        public string phase2Trigger = "Phase2";

        [Header("Skills")]
        public List<BossSkillDefinition> phase1Skills = new List<BossSkillDefinition>();
        public List<BossSkillDefinition> phase2Skills = new List<BossSkillDefinition>();

        [Header("Behavior Depth")]
        public bool avoidSkillSpam = true;
        [Range(0f, 1f)] public float repeatSkillWeightPenalty = 0.55f;
        public bool preferRangeMatching = true;
        [Range(0f, 1f)] public float outOfRangeWeightPenalty = 0.35f;
        [Min(0.2f)] public float phase2DecisionIntervalMultiplier = 0.75f;
        [Min(0.1f)] public float phase2DamageMultiplier = 1.15f;
        public bool refreshCooldownOnPhaseTransition = true;
        [Range(0f, 1f)] public float phase2CooldownRemainingScale = 0.4f;

        [Header("Break Window")]
        public bool enableBreakWindow = true;
        public float breakWindowDuration = 4f;
        public float breakCooldown = 12f;
        public float breakWindowDamageMultiplier = 1.6f;
        public bool forceKnockdownDuringBreak = true;
        public bool allowHeavyKnockdownOutsideBreak = false;
        public float staggerMax = 100f;
        public float staggerPerDamage = 1f;
        public string breakTrigger = "Break";

        [Header("Counterplay Window")]
        public bool enableMissPunishWindow = true;
        [Min(0f)] public float missPunishWindowDuration = 1.25f;
        [Min(1f)] public float punishWindowStaggerMultiplier = 1.4f;

        [Header("Weak Point")]
        public Transform weakPoint;
        public float weakPointDamageMultiplier = 2f;
        public float weakPointRadius = 0.75f;

        [Header("Weakness")]
        public bool useResistanceWeakness = true;
        public DamageElementType weakElementType = DamageElementType.Physical;

        [Header("Debug")]
        public bool debugMessages = false;

        public System.Action<BossCombatPhase> OnPhaseChanged;
        public System.Action<BossSkillDefinition> OnSkillStarted;
        public System.Action OnBreakWindowStart;
        public System.Action OnBreakWindowEnd;

        private readonly Dictionary<string, float> nextReadyTime = new Dictionary<string, float>();
        private float nextDecisionTime;
        protected bool isExecutingSkill;
        private bool isDead;
        private bool breakWindowActive;
        private float breakCooldownTimer;
        private float breakTimer;
        private float staggerCurrent;
        private bool punishWindowActive;
        private float punishWindowTimer;
        private PlayerHealth cachedPlayer;
        private bool suppressNextDamageStagger;
        private string lastSkillId = string.Empty;
        private int repeatedSkillCount = 0;

        public bool IsBreakWindowActive => breakWindowActive;
        public bool IsPunishWindowActive => punishWindowActive;

        protected virtual void Awake()
        {
            if (health == null) health = GetComponent<EnemyHealth>();
            if (ai == null) ai = GetComponent<EnemyAI>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        protected virtual void OnEnable()
        {
            if (health != null)
            {
                health.OnDamageTaken += HandleDamageTaken;
                health.OnDeath += HandleDeath;
            }
        }

        protected virtual void OnDisable()
        {
            if (health != null)
            {
                health.OnDamageTaken -= HandleDamageTaken;
                health.OnDeath -= HandleDeath;
            }
        }

        protected virtual void Update()
        {
            if (isDead)
            {
                return;
            }

            UpdatePhase();
            UpdateBreakWindow();
            UpdatePunishWindow();

            if (!enableDecisions || breakWindowActive || isExecutingSkill)
            {
                return;
            }

            if (Time.time >= nextDecisionTime)
            {
                nextDecisionTime = Time.time + GetEffectiveDecisionInterval();
                BossSkillDefinition skill = SelectSkill();
                if (skill != null)
                {
                    StartCoroutine(ExecuteSkill(skill));
                }
            }
        }

        protected virtual void UpdatePhase()
        {
            if (health == null)
            {
                return;
            }

            float ratio = (float)health.CurrentHealth / Mathf.Max(1, health.MaxHealth);
            if (currentPhase == BossCombatPhase.Phase1 && ratio <= phase2HealthThreshold)
            {
                EnterPhase2();
            }
        }

        protected virtual void EnterPhase2()
        {
            currentPhase = BossCombatPhase.Phase2;
            if (refreshCooldownOnPhaseTransition)
            {
                RefreshCooldownForPhase2();
            }

            nextDecisionTime = Mathf.Min(nextDecisionTime, Time.time + GetEffectiveDecisionInterval() * 0.5f);
            if (animator != null && !string.IsNullOrEmpty(phase2Trigger))
            {
                animator.SetTrigger(phase2Trigger);
            }

            OnPhaseChanged?.Invoke(currentPhase);
            if (debugMessages)
            {
                GameEvents.ShowMessage(Localize("boss.phase2", "Boss Phase 2"), 2f);
            }
        }

        protected virtual void HandleDamageTaken(int damage, Vector3 source)
        {
            if (!enableBreakWindow || breakWindowActive || damage <= 0)
            {
                return;
            }

            if (breakCooldownTimer > 0f)
            {
                return;
            }

            if (suppressNextDamageStagger)
            {
                suppressNextDamageStagger = false;
                return;
            }

            float breakValue = damage * Mathf.Max(0f, staggerPerDamage);
            ApplyBreakValue(breakValue * GetCurrentPunishBreakMultiplier());
        }

        public void RegisterBreakValue(float breakValue)
        {
            if (!enableBreakWindow || breakWindowActive || breakValue <= 0f)
            {
                return;
            }

            if (breakCooldownTimer > 0f)
            {
                return;
            }

            suppressNextDamageStagger = true;
            ApplyBreakValue(breakValue * GetCurrentPunishBreakMultiplier());
        }

        private void ApplyBreakValue(float value)
        {
            staggerCurrent = Mathf.Min(staggerMax, staggerCurrent + value);
            if (staggerCurrent >= staggerMax)
            {
                TriggerBreakWindow();
            }
        }

        protected virtual void TriggerBreakWindow()
        {
            breakWindowActive = true;
            breakTimer = 0f;
            staggerCurrent = 0f;
            breakCooldownTimer = breakCooldown;
            ClosePunishWindow();

            if (animator != null && !string.IsNullOrEmpty(breakTrigger))
            {
                animator.SetTrigger(breakTrigger);
            }

            if (ai != null)
            {
                ai.enabled = false;
            }

            OnBreakWindowStart?.Invoke();
            GameEvents.BossBreakWindowStart();
            if (debugMessages)
            {
                GameEvents.ShowMessage(Localize("boss.break_window", "Break Window"), 2f);
            }
        }

        protected virtual void UpdateBreakWindow()
        {
            if (breakCooldownTimer > 0f)
            {
                breakCooldownTimer -= Time.deltaTime;
            }

            if (!breakWindowActive)
            {
                return;
            }

            breakTimer += Time.deltaTime;
            if (breakTimer >= breakWindowDuration)
            {
                breakWindowActive = false;
                breakTimer = 0f;
                if (ai != null)
                {
                    ai.enabled = true;
                }

                OnBreakWindowEnd?.Invoke();
            }
        }

        protected virtual void UpdatePunishWindow()
        {
            if (!punishWindowActive)
            {
                return;
            }

            punishWindowTimer -= Time.deltaTime;
            if (punishWindowTimer <= 0f)
            {
                ClosePunishWindow();
            }
        }

        protected void TriggerPunishWindow(float duration = -1f)
        {
            if (!enableMissPunishWindow || breakWindowActive)
            {
                return;
            }

            float resolvedDuration = duration >= 0f
                ? duration
                : missPunishWindowDuration;
            resolvedDuration = Mathf.Max(0f, resolvedDuration);
            if (resolvedDuration <= 0f)
            {
                return;
            }

            punishWindowActive = true;
            punishWindowTimer = resolvedDuration;
        }

        protected void ClosePunishWindow()
        {
            punishWindowActive = false;
            punishWindowTimer = 0f;
        }

        private float GetCurrentPunishBreakMultiplier()
        {
            if (!punishWindowActive)
            {
                return 1f;
            }

            return Mathf.Max(1f, punishWindowStaggerMultiplier);
        }

        protected virtual BossSkillDefinition SelectSkill()
        {
            List<BossSkillDefinition> pool = currentPhase == BossCombatPhase.Phase1 ? phase1Skills : phase2Skills;
            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            float distanceToPlayer = GetDistanceToPlayerFlat();
            float totalWeight = 0f;
            List<BossSkillDefinition> available = new List<BossSkillDefinition>();
            List<float> dynamicWeights = new List<float>();
            for (int i = 0; i < pool.Count; i++)
            {
                BossSkillDefinition skill = pool[i];
                if (skill == null)
                {
                    continue;
                }

                if (!IsSkillReady(skill))
                {
                    continue;
                }

                float dynamicWeight = ComputeDynamicSkillWeight(skill, distanceToPlayer);
                if (dynamicWeight <= 0.001f)
                {
                    continue;
                }

                available.Add(skill);
                dynamicWeights.Add(dynamicWeight);
                totalWeight += dynamicWeight;
            }

            if (available.Count == 0)
            {
                return null;
            }

            float roll = Random.Range(0f, totalWeight);
            float accum = 0f;
            for (int i = 0; i < available.Count; i++)
            {
                accum += dynamicWeights[i];
                if (roll <= accum)
                {
                    return available[i];
                }
            }

            return available[available.Count - 1];
        }

        public bool IsWeakPointHit(Vector3 hitPoint)
        {
            if (weakPoint == null)
            {
                return false;
            }

            float radius = Mathf.Max(0.05f, weakPointRadius);
            return Vector3.Distance(weakPoint.position, hitPoint) <= radius;
        }

        public DamageElementType GetWeakElementType(EnemyHealth health)
        {
            if (!useResistanceWeakness)
            {
                return weakElementType;
            }

            if (health == null)
            {
                return weakElementType;
            }

            float min = health.resistPhysical;
            DamageElementType selected = DamageElementType.Physical;

            if (health.resistHeat < min)
            {
                min = health.resistHeat;
                selected = DamageElementType.Heat;
            }

            if (health.resistElectric < min)
            {
                min = health.resistElectric;
                selected = DamageElementType.Electric;
            }

            if (health.resistToxin < min)
            {
                min = health.resistToxin;
                selected = DamageElementType.Toxin;
            }

            if (health.resistCorrosion < min)
            {
                selected = DamageElementType.Corrosion;
            }

            return selected;
        }

        protected virtual IEnumerator ExecuteSkill(BossSkillDefinition skill)
        {
            if (skill == null)
            {
                yield break;
            }

            yield return ExecuteSkillTimeline(skill, null);
        }

        protected IEnumerator ExecuteSkillTimeline(BossSkillDefinition skill, System.Action onActive)
        {
            BeginSkillExecution(skill);

            if (skill.windup > 0f)
            {
                yield return new WaitForSeconds(skill.windup);
            }

            onActive?.Invoke();

            if (skill.active > 0f)
            {
                yield return new WaitForSeconds(skill.active);
            }

            if (skill.recovery > 0f)
            {
                yield return new WaitForSeconds(skill.recovery);
            }

            EndSkillExecution(skill);
        }

        protected void BeginSkillExecution(BossSkillDefinition skill)
        {
            isExecutingSkill = true;
            OnSkillStarted?.Invoke(skill);
            if (animator != null && !string.IsNullOrEmpty(skill.animatorTrigger))
            {
                animator.SetTrigger(skill.animatorTrigger);
            }

            if (debugMessages)
            {
                GameEvents.ShowMessage(string.Format(Localize("boss.skill_format", "Boss Skill: {0}"), skill.name), 1.5f);
            }
        }

        protected void EndSkillExecution(BossSkillDefinition skill)
        {
            SetSkillCooldown(skill);
            UpdateSkillHistory(skill);
            isExecutingSkill = false;
        }

        protected PlayerHealth GetPlayer()
        {
            if (cachedPlayer != null)
            {
                return cachedPlayer;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                cachedPlayer = playerObject.GetComponent<PlayerHealth>();
            }

            return cachedPlayer;
        }

        protected int GetSkillDamage(BossSkillDefinition skill)
        {
            int baseValue = baseDamage;
            if (ai != null)
            {
                baseValue = Mathf.Max(1, ai.attackDamage);
            }

            float phaseMultiplier = currentPhase == BossCombatPhase.Phase2
                ? Mathf.Max(0.1f, phase2DamageMultiplier)
                : 1f;

            return Mathf.Max(1, Mathf.RoundToInt(baseValue * Mathf.Max(0.1f, skill.damageMultiplier) * phaseMultiplier));
        }

        protected float GetSkillKnockback()
        {
            if (ai != null)
            {
                return ai.attackKnockback;
            }

            return baseKnockback;
        }

        protected void ApplyDamageToPlayer(PlayerHealth player, int damage, float knockback)
        {
            if (player == null)
            {
                return;
            }

            player.TakeDamage(damage, transform.position, knockback);
        }

        protected bool IsPlayerInCone(PlayerHealth player, float range, float angle)
        {
            if (player == null)
            {
                return false;
            }

            Vector3 origin = transform.position;
            Vector3 direction = (player.transform.position - origin);
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance > range)
            {
                return false;
            }

            if (distance <= 0.001f)
            {
                return true;
            }

            float angleToTarget = Vector3.Angle(transform.forward, direction.normalized);
            return angleToTarget <= angle * 0.5f;
        }

        protected bool IsPlayerInRadius(PlayerHealth player, float radius)
        {
            if (player == null)
            {
                return false;
            }

            float distance = Vector3.Distance(transform.position, player.transform.position);
            return distance <= radius;
        }

        protected IEnumerator DashForward(float speed, float duration, float hitRadius, int damage, float knockback, System.Action<bool> onResolved = null)
        {
            PlayerHealth player = GetPlayer();
            bool hitApplied = false;
            float timer = 0f;

            if (ai != null)
            {
                ai.enabled = false;
            }

            while (timer < duration)
            {
                transform.position += transform.forward * speed * Time.deltaTime;
                if (!hitApplied && player != null)
                {
                    float distance = Vector3.Distance(transform.position, player.transform.position);
                    if (distance <= hitRadius)
                    {
                        ApplyDamageToPlayer(player, damage, knockback);
                        hitApplied = true;
                    }
                }

                timer += Time.deltaTime;
                yield return null;
            }

            if (ai != null)
            {
                ai.enabled = true;
            }

            onResolved?.Invoke(hitApplied);
        }

        protected virtual bool IsSkillReady(BossSkillDefinition skill)
        {
            if (skill == null)
            {
                return false;
            }

            if (!nextReadyTime.TryGetValue(skill.id, out float readyTime))
            {
                return true;
            }

            return Time.time >= readyTime;
        }

        protected virtual void SetSkillCooldown(BossSkillDefinition skill)
        {
            if (skill == null || string.IsNullOrEmpty(skill.id))
            {
                return;
            }

            nextReadyTime[skill.id] = Time.time + Mathf.Max(0f, skill.cooldown);
        }

        private float GetEffectiveDecisionInterval()
        {
            float interval = Mathf.Max(0.1f, decisionInterval);
            if (currentPhase == BossCombatPhase.Phase2)
            {
                interval *= Mathf.Max(0.2f, phase2DecisionIntervalMultiplier);
            }

            return Mathf.Max(0.1f, interval);
        }

        private void RefreshCooldownForPhase2()
        {
            if (nextReadyTime.Count == 0)
            {
                return;
            }

            List<string> keys = new List<string>(nextReadyTime.Keys);
            float remainScale = Mathf.Clamp01(phase2CooldownRemainingScale);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                float currentReady = nextReadyTime[key];
                if (currentReady <= Time.time)
                {
                    continue;
                }

                float remaining = currentReady - Time.time;
                nextReadyTime[key] = Time.time + remaining * remainScale;
            }
        }

        private float ComputeDynamicSkillWeight(BossSkillDefinition skill, float distanceToPlayer)
        {
            if (skill == null)
            {
                return 0f;
            }

            float weight = Mathf.Max(0.01f, skill.weight);

            if (currentPhase == BossCombatPhase.Phase2)
            {
                weight *= Mathf.Max(0.1f, skill.phase2WeightMultiplier);
            }

            if (avoidSkillSpam && !string.IsNullOrEmpty(lastSkillId) && skill.id == lastSkillId)
            {
                float penalty = Mathf.Clamp01(repeatSkillWeightPenalty);
                int repeatCount = Mathf.Max(1, repeatedSkillCount);
                float penaltyFactor = Mathf.Pow(Mathf.Lerp(1f, 0.05f, penalty), repeatCount);
                weight *= penaltyFactor;
            }

            if (preferRangeMatching && !float.IsInfinity(distanceToPlayer) && !IsSkillWithinPreferredRange(skill, distanceToPlayer))
            {
                weight *= Mathf.Clamp01(outOfRangeWeightPenalty);
            }

            return Mathf.Max(0f, weight);
        }

        private bool IsSkillWithinPreferredRange(BossSkillDefinition skill, float distanceToPlayer)
        {
            if (skill == null)
            {
                return true;
            }

            float minRange;
            float maxRange;
            if (skill.usePreferredRange)
            {
                minRange = Mathf.Max(0f, skill.preferredMinRange);
                maxRange = Mathf.Max(minRange, skill.preferredMaxRange);
            }
            else if (!TryGetPreferredRangeBySkillId(skill.id, out minRange, out maxRange))
            {
                return true;
            }

            return distanceToPlayer >= minRange && distanceToPlayer <= maxRange;
        }

        private static bool TryGetPreferredRangeBySkillId(string skillId, out float minRange, out float maxRange)
        {
            minRange = 0f;
            maxRange = 8f;
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            string id = skillId.ToLowerInvariant();
            if (id.Contains("charge") || id.Contains("dash") || id.Contains("rush"))
            {
                minRange = 3f;
                maxRange = 14f;
                return true;
            }

            if (id.Contains("tail") || id.Contains("sweep") || id.Contains("shield") || id.Contains("slam"))
            {
                minRange = 0f;
                maxRange = 4.8f;
                return true;
            }

            if (id.Contains("spray"))
            {
                minRange = 2f;
                maxRange = 8f;
                return true;
            }

            if (id.Contains("shock") || id.Contains("overload") || id.Contains("vortex") || id.Contains("devour"))
            {
                minRange = 1.5f;
                maxRange = 7.5f;
                return true;
            }

            return false;
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

        private float GetDistanceToPlayerFlat()
        {
            PlayerHealth player = GetPlayer();
            if (player == null)
            {
                return float.PositiveInfinity;
            }

            Vector3 delta = player.transform.position - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        private void UpdateSkillHistory(BossSkillDefinition skill)
        {
            if (skill == null || string.IsNullOrEmpty(skill.id))
            {
                lastSkillId = string.Empty;
                repeatedSkillCount = 0;
                return;
            }

            if (skill.id == lastSkillId)
            {
                repeatedSkillCount++;
            }
            else
            {
                lastSkillId = skill.id;
                repeatedSkillCount = 1;
            }
        }

        protected virtual void HandleDeath()
        {
            isDead = true;
            if (ai != null)
            {
                ai.enabled = false;
            }
        }
    }
}
