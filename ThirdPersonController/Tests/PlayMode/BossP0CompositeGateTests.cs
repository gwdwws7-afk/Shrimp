using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class BossP0CompositeGateTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object obj = createdObjects[i];
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void BossController_P0Composite_PhaseBreakQueueAndTimePressure_StayConsistent()
        {
            GameObject bossGo = new GameObject("Boss_P0Composite");
            createdObjects.Add(bossGo);
            bossGo.transform.position = Vector3.zero;

            EnemyHealth health = bossGo.AddComponent<EnemyHealth>();
            health.maxHealth = 100;
            health.OnSpawned();

            BossController boss = bossGo.AddComponent<BossController>();

            CreatePlayer("Player_P0Composite", new Vector3(4f, 0f, 0f));

            boss.enablePostBreakPunishWindow = false;
            boss.enableBreakWindow = true;
            boss.breakWindowDuration = 4f;
            boss.breakWindowCooldown = 0f;
            boss.staggerMax = 10f;
            boss.staggerPerDamage = 1f;

            boss.useAttackQueue = true;
            boss.queuedAttackLimit = 4;
            boss.maxSameAttackQueued = 1;
            boss.immediateRepeatPenalty = 1f;
            boss.weightAttacksByDistance = false;
            boss.prioritizeSpecialAttacksWhenEnraged = false;
            boss.attacks.Clear();
            boss.attacks.Add(new BossAttack
            {
                attackId = "p0_slash",
                attackName = "Slash",
                selectionWeight = 1f,
                cooldown = 0.1f,
                range = 3f
            });
            boss.attacks.Add(new BossAttack
            {
                attackId = "p0_beam",
                attackName = "Beam",
                selectionWeight = 1f,
                cooldown = 0.1f,
                range = 8f
            });

            boss.usePhases = true;
            boss.phases.Clear();
            boss.phases.Add(new BossPhase { phaseName = "P1", healthPercentThreshold = 1f, damageMultiplier = 1f, speedMultiplier = 1f, defenseMultiplier = 1f });
            boss.phases.Add(new BossPhase { phaseName = "P2", healthPercentThreshold = 0.7f, damageMultiplier = 1.15f, speedMultiplier = 1.1f, defenseMultiplier = 1f });
            boss.phases.Add(new BossPhase { phaseName = "P3", healthPercentThreshold = 0.35f, damageMultiplier = 1.3f, speedMultiplier = 1.18f, defenseMultiplier = 1.05f });
            SetPrivateField(boss, "currentPhaseIndex", 0);
            boss.currentPhase = 1;

            boss.enableTimePressure = true;
            boss.timePressureDelay = 0f;
            boss.timePressureRampDuration = 10f;
            boss.scaleDecisionIntervalWithTimePressure = true;
            boss.minDecisionIntervalMultiplierAtMaxPressure = 0.5f;
            boss.decisionInterval = 1f;
            SetPrivateField(boss, "encounterElapsed", 0f);
            float baseDecisionInterval = (float)InvokePrivateMethod(boss, "GetEffectiveDecisionInterval");
            SetPrivateField(boss, "encounterElapsed", 10f);
            float pressuredDecisionInterval = (float)InvokePrivateMethod(boss, "GetEffectiveDecisionInterval");
            Assert.Greater(baseDecisionInterval, pressuredDecisionInterval, "Time pressure should reduce effective decision interval.");

            health.TakeDamage(35, Vector3.zero, 0f);
            InvokePrivateMethod(boss, "CheckPhaseTransition");
            Assert.AreEqual(2, boss.currentPhase, "Boss should advance to phase 2 after crossing threshold.");

            boss.RegisterBreakValue(12f);
            Assert.IsTrue(boss.IsBreakWindowActive, "Break window should activate when stagger reaches threshold.");

            // End break-state manually to continue planning assertions in this isolated test.
            SetPrivateField(boss, "breakWindowActive", false);
            SetPrivateField(boss, "stunTimer", 0f);

            InvokePrivateMethod(boss, "TryEnqueueWeightedAttack");
            InvokePrivateMethod(boss, "TryEnqueueWeightedAttack");

            string[] queuedKeys = GetQueuedAttackKeys(boss);
            Assert.AreEqual(2, queuedKeys.Length, "Boss should queue two attacks.");
            Assert.AreNotEqual(queuedKeys[0], queuedKeys[1], "maxSameAttackQueued=1 should prevent duplicate queue entries.");
        }

        private GameObject CreatePlayer(string name, Vector3 position)
        {
            GameObject playerGo = new GameObject(name);
            createdObjects.Add(playerGo);
            playerGo.tag = "Player";
            playerGo.transform.position = position;
            playerGo.AddComponent<PlayerHealth>();
            return playerGo;
        }

        private static string[] GetQueuedAttackKeys(BossController boss)
        {
            FieldInfo queueField = typeof(BossController).GetField("plannedAttacks", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(queueField);
            object queue = queueField.GetValue(boss);
            Assert.NotNull(queue);

            MethodInfo toArray = queue.GetType().GetMethod("ToArray", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(toArray);
            Array entries = toArray.Invoke(queue, null) as Array;
            Assert.NotNull(entries);

            var keys = new string[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                object queued = entries.GetValue(i);
                FieldInfo keyField = queued.GetType().GetField("key", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.NotNull(keyField);
                keys[i] = keyField.GetValue(queued) as string;
            }

            return keys;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{fieldName} should exist.");
            field.SetValue(target, value);
        }

        private static object InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} should exist.");
            return method.Invoke(target, args);
        }
    }
}
