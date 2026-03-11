using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class EnemyAIP0P1RegressionTests
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
        public void EnemyAI_UpdateState_LowHealthFleeHasPriority()
        {
            EnemyAI enemy = CreateEnemy("AI_Flee");
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            Transform target = CreateTarget("Target_Flee", new Vector3(3f, 0f, 0f));

            enemy.canFlee = true;
            enemy.fleeHealthThreshold = 0.5f;
            enemy.canCharge = false;
            enemy.canDodge = false;
            enemy.canBlock = false;

            SetPrivateField(health, "currentHealth", 40);
            SetPrivateField(enemy, "currentTarget", target);
            SetPrivateField(enemy, "isChasing", true);

            InvokePrivate(enemy, "UpdateState");

            Assert.AreEqual("Flee", GetPrivateField<object>(enemy, "currentState").ToString());
        }

        [Test]
        public void EnemyAI_UpdateState_CanEnterChargeWhenConfigured()
        {
            EnemyAI enemy = CreateEnemy("AI_Charge");
            Transform target = CreateTarget("Target_Charge", new Vector3(3f, 0f, 0f));

            enemy.canFlee = false;
            enemy.canDodge = false;
            enemy.canBlock = false;
            enemy.canCharge = true;
            enemy.chargeChance = 1f;
            enemy.chargeMinDistance = 1f;
            enemy.chargeMaxDistance = 5f;
            enemy.attackRange = 1.2f;

            SetPrivateField(enemy, "currentTarget", target);
            SetPrivateField(enemy, "isChasing", true);
            SetPrivateField(enemy, "attackCooldownTimer", 0f);

            InvokePrivate(enemy, "UpdateState");

            Assert.AreEqual("Charge", GetPrivateField<object>(enemy, "currentState").ToString());
            Assert.IsTrue(GetPrivateField<bool>(enemy, "isCharging"));
        }

        [Test]
        public void EnemyAI_UpdateState_CanEnterBlockWhenConfigured()
        {
            EnemyAI enemy = CreateEnemy("AI_Block");
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            Transform target = CreateTarget("Target_Block", new Vector3(1.1f, 0f, 0f));

            enemy.canFlee = false;
            enemy.canDodge = false;
            enemy.canCharge = false;
            enemy.canBlock = true;
            enemy.blockChance = 1f;
            enemy.blockDefenseBonus = 8f;
            enemy.attackRange = 1.6f;
            health.defense = 2f;

            SetPrivateField(enemy, "currentTarget", target);
            SetPrivateField(enemy, "isChasing", true);

            InvokePrivate(enemy, "UpdateState");

            Assert.AreEqual("Block", GetPrivateField<object>(enemy, "currentState").ToString());
            Assert.IsTrue(GetPrivateField<bool>(enemy, "isBlocking"));
            Assert.Greater(health.defense, 2f, "Block should temporarily increase defense.");
        }

        [Test]
        public void EnemyAI_ApplyStun_ClearsTransientActionsAndToken()
        {
            EnemyAI enemy = CreateEnemy("AI_StunCleanup");
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();

            SetPrivateField(enemy, "hasAttackToken", true);
            SetPrivateField(enemy, "isAttacking", true);
            SetPrivateField(enemy, "isDodging", true);
            SetPrivateField(enemy, "isBlocking", true);
            SetPrivateField(enemy, "isCharging", true);
            SetPrivateField(enemy, "isFleeing", true);
            SetPrivateField(enemy, "blockDefenseBaseline", 3f);
            SetPrivateField(enemy, "blockDefenseApplied", true);
            health.defense = 12f;

            enemy.ApplyStun(0.5f);

            Assert.IsTrue(GetPrivateField<bool>(enemy, "isStunned"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "hasAttackToken"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "isAttacking"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "isDodging"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "isBlocking"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "isCharging"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "isFleeing"));
            Assert.AreEqual(3f, health.defense, 0.001f, "Stun cleanup should restore pre-block defense value.");
        }

        [Test]
        public void EnemyCrowdCoordinator_TracksTokenGrantRejectAndUtilization()
        {
            EnemyCrowdCoordinator coordinator = CreateCoordinator("AI_Crowd");
            coordinator.scaleAttackersWithNearby = false;
            coordinator.maxActiveAttackers = 1;

            EnemyAI enemyA = CreateEnemy("AI_Crowd_A");
            EnemyAI enemyB = CreateEnemy("AI_Crowd_B");

            bool grantedA = coordinator.RequestAttackToken(enemyA);
            bool grantedB = coordinator.RequestAttackToken(enemyB);

            Assert.IsTrue(grantedA);
            Assert.IsFalse(grantedB);
            Assert.AreEqual(2, coordinator.TokenRequestCount);
            Assert.AreEqual(1, coordinator.TokenGrantedCount);
            Assert.AreEqual(1, coordinator.TokenRejectedCount);
            Assert.AreEqual(1, coordinator.ActiveAttackersCount);
            Assert.AreEqual(1, coordinator.EffectiveMaxAttackers);
            Assert.AreEqual(1f, coordinator.TokenUtilization, 0.001f);

            coordinator.ReleaseAttackToken(enemyA);
            Assert.AreEqual(1, coordinator.TokenReleaseCount);
            Assert.AreEqual(0, coordinator.ActiveAttackersCount);
        }

        private EnemyAI CreateEnemy(string name)
        {
            GameObject enemyGo = new GameObject(name);
            createdObjects.Add(enemyGo);
            EnemyAI enemy = enemyGo.AddComponent<EnemyAI>();
            enemy.useCrowdCoordinator = false;
            enemy.detectionRange = 50f;
            enemy.attackRange = 2f;
            enemy.farUpdateDistance = 20f;
            enemy.nearUpdateDistance = 5f;

            EnemyHealth health = enemyGo.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.maxHealth = 100;
                health.OnSpawned();
            }

            return enemy;
        }

        private EnemyCrowdCoordinator CreateCoordinator(string name)
        {
            GameObject coordinatorGo = new GameObject(name);
            createdObjects.Add(coordinatorGo);
            EnemyCrowdCoordinator coordinator = coordinatorGo.AddComponent<EnemyCrowdCoordinator>();
            return coordinator;
        }

        private Transform CreateTarget(string name, Vector3 position)
        {
            GameObject targetGo = new GameObject(name);
            createdObjects.Add(targetGo);
            targetGo.transform.position = position;
            return targetGo.transform;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"Expected private method: {methodName}");
            method.Invoke(target, null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected private field: {fieldName}");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected private field: {fieldName}");
            return (T)field.GetValue(target);
        }
    }
}
