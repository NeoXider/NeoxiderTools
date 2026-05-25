using Neo.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Network.Tests
{
    public class NetworkDiagnosticsTests
    {
        [SetUp]
        public void SetUp()
        {
            NetworkDiagnostics.RuntimeLogsEnabled = false;
            NetworkDiagnostics.RuntimeWarningsEnabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            NetworkDiagnostics.RuntimeLogsEnabled = false;
            NetworkDiagnostics.RuntimeWarningsEnabled = false;
        }

        [Test]
        public void LogWarning_DoesNotEmit_WhenRuntimeWarningsAreDisabled()
        {
            NetworkDiagnostics.LogWarning("[NetworkDiagnosticsTests] hidden warning");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogWarning_Emits_WhenForced()
        {
            const string message = "[NetworkDiagnosticsTests] forced warning";
            LogAssert.Expect(LogType.Warning, message);

            NetworkDiagnostics.LogWarning(message, force: true);
        }

        [Test]
        public void Log_Emits_WhenRuntimeLogsAreEnabled()
        {
            const string message = "[NetworkDiagnosticsTests] enabled log";
            NetworkDiagnostics.RuntimeLogsEnabled = true;
            LogAssert.Expect(LogType.Log, message);

            NetworkDiagnostics.Log(message);
        }
    }
}
