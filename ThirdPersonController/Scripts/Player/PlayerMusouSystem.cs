using UnityEngine;

namespace ThirdPersonController
{
    public class PlayerMusouSystem : MonoBehaviour
    {
        [Header("Meter")]
        public float maxMusou = 100f;
        public float currentMusou = 0f;
        public float gainPerDamageDealt = 0.2f;
        public float gainPerDamageTaken = 0.5f;
        public bool gainWhileActive = false;

        [Header("Activation")]
        public KeyCode musouKey = KeyCode.V;
        public bool requireFullMeter = true;
        public bool allowActivationWhileAttacking = true;

        [Header("Burst")]
        public float duration = 8f;
        public float damageMultiplier = 1.8f;
        public float rangeMultiplier = 1.5f;
        public float knockbackMultiplier = 1.4f;
        public bool clearMeterOnActivate = true;

        [Header("Fatigue")]
        public float fatigueDuration = 2.5f;
        public float fatigueDamageMultiplier = 0.85f;
        public float fatigueRangeMultiplier = 0.9f;
        public float fatigueKnockbackMultiplier = 0.9f;

        private PlayerActionController actionController;
        private PlayerHealth playerHealth;
        private PlayerStatsController statsController;

        private bool isActive;
        private bool isFatigued;
        private float activeTimer;
        private float fatigueTimer;
        [SerializeField] private bool isUnlocked = true;

        public bool IsActive => isActive;
        public bool IsFatigued => isFatigued;
        public bool IsReady => currentMusou >= maxMusou;
        public bool IsUnlocked => isUnlocked;
        public float Normalized => maxMusou <= 0f ? 0f : Mathf.Clamp01(currentMusou / maxMusou);

        public float DamageMultiplier => isActive ? damageMultiplier : (isFatigued ? fatigueDamageMultiplier : 1f);
        public float RangeMultiplier => isActive ? rangeMultiplier : (isFatigued ? fatigueRangeMultiplier : 1f);
        public float KnockbackMultiplier => isActive ? knockbackMultiplier : (isFatigued ? fatigueKnockbackMultiplier : 1f);

        private void Awake()
        {
            actionController = GetComponent<PlayerActionController>();
            playerHealth = GetComponent<PlayerHealth>();
            statsController = GetComponent<PlayerStatsController>();
            ClampMusou();
        }

        private void OnEnable()
        {
            GameEvents.OnDamageDealt += HandleDamageDealt;
            GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
            GameEvents.OnPlayerRespawn += HandlePlayerRespawn;
            GameEvents.MusouChanged(currentMusou, maxMusou);
        }

        private void OnDisable()
        {
            GameEvents.OnDamageDealt -= HandleDamageDealt;
            GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            GameEvents.OnPlayerRespawn -= HandlePlayerRespawn;
        }

        private void Update()
        {
            if (!isUnlocked)
            {
                return;
            }

            HandleInput();
            UpdateTimers();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(musouKey))
            {
                TryActivate();
            }
        }

        private void UpdateTimers()
        {
            if (isActive)
            {
                activeTimer -= Time.deltaTime;
                if (activeTimer <= 0f)
                {
                    EndMusou();
                }
            }

            if (isFatigued)
            {
                fatigueTimer -= Time.deltaTime;
                if (fatigueTimer <= 0f)
                {
                    EndFatigue();
                }
            }
        }

        public bool TryActivate()
        {
            if (!isUnlocked)
            {
                return false;
            }

            if (isActive)
            {
                return false;
            }

            if (playerHealth != null && playerHealth.IsDead)
            {
                return false;
            }

            if (requireFullMeter && !IsReady)
            {
                return false;
            }

            if (!allowActivationWhileAttacking && actionController != null
                && actionController.CurrentState == PlayerActionState.Attack)
            {
                return false;
            }

            Activate();
            return true;
        }

        private void Activate()
        {
            isActive = true;
            activeTimer = Mathf.Max(0.1f, duration);
            if (clearMeterOnActivate)
            {
                currentMusou = 0f;
                GameEvents.MusouChanged(currentMusou, maxMusou);
            }

            GameEvents.MusouStateChanged(true);
        }

        private void EndMusou()
        {
            if (!isActive)
            {
                return;
            }

            isActive = false;
            GameEvents.MusouStateChanged(false);

            if (fatigueDuration > 0f)
            {
                StartFatigue();
            }
        }

        private void StartFatigue()
        {
            isFatigued = true;
            fatigueTimer = fatigueDuration;
            GameEvents.MusouFatigueStateChanged(true);
        }

        private void EndFatigue()
        {
            if (!isFatigued)
            {
                return;
            }

            isFatigued = false;
            GameEvents.MusouFatigueStateChanged(false);
        }

        public void AddMusou(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            if (!isUnlocked)
            {
                return;
            }

            if (isActive && !gainWhileActive)
            {
                return;
            }

            if (statsController != null)
            {
                amount *= statsController.GetMusouGainMultiplier();
            }

            float previous = currentMusou;
            currentMusou = Mathf.Clamp(currentMusou + amount, 0f, maxMusou);
            if (!Mathf.Approximately(previous, currentMusou))
            {
                GameEvents.MusouChanged(currentMusou, maxMusou);
            }
        }

        public void ResetMusou()
        {
            currentMusou = 0f;
            GameEvents.MusouChanged(currentMusou, maxMusou);
        }

        private void HandleDamageDealt(int damage, Vector3 position, bool isCritical)
        {
            if (damage <= 0)
            {
                return;
            }

            AddMusou(damage * gainPerDamageDealt);
        }

        private void HandlePlayerDamaged(float damage, Vector3 source)
        {
            if (damage <= 0f)
            {
                return;
            }

            AddMusou(damage * gainPerDamageTaken);
        }

        private void HandlePlayerDeath()
        {
            isActive = false;
            isFatigued = false;
            activeTimer = 0f;
            fatigueTimer = 0f;
            ResetMusou();
            GameEvents.MusouStateChanged(false);
            GameEvents.MusouFatigueStateChanged(false);
        }

        private void HandlePlayerRespawn()
        {
            ResetMusou();
        }

        private void ClampMusou()
        {
            if (maxMusou <= 0f)
            {
                maxMusou = 1f;
            }

            currentMusou = Mathf.Clamp(currentMusou, 0f, maxMusou);
        }

        public void SetUnlocked(bool unlocked)
        {
            if (isUnlocked == unlocked)
            {
                return;
            }

            isUnlocked = unlocked;
            if (!isUnlocked)
            {
                isActive = false;
                isFatigued = false;
                activeTimer = 0f;
                fatigueTimer = 0f;
                ResetMusou();
                GameEvents.MusouStateChanged(false);
                GameEvents.MusouFatigueStateChanged(false);
            }
            else
            {
                GameEvents.MusouChanged(currentMusou, maxMusou);
            }
        }
    }
}
