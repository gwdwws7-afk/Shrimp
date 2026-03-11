using UnityEngine;

namespace ThirdPersonController
{
    public class ConsumableUseSystem : MonoBehaviour
    {
        public ConsumableInventory inventory;
        public ConsumableCatalog catalog;
        public bool showMessages = true;
        public float useCooldown = 1.5f;

        private PlayerHealth playerHealth;
        private StaminaSystem staminaSystem;
        private float nextUseTime = 0f;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = ConsumableInventory.EnsureInstance();
            }

            if (catalog == null)
            {
                catalog = Resources.Load<ConsumableCatalog>("ConsumableCatalog") ?? ConsumableCatalog.CreateDefault();
            }
        }

        private void Start()
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
            staminaSystem = FindObjectOfType<StaminaSystem>();
        }

        public bool UseConsumable(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            if (useCooldown > 0f && Time.time < nextUseTime)
            {
                if (showMessages)
                {
                    GameEvents.ShowMessage("提示", 1f);
                }
                return false;
            }

            ConsumableDefinition item = catalog != null ? catalog.GetById(itemId) : null;
            if (item == null)
            {
                return false;
            }

            if (inventory == null || !inventory.Consume(itemId, 1))
            {
                return false;
            }

            ApplyEffect(item);
            if (useCooldown > 0f)
            {
                nextUseTime = Time.time + useCooldown;
            }
            return true;
        }

        private void ApplyEffect(ConsumableDefinition item)
        {
            if (item == null)
            {
                return;
            }

            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
            }

            if (staminaSystem == null)
            {
                staminaSystem = FindObjectOfType<StaminaSystem>();
            }

            switch (item.effectType)
            {
                case ConsumableEffectType.HealFlat:
                    if (playerHealth != null)
                    {
                        playerHealth.Heal(Mathf.RoundToInt(item.amount));
                    }
                    break;
                case ConsumableEffectType.HealPercent:
                    if (playerHealth != null)
                    {
                        int healAmount = Mathf.RoundToInt(playerHealth.MaxHealth * Mathf.Clamp01(item.amount));
                        playerHealth.Heal(healAmount);
                    }
                    break;
                case ConsumableEffectType.RestoreStaminaFlat:
                    if (staminaSystem != null)
                    {
                        staminaSystem.RecoverStamina(item.amount);
                    }
                    break;
                case ConsumableEffectType.RestoreStaminaPercent:
                    if (staminaSystem != null)
                    {
                        float restore = staminaSystem.maxStamina * Mathf.Clamp01(item.amount);
                        staminaSystem.RecoverStamina(restore);
                    }
                    break;
                case ConsumableEffectType.DamageReduction:
                    if (playerHealth != null)
                    {
                        playerHealth.ApplyDamageReduction(Mathf.Clamp01(item.amount), Mathf.Max(0.1f, item.duration));
                    }
                    break;
                case ConsumableEffectType.Invincibility:
                    if (playerHealth != null)
                    {
                        playerHealth.ApplyInvincibility(Mathf.Max(0.1f, item.duration));
                    }
                    break;
            }

            if (showMessages)
            {
                GameEvents.ShowMessage($"已使用 {item.displayName}", 1.2f);
            }
        }
    }
}
