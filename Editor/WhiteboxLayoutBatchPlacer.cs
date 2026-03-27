using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WhiteboxLayoutBatchPlacer
{
    private const string LayoutCsvPath = "Assets/GameDesign/游戏设计/33A_WhiteboxLayoutSpec_10Levels_FinalPolish.csv";
    private const string EnemyCsvPath = "Assets/GameDesign/游戏设计/33B_EnemySpawnPointPlan_10Levels_FinalPolish.csv";

    private const string LayoutRootName = "WB_AutoLayout";
    private const string EnemyRootName = "WB_EnemySpawns";

    private static readonly Dictionary<string, string> EnemyPrefabByTag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "grunt", "Assets/Prefabs/Enemies/ENM_DeepseaFish_01.prefab" },
        { "rusher", "Assets/Prefabs/Enemies/ENM_MantisShrimp_01.prefab" },
        { "ranged", "Assets/Prefabs/Enemies/ENM_Squid_01.prefab" },
        { "controller", "Assets/Prefabs/Enemies/ENM_Squid_01.prefab" },
        { "tank", "Assets/Prefabs/Enemies/ENM_HermitCrab_01.prefab" },
        { "elite", "Assets/Prefabs/Enemies/ENM_Angler_01.prefab" },
        { "suicide", "Assets/Prefabs/Enemies/ENM_SeaUrchin_01.prefab" }
    };

    [MenuItem("Tools/Whitebox/Apply 33A+33B (All Levels)")]
    public static void ApplyFromMenu()
    {
        ExecutePipeline(exitOnFinish: false);
    }

    // Batch mode entry:
    // Unity.exe -batchmode -projectPath <path> -executeMethod WhiteboxLayoutBatchPlacer.RunBatch -quit
    public static void RunBatch()
    {
        ExecutePipeline(exitOnFinish: true);
    }

    private static void ExecutePipeline(bool exitOnFinish)
    {
        try
        {
            List<Dictionary<string, string>> layoutRows = ReadCsv(LayoutCsvPath);
            List<Dictionary<string, string>> enemyRows = ReadCsv(EnemyCsvPath);

            Dictionary<string, List<Dictionary<string, string>>> enemyByScene = GroupBy(enemyRows, "scene_name");

            int updatedScenes = 0;

            foreach (Dictionary<string, string> row in layoutRows)
            {
                string sceneName = Get(row, "scene_name");
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    Debug.LogWarning("WhiteboxLayoutBatchPlacer: missing scene_name, skip row.");
                    continue;
                }

                string scenePath = $"Assets/Scenes/{sceneName}.unity";
                if (!File.Exists(ToAbsolutePath(scenePath)))
                {
                    Debug.LogWarning($"WhiteboxLayoutBatchPlacer: scene not found -> {scenePath}");
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                CleanGeneratedRoots();

                GameObject layoutRoot = new GameObject(LayoutRootName);
                BuildWhiteboxLayout(layoutRoot.transform, row);

                GameObject enemyRoot = new GameObject(EnemyRootName);
                if (enemyByScene.TryGetValue(sceneName, out List<Dictionary<string, string>> sceneEnemyRows))
                {
                    BuildEnemySpawnMarkers(enemyRoot.transform, scene, sceneEnemyRows);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                updatedScenes++;
                Debug.Log($"WhiteboxLayoutBatchPlacer: saved {scenePath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"WhiteboxLayoutBatchPlacer: done. Scenes updated = {updatedScenes}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WhiteboxLayoutBatchPlacer failed: {ex}");
            if (exitOnFinish)
            {
                EditorApplication.Exit(1);
            }
            return;
        }

        if (exitOnFinish)
        {
            EditorApplication.Exit(0);
        }
    }

    private static void CleanGeneratedRoots()
    {
        GameObject oldLayout = GameObject.Find(LayoutRootName);
        if (oldLayout != null)
        {
            UnityEngine.Object.DestroyImmediate(oldLayout);
        }

        GameObject oldEnemy = GameObject.Find(EnemyRootName);
        if (oldEnemy != null)
        {
            UnityEngine.Object.DestroyImmediate(oldEnemy);
        }
    }

    private static void BuildWhiteboxLayout(Transform root, Dictionary<string, string> row)
    {
        Vector3 entry = ParseVector3(Get(row, "entry_anchor"));
        Vector3 hub = ParseVector3(Get(row, "hub_anchor"));
        Vector3 strongholdA = ParseVector3(Get(row, "stronghold_a_center"));
        Vector3 transition = ParseVector3(Get(row, "transition_anchor"));
        Vector3 strongholdB = ParseVector3(Get(row, "stronghold_b_center"));
        Vector3 bossGate = ParseVector3(Get(row, "boss_gate_anchor"));
        Vector3 bossArena = ParseVector3(Get(row, "boss_arena_center"));

        Vector3 strongholdC = ParseVector3OrFallback(
            Get(row, "stronghold_c_center"),
            Vector3.Lerp(strongholdB, bossGate, 0.52f) + LateralOffset(strongholdB, bossGate, 14f, leftSide: true));
        Vector3 detourLeft = ParseVector3OrFallback(
            Get(row, "detour_left_anchor"),
            Vector3.Lerp(strongholdA, transition, 0.45f) + LateralOffset(strongholdA, transition, 18f, leftSide: true));
        Vector3 detourRight = ParseVector3OrFallback(
            Get(row, "detour_right_anchor"),
            Vector3.Lerp(transition, strongholdC, 0.58f) + LateralOffset(transition, strongholdC, 20f, leftSide: false));

        float mainLaneWidth = ParseFloat(Get(row, "main_lane_width_m"), 12f);
        float sideLaneWidth = ParseFloat(Get(row, "side_lane_width_m"), 8f);
        float detourLaneWidth = ParseFloat(Get(row, "detour_lane_width_m"), sideLaneWidth);

        Vector2 arenaASize = ParseSizeXZ(Get(row, "arena_a_size_m"), new Vector2(36f, 32f));
        Vector2 arenaBSize = ParseSizeXZ(Get(row, "arena_b_size_m"), new Vector2(40f, 36f));
        Vector2 arenaCSize = ParseSizeXZ(
            Get(row, "arena_c_size_m"),
            new Vector2(Mathf.Max(arenaBSize.x, 42f), Mathf.Max(arenaBSize.y, 38f)));
        Vector2 preBossSize = ParseSizeXZ(Get(row, "boss_pre_room_size_m"), new Vector2(30f, 24f));

        CreateAnchor(root, "EntryAnchor", entry);
        CreateAnchor(root, "HubAnchor", hub);
        CreateAnchor(root, "StrongholdA_Anchor", strongholdA);
        CreateAnchor(root, "TransitionAnchor", transition);
        CreateAnchor(root, "StrongholdB_Anchor", strongholdB);
        CreateAnchor(root, "StrongholdC_Anchor", strongholdC);
        CreateAnchor(root, "DetourLeft_Anchor", detourLeft);
        CreateAnchor(root, "DetourRight_Anchor", detourRight);
        CreateAnchor(root, "BossGate_Anchor", bossGate);
        CreateAnchor(root, "BossArena_Anchor", bossArena);

        Transform lanesRoot = CreateChild(root, "Lanes");
        CreateLane(lanesRoot, "Lane_Entry_Hub", entry, hub, mainLaneWidth, 1.0f);
        CreateLane(lanesRoot, "Lane_Hub_A", hub, strongholdA, mainLaneWidth, 1.0f);
        CreateLane(lanesRoot, "Lane_A_Transition", strongholdA, transition, mainLaneWidth, 1.0f);
        CreateLane(lanesRoot, "Lane_Transition_B", transition, strongholdB, mainLaneWidth, 1.0f);
        CreateLane(lanesRoot, "Lane_B_C", strongholdB, strongholdC, mainLaneWidth, 1.0f);
        CreateLane(lanesRoot, "Lane_C_BossGate", strongholdC, bossGate, mainLaneWidth, 1.0f);
        CreateLane(lanesRoot, "Lane_BossGate_BossArena", bossGate, bossArena, mainLaneWidth, 1.0f);
        CreateLane(lanesRoot, "Lane_Hub_Transition_Shortcut", hub, transition, detourLaneWidth, 0.8f);
        CreateLane(lanesRoot, "Lane_A_DetourLeft", strongholdA, detourLeft, detourLaneWidth, 0.8f);
        CreateLane(lanesRoot, "Lane_DetourLeft_B", detourLeft, strongholdB, detourLaneWidth, 0.8f);
        CreateLane(lanesRoot, "Lane_Transition_DetourRight", transition, detourRight, detourLaneWidth, 0.8f);
        CreateLane(lanesRoot, "Lane_DetourRight_C", detourRight, strongholdC, detourLaneWidth, 0.8f);

        Transform arenasRoot = CreateChild(root, "Arenas");
        CreateFloorBox(arenasRoot, "Arena_A", strongholdA, arenaASize);
        CreateFloorBox(arenasRoot, "Arena_B", strongholdB, arenaBSize);
        CreateFloorBox(arenasRoot, "Arena_C", strongholdC, arenaCSize);
        CreateFloorBox(arenasRoot, "Arena_PreBoss", bossGate, preBossSize);
        CreateFloorBox(arenasRoot, "Arena_Boss", bossArena, new Vector2(preBossSize.x + 8f, preBossSize.y + 8f));

        int verticalPoints = Mathf.Max(1, ParseInt(Get(row, "vertical_points_count"), 1));
        int coverA = CoverCountFromDensity(Get(row, "cover_density_a"), verticalPoints + 3);
        int coverB = CoverCountFromDensity(Get(row, "cover_density_b"), verticalPoints + 4);
        int coverC = CoverCountFromDensity(Get(row, "cover_density_c"), verticalPoints + 5);

        Transform coverRoot = CreateChild(root, "Cover");
        CreateCoverCluster(coverRoot, "CoverA", strongholdA, arenaASize, coverA);
        CreateCoverCluster(coverRoot, "CoverB", strongholdB, arenaBSize, coverB);
        CreateCoverCluster(coverRoot, "CoverC", strongholdC, arenaCSize, coverC);

        Transform verticalRoot = CreateChild(root, "VerticalPoints");
        for (int i = 0; i < verticalPoints; i++)
        {
            float t = verticalPoints == 1 ? 0.5f : i / (float)(verticalPoints - 1);
            Vector3 pos = Vector3.Lerp(hub, strongholdC, t) + new Vector3((i % 2 == 0 ? 1 : -1) * 8f, 1.5f, 0f);
            CreateMarkerCube(verticalRoot, $"Vertical_{i + 1}", pos, new Vector3(4f, 3f, 4f));
        }

        Transform metaRoot = CreateChild(root, "Meta");
        CreateTextMeta(metaRoot, "Topology", Get(row, "topology_type"));
        CreateTextMeta(metaRoot, "RiskShortcut", Get(row, "risk_shortcut_path"));
        CreateTextMeta(metaRoot, "ExpectedTime", Get(row, "expected_clear_time_min"));
        CreateTextMeta(metaRoot, "ThirdCombatZone", "StrongholdC");
        CreateTextMeta(metaRoot, "DetourLanes", "A->DetourLeft->B and Transition->DetourRight->C");
    }

    private static void BuildEnemySpawnMarkers(Transform root, Scene scene, List<Dictionary<string, string>> enemyRows)
    {
        foreach (Dictionary<string, string> row in enemyRows)
        {
            string strongholdId = Get(row, "stronghold_id");
            string sector = Get(row, "sector_id");
            string groupName = $"{strongholdId}_{sector}";

            Transform group = CreateChild(root, groupName);
            Vector3 center = ParseVector3(Get(row, "center"));
            float radius = Mathf.Max(1f, ParseFloat(Get(row, "radius_m"), 5f));
            float angle = ParseFloat(Get(row, "entry_angle_deg"), 0f);
            int cap = ParseInt(Get(row, "concurrent_cap"), 8);
            int markerCount = MarkerCountFromCap(cap);

            GameObject prefab = ResolveEnemyPrefab(Get(row, "mix_profile"), Get(row, "base_mix"), sector);

            CreateMarkerCube(group, "SectorCenter", center + new Vector3(0f, 0.25f, 0f), new Vector3(1.2f, 0.5f, 1.2f));

            for (int i = 0; i < markerCount; i++)
            {
                float theta = (angle + (360f / markerCount) * i) * Mathf.Deg2Rad;
                float r = radius * 0.45f;
                Vector3 pos = center + new Vector3(Mathf.Cos(theta) * r, center.y, Mathf.Sin(theta) * r);

                GameObject instance = InstantiateMarker(prefab, scene);
                instance.name = $"{sector}_Enemy_{i + 1}";
                instance.transform.SetParent(group, true);
                instance.transform.position = pos;
                instance.transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }
    }

    private static GameObject InstantiateMarker(GameObject prefab, Scene scene)
    {
        if (prefab != null)
        {
            GameObject obj = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (obj != null)
            {
                return obj;
            }
        }

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fallback.name = "EnemyMarker_Fallback";
        return fallback;
    }

    private static GameObject ResolveEnemyPrefab(string mixProfile, string baseMix, string sector)
    {
        string key = "grunt";

        string source = $"{mixProfile} {baseMix} {sector}".ToLowerInvariant();
        if (source.Contains("elite"))
        {
            key = "elite";
        }
        else if (source.Contains("tank"))
        {
            key = "tank";
        }
        else if (source.Contains("ranged") || source.Contains("controller") || source.Contains("highground"))
        {
            key = "ranged";
        }
        else if (source.Contains("rusher"))
        {
            key = "rusher";
        }
        else if (source.Contains("suicide"))
        {
            key = "suicide";
        }

        if (EnemyPrefabByTag.TryGetValue(key, out string path))
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        return null;
    }

    private static int MarkerCountFromCap(int cap)
    {
        if (cap <= 8) return 2;
        if (cap <= 10) return 3;
        return 4;
    }

    private static int CoverCountFromDensity(string densityText, int fallback)
    {
        if (string.IsNullOrWhiteSpace(densityText))
        {
            return fallback;
        }

        int start = densityText.IndexOf('(');
        int end = densityText.IndexOf('%');
        if (start >= 0 && end > start)
        {
            string number = densityText.Substring(start + 1, end - start - 1);
            if (int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pct))
            {
                return Mathf.Clamp(Mathf.RoundToInt(pct / 5f), 4, 10);
            }
        }

        return fallback;
    }

    private static void CreateCoverCluster(Transform parent, string name, Vector3 center, Vector2 areaSize, int count)
    {
        Transform cluster = CreateChild(parent, name);
        float halfX = areaSize.x * 0.45f;
        float halfZ = areaSize.y * 0.45f;
        int rows = Mathf.Max(2, Mathf.CeilToInt(Mathf.Sqrt(count)));
        int cols = Mathf.Max(2, Mathf.CeilToInt(count / (float)rows));
        int placed = 0;

        for (int r = 0; r < rows && placed < count; r++)
        {
            for (int c = 0; c < cols && placed < count; c++)
            {
                float tx = cols == 1 ? 0f : c / (float)(cols - 1);
                float tz = rows == 1 ? 0f : r / (float)(rows - 1);
                float x = Mathf.Lerp(-halfX, halfX, tx);
                float z = Mathf.Lerp(-halfZ, halfZ, tz);
                Vector3 pos = center + new Vector3(x, 0.8f, z);
                Vector3 size = new Vector3(2.2f, 1.6f, 1.2f);
                CreateMarkerCube(cluster, $"Cover_{placed + 1}", pos, size);
                placed++;
            }
        }
    }

    private static void CreateLane(Transform parent, string name, Vector3 start, Vector3 end, float width, float height)
    {
        Vector3 dir = end - start;
        dir.y = 0f;
        float length = Mathf.Max(1f, dir.magnitude);
        Vector3 center = (start + end) * 0.5f;
        center.y = 0f;

        GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lane.name = name;
        lane.transform.SetParent(parent, true);
        lane.transform.position = center;
        lane.transform.rotation = dir.sqrMagnitude < 0.0001f ? Quaternion.identity : Quaternion.LookRotation(dir.normalized, Vector3.up);
        lane.transform.localScale = new Vector3(width, height, length);
    }

    private static void CreateFloorBox(Transform parent, string name, Vector3 center, Vector2 sizeXZ)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = name;
        floor.transform.SetParent(parent, true);
        floor.transform.position = new Vector3(center.x, -0.3f, center.z);
        floor.transform.localScale = new Vector3(sizeXZ.x, 0.6f, sizeXZ.y);
    }

    private static void CreateAnchor(Transform parent, string name, Vector3 pos)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = name;
        g.transform.SetParent(parent, true);
        g.transform.position = pos + new Vector3(0f, 0.6f, 0f);
        g.transform.localScale = Vector3.one * 1.2f;
    }

    private static void CreateMarkerCube(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, true);
        g.transform.position = pos;
        g.transform.localScale = size;
    }

    private static void CreateTextMeta(Transform parent, string key, string value)
    {
        GameObject go = new GameObject($"Meta_{key}");
        go.transform.SetParent(parent, true);
        WhiteboxMetaTag tag = go.AddComponent<WhiteboxMetaTag>();
        tag.key = key;
        tag.value = value;
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Vector2 ParseSizeXZ(string text, Vector2 fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        string[] parts = text.Split('x');
        if (parts.Length != 2)
        {
            return fallback;
        }

        if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
        {
            return new Vector2(Mathf.Max(1f, x), Mathf.Max(1f, z));
        }

        return fallback;
    }

    private static Vector3 ParseVector3(string text)
    {
        return TryParseVector3(text, out Vector3 parsed) ? parsed : Vector3.zero;
    }

    private static Vector3 ParseVector3OrFallback(string text, Vector3 fallback)
    {
        return TryParseVector3(text, out Vector3 parsed) ? parsed : fallback;
    }

    private static bool TryParseVector3(string text, out Vector3 parsed)
    {
        parsed = Vector3.zero;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string trimmed = text.Trim();
        if (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2);
        }

        string[] parts = trimmed.Split(',');
        if (parts.Length != 3)
        {
            return false;
        }

        parsed = new Vector3(
            ParseFloat(parts[0], 0f),
            ParseFloat(parts[1], 0f),
            ParseFloat(parts[2], 0f));
        return true;
    }

    private static Vector3 LateralOffset(Vector3 from, Vector3 to, float distance, bool leftSide)
    {
        Vector3 dir = to - from;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 right = Vector3.Cross(Vector3.up, dir.normalized);
        float sign = leftSide ? -1f : 1f;
        return right * distance * sign;
    }

    private static float ParseFloat(string text, float fallback)
    {
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
        {
            return v;
        }

        if (float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out v))
        {
            return v;
        }

        return fallback;
    }

    private static int ParseInt(string text, int fallback)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
        {
            return v;
        }

        return fallback;
    }

    private static string Get(Dictionary<string, string> row, string key)
    {
        if (row == null || !row.TryGetValue(key, out string value))
        {
            return string.Empty;
        }

        return value ?? string.Empty;
    }

    private static Dictionary<string, List<Dictionary<string, string>>> GroupBy(List<Dictionary<string, string>> rows, string key)
    {
        Dictionary<string, List<Dictionary<string, string>>> grouped = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (Dictionary<string, string> row in rows)
        {
            string groupKey = Get(row, key);
            if (string.IsNullOrWhiteSpace(groupKey))
            {
                continue;
            }

            if (!grouped.TryGetValue(groupKey, out List<Dictionary<string, string>> list))
            {
                list = new List<Dictionary<string, string>>();
                grouped[groupKey] = list;
            }

            list.Add(row);
        }

        return grouped;
    }

    private static List<Dictionary<string, string>> ReadCsv(string assetRelativePath)
    {
        string absolutePath = ToAbsolutePath(assetRelativePath);
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"CSV not found: {absolutePath}");
        }

        string[] lines = File.ReadAllLines(absolutePath, Encoding.UTF8);
        if (lines.Length == 0)
        {
            return new List<Dictionary<string, string>>();
        }

        string[] headers = ParseCsvLine(lines[0]);
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] fields = ParseCsvLine(lines[i]);
            Dictionary<string, string> row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int h = 0; h < headers.Length; h++)
            {
                string value = h < fields.Length ? fields[h] : string.Empty;
                row[headers[h]] = value;
            }
            rows.Add(row);
        }

        return rows;
    }

    private static string[] ParseCsvLine(string line)
    {
        List<string> fields = new List<string>();
        StringBuilder sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(sb.ToString());
                sb.Length = 0;
            }
            else
            {
                sb.Append(c);
            }
        }

        fields.Add(sb.ToString());
        return fields.ToArray();
    }

    private static string ToAbsolutePath(string assetRelativePath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(projectRoot, assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}

public class WhiteboxMetaTag : MonoBehaviour
{
    public string key = string.Empty;
    [TextArea(2, 4)]
    public string value = string.Empty;
}
