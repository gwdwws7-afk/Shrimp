using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ThirdPersonController
{
    public enum BossPrototypeType
    {
        Eel,
        Guardian
    }

    public class BossSpawnPoint : MonoBehaviour
    {
        private const string DefaultFallbackBossPrefabPath = "Assets/Prefabs/Enemies/ENM_Starman_01.prefab";
        private const string DefaultEelBossPrefabPath = "Assets/Prefabs/Bosses/BOSS_Eel_Controller.prefab";
        private const string DefaultGuardianBossPrefabPath = "Assets/Prefabs/Bosses/BOSS_Guardian_Controller.prefab";

        [Header("Spawn")]
        public GameObject bossPrefab;
        public BossPrototypeType prototype = BossPrototypeType.Eel;
        public string bossName = "Boss";
        public bool spawnOnStart = true;
        public Vector3 spawnOffset = Vector3.zero;
        public float scaleMultiplier = 2.2f;

        [Header("Stats")]
        public int maxHealth = 3000;
        public int expReward = 300;
        public int baseDamage = 25;
        public float knockback = 6f;

        [Header("Encounter Profile")]
        public BossEncounterProfile encounterProfile;
        public bool applyEncounterProfile = true;
        public bool logEncounterProfileApply = false;

        [Header("Encounter Tuning")]
        public bool overrideEncounterTuning = false;
        public float phase2HealthThreshold = 0.66f;
        public float phase3HealthThreshold = 0.33f;
        public float breakWindowDuration = 4f;
        public float breakWindowCooldown = 12f;
        public float breakWindowDamageMultiplier = 1.6f;
        public float staggerMax = 120f;
        public float staggerPerDamage = 1f;
        public float attackInterval = 3.2f;
        public float decisionInterval = 0.78f;
        public int queuedAttackLimit = 3;
        public float immediateRepeatPenalty = 0.32f;
        public bool enablePostBreakPunishWindow = true;
        public float postBreakPunishDuration = 5f;
        public float postBreakAttackIntervalMultiplier = 0.75f;
        public float postBreakDecisionIntervalMultiplier = 0.82f;
        public float postBreakChaseSpeedMultiplier = 1.15f;
        public bool enablePhaseComboChain = true;
        public float phase2ComboChance = 0.45f;
        public float phase3ComboChance = 0.65f;
        public float comboStartDelay = 0.08f;
        public float comboRepeatPenalty = 0.35f;
        public bool enableInterruptRecoveryGate = true;
        public float interruptRecoveryDuration = 0.2f;
        public float interruptedAttackCooldownScale = 0.45f;
        public bool enableTimePressure = true;
        public float timePressureDelay = 75f;
        public float timePressureRampDuration = 60f;
        public float maxTimePressureDamageMultiplier = 1.35f;
        public float maxTimePressureSpeedMultiplier = 1.2f;
        public bool enablePhaseTransitionOpeners = true;
        public string phase2TransitionOpenerId = "";
        public string phase3TransitionOpenerId = "";
        public bool enablePhaseTransitionOpenerRetry = true;
        public float phaseTransitionOpenerRetryDelay = 0.12f;
        public int phaseTransitionOpenerMaxRetries = 3;
        public bool enablePhaseTransitionFollowupChain = false;
        public string phase2TransitionFollowupId = "";
        public string phase3TransitionFollowupId = "";
        public bool enablePhaseTransitionFollowupRetry = true;
        public float phaseTransitionFollowupRetryDelay = 0.12f;
        public int phaseTransitionFollowupMaxRetries = 2;
        public bool enablePhase3SpecialPriorityWindow = true;
        public float phase3SpecialPriorityDuration = 6f;
        public float phase3SpecialPriorityWeightMultiplier = 1.7f;
        public bool forceSpecialQueueDuringPhase3Priority = true;

        [Header("UI")]
        public UI_BossHealthBar bossHealthBar;

        private GameObject spawnedBoss;
        private bool hasSpawned;
        private bool isDefeated;
        private EnemyHealth cachedHealth;

        public bool HasSpawned => hasSpawned;
        public bool IsDefeated => isDefeated;

        public System.Action<BossSpawnPoint> OnBossDefeated;

        private void Reset()
        {
            TryAssignDefaultPrefab();
        }

        private void OnValidate()
        {
            TryAssignDefaultPrefab();
        }

        private void Start()
        {
            TryAssignDefaultPrefab();
            if (spawnOnStart)
            {
                SpawnBoss();
            }
        }

        private void TryAssignDefaultPrefab()
        {
#if UNITY_EDITOR
            if (bossPrefab == null)
            {
                string preferredPath = ResolveDefaultPrefabPathByPrototype();
                bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(preferredPath);
                if (bossPrefab == null && !string.Equals(preferredPath, DefaultFallbackBossPrefabPath))
                {
                    bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultFallbackBossPrefabPath);
                }
            }
#endif
        }

        private string ResolveDefaultPrefabPathByPrototype()
        {
            switch (prototype)
            {
                case BossPrototypeType.Guardian:
                    return DefaultGuardianBossPrefabPath;
                case BossPrototypeType.Eel:
                default:
                    return DefaultEelBossPrefabPath;
            }
        }

        private void ApplyEncounterProfileIfNeeded()
        {
            if (!applyEncounterProfile || encounterProfile == null)
            {
                return;
            }

            encounterProfile.ApplyTo(this);
            if (logEncounterProfileApply)
            {
                Debug.Log($"[BossSpawnPoint] Applied encounter profile '{encounterProfile.name}' to '{name}'.");
            }
        }

        public void SpawnBoss()
        {
            if (bossPrefab == null || spawnedBoss != null || isDefeated)
            {
                return;
            }

            ApplyEncounterProfileIfNeeded();

            Vector3 spawnPosition = transform.position + spawnOffset;
            spawnedBoss = Instantiate(bossPrefab, spawnPosition, transform.rotation);
            spawnedBoss.name = bossName;
            spawnedBoss.transform.localScale *= scaleMultiplier;
            hasSpawned = true;

            cachedHealth = spawnedBoss.GetComponent<EnemyHealth>();
            if (cachedHealth != null)
            {
                cachedHealth.maxHealth = maxHealth;
                cachedHealth.expReward = expReward;
                cachedHealth.enemyType = EnemyType.Boss;
                cachedHealth.OnSpawned();
                cachedHealth.OnDeath += HandleBossDeath;
            }

            EnemyAI ai = spawnedBoss.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.attackDamage = Mathf.Max(1, baseDamage);
                ai.attackKnockback = knockback;
            }

            UI_BossHealthBar ui = bossHealthBar != null ? bossHealthBar : EnsureBossHealthUI();
            BossController controller = spawnedBoss.GetComponent<BossController>();
            if (controller != null)
            {
                ConfigureBossController(controller);
                DisablePrototypeTemplateWhenControllerMode(spawnedBoss);
                if (ui != null)
                {
                    ui.SetupBoss(controller);
                }

                return;
            }

            BossCombatTemplate template = spawnedBoss.GetComponent<BossCombatTemplate>();
            if (template == null)
            {
                template = spawnedBoss.AddComponent<BossCombatTemplate>();
            }
            template.baseDamage = baseDamage;
            template.baseKnockback = knockback;
            ApplyEncounterTuning(template);

            AttachPrototype(spawnedBoss);

            if (ui != null)
            {
                ui.SetupBoss(template);
            }
        }

        private void ConfigureBossController(BossController controller)
        {
            if (controller == null)
            {
                return;
            }

            controller.health = cachedHealth;
            controller.ai = spawnedBoss != null ? spawnedBoss.GetComponent<EnemyAI>() : null;
            if (controller.animator == null && spawnedBoss != null)
            {
                controller.animator = spawnedBoss.GetComponent<Animator>();
            }

            controller.maxHealth = maxHealth;
            controller.currentPhase = 1;
            ApplyEncounterTuning(controller);
            controller.enabled = true;
        }

        private void ApplyEncounterTuning(BossController controller)
        {
            if (!overrideEncounterTuning || controller == null)
            {
                return;
            }

            controller.breakWindowDuration = Mathf.Max(0f, breakWindowDuration);
            controller.breakWindowCooldown = Mathf.Max(0f, breakWindowCooldown);
            controller.breakWindowDamageMultiplier = Mathf.Max(1f, breakWindowDamageMultiplier);
            controller.staggerMax = Mathf.Max(1f, staggerMax);
            controller.staggerPerDamage = Mathf.Max(0f, staggerPerDamage);
            controller.attackInterval = Mathf.Max(0f, attackInterval);
            controller.decisionInterval = Mathf.Max(0.05f, decisionInterval);
            controller.queuedAttackLimit = Mathf.Max(1, queuedAttackLimit);
            controller.immediateRepeatPenalty = Mathf.Clamp01(immediateRepeatPenalty);
            controller.enablePostBreakPunishWindow = enablePostBreakPunishWindow;
            controller.postBreakPunishDuration = Mathf.Max(0f, postBreakPunishDuration);
            controller.postBreakAttackIntervalMultiplier = Mathf.Clamp(postBreakAttackIntervalMultiplier, 0.3f, 1f);
            controller.postBreakDecisionIntervalMultiplier = Mathf.Clamp(postBreakDecisionIntervalMultiplier, 0.3f, 1f);
            controller.postBreakChaseSpeedMultiplier = Mathf.Max(1f, postBreakChaseSpeedMultiplier);
            controller.enablePhaseComboChain = enablePhaseComboChain;
            controller.phase2ComboChance = Mathf.Clamp01(phase2ComboChance);
            controller.phase3ComboChance = Mathf.Clamp01(phase3ComboChance);
            controller.comboStartDelay = Mathf.Max(0f, comboStartDelay);
            controller.comboRepeatPenalty = Mathf.Clamp01(comboRepeatPenalty);
            controller.enableInterruptRecoveryGate = enableInterruptRecoveryGate;
            controller.interruptRecoveryDuration = Mathf.Max(0f, interruptRecoveryDuration);
            controller.interruptedAttackCooldownScale = Mathf.Clamp01(interruptedAttackCooldownScale);
            controller.enableTimePressure = enableTimePressure;
            controller.timePressureDelay = Mathf.Max(0f, timePressureDelay);
            controller.timePressureRampDuration = Mathf.Max(1f, timePressureRampDuration);
            controller.maxTimePressureDamageMultiplier = Mathf.Max(1f, maxTimePressureDamageMultiplier);
            controller.maxTimePressureSpeedMultiplier = Mathf.Max(1f, maxTimePressureSpeedMultiplier);
            controller.enablePhaseTransitionOpeners = enablePhaseTransitionOpeners;
            controller.phase2TransitionOpenerId = phase2TransitionOpenerId ?? string.Empty;
            controller.phase3TransitionOpenerId = phase3TransitionOpenerId ?? string.Empty;
            controller.enablePhaseTransitionOpenerRetry = enablePhaseTransitionOpenerRetry;
            controller.phaseTransitionOpenerRetryDelay = Mathf.Max(0f, phaseTransitionOpenerRetryDelay);
            controller.phaseTransitionOpenerMaxRetries = Mathf.Max(0, phaseTransitionOpenerMaxRetries);
            controller.enablePhaseTransitionFollowupChain = enablePhaseTransitionFollowupChain;
            controller.phase2TransitionFollowupId = phase2TransitionFollowupId ?? string.Empty;
            controller.phase3TransitionFollowupId = phase3TransitionFollowupId ?? string.Empty;
            controller.enablePhaseTransitionFollowupRetry = enablePhaseTransitionFollowupRetry;
            controller.phaseTransitionFollowupRetryDelay = Mathf.Max(0f, phaseTransitionFollowupRetryDelay);
            controller.phaseTransitionFollowupMaxRetries = Mathf.Max(0, phaseTransitionFollowupMaxRetries);
            controller.enablePhase3SpecialPriorityWindow = enablePhase3SpecialPriorityWindow;
            controller.phase3SpecialPriorityDuration = Mathf.Max(0f, phase3SpecialPriorityDuration);
            controller.phase3SpecialPriorityWeightMultiplier = Mathf.Max(1f, phase3SpecialPriorityWeightMultiplier);
            controller.forceSpecialQueueDuringPhase3Priority = forceSpecialQueueDuringPhase3Priority;
            ApplyPhaseThresholds(controller.phases);
        }

        private void ApplyEncounterTuning(BossCombatTemplate template)
        {
            if (!overrideEncounterTuning || template == null)
            {
                return;
            }

            template.phase2HealthThreshold = Mathf.Clamp(phase2HealthThreshold, 0.1f, 0.95f);
            template.breakWindowDuration = Mathf.Max(0f, breakWindowDuration);
            template.breakCooldown = Mathf.Max(0f, breakWindowCooldown);
            template.breakWindowDamageMultiplier = Mathf.Max(1f, breakWindowDamageMultiplier);
            template.staggerMax = Mathf.Max(1f, staggerMax);
            template.staggerPerDamage = Mathf.Max(0f, staggerPerDamage);
        }

        private void ApplyPhaseThresholds(System.Collections.Generic.List<BossPhase> phases)
        {
            if (phases == null || phases.Count == 0)
            {
                return;
            }

            float phase2 = Mathf.Clamp(phase2HealthThreshold, 0.1f, 0.95f);
            float phase3 = Mathf.Clamp(phase3HealthThreshold, 0.05f, phase2 - 0.05f);

            if (phases.Count > 1 && phases[1] != null)
            {
                phases[1].healthPercentThreshold = phase2;
            }

            if (phases.Count > 2 && phases[2] != null)
            {
                phases[2].healthPercentThreshold = phase3;
            }
        }

        private static void DisablePrototypeTemplateWhenControllerMode(GameObject bossObject)
        {
            if (bossObject == null)
            {
                return;
            }

            BossCombatTemplate template = bossObject.GetComponent<BossCombatTemplate>();
            if (template != null)
            {
                template.enabled = false;
            }
        }

        private void HandleBossDeath()
        {
            if (cachedHealth != null)
            {
                cachedHealth.OnDeath -= HandleBossDeath;
            }

            isDefeated = true;
            OnBossDefeated?.Invoke(this);
            GameEvents.BossDefeated(this);
        }

        private void AttachPrototype(GameObject bossObject)
        {
            switch (prototype)
            {
                case BossPrototypeType.Eel:
                    if (bossObject.GetComponent<BossEelPrototype>() == null)
                    {
                        bossObject.AddComponent<BossEelPrototype>();
                    }
                    break;
                case BossPrototypeType.Guardian:
                    if (bossObject.GetComponent<BossGuardianPrototype>() == null)
                    {
                        bossObject.AddComponent<BossGuardianPrototype>();
                    }
                    break;
            }
        }

        private UI_BossHealthBar EnsureBossHealthUI()
        {
            // In batch/headless test runs, creating TMP UI can trigger editor-only importer windows.
            // Skip dynamic UI creation to keep PlayMode gates deterministic in CI.
            if (Application.isBatchMode)
            {
                return null;
            }

            UI_BossHealthBar existing = FindObjectOfType<UI_BossHealthBar>();
            if (existing != null)
            {
                return existing;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            GameObject root = new GameObject("BossHealthBar");
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -24f);
            rootRect.sizeDelta = new Vector2(640f, 72f);

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            UI_BossHealthBar bar = root.AddComponent<UI_BossHealthBar>();
            bar.canvasGroup = canvasGroup;
            bar.barContainer = rootRect;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(root.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.6f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 1f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(bgObj.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = Color.red;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(4f, 10f);
            fillRect.offsetMax = new Vector2(-4f, -10f);

            GameObject nameObj = new GameObject("BossName");
            nameObj.transform.SetParent(root.transform, false);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 24f;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.color = Color.white;
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(0f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.anchoredPosition = new Vector2(8f, 0f);
            nameRect.sizeDelta = new Vector2(320f, 28f);

            GameObject hpObj = new GameObject("HealthText");
            hpObj.transform.SetParent(root.transform, false);
            TextMeshProUGUI hpText = hpObj.AddComponent<TextMeshProUGUI>();
            hpText.fontSize = 18f;
            hpText.alignment = TextAlignmentOptions.Right;
            hpText.color = new Color(1f, 1f, 1f, 0.85f);
            RectTransform hpRect = hpObj.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(1f, 1f);
            hpRect.anchorMax = new Vector2(1f, 1f);
            hpRect.pivot = new Vector2(1f, 1f);
            hpRect.anchoredPosition = new Vector2(-8f, 0f);
            hpRect.sizeDelta = new Vector2(180f, 24f);

            bar.healthBackgroundImage = bgImage;
            bar.healthFillImage = fillImage;
            bar.bossNameText = nameText;
            bar.healthText = hpText;

            return bar;
        }
    }
}
