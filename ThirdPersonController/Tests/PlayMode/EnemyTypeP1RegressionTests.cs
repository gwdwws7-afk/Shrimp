using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class EnemyTypeP1RegressionTests
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
        public void EnemyTypeBindingValidation_PrefabWithoutConfigurator_ReportsError()
        {
            GameObject prefab = CreateEnemyPrefab("P1_NoConfigurator", EnemyType.Grunt);
            List<EnemyTypeBindingIssue> issues = EnemyTypeBindingValidation.ValidatePrefabBinding(prefab, "P1");

            Assert.IsTrue(ContainsIssue(issues, EnemyTypeBindingIssueCode.MissingArchetypeConfigurator, EnemyTypeBindingIssueSeverity.Error));
        }

        [Test]
        public void EnemyTypeBindingValidation_BossPrefab_ExemptWhenIgnoreBossEnabled()
        {
            GameObject bossPrefab = CreateEnemyPrefab("P1_BossPrefab", EnemyType.Boss);

            List<EnemyTypeBindingIssue> ignoredBossIssues =
                EnemyTypeBindingValidation.ValidatePrefabBinding(bossPrefab, "P1", ignoreBossPrefabs: true);
            List<EnemyTypeBindingIssue> strictIssues =
                EnemyTypeBindingValidation.ValidatePrefabBinding(bossPrefab, "P1", ignoreBossPrefabs: false);

            Assert.IsFalse(
                ContainsIssue(ignoredBossIssues, EnemyTypeBindingIssueCode.MissingArchetypeConfigurator),
                "Boss-like prefab should be ignored when ignoreBossPrefabs is enabled.");
            Assert.IsTrue(
                ContainsIssue(strictIssues, EnemyTypeBindingIssueCode.MissingArchetypeConfigurator),
                "Strict mode should still report missing configurator for boss-like prefab.");
        }

        [Test]
        public void EnemyTypeBindingValidation_UnsupportedArchetypeAndTypeMismatch_ReportedAsWarnings()
        {
            GameObject prefab = CreateEnemyPrefab("P1_UnsupportedAndMismatch", EnemyType.Grunt);
            EnemyArchetypeConfigurator configurator = prefab.AddComponent<EnemyArchetypeConfigurator>();
            configurator.applyOnStart = false;
            configurator.applyOnSpawned = false;

            EnemyArchetype archetype = CreateArchetype("unknown_type", EnemyType.Rusher);
            configurator.archetype = archetype;

            List<EnemyTypeBindingIssue> issues = EnemyTypeBindingValidation.ValidatePrefabBinding(prefab, "P1");

            Assert.IsTrue(ContainsIssue(issues, EnemyTypeBindingIssueCode.UnsupportedArchetypeId, EnemyTypeBindingIssueSeverity.Warning));
            Assert.IsTrue(ContainsIssue(issues, EnemyTypeBindingIssueCode.EnemyTypeMismatch, EnemyTypeBindingIssueSeverity.Warning));
        }

        [Test]
        public void EnemyTypeBindingValidation_WaveGroupWithOverride_DoesNotRequirePrefabConfigurator()
        {
            GameObject prefab = CreateEnemyPrefab("P1_WaveOverride", EnemyType.Grunt);
            WaveSpawnGroup group = new WaveSpawnGroup
            {
                prefab = prefab,
                count = 3,
                archetypeOverride = CreateArchetype("elite", EnemyType.Elite)
            };

            List<EnemyTypeBindingIssue> issues = EnemyTypeBindingValidation.ValidateWaveGroupBinding(group, "P1");
            Assert.IsFalse(ContainsIssue(issues, EnemyTypeBindingIssueCode.MissingArchetypeConfigurator));
        }

        [Test]
        public void EnemyArchetypeValidation_RecommendedEnemyTypeMismatch_IsReported()
        {
            EnemyArchetype archetype = CreateArchetype("rusher", EnemyType.Tank);
            List<EnemyArchetypeValidationIssue> issues = EnemyArchetypeValidation.Validate(new[] { archetype });

            Assert.IsTrue(ContainsArchetypeIssue(issues, EnemyArchetypeValidationIssueCode.UnexpectedEnemyTypeForArchetypeId, "rusher"));
        }

        private GameObject CreateEnemyPrefab(string name, EnemyType enemyType)
        {
            GameObject go = new GameObject(name);
            createdObjects.Add(go);

            go.AddComponent<EnemyAI>();
            EnemyHealth health = go.GetComponent<EnemyHealth>();
            health.enemyType = enemyType;
            return go;
        }

        private EnemyArchetype CreateArchetype(string archetypeId, EnemyType enemyType)
        {
            EnemyArchetype archetype = ScriptableObject.CreateInstance<EnemyArchetype>();
            archetype.archetypeId = archetypeId;
            archetype.enemyType = enemyType;
            createdObjects.Add(archetype);
            return archetype;
        }

        private static bool ContainsIssue(
            IReadOnlyList<EnemyTypeBindingIssue> issues,
            EnemyTypeBindingIssueCode code,
            EnemyTypeBindingIssueSeverity? severity = null)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                EnemyTypeBindingIssue issue = issues[i];
                if (issue.code != code)
                {
                    continue;
                }

                if (!severity.HasValue || issue.severity == severity.Value)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsArchetypeIssue(
            IReadOnlyList<EnemyArchetypeValidationIssue> issues,
            EnemyArchetypeValidationIssueCode code,
            string normalizedId)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                EnemyArchetypeValidationIssue issue = issues[i];
                if (issue.code != code)
                {
                    continue;
                }

                if (issue.normalizedArchetypeId == normalizedId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
