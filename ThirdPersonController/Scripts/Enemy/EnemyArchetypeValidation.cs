using System.Collections.Generic;

namespace ThirdPersonController
{
    public enum EnemyArchetypeValidationIssueCode
    {
        NullAsset,
        EmptyArchetypeId,
        DuplicateArchetypeId,
        UnsupportedIntensityMappingId,
        UnexpectedEnemyTypeForArchetypeId
    }

    public struct EnemyArchetypeValidationIssue
    {
        public EnemyArchetypeValidationIssueCode code;
        public EnemyArchetype archetype;
        public string normalizedArchetypeId;
        public string message;
    }

    public static class EnemyArchetypeValidation
    {
        private static readonly string[] IntensitySupportedArchetypeIds =
        {
            "grunt",
            "rusher",
            "tank",
            "elite",
            "ranged",
            "controller",
            "suicide"
        };

        private static readonly HashSet<string> IntensitySupportedArchetypeIdSet =
            new HashSet<string>(IntensitySupportedArchetypeIds);

        private static readonly Dictionary<string, EnemyType> RecommendedEnemyTypeByArchetypeId =
            new Dictionary<string, EnemyType>
            {
                { "grunt", EnemyType.Grunt },
                { "rusher", EnemyType.Rusher },
                { "tank", EnemyType.Tank },
                { "elite", EnemyType.Elite },
                { "ranged", EnemyType.Mutant },
                { "controller", EnemyType.Mutant },
                { "suicide", EnemyType.Mutant }
            };

        public static IReadOnlyList<string> GetIntensitySupportedArchetypeIds()
        {
            return IntensitySupportedArchetypeIds;
        }

        public static bool IsIntensitySupportedArchetypeId(string archetypeId)
        {
            string normalizedId = NormalizeArchetypeId(archetypeId);
            return IntensitySupportedArchetypeIdSet.Contains(normalizedId);
        }

        public static bool TryGetRecommendedEnemyType(string archetypeId, out EnemyType enemyType)
        {
            string normalizedId = NormalizeArchetypeId(archetypeId);
            return RecommendedEnemyTypeByArchetypeId.TryGetValue(normalizedId, out enemyType);
        }

        public static List<EnemyArchetypeValidationIssue> Validate(IEnumerable<EnemyArchetype> archetypes)
        {
            var issues = new List<EnemyArchetypeValidationIssue>();
            if (archetypes == null)
            {
                return issues;
            }

            var seenIds = new Dictionary<string, EnemyArchetype>();
            foreach (EnemyArchetype archetype in archetypes)
            {
                if (archetype == null)
                {
                    issues.Add(new EnemyArchetypeValidationIssue
                    {
                        code = EnemyArchetypeValidationIssueCode.NullAsset,
                        archetype = null,
                        normalizedArchetypeId = string.Empty,
                        message = "Encountered null EnemyArchetype asset reference."
                    });
                    continue;
                }

                string normalizedId = NormalizeArchetypeId(archetype.archetypeId);
                if (string.IsNullOrEmpty(normalizedId))
                {
                    issues.Add(new EnemyArchetypeValidationIssue
                    {
                        code = EnemyArchetypeValidationIssueCode.EmptyArchetypeId,
                        archetype = archetype,
                        normalizedArchetypeId = string.Empty,
                        message = "archetypeId is empty. Fill a canonical id (e.g. grunt/rusher/tank)."
                    });
                    continue;
                }

                if (seenIds.TryGetValue(normalizedId, out EnemyArchetype firstSeen))
                {
                    string firstName = firstSeen != null ? firstSeen.name : "<missing>";
                    issues.Add(new EnemyArchetypeValidationIssue
                    {
                        code = EnemyArchetypeValidationIssueCode.DuplicateArchetypeId,
                        archetype = archetype,
                        normalizedArchetypeId = normalizedId,
                        message = $"Duplicate archetypeId '{normalizedId}'. First seen in '{firstName}'."
                    });
                }
                else
                {
                    seenIds.Add(normalizedId, archetype);
                }

                if (!IsIntensitySupportedArchetypeId(normalizedId))
                {
                    issues.Add(new EnemyArchetypeValidationIssue
                    {
                        code = EnemyArchetypeValidationIssueCode.UnsupportedIntensityMappingId,
                        archetype = archetype,
                        normalizedArchetypeId = normalizedId,
                        message =
                            $"archetypeId '{normalizedId}' is not mapped by IntensityWaveDirector profile switch; current runtime falls back to multiplier 1."
                    });
                }

                if (TryGetRecommendedEnemyType(normalizedId, out EnemyType recommendedType)
                    && archetype.enemyType != recommendedType)
                {
                    issues.Add(new EnemyArchetypeValidationIssue
                    {
                        code = EnemyArchetypeValidationIssueCode.UnexpectedEnemyTypeForArchetypeId,
                        archetype = archetype,
                        normalizedArchetypeId = normalizedId,
                        message =
                            $"archetypeId '{normalizedId}' is recommended to use EnemyType '{recommendedType}', but current value is '{archetype.enemyType}'."
                    });
                }
            }

            return issues;
        }

        public static string NormalizeArchetypeId(string archetypeId)
        {
            return string.IsNullOrWhiteSpace(archetypeId) ? string.Empty : archetypeId.Trim().ToLowerInvariant();
        }
    }
}
