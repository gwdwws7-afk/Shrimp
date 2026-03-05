using UnityEngine;
using System.Collections;

namespace ThirdPersonController
{
    public class EnemyHealth : MonoBehaviour, IPoolable
    {
        [Header("Health Settings")]
        public int maxHealth = 50;
        public float deathDelay = 2f;
        public float hitStunDuration = 0.2f;

        [Header("Visual Effects")]
        public ParticleSystem hitEffect;
        public ParticleSystem heavyHitEffect;
        public ParticleSystem knockdownEffect;
        public ParticleSystem deathEffect;
        public AudioClip hitSound;
        public AudioClip deathSound;

        [Header("Loot")]
        public GameObject[] dropItems;
        public float dropChance = 0.3f;

        [Header("Rewards")]
        public EnemyType enemyType = EnemyType.Grunt;
        public int expReward = 1;

        [Header("Resistances")]
        [Range(-1f, 1f)]
        public float resistPhysical = 0f;
        [Range(-1f, 1f)]
        public float resistHeat = 0f;
        [Range(-1f, 1f)]
        public float resistElectric = 0f;
        [Range(-1f, 1f)]
        public float resistToxin = 0f;
        [Range(-1f, 1f)]
        public float resistCorrosion = 0f;

        private int currentHealth;
        private bool isDead = false;
        private Animator animator;
        private AudioSource audioSource;
        private EnemyAI ai;
        private EnemyHitReaction hitReaction;
        private EnemyHitReactionType lastHitReactionType = EnemyHitReactionType.Flinch;
        private DamageSourceType lastDamageSourceType = DamageSourceType.Environment;
        private bool lastDamageWasHeavy;
        private BossCombatTemplate bossTemplate;
        private Coroutine deathRoutine;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public float defense = 0f;
        public DamageSourceType LastDamageSourceType => lastDamageSourceType;
        public bool LastDamageWasHeavy => lastDamageWasHeavy;
        
        public System.Action<int, Vector3> OnDamageTaken;
        public System.Action OnDeath;

        private void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            ai = GetComponent<EnemyAI>();
            hitReaction = GetComponent<EnemyHitReaction>();
            bossTemplate = GetComponent<BossCombatTemplate>();

            if (hitReaction != null && hitStunDuration > 0f && hitReaction.profile == null)
            {
                hitReaction.flinchDuration = hitStunDuration;
            }
        }

        private void OnEnable()
        {
            ResetState();
        }

        public void TakeDamage(int damage, Vector3 damageSource, float knockbackForce = 0f)
        {
            if (isDead) return;

            currentHealth -= damage;
            
            OnDamageTaken?.Invoke(damage, damageSource);

            // Play hit effects
            if (hitEffect != null)
            {
                hitEffect.Play();
            }

            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
            }

            if (hitReaction != null)
            {
                float appliedKnockback = GetAppliedKnockbackForce(knockbackForce);
                EnemyHitReactionType? forcedReaction = GetForcedReaction();
                lastHitReactionType = hitReaction.ApplyHit(damageSource, appliedKnockback, forcedReaction);
            }
            else
            {
                // Fallback hit animation
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.SetTrigger("Hit");
                }

                if (hitStunDuration > 0f && ai != null)
                {
                    StartCoroutine(HitStunFallback());
                }
            }

            if (lastHitReactionType == EnemyHitReactionType.Knockdown)
            {
                if (knockdownEffect != null)
                {
                    knockdownEffect.Play();
                }
            }
            else if (lastHitReactionType == EnemyHitReactionType.Knockback)
            {
                if (heavyHitEffect != null)
                {
                    heavyHitEffect.Play();
                }
            }

            GameEvents.EnemyHit(damage, transform.position, lastHitReactionType);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void RegisterDamageSource(DamageSourceType sourceType, bool isHeavyAttack)
        {
            lastDamageSourceType = sourceType;
            lastDamageWasHeavy = isHeavyAttack;
        }

        public float GetResistance(DamageElementType elementType)
        {
            switch (elementType)
            {
                case DamageElementType.Heat:
                    return resistHeat;
                case DamageElementType.Electric:
                    return resistElectric;
                case DamageElementType.Toxin:
                    return resistToxin;
                case DamageElementType.Corrosion:
                    return resistCorrosion;
                default:
                    return resistPhysical;
            }
        }

        private EnemyHitReactionType? GetForcedReaction()
        {
            if (bossTemplate != null)
            {
                if (bossTemplate.IsBreakWindowActive && bossTemplate.forceKnockdownDuringBreak)
                {
                    return EnemyHitReactionType.Knockdown;
                }
            }

            if (!lastDamageWasHeavy)
            {
                return null;
            }

            switch (enemyType)
            {
                case EnemyType.Grunt:
                case EnemyType.Rusher:
                case EnemyType.Tank:
                    return EnemyHitReactionType.Knockdown;
                case EnemyType.Elite:
                case EnemyType.Mutant:
                    return EnemyHitReactionType.Knockback;
                default:
                    return null;
            }
        }

        private float GetAppliedKnockbackForce(float knockbackForce)
        {
            if (enemyType != EnemyType.Boss || !lastDamageWasHeavy)
            {
                return knockbackForce;
            }

            if (bossTemplate != null && bossTemplate.IsBreakWindowActive)
            {
                return knockbackForce;
            }

            return 0f;
        }

        private void Die()
        {
            isDead = true;
            OnDeath?.Invoke();

            // Play death effects
            if (deathEffect != null)
            {
                deathEffect.Play();
            }

            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound);
            }

            // Trigger death animation
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Death");
            }

            // Disable AI
            EnemyAI ai = GetComponent<EnemyAI>();
            if (ai != null)
                ai.enabled = false;

            if (hitReaction != null)
            {
                hitReaction.CancelReaction();
            }

            // Drop loot
            if (dropItems.Length > 0 && Random.value < dropChance)
            {
                GameObject dropItem = dropItems[Random.Range(0, dropItems.Length)];
                Instantiate(dropItem, transform.position + Vector3.up, Quaternion.identity);
            }

            GameEvents.EnemyKilled(enemyType, transform.position, expReward);
            GameEvents.EnemyKilledDetailed(enemyType, transform.position, expReward, lastDamageSourceType, lastDamageWasHeavy);

            // Destroy after delay
            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
            }
            deathRoutine = StartCoroutine(DestroyAfterDelay());
        }

        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(deathDelay);
            ObjectPoolManager.Despawn(gameObject);
        }

        private IEnumerator HitStunFallback()
        {
            ai.enabled = false;
            yield return new WaitForSeconds(hitStunDuration);
            if (!isDead)
            {
                ai.enabled = true;
            }
        }

        public void OnSpawned()
        {
            ResetState();
        }

        public void OnDespawned()
        {
            StopAllCoroutines();
        }

        private void ResetState()
        {
            currentHealth = maxHealth;
            isDead = false;
            lastDamageSourceType = DamageSourceType.Environment;
            lastDamageWasHeavy = false;
            if (ai != null)
            {
                ai.enabled = true;
            }

            if (hitReaction != null)
            {
                hitReaction.CancelReaction();
            }
        }
    }
}
