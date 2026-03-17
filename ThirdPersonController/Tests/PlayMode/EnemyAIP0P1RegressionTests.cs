using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

            if (ObjectPoolManager.Instance != null)
            {
                Object.DestroyImmediate(ObjectPoolManager.Instance.gameObject);
            }

            if (EffectPoolManager.Instance != null)
            {
                Object.DestroyImmediate(EffectPoolManager.Instance.gameObject);
            }
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
        public void EnemyAI_InterruptionStorm_ReleasesTokenAndRestoresControlState()
        {
            EnemyCrowdCoordinator coordinator = CreateCoordinator("AI_InterruptStorm_Coordinator");
            coordinator.scaleAttackersWithNearby = false;
            coordinator.maxActiveAttackers = 1;
            coordinator.useFairTokenQueue = true;

            EnemyAI enemy = CreateEnemy("AI_InterruptStorm");
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            enemy.useCrowdCoordinator = true;
            coordinator.Register(enemy);
            SetPrivateField(enemy, "crowdCoordinator", coordinator);

            bool initialGrant = coordinator.RequestAttackToken(enemy);
            Assert.IsTrue(initialGrant, "Expected initial token grant for interruption stress setup.");

            for (int i = 0; i < 3; i++)
            {
                SetPrivateField(enemy, "hasAttackToken", true);
                SetPrivateField(enemy, "isAttacking", true);
                SetPrivateField(enemy, "isDodging", true);
                SetPrivateField(enemy, "isBlocking", true);
                SetPrivateField(enemy, "isCharging", true);
                SetPrivateField(enemy, "isFleeing", true);
                SetPrivateField(enemy, "blockDefenseBaseline", 4f);
                SetPrivateField(enemy, "blockDefenseApplied", true);
                health.defense = 14f;

                enemy.SetSuppressed(true);
                enemy.ApplyStun(0.25f);
                enemy.SetSuppressed(false);
                enemy.SetStunned(false);

                Assert.IsFalse(GetPrivateField<bool>(enemy, "hasAttackToken"));
                Assert.AreEqual(0, coordinator.ActiveAttackersCount);
                Assert.AreEqual(4f, health.defense, 0.001f, "Interruption cleanup should restore defense baseline.");

                if (i < 2)
                {
                    bool reacquired = coordinator.RequestAttackToken(enemy);
                    Assert.IsTrue(reacquired, "Expected token to be reacquired for next interruption cycle.");
                }
            }

            Assert.IsFalse(GetPrivateField<bool>(enemy, "isAttacking"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "isDodging"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "isBlocking"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "isCharging"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "isFleeing"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "isSuppressed"));
            Assert.IsFalse(GetPrivateField<bool>(enemy, "isStunned"));
            Assert.GreaterOrEqual(coordinator.TokenReleaseCount, 1, "Expected at least one token release during interruption storm.");
        }

        [Test]
        public void EnemyCrowdCoordinator_TracksTokenGrantRejectAndUtilization()
        {
            EnemyCrowdCoordinator coordinator = CreateCoordinator("AI_Crowd");
            coordinator.scaleAttackersWithNearby = false;
            coordinator.maxActiveAttackers = 1;
            coordinator.useFairTokenQueue = false;

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

        [Test]
        public void EnemyCrowdCoordinator_FairTokenQueue_GrantsInFifoOrder()
        {
            EnemyCrowdCoordinator coordinator = CreateCoordinator("AI_Crowd_FairQueue");
            coordinator.scaleAttackersWithNearby = false;
            coordinator.maxActiveAttackers = 1;
            coordinator.useFairTokenQueue = true;

            EnemyAI enemyA = CreateEnemy("AI_Crowd_Fair_A");
            EnemyAI enemyB = CreateEnemy("AI_Crowd_Fair_B");
            EnemyAI enemyC = CreateEnemy("AI_Crowd_Fair_C");

            Assert.IsTrue(coordinator.RequestAttackToken(enemyA));
            Assert.IsFalse(coordinator.RequestAttackToken(enemyB));
            Assert.IsFalse(coordinator.RequestAttackToken(enemyC));
            Assert.AreEqual(2, coordinator.WaitingAttackersCount);

            coordinator.ReleaseAttackToken(enemyA);
            Assert.IsTrue(coordinator.RequestAttackToken(enemyB), "Second requester should be served first after release.");
            Assert.IsFalse(coordinator.RequestAttackToken(enemyC), "Third requester should remain waiting while slot is occupied.");
            Assert.AreEqual(1, coordinator.WaitingAttackersCount);

            coordinator.ReleaseAttackToken(enemyB);
            Assert.IsTrue(coordinator.RequestAttackToken(enemyC), "Third requester should receive token after second releases.");
        }

        [Test]
        public void EnemyCrowdCoordinator_UnregisterQueuedEnemy_DoesNotBlockQueueProgress()
        {
            EnemyCrowdCoordinator coordinator = CreateCoordinator("AI_Crowd_UnregisterQueue");
            coordinator.scaleAttackersWithNearby = false;
            coordinator.maxActiveAttackers = 1;
            coordinator.useFairTokenQueue = true;
            coordinator.queueRequestFreshWindow = 10f;

            EnemyAI enemyA = CreateEnemy("AI_Crowd_Unregister_A");
            EnemyAI enemyB = CreateEnemy("AI_Crowd_Unregister_B");
            EnemyAI enemyC = CreateEnemy("AI_Crowd_Unregister_C");

            Assert.IsTrue(coordinator.RequestAttackToken(enemyA));
            Assert.IsFalse(coordinator.RequestAttackToken(enemyB));
            Assert.IsFalse(coordinator.RequestAttackToken(enemyC));
            Assert.AreEqual(2, coordinator.WaitingAttackersCount);

            coordinator.Unregister(enemyB);
            Assert.AreEqual(1, coordinator.WaitingAttackersCount, "Queued enemy removal should shrink queue immediately.");

            coordinator.ReleaseAttackToken(enemyA);
            Assert.IsTrue(coordinator.RequestAttackToken(enemyC), "Remaining queued enemy should receive token after release.");

            coordinator.ReleaseAttackToken(enemyC);
            Assert.AreEqual(0, coordinator.WaitingAttackersCount);
            Assert.AreEqual(0, coordinator.ActiveAttackersCount);
        }

        [Test]
        public void EnemyCrowdCoordinator_BurstContention_AllQueuedAttackersEventuallyReceiveToken()
        {
            EnemyCrowdCoordinator coordinator = CreateCoordinator("AI_Crowd_BurstContention");
            coordinator.scaleAttackersWithNearby = false;
            coordinator.maxActiveAttackers = 2;
            coordinator.useFairTokenQueue = true;
            coordinator.queueRequestFreshWindow = 10f;

            EnemyAI enemyA = CreateEnemy("AI_Crowd_Burst_A");
            EnemyAI enemyB = CreateEnemy("AI_Crowd_Burst_B");
            EnemyAI enemyC = CreateEnemy("AI_Crowd_Burst_C");
            EnemyAI enemyD = CreateEnemy("AI_Crowd_Burst_D");
            EnemyAI enemyE = CreateEnemy("AI_Crowd_Burst_E");

            Assert.IsTrue(coordinator.RequestAttackToken(enemyA));
            Assert.IsTrue(coordinator.RequestAttackToken(enemyB));
            Assert.IsFalse(coordinator.RequestAttackToken(enemyC));
            Assert.IsFalse(coordinator.RequestAttackToken(enemyD));
            Assert.IsFalse(coordinator.RequestAttackToken(enemyE));
            Assert.AreEqual(3, coordinator.WaitingAttackersCount);

            coordinator.ReleaseAttackToken(enemyA);
            coordinator.ReleaseAttackToken(enemyB);

            Assert.IsTrue(coordinator.RequestAttackToken(enemyC), "First queued attacker should eventually acquire token.");
            Assert.IsTrue(coordinator.RequestAttackToken(enemyD), "Second queued attacker should eventually acquire token.");
            Assert.IsFalse(coordinator.RequestAttackToken(enemyE), "Third queued attacker should wait while two slots are occupied.");

            coordinator.ReleaseAttackToken(enemyC);
            Assert.IsTrue(coordinator.RequestAttackToken(enemyE), "Last queued attacker should acquire token after a slot release.");

            coordinator.ReleaseAttackToken(enemyD);
            coordinator.ReleaseAttackToken(enemyE);
            Assert.AreEqual(0, coordinator.ActiveAttackersCount);
            Assert.AreEqual(0, coordinator.WaitingAttackersCount);
        }

        [Test]
        public void EnemyCrowdCoordinator_GetRingPosition_AvoidsHardStackWhenSlotsSaturated()
        {
            EnemyCrowdCoordinator coordinator = CreateCoordinator("AI_Crowd_RingConflict");
            coordinator.player = CreateTarget("Player_RingConflict", Vector3.zero);
            coordinator.ringSlots = 1;
            coordinator.ringRadius = 2.5f;
            coordinator.ringJitter = 0f;
            coordinator.avoidSlotConflicts = true;
            coordinator.overflowRadiusStep = 0.6f;

            EnemyAI enemyA = CreateEnemy("AI_Crowd_Ring_A");
            EnemyAI enemyB = CreateEnemy("AI_Crowd_Ring_B");
            coordinator.Register(enemyA);
            coordinator.Register(enemyB);

            Vector3 positionA = coordinator.GetRingPosition(enemyA);
            Vector3 positionB = coordinator.GetRingPosition(enemyB);

            float separation = Vector3.Distance(positionA, positionB);
            Assert.Greater(separation, 0.2f, "Stacked slots should receive radial separation when no free slot exists.");
        }

        [Test]
        public void EnemyCrowdCoordinator_GetRingPosition_SplitsMeleeAndRangedLayers()
        {
            EnemyCrowdCoordinator coordinator = CreateCoordinator("AI_Crowd_Layered");
            coordinator.player = CreateTarget("Player_Layered", Vector3.zero);
            coordinator.ringSlots = 8;
            coordinator.ringRadius = 2.8f;
            coordinator.ringJitter = 0f;
            coordinator.separateMeleeAndRangedLayers = true;
            coordinator.meleeRingRadiusMultiplier = 0.9f;
            coordinator.rangedRingRadiusMultiplier = 1.35f;

            EnemyAI meleeEnemy = CreateEnemy("AI_Crowd_Melee");
            EnemyAI rangedEnemy = CreateEnemy("AI_Crowd_Ranged");
            rangedEnemy.useAttackPatterns = true;
            rangedEnemy.attackPatterns = new List<EnemyAttackPattern>
            {
                new EnemyAttackPattern
                {
                    patternId = "ranged_primary",
                    isRanged = true,
                    range = 7f
                }
            };

            coordinator.Register(meleeEnemy);
            coordinator.Register(rangedEnemy);

            Vector3 meleePos = coordinator.GetRingPosition(meleeEnemy);
            Vector3 rangedPos = coordinator.GetRingPosition(rangedEnemy);

            float meleeDistance = Vector3.Distance(coordinator.player.position, meleePos);
            float rangedDistance = Vector3.Distance(coordinator.player.position, rangedPos);
            Assert.Greater(rangedDistance, meleeDistance + 0.2f, "Ranged units should hold outer ring compared to melee.");
        }

        [Test]
        public void EnemyCrowdCoordinator_GetRingPosition_HeavyContentionMaintainsPairwiseSeparation()
        {
            EnemyCrowdCoordinator coordinator = CreateCoordinator("AI_Crowd_HeavyContention");
            coordinator.player = CreateTarget("Player_HeavyContention", Vector3.zero);
            coordinator.ringSlots = 2;
            coordinator.ringRadius = 2.6f;
            coordinator.ringJitter = 0f;
            coordinator.avoidSlotConflicts = true;
            coordinator.overflowRadiusStep = 0.55f;
            coordinator.separateMeleeAndRangedLayers = false;

            List<EnemyAI> enemies = new List<EnemyAI>();
            for (int i = 0; i < 8; i++)
            {
                EnemyAI enemy = CreateEnemy($"AI_Crowd_Heavy_{i}");
                enemies.Add(enemy);
                coordinator.Register(enemy);
            }

            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < enemies.Count; i++)
            {
                positions.Add(coordinator.GetRingPosition(enemies[i]));
            }

            for (int i = 0; i < positions.Count; i++)
            {
                for (int j = i + 1; j < positions.Count; j++)
                {
                    float separation = Vector3.Distance(positions[i], positions[j]);
                    Assert.Greater(separation, 0.15f, $"Enemies {i} and {j} should not overlap under heavy slot contention.");
                }
            }
        }

        [Test]
        public void EnemyCrowdCoordinator_GetRingPosition_SaturatedLayersKeepRangedOutsideMelee()
        {
            EnemyCrowdCoordinator coordinator = CreateCoordinator("AI_Crowd_SaturatedLayers");
            coordinator.player = CreateTarget("Player_SaturatedLayers", Vector3.zero);
            coordinator.ringSlots = 2;
            coordinator.ringRadius = 2.8f;
            coordinator.ringJitter = 0f;
            coordinator.separateMeleeAndRangedLayers = true;
            coordinator.meleeRingRadiusMultiplier = 0.85f;
            coordinator.rangedRingRadiusMultiplier = 1.4f;
            coordinator.overflowRadiusStep = 0.45f;

            List<float> meleeDistances = new List<float>();
            List<float> rangedDistances = new List<float>();

            for (int i = 0; i < 4; i++)
            {
                EnemyAI melee = CreateEnemy($"AI_Crowd_Saturated_Melee_{i}");
                coordinator.Register(melee);
                Vector3 meleePos = coordinator.GetRingPosition(melee);
                meleeDistances.Add(Vector3.Distance(coordinator.player.position, meleePos));
            }

            for (int i = 0; i < 4; i++)
            {
                EnemyAI ranged = CreateEnemy($"AI_Crowd_Saturated_Ranged_{i}");
                ranged.useAttackPatterns = true;
                ranged.attackPatterns = new List<EnemyAttackPattern>
                {
                    new EnemyAttackPattern
                    {
                        patternId = $"ranged_sat_{i}",
                        isRanged = true,
                        range = 8f
                    }
                };

                coordinator.Register(ranged);
                Vector3 rangedPos = coordinator.GetRingPosition(ranged);
                rangedDistances.Add(Vector3.Distance(coordinator.player.position, rangedPos));
            }

            float maxMelee = 0f;
            for (int i = 0; i < meleeDistances.Count; i++)
            {
                maxMelee = Mathf.Max(maxMelee, meleeDistances[i]);
            }

            float minRanged = float.MaxValue;
            for (int i = 0; i < rangedDistances.Count; i++)
            {
                minRanged = Mathf.Min(minRanged, rangedDistances[i]);
            }

            Assert.Greater(minRanged, maxMelee + 0.3f, "Ranged layer should stay outside melee layer even when slots are saturated.");
        }

        [Test]
        public void EnemyAI_PerformanceLod_ResolvesFullSimplifiedMinimalByDistance()
        {
            EnemyAI enemy = CreateEnemy("AI_PerfLOD");
            enemy.enableDistanceLod = true;
            enemy.lodFullDistance = 8f;
            enemy.lodSimplifiedDistance = 18f;

            object full = InvokePrivateWithResult(enemy, "ResolveUpdateLodTier", 5f);
            object simplified = InvokePrivateWithResult(enemy, "ResolveUpdateLodTier", 12f);
            object minimal = InvokePrivateWithResult(enemy, "ResolveUpdateLodTier", 30f);

            Assert.AreEqual("Full", full.ToString());
            Assert.AreEqual("Simplified", simplified.ToString());
            Assert.AreEqual("Minimal", minimal.ToString());
        }

        [UnityTest]
        public System.Collections.IEnumerator EnemyAI_LowFpsJitter_DecisionIntervalIsClampedAndAIStaysResponsive()
        {
            EnemyAI enemy = CreateEnemy("AI_LowFpsJitter");
            Transform target = CreateTarget("Target_LowFpsJitter", new Vector3(12f, 0f, 0f));
            enemy.SetOverrideTarget(target, true);
            enemy.patrolPoints = new Transform[0];

            enemy.scaleDecisionIntervalWithCrowd = false;
            enemy.enableDistanceLod = true;
            enemy.enableBatchDecisionTick = true;
            enemy.simplifiedBatchModulo = 2;
            enemy.minimalBatchModulo = 3;
            enemy.detectionRange = 0.1f;
            enemy.aiUpdateInterval = 0.01f;
            enemy.nearUpdateInterval = 0.005f;
            enemy.farUpdateInterval = 0.012f;
            enemy.aiUpdateJitter = 0.2f;
            enemy.maxDecisionInterval = 0.25f;

            int originalTargetFrameRate = Application.targetFrameRate;
            float originalTimeScale = Time.timeScale;
            Application.targetFrameRate = 8;

            for (int i = 0; i < 30; i++)
            {
                Time.timeScale = (i % 3 == 0) ? 0.35f : (i % 3 == 1 ? 1f : 0.6f);
                yield return null;
            }

            Time.timeScale = originalTimeScale;
            Application.targetFrameRate = originalTargetFrameRate;

            EnemyAI.EnemyAIDebugSnapshot snapshot = enemy.GetDebugSnapshot();
            Assert.Greater(snapshot.decisionCount, 0, "AI should still execute decisions under low-FPS jitter.");
            Assert.GreaterOrEqual(
                snapshot.lastDecisionIntervalSeconds,
                0.0195f,
                "Decision interval should stay clamped to >= 0.02s even with strong negative jitter.");
        }

        [Test]
        public void EnemyCrowdCoordinator_WaveSwitch_MassUnregisterReregister_NoStaleQueueAndNoStarvation()
        {
            EnemyCrowdCoordinator coordinator = CreateCoordinator("AI_Crowd_WaveSwitch");
            coordinator.scaleAttackersWithNearby = false;
            coordinator.maxActiveAttackers = 3;
            coordinator.useFairTokenQueue = true;
            coordinator.queueRequestFreshWindow = 15f;
            coordinator.maxQueueGrantsPerTick = 16;

            List<EnemyAI> wave1 = CreateEnemyBatch("AI_Crowd_W1", 16);
            List<EnemyAI> wave2 = CreateEnemyBatch("AI_Crowd_W2", 16);

            for (int i = 0; i < wave1.Count; i++)
            {
                coordinator.RequestAttackToken(wave1[i]);
            }

            Assert.AreEqual(3, coordinator.ActiveAttackersCount);
            Assert.GreaterOrEqual(coordinator.WaitingAttackersCount, 10, "Wave1 should build a waiting queue under contention.");

            for (int i = 0; i < wave1.Count; i++)
            {
                coordinator.Unregister(wave1[i]);
            }

            Assert.AreEqual(0, coordinator.ActiveAttackersCount, "After wave switch, active attackers should reset.");
            Assert.AreEqual(0, coordinator.WaitingAttackersCount, "After wave switch, stale queue entries should be cleared.");

            for (int i = 0; i < wave2.Count; i++)
            {
                coordinator.RequestAttackToken(wave2[i]);
            }

            Assert.AreEqual(3, coordinator.ActiveAttackersCount);
            Assert.GreaterOrEqual(coordinator.WaitingAttackersCount, 10, "Wave2 should rebuild queue without being blocked by wave1 residue.");

            HashSet<EnemyAI> grantedAtLeastOnce = new HashSet<EnemyAI>();
            for (int round = 0; round < 12 && grantedAtLeastOnce.Count < wave2.Count; round++)
            {
                List<EnemyAI> activeThisRound = new List<EnemyAI>();
                for (int i = 0; i < wave2.Count; i++)
                {
                    EnemyAI enemy = wave2[i];
                    if (coordinator.RequestAttackToken(enemy))
                    {
                        grantedAtLeastOnce.Add(enemy);
                        activeThisRound.Add(enemy);
                    }
                }

                for (int i = 0; i < activeThisRound.Count; i++)
                {
                    coordinator.ReleaseAttackToken(activeThisRound[i]);
                }
            }

            Assert.AreEqual(
                wave2.Count,
                grantedAtLeastOnce.Count,
                "All wave2 enemies should eventually receive token access after repeated contention rounds.");

            for (int i = 0; i < wave2.Count; i++)
            {
                coordinator.Unregister(wave2[i]);
            }

            Assert.AreEqual(0, coordinator.ActiveAttackersCount);
            Assert.AreEqual(0, coordinator.WaitingAttackersCount);
        }

        [Test]
        public void EnemyProjectile_LifetimeExpiry_DespawnsToPool()
        {
            GameObject projectilePrefab = new GameObject("EnemyProjectile_Prefab");
            createdObjects.Add(projectilePrefab);
            projectilePrefab.AddComponent<SphereCollider>();
            projectilePrefab.AddComponent<Rigidbody>();
            EnemyProjectile prefabProjectile = projectilePrefab.AddComponent<EnemyProjectile>();
            prefabProjectile.lifetime = 0.01f;

            GameObject projectileObj = ObjectPoolManager.Spawn(projectilePrefab, Vector3.zero, Quaternion.identity);
            createdObjects.Add(projectileObj);
            EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();
            Assert.NotNull(projectile);

            projectile.Launch(Vector3.forward, null);
            SetPrivateField(projectile, "timer", projectile.lifetime);
            InvokePrivate(projectile, "Update");

            Assert.IsFalse(projectileObj.activeSelf, "Projectile should return to pool when lifetime is exceeded.");
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

        private List<EnemyAI> CreateEnemyBatch(string namePrefix, int count)
        {
            List<EnemyAI> enemies = new List<EnemyAI>(Mathf.Max(0, count));
            for (int i = 0; i < count; i++)
            {
                enemies.Add(CreateEnemy($"{namePrefix}_{i}"));
            }

            return enemies;
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

        private static object InvokePrivateWithResult(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"Expected private method: {methodName}");
            return method.Invoke(target, args);
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
