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

        private int currentHealth;
        private bool isDead = false;
        private Animator animator;
        private AudioSource audioSource;
        private EnemyAI ai;
        private EnemyHitReaction hitReaction;
        private EnemyHitReactionType lastHitReactionType = EnemyHitReactionType.Flinch;
        private Coroutine deathRoutine;

        public int CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        private void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            ai = GetComponent<EnemyAI>();
            hitReaction = GetComponent<EnemyHitReaction>();

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
                lastHitReactionType = hitReaction.ApplyHit(damageSource, knockbackForce);
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

        private void Die()
        {
            isDead = true;

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
