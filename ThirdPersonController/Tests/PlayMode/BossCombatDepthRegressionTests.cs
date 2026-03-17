using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class BossCombatDepthRegressionTests
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
        public void BossCombatTemplate_SelectSkill_PrefersInRangeSkill()
        {
            GameObject bossGo = new GameObject("Boss_SelectSkill");
            createdObjects.Add(bossGo);
            BossCombatTemplateProbe boss = bossGo.AddComponent<BossCombatTemplateProbe>();

            CreatePlayer("Player_SelectSkill", new Vector3(2f, 0f, 0f));

            boss.currentPhase = BossCombatPhase.Phase1;
            boss.preferRangeMatching = true;
            boss.outOfRangeWeightPenalty = 0f;
            boss.avoidSkillSpam = false;
            boss.phase1Skills.Clear();
            boss.phase1Skills.Add(new BossSkillDefinition
            {
                id = "in_range",
                weight = 1f,
                usePreferredRange = true,
                preferredMinRange = 0f,
                preferredMaxRange = 4f
            });
            boss.phase1Skills.Add(new BossSkillDefinition
            {
                id = "out_of_range",
                weight = 100f,
                usePreferredRange = true,
                preferredMinRange = 8f,
                preferredMaxRange = 15f
            });

            BossSkillDefinition selected = boss.CallSelectSkill();
            Assert.NotNull(selected);
            Assert.AreEqual("in_range", selected.id);
        }

        [Test]
        public void BossEelPrototype_VortexSkill_PullsPlayerCloser()
        {
            GameObject bossGo = new GameObject("Boss_Eel");
            createdObjects.Add(bossGo);
            BossEelPrototype eel = bossGo.AddComponent<BossEelPrototype>();
            bossGo.transform.position = Vector3.zero;

            GameObject playerGo = CreatePlayer("Player_Eel", new Vector3(6f, 0f, 0f));
            float before = Vector3.Distance(playerGo.transform.position, bossGo.transform.position);

            BossSkillDefinition skill = new BossSkillDefinition
            {
                id = "eel_vortex",
                windup = 0f,
                active = 0f,
                recovery = 0f,
                damageMultiplier = 1f
            };

            RunSkillCoroutine(eel, skill);

            float after = Vector3.Distance(playerGo.transform.position, bossGo.transform.position);
            Assert.Less(after, before, "Vortex should pull the player closer.");
        }

        [Test]
        public void BossGuardianPrototype_Overload_TriggersMultiPulse()
        {
            GameObject bossGo = new GameObject("Boss_Guardian");
            createdObjects.Add(bossGo);
            BossGuardianPrototype guardian = bossGo.AddComponent<BossGuardianPrototype>();
            bossGo.transform.position = Vector3.zero;

            CreatePlayer("Player_Guardian", new Vector3(2f, 0f, 0f));

            BossSkillDefinition skill = new BossSkillDefinition
            {
                id = "guard_overload",
                windup = 0f,
                active = 0f,
                recovery = 0f,
                damageMultiplier = 1f
            };

            RunSkillCoroutine(guardian, skill);
            Assert.GreaterOrEqual(guardian.DebugLastPulseCount, 2, "Overload should execute multiple pulse windows.");
        }

        [Test]
        public void BossController_PhaseTransition_AdvancesWithHealthThresholds()
        {
            GameObject bossGo = new GameObject("Boss_Controller_Phase");
            createdObjects.Add(bossGo);
            EnemyHealth health = bossGo.AddComponent<EnemyHealth>();
            BossController boss = bossGo.AddComponent<BossController>();

            health.maxHealth = 100;
            health.OnSpawned();

            boss.usePhases = true;
            boss.phases.Clear();
            boss.phases.Add(new BossPhase { phaseName = "P1", healthPercentThreshold = 1f });
            boss.phases.Add(new BossPhase { phaseName = "P2", healthPercentThreshold = 0.7f });
            boss.phases.Add(new BossPhase { phaseName = "P3", healthPercentThreshold = 0.35f });

            health.TakeDamage(35, Vector3.zero, 0f);
            Assert.AreEqual(2, boss.currentPhase, "Health ratio 0.65 should move boss to phase 2.");

            health.TakeDamage(35, Vector3.zero, 0f);
            Assert.AreEqual(3, boss.currentPhase, "Health ratio 0.30 should move boss to phase 3.");
        }

        [Test]
        public void BossController_BreakAndQueue_RespectCoreRules()
        {
            GameObject bossGo = new GameObject("Boss_Controller_BreakQueue");
            createdObjects.Add(bossGo);
            bossGo.AddComponent<EnemyHealth>();
            BossController boss = bossGo.AddComponent<BossController>();

            boss.enableBreakWindow = true;
            boss.staggerMax = 10f;
            boss.breakWindowDuration = 5f;
            boss.breakWindowCooldown = 0f;
            boss.RegisterBreakValue(12f);
            Assert.IsTrue(boss.IsBreakWindowActive, "Break window should start when stagger reaches threshold.");

            SetPrivateField(boss, "breakWindowActive", false);

            boss.currentPhase = 1;
            SetPrivateField(boss, "currentPhaseIndex", 0);
            boss.attacks.Clear();
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase1_safe",
                attackName = "Phase1 Safe",
                selectionWeight = 1f,
                requiresPhase2 = false
            });
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase2_locked",
                attackName = "Phase2 Locked",
                selectionWeight = 999f,
                requiresPhase2 = true
            });

            InvokePrivateMethod(boss, "TryEnqueueWeightedAttack");
            Assert.AreEqual(1, boss.QueuedAttackCount, "Only one attack should be planned.");

            object queuedAttack = PeekPlannedAttack(boss);
            BossAttack selected = GetQueuedAttackPayload(queuedAttack);
            Assert.NotNull(selected);
            Assert.AreEqual("phase1_safe", selected.attackId, "Phase-locked attack should not be enqueued in phase 1.");
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

        private static void RunSkillCoroutine(BossCombatTemplate boss, BossSkillDefinition skill)
        {
            MethodInfo executeSkill = boss.GetType().GetMethod("ExecuteSkill", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(executeSkill, "ExecuteSkill method should exist on boss type.");
            IEnumerator routine = executeSkill.Invoke(boss, new object[] { skill }) as IEnumerator;
            Assert.NotNull(routine, "ExecuteSkill should return an IEnumerator.");
            while (routine.MoveNext())
            {
            }
        }

        private static object PeekPlannedAttack(BossController boss)
        {
            FieldInfo queueField = typeof(BossController).GetField("plannedAttacks", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(queueField, "plannedAttacks field should exist.");
            object queue = queueField.GetValue(boss);
            Assert.NotNull(queue, "plannedAttacks should be initialized.");

            MethodInfo peek = queue.GetType().GetMethod("Peek", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(peek, "Queue.Peek should exist.");
            return peek.Invoke(queue, null);
        }

        private static BossAttack GetQueuedAttackPayload(object queuedAttackStruct)
        {
            Assert.NotNull(queuedAttackStruct, "Queued struct should not be null.");
            FieldInfo attackField = queuedAttackStruct.GetType().GetField("attack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(attackField, "Queued struct should expose attack field.");
            return attackField.GetValue(queuedAttackStruct) as BossAttack;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            Assert.NotNull(target);
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{fieldName} field should exist.");
            field.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            Assert.NotNull(target);
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} should exist.");
            method.Invoke(target, null);
        }

        private class BossCombatTemplateProbe : BossCombatTemplate
        {
            public BossSkillDefinition CallSelectSkill()
            {
                return SelectSkill();
            }
        }
    }
}
