using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    // WHY: Top-level types: nested type FullNames contain '+', which stack traces never show,
    // so the attribution regex would (correctly) skip them.
    internal sealed class NeoComponentHealthProbe : ScriptableObject
    {
    }

    internal sealed class NeoComponentHealthOtherProbe : ScriptableObject
    {
    }

    [TestFixture]
    public class NeoComponentHealthTests
    {
        private static int _messageCounter;

        private NeoComponentHealthProbe _probe;
        private NeoComponentHealthOtherProbe _otherProbe;

        [SetUp]
        public void SetUp()
        {
            NeoComponentHealth.ResetForTests();
            _probe = ScriptableObject.CreateInstance<NeoComponentHealthProbe>();
            _otherProbe = ScriptableObject.CreateInstance<NeoComponentHealthOtherProbe>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_probe);
            Object.DestroyImmediate(_otherProbe);
            NeoComponentHealth.ResetForTests();
        }

        // WHY: OnLogMessage throttles repeated identical conditions; unique text keeps tests independent.
        private static string UniqueCondition(string prefix)
        {
            return prefix + " #" + ++_messageCounter;
        }

        private static string StackFor<T>()
        {
            return typeof(T).FullName + ":Update ()\nUnityEngine.Debug:LogError (object)\n";
        }

        [Test]
        public void OnLogMessage_AttributesErrorToTypeFromStackTrace()
        {
            NeoComponentHealth.OnLogMessage(UniqueCondition("NullReferenceException"),
                StackFor<NeoComponentHealthProbe>(), LogType.Error);

            NeoComponentHealth.Report report = NeoComponentHealth.GetReport(_probe);
            Assert.That(report.ConsoleErrors, Is.EqualTo(1));
            Assert.That(report.Mood, Is.EqualTo(NeoComponentHealth.Mood.Alarmed));
        }

        [Test]
        public void OnLogMessage_SkipsEngineEditorAndSystemFrames()
        {
            const string engineOnlyStack =
                "UnityEngine.Debug:LogError (object)\n" +
                "UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()\n" +
                "System.Threading.Tasks.Task:Execute ()\n";
            NeoComponentHealth.OnLogMessage(UniqueCondition("EngineOnly"), engineOnlyStack, LogType.Error);

            Assert.That(NeoComponentHealth.GetReport(_probe).ConsoleErrors, Is.EqualTo(0));
        }

        [Test]
        public void OnLogMessage_CountsOneTypeOncePerError()
        {
            string typeName = typeof(NeoComponentHealthProbe).FullName;
            string stack = typeName + ":Update ()\n" + typeName + ":Helper ()\n";
            NeoComponentHealth.OnLogMessage(UniqueCondition("Repeated frames"), stack, LogType.Error);

            Assert.That(NeoComponentHealth.GetReport(_probe).ConsoleErrors, Is.EqualTo(1));
        }

        [Test]
        public void OnLogMessage_IgnoresWarningsAndLogs()
        {
            NeoComponentHealth.OnLogMessage(UniqueCondition("Warning"),
                StackFor<NeoComponentHealthProbe>(), LogType.Warning);
            NeoComponentHealth.OnLogMessage(UniqueCondition("Log"),
                StackFor<NeoComponentHealthProbe>(), LogType.Log);

            Assert.That(NeoComponentHealth.GetReport(_probe).ConsoleErrors, Is.EqualTo(0));
        }

        [Test]
        public void OnLogMessage_ThrottlesImmediateDuplicateCondition()
        {
            string condition = UniqueCondition("Update spam");
            NeoComponentHealth.OnLogMessage(condition, StackFor<NeoComponentHealthProbe>(), LogType.Error);
            NeoComponentHealth.OnLogMessage(condition, StackFor<NeoComponentHealthProbe>(), LogType.Error);

            Assert.That(NeoComponentHealth.GetReport(_probe).ConsoleErrors, Is.EqualTo(1));
        }

        [Test]
        public void SyncConsoleErrorCount_ZeroWipesMemoryAndSessionState()
        {
            NeoComponentHealth.OnLogMessage(UniqueCondition("Cleared later"),
                StackFor<NeoComponentHealthProbe>(), LogType.Error);
            SessionState.SetString(NeoComponentHealth.SessionKey, "stale");

            Assert.That(NeoComponentHealth.SyncConsoleErrorCount(0), Is.True);
            Assert.That(NeoComponentHealth.GetReport(_probe).ConsoleErrors, Is.EqualTo(0));
            Assert.That(SessionState.GetString(NeoComponentHealth.SessionKey, string.Empty), Is.Empty);
        }

        [Test]
        public void SyncConsoleErrorCount_NonZeroKeepsMemory()
        {
            NeoComponentHealth.OnLogMessage(UniqueCondition("Still visible"),
                StackFor<NeoComponentHealthProbe>(), LogType.Error);

            Assert.That(NeoComponentHealth.SyncConsoleErrorCount(2), Is.False);
            Assert.That(NeoComponentHealth.GetReport(_probe).ConsoleErrors, Is.EqualTo(1));
        }

        [Test]
        public void SyncConsoleErrorCount_ZeroWithoutMemoryIsNoOp()
        {
            Assert.That(NeoComponentHealth.SyncConsoleErrorCount(0), Is.False);
        }

        [Test]
        public void ClearConsoleErrors_ForgetsOnlyTheGivenType()
        {
            NeoComponentHealth.OnLogMessage(UniqueCondition("Probe error"),
                StackFor<NeoComponentHealthProbe>(), LogType.Error);
            NeoComponentHealth.OnLogMessage(UniqueCondition("Other error"),
                StackFor<NeoComponentHealthOtherProbe>(), LogType.Error);

            NeoComponentHealth.ClearConsoleErrors(typeof(NeoComponentHealthProbe));

            Assert.That(NeoComponentHealth.GetReport(_probe).ConsoleErrors, Is.EqualTo(0));
            Assert.That(NeoComponentHealth.GetReport(_otherProbe).ConsoleErrors, Is.EqualTo(1));
        }
    }
}
