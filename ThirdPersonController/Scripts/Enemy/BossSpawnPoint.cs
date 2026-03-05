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
        private const string DefaultBossPrefabPath = "Assets/Prefabs/Enemies/ENM_Starman_01.prefab";

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
                bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBossPrefabPath);
            }
#endif
        }

        public void SpawnBoss()
        {
            if (bossPrefab == null || spawnedBoss != null || isDefeated)
            {
                return;
            }

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

            BossCombatTemplate template = spawnedBoss.GetComponent<BossCombatTemplate>();
            if (template == null)
            {
                template = spawnedBoss.AddComponent<BossCombatTemplate>();
            }
            template.baseDamage = baseDamage;
            template.baseKnockback = knockback;

            AttachPrototype(spawnedBoss);

            UI_BossHealthBar ui = bossHealthBar != null ? bossHealthBar : EnsureBossHealthUI();
            if (ui != null)
            {
                ui.SetupBoss(template);
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
