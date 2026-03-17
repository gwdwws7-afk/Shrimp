using System.Collections.Generic;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

public static class EnemyArchetypeValidationMenu
{
    private const string MenuPath = "Tools/AI/Validate Enemy Archetypes (P0)";

    [MenuItem(MenuPath)]
    public static void ValidateEnemyArchetypes()
    {
        string[] guids = AssetDatabase.FindAssets("t:EnemyArchetype");
        var archetypes = new List<EnemyArchetype>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EnemyArchetype archetype = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(path);
            if (archetype != null)
            {
                archetypes.Add(archetype);
            }
        }

        List<EnemyArchetypeValidationIssue> issues = EnemyArchetypeValidation.Validate(archetypes);
        if (issues.Count == 0)
        {
            string okMessage = $"Validated {archetypes.Count} EnemyArchetype assets. No issues found.";
            Debug.Log($"[EnemyArchetypeValidation] {okMessage}");
            EditorUtility.DisplayDialog("Enemy Archetype Validation", okMessage, "OK");
            return;
        }

        int errorCount = 0;
        int warningCount = 0;
        for (int i = 0; i < issues.Count; i++)
        {
            EnemyArchetypeValidationIssue issue = issues[i];
            bool isError = IsError(issue.code);
            string prefix = isError ? "ERROR" : "WARN";
            string ownerName = issue.archetype != null ? issue.archetype.name : "<null>";
            string message = $"[EnemyArchetypeValidation] [{prefix}] [{issue.code}] {ownerName}: {issue.message}";
            if (isError)
            {
                errorCount++;
                Debug.LogError(message, issue.archetype);
            }
            else
            {
                warningCount++;
                Debug.LogWarning(message, issue.archetype);
            }
        }

        var summary = new StringBuilder();
        summary.AppendLine($"Validated assets: {archetypes.Count}");
        summary.AppendLine($"Errors: {errorCount}");
        summary.AppendLine($"Warnings: {warningCount}");
        summary.AppendLine("See Console for per-asset details.");
        summary.AppendLine();
        summary.Append("Supported Intensity ids: ");

        IReadOnlyList<string> supportedIds = EnemyArchetypeValidation.GetIntensitySupportedArchetypeIds();
        for (int i = 0; i < supportedIds.Count; i++)
        {
            if (i > 0)
            {
                summary.Append(", ");
            }

            summary.Append(supportedIds[i]);
        }

        EditorUtility.DisplayDialog("Enemy Archetype Validation", summary.ToString(), "OK");
    }

    private static bool IsError(EnemyArchetypeValidationIssueCode code)
    {
        return code == EnemyArchetypeValidationIssueCode.NullAsset
            || code == EnemyArchetypeValidationIssueCode.EmptyArchetypeId
            || code == EnemyArchetypeValidationIssueCode.DuplicateArchetypeId;
    }
}
