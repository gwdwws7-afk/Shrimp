using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum EnemyTypeBindingIssueSeverity
    {
        Warning,
        Error
    }

    public enum EnemyTypeBindingIssueCode
    {
        NullPrefab,
        MissingWaveGroupPrefab,
        MissingArchetypeConfigurator,
        MissingArchetypeReference,
        EmptyArchetypeId,
        UnsupportedArchetypeId,
        EnemyTypeMismatch
    }

    public struct EnemyTypeBindingIssue
    {
        public EnemyTypeBindingIssueSeverity severity;
        public EnemyTypeBindingIssueCode code;
        public GameObject prefab;
        public string context;
        public string normalizedArchetypeId;
        public string message;
    }

    public static class EnemyTypeBindingValidation
    {
        public static List<EnemyTypeBindingIssue> ValidatePrefabBinding(
            GameObject prefab,
            string context,
            bool ignoreBossPrefabs = true)
        {
            var issues = new List<EnemyTypeBindingIssue>();
            if (prefab == null)
            {
                AddIssue(
                    issues,
                    EnemyTypeBindingIssueSeverity.Error,
                    EnemyTypeBindingIssueCode.NullPrefab,
                    null,
                    context,
                    string.Empty,
                    "Prefab reference is null.");
                return issues;
            }

            EnemyAI ai = prefab.GetComponentInChildren<EnemyAI>(true);
            EnemyHealth health = prefab.GetComponentInChildren<EnemyHealth>(true);
            if (ai == null && health == null)
            {
                return issues;
            }

            bool isBossLike = IsBossLikePrefab(prefab, health);
            if (ignoreBossPrefabs && isBossLike)
            {
                return issues;
            }

            EnemyArchetypeConfigurator configurator = prefab.GetComponentInChildren<EnemyArchetypeConfigurator>(true);
            if (configurator == null)
            {
                AddIssue(
                    issues,
                    EnemyTypeBindingIssueSeverity.Error,
                    EnemyTypeBindingIssueCode.MissingArchetypeConfigurator,
                    prefab,
                    context,
                    string.Empty,
                    "Enemy prefab is missing EnemyArchetypeConfigurator.");
                return issues;
            }

            ValidateArchetypeReference(issues, prefab, context, configurator.archetype, health);
            return issues;
        }

        public static List<EnemyTypeBindingIssue> ValidateWaveGroupBinding(
            WaveSpawnGroup group,
            string context,
            bool ignoreBossPrefabs = true)
        {
            var issues = new List<EnemyTypeBindingIssue>();
            if (group == null)
            {
                return issues;
            }

            if (group.prefab == null)
            {
                AddIssue(
                    issues,
                    EnemyTypeBindingIssueSeverity.Error,
                    EnemyTypeBindingIssueCode.MissingWaveGroupPrefab,
                    null,
                    context,
                    string.Empty,
                    "Wave spawn group has null prefab.");
                return issues;
            }

            EnemyHealth health = group.prefab.GetComponentInChildren<EnemyHealth>(true);

            // Wave group override is authoritative for this spawn entry.
            if (group.archetypeOverride != null)
            {
                ValidateArchetypeReference(issues, group.prefab, context, group.archetypeOverride, health);
                return issues;
            }

            List<EnemyTypeBindingIssue> prefabIssues = ValidatePrefabBinding(group.prefab, context, ignoreBossPrefabs);
            issues.AddRange(prefabIssues);
            return issues;
        }

        public static bool IsBossLikePrefab(GameObject prefab)
        {
            EnemyHealth health = prefab != null ? prefab.GetComponentInChildren<EnemyHealth>(true) : null;
            return IsBossLikePrefab(prefab, health);
        }

        private static bool IsBossLikePrefab(GameObject prefab, EnemyHealth health)
        {
            if (prefab == null)
            {
                return false;
            }

            if (health != null && health.enemyType == EnemyType.Boss)
            {
                return true;
            }

            if (prefab.GetComponentInChildren<BossController>(true) != null)
            {
                return true;
            }

            if (prefab.GetComponentInChildren<BossCombatTemplate>(true) != null)
            {
                return true;
            }

            if (prefab.GetComponentInChildren<BossEelPrototype>(true) != null)
            {
                return true;
            }

            return prefab.GetComponentInChildren<BossGuardianPrototype>(true) != null;
        }

        private static void ValidateArchetypeReference(
            List<EnemyTypeBindingIssue> issues,
            GameObject prefab,
            string context,
            EnemyArchetype archetype,
            EnemyHealth health)
        {
            if (archetype == null)
            {
                AddIssue(
                    issues,
                    EnemyTypeBindingIssueSeverity.Error,
                    EnemyTypeBindingIssueCode.MissingArchetypeReference,
                    prefab,
                    context,
                    string.Empty,
                    "Archetype binding is null.");
                return;
            }

            string normalizedId = EnemyArchetypeValidation.NormalizeArchetypeId(archetype.archetypeId);
            if (string.IsNullOrEmpty(normalizedId))
            {
                AddIssue(
                    issues,
                    EnemyTypeBindingIssueSeverity.Error,
                    EnemyTypeBindingIssueCode.EmptyArchetypeId,
                    prefab,
                    context,
                    string.Empty,
                    "Bound archetype has empty archetypeId.");
                return;
            }

            if (!EnemyArchetypeValidation.IsIntensitySupportedArchetypeId(normalizedId))
            {
                AddIssue(
                    issues,
                    EnemyTypeBindingIssueSeverity.Warning,
                    EnemyTypeBindingIssueCode.UnsupportedArchetypeId,
                    prefab,
                    context,
                    normalizedId,
                    $"archetypeId '{normalizedId}' is not covered by IntensityWaveDirector mapping.");
            }

            if (health != null && health.enemyType != archetype.enemyType)
            {
                AddIssue(
                    issues,
                    EnemyTypeBindingIssueSeverity.Warning,
                    EnemyTypeBindingIssueCode.EnemyTypeMismatch,
                    prefab,
                    context,
                    normalizedId,
                    $"EnemyHealth.enemyType='{health.enemyType}' differs from archetype.enemyType='{archetype.enemyType}'.");
            }
        }

        private static void AddIssue(
            List<EnemyTypeBindingIssue> issues,
            EnemyTypeBindingIssueSeverity severity,
            EnemyTypeBindingIssueCode code,
            GameObject prefab,
            string context,
            string normalizedArchetypeId,
            string message)
        {
            issues.Add(new EnemyTypeBindingIssue
            {
                severity = severity,
                code = code,
                prefab = prefab,
                context = context ?? string.Empty,
                normalizedArchetypeId = normalizedArchetypeId ?? string.Empty,
                message = message ?? string.Empty
            });
        }
    }
}
