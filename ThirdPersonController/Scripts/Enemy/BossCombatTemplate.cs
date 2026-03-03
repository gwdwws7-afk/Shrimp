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

        [Header("Break Window")]
        public bool enableBreakWindow = true;
        public float breakWindowDuration = 4f;
        public float breakCooldown = 12f;
        public float staggerMax = 100f;
        public float staggerPerDamage = 1f;
        public string breakTrigger = "Break";

        [Header("Weak Point")]
        public Transform weakPoint;
        public float weakPointDamageMultiplier = 2f;

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
        private PlayerHealth cachedPlayer;

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

            if (!enableDecisions || breakWindowActive || isExecutingSkill)
            {
                return;
            }

            if (Time.time >= nextDecisionTime)
            {
                nextDecisionTime = Time.time + Mathf.Max(0.1f, decisionInterval);
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
            if (animator != null && !string.IsNullOrEmpty(phase2Trigger))
            {
                animator.SetTrigger(phase2Trigger);
            }

            OnPhaseChanged?.Invoke(currentPhase);
            if (debugMessages)
            {
                GameEvents.ShowMessage("Boss Phase 2", 2f);
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

            staggerCurrent = Mathf.Min(staggerMax, staggerCurrent + damage * Mathf.Max(0f, staggerPerDamage));
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

            if (animator != null && !string.IsNullOrEmpty(breakTrigger))
            {
                animator.SetTrigger(breakTrigger);
            }

            if (ai != null)
            {
                ai.enabled = false;
            }

            OnBreakWindowStart?.Invoke();
            if (debugMessages)
            {
                GameEvents.ShowMessage("Break Window", 2f);
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

        protected virtual BossSkillDefinition SelectSkill()
        {
            List<BossSkillDefinition> pool = currentPhase == BossCombatPhase.Phase1 ? phase1Skills : phase2Skills;
            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            List<BossSkillDefinition> available = new List<BossSkillDefinition>();
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

                available.Add(skill);
                totalWeight += Mathf.Max(0.01f, skill.weight);
            }

            if (available.Count == 0)
            {
                return null;
            }

            float roll = Random.Range(0f, totalWeight);
            float accum = 0f;
            for (int i = 0; i < available.Count; i++)
            {
                accum += Mathf.Max(0.01f, available[i].weight);
                if (roll <= accum)
                {
                    return available[i];
                }
            }

            return available[available.Count - 1];
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
                GameEvents.ShowMessage($"Boss Skill: {skill.name}", 1.5f);
            }
        }

        protected void EndSkillExecution(BossSkillDefinition skill)
        {
            SetSkillCooldown(skill);
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

            return Mathf.Max(1, Mathf.RoundToInt(baseValue * Mathf.Max(0.1f, skill.damageMultiplier)));
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

        protected IEnumerator DashForward(float speed, float duration, float hitRadius, int damage, float knockback)
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
