using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ThirdPersonController;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonController.Editor
{
    public static class SteamRuntimeModeGateValidator
    {
        private const string ValidateMenuPath = "Tools/Productization/P2/Validate Steam Runtime Mode (CSV)";
        private const string ValidateGateMenuPath = "Tools/Productization/P2/Validate Steam Runtime Mode (CI Gate)";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/steam_runtime_mode_report.csv";
        private const string DefaultConfigAssetPath = "Assets/ThirdPersonController/Resources/Steam/DefaultSteamIntegrationConfig.asset";
        private const string LogPrefix = "[SteamRuntimeModeGate]";

        private struct ValidationRow
        {
            public string checkId;
            public string status;
            public string value;
            public string note;
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            Run(failOnBlocking: false, interactive: true);
        }

        [MenuItem(ValidateGateMenuPath)]
        public static void ValidateCiGate()
        {
            Run(failOnBlocking: true, interactive: false);
        }

        public static void ValidateForBatch()
        {
            Run(failOnBlocking: true, interactive: false);
        }

        private static void Run(bool failOnBlocking, bool interactive)
        {
            var rows = new List<ValidationRow>(64);
            int gapTotal = 0;
            int warningTotal = 0;

            bool hasSteamworks;
            bool hasSteamworksNet;
            EvaluateScriptingDefines(rows, ref gapTotal, ref warningTotal, out hasSteamworks, out hasSteamworksNet);
            bool hasSteamworksNetPackage = EvaluateSteamworksNetPackagePresence(rows, hasSteamworks, hasSteamworksNet, ref gapTotal);
            EvaluateCompiledBackendMode(rows, hasSteamworks, hasSteamworksNet, hasSteamworksNetPackage, ref gapTotal);
            EvaluateCloudConflictBranches(rows, ref gapTotal);
            bool hasDefaultConfigAsset;
            uint defaultConfigAppId;
            EvaluateDefaultConfigAsset(rows, ref gapTotal, out hasDefaultConfigAsset, out defaultConfigAppId);
            EvaluateSteamAppIdFile(rows, hasSteamworks, hasSteamworksNet, hasSteamworksNetPackage, hasDefaultConfigAsset, defaultConfigAppId, ref gapTotal);
            EvaluateBootstrapConfigWiring(rows, ref gapTotal);
            EvaluateConfigRegressionTests(rows, ref gapTotal);

            string reportPath = WriteCsv(rows);
            AssetDatabase.Refresh();

            string summary = $"rows={rows.Count} gap={gapTotal} warnings={warningTotal} report={reportPath}";
            Debug.Log($"{LogPrefix} {summary}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Steam Runtime Mode Gate", summary, "OK");
            }

            if (failOnBlocking && gapTotal > 0)
            {
                throw new InvalidOperationException($"{LogPrefix} gate failed. gap={gapTotal} report={reportPath}");
            }
        }

        private static void EvaluateScriptingDefines(
            List<ValidationRow> rows,
            ref int gapTotal,
            ref int warningTotal,
            out bool hasSteamworks,
            out bool hasSteamworksNet)
        {
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (group == BuildTargetGroup.Unknown)
            {
                group = BuildTargetGroup.Standalone;
            }

            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group) ?? string.Empty;
            var defineSet = new HashSet<string>(symbols.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            hasSteamworks = defineSet.Contains("STEAMWORKS");
            hasSteamworksNet = defineSet.Contains("STEAMWORKS_NET");

            string value = $"{group} | STEAMWORKS={hasSteamworks} | STEAMWORKS_NET={hasSteamworksNet}";
            string status = "Ok";
            string note = "runtime_mode_stub_or_real_detectable";

            if (!hasSteamworks && hasSteamworksNet)
            {
                status = "Gap";
                note = "STEAMWORKS_NET is defined but STEAMWORKS is missing.";
                gapTotal++;
            }
            else if (hasSteamworks && !hasSteamworksNet)
            {
                warningTotal++;
                note = "Stub path expected until STEAMWORKS_NET is enabled.";
            }

            rows.Add(new ValidationRow
            {
                checkId = "defines.steam_symbols",
                status = status,
                value = value,
                note = note
            });
        }

        private static void EvaluateCompiledBackendMode(
            List<ValidationRow> rows,
            bool hasSteamworks,
            bool hasSteamworksNet,
            bool hasSteamworksNetPackage,
            ref int gapTotal)
        {
            bool expectRealBackend = hasSteamworks && hasSteamworksNet;
            bool realBackendReady = expectRealBackend && hasSteamworksNetPackage;
            string modeValue = expectRealBackend ? "RealBackendPathCompiled" : "StubPathCompiled";
            string note;
            string status;
            if (expectRealBackend && !hasSteamworksNetPackage)
            {
                status = "Gap";
                gapTotal++;
                note = "STEAMWORKS_NET is enabled, but Steamworks.NET package was not found.";
            }
            else
            {
                status = "Ok";
                note = realBackendReady
                    ? "STEAMWORKS+STEAMWORKS_NET enabled and package present; runtime can use real backend path."
                    : "Stub compile path is active until STEAMWORKS_NET is enabled.";
            }

            rows.Add(new ValidationRow
            {
                checkId = "runtime.mode_split",
                status = status,
                value = modeValue,
                note = note
            });
        }

        private static bool EvaluateSteamworksNetPackagePresence(
            List<ValidationRow> rows,
            bool hasSteamworks,
            bool hasSteamworksNet,
            ref int gapTotal)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");
            bool presentInManifest = false;
            if (File.Exists(manifestPath))
            {
                string manifest = File.ReadAllText(manifestPath);
                presentInManifest = manifest.IndexOf("com.rlabrecque.steamworks.net", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            bool presentInPackagesFolder = Directory.Exists(Path.Combine(projectRoot, "Packages", "com.rlabrecque.steamworks.net"));
            bool presentInPluginsFolder = Directory.Exists(Path.Combine(projectRoot, "Assets", "Plugins", "Steamworks.NET"))
                || Directory.Exists(Path.Combine(projectRoot, "Assets", "Plugins", "Steamworks"));

            bool hasPackage = presentInManifest || presentInPackagesFolder || presentInPluginsFolder;
            bool requirePackage = hasSteamworks && hasSteamworksNet;
            bool missingRequiredPackage = requirePackage && !hasPackage;
            if (missingRequiredPackage)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "package.steamworks_net_presence",
                status = missingRequiredPackage ? "Gap" : "Ok",
                value = $"manifest={presentInManifest}; packages={presentInPackagesFolder}; plugins={presentInPluginsFolder}",
                note = missingRequiredPackage
                    ? "Real backend define is enabled, but Steamworks.NET package was not detected."
                    : "Steamworks.NET package presence is consistent with current define mode."
            });

            return hasPackage;
        }

        private static void EvaluateCloudConflictBranches(List<ValidationRow> rows, ref int gapTotal)
        {
            Type bridgeType = typeof(SteamCloudSaveBridge);
            string[] priorityNames = Enum.GetNames(typeof(SteamCloudSaveBridge.CloudSavePriority));
            bool hasPrioritySet = Array.IndexOf(priorityNames, "Newest") >= 0
                && Array.IndexOf(priorityNames, "CloudPreferred") >= 0
                && Array.IndexOf(priorityNames, "LocalPreferred") >= 0;
            if (!hasPrioritySet)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "cloud.priority_enum",
                status = hasPrioritySet ? "Ok" : "Gap",
                value = string.Join("|", priorityNames),
                note = hasPrioritySet
                    ? "All conflict priorities are present."
                    : "Missing expected CloudSavePriority enum values."
            });

            MethodInfo resolveSync = bridgeType.GetMethod("ResolveSync", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo resolveNewest = bridgeType.GetMethod("ResolveNewest", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo upload = bridgeType.GetMethod("UploadFileIfExists", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo download = bridgeType.GetMethod("DownloadFileIfExists", BindingFlags.Instance | BindingFlags.NonPublic);

            bool hasBranchMethods = resolveSync != null && resolveNewest != null && upload != null && download != null;
            if (!hasBranchMethods)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "cloud.branch_methods",
                status = hasBranchMethods ? "Ok" : "Gap",
                value = $"ResolveSync={(resolveSync != null)}; ResolveNewest={(resolveNewest != null)}; Upload={(upload != null)}; Download={(download != null)}",
                note = hasBranchMethods
                    ? "SteamCloudSaveBridge conflict branch methods are available."
                    : "Missing one or more conflict branch methods."
            });

            const string regressionTestPath = "Assets/ThirdPersonController/Tests/PlayMode/SteamCloudSaveBridgeRegressionTests.cs";
            string[] requiredTests =
            {
                "SteamCloudSaveBridge_TryPullFromCloud_DownloadsMissingLocalSave",
                "SteamCloudSaveBridge_PushToCloud_UploadsLocalSaveFile",
                "SteamCloudSaveBridge_PullSettings_AppliesLocalizationLanguage",
                "SteamCloudSaveBridge_NewestPriority_DownloadsCloudWhenCloudTimestampNewer",
                "SteamCloudSaveBridge_NewestPriority_UploadsLocalWhenLocalTimestampNewer"
            };

            bool hasAllRequiredTests = false;
            if (File.Exists(regressionTestPath))
            {
                hasAllRequiredTests = true;
                string source = File.ReadAllText(regressionTestPath);
                for (int i = 0; i < requiredTests.Length; i++)
                {
                    if (source.IndexOf(requiredTests[i], StringComparison.Ordinal) < 0)
                    {
                        hasAllRequiredTests = false;
                        break;
                    }
                }
            }

            if (!hasAllRequiredTests)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "cloud.branch_regression_tests",
                status = hasAllRequiredTests ? "Ok" : "Gap",
                value = File.Exists(regressionTestPath) ? regressionTestPath : "file_missing",
                note = hasAllRequiredTests
                    ? "Conflict branch regression tests are present for pull/push/newest paths."
                    : "Missing required Steam cloud conflict regression tests."
            });
        }

        private static void EvaluateDefaultConfigAsset(
            List<ValidationRow> rows,
            ref int gapTotal,
            out bool hasDefaultConfig,
            out uint appId)
        {
            SteamIntegrationConfig config = AssetDatabase.LoadAssetAtPath<SteamIntegrationConfig>(DefaultConfigAssetPath);
            bool exists = config != null;
            hasDefaultConfig = exists;
            appId = exists ? config.appId : 0u;
            if (!exists)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "config.default_asset_exists",
                status = exists ? "Ok" : "Gap",
                value = DefaultConfigAssetPath,
                note = exists
                    ? "Default steam integration config asset is present."
                    : "Missing default steam integration config asset."
            });

            if (!exists)
            {
                return;
            }

            bool validAppId = config.appId > 0;
            if (!validAppId)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "config.app_id",
                status = validAppId ? "Ok" : "Gap",
                value = config.appId.ToString(),
                note = validAppId ? "AppId is configured." : "AppId must be greater than zero."
            });

            bool reflectionPreferred = config.preferReflectionBackend;
            rows.Add(new ValidationRow
            {
                checkId = "config.prefer_reflection_backend",
                status = reflectionPreferred ? "Ok" : "Warning",
                value = reflectionPreferred.ToString(),
                note = reflectionPreferred
                    ? "Reflection fallback backend is enabled for package-late runtime hookup."
                    : "Reflection fallback backend is disabled; runtime depends on compile-time Steamworks path."
            });
        }

        private static void EvaluateSteamAppIdFile(
            List<ValidationRow> rows,
            bool hasSteamworks,
            bool hasSteamworksNet,
            bool hasSteamworksNetPackage,
            bool hasDefaultConfigAsset,
            uint defaultConfigAppId,
            ref int gapTotal)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string appIdPath = Path.Combine(projectRoot, "steam_appid.txt");
            bool exists = File.Exists(appIdPath);

            bool requireForRealBackend = hasSteamworks && hasSteamworksNet && hasSteamworksNetPackage;
            if (!exists)
            {
                bool gap = requireForRealBackend;
                if (gap)
                {
                    gapTotal++;
                }

                rows.Add(new ValidationRow
                {
                    checkId = "appid.file_sync",
                    status = gap ? "Gap" : "Ok",
                    value = appIdPath,
                    note = gap
                        ? "Real backend mode is enabled, but steam_appid.txt is missing."
                        : "steam_appid.txt is optional in stub mode."
                });
                return;
            }

            string text = File.ReadAllText(appIdPath).Trim();
            uint fileAppId;
            bool parsed = uint.TryParse(text, out fileAppId) && fileAppId > 0u;
            bool mismatchWithConfig = hasDefaultConfigAsset && parsed && defaultConfigAppId > 0u && fileAppId != defaultConfigAppId;
            bool gapState = !parsed || mismatchWithConfig;
            if (gapState)
            {
                gapTotal++;
            }

            string note;
            if (!parsed)
            {
                note = "steam_appid.txt must contain a positive integer AppId.";
            }
            else if (mismatchWithConfig)
            {
                note = $"steam_appid.txt ({fileAppId}) does not match config AppId ({defaultConfigAppId}).";
            }
            else
            {
                note = "steam_appid.txt is present and consistent with steam config.";
            }

            rows.Add(new ValidationRow
            {
                checkId = "appid.file_sync",
                status = gapState ? "Gap" : "Ok",
                value = parsed ? fileAppId.ToString() : text,
                note = note
            });
        }

        private static void EvaluateBootstrapConfigWiring(List<ValidationRow> rows, ref int gapTotal)
        {
            Type bootstrapType = typeof(SteamIntegrationBootstrap);
            FieldInfo resolverField = bootstrapType.GetField("ConfigResolverOverride", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            MethodInfo resolveConfigMethod = bootstrapType.GetMethod("ResolveConfig", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo bootstrapMethod = bootstrapType.GetMethod("Bootstrap", BindingFlags.Static | BindingFlags.NonPublic);

            bool hasWiring = resolverField != null && resolveConfigMethod != null && bootstrapMethod != null;
            if (!hasWiring)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "bootstrap.config_wiring",
                status = hasWiring ? "Ok" : "Gap",
                value = $"ResolverField={(resolverField != null)}; ResolveConfig={(resolveConfigMethod != null)}; Bootstrap={(bootstrapMethod != null)}",
                note = hasWiring
                    ? "Bootstrap exposes config resolver override and resolve pipeline."
                    : "Bootstrap config resolver wiring is incomplete."
            });
        }

        private static void EvaluateConfigRegressionTests(List<ValidationRow> rows, ref int gapTotal)
        {
            const string regressionTestPath = "Assets/ThirdPersonController/Tests/PlayMode/SteamIntegrationConfigRegressionTests.cs";
            const string bootstrapRegressionPath = "Assets/ThirdPersonController/Tests/PlayMode/SteamIntegrationBootstrapRegressionTests.cs";
            const string statusRegressionPath = "Assets/ThirdPersonController/Tests/PlayMode/SteamIntegrationStatusRegressionTests.cs";
            string[] requiredConfigTests =
            {
                "SteamAchievementTracker_ApplyConfig_MapsEnableFlag",
                "SteamStatsTracker_ApplyConfig_MapsEnableAndFlushInterval",
                "SteamCloudSaveBridge_ApplyConfig_MapsCloudSettings"
            };

            bool hasConfigTests = HasRequiredTestMethods(regressionTestPath, requiredConfigTests);
            if (!hasConfigTests)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "config.regression_tests",
                status = hasConfigTests ? "Ok" : "Gap",
                value = File.Exists(regressionTestPath) ? regressionTestPath : "file_missing",
                note = hasConfigTests
                    ? "Config mapping regression tests are present."
                    : "Missing required config mapping regression tests."
            });

            string[] requiredBootstrapTests =
            {
                "SteamIntegrationBootstrap_Bootstrap_AppliesResolvedConfigToServiceAndTrackers"
            };
            bool hasBootstrapTests = HasRequiredTestMethods(bootstrapRegressionPath, requiredBootstrapTests);
            if (!hasBootstrapTests)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "bootstrap.regression_tests",
                status = hasBootstrapTests ? "Ok" : "Gap",
                value = File.Exists(bootstrapRegressionPath) ? bootstrapRegressionPath : "file_missing",
                note = hasBootstrapTests
                    ? "Bootstrap config application regression tests are present."
                    : "Missing bootstrap config regression tests."
            });

            string[] requiredStatusTests =
            {
                "SteamIntegrationService_RuntimeStatus_MatchesCurrentServiceFlags",
                "SteamIntegrationService_RuntimeStatus_RequireRealBackend_WithStub_IsInvalid",
                "SteamIntegrationService_RuntimeStatus_RequireRealBackend_WithRealClient_IsValid",
                "SteamIntegrationService_RuntimeStatus_RequireCloud_WithUnavailableCloud_IsInvalid"
            };
            bool hasStatusTests = HasRequiredTestMethods(statusRegressionPath, requiredStatusTests);
            if (!hasStatusTests)
            {
                gapTotal++;
            }

            rows.Add(new ValidationRow
            {
                checkId = "status.regression_tests",
                status = hasStatusTests ? "Ok" : "Gap",
                value = File.Exists(statusRegressionPath) ? statusRegressionPath : "file_missing",
                note = hasStatusTests
                    ? "Runtime status regression tests cover stub and real backend branches."
                    : "Missing required runtime status regression tests for backend mode branches."
            });
        }

        private static bool HasRequiredTestMethods(string sourcePath, string[] requiredNames)
        {
            if (!File.Exists(sourcePath))
            {
                return false;
            }

            string source = File.ReadAllText(sourcePath);
            for (int i = 0; i < requiredNames.Length; i++)
            {
                if (source.IndexOf(requiredNames[i], StringComparison.Ordinal) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static string WriteCsv(List<ValidationRow> rows)
        {
            string fullPath = Path.GetFullPath(ReportCsvPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var builder = new StringBuilder(2048);
            builder.AppendLine("check_id,status,value,note");
            for (int i = 0; i < rows.Count; i++)
            {
                ValidationRow row = rows[i];
                builder
                    .Append(Escape(row.checkId)).Append(',')
                    .Append(Escape(row.status)).Append(',')
                    .Append(Escape(row.value)).Append(',')
                    .Append(Escape(row.note))
                    .AppendLine();
            }

            File.WriteAllText(fullPath, builder.ToString(), new UTF8Encoding(false));
            return ReportCsvPath;
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            bool needsQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (value.IndexOf('"') >= 0)
            {
                value = value.Replace("\"", "\"\"");
            }

            return needsQuote ? $"\"{value}\"" : value;
        }

    }
}
