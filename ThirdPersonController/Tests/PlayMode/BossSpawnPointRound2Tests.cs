using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace ThirdPersonController.Tests
{
    public class BossSpawnPointRound2Tests
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
        }

        [Test]
        public void SpawnBoss_WithBossControllerPrefab_UsesControllerPath()
        {
            GameObject sourcePrefab = CreateBossSource("BossControllerSource", withController: true);

            GameObject spawnerGo = new GameObject("BossSpawnPoint_ControllerPath");
            createdObjects.Add(spawnerGo);
            BossSpawnPoint spawnPoint = spawnerGo.AddComponent<BossSpawnPoint>();
            spawnPoint.spawnOnStart = false;
            spawnPoint.bossPrefab = sourcePrefab;
            spawnPoint.maxHealth = 4321;
            spawnPoint.baseDamage = 77;
            spawnPoint.knockback = 9f;

            spawnPoint.SpawnBoss();

            GameObject spawned = GetSpawnedBoss(spawnPoint);
            Assert.NotNull(spawned, "Spawned boss should exist.");

            BossController controller = spawned.GetComponent<BossController>();
            Assert.NotNull(controller, "Controller boss prefab should keep BossController component.");
            Assert.AreEqual(4321, controller.maxHealth, "SpawnPoint should propagate maxHealth into BossController.");
            Assert.NotNull(controller.health, "BossController should have EnemyHealth reference after spawn.");

            BossCombatTemplate template = spawned.GetComponent<BossCombatTemplate>();
            Assert.IsTrue(template == null || !template.enabled,
                "Controller path should not run BossCombatTemplate logic in parallel.");
        }

        [Test]
        public void SpawnBoss_WithoutBossController_FallsBackToPrototypeTemplatePath()
        {
            GameObject sourcePrefab = CreateBossSource("BossTemplateSource", withController: false);

            GameObject spawnerGo = new GameObject("BossSpawnPoint_TemplatePath");
            createdObjects.Add(spawnerGo);
            BossSpawnPoint spawnPoint = spawnerGo.AddComponent<BossSpawnPoint>();
            spawnPoint.spawnOnStart = false;
            spawnPoint.bossPrefab = sourcePrefab;
            spawnPoint.prototype = BossPrototypeType.Eel;

            spawnPoint.SpawnBoss();

            GameObject spawned = GetSpawnedBoss(spawnPoint);
            Assert.NotNull(spawned, "Spawned boss should exist.");

            BossController controller = spawned.GetComponent<BossController>();
            Assert.IsNull(controller, "Fallback path should not inject BossController automatically.");

            BossCombatTemplate template = spawned.GetComponent<BossCombatTemplate>();
            Assert.NotNull(template, "Fallback path should attach BossCombatTemplate.");

            BossEelPrototype eel = spawned.GetComponent<BossEelPrototype>();
            Assert.NotNull(eel, "Eel prototype should be attached for Eel spawn type.");
        }

        [Test]
        public void SpawnBoss_ControllerPath_OverrideEncounterTuning_PropagatesDepthFields()
        {
            GameObject sourcePrefab = CreateBossSource("BossControllerSource_DepthFields", withController: true);

            GameObject spawnerGo = new GameObject("BossSpawnPoint_ControllerPath_DepthFields");
            createdObjects.Add(spawnerGo);
            BossSpawnPoint spawnPoint = spawnerGo.AddComponent<BossSpawnPoint>();
            spawnPoint.spawnOnStart = false;
            spawnPoint.bossPrefab = sourcePrefab;
            spawnPoint.overrideEncounterTuning = true;
            spawnPoint.breakWindowDuration = 5.2f;
            spawnPoint.breakWindowCooldown = 10.5f;
            spawnPoint.breakWindowDamageMultiplier = 1.9f;
            spawnPoint.staggerMax = 155f;
            spawnPoint.staggerPerDamage = 1.55f;
            spawnPoint.attackInterval = 2.36f;
            spawnPoint.decisionInterval = 0.57f;
            spawnPoint.queuedAttackLimit = 4;
            spawnPoint.immediateRepeatPenalty = 0.22f;
            spawnPoint.enablePostBreakPunishWindow = true;
            spawnPoint.postBreakPunishDuration = 6.4f;
            spawnPoint.postBreakAttackIntervalMultiplier = 0.62f;
            spawnPoint.postBreakDecisionIntervalMultiplier = 0.71f;
            spawnPoint.postBreakChaseSpeedMultiplier = 1.28f;
            spawnPoint.enablePhaseComboChain = true;
            spawnPoint.phase2ComboChance = 0.58f;
            spawnPoint.phase3ComboChance = 0.79f;
            spawnPoint.comboStartDelay = 0.06f;
            spawnPoint.comboRepeatPenalty = 0.2f;
            spawnPoint.enableInterruptRecoveryGate = true;
            spawnPoint.interruptRecoveryDuration = 0.16f;
            spawnPoint.interruptedAttackCooldownScale = 0.34f;
            spawnPoint.enableTimePressure = true;
            spawnPoint.timePressureDelay = 45f;
            spawnPoint.timePressureRampDuration = 32f;
            spawnPoint.maxTimePressureDamageMultiplier = 1.42f;
            spawnPoint.maxTimePressureSpeedMultiplier = 1.25f;
            spawnPoint.enablePhaseIntentStyle = true;
            spawnPoint.phase1IntentStyle = BossPhaseIntentStyle.Balanced;
            spawnPoint.phase2IntentStyle = BossPhaseIntentStyle.PressureClose;
            spawnPoint.phase3IntentStyle = BossPhaseIntentStyle.SpecialBurst;
            spawnPoint.closeRangeIntentThreshold = 5.2f;
            spawnPoint.intentCloseWeightBoost = 1.8f;
            spawnPoint.intentRangedWeightBoost = 1.45f;
            spawnPoint.intentAoeWeightBoost = 1.3f;
            spawnPoint.intentSpecialWeightBoost = 1.9f;
            spawnPoint.intentFastDecisionMultiplier = 0.82f;
            spawnPoint.intentSlowDecisionMultiplier = 1.22f;

            spawnPoint.SpawnBoss();

            GameObject spawned = GetSpawnedBoss(spawnPoint);
            Assert.NotNull(spawned);

            BossController controller = spawned.GetComponent<BossController>();
            Assert.NotNull(controller);

            Assert.AreEqual(5.2f, controller.breakWindowDuration, 0.0001f);
            Assert.AreEqual(10.5f, controller.breakWindowCooldown, 0.0001f);
            Assert.AreEqual(1.9f, controller.breakWindowDamageMultiplier, 0.0001f);
            Assert.AreEqual(155f, controller.staggerMax, 0.0001f);
            Assert.AreEqual(1.55f, controller.staggerPerDamage, 0.0001f);
            Assert.AreEqual(2.36f, controller.attackInterval, 0.0001f);
            Assert.AreEqual(0.57f, controller.decisionInterval, 0.0001f);
            Assert.AreEqual(4, controller.queuedAttackLimit);
            Assert.AreEqual(0.22f, controller.immediateRepeatPenalty, 0.0001f);
            Assert.IsTrue(controller.enablePostBreakPunishWindow);
            Assert.AreEqual(6.4f, controller.postBreakPunishDuration, 0.0001f);
            Assert.AreEqual(0.62f, controller.postBreakAttackIntervalMultiplier, 0.0001f);
            Assert.AreEqual(0.71f, controller.postBreakDecisionIntervalMultiplier, 0.0001f);
            Assert.AreEqual(1.28f, controller.postBreakChaseSpeedMultiplier, 0.0001f);
            Assert.IsTrue(controller.enablePhaseComboChain);
            Assert.AreEqual(0.58f, controller.phase2ComboChance, 0.0001f);
            Assert.AreEqual(0.79f, controller.phase3ComboChance, 0.0001f);
            Assert.AreEqual(0.06f, controller.comboStartDelay, 0.0001f);
            Assert.AreEqual(0.2f, controller.comboRepeatPenalty, 0.0001f);
            Assert.IsTrue(controller.enableInterruptRecoveryGate);
            Assert.AreEqual(0.16f, controller.interruptRecoveryDuration, 0.0001f);
            Assert.AreEqual(0.34f, controller.interruptedAttackCooldownScale, 0.0001f);
            Assert.IsTrue(controller.enableTimePressure);
            Assert.AreEqual(45f, controller.timePressureDelay, 0.0001f);
            Assert.AreEqual(32f, controller.timePressureRampDuration, 0.0001f);
            Assert.AreEqual(1.42f, controller.maxTimePressureDamageMultiplier, 0.0001f);
            Assert.AreEqual(1.25f, controller.maxTimePressureSpeedMultiplier, 0.0001f);
            Assert.IsTrue(controller.enablePhaseIntentStyle);
            Assert.AreEqual(BossPhaseIntentStyle.Balanced, controller.phase1IntentStyle);
            Assert.AreEqual(BossPhaseIntentStyle.PressureClose, controller.phase2IntentStyle);
            Assert.AreEqual(BossPhaseIntentStyle.SpecialBurst, controller.phase3IntentStyle);
            Assert.AreEqual(5.2f, controller.closeRangeIntentThreshold, 0.0001f);
            Assert.AreEqual(1.8f, controller.intentCloseWeightBoost, 0.0001f);
            Assert.AreEqual(1.45f, controller.intentRangedWeightBoost, 0.0001f);
            Assert.AreEqual(1.3f, controller.intentAoeWeightBoost, 0.0001f);
            Assert.AreEqual(1.9f, controller.intentSpecialWeightBoost, 0.0001f);
            Assert.AreEqual(0.82f, controller.intentFastDecisionMultiplier, 0.0001f);
            Assert.AreEqual(1.22f, controller.intentSlowDecisionMultiplier, 0.0001f);
        }

        [Test]
        public void SpawnBoss_WithEncounterProfile_OverridesSpawnStatsAndControllerTuning()
        {
            GameObject sourcePrefab = CreateBossSource("BossControllerSource_Profile", withController: true);

            BossEncounterProfile profile = ScriptableObject.CreateInstance<BossEncounterProfile>();
            createdObjects.Add(profile);
            profile.bossDisplayName = "ProfiledBoss";
            profile.overrideSpawnStats = true;
            profile.maxHealth = 5123;
            profile.expReward = 777;
            profile.baseDamage = 66;
            profile.knockback = 12f;
            profile.overrideEncounterTuning = true;
            profile.breakWindowDuration = 5.8f;
            profile.decisionInterval = 0.52f;
            profile.queuedAttackLimit = 5;
            profile.phase2HealthThreshold = 0.61f;
            profile.phase3HealthThreshold = 0.29f;
            profile.enablePhaseIntentStyle = true;
            profile.phase1IntentStyle = BossPhaseIntentStyle.Zoning;
            profile.phase2IntentStyle = BossPhaseIntentStyle.PressureClose;
            profile.phase3IntentStyle = BossPhaseIntentStyle.SpecialBurst;
            profile.closeRangeIntentThreshold = 5.5f;
            profile.intentCloseWeightBoost = 1.6f;
            profile.intentRangedWeightBoost = 1.7f;
            profile.intentAoeWeightBoost = 1.35f;
            profile.intentSpecialWeightBoost = 1.95f;
            profile.intentFastDecisionMultiplier = 0.8f;
            profile.intentSlowDecisionMultiplier = 1.25f;

            GameObject spawnerGo = new GameObject("BossSpawnPoint_ProfilePath");
            createdObjects.Add(spawnerGo);
            BossSpawnPoint spawnPoint = spawnerGo.AddComponent<BossSpawnPoint>();
            spawnPoint.spawnOnStart = false;
            spawnPoint.bossPrefab = sourcePrefab;
            spawnPoint.applyEncounterProfile = true;
            spawnPoint.encounterProfile = profile;

            // If profile applies correctly, these manual fields should be ignored.
            spawnPoint.maxHealth = 1111;
            spawnPoint.baseDamage = 11;
            spawnPoint.breakWindowDuration = 2.2f;
            spawnPoint.decisionInterval = 1.1f;

            spawnPoint.SpawnBoss();

            GameObject spawned = GetSpawnedBoss(spawnPoint);
            Assert.NotNull(spawned);
            Assert.AreEqual("ProfiledBoss", spawned.name);

            EnemyHealth health = spawned.GetComponent<EnemyHealth>();
            Assert.NotNull(health);
            Assert.AreEqual(5123, health.maxHealth);
            Assert.AreEqual(777, health.expReward);

            EnemyAI ai = spawned.GetComponent<EnemyAI>();
            Assert.NotNull(ai);
            Assert.AreEqual(66, ai.attackDamage);
            Assert.AreEqual(12f, ai.attackKnockback, 0.0001f);

            BossController controller = spawned.GetComponent<BossController>();
            Assert.NotNull(controller);
            Assert.AreEqual(5123, controller.maxHealth);
            Assert.AreEqual(5.8f, controller.breakWindowDuration, 0.0001f);
            Assert.AreEqual(0.52f, controller.decisionInterval, 0.0001f);
            Assert.AreEqual(5, controller.queuedAttackLimit);
            Assert.IsTrue(controller.phases != null && controller.phases.Count >= 3);
            Assert.AreEqual(0.61f, controller.phases[1].healthPercentThreshold, 0.0001f);
            Assert.AreEqual(0.29f, controller.phases[2].healthPercentThreshold, 0.0001f);
            Assert.IsTrue(controller.enablePhaseIntentStyle);
            Assert.AreEqual(BossPhaseIntentStyle.Zoning, controller.phase1IntentStyle);
            Assert.AreEqual(BossPhaseIntentStyle.PressureClose, controller.phase2IntentStyle);
            Assert.AreEqual(BossPhaseIntentStyle.SpecialBurst, controller.phase3IntentStyle);
            Assert.AreEqual(5.5f, controller.closeRangeIntentThreshold, 0.0001f);
            Assert.AreEqual(1.6f, controller.intentCloseWeightBoost, 0.0001f);
            Assert.AreEqual(1.7f, controller.intentRangedWeightBoost, 0.0001f);
            Assert.AreEqual(1.35f, controller.intentAoeWeightBoost, 0.0001f);
            Assert.AreEqual(1.95f, controller.intentSpecialWeightBoost, 0.0001f);
            Assert.AreEqual(0.8f, controller.intentFastDecisionMultiplier, 0.0001f);
            Assert.AreEqual(1.25f, controller.intentSlowDecisionMultiplier, 0.0001f);
        }

        private GameObject CreateBossSource(string name, bool withController)
        {
            GameObject source = new GameObject(name);
            createdObjects.Add(source);

            source.AddComponent<Rigidbody>();
            source.AddComponent<CapsuleCollider>();
            source.AddComponent<NavMeshAgent>();

            EnemyHealth health = source.AddComponent<EnemyHealth>();
            health.enemyType = EnemyType.Boss;

            EnemyAI ai = source.AddComponent<EnemyAI>();
            ai.animator = source.AddComponent<Animator>();

            if (withController)
            {
                BossController controller = source.AddComponent<BossController>();
                controller.health = health;
                controller.ai = ai;
                controller.animator = ai.animator;
                controller.usePhases = true;
            }

            return source;
        }

        private static GameObject GetSpawnedBoss(BossSpawnPoint spawnPoint)
        {
            FieldInfo field = typeof(BossSpawnPoint).GetField("spawnedBoss", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, "BossSpawnPoint.spawnedBoss private field should exist.");
            return field.GetValue(spawnPoint) as GameObject;
        }
    }
}
