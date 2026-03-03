using UnityEngine;

namespace ThirdPersonController
{
    public class DefenseTarget : MonoBehaviour
    {
        [Header("Health")]
        public string defenseTargetId = "";
        public int maxHealth = 200;
        public int currentHealth = 200;
        public bool destroyOnDeath = true;

        public bool IsDestroyed => currentHealth <= 0;

        public System.Action<DefenseTarget> OnDestroyed;

        private void Awake()
        {
            if (currentHealth <= 0)
            {
                currentHealth = maxHealth;
            }
        }

        public void ResetHealth(int newMaxHealth)
        {
            maxHealth = Mathf.Max(1, newMaxHealth);
            currentHealth = maxHealth;
        }

        public void TakeDamage(int damage, Vector3 damageSource, float knockback)
        {
            if (damage <= 0 || IsDestroyed)
            {
                return;
            }

            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                HandleDestroyed();
            }
        }

        private void HandleDestroyed()
        {
            if (!string.IsNullOrEmpty(defenseTargetId))
            {
                GameEvents.DefenseTargetDestroyed(defenseTargetId);
            }

            OnDestroyed?.Invoke(this);
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}
