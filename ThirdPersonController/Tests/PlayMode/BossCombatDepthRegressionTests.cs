using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
        public void BossEelPrototype_ChainRushMiss_OpensPunishWindowInPhase2()
        {
            GameObject bossGo = new GameObject("Boss_Eel_ChainMiss");
            createdObjects.Add(bossGo);
            BossEelPrototype eel = bossGo.AddComponent<BossEelPrototype>();
            bossGo.transform.position = Vector3.zero;

            CreatePlayer("Player_Eel_ChainMiss", new Vector3(30f, 0f, 0f));

            eel.currentPhase = BossCombatPhase.Phase2;
            eel.enableMissPunishWindow = true;
            eel.enableChainMissPunishWindow = true;
            eel.chainRushMissPunishDuration = 1.2f;

            BossSkillDefinition skill = new BossSkillDefinition
            {
                id = "eel_chain",
                windup = 0f,
                active = 0f,
                recovery = 0f,
                damageMultiplier = 1f
            };

            RunSkillCoroutine(eel, skill);

            Assert.IsTrue(eel.DebugLastPunishWindowTriggered, "Missing chain rush should open punish window in phase 2.");
            Assert.IsTrue(eel.IsPunishWindowActive, "Punish window should remain active immediately after miss resolution.");
        }

        [Test]
        public void BossGuardianPrototype_ShieldWhiff_PunishWindowAmplifiesBreakPressure()
        {
            GameObject bossGo = new GameObject("Boss_Guardian_ShieldWhiff");
            createdObjects.Add(bossGo);
            bossGo.transform.position = Vector3.zero;

            EnemyHealth health = bossGo.AddComponent<EnemyHealth>();
            health.maxHealth = 100;
            health.OnSpawned();

            BossGuardianPrototype guardian = bossGo.AddComponent<BossGuardianPrototype>();
            guardian.enableMissPunishWindow = true;
            guardian.enableWhiffPunishWindow = true;
            guardian.shieldMissPunishDuration = 1.2f;
            guardian.punishWindowStaggerMultiplier = 1.5f;
            guardian.staggerMax = 100f;
            guardian.breakCooldown = 0f;
            guardian.breakWindowDuration = 3f;

            CreatePlayer("Player_Guardian_ShieldWhiff", new Vector3(30f, 0f, 0f));

            BossSkillDefinition skill = new BossSkillDefinition
            {
                id = "guard_shield",
                windup = 0f,
                active = 0f,
                recovery = 0f,
                damageMultiplier = 1f
            };

            RunSkillCoroutine(guardian, skill);
            Assert.IsTrue(guardian.DebugLastPunishWindowTriggered, "Whiffed shield should enter punish window.");
            Assert.IsTrue(guardian.IsPunishWindowActive, "Punish window should be active before applying break value.");

            guardian.RegisterBreakValue(70f);
            Assert.IsTrue(guardian.IsBreakWindowActive, "Punish window multiplier should allow 70 break value to trigger break.");
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
        public void BossController_PhaseTransition_QueuesConfiguredOpenerAttack()
        {
            GameObject bossGo = new GameObject("Boss_Controller_PhaseOpener");
            createdObjects.Add(bossGo);

            EnemyHealth health = bossGo.AddComponent<EnemyHealth>();
            health.maxHealth = 100;
            health.OnSpawned();

            BossController boss = bossGo.AddComponent<BossController>();
            boss.usePhases = true;
            boss.enableBreakWindow = false;
            boss.enablePhaseTransitionOpeners = true;
            boss.phase2TransitionOpenerId = "phase2_opener";
            boss.phase3TransitionOpenerId = "phase3_opener";

            boss.phases.Clear();
            boss.phases.Add(new BossPhase { phaseName = "P1", healthPercentThreshold = 1f });
            boss.phases.Add(new BossPhase { phaseName = "P2", healthPercentThreshold = 0.7f, unlockSpecialAttacks = true });
            boss.phases.Add(new BossPhase { phaseName = "P3", healthPercentThreshold = 0.35f, unlockSpecialAttacks = true });

            boss.attacks.Clear();
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase2_opener",
                attackName = "Phase2 Opener",
                selectionWeight = 1f,
                requiresPhase2 = true,
                targetPlayer = false,
                aoe = false
            });
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase2_other",
                attackName = "Phase2 Other",
                selectionWeight = 10f,
                requiresPhase2 = true,
                targetPlayer = false,
                aoe = false
            });

            health.TakeDamage(35, Vector3.zero, 0f);

            Assert.AreEqual(2, boss.currentPhase, "Boss should enter phase 2.");
            Assert.AreEqual(1, boss.QueuedAttackCount, "Phase opener should be enqueued on phase transition.");
            BossAttack queued = GetQueuedAttackPayload(PeekPlannedAttack(boss));
            Assert.NotNull(queued);
            Assert.AreEqual("phase2_opener", queued.attackId);
            Assert.IsTrue(boss.DebugLastPhaseOpenerQueued, "Debug flag should mark opener queue success.");
        }

        [Test]
        public void BossController_PhaseTransitionOpenerRetry_QueuesWhenInitialCooldownBlocksImmediateQueue()
        {
            GameObject bossGo = new GameObject("Boss_Controller_PhaseOpenerRetry");
            createdObjects.Add(bossGo);

            EnemyHealth health = bossGo.AddComponent<EnemyHealth>();
            health.maxHealth = 100;
            health.OnSpawned();

            BossController boss = bossGo.AddComponent<BossController>();
            boss.usePhases = true;
            boss.enableBreakWindow = false;
            boss.enablePhaseTransitionOpeners = true;
            boss.enablePhaseTransitionOpenerRetry = true;
            boss.phaseTransitionOpenerRetryDelay = 0.05f;
            boss.phaseTransitionOpenerMaxRetries = 4;
            boss.decisionInterval = 999f;
            boss.phase2TransitionOpenerId = "phase2_opener";

            boss.phases.Clear();
            boss.phases.Add(new BossPhase { phaseName = "P1", healthPercentThreshold = 1f });
            boss.phases.Add(new BossPhase { phaseName = "P2", healthPercentThreshold = 0.7f, unlockSpecialAttacks = true });

            boss.attacks.Clear();
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase2_opener",
                attackName = "Phase2 Opener",
                selectionWeight = 1f,
                requiresPhase2 = true,
                targetPlayer = false,
                aoe = false
            });

            InvokePrivateMethod(boss, "SetAttackReadyTime", "phase2_opener", 10f);
            health.TakeDamage(35, Vector3.zero, 0f);

            Assert.AreEqual(2, boss.currentPhase, "Boss should enter phase 2.");
            Assert.AreEqual(0, boss.QueuedAttackCount, "Immediate phase opener queue should fail while opener is still on cooldown.");
            Assert.IsFalse(boss.DebugLastPhaseOpenerQueued, "Debug flag should stay false when immediate queue fails.");

            Dictionary<string, float> readyMap = GetPrivateField(boss, "nextAttackReadyTime") as Dictionary<string, float>;
            Assert.NotNull(readyMap, "Attack cooldown map should be available.");
            readyMap["phase2_opener"] = Time.time - 0.1f;

            InvokePrivateMethod(boss, "UpdatePhaseTransitionOpenerRetry", 0.2f);

            Assert.AreEqual(1, boss.QueuedAttackCount, "Retry path should enqueue phase opener after cooldown expires.");
            BossAttack queued = GetQueuedAttackPayload(PeekPlannedAttack(boss));
            Assert.NotNull(queued);
            Assert.AreEqual("phase2_opener", queued.attackId);
            Assert.IsTrue(boss.DebugLastPhaseOpenerQueued, "Debug flag should flip once retry queue succeeds.");
        }

        [Test]
        public void BossController_PhaseTransitionOpenerRetry_ExhaustsRetriesUnderCoarseDelta_AndClearsPendingState()
        {
            GameObject bossGo = new GameObject("Boss_Controller_PhaseOpenerRetry_Exhaust");
            createdObjects.Add(bossGo);

            EnemyHealth health = bossGo.AddComponent<EnemyHealth>();
            health.maxHealth = 100;
            health.OnSpawned();

            BossController boss = bossGo.AddComponent<BossController>();
            boss.usePhases = true;
            boss.enableBreakWindow = false;
            boss.enablePhaseTransitionOpeners = true;
            boss.enablePhaseTransitionOpenerRetry = true;
            boss.phaseTransitionOpenerRetryDelay = 0.05f;
            boss.phaseTransitionOpenerMaxRetries = 2;
            boss.phase2TransitionOpenerId = "phase2_opener";

            boss.phases.Clear();
            boss.phases.Add(new BossPhase { phaseName = "P1", healthPercentThreshold = 1f });
            boss.phases.Add(new BossPhase { phaseName = "P2", healthPercentThreshold = 0.7f, unlockSpecialAttacks = true });

            boss.attacks.Clear();
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase2_opener",
                attackName = "Phase2 Opener",
                selectionWeight = 1f,
                requiresPhase2 = true,
                targetPlayer = false,
                aoe = false
            });

            InvokePrivateMethod(boss, "SetAttackReadyTime", "phase2_opener", 60f);
            health.TakeDamage(35, Vector3.zero, 0f);

            Assert.AreEqual(2, boss.currentPhase, "Boss should enter phase 2.");
            Assert.AreEqual(0, boss.QueuedAttackCount, "Opener should remain blocked when cooldown is still active.");
            Assert.IsFalse(boss.DebugLastPhaseOpenerQueued, "Debug flag should remain false while retries have not succeeded.");

            InvokePrivateMethod(boss, "UpdatePhaseTransitionOpenerRetry", 0.2f);
            InvokePrivateMethod(boss, "UpdatePhaseTransitionOpenerRetry", 0.2f);
            InvokePrivateMethod(boss, "UpdatePhaseTransitionOpenerRetry", 0.2f);

            Assert.AreEqual(0, boss.QueuedAttackCount, "No opener should be queued after retry budget is exhausted.");
            Assert.AreEqual(string.Empty, (string)GetPrivateField(boss, "pendingPhaseTransitionOpenerId"),
                "Pending opener id should be cleared after retries are exhausted.");
            Assert.AreEqual(0, (int)GetPrivateField(boss, "pendingPhaseTransitionOpenerRetriesLeft"),
                "Retry counter should be cleared to zero after exhaustion.");
        }

        [Test]
        public void BossController_PhaseTransition_QueuesFollowupChain_AfterOpener()
        {
            GameObject bossGo = new GameObject("Boss_Controller_PhaseFollowupChain");
            createdObjects.Add(bossGo);

            EnemyHealth health = bossGo.AddComponent<EnemyHealth>();
            health.maxHealth = 100;
            health.OnSpawned();

            BossController boss = bossGo.AddComponent<BossController>();
            boss.usePhases = true;
            boss.enableBreakWindow = false;
            boss.enablePhaseTransitionOpeners = true;
            boss.phase2TransitionOpenerId = "phase2_opener";
            boss.enablePhaseTransitionFollowupChain = true;
            boss.phase2TransitionFollowupId = "phase2_follow";

            boss.phases.Clear();
            boss.phases.Add(new BossPhase { phaseName = "P1", healthPercentThreshold = 1f });
            boss.phases.Add(new BossPhase { phaseName = "P2", healthPercentThreshold = 0.7f, unlockSpecialAttacks = true });

            boss.attacks.Clear();
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase2_opener",
                attackName = "Phase2 Opener",
                selectionWeight = 1f,
                requiresPhase2 = true,
                targetPlayer = false,
                aoe = false
            });
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase2_follow",
                attackName = "Phase2 Follow",
                selectionWeight = 1f,
                requiresPhase2 = true,
                targetPlayer = false,
                aoe = false
            });

            health.TakeDamage(35, Vector3.zero, 0f);

            Assert.AreEqual(2, boss.currentPhase, "Boss should enter phase 2.");
            Assert.AreEqual(2, boss.QueuedAttackCount, "Opener and follow-up should both be queued.");
            Assert.IsTrue(boss.DebugLastPhaseOpenerQueued, "Opener should be queued on transition.");
            Assert.IsTrue(boss.DebugLastPhaseFollowupQueued, "Follow-up should be queued after opener.");

            BossAttack firstQueued = GetQueuedAttackPayload(DequeuePlannedAttack(boss));
            Assert.NotNull(firstQueued);
            Assert.AreEqual("phase2_opener", firstQueued.attackId, "First queued attack should be the configured opener.");

            BossAttack secondQueued = GetQueuedAttackPayload(PeekPlannedAttack(boss));
            Assert.NotNull(secondQueued);
            Assert.AreEqual("phase2_follow", secondQueued.attackId, "Second queued attack should be the configured follow-up.");
        }

        [Test]
        public void BossController_PhaseTransitionFollowupRetry_QueuesWhenFollowupCooldownExpires()
        {
            GameObject bossGo = new GameObject("Boss_Controller_PhaseFollowupRetry");
            createdObjects.Add(bossGo);

            EnemyHealth health = bossGo.AddComponent<EnemyHealth>();
            health.maxHealth = 100;
            health.OnSpawned();

            BossController boss = bossGo.AddComponent<BossController>();
            boss.usePhases = true;
            boss.enableBreakWindow = false;
            boss.enablePhaseTransitionOpeners = true;
            boss.phase2TransitionOpenerId = "phase2_opener";
            boss.enablePhaseTransitionFollowupChain = true;
            boss.phase2TransitionFollowupId = "phase2_follow";
            boss.enablePhaseTransitionFollowupRetry = true;
            boss.phaseTransitionFollowupRetryDelay = 0.05f;
            boss.phaseTransitionFollowupMaxRetries = 3;

            boss.phases.Clear();
            boss.phases.Add(new BossPhase { phaseName = "P1", healthPercentThreshold = 1f });
            boss.phases.Add(new BossPhase { phaseName = "P2", healthPercentThreshold = 0.7f, unlockSpecialAttacks = true });

            boss.attacks.Clear();
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase2_opener",
                attackName = "Phase2 Opener",
                selectionWeight = 1f,
                requiresPhase2 = true,
                targetPlayer = false,
                aoe = false
            });
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase2_follow",
                attackName = "Phase2 Follow",
                selectionWeight = 1f,
                requiresPhase2 = true,
                targetPlayer = false,
                aoe = false
            });

            InvokePrivateMethod(boss, "SetAttackReadyTime", "phase2_follow", 10f);
            health.TakeDamage(35, Vector3.zero, 0f);

            Assert.AreEqual(2, boss.currentPhase, "Boss should enter phase 2.");
            Assert.AreEqual(1, boss.QueuedAttackCount, "Only opener should queue while follow-up is on cooldown.");
            Assert.IsFalse(boss.DebugLastPhaseFollowupQueued, "Follow-up debug flag should stay false before retry succeeds.");
            Assert.AreEqual("phase2_follow", (string)GetPrivateField(boss, "pendingPhaseTransitionFollowupId"),
                "Pending follow-up id should be tracked for retry.");

            Dictionary<string, float> readyMap = GetPrivateField(boss, "nextAttackReadyTime") as Dictionary<string, float>;
            Assert.NotNull(readyMap, "Attack cooldown map should be available.");
            readyMap["phase2_follow"] = Time.time - 0.1f;

            InvokePrivateMethod(boss, "UpdatePhaseTransitionFollowupRetry", 0.2f);

            Assert.AreEqual(2, boss.QueuedAttackCount, "Follow-up should be queued after retry once cooldown clears.");
            Assert.IsTrue(boss.DebugLastPhaseFollowupQueued, "Follow-up debug flag should flip after retry success.");
            BossAttack queuedOpener = GetQueuedAttackPayload(DequeuePlannedAttack(boss));
            Assert.NotNull(queuedOpener);
            Assert.AreEqual("phase2_opener", queuedOpener.attackId, "Queue front should remain opener.");

            BossAttack queuedFollow = GetQueuedAttackPayload(PeekPlannedAttack(boss));
            Assert.NotNull(queuedFollow);
            Assert.AreEqual("phase2_follow", queuedFollow.attackId, "Follow-up should appear as second queued attack.");
        }

        [Test]
        public void BossController_Phase3PriorityWindow_ForcesSpecialAttackQueue()
        {
            GameObject bossGo = new GameObject("Boss_Controller_Phase3Priority");
            createdObjects.Add(bossGo);
            bossGo.AddComponent<EnemyHealth>();
            BossController boss = bossGo.AddComponent<BossController>();

            boss.enableBreakWindow = false;
            boss.weightAttacksByDistance = false;
            boss.prioritizeSpecialAttacksWhenEnraged = false;
            boss.enablePhase3SpecialPriorityWindow = true;
            boss.forceSpecialQueueDuringPhase3Priority = true;
            boss.phase3SpecialPriorityWeightMultiplier = 2f;
            boss.currentPhase = 3;
            SetPrivateField(boss, "currentPhaseIndex", 2);
            SetPrivateField(boss, "phase3SpecialPriorityTimer", 4f);

            boss.attacks.Clear();
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase3_normal",
                attackName = "Phase3 Normal",
                selectionWeight = 100f,
                requiresPhase3 = true,
                isSpecial = false,
                targetPlayer = false,
                aoe = false
            });
            boss.attacks.Add(new BossAttack
            {
                attackId = "phase3_special",
                attackName = "Phase3 Special",
                selectionWeight = 0.1f,
                requiresPhase3 = true,
                isSpecial = true,
                targetPlayer = false,
                aoe = false
            });

            InvokePrivateMethod(boss, "TryEnqueueWeightedAttack");
            Assert.AreEqual(1, boss.QueuedAttackCount, "Exactly one attack should be queued.");

            BossAttack queued = GetQueuedAttackPayload(PeekPlannedAttack(boss));
            Assert.NotNull(queued);
            Assert.AreEqual("phase3_special", queued.attackId, "Priority window should force queueing a special attack.");
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

        [Test]
        public void BossController_AttackPlanning_DistanceWeightingAvoidsOutOfRangeSpam()
        {
            GameObject bossGo = new GameObject("Boss_Controller_DistanceWeight");
            createdObjects.Add(bossGo);
            bossGo.transform.position = Vector3.zero;
            bossGo.AddComponent<EnemyHealth>();
            bossGo.AddComponent<EnemyAI>();
            BossController boss = bossGo.AddComponent<BossController>();

            CreatePlayer("Player_DistanceWeight", new Vector3(8f, 0f, 0f));

            boss.weightAttacksByDistance = true;
            boss.inRangeWeightMultiplier = 1f;
            boss.outOfRangeWeightMultiplier = 0f;
            boss.prioritizeSpecialAttacksWhenEnraged = false;
            boss.attacks.Clear();
            boss.attacks.Add(new BossAttack
            {
                attackId = "short_range",
                attackName = "Short Range",
                range = 2f,
                selectionWeight = 100f
            });
            boss.attacks.Add(new BossAttack
            {
                attackId = "long_range",
                attackName = "Long Range",
                range = 10f,
                selectionWeight = 1f
            });

            InvokePrivateMethod(boss, "TryEnqueueWeightedAttack");
            Assert.AreEqual(1, boss.QueuedAttackCount, "Expected one queued attack after planning.");

            object queuedAttack = PeekPlannedAttack(boss);
            BossAttack selected = GetQueuedAttackPayload(queuedAttack);
            Assert.NotNull(selected);
            Assert.AreEqual("long_range", selected.attackId, "Out-of-range attack should be suppressed when weighting is enabled.");
        }

        [Test]
        public void BossController_PostBreakPunish_ModifiesIntervalsAndChaseSpeed()
        {
            GameObject bossGo = new GameObject("Boss_Controller_PostBreakPunish");
            createdObjects.Add(bossGo);

            EnemyAI ai = bossGo.AddComponent<EnemyAI>();
            ai.chaseSpeed = 4f;
            ai.attackDamage = 20;

            EnemyHealth health = bossGo.AddComponent<EnemyHealth>();
            health.maxHealth = 100;
            health.OnSpawned();

            BossController boss = bossGo.AddComponent<BossController>();
            boss.enableTimePressure = false;
            boss.attackInterval = 3f;
            boss.decisionInterval = 1f;
            boss.enablePostBreakPunishWindow = true;
            boss.postBreakPunishDuration = 6f;
            boss.postBreakAttackIntervalMultiplier = 0.5f;
            boss.postBreakDecisionIntervalMultiplier = 0.6f;
            boss.postBreakChaseSpeedMultiplier = 1.3f;

            boss.phases.Clear();
            boss.phases.Add(new BossPhase
            {
                phaseName = "P1",
                healthPercentThreshold = 1f,
                damageMultiplier = 1f,
                speedMultiplier = 1f,
                defenseMultiplier = 1f
            });

            InvokePrivateMethod(boss, "ApplyPhaseStats", boss.phases[0]);
            float baseSpeed = ai.chaseSpeed;

            SetPrivateField(boss, "postBreakPunishTimer", 6f);
            InvokePrivateMethod(boss, "ApplyEffectiveStats", 0f);

            float punishedAttackInterval = (float)InvokePrivateMethod(boss, "GetEffectiveAttackInterval");
            float punishedDecisionInterval = (float)InvokePrivateMethod(boss, "GetEffectiveDecisionInterval");
            float punishedSpeed = ai.chaseSpeed;

            Assert.AreEqual(1.5f, punishedAttackInterval, 0.0001f, "Punish window should compress attack interval.");
            Assert.AreEqual(0.6f, punishedDecisionInterval, 0.0001f, "Punish window should compress decision interval.");
            Assert.AreEqual(baseSpeed * 1.3f, punishedSpeed, 0.0001f, "Punish window should boost chase speed.");

            SetPrivateField(boss, "postBreakPunishTimer", 0f);
            InvokePrivateMethod(boss, "ApplyEffectiveStats", 0f);
            float recoveredAttackInterval = (float)InvokePrivateMethod(boss, "GetEffectiveAttackInterval");
            float recoveredDecisionInterval = (float)InvokePrivateMethod(boss, "GetEffectiveDecisionInterval");
            float recoveredSpeed = ai.chaseSpeed;

            Assert.AreEqual(3f, recoveredAttackInterval, 0.0001f);
            Assert.AreEqual(1f, recoveredDecisionInterval, 0.0001f);
            Assert.AreEqual(baseSpeed, recoveredSpeed, 0.0001f);
        }

        [Test]
        public void BossController_BreakWindowEnd_PostBreakPunish_SeedsImmediateReengageTimers()
        {
            GameObject bossGo = new GameObject("Boss_Controller_BreakEndReengage");
            createdObjects.Add(bossGo);

            EnemyAI ai = bossGo.AddComponent<EnemyAI>();
            ai.chaseSpeed = 4f;
            ai.attackDamage = 20;

            EnemyHealth health = bossGo.AddComponent<EnemyHealth>();
            health.maxHealth = 100;
            health.OnSpawned();

            BossController boss = bossGo.AddComponent<BossController>();
            boss.enableTimePressure = false;
            boss.enableBreakWindow = true;
            boss.staggerMax = 10f;
            boss.staggerPerDamage = 1f;
            boss.breakWindowDuration = 0.3f;
            boss.breakWindowCooldown = 0f;
            boss.attackInterval = 2f;
            boss.decisionInterval = 0.8f;
            boss.enablePostBreakPunishWindow = true;
            boss.postBreakPunishDuration = 2f;
            boss.postBreakAttackIntervalMultiplier = 0.6f;
            boss.postBreakDecisionIntervalMultiplier = 0.7f;

            boss.RegisterBreakValue(12f);
            Assert.IsTrue(boss.IsBreakWindowActive, "Break window should be active before end-step simulation.");

            SetPrivateField(boss, "attackTimer", 0f);
            SetPrivateField(boss, "decisionTimer", 0f);
            SetPrivateField(boss, "breakTimer", 0.28f);

            InvokePrivateMethod(boss, "UpdateBreakWindow", 0.05f);

            Assert.IsFalse(boss.IsBreakWindowActive, "Break window should close once duration is reached.");
            float postBreakPunishTimer = (float)GetPrivateField(boss, "postBreakPunishTimer");
            Assert.Greater(postBreakPunishTimer, 0f, "Post-break punish timer should be started when break ends.");

            float effectiveAttackInterval = (float)InvokePrivateMethod(boss, "GetEffectiveAttackInterval");
            float effectiveDecisionInterval = (float)InvokePrivateMethod(boss, "GetEffectiveDecisionInterval");
            float attackTimer = (float)GetPrivateField(boss, "attackTimer");
            float decisionTimer = (float)GetPrivateField(boss, "decisionTimer");

            Assert.GreaterOrEqual(attackTimer, effectiveAttackInterval - 0.0001f, "Break end should seed attack timer for immediate re-engage.");
            Assert.GreaterOrEqual(decisionTimer, effectiveDecisionInterval - 0.0001f, "Break end should seed decision timer for immediate re-plan.");
        }

        [Test]
        public void BossController_TimePressure_RampsDamageAndSpeedWithFightDuration()
        {
            GameObject bossGo = new GameObject("Boss_Controller_TimePressure");
            createdObjects.Add(bossGo);

            EnemyAI ai = bossGo.AddComponent<EnemyAI>();
            ai.attackDamage = 20;
            ai.chaseSpeed = 4f;

            EnemyHealth health = bossGo.AddComponent<EnemyHealth>();
            health.maxHealth = 100;
            health.OnSpawned();

            BossController boss = bossGo.AddComponent<BossController>();
            boss.enableTimePressure = true;
            boss.timePressureDelay = 2f;
            boss.timePressureRampDuration = 4f;
            boss.maxTimePressureDamageMultiplier = 1.5f;
            boss.maxTimePressureSpeedMultiplier = 1.25f;

            boss.phases.Clear();
            boss.phases.Add(new BossPhase
            {
                phaseName = "P1",
                healthPercentThreshold = 1f,
                damageMultiplier = 1f,
                speedMultiplier = 1f,
                defenseMultiplier = 1f
            });

            InvokePrivateMethod(boss, "ApplyPhaseStats", boss.phases[0]);
            SetPrivateField(boss, "encounterElapsed", 6f);
            InvokePrivateMethod(boss, "UpdateTimePressure");

            Assert.AreEqual(30, ai.attackDamage, "Time pressure should apply damage multiplier cap.");
            Assert.AreEqual(5f, ai.chaseSpeed, 0.001f, "Time pressure should apply speed multiplier cap.");
        }

        [Test]
        public void BossController_EffectiveDecisionInterval_ScalesWithTimePressure()
        {
            GameObject bossGo = new GameObject("Boss_Controller_DecisionPressure");
            createdObjects.Add(bossGo);

            bossGo.AddComponent<EnemyAI>();
            bossGo.AddComponent<EnemyHealth>();
            BossController boss = bossGo.AddComponent<BossController>();

            boss.decisionInterval = 1f;
            boss.scaleDecisionIntervalWithTimePressure = true;
            boss.minDecisionIntervalMultiplierAtMaxPressure = 0.5f;
            boss.enableTimePressure = true;
            boss.timePressureDelay = 0f;
            boss.timePressureRampDuration = 10f;

            SetPrivateField(boss, "encounterElapsed", 0f);
            float baseInterval = (float)InvokePrivateMethod(boss, "GetEffectiveDecisionInterval");

            SetPrivateField(boss, "encounterElapsed", 10f);
            float pressuredInterval = (float)InvokePrivateMethod(boss, "GetEffectiveDecisionInterval");

            Assert.AreEqual(1f, baseInterval, 0.0001f, "Decision interval should start from configured baseline.");
            Assert.AreEqual(0.5f, pressuredInterval, 0.0001f, "Decision interval should shrink to configured minimum under max pressure.");
        }

        [UnityTest]
        public IEnumerator BossController_AttackTimeline_LowFpsCoarseStep_CompletesSingleCycle()
        {
            Time.timeScale = 1f;

            GameObject bossGo = new GameObject("Boss_Controller_LowFpsTiming");
            createdObjects.Add(bossGo);
            bossGo.AddComponent<EnemyAI>();
            bossGo.AddComponent<EnemyHealth>();
            BossController boss = bossGo.AddComponent<BossController>();

            boss.enableBreakWindow = false;
            boss.useAttackQueue = true;
            boss.queuedAttackLimit = 1;
            boss.attackInterval = 0f;
            boss.decisionInterval = 999f;
            boss.attacks.Clear();
            boss.attacks.Add(new BossAttack
            {
                attackId = "timing_probe",
                attackName = "Timing Probe",
                selectionWeight = 1f,
                targetPlayer = false,
                aoe = false,
                cooldown = 0.25f,
                windupTime = 0.03f,
                activeTime = 0.03f,
                recoveryTime = 0.03f
            });

            SetPrivateField(boss, "attackTimer", 999f);
            InvokePrivateMethod(boss, "TryEnqueueWeightedAttack");
            Assert.AreEqual(1, boss.QueuedAttackCount, "Single attack should be queued before start.");

            InvokePrivateMethod(boss, "TryStartPlannedAttack");
            Assert.IsTrue((bool)GetPrivateField(boss, "isInAttack"), "Attack should enter running state after start.");

            // Coarse wait emulates low-FPS stepping while still spanning windup/active/recovery.
            yield return new WaitForSeconds(0.16f);

            Assert.IsFalse((bool)GetPrivateField(boss, "isInAttack"), "Attack should complete without stalling under coarse frame cadence.");
            Assert.IsFalse((bool)GetPrivateField(boss, "isVulnerable"), "Vulnerable flag should close after recovery.");
            Assert.AreEqual(0, boss.QueuedAttackCount, "Attack queue should be drained after single-cycle execution.");
        }

        [UnityTest]
        public IEnumerator BossController_Phase2ComboChain_QueuesFollowupAfterOpener()
        {
            GameObject bossGo = new GameObject("Boss_Controller_Phase2Combo");
            createdObjects.Add(bossGo);
            bossGo.AddComponent<EnemyAI>();
            bossGo.AddComponent<EnemyHealth>();
            BossController boss = bossGo.AddComponent<BossController>();

            boss.enableBreakWindow = false;
            boss.useAttackQueue = true;
            boss.queuedAttackLimit = 2;
            boss.maxSameAttackQueued = 1;
            boss.attackInterval = 0f;
            boss.decisionInterval = 999f;
            boss.enablePhaseComboChain = true;
            boss.phase2ComboChance = 1f;
            boss.phase3ComboChance = 1f;
            boss.comboRepeatPenalty = 0f;
            boss.comboStartDelay = 0.2f;
            boss.currentPhase = 2;
            SetPrivateField(boss, "currentPhaseIndex", 1);

            BossAttack opener = new BossAttack
            {
                attackId = "combo_opener",
                attackName = "Combo Opener",
                selectionWeight = 0.5f,
                requiresPhase2 = true,
                targetPlayer = false,
                aoe = false,
                cooldown = 1f,
                windupTime = 0f,
                activeTime = 0.08f,
                recoveryTime = 0f
            };
            BossAttack follow = new BossAttack
            {
                attackId = "combo_follow",
                attackName = "Combo Follow",
                selectionWeight = 10f,
                requiresPhase2 = true,
                targetPlayer = false,
                aoe = false,
                cooldown = 0f,
                windupTime = 0f,
                activeTime = 0.08f,
                recoveryTime = 0f
            };

            boss.attacks.Clear();
            boss.attacks.Add(opener);

            SetPrivateField(boss, "attackTimer", 999f);
            InvokePrivateMethod(boss, "TryEnqueueWeightedAttack");
            Assert.AreEqual(1, boss.QueuedAttackCount, "Opener should be queued before start.");
            InvokePrivateMethod(boss, "TryStartPlannedAttack");
            Assert.IsTrue((bool)GetPrivateField(boss, "isInAttack"), "Opener should start immediately.");

            boss.attacks.Add(follow);
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(boss.DebugLastComboTriggered, "Phase2 combo chain should queue a follow-up.");
            Assert.AreEqual(1, boss.QueuedAttackCount, "Follow-up should remain queued after opener.");
            BossAttack queuedFollow = GetQueuedAttackPayload(PeekPlannedAttack(boss));
            Assert.NotNull(queuedFollow);
            Assert.AreEqual("combo_follow", queuedFollow.attackId);

            SetPrivateField(boss, "attackTimer", 999f);
            InvokePrivateMethod(boss, "TryStartPlannedAttack");
            Assert.IsFalse((bool)GetPrivateField(boss, "isInAttack"), "Combo start delay should gate immediate follow-up.");

            yield return new WaitForSeconds(0.22f);
            SetPrivateField(boss, "attackTimer", 999f);
            InvokePrivateMethod(boss, "TryStartPlannedAttack");
            Assert.IsTrue((bool)GetPrivateField(boss, "isInAttack"), "Follow-up should start once combo delay expires.");
        }

        [UnityTest]
        public IEnumerator BossController_InterruptRecoveryGate_BlocksImmediateRestartAfterStun()
        {
            GameObject bossGo = new GameObject("Boss_Controller_InterruptRecovery");
            createdObjects.Add(bossGo);
            bossGo.AddComponent<EnemyAI>();
            bossGo.AddComponent<EnemyHealth>();
            BossController boss = bossGo.AddComponent<BossController>();

            boss.enableBreakWindow = false;
            boss.useAttackQueue = true;
            boss.attackInterval = 0f;
            boss.decisionInterval = 999f;
            boss.enablePhaseComboChain = false;
            boss.enableInterruptRecoveryGate = true;
            boss.interruptRecoveryDuration = 0.25f;
            boss.interruptedAttackCooldownScale = 0f;

            boss.attacks.Clear();
            boss.attacks.Add(new BossAttack
            {
                attackId = "interrupt_probe",
                attackName = "Interrupt Probe",
                selectionWeight = 1f,
                targetPlayer = false,
                aoe = false,
                cooldown = 0f,
                windupTime = 0f,
                activeTime = 0.35f,
                recoveryTime = 0f
            });

            SetPrivateField(boss, "attackTimer", 999f);
            InvokePrivateMethod(boss, "TryEnqueueWeightedAttack");
            InvokePrivateMethod(boss, "TryStartPlannedAttack");
            Assert.IsTrue((bool)GetPrivateField(boss, "isInAttack"), "Probe attack should enter execution.");

            yield return null;
            boss.StunBoss(0.05f);
            Assert.IsFalse((bool)GetPrivateField(boss, "isInAttack"), "Stun should interrupt the running attack.");

            yield return new WaitForSeconds(0.06f);
            Assert.Greater(boss.DebugInterruptRecoveryTimer, 0f, "Interrupt recovery timer should be active.");

            SetPrivateField(boss, "attackTimer", 999f);
            InvokePrivateMethod(boss, "TryEnqueueWeightedAttack");
            InvokePrivateMethod(boss, "TryStartPlannedAttack");
            Assert.IsFalse((bool)GetPrivateField(boss, "isInAttack"), "Interrupt recovery gate should block immediate restart.");

            yield return new WaitForSeconds(0.24f);
            SetPrivateField(boss, "attackTimer", 999f);
            InvokePrivateMethod(boss, "TryStartPlannedAttack");
            Assert.IsTrue((bool)GetPrivateField(boss, "isInAttack"), "Attack should restart after interrupt recovery gate expires.");
        }

        [UnityTest]
        public IEnumerator BossController_InterruptRecoveryGate_LowFpsJitter_StillRespectsCounterWindow()
        {
            float originalTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 1f;

                GameObject bossGo = new GameObject("Boss_Controller_InterruptRecovery_LowFpsJitter");
                createdObjects.Add(bossGo);
                bossGo.AddComponent<EnemyAI>();
                bossGo.AddComponent<EnemyHealth>();
                BossController boss = bossGo.AddComponent<BossController>();

                boss.enableBreakWindow = false;
                boss.useAttackQueue = true;
                boss.queuedAttackLimit = 1;
                boss.attackInterval = 0f;
                boss.decisionInterval = 999f;
                boss.enablePhaseComboChain = false;
                boss.enableInterruptRecoveryGate = true;
                boss.interruptRecoveryDuration = 0.3f;
                boss.interruptedAttackCooldownScale = 0f;

                boss.attacks.Clear();
                boss.attacks.Add(new BossAttack
                {
                    attackId = "interrupt_jitter_probe",
                    attackName = "Interrupt Jitter Probe",
                    selectionWeight = 1f,
                    targetPlayer = false,
                    aoe = false,
                    cooldown = 0f,
                    windupTime = 0f,
                    activeTime = 0.45f,
                    recoveryTime = 0f
                });

                SetPrivateField(boss, "attackTimer", 999f);
                InvokePrivateMethod(boss, "TryEnqueueWeightedAttack");
                InvokePrivateMethod(boss, "TryStartPlannedAttack");
                Assert.IsTrue((bool)GetPrivateField(boss, "isInAttack"), "Probe attack should start before interrupt.");

                yield return null;
                boss.StunBoss(0.03f);
                Assert.IsFalse((bool)GetPrivateField(boss, "isInAttack"), "Stun should interrupt the active attack.");

                float[] jitterScales = { 0.35f, 0.8f, 0.45f };
                for (int i = 0; i < jitterScales.Length; i++)
                {
                    Time.timeScale = jitterScales[i];
                    SetPrivateField(boss, "attackTimer", 999f);
                    InvokePrivateMethod(boss, "TryEnqueueWeightedAttack");
                    InvokePrivateMethod(boss, "TryStartPlannedAttack");
                    Assert.IsFalse((bool)GetPrivateField(boss, "isInAttack"),
                        $"Interrupt recovery should block restart under low-FPS jitter (step {i + 1}).");
                    yield return new WaitForSecondsRealtime(0.08f);
                }

                Assert.Greater(boss.DebugInterruptRecoveryTimer, 0f, "Recovery timer should remain active through jitter window.");

                Time.timeScale = 1f;
                yield return new WaitForSecondsRealtime(0.35f);

                SetPrivateField(boss, "attackTimer", 999f);
                InvokePrivateMethod(boss, "TryStartPlannedAttack");
                Assert.IsTrue((bool)GetPrivateField(boss, "isInAttack"),
                    "Attack should be allowed again after the recovery counter window fully expires.");
            }
            finally
            {
                Time.timeScale = originalTimeScale;
            }
        }

        [Test]
        public void BossSpawnPoint_EncounterTuning_AppliesToSpawnedController()
        {
            GameObject spawnPointGo = new GameObject("BossSpawnPoint_EncounterTuning");
            createdObjects.Add(spawnPointGo);
            BossSpawnPoint spawnPoint = spawnPointGo.AddComponent<BossSpawnPoint>();
            spawnPoint.spawnOnStart = false;
            spawnPoint.bossName = "Boss_EncounterTuned";

            spawnPoint.overrideEncounterTuning = true;
            spawnPoint.phase2HealthThreshold = 0.6f;
            spawnPoint.phase3HealthThreshold = 0.28f;
            spawnPoint.breakWindowDuration = 5.2f;
            spawnPoint.breakWindowCooldown = 8.8f;
            spawnPoint.breakWindowDamageMultiplier = 1.9f;
            spawnPoint.staggerMax = 155f;
            spawnPoint.staggerPerDamage = 1.35f;
            spawnPoint.enablePhaseComboChain = true;
            spawnPoint.phase2ComboChance = 0.57f;
            spawnPoint.phase3ComboChance = 0.76f;
            spawnPoint.comboStartDelay = 0.07f;
            spawnPoint.comboRepeatPenalty = 0.19f;
            spawnPoint.enableInterruptRecoveryGate = true;
            spawnPoint.interruptRecoveryDuration = 0.14f;
            spawnPoint.interruptedAttackCooldownScale = 0.36f;
            spawnPoint.enablePhaseTransitionOpeners = true;
            spawnPoint.phase2TransitionOpenerId = "phase2_open";
            spawnPoint.phase3TransitionOpenerId = "phase3_open";
            spawnPoint.enablePhaseTransitionOpenerRetry = true;
            spawnPoint.phaseTransitionOpenerRetryDelay = 0.11f;
            spawnPoint.phaseTransitionOpenerMaxRetries = 6;
            spawnPoint.enablePhaseTransitionFollowupChain = true;
            spawnPoint.phase2TransitionFollowupId = "phase2_follow";
            spawnPoint.phase3TransitionFollowupId = "phase3_follow";
            spawnPoint.enablePhaseTransitionFollowupRetry = true;
            spawnPoint.phaseTransitionFollowupRetryDelay = 0.09f;
            spawnPoint.phaseTransitionFollowupMaxRetries = 5;
            spawnPoint.enablePhase3SpecialPriorityWindow = true;
            spawnPoint.phase3SpecialPriorityDuration = 8.4f;
            spawnPoint.phase3SpecialPriorityWeightMultiplier = 2.05f;
            spawnPoint.forceSpecialQueueDuringPhase3Priority = true;

            GameObject bossPrefab = new GameObject("BossPrefab_EncounterTuning");
            createdObjects.Add(bossPrefab);
            bossPrefab.AddComponent<EnemyHealth>();
            bossPrefab.AddComponent<EnemyAI>();
            BossController prefabController = bossPrefab.AddComponent<BossController>();
            prefabController.usePhases = true;
            prefabController.phases.Clear();
            prefabController.phases.Add(new BossPhase { phaseName = "P1", healthPercentThreshold = 1f });
            prefabController.phases.Add(new BossPhase { phaseName = "P2", healthPercentThreshold = 0.66f });
            prefabController.phases.Add(new BossPhase { phaseName = "P3", healthPercentThreshold = 0.33f });

            spawnPoint.bossPrefab = bossPrefab;
            spawnPoint.SpawnBoss();

            BossController[] allControllers = Object.FindObjectsOfType<BossController>();
            BossController spawnedController = null;
            for (int i = 0; i < allControllers.Length; i++)
            {
                BossController controller = allControllers[i];
                if (controller != null && controller.gameObject.name == "Boss_EncounterTuned")
                {
                    spawnedController = controller;
                    break;
                }
            }

            Assert.NotNull(spawnedController, "Spawned boss controller should be found.");
            createdObjects.Add(spawnedController.gameObject);

            Assert.AreEqual(5.2f, spawnedController.breakWindowDuration, 0.0001f);
            Assert.AreEqual(8.8f, spawnedController.breakWindowCooldown, 0.0001f);
            Assert.AreEqual(1.9f, spawnedController.breakWindowDamageMultiplier, 0.0001f);
            Assert.AreEqual(155f, spawnedController.staggerMax, 0.0001f);
            Assert.AreEqual(1.35f, spawnedController.staggerPerDamage, 0.0001f);
            Assert.IsTrue(spawnedController.enablePhaseComboChain);
            Assert.AreEqual(0.57f, spawnedController.phase2ComboChance, 0.0001f);
            Assert.AreEqual(0.76f, spawnedController.phase3ComboChance, 0.0001f);
            Assert.AreEqual(0.07f, spawnedController.comboStartDelay, 0.0001f);
            Assert.AreEqual(0.19f, spawnedController.comboRepeatPenalty, 0.0001f);
            Assert.IsTrue(spawnedController.enableInterruptRecoveryGate);
            Assert.AreEqual(0.14f, spawnedController.interruptRecoveryDuration, 0.0001f);
            Assert.AreEqual(0.36f, spawnedController.interruptedAttackCooldownScale, 0.0001f);
            Assert.IsTrue(spawnedController.enablePhaseTransitionOpeners);
            Assert.AreEqual("phase2_open", spawnedController.phase2TransitionOpenerId);
            Assert.AreEqual("phase3_open", spawnedController.phase3TransitionOpenerId);
            Assert.IsTrue(spawnedController.enablePhaseTransitionOpenerRetry);
            Assert.AreEqual(0.11f, spawnedController.phaseTransitionOpenerRetryDelay, 0.0001f);
            Assert.AreEqual(6, spawnedController.phaseTransitionOpenerMaxRetries);
            Assert.IsTrue(spawnedController.enablePhaseTransitionFollowupChain);
            Assert.AreEqual("phase2_follow", spawnedController.phase2TransitionFollowupId);
            Assert.AreEqual("phase3_follow", spawnedController.phase3TransitionFollowupId);
            Assert.IsTrue(spawnedController.enablePhaseTransitionFollowupRetry);
            Assert.AreEqual(0.09f, spawnedController.phaseTransitionFollowupRetryDelay, 0.0001f);
            Assert.AreEqual(5, spawnedController.phaseTransitionFollowupMaxRetries);
            Assert.IsTrue(spawnedController.enablePhase3SpecialPriorityWindow);
            Assert.AreEqual(8.4f, spawnedController.phase3SpecialPriorityDuration, 0.0001f);
            Assert.AreEqual(2.05f, spawnedController.phase3SpecialPriorityWeightMultiplier, 0.0001f);
            Assert.IsTrue(spawnedController.forceSpecialQueueDuringPhase3Priority);

            Assert.GreaterOrEqual(spawnedController.phases.Count, 3, "Boss should have at least three phases.");
            Assert.AreEqual(0.6f, spawnedController.phases[1].healthPercentThreshold, 0.0001f);
            Assert.AreEqual(0.28f, spawnedController.phases[2].healthPercentThreshold, 0.0001f);
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

        private static object DequeuePlannedAttack(BossController boss)
        {
            FieldInfo queueField = typeof(BossController).GetField("plannedAttacks", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(queueField, "plannedAttacks field should exist.");
            object queue = queueField.GetValue(boss);
            Assert.NotNull(queue, "plannedAttacks should be initialized.");

            MethodInfo dequeue = queue.GetType().GetMethod("Dequeue", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(dequeue, "Queue.Dequeue should exist.");
            return dequeue.Invoke(queue, null);
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
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{fieldName} field should exist.");
            field.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            Assert.NotNull(target);
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{fieldName} field should exist.");
            return field.GetValue(target);
        }

        private static object InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            Assert.NotNull(target);
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} should exist.");
            return method.Invoke(target, args);
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
