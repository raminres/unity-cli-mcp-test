using System;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Arcade.Editor
{
    public static class RunBlockBreakerTests
    {
        private class TestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"<color=cyan>[TEST RUN STARTED] Running {testsToRun.TestCaseCount} tests...</color>");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                string statusColor = result.FailCount == 0 ? "green" : "red";
                Debug.Log($"<color={statusColor}>====================================================</color>");
                Debug.Log($"<color={statusColor}>[TEST RUN COMPLETED] Passed: {result.PassCount}, Failed: {result.FailCount}, Skipped: {result.SkipCount}</color>");
                Debug.Log($"<color={statusColor}>====================================================</color>");
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren)
                {
                    if (result.TestStatus == TestStatus.Passed)
                    {
                        Debug.Log($"<color=green>[PASS]</color> {result.Name} ({result.Duration:F4}s)");
                    }
                    else if (result.TestStatus == TestStatus.Failed)
                    {
                        Debug.LogError($"<color=red>[FAIL]</color> {result.Name}: {result.Message}\n{result.StackTrace}");
                    }
                }
            }
        }

        [MenuItem("Tools/Arcade/Run Block Breaker Tests")]
        public static void RunTests()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                groupNames = new[] { "Arcade.Tests" }
            };

            api.RegisterCallbacks(new TestCallbacks());
            api.Execute(new ExecutionSettings(filter));
        }
    }
}
