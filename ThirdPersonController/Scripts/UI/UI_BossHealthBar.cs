using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ThirdPersonController
{
    public class UI_BossHealthBar : MonoBehaviour
    {
        [Header("References")]
        public RectTransform barContainer;
        public Image healthFillImage;
        public Image healthBackgroundImage;
        public TextMeshProUGUI bossNameText;
        public TextMeshProUGUI healthText;
        public CanvasGroup canvasGroup;

        [Header("Weakness")]
        public Image weaknessIcon;
        public Sprite physicalWeaknessSprite;
        public Sprite heatWeaknessSprite;
        public Sprite electricWeaknessSprite;
        public Sprite toxinWeaknessSprite;
        public Sprite corrosionWeaknessSprite;
        public bool hideWeaknessIfNone = true;
        
        [Header("Settings")]
        public bool showOnStart = false;
        public float fadeInDuration = 0.5f;
        public float fadeOutDuration = 1f;
        public Color phase1Color = Color.red;
        public Color phase2Color = new Color(1f, 0.5f, 0f);
        public Color phase3Color = Color.magenta;
        
        [Header("Animation")]
        public float damageShakeDuration = 0.3f;
        public float damageShakeStrength = 10f;
        
        private BossController boss;
        private BossCombatTemplate bossTemplate;
        private EnemyHealth bossHealth;
        private int maxHealth;
        private int currentHealth;
        
        private void Awake()
        {
            if (!showOnStart && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }
        
        private void OnEnable()
        {
            FindBossAndSubscribe();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromBoss();
        }
        
        private void FindBossAndSubscribe()
        {
            BossController[] bosses = FindObjectsOfType<BossController>();
            
            if (bosses.Length > 0)
            {
                SetupBoss(bosses[0]);
                return;
            }

            BossCombatTemplate[] templates = FindObjectsOfType<BossCombatTemplate>();
            if (templates.Length > 0)
            {
                SetupBoss(templates[0]);
            }
        }
        
        public void SetupBoss(BossController newBoss)
        {
            UnsubscribeFromBoss();
            
            boss = newBoss;
            bossTemplate = null;
            
            if (boss == null) return;
            
            bossHealth = boss.GetComponent<EnemyHealth>();
            
            if (bossHealth != null)
            {
                bossHealth.OnDamageTaken += HandleDamageTaken;
                bossHealth.OnDeath += HandleDeath;
            }
            
            boss.OnPhaseChanged += HandlePhaseChanged;
            
            maxHealth = boss.maxHealth;
            currentHealth = maxHealth;
            
            if (bossNameText != null)
            {
                bossNameText.text = boss.name;
            }

            UpdateWeaknessIcon();
            
            Show();
            UpdateHealthBar();
        }

        public void SetupBoss(BossCombatTemplate newBoss)
        {
            UnsubscribeFromBoss();

            boss = null;
            bossTemplate = newBoss;

            if (bossTemplate == null)
            {
                return;
            }

            bossHealth = bossTemplate.GetComponent<EnemyHealth>();
            if (bossHealth != null)
            {
                bossHealth.OnDamageTaken += HandleDamageTaken;
                bossHealth.OnDeath += HandleDeath;
            }

            bossTemplate.OnPhaseChanged += HandleTemplatePhaseChanged;

            maxHealth = bossHealth != null ? bossHealth.MaxHealth : 0;
            currentHealth = maxHealth;

            if (bossNameText != null)
            {
                bossNameText.text = bossTemplate.name;
            }

            UpdateWeaknessIcon();

            Show();
            UpdateHealthBar();
        }
        
        private void UnsubscribeFromBoss()
        {
            if (boss != null)
            {
                boss.OnPhaseChanged -= HandlePhaseChanged;
            }

            if (bossTemplate != null)
            {
                bossTemplate.OnPhaseChanged -= HandleTemplatePhaseChanged;
            }
            
            if (bossHealth != null)
            {
                bossHealth.OnDamageTaken -= HandleDamageTaken;
                bossHealth.OnDeath -= HandleDeath;
            }
        }
        
        private void HandleDamageTaken(int damage, Vector3 source)
        {
            if (bossHealth != null)
            {
                currentHealth = bossHealth.CurrentHealth;
                UpdateHealthBar();
                ShakeBar();
            }
        }
        
        private void HandleDeath()
        {
            Hide();
        }
        
        private void HandlePhaseChanged(int newPhase)
        {
            Color phaseColor = phase1Color;
            
            switch (newPhase)
            {
                case 1:
                    phaseColor = phase1Color;
                    break;
                case 2:
                    phaseColor = phase2Color;
                    break;
                case 3:
                    phaseColor = phase3Color;
                    break;
            }
            
            if (healthFillImage != null)
            {
                healthFillImage.DOColor(phaseColor, 0.5f);
            }
            
            if (bossNameText != null)
            {
                bossNameText.DOColor(phaseColor, 0.5f);
            }
        }

        private void HandleTemplatePhaseChanged(BossCombatPhase phase)
        {
            Color phaseColor = phase1Color;
            switch (phase)
            {
                case BossCombatPhase.Phase1:
                    phaseColor = phase1Color;
                    break;
                case BossCombatPhase.Phase2:
                    phaseColor = phase2Color;
                    break;
            }

            if (healthFillImage != null)
            {
                healthFillImage.DOColor(phaseColor, 0.5f);
            }

            if (bossNameText != null)
            {
                bossNameText.DOColor(phaseColor, 0.5f);
            }
        }
        
        private void UpdateHealthBar()
        {
            if (healthFillImage != null)
            {
                float healthPercent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
                healthFillImage.fillAmount = healthPercent;
            }
            
            if (healthText != null)
            {
                healthText.text = $"{currentHealth} / {maxHealth}";
            }
        }

        private void UpdateWeaknessIcon()
        {
            if (weaknessIcon == null)
            {
                return;
            }

            DamageElementType element = DamageElementType.Physical;
            if (bossTemplate != null)
            {
                element = bossTemplate.GetWeakElementType(bossHealth);
            }
            else if (boss != null)
            {
                if (boss.hasWeakness && !string.IsNullOrEmpty(boss.weaknessElement))
                {
                    element = ParseWeaknessElement(boss.weaknessElement);
                }
                else
                {
                    element = ResolveWeaknessFromHealth(bossHealth);
                }
            }
            else
            {
                element = ResolveWeaknessFromHealth(bossHealth);
            }

            Sprite sprite = GetWeaknessSprite(element);
            if (sprite == null && hideWeaknessIfNone)
            {
                weaknessIcon.enabled = false;
                return;
            }

            weaknessIcon.enabled = true;
            weaknessIcon.sprite = sprite;
        }

        private DamageElementType ResolveWeaknessFromHealth(EnemyHealth health)
        {
            if (health == null)
            {
                return DamageElementType.Physical;
            }

            float min = health.resistPhysical;
            DamageElementType selected = DamageElementType.Physical;
            if (health.resistHeat < min)
            {
                min = health.resistHeat;
                selected = DamageElementType.Heat;
            }
            if (health.resistElectric < min)
            {
                min = health.resistElectric;
                selected = DamageElementType.Electric;
            }
            if (health.resistToxin < min)
            {
                min = health.resistToxin;
                selected = DamageElementType.Toxin;
            }
            if (health.resistCorrosion < min)
            {
                selected = DamageElementType.Corrosion;
            }

            return selected;
        }

        private DamageElementType ParseWeaknessElement(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return DamageElementType.Physical;
            }

            string value = raw.Trim().ToLowerInvariant();
            switch (value)
            {
                case "heat":
                case "fire":
                    return DamageElementType.Heat;
                case "electric":
                case "electricity":
                case "lightning":
                    return DamageElementType.Electric;
                case "toxin":
                case "poison":
                    return DamageElementType.Toxin;
                case "corrosion":
                case "acid":
                    return DamageElementType.Corrosion;
                default:
                    return DamageElementType.Physical;
            }
        }

        private Sprite GetWeaknessSprite(DamageElementType element)
        {
            switch (element)
            {
                case DamageElementType.Heat:
                    return heatWeaknessSprite;
                case DamageElementType.Electric:
                    return electricWeaknessSprite;
                case DamageElementType.Toxin:
                    return toxinWeaknessSprite;
                case DamageElementType.Corrosion:
                    return corrosionWeaknessSprite;
                default:
                    return physicalWeaknessSprite;
            }
        }
        
        private void ShakeBar()
        {
            if (barContainer != null)
            {
                barContainer.DOKill();
                barContainer.anchoredPosition = Vector2.zero;
                barContainer.DOShakePosition(damageShakeDuration, damageShakeStrength, 10, 90f);
            }
        }
        
        public void Show()
        {
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(1f, fadeInDuration);
            }
        }
        
        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, fadeOutDuration);
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromBoss();
        }
    }
}
