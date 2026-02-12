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
        public float hitStunDuration = 0.3f;

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
        private PlayerActionController actionController;
        private BlockDodgeSystem blockDodgeSystem;

        private float externalInvincibilityTimer = 0f;
        private float damageReductionTimer = 0f;
        private float damageReductionPercent = 0f;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public float HealthPercent => (float)currentHealth / maxHealth;

        public delegate void HealthChangedEvent(int currentHealth, int maxHealth);
        public event HealthChangedEvent OnHealthChanged;

        public delegate void DeathEvent();
        public event DeathEvent OnDeath;

        public void ApplyMaxHealth(int newMaxHealth, bool keepPercent)
        {
            if (newMaxHealth < 1)
            {
                newMaxHealth = 1;
            }

            float percent = keepPercent ? HealthPercent : 1f;
            maxHealth = newMaxHealth;
            currentHealth = Mathf.Clamp(Mathf.RoundToInt(maxHealth * percent), 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponent<Animator>();
            renderers = GetComponentsInChildren<Renderer>();
            actionController = GetComponent<PlayerActionController>();
            blockDodgeSystem = GetComponent<BlockDodgeSystem>();
        }

        private void Update()
        {
            if (externalInvincibilityTimer > 0f)
            {
                externalInvincibilityTimer -= Time.deltaTime;
            }

            if (damageReductionTimer > 0f)
            {
                damageReductionTimer -= Time.deltaTime;
                if (damageReductionTimer <= 0f)
                {
                    damageReductionPercent = 0f;
                }
            }
        }

        public void TakeDamage(int damage, Vector3 damageSource, float knockbackForce = 0f)
        {
            if (isDead) return;

            if (IsInvincible())
            {
                return;
            }

            int finalDamage = Mathf.Max(0, damage);
            if (blockDodgeSystem != null)
            {
                finalDamage = blockDodgeSystem.ProcessBlockDamage(finalDamage);
            }

            if (finalDamage <= 0)
            {
                return;
            }

            if (damageReductionPercent > 0f)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * (1f - damageReductionPercent));
                finalDamage = Mathf.Max(0, finalDamage);
            }

            if (finalDamage <= 0)
            {
                return;
            }

            currentHealth -= finalDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            // 触发事件
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            GameEvents.PlayerDamaged(finalDamage, damageSource);

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                if (actionController != null)
                {
                    actionController.TryStartAction(
                        PlayerActionState.Hit,
                        ActionPriority.Hit,
                        hitStunDuration,
                        true,
                        true,
                        true,
                        false,
                        ActionInterruptMask.None,
                        true);
                }
                StartCoroutine(HitReaction(damageSource, knockbackForce));
            }
        }

        public void ApplyInvincibility(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            externalInvincibilityTimer = Mathf.Max(externalInvincibilityTimer, duration);
        }

        public void ApplyDamageReduction(float reductionPercent, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            float clamped = Mathf.Clamp01(reductionPercent);
            if (clamped <= 0f)
            {
                return;
            }

            damageReductionPercent = Mathf.Max(damageReductionPercent, clamped);
            damageReductionTimer = Mathf.Max(damageReductionTimer, duration);
        }

        private bool IsInvincible()
        {
            if (isInvincible)
            {
                return true;
            }

            if (externalInvincibilityTimer > 0f)
            {
                return true;
            }

            if (blockDodgeSystem != null && blockDodgeSystem.IsInvincible)
            {
                return true;
            }

            return false;
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

            if (actionController != null)
            {
                actionController.TryStartAction(
                    PlayerActionState.Dead,
                    ActionPriority.Dead,
                    0f,
                    true,
                    true,
                    false,
                    false,
                    ActionInterruptMask.None,
                    true);
            }

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

            if (actionController != null)
            {
                actionController.EndAction(PlayerActionState.Dead);
            }

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
