using System.IO;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class SteamIntegrationConfigProvisionTool
    {
        private const string MenuPath = "Tools/Productization/P4/Ensure Default Steam Integration Config";
        private const string AssetPath = "Assets/ThirdPersonController/Resources/Steam/DefaultSteamIntegrationConfig.asset";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/steam_config_provision_report.csv";

        [MenuItem(MenuPath)]
        public static void EnsureFromMenu()
        {
            Ensure(interactive: true);
        }

        public static void EnsureForBatch()
        {
            Ensure(interactive: false);
        }

        private static void Ensure(bool interactive)
        {
            bool created = false;
            int fixedCount = 0;
            SteamIntegrationConfig config = AssetDatabase.LoadAssetAtPath<SteamIntegrationConfig>(AssetPath);
            if (config == null)
            {
                EnsureParentFolder();
                config = ScriptableObject.CreateInstance<SteamIntegrationConfig>();
                AssetDatabase.CreateAsset(config, AssetPath);
                created = true;
            }

            if (config.appId == 0u)
            {
                config.appId = 480u;
                fixedCount++;
            }

            if (config.statsFlushInterval < 1f)
            {
                config.statsFlushInterval = 20f;
                fixedCount++;
            }

            if (config.cloudUploadCooldown < 0.1f)
            {
                config.cloudUploadCooldown = 1.5f;
                fixedCount++;
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            WriteCsv(created, fixedCount, config);
            AssetDatabase.Refresh();

            string summary = created
                ? $"created default config at {AssetPath}"
                : $"default config already exists at {AssetPath}";
            if (fixedCount > 0)
            {
                summary += $"; normalized={fixedCount}";
            }
            Debug.Log($"[SteamConfigProvision] {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Steam Integration Config", summary, "OK");
            }
        }

        private static void WriteCsv(bool created, int fixedCount, SteamIntegrationConfig config)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string status = config != null && config.appId > 0 ? "Ok" : "Gap";
            string note = created ? "created" : "existing";
            if (fixedCount > 0)
            {
                note += $";normalized={fixedCount}";
            }

            var sb = new StringBuilder(256);
            sb.AppendLine("check_id,status,value,note");
            sb.Append("config.provision").Append(',')
                .Append(status).Append(',')
                .Append(Escape(AssetPath)).Append(',')
                .Append(Escape(note))
                .AppendLine();

            File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(false));
        }

        private static string Escape(string value)
        {
            string text = value ?? string.Empty;
            bool needsQuote = text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (text.IndexOf('"') >= 0)
            {
                text = text.Replace("\"", "\"\"");
            }

            return needsQuote ? $"\"{text}\"" : text;
        }

        private static void EnsureParentFolder()
        {
            string folder = Path.GetDirectoryName(AssetPath);
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            folder = folder.Replace('\\', '/');
            string[] segments = folder.Split('/');
            if (segments.Length == 0)
            {
                return;
            }

            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
