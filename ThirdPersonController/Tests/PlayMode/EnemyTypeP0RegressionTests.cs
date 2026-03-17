using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace ThirdPersonController.Tests
{
    public class EnemyTypeP0RegressionTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object obj = createdObjects[i];
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            createdObjects.Clear();

            if (ObjectPoolManager.Instance != null)
            {
                Object.DestroyImmediate(ObjectPoolManager.Instance.gameObject);
            }
        }

        [Test]
        public void EnemyArchetypeConfigurator_ApplyArchetype_AppliesCoreFieldsToComponents()
        {
            GameObject enemyGo = CreateTrackedGameObject("P0_Configurator_Target");
            EnemyAI ai = enemyGo.AddComponent<EnemyAI>();
            EnemyHealth health = enemyGo.GetComponent<EnemyHealth>();
            EnemyHitReaction hitReaction = enemyGo.AddComponent<EnemyHitReaction>();
            EnemyArchetypeConfigurator configurator = enemyGo.AddComponent<EnemyArchetypeConfigurator>();
            NavMeshAgent agent = enemyGo.GetComponent<NavMeshAgent>();

            EnemyArchetype archetype = CreateArchetype("rusher");
            archetype.maxHealth = 234;
            archetype.hitStunDuration = 0.27f;
            archetype.defense = 3.5f;
            archetype.expReward = 9;
            archetype.dropChance = 0.42f;
            archetype.enemyType = EnemyType.Rusher;
            archetype.resistPhysical = 0.2f;
            archetype.resistHeat = 0.15f;
            archetype.resistElectric = -0.1f;
            archetype.resistToxin = 0.25f;
            archetype.resistCorrosion = 0.3f;

            archetype.detectionRange = 21f;
            archetype.attackRange = 2.8f;
            archetype.fieldOfView = 145f;
            archetype.patrolSpeed = 3.3f;
            archetype.chaseSpeed = 6.7f;
            archetype.rotationSpeed = 7.5f;
            archetype.stoppingDistance = 1.9f;
            archetype.attackCooldown = 1.1f;
            archetype.attackDamage = 37;
            archetype.attackKnockback = 4.4f;
            archetype.attackWindup = 0.23f;
            archetype.attackActiveTime = 0.14f;
            archetype.attackRecovery = 0.32f;
            archetype.attackHitRadius = 1.6f;
            archetype.attackHitAngle = 150f;
            archetype.useAttackPatterns = true;
            archetype.attackPatterns = new List<EnemyAttackPattern>
            {
                new EnemyAttackPattern
                {
                    patternId = "p0_ranged",
                    isRanged = true,
                    damage = 18,
                    range = 7f,
                    cooldown = 2.2f
                }
            };

            archetype.useCrowdCoordinator = false;
            archetype.ringStandoffDistance = 3.8f;

            archetype.overrideAgentSettings = true;
            archetype.agentSpeed = 4.5f;
            archetype.agentAcceleration = 11f;
            archetype.agentAngularSpeed = 420f;
            archetype.agentStoppingDistance = 2.2f;
            archetype.agentRadius = 0.55f;
            archetype.agentHeight = 1.95f;

            archetype.overrideHitReaction = true;
            archetype.knockbackThreshold = 3.2f;
            archetype.knockdownThreshold = 7.7f;
            archetype.flinchDuration = 0.3f;
            archetype.knockbackDuration = 0.4f;
            archetype.knockbackDistance = 1.6f;
            archetype.knockdownDuration = 0.55f;
            archetype.knockdownDistance = 3.1f;
            archetype.knockdownRecoverTime = 0.85f;

            configurator.ApplyArchetype(archetype);

            Assert.AreSame(archetype, configurator.archetype);
            Assert.AreEqual(archetype.maxHealth, health.maxHealth);
            Assert.AreEqual(archetype.hitStunDuration, health.hitStunDuration, 0.0001f);
            Assert.AreEqual(archetype.defense, health.defense, 0.0001f);
            Assert.AreEqual(archetype.expReward, health.expReward);
            Assert.AreEqual(archetype.enemyType, health.enemyType);
            Assert.AreEqual(archetype.dropChance, health.dropChance, 0.0001f);
            Assert.AreEqual(archetype.resistPhysical, health.resistPhysical, 0.0001f);
            Assert.AreEqual(archetype.resistHeat, health.resistHeat, 0.0001f);
            Assert.AreEqual(archetype.resistElectric, health.resistElectric, 0.0001f);
            Assert.AreEqual(archetype.resistToxin, health.resistToxin, 0.0001f);
            Assert.AreEqual(archetype.resistCorrosion, health.resistCorrosion, 0.0001f);

            Assert.AreEqual(archetype.detectionRange, ai.detectionRange, 0.0001f);
            Assert.AreEqual(archetype.attackRange, ai.attackRange, 0.0001f);
            Assert.AreEqual(archetype.fieldOfView, ai.fieldOfView, 0.0001f);
            Assert.AreEqual(archetype.patrolSpeed, ai.patrolSpeed, 0.0001f);
            Assert.AreEqual(archetype.chaseSpeed, ai.chaseSpeed, 0.0001f);
            Assert.AreEqual(archetype.rotationSpeed, ai.rotationSpeed, 0.0001f);
            Assert.AreEqual(archetype.stoppingDistance, ai.stoppingDistance, 0.0001f);
            Assert.AreEqual(archetype.attackCooldown, ai.attackCooldown, 0.0001f);
            Assert.AreEqual(archetype.attackDamage, ai.attackDamage);
            Assert.AreEqual(archetype.attackKnockback, ai.attackKnockback, 0.0001f);
            Assert.AreEqual(archetype.attackWindup, ai.attackWindup, 0.0001f);
            Assert.AreEqual(archetype.attackActiveTime, ai.attackActiveTime, 0.0001f);
            Assert.AreEqual(archetype.attackRecovery, ai.attackRecovery, 0.0001f);
            Assert.AreEqual(archetype.attackHitRadius, ai.attackHitRadius, 0.0001f);
            Assert.AreEqual(archetype.attackHitAngle, ai.attackHitAngle, 0.0001f);
            Assert.AreEqual(archetype.useCrowdCoordinator, ai.useCrowdCoordinator);
            Assert.AreEqual(archetype.ringStandoffDistance, ai.ringStandoffDistance, 0.0001f);
            Assert.AreEqual(archetype.attackPatterns.Count, ai.attackPatterns.Count);
            Assert.AreNotSame(archetype.attackPatterns, ai.attackPatterns);

            Assert.AreEqual(archetype.agentSpeed, agent.speed, 0.0001f);
            Assert.AreEqual(archetype.agentAcceleration, agent.acceleration, 0.0001f);
            Assert.AreEqual(archetype.agentAngularSpeed, agent.angularSpeed, 0.0001f);
            Assert.AreEqual(ai.stoppingDistance, agent.stoppingDistance, 0.0001f);
            Assert.AreEqual(archetype.agentRadius, agent.radius, 0.0001f);
            Assert.AreEqual(archetype.agentHeight, agent.height, 0.0001f);

            Assert.AreEqual(archetype.knockbackThreshold, hitReaction.knockbackThreshold, 0.0001f);
            Assert.AreEqual(archetype.knockdownThreshold, hitReaction.knockdownThreshold, 0.0001f);
            Assert.AreEqual(archetype.flinchDuration, hitReaction.flinchDuration, 0.0001f);
            Assert.AreEqual(archetype.knockbackDuration, hitReaction.knockbackDuration, 0.0001f);
            Assert.AreEqual(archetype.knockbackDistance, hitReaction.knockbackDistance, 0.0001f);
            Assert.AreEqual(archetype.knockdownDuration, hitReaction.knockdownDuration, 0.0001f);
            Assert.AreEqual(archetype.knockdownDistance, hitReaction.knockdownDistance, 0.0001f);
            Assert.AreEqual(archetype.knockdownRecoverTime, hitReaction.knockdownRecoverTime, 0.0001f);
        }

        [Test]
        public void StrongholdController_SpawnEnemyAtPosition_AppliesArchetypeOverride()
        {
            GameObject controllerGo = CreateTrackedGameObject("P0_Stronghold_Controller");
            StrongholdController controller = controllerGo.AddComponent<StrongholdController>();
            controller.usePooling = false;
            controller.facePlayerOnSpawn = false;

            GameObject prefab = CreateTrackedGameObject("P0_EnemyPrefab");
            prefab.AddComponent<EnemyAI>();
            prefab.AddComponent<EnemyHitReaction>();
            EnemyArchetypeConfigurator prefabConfigurator = prefab.AddComponent<EnemyArchetypeConfigurator>();
            prefabConfigurator.applyOnStart = false;
            prefabConfigurator.applyOnSpawned = false;
            prefabConfigurator.archetype = CreateArchetype("grunt");

            EnemyArchetype overrideArchetype = CreateArchetype("elite");
            overrideArchetype.maxHealth = 333;
            overrideArchetype.attackDamage = 54;

            InvokePrivateSpawnEnemyAtPosition(
                controller,
                prefab,
                waveIndex: 0,
                isElite: false,
                spawnPosition: new Vector3(2f, 0f, 1f),
                targetOverride: null,
                archetypeOverride: overrideArchetype);

            EnemyArchetypeConfigurator[] allConfigurators = Object.FindObjectsOfType<EnemyArchetypeConfigurator>();
            EnemyArchetypeConfigurator spawnedConfigurator = null;
            for (int i = 0; i < allConfigurators.Length; i++)
            {
                EnemyArchetypeConfigurator candidate = allConfigurators[i];
                if (candidate != null && candidate.gameObject != prefab && candidate.gameObject.name.StartsWith(prefab.name))
                {
                    spawnedConfigurator = candidate;
                    break;
                }
            }

            Assert.NotNull(spawnedConfigurator, "Expected spawned enemy clone with EnemyArchetypeConfigurator.");
            createdObjects.Add(spawnedConfigurator.gameObject);
            Assert.AreSame(overrideArchetype, spawnedConfigurator.archetype);

            EnemyHealth spawnedHealth = spawnedConfigurator.GetComponent<EnemyHealth>();
            EnemyAI spawnedAi = spawnedConfigurator.GetComponent<EnemyAI>();
            Assert.NotNull(spawnedHealth);
            Assert.NotNull(spawnedAi);
            Assert.AreEqual(overrideArchetype.maxHealth, spawnedHealth.maxHealth);
            Assert.AreEqual(overrideArchetype.attackDamage, spawnedAi.attackDamage);
        }

        [Test]
        public void WaveArchetypeProfile_GetMultiplier_UsesNormalizedIdAndFallback()
        {
            WaveArchetypeProfile profile = new WaveArchetypeProfile
            {
                gruntMultiplier = 1.1f,
                rusherMultiplier = 0.9f,
                tankMultiplier = 1.25f,
                eliteMultiplier = 0.75f,
                rangedMultiplier = 1.3f,
                controllerMultiplier = 0.6f,
                suicideMultiplier = 0.4f
            };

            Assert.AreEqual(1.1f, profile.GetMultiplier(" Grunt "), 0.0001f);
            Assert.AreEqual(0.9f, profile.GetMultiplier("RUSHER"), 0.0001f);
            Assert.AreEqual(1.25f, profile.GetMultiplier("tank"), 0.0001f);
            Assert.AreEqual(0.75f, profile.GetMultiplier("elite"), 0.0001f);
            Assert.AreEqual(1.3f, profile.GetMultiplier("ranged"), 0.0001f);
            Assert.AreEqual(0.6f, profile.GetMultiplier("controller"), 0.0001f);
            Assert.AreEqual(0.4f, profile.GetMultiplier("suicide"), 0.0001f);
            Assert.AreEqual(1f, profile.GetMultiplier("unknown"), 0.0001f);
            Assert.AreEqual(1f, profile.GetMultiplier(string.Empty), 0.0001f);
            Assert.AreEqual(1f, profile.GetMultiplier(null), 0.0001f);
        }

        [Test]
        public void IntensityWaveDirector_AdjustSpawnCount_UsesPrefabConfiguratorArchetype()
        {
            GameObject directorGo = CreateTrackedGameObject("P0_Intensity_Director");
            IntensityWaveDirector director = directorGo.AddComponent<IntensityWaveDirector>();
            director.minCountMultiplier = 1f;
            director.maxCountMultiplier = 1f;
            director.waveRampPerWave = 0f;
            director.maxTotalCountMultiplier = 1f;
            director.comboIntensityBonus = 0f;
            director.musouIntensityBonus = 0f;
            director.targetKillsPerMinute = 1000f;
            director.waveProfiles = new List<WaveArchetypeProfile>
            {
                new WaveArchetypeProfile
                {
                    waveIndex = 0,
                    gruntMultiplier = 1f,
                    rusherMultiplier = 0.5f,
                    tankMultiplier = 1f,
                    eliteMultiplier = 1f,
                    rangedMultiplier = 1f,
                    controllerMultiplier = 1f,
                    suicideMultiplier = 1f
                }
            };

            GameObject prefab = CreateTrackedGameObject("P0_RusherPrefab");
            prefab.AddComponent<EnemyAI>();
            prefab.AddComponent<EnemyHitReaction>();
            EnemyArchetypeConfigurator configurator = prefab.AddComponent<EnemyArchetypeConfigurator>();
            configurator.applyOnStart = false;
            configurator.applyOnSpawned = false;
            configurator.archetype = CreateArchetype("rusher");

            WaveSpawnGroup group = new WaveSpawnGroup
            {
                prefab = prefab,
                count = 10,
                archetypeOverride = null
            };

            int adjusted = director.AdjustSpawnCount(
                controller: null,
                wave: new StrongholdWave(),
                group: group,
                waveIndex: 0,
                isElite: false,
                baseCount: 10);

            Assert.AreEqual(5, adjusted, "Director should apply rusher composition multiplier from prefab archetype.");
        }

        [Test]
        public void EnemyArchetypeValidation_ReportsEmptyDuplicateAndUnsupportedIds()
        {
            EnemyArchetype emptyId = CreateArchetype(string.Empty);
            EnemyArchetype firstGrunt = CreateArchetype("Grunt");
            EnemyArchetype duplicateGrunt = CreateArchetype(" grunt ");
            EnemyArchetype unsupported = CreateArchetype("boss_unique");

            List<EnemyArchetypeValidationIssue> issues = EnemyArchetypeValidation.Validate(
                new[] { emptyId, firstGrunt, duplicateGrunt, unsupported });

            Assert.IsTrue(ContainsIssue(issues, EnemyArchetypeValidationIssueCode.EmptyArchetypeId));
            Assert.IsTrue(ContainsIssue(issues, EnemyArchetypeValidationIssueCode.DuplicateArchetypeId, "grunt"));
            Assert.IsTrue(ContainsIssue(issues, EnemyArchetypeValidationIssueCode.UnsupportedIntensityMappingId, "boss_unique"));
        }

        private GameObject CreateTrackedGameObject(string name)
        {
            GameObject go = new GameObject(name);
            createdObjects.Add(go);
            return go;
        }

        private EnemyArchetype CreateArchetype(string id)
        {
            EnemyArchetype archetype = ScriptableObject.CreateInstance<EnemyArchetype>();
            archetype.archetypeId = id;
            createdObjects.Add(archetype);
            return archetype;
        }

        private static bool ContainsIssue(
            IReadOnlyList<EnemyArchetypeValidationIssue> issues,
            EnemyArchetypeValidationIssueCode code,
            string normalizedId = null)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                EnemyArchetypeValidationIssue issue = issues[i];
                if (issue.code != code)
                {
                    continue;
                }

                if (normalizedId == null || issue.normalizedArchetypeId == normalizedId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void InvokePrivateSpawnEnemyAtPosition(
            StrongholdController controller,
            GameObject prefab,
            int waveIndex,
            bool isElite,
            Vector3 spawnPosition,
            Transform targetOverride,
            EnemyArchetype archetypeOverride)
        {
            MethodInfo method = typeof(StrongholdController).GetMethod("SpawnEnemyAtPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, "Expected private StrongholdController.SpawnEnemyAtPosition.");
            method.Invoke(controller, new object[]
            {
                prefab,
                waveIndex,
                isElite,
                spawnPosition,
                targetOverride,
                archetypeOverride
            });
        }
    }
}
