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
