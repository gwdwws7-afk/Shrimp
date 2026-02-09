using UnityEngine;
using System.Collections;

namespace ThirdPersonController
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public int maxHealth = 100;
        public float invincibilityTime = 1f;
        public float damageFlashDuration = 0.2f;

        [Header("UI")]
        public GameObject damageEffect;

        [Header("Animation")]
        public string hitAnimTrigger = "Hit";
        public string deathAnimTrigger = "Death";

        private int currentHealth;
        private bool isInvincible = false;
        private bool isDead = false;
        private Animator animator;
        private Renderer[] renderers;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public float HealthPercent => (float)currentHealth / maxHealth;

        public delegate void HealthChangedEvent(int currentHealth, int maxHealth);
        public event HealthChangedEvent OnHealthChanged;

        public delegate void DeathEvent();
        public event DeathEvent OnDeath;

        private void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponent<Animator>();
            renderers = GetComponentsInChildren<Renderer>();
        }

        public void TakeDamage(int damage, Vector3 damageSource, float knockbackForce = 0f)
        {
            if (isInvincible || isDead) return;

            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            // 触发事件
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            GameEvents.PlayerDamaged(damage, damageSource);

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(HitReaction(damageSource, knockbackForce));
            }
        }

        public void Heal(int amount)
        {
            if (isDead) return;

            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            // 触发事件
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            GameEvents.PlayerHealed(amount);
        }

        private IEnumerator HitReaction(Vector3 damageSource, float knockbackForce)
        {
            isInvincible = true;

            // Play hit animation
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Hit");
            }

            // Apply knockback
            if (knockbackForce > 0f)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 knockbackDir = (transform.position - damageSource).normalized;
                    knockbackDir.y = 0.5f;
                    rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                }
            }

            // Flash red
            StartCoroutine(DamageFlash());

            // Show damage effect
            if (damageEffect != null)
            {
                damageEffect.SetActive(true);
                yield return new WaitForSeconds(damageFlashDuration);
                damageEffect.SetActive(false);
            }

            // Wait for invincibility period
            yield return new WaitForSeconds(invincibilityTime - damageFlashDuration);

            isInvincible = false;
        }

        private IEnumerator DamageFlash()
        {
            float elapsed = 0f;
            while (elapsed < damageFlashDuration)
            {
                float flashAmount = Mathf.PingPong(elapsed * 10f, 1f);
                
                foreach (var rend in renderers)
                {
                    foreach (var mat in rend.materials)
                    {
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.SetColor("_EmissionColor", Color.red * flashAmount);
                        }
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Reset emission
            foreach (var rend in renderers)
            {
                foreach (var mat in rend.materials)
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", Color.black);
                    }
                }
            }
        }

        private void Die()
        {
            isDead = true;

            // Play death animation
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Death");
            }

            // Disable movement
            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement != null)
                movement.enabled = false;

            PlayerCombat combat = GetComponent<PlayerCombat>();
            if (combat != null)
                combat.enabled = false;

            // 触发事件
            OnDeath?.Invoke();
            GameEvents.PlayerDeath();
        }

        public void Respawn(Vector3 respawnPosition)
        {
            currentHealth = maxHealth;
            isDead = false;
            isInvincible = false;

            transform.position = respawnPosition;

            // Re-enable components
            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement != null)
                movement.enabled = true;

            PlayerCombat combat = GetComponent<PlayerCombat>();
            if (combat != null)
                combat.enabled = true;

            if (animator != null)
            {
                animator.Rebind();
            }

            // 触发事件
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            GameEvents.PlayerRespawn();
        }
    }
}
