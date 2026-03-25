#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace ThirdPersonController.Editor
{
    public static class InputActionsMirrorAuditTool
    {
        private const string InputActionsAssetPath = "Assets/ThirdPersonController/Inputs/PlayerInputActions.inputactions";
        private const string ReportCsvPath = "Assets/ThirdPersonController/Reports/input_actions_mirror_audit.csv";

        private struct MirrorRow
        {
            public string action;
            public string device;
            public bool runtimeHas;
            public bool assetHas;
            public string status;
            public string note;
        }

        [MenuItem("Tools/ThirdPersonController/Input/Validate Runtime Mirror")]
        public static void ValidateFromMenu()
        {
            ValidateAndWriteReport(logToConsole: true, throwOnMismatch: false);
        }

        public static void ValidateForBatch()
        {
            ValidateAndWriteReport(logToConsole: true, throwOnMismatch: true);
        }

        private static void ValidateAndWriteReport(bool logToConsole, bool throwOnMismatch)
        {
            List<MirrorRow> rows = BuildRows(out int mismatchCount, out string summary);
            WriteCsv(rows, ReportCsvPath);
            AssetDatabase.Refresh();

            if (logToConsole)
            {
                Debug.Log($"[InputMirrorAudit] {summary} | mismatches={mismatchCount} | report={ReportCsvPath}");
            }

            if (throwOnMismatch && mismatchCount > 0)
            {
                throw new Exception($"Input action mirror audit failed with {mismatchCount} mismatch(es). See {ReportCsvPath}");
            }
        }

        private static List<MirrorRow> BuildRows(out int mismatchCount, out string summary)
        {
            mismatchCount = 0;
            var rows = new List<MirrorRow>();
            string[] devices = { "Keyboard", "Mouse", "Gamepad" };

            GameObject probe = new GameObject("InputMirrorAuditProbe");
            try
            {
                PlayerInputHandler handler = probe.AddComponent<PlayerInputHandler>();
                FieldInfo gameplayMapField = typeof(PlayerInputHandler).GetField(
                    "gameplayActionMap",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (gameplayMapField == null)
                {
                    rows.Add(new MirrorRow
                    {
                        action = "GameplayMap",
                        device = "*",
                        runtimeHas = false,
                        assetHas = false,
                        status = "Error",
                        note = "PlayerInputHandler.gameplayActionMap field not found."
                    });
                    mismatchCount++;
                    summary = "runtime map unavailable";
                    return rows;
                }

                InputActionMap runtimeMap = EnsureRuntimeMapReady(handler, gameplayMapField, out string runtimeMapError);
                if (runtimeMap == null)
                {
                    rows.Add(new MirrorRow
                    {
                        action = "GameplayMap",
                        device = "*",
                        runtimeHas = false,
                        assetHas = false,
                        status = "Error",
                        note = runtimeMapError
                    });
                    mismatchCount++;
                    summary = "runtime map unavailable";
                    return rows;
                }

                InputActionAsset asset = LoadInputActionsAsset(out string loadError);
                if (asset == null)
                {
                    rows.Add(new MirrorRow
                    {
                        action = "InputActionsAsset",
                        device = "*",
                        runtimeHas = true,
                        assetHas = false,
                        status = "Error",
                        note = loadError
                    });
                    mismatchCount++;
                    summary = "inputactions asset unavailable";
                    return rows;
                }

                try
                {
                    InputActionMap assetMap = asset.FindActionMap("Gameplay", throwIfNotFound: false);
                    if (assetMap == null && asset.actionMaps.Count > 0)
                    {
                        assetMap = asset.actionMaps[0];
                    }

                    if (assetMap == null)
                    {
                        rows.Add(new MirrorRow
                        {
                            action = "InputActionsAsset",
                            device = "*",
                            runtimeHas = true,
                            assetHas = false,
                            status = "Error",
                            note = "No action map found in .inputactions asset."
                        });
                        mismatchCount++;
                        summary = "inputactions map unavailable";
                        return rows;
                    }

                    for (int i = 0; i < runtimeMap.actions.Count; i++)
                    {
                        InputAction runtimeAction = runtimeMap.actions[i];
                        if (runtimeAction == null || string.IsNullOrEmpty(runtimeAction.name))
                        {
                            continue;
                        }

                        InputAction assetAction = assetMap.FindAction(runtimeAction.name, throwIfNotFound: false);
                        if (assetAction == null)
                        {
                            rows.Add(new MirrorRow
                            {
                                action = runtimeAction.name,
                                device = "*",
                                runtimeHas = true,
                                assetHas = false,
                                status = "Skipped",
                                note = "Action exists at runtime but is outside current .inputactions mirror scope."
                            });
                            continue;
                        }

                        for (int deviceIndex = 0; deviceIndex < devices.Length; deviceIndex++)
                        {
                            string device = devices[deviceIndex];
                            bool runtimeHas = HasBindingForDevice(runtimeAction, device);
                            bool assetHas = HasBindingForDevice(assetAction, device);
                            string status;
                            string note = string.Empty;
                            if (runtimeHas == assetHas)
                            {
                                status = "OK";
                            }
                            else if (string.Equals(device, "Gamepad", StringComparison.Ordinal))
                            {
                                // The mirror asset is currently keyboard/mouse-first; gamepad deltas are advisory.
                                status = "Skipped";
                                note = runtimeHas
                                    ? "Gamepad binding exists at runtime but is not mirrored in .inputactions."
                                    : "Gamepad binding exists in .inputactions but not in runtime map.";
                            }
                            else
                            {
                                status = runtimeHas ? "MissingBindingInAsset" : "ExtraBindingInAsset";
                            }

                            if (!string.Equals(status, "OK", StringComparison.Ordinal) &&
                                !string.Equals(status, "Skipped", StringComparison.Ordinal))
                            {
                                mismatchCount++;
                            }

                            rows.Add(new MirrorRow
                            {
                                action = runtimeAction.name,
                                device = device,
                                runtimeHas = runtimeHas,
                                assetHas = assetHas,
                                status = status,
                                note = note
                            });
                        }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(asset);
                }
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }

            summary = $"rows={rows.Count}";
            return rows;
        }

        private static InputActionMap EnsureRuntimeMapReady(
            PlayerInputHandler handler,
            FieldInfo gameplayMapField,
            out string error)
        {
            error = "Runtime gameplay action map is null.";
            if (handler == null || gameplayMapField == null)
            {
                return null;
            }

            InputActionMap runtimeMap = gameplayMapField.GetValue(handler) as InputActionMap;
            if (runtimeMap != null)
            {
                return runtimeMap;
            }

            MethodInfo ensureMethod = typeof(PlayerInputHandler).GetMethod(
                "EnsureInputActionsReady",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (ensureMethod != null)
            {
                try
                {
                    ensureMethod.Invoke(handler, null);
                }
                catch (Exception exception)
                {
                    error = $"EnsureInputActionsReady invoke failed: {exception.InnerException?.Message ?? exception.Message}";
                    return null;
                }
            }
            else
            {
                MethodInfo buildMethod = typeof(PlayerInputHandler).GetMethod(
                    "BuildInputActions",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (buildMethod == null)
                {
                    error = "PlayerInputHandler input-action bootstrap methods not found.";
                    return null;
                }

                try
                {
                    buildMethod.Invoke(handler, null);
                }
                catch (Exception exception)
                {
                    error = $"BuildInputActions invoke failed: {exception.InnerException?.Message ?? exception.Message}";
                    return null;
                }
            }

            runtimeMap = gameplayMapField.GetValue(handler) as InputActionMap;
            if (runtimeMap == null)
            {
                error = "Runtime gameplay action map remains null after bootstrap.";
            }

            return runtimeMap;
        }

        private static InputActionAsset LoadInputActionsAsset(out string error)
        {
            error = string.Empty;
            string absolutePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                InputActionsAssetPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(absolutePath))
            {
                error = $"Input action asset file not found: {InputActionsAssetPath}";
                return null;
            }

            string json = File.ReadAllText(absolutePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                error = $"Input action asset file is empty: {InputActionsAssetPath}";
                return null;
            }

            return InputActionAsset.FromJson(json);
        }

        private static bool HasBindingForDevice(InputAction action, string deviceName)
        {
            if (action == null || string.IsNullOrEmpty(deviceName))
            {
                return false;
            }

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite)
                {
                    continue;
                }

                string path = !string.IsNullOrEmpty(binding.overridePath) ? binding.overridePath : binding.path;
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (path.IndexOf($"<{deviceName}>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void WriteCsv(List<MirrorRow> rows, string reportPath)
        {
            string absolutePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                reportPath.Replace('/', Path.DirectorySeparatorChar));

            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var builder = new StringBuilder();
            builder.AppendLine("action,device,runtime_has,asset_has,status,note");
            for (int i = 0; i < rows.Count; i++)
            {
                MirrorRow row = rows[i];
                builder.Append(EscapeCsv(row.action)).Append(',')
                    .Append(EscapeCsv(row.device)).Append(',')
                    .Append(row.runtimeHas ? "1" : "0").Append(',')
                    .Append(row.assetHas ? "1" : "0").Append(',')
                    .Append(EscapeCsv(row.status)).Append(',')
                    .Append(EscapeCsv(row.note)).AppendLine();
            }

            File.WriteAllText(absolutePath, builder.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsv(string value)
        {
            string text = value ?? string.Empty;
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }

            return text;
        }
    }
}
#endif
