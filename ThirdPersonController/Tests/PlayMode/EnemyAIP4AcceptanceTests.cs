using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ThirdPersonController.Tests
{
    public class EnemyAIP4AcceptanceTests
    {
        private const string ScenePath = "Assets/Scenes/Level_01_TrenchRift.unity";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemies/ENM_Angler_01.prefab";
        private const string OutputFileName = "enemy_ai_p3_stress_metrics.csv";
        private const string LongRunOutputFileName = "enemy_ai_p4_longrun_metrics.csv";
        private const float RunTimeoutSeconds = 220f;
        private const float LongRunTimeoutSeconds = 1200f;

        private readonly System.Collections.Generic.List<Object> createdObjects = new System.Collections.Generic.List<Object>();

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

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator P4_RealScene_StressHarness_ExportsMetricsCsv()
        {
            yield return RunAcceptanceScenario(
                OutputFileName,
                RunTimeoutSeconds,
                harness => harness.ResetDefaultSteps(),
                new[] { "P3_100", "P3_150" });
        }

        [UnityTest]
        [Explicit("Manual long-run acceptance for real-device coverage.")]
        [Timeout(1800000)]
        public IEnumerator P4_RealScene_LongRun_StressHarness_ExportsMetricsCsv()
        {
            yield return RunAcceptanceScenario(
                LongRunOutputFileName,
                LongRunTimeoutSeconds,
                harness => harness.ResetLongRunSteps(),
                new[] { "P4_LONG_100_R1", "P4_LONG_150_R1" });
        }

        private static Transform ResolvePlayerTransform()
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                return taggedPlayer.transform;
            }

            PlayerMovement playerMovement = Object.FindObjectOfType<PlayerMovement>();
            if (playerMovement != null)
            {
                return playerMovement.transform;
            }

            return null;
        }

        private static void EnsurePlayerTag(GameObject playerObject)
        {
            if (playerObject == null)
            {
                return;
            }

            if (playerObject.CompareTag("Player"))
            {
                return;
            }

            playerObject.tag = "Player";
        }

        private static GameObject LoadEnemyPrefab()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
#else
            return null;
#endif
        }

        private static IEnumerator LoadSceneForPlayMode()
        {
#if UNITY_EDITOR
            AsyncOperation operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            AsyncOperation operation = SceneManager.LoadSceneAsync("Level_01_TrenchRift", LoadSceneMode.Single);
#endif
            Assert.NotNull(operation, "Failed to start async scene load operation.");
            while (!operation.isDone)
            {
                yield return null;
            }
        }

        private IEnumerator RunAcceptanceScenario(
            string outputFileName,
            float timeoutSeconds,
            System.Action<EnemyPerformanceStressHarness> configureHarness,
            string[] expectedLabels)
        {
            yield return LoadSceneForPlayMode();
            yield return null;
            yield return null;

            Transform player = ResolvePlayerTransform();
            Assert.NotNull(player, "P4 acceptance requires a valid player transform in scene.");
            EnsurePlayerTag(player.gameObject);

            GameObject enemyPrefab = LoadEnemyPrefab();
            Assert.NotNull(enemyPrefab, $"Enemy prefab not found: {EnemyPrefabPath}");

            string outputPath = Path.Combine(Application.dataPath, "ThirdPersonController", "Reports", outputFileName);
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            GameObject harnessRoot = new GameObject($"P4_AcceptanceHarnessRoot_{outputFileName}");
            createdObjects.Add(harnessRoot);

            EnemyPerformanceMetricsSampler sampler = harnessRoot.AddComponent<EnemyPerformanceMetricsSampler>();
            sampler.writeToAssetsReports = true;
            sampler.outputFileName = outputFileName;
            sampler.logStepSummary = true;
            sampler.aiSampleInterval = 0.25f;

            EnemyPerformanceStressHarness harness = harnessRoot.AddComponent<EnemyPerformanceStressHarness>();
            harness.enemyPrefab = enemyPrefab;
            harness.player = player;
            harness.metricsSampler = sampler;
            harness.autoExportCsv = true;
            harness.runOnStart = false;
            harness.spawnRadius = 16f;
            harness.spawnHeight = 0f;
            harness.minSpawnDistance = 3f;
            harness.projectSpawnToNavMesh = true;
            harness.navMeshSampleRadius = 6f;
            harness.navMeshSampleAttempts = 10;
            configureHarness?.Invoke(harness);

            Time.timeScale = 1f;
            harness.StartStressRun();
            float deadline = Time.realtimeSinceStartup + Mathf.Max(1f, timeoutSeconds);
            while (harness.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsFalse(harness.IsRunning, "Stress harness run timed out before completion.");
            yield return null;

            Assert.IsTrue(File.Exists(outputPath), $"Expected metrics CSV to be generated at: {outputPath}");
            string csv = File.ReadAllText(outputPath);
            for (int i = 0; i < expectedLabels.Length; i++)
            {
                StringAssert.Contains(expectedLabels[i], csv);
            }

            Debug.Log($"[P4Acceptance] Metrics exported: {outputPath}");
        }
    }
}
