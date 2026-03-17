using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class EnemyAIP3StressRegressionTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();
        private readonly List<string> createdFiles = new List<string>();

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

            for (int i = 0; i < createdFiles.Count; i++)
            {
                string file = createdFiles[i];
                if (!string.IsNullOrEmpty(file) && File.Exists(file))
                {
                    File.Delete(file);
                }
            }

            createdFiles.Clear();

            if (ObjectPoolManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(ObjectPoolManager.Instance.gameObject);
            }

            if (EffectPoolManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(EffectPoolManager.Instance.gameObject);
            }
        }

        [Test]
        public void EnemyPerformanceMetricsSampler_BeginEndStep_ExportsCsv()
        {
            GameObject samplerGo = new GameObject("P3Sampler");
            createdObjects.Add(samplerGo);
            EnemyPerformanceMetricsSampler sampler = samplerGo.AddComponent<EnemyPerformanceMetricsSampler>();
            sampler.writeToAssetsReports = false;
            sampler.logStepSummary = false;
            sampler.outputFileName = $"enemy_ai_p3_metrics_test_{Guid.NewGuid():N}.csv";

            sampler.BeginStep("P3Smoke_100", 100);
            sampler.EndStep();

            Assert.AreEqual(1, sampler.RecordedStepCount);

            string outputPath = sampler.ExportCsv();
            createdFiles.Add(outputPath);

            Assert.IsFalse(string.IsNullOrEmpty(outputPath));
            Assert.IsTrue(File.Exists(outputPath), "Expected CSV file to be created.");

            string csv = File.ReadAllText(outputPath);
            StringAssert.Contains("P3Smoke_100", csv);
            StringAssert.Contains("target_count", csv);
        }

        [Test]
        public void EnemyPerformanceStressHarness_DefaultStepsContain100And150Targets()
        {
            GameObject go = new GameObject("P3Harness_Defaults");
            createdObjects.Add(go);
            EnemyPerformanceStressHarness harness = go.AddComponent<EnemyPerformanceStressHarness>();
            harness.steps.Clear();

            harness.EnsureDefaultSteps();

            Assert.GreaterOrEqual(harness.steps.Count, 2);
            Assert.AreEqual(100, harness.steps[0].targetEnemyCount);
            Assert.AreEqual(150, harness.steps[1].targetEnemyCount);
        }

        [Test]
        public void EnemyPerformanceStressHarness_LongRunPreset_ConfiguresLongLabelsAndRepeats()
        {
            GameObject go = new GameObject("P3Harness_LongRunPreset");
            createdObjects.Add(go);
            EnemyPerformanceStressHarness harness = go.AddComponent<EnemyPerformanceStressHarness>();

            harness.steps.Clear();
            harness.longRunSampleSecondsFor100 = 90f;
            harness.longRunSampleSecondsFor150 = 120f;
            harness.longRunRepeatCount = 3;
            harness.ApplyLongRunPreset(true);

            Assert.AreEqual(2, harness.steps.Count);
            Assert.AreEqual("P4_LONG_100", harness.steps[0].label);
            Assert.AreEqual("P4_LONG_150", harness.steps[1].label);
            Assert.GreaterOrEqual(harness.steps[0].sampleSeconds, 90f);
            Assert.GreaterOrEqual(harness.steps[1].sampleSeconds, 120f);
            Assert.AreEqual(3, harness.repeatPerStep);
            Assert.IsTrue(harness.appendRepeatIndexToStepLabel);
        }

        [Test]
        public void EnemyPerformanceStressHarness_SpawnAndClear_MatchesTargetCount()
        {
            GameObject harnessGo = new GameObject("P3Harness_Runtime");
            createdObjects.Add(harnessGo);
            EnemyPerformanceStressHarness harness = harnessGo.AddComponent<EnemyPerformanceStressHarness>();
            EnemyPerformanceMetricsSampler sampler = harnessGo.AddComponent<EnemyPerformanceMetricsSampler>();
            sampler.logStepSummary = false;

            GameObject playerGo = new GameObject("P3Harness_Player");
            createdObjects.Add(playerGo);
            harness.player = playerGo.transform;
            harness.metricsSampler = sampler;
            harness.enemyPrefab = CreateEnemyHealthPrefab("P3Harness_EnemyPrefab");
            harness.projectSpawnToNavMesh = false;

            int baseline = harness.GetActiveEnemyCount();
            harness.SpawnToTargetImmediate(baseline + 12);

            Assert.GreaterOrEqual(harness.GetActiveEnemyCount(), baseline + 12);

            harness.ClearEnemiesNow();
            Assert.LessOrEqual(
                harness.GetActiveEnemyCount(),
                baseline,
                "Clear should remove spawned runtime enemies and return to initial baseline.");
        }

        private GameObject CreateEnemyHealthPrefab(string name)
        {
            GameObject prefab = new GameObject(name);
            createdObjects.Add(prefab);
            EnemyHealth health = prefab.AddComponent<EnemyHealth>();
            health.maxHealth = 20;
            health.OnSpawned();
            return prefab;
        }
    }
}
