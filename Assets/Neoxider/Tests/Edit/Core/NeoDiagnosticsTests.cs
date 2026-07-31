using System;
using System.Collections.Generic;
using System.Linq;
using Neo;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public class NeoDiagnosticsTests
    {
        // WHY: a swapped-in log handler keeps the deliberate test messages out of the editor
        // console - real error entries there read as failures to a human, even when expected.
        private sealed class CapturingLogHandler : ILogHandler
        {
            public readonly List<(LogType Type, string Message)> Entries = new();

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                Entries.Add((logType, string.Format(format, args)));
            }

            public void LogException(Exception exception, Object context)
            {
                Entries.Add((LogType.Exception, exception.Message));
            }
        }

        private CapturingLogHandler _capture;
        private ILogHandler _previousHandler;

        [SetUp]
        public void SetUp()
        {
            NeoDiagnostics.ResetStaticState();
            _capture = new CapturingLogHandler();
            _previousHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = _capture;
        }

        [TearDown]
        public void TearDown()
        {
            Debug.unityLogger.logHandler = _previousHandler;
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
            NeoDiagnostics.LogError("visible error");

            Assert.That(_capture.Entries, Has.Count.EqualTo(1));
            Assert.That(_capture.Entries[0].Type, Is.EqualTo(LogType.Error));
            Assert.That(_capture.Entries[0].Message, Is.EqualTo("visible error"));
        }

        [Test]
        public void Configure_EnablesRequestedChannels()
        {
            NeoDiagnostics.Configure(true, true, true);

            NeoDiagnostics.Log("visible info");
            NeoDiagnostics.LogWarning("visible warning");
            NeoDiagnostics.LogError("visible error");

            Assert.That(_capture.Entries.Select(e => e.Type),
                Is.EqualTo(new[] { LogType.Log, LogType.Warning, LogType.Error }));
            Assert.That(_capture.Entries.Select(e => e.Message),
                Is.EqualTo(new[] { "visible info", "visible warning", "visible error" }));
        }

        [Test]
        public void ThrottledWarning_EmitsOnceForSameKey()
        {
            NeoDiagnostics.Configure(warnings: true);

            NeoDiagnostics.LogWarningThrottled("same-key", "first warning", seconds: 60f);
            NeoDiagnostics.LogWarningThrottled("same-key", "second warning", seconds: 60f);

            Assert.That(_capture.Entries, Has.Count.EqualTo(1));
            Assert.That(_capture.Entries[0].Message, Is.EqualTo("first warning"));
        }

        [Test]
        public void Force_BypassesDisabledChannels()
        {
            NeoDiagnostics.Log("forced info", force: true);
            NeoDiagnostics.LogWarning("forced warning", force: true);
            NeoDiagnostics.LogError("forced error", force: true);

            Assert.That(_capture.Entries.Select(e => e.Message),
                Is.EqualTo(new[] { "forced info", "forced warning", "forced error" }));
        }
    }
}
