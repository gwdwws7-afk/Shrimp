using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class BatchPlayModeTestRunner
{
    private const string ResultsArg = "-batchTestResults";
    private const string TestFilterArg = "-batchTestFilter";
    private const string AssemblyFilterArg = "-batchAssemblyFilter";
    private const string LogPrefix = "[BatchPlayModeTestRunner]";
    private static TestRunnerApi activeApi;
    private static BatchRunCallbacks activeCallbacks;
    private static bool runInProgress;

    public static void RunPlayModeTestsFromCommandLine()
    {
        try
        {
            if (runInProgress)
            {
                Debug.LogWarning($"{LogPrefix} run already in progress, ignoring duplicate executeMethod call.");
                return;
            }

            string[] args = Environment.GetCommandLineArgs();
            string projectPath = Directory.GetCurrentDirectory();
            string defaultResults = Path.Combine(projectPath, "Logs", "PlayModeBatchResults.xml");
            string resultsPath = ReadArgValue(args, ResultsArg, defaultResults);
            string testFilterRaw = ReadArgValue(args, TestFilterArg, string.Empty);
            string assemblyFilterRaw = ReadArgValue(args, AssemblyFilterArg, string.Empty);

            EnsureParentDirectory(resultsPath);
            Debug.Log($"{LogPrefix} start results={resultsPath}");

            Filter filter = new Filter
            {
                testMode = TestMode.PlayMode
            };

            string[] testNames = SplitCsvArg(testFilterRaw);
            if (testNames.Length > 0)
            {
                filter.testNames = testNames;
                Debug.Log($"{LogPrefix} test filter count={testNames.Length}");
            }

            string[] assemblyNames = SplitCsvArg(assemblyFilterRaw);
            if (assemblyNames.Length > 0)
            {
                filter.assemblyNames = assemblyNames;
                Debug.Log($"{LogPrefix} assembly filter count={assemblyNames.Length}");
            }

            activeCallbacks = new BatchRunCallbacks(resultsPath);
            activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            activeApi.RegisterCallbacks(activeCallbacks);
            runInProgress = true;

            ExecutionSettings execution = new ExecutionSettings(filter)
            {
                runSynchronously = false
            };

            activeApi.Execute(execution);
        }
        catch (Exception ex)
        {
            Debug.LogError($"{LogPrefix} fatal error before test execution: {ex}");
            CleanupRunState();
            EditorApplication.Exit(3);
        }
    }

    private static string ReadArgValue(string[] args, string key, string fallback)
    {
        if (args == null || args.Length == 0 || string.IsNullOrEmpty(key))
        {
            return fallback;
        }

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = args[i + 1];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        return fallback;
    }

    private static string[] SplitCsvArg(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .ToArray();
    }

    private static void EnsureParentDirectory(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(parent))
        {
            return;
        }

        Directory.CreateDirectory(parent);
    }

    private static void CleanupRunState()
    {
        runInProgress = false;
        if (activeApi != null && activeCallbacks != null)
        {
            try
            {
                activeApi.UnregisterCallbacks(activeCallbacks);
            }
            catch
            {
            }
        }

        if (activeApi != null)
        {
            ScriptableObject.DestroyImmediate(activeApi);
            activeApi = null;
        }

        activeCallbacks = null;
    }

    private sealed class BatchRunCallbacks : ICallbacks
    {
        private readonly string resultsPath;
        private bool finished;

        public BatchRunCallbacks(string resultsPath)
        {
            this.resultsPath = resultsPath;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            int count = testsToRun != null ? testsToRun.TestCaseCount : 0;
            Debug.Log($"{LogPrefix} run started testcasecount={count}");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (finished)
            {
                return;
            }

            finished = true;
            int exitCode = 0;

            try
            {
                if (result != null)
                {
                    var xml = result.ToXml();
                    File.WriteAllText(resultsPath, xml.ToString());
                    Debug.Log(
                        $"{LogPrefix} run finished pass={result.PassCount} fail={result.FailCount} skip={result.SkipCount} inconclusive={result.InconclusiveCount} results={resultsPath}");

                    if (result.FailCount > 0)
                    {
                        exitCode = 2;
                    }
                }
                else
                {
                    Debug.LogError($"{LogPrefix} run finished with null result.");
                    exitCode = 3;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} failed to persist results: {ex}");
                exitCode = 3;
            }
            finally
            {
                CleanupRunState();
                EditorApplication.Exit(exitCode);
            }
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }
    }
}
