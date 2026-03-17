using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace ThirdPersonController.Editor
{
    public static class BossPrefabRound2Builder
    {
        private const string SourcePrefabPath = "Assets/Prefabs/Enemies/ENM_Starman_01.prefab";
        private const string BossFolderPath = "Assets/Prefabs/Bosses";
        private const string EelBossPrefabPath = BossFolderPath + "/BOSS_Eel_Controller.prefab";
        private const string GuardianBossPrefabPath = BossFolderPath + "/BOSS_Guardian_Controller.prefab";

        [MenuItem("Tools/Boss/Round2/Create Controller Boss Prefabs")]
        public static void CreateControllerBossPrefabs()
        {
            CreateControllerBossPrefabsInternal(true);
        }

        public static void CreateControllerBossPrefabsForBatch()
        {
            CreateControllerBossPrefabsInternal(false);
        }

        private static void CreateControllerBossPrefabsInternal(bool interactive)
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (sourcePrefab == null)
            {
                string message = $"Missing source prefab: {SourcePrefabPath}";
                Debug.LogError(message);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Boss Prefab Build", message, "OK");
                }

                return;
            }

            EnsureFolder(BossFolderPath);

            BuildEelBossPrefab(sourcePrefab);
            BuildGuardianBossPrefab(sourcePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BossRound2] Built boss prefabs:\n- {EelBossPrefabPath}\n- {GuardianBossPrefabPath}");
            if (interactive)
            {
                EditorUtility.DisplayDialog(
                    "Boss Prefab Build",
                    "Round2 Boss controller prefabs created successfully.\n\n" +
                    $"- {EelBossPrefabPath}\n" +
                    $"- {GuardianBossPrefabPath}",
                    "OK");
            }
        }

        private static void BuildEelBossPrefab(GameObject sourcePrefab)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[BossRound2] Failed to instantiate source prefab for eel boss.");
                return;
            }

            try
            {
                instance.name = "BOSS_Eel_Controller";
                SetLayerRecursively(instance, LayerMask.NameToLayer("Enemy"));

                Rigidbody rb = EnsureComponent<Rigidbody>(instance);
                rb.mass = 180f;
                rb.drag = 0f;
                rb.angularDrag = 0.05f;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

                CapsuleCollider capsule = EnsureComponent<CapsuleCollider>(instance);
                capsule.center = new Vector3(0f, 1.05f, 0f);
                capsule.radius = 0.78f;
                capsule.height = 2.15f;
                capsule.direction = 1;

                NavMeshAgent agent = EnsureComponent<NavMeshAgent>(instance);
                agent.speed = 3.4f;
                agent.angularSpeed = 360f;
                agent.acceleration = 12f;
                agent.stoppingDistance = 2.4f;
                agent.radius = 0.75f;
                agent.height = 2.15f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

                EnemyHealth health = EnsureComponent<EnemyHealth>(instance);
                health.enemyType = EnemyType.Boss;
                health.maxHealth = 5200;
                health.expReward = 420;
                health.defense = 8f;
                health.hitStunDuration = 0.08f;
                health.dropChance = 0f;

                EnemyAI ai = EnsureComponent<EnemyAI>(instance);
                ai.animator = EnsureComponent<Animator>(instance);
                ai.useCrowdCoordinator = false;
                ai.detectionRange = 32f;
                ai.attackRange = 2.2f;
                ai.patrolSpeed = 2f;
                ai.chaseSpeed = 3.4f;
                ai.stoppingDistance = 2.4f;
                ai.attackCooldown = 12f;
                ai.attackDamage = 1;
                ai.attackKnockback = 0f;
                ai.playerLayer = LayerMask.GetMask("Player");
                ai.obstructionLayer = LayerMask.GetMask("Default", "Ground", "Environment");
                ai.canDodge = false;
                ai.canBlock = false;
                ai.canCharge = false;
                ai.canFlee = false;

                RemoveBossPrototypeComponents(instance);
                BossController controller = EnsureComponent<BossController>(instance);
                controller.health = health;
                controller.ai = ai;
                controller.animator = ai.animator;
                controller.maxHealth = health.maxHealth;
                controller.usePhases = true;
                controller.currentPhase = 1;
                controller.attackInterval = 2.8f;
                controller.decisionInterval = 0.65f;
                controller.useAttackQueue = true;
                controller.queuedAttackLimit = 3;
                controller.maxSameAttackQueued = 1;
                controller.immediateRepeatPenalty = 0.3f;
                controller.prioritizeSpecialAttacksWhenEnraged = true;

                controller.enableBreakWindow = true;
                controller.staggerMax = 140f;
                controller.staggerPerDamage = 1.0f;
                controller.breakWindowDuration = 4.2f;
                controller.breakWindowCooldown = 10f;
                controller.breakWindowDamageMultiplier = 1.65f;
                controller.forceKnockdownDuringBreak = true;
                controller.allowHeavyKnockdownOutsideBreak = false;
                controller.breakTrigger = "Break";

                controller.hasWeakness = true;
                controller.weaknessElement = "electric";
                controller.weaknessMultiplier = 1.7f;

                controller.phases = new List<BossPhase>
                {
                    new BossPhase
                    {
                        phaseName = "Calm Tide",
                        healthPercentThreshold = 1f,
                        timeScale = 1f,
                        damageMultiplier = 1f,
                        speedMultiplier = 1f,
                        defenseMultiplier = 1f,
                        unlockSpecialAttacks = false
                    },
                    new BossPhase
                    {
                        phaseName = "Raging Current",
                        healthPercentThreshold = 0.68f,
                        timeScale = 1f,
                        damageMultiplier = 1.14f,
                        speedMultiplier = 1.08f,
                        defenseMultiplier = 1.1f,
                        unlockSpecialAttacks = true,
                        unlockedAttacks = new List<string> { "eel_charge", "eel_vortex" },
                        phaseColor = new Color(1f, 0.58f, 0.12f, 1f)
                    },
                    new BossPhase
                    {
                        phaseName = "Abyss Frenzy",
                        healthPercentThreshold = 0.35f,
                        timeScale = 1f,
                        damageMultiplier = 1.28f,
                        speedMultiplier = 1.16f,
                        defenseMultiplier = 1.18f,
                        unlockSpecialAttacks = true,
                        unlockedAttacks = new List<string> { "eel_charge", "eel_vortex", "eel_devour" },
                        phaseColor = new Color(1f, 0.24f, 0.12f, 1f)
                    }
                };

                controller.attacks = new List<BossAttack>
                {
                    new BossAttack
                    {
                        attackId = "eel_tail",
                        attackName = "Tail Sweep",
                        damage = 82f,
                        range = 4.8f,
                        cooldown = 4.2f,
                        windupTime = 0.32f,
                        activeTime = 0.24f,
                        recoveryTime = 0.5f,
                        knockbackForce = 7.5f,
                        selectionWeight = 1.2f,
                        targetPlayer = true,
                        aoe = false,
                        isSpecial = false
                    },
                    new BossAttack
                    {
                        attackId = "eel_charge",
                        attackName = "Piercing Charge",
                        damage = 96f,
                        range = 10.5f,
                        cooldown = 6.4f,
                        windupTime = 0.55f,
                        activeTime = 0.3f,
                        recoveryTime = 0.68f,
                        knockbackForce = 10f,
                        selectionWeight = 1.05f,
                        targetPlayer = true,
                        aoe = false,
                        isSpecial = true,
                        requiresPhase2 = true
                    },
                    new BossAttack
                    {
                        attackId = "eel_vortex",
                        attackName = "Abyss Vortex",
                        damage = 74f,
                        range = 6.5f,
                        cooldown = 8.6f,
                        windupTime = 0.72f,
                        activeTime = 0.36f,
                        recoveryTime = 0.82f,
                        knockbackForce = 6.5f,
                        selectionWeight = 0.95f,
                        targetPlayer = false,
                        aoe = true,
                        aoeRadius = 6.5f,
                        isSpecial = true,
                        requiresPhase2 = true
                    },
                    new BossAttack
                    {
                        attackId = "eel_devour",
                        attackName = "Devour Bite",
                        damage = 134f,
                        range = 4.6f,
                        cooldown = 10.8f,
                        windupTime = 0.8f,
                        activeTime = 0.22f,
                        recoveryTime = 0.92f,
                        knockbackForce = 12f,
                        selectionWeight = 0.88f,
                        targetPlayer = true,
                        aoe = false,
                        isSpecial = true,
                        requiresPhase3 = true
                    }
                };

                PrefabUtility.SaveAsPrefabAsset(instance, EelBossPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void BuildGuardianBossPrefab(GameObject sourcePrefab)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[BossRound2] Failed to instantiate source prefab for guardian boss.");
                return;
            }

            try
            {
                instance.name = "BOSS_Guardian_Controller";
                SetLayerRecursively(instance, LayerMask.NameToLayer("Enemy"));

                Rigidbody rb = EnsureComponent<Rigidbody>(instance);
                rb.mass = 220f;
                rb.drag = 0f;
                rb.angularDrag = 0.05f;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

                CapsuleCollider capsule = EnsureComponent<CapsuleCollider>(instance);
                capsule.center = new Vector3(0f, 1.12f, 0f);
                capsule.radius = 0.84f;
                capsule.height = 2.3f;
                capsule.direction = 1;

                NavMeshAgent agent = EnsureComponent<NavMeshAgent>(instance);
                agent.speed = 2.9f;
                agent.angularSpeed = 300f;
                agent.acceleration = 10f;
                agent.stoppingDistance = 2.8f;
                agent.radius = 0.82f;
                agent.height = 2.3f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

                EnemyHealth health = EnsureComponent<EnemyHealth>(instance);
                health.enemyType = EnemyType.Boss;
                health.maxHealth = 6200;
                health.expReward = 520;
                health.defense = 11f;
                health.hitStunDuration = 0.06f;
                health.dropChance = 0f;

                EnemyAI ai = EnsureComponent<EnemyAI>(instance);
                ai.animator = EnsureComponent<Animator>(instance);
                ai.useCrowdCoordinator = false;
                ai.detectionRange = 30f;
                ai.attackRange = 2.5f;
                ai.patrolSpeed = 1.8f;
                ai.chaseSpeed = 2.9f;
                ai.stoppingDistance = 2.8f;
                ai.attackCooldown = 14f;
                ai.attackDamage = 1;
                ai.attackKnockback = 0f;
                ai.playerLayer = LayerMask.GetMask("Player");
                ai.obstructionLayer = LayerMask.GetMask("Default", "Ground", "Environment");
                ai.canDodge = false;
                ai.canBlock = false;
                ai.canCharge = false;
                ai.canFlee = false;

                RemoveBossPrototypeComponents(instance);
                BossController controller = EnsureComponent<BossController>(instance);
                controller.health = health;
                controller.ai = ai;
                controller.animator = ai.animator;
                controller.maxHealth = health.maxHealth;
                controller.usePhases = true;
                controller.currentPhase = 1;
                controller.attackInterval = 3.15f;
                controller.decisionInterval = 0.75f;
                controller.useAttackQueue = true;
                controller.queuedAttackLimit = 3;
                controller.maxSameAttackQueued = 1;
                controller.immediateRepeatPenalty = 0.28f;
                controller.prioritizeSpecialAttacksWhenEnraged = true;

                controller.enableBreakWindow = true;
                controller.staggerMax = 165f;
                controller.staggerPerDamage = 0.95f;
                controller.breakWindowDuration = 4.8f;
                controller.breakWindowCooldown = 11.5f;
                controller.breakWindowDamageMultiplier = 1.7f;
                controller.forceKnockdownDuringBreak = true;
                controller.allowHeavyKnockdownOutsideBreak = false;
                controller.breakTrigger = "Break";

                controller.hasWeakness = true;
                controller.weaknessElement = "heat";
                controller.weaknessMultiplier = 1.65f;

                controller.phases = new List<BossPhase>
                {
                    new BossPhase
                    {
                        phaseName = "Fortified Shell",
                        healthPercentThreshold = 1f,
                        timeScale = 1f,
                        damageMultiplier = 1f,
                        speedMultiplier = 1f,
                        defenseMultiplier = 1f,
                        unlockSpecialAttacks = false
                    },
                    new BossPhase
                    {
                        phaseName = "Overload Core",
                        healthPercentThreshold = 0.7f,
                        timeScale = 1f,
                        damageMultiplier = 1.15f,
                        speedMultiplier = 1.06f,
                        defenseMultiplier = 1.12f,
                        unlockSpecialAttacks = true,
                        unlockedAttacks = new List<string> { "guard_overload", "guard_spray" },
                        phaseColor = new Color(1f, 0.62f, 0.22f, 1f)
                    },
                    new BossPhase
                    {
                        phaseName = "Meltdown March",
                        healthPercentThreshold = 0.32f,
                        timeScale = 1f,
                        damageMultiplier = 1.32f,
                        speedMultiplier = 1.12f,
                        defenseMultiplier = 1.2f,
                        unlockSpecialAttacks = true,
                        unlockedAttacks = new List<string> { "guard_overload", "guard_spray", "guard_blade" },
                        phaseColor = new Color(1f, 0.35f, 0.1f, 1f)
                    }
                };

                controller.attacks = new List<BossAttack>
                {
                    new BossAttack
                    {
                        attackId = "guard_slam",
                        attackName = "Shield Slam",
                        damage = 92f,
                        range = 5f,
                        cooldown = 4.8f,
                        windupTime = 0.42f,
                        activeTime = 0.26f,
                        recoveryTime = 0.58f,
                        knockbackForce = 9f,
                        selectionWeight = 1.2f,
                        targetPlayer = false,
                        aoe = true,
                        aoeRadius = 5f,
                        isSpecial = false
                    },
                    new BossAttack
                    {
                        attackId = "guard_spray",
                        attackName = "Corrosion Spray",
                        damage = 72f,
                        range = 7f,
                        cooldown = 6.2f,
                        windupTime = 0.58f,
                        activeTime = 0.35f,
                        recoveryTime = 0.78f,
                        knockbackForce = 6f,
                        selectionWeight = 1.0f,
                        targetPlayer = false,
                        aoe = true,
                        aoeRadius = 7f,
                        isSpecial = true,
                        requiresPhase2 = true
                    },
                    new BossAttack
                    {
                        attackId = "guard_overload",
                        attackName = "Overload Burst",
                        damage = 122f,
                        range = 7.8f,
                        cooldown = 9.2f,
                        windupTime = 0.76f,
                        activeTime = 0.34f,
                        recoveryTime = 0.9f,
                        knockbackForce = 11.5f,
                        selectionWeight = 0.92f,
                        targetPlayer = false,
                        aoe = true,
                        aoeRadius = 7.8f,
                        isSpecial = true,
                        requiresPhase2 = true
                    },
                    new BossAttack
                    {
                        attackId = "guard_blade",
                        attackName = "Blade Sweep",
                        damage = 112f,
                        range = 5.6f,
                        cooldown = 7.2f,
                        windupTime = 0.5f,
                        activeTime = 0.24f,
                        recoveryTime = 0.7f,
                        knockbackForce = 10f,
                        selectionWeight = 0.96f,
                        targetPlayer = true,
                        aoe = false,
                        isSpecial = true,
                        requiresPhase3 = true
                    }
                };

                PrefabUtility.SaveAsPrefabAsset(instance, GuardianBossPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void RemoveBossPrototypeComponents(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            BossEelPrototype eel = root.GetComponent<BossEelPrototype>();
            if (eel != null)
            {
                Object.DestroyImmediate(eel, true);
            }

            BossGuardianPrototype guardian = root.GetComponent<BossGuardianPrototype>();
            if (guardian != null)
            {
                Object.DestroyImmediate(guardian, true);
            }

            BossCombatTemplate template = root.GetComponent<BossCombatTemplate>();
            if (template != null)
            {
                Object.DestroyImmediate(template, true);
            }
        }

        private static void EnsureFolder(string targetPath)
        {
            if (AssetDatabase.IsValidFolder(targetPath))
            {
                return;
            }

            string[] parts = targetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null || layer < 0)
            {
                return;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].gameObject.layer = layer;
            }
        }
    }
}
