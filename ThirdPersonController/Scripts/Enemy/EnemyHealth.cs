using UnityEngine;
using System.Collections;

namespace ThirdPersonController
{
    public class EnemyHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public int maxHealth = 50;
        public float deathDelay = 2f;
        public float hitStunDuration = 0.2f;

        [Header("Visual Effects")]
        public ParticleSystem hitEffect;
        public ParticleSystem deathEffect;
        public AudioClip hitSound;
        public AudioClip deathSound;

        [Header("Loot")]
        public GameObject[] dropItems;
        public float dropChance = 0.3f;

        private int currentHealth;
        private bool isDead = false;
        private Animator animator;
        private AudioSource audioSource;
        private Rigidbody rb;
        private EnemyAI ai;
        private Coroutine hitStunRoutine;

        public int CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        private void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            rb = GetComponent<Rigidbody>();
            ai = GetComponent<EnemyAI>();
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

            // Apply knockback
            if (knockbackForce > 0f && rb != null)
            {
                Vector3 knockbackDir = (transform.position - damageSource).normalized;
                rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
            }

            // Trigger hit animation
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Hit");
            }

            if (hitStunDuration > 0f && ai != null)
            {
                if (hitStunRoutine != null)
                {
                    StopCoroutine(hitStunRoutine);
                }
                hitStunRoutine = StartCoroutine(HitStun());
            }

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

            // Drop loot
            if (dropItems.Length > 0 && Random.value < dropChance)
            {
                GameObject dropItem = dropItems[Random.Range(0, dropItems.Length)];
                Instantiate(dropItem, transform.position + Vector3.up, Quaternion.identity);
            }

            // Destroy after delay
            StartCoroutine(DestroyAfterDelay());
        }

        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(deathDelay);
            Destroy(gameObject);
        }

        private IEnumerator HitStun()
        {
            ai.enabled = false;
            yield return new WaitForSeconds(hitStunDuration);
            if (!isDead)
            {
                ai.enabled = true;
            }
            hitStunRoutine = null;
        }
    }
}
