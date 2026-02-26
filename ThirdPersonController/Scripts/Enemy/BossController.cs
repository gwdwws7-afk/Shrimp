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

    [System.Serializable]
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

    [System.Serializable]
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
        [Header("Configuration")]
        public int maxHealth = 5000;
        public int currentPhase = 1;
        public bool usePhases = true;
        
        [Header("Phases")]
        public List<BossPhase> phases = new List<BossPhase>();
        
        [Header("Attacks")]
        public List<BossAttack> attacks = new List<BossAttack>();
        public float attackInterval = 4f;
        public float decisionInterval = 1f;
        
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
        public System.Action<int> OnPhaseChanged;
        public System.Action<BossAttack> OnAttackStarted;
        public System.Action OnBossDefeated;

        private int currentPhaseIndex = 0;
        private float attackTimer = 0f;
        private float decisionTimer = 0f;
        private bool isDead = false;

        private void Awake()
        {
            if (health == null) health = GetComponent<EnemyHealth>();
            if (ai == null) ai = GetComponent<EnemyAI>();
            if (animator == null) animator = GetComponent<Animator>();
            
            InitializePhases();
        }

        private void InitializePhases()
        {
            if (phases.Count == 0)
            {
                phases.Add(new BossPhase { phaseName = "Normal", healthPercentThreshold = 1f });
                phases.Add(new BossPhase { phaseName = "Enraged", healthPercentThreshold = 0.66f });
                phases.Add(new BossPhase { phaseName = "Desperate", healthPercentThreshold = 0.33f });
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamageTaken += HandleDamageTaken;
                health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamageTaken -= HandleDamageTaken;
                health.OnDeath -= HandleDeath;
            }
        }

        private void Update()
        {
            if (isDead) return;

            UpdatePhase();
            UpdateAttacks();
        }

        private void HandleDamageTaken(int damage, Vector3 source)
        {
            if (usePhases)
            {
                CheckPhaseTransition();
            }
        }

        private void HandleDeath()
        {
            isDead = true;
            OnBossDefeated?.Invoke();
            GameEvents.ShowMessage("BOSS DEFEATED!", 5f);
        }

        private void UpdatePhase()
        {
            if (health == null || !usePhases || phases.Count == 0) return;

            float healthPercent = (float)health.CurrentHealth / health.MaxHealth;

            for (int i = 0; i < phases.Count; i++)
            {
                if (healthPercent <= phases[i].healthPercentThreshold && i > currentPhaseIndex)
                {
                    TransitionToPhase(i);
                    break;
                }
            }
        }

        private void TransitionToPhase(int newPhaseIndex)
        {
            if (newPhaseIndex >= phases.Count) return;

            currentPhaseIndex = newPhaseIndex;
            currentPhase = newPhaseIndex + 1;
            
            BossPhase phase = phases[newPhaseIndex];
            
            Time.timeScale = phase.timeScale;
            
            if (phase.phaseEnterEffect != null)
            {
                phase.phaseEnterEffect.Play();
            }
            
            if (phase.phaseEnterSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(phase.phaseEnterSound, transform.position);
            }
            
            ApplyPhaseStats(phase);
            
            OnPhaseChanged?.Invoke(currentPhase);
            
            GameEvents.ShowMessage($"PHASE {currentPhase}: {phase.phaseName}!", 3f);
        }

        private void ApplyPhaseStats(BossPhase phase)
        {
            if (ai != null)
            {
                ai.chaseSpeed *= phase.speedMultiplier;
                ai.attackDamage = Mathf.RoundToInt(ai.attackDamage * phase.damageMultiplier);
            }
            
            if (health != null)
            {
                health.defense *= phase.defenseMultiplier;
            }
        }

        private void CheckPhaseTransition()
        {
            if (health == null) return;

            float healthPercent = (float)health.CurrentHealth / health.MaxHealth;

            if (healthPercent <= 0.33f && currentPhaseIndex < 2)
            {
                TransitionToPhase(2);
            }
            else if (healthPercent <= 0.66f && currentPhaseIndex < 1)
            {
                TransitionToPhase(1);
            }
        }

        private void UpdateAttacks()
        {
            if (isInAttack || attacks.Count == 0) return;

            attackTimer += Time.deltaTime;
            decisionTimer += Time.deltaTime;

            if (attackTimer >= attackInterval)
            {
                if (decisionTimer >= decisionInterval)
                {
                    DecideNextAttack();
                    decisionTimer = 0f;
                }
            }
        }

        private void DecideNextAttack()
        {
            List<BossAttack> availableAttacks = GetAvailableAttacks();
            
            if (availableAttacks.Count == 0) return;

            BossAttack selected = availableAttacks[Random.Range(0, availableAttacks.Count)];
            StartCoroutine(ExecuteAttack(selected));
        }

        private List<BossAttack> GetAvailableAttacks()
        {
            List<BossAttack> available = new List<BossAttack>();
            
            for (int i = 0; i < attacks.Count; i++)
            {
                BossAttack attack = attacks[i];
                
                if (attack.requiresPhase2 && currentPhaseIndex < 1) continue;
                if (attack.requiresPhase3 && currentPhaseIndex < 2) continue;
                
                available.Add(attack);
            }
            
            return available;
        }

        private IEnumerator ExecuteAttack(BossAttack attack)
        {
            isInAttack = true;
            attackTimer = 0f;
            
            OnAttackStarted?.Invoke(attack);
            
            if (animator != null && !string.IsNullOrEmpty(attack.attackId))
            {
                animator.SetTrigger(attack.attackId);
            }
            
            if (attack.windupTime > 0)
            {
                isVulnerable = true;
                yield return new WaitForSeconds(attack.windupTime);
            }
            
            if (attack.aoe)
            {
                ExecuteAOEAttack(attack);
            }
            else if (attack.targetPlayer)
            {
                ExecuteTargetedAttack(attack);
            }
            
            if (attack.activeTime > 0)
            {
                yield return new WaitForSeconds(attack.activeTime);
            }
            
            isVulnerable = false;
            
            if (attack.recoveryTime > 0)
            {
                yield return new WaitForSeconds(attack.recoveryTime);
            }
            
            isInAttack = false;
        }

        private void ExecuteAOEAttack(BossAttack attack)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, attack.aoeRadius, LayerMask.GetMask("Player"));
            
            for (int i = 0; i < hits.Length; i++)
            {
                PlayerHealth playerHealth = hits[i].GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Vector3 knockbackDir = (hits[i].transform.position - transform.position).normalized;
                    playerHealth.TakeDamage(Mathf.RoundToInt(attack.damage), transform.position, attack.knockbackForce);
                }
            }
        }

        private void ExecuteTargetedAttack(BossAttack attack)
        {
            if (ai != null && ai.Player != null)
            {
                float distance = Vector3.Distance(transform.position, ai.Player.position);
                if (distance <= attack.range)
                {
                    PlayerHealth playerHealth = ai.Player.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        Vector3 knockbackDir = (ai.Player.position - transform.position).normalized;
                        playerHealth.TakeDamage(Mathf.RoundToInt(attack.damage), transform.position, attack.knockbackForce);
                    }
                }
            }
        }

        public void StunBoss(float duration)
        {
            if (ai != null)
            {
                ai.SetStunned(true);
                StartCoroutine(UnstunAfterDelay(duration));
            }
        }

        private IEnumerator UnstunAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (ai != null)
            {
                ai.SetStunned(false);
            }
        }

        public float GetWeaknessMultiplier()
        {
            return hasWeakness ? weaknessMultiplier : 1f;
        }

        public BossPhase GetCurrentPhase()
        {
            if (currentPhaseIndex >= 0 && currentPhaseIndex < phases.Count)
            {
                return phases[currentPhaseIndex];
            }
            return null;
        }
    }
}
