using Neo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    public class NeoDiagnosticsTests
    {
        [SetUp]
        public void SetUp()
        {
            NeoDiagnostics.ResetStaticState();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.NoUnexpectedReceived();
            NeoDiagnostics.ResetStaticState();
        }

        [Test]
        public void ResetStaticState_DisablesInfoAndWarnings_ButKeepsErrors()
        {
            Assert.False(NeoDiagnostics.RuntimeLogsEnabled);
            Assert.False(NeoDiagnostics.RuntimeWarningsEnabled);
            Assert.True(NeoDiagnostics.RuntimeErrorsEnabled);

            NeoDiagnostics.Log("hidden info");
            NeoDiagnostics.LogWarning("hidden warning");

            LogAssert.Expect(LogType.Error, "visible error");
            NeoDiagnostics.LogError("visible error");
        }

        [Test]
        public void Configure_EnablesRequestedChannels()
        {
            NeoDiagnostics.Configure(true, true, true);

            LogAssert.Expect(LogType.Log, "visible info");
            LogAssert.Expect(LogType.Warning, "visible warning");
            LogAssert.Expect(LogType.Error, "visible error");

            NeoDiagnostics.Log("visible info");
            NeoDiagnostics.LogWarning("visible warning");
            NeoDiagnostics.LogError("visible error");
        }

        [Test]
        public void ThrottledWarning_EmitsOnceForSameKey()
        {
            NeoDiagnostics.Configure(warnings: true);

            LogAssert.Expect(LogType.Warning, "first warning");
            NeoDiagnostics.LogWarningThrottled("same-key", "first warning", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("same-key", "second warning", seconds: 60f);
        }

        [Test]
        public void Force_BypassesDisabledChannels()
        {
            LogAssert.Expect(LogType.Log, "forced info");
            LogAssert.Expect(LogType.Warning, "forced warning");
            LogAssert.Expect(LogType.Error, "forced error");

            NeoDiagnostics.Log("forced info", force: true);
            NeoDiagnostics.LogWarning("forced warning", force: true);
            NeoDiagnostics.LogError("forced error", force: true);
        }
    }
}
