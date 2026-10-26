using System.Reflection;
using Neo.Audio;
using Neo.Extensions;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Tests.Tools
{
    public sealed class Audit20261023NeoToolsDebugRegressionTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _owner;
        private GameObject _output;
        private NeoDebugOverlay _overlay;
        private TextMeshProUGUI _text;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("AuditDebugOwner");
            _owner.SetActive(false);
            _overlay = _owner.AddComponent<NeoDebugOverlay>();

            _output = new GameObject("AuditDebugOutput");
            _output.SetActive(false);
            _text = _output.AddComponent<TextMeshProUGUI>();
            SetOverlayField("_text", _text);
        }

        [TearDown]
        public void TearDown()
        {
            // The injected output is owned by the fixture, rather than the runtime-built canvas.
            SetOverlayField("_root", null);
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_output);
        }

        [Test]
        public void Overlay_Update_RendersWithTheConfiguredInputBackend()
        {
            SetOverlayField("_root", _output);
            SetOverlayField("_visible", true);
            SetOverlayField("_toggleKey", KeyCode.None);
            SetOverlayField("_showManagers", false);

            InvokeOverlay("Update");

            // In Input System-only projects, the former legacy input call threw before any text was built.
            Assert.That(_text.text, Does.Contain("FPS  "));
            Assert.That(_text.text, Does.Contain("Scene  " + SceneManager.GetActiveScene().name));
            Assert.That(_text.text, Does.Contain("TimeScale  "));
        }

        [Test]
        public void Overlay_Readout_DoesNotCreateLazyAudioManager()
        {
            Assume.That(AM.HasInstance, Is.False, "This regression requires an unresolved audio singleton.");
            Assume.That(Object.FindObjectsByType<AM>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty,
                "This regression requires an isolated scene without an audio manager.");
            bool originalCreateInstance = AM.CreateInstance;

            try
            {
                AM.CreateInstance = true;
                InvokeOverlay("RebuildText");

                Assert.That(AM.HasInstance, Is.False, "Reading diagnostics must not create a singleton.");
                Assert.That(Object.FindObjectsByType<AM>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
                Assert.That(_text.text, Does.Contain("AM  —"));
            }
            finally
            {
                AM.CreateInstance = originalCreateInstance;
                // Also clean up an instance created by the faulty implementation when this regression fails.
                foreach (AM manager in Object.FindObjectsByType<AM>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    Object.DestroyImmediate(manager.gameObject);
                }
            }
        }

        [Test]
        public void Overlay_Readout_DoesNotResolveInactiveAudioManager()
        {
            Assume.That(AM.HasInstance, Is.False, "This regression requires an unresolved audio singleton.");
            Assume.That(Object.FindObjectsByType<AM>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty,
                "This regression requires an isolated scene without an audio manager.");
            bool originalCreateInstance = AM.CreateInstance;
            GameObject managerObject = new GameObject("AuditInactiveAudioManager");
            managerObject.SetActive(false);

            try
            {
                AM.CreateInstance = true;
                managerObject.AddComponent<AM>();
                Assert.That(AM.HasInstance, Is.False);

                InvokeOverlay("RebuildText");

                Assert.That(AM.HasInstance, Is.False, "Reading diagnostics must not resolve an inactive singleton.");
                Assert.That(managerObject.activeSelf, Is.False);
                Assert.That(_text.text, Does.Contain("AM  —"));
            }
            finally
            {
                AM.CreateInstance = originalCreateInstance;
                Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void Overlay_Readout_DisplaysAnAlreadyResolvedAudioManager()
        {
            Assume.That(AM.HasInstance, Is.False, "This regression requires an isolated audio singleton.");
            Assume.That(Object.FindObjectsByType<AM>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty,
                "This regression requires an isolated scene without an audio manager.");
            bool originalCreateInstance = AM.CreateInstance;
            GameObject managerObject = new GameObject("AuditResolvedAudioManager");
            managerObject.SetActive(false);

            try
            {
                AM manager = managerObject.AddComponent<AM>();
                AM.CreateInstance = true;
                Assert.That(AM.I, Is.SameAs(manager));

                InvokeOverlay("RebuildText");

                Assert.That(_text.text, Does.Contain("AM  music=stopped  random=off  clip=—"));
                Assert.That(AM.I, Is.SameAs(manager));
            }
            finally
            {
                AM.CreateInstance = originalCreateInstance;
                Object.DestroyImmediate(managerObject);
            }
        }

        [TestCase("(at Assets/Gameplay/Logger.cs:27)", "Assets/Gameplay/Logger.cs:27")]
        [TestCase("(at Assets/Feature (Demo)/Logger.cs:27)", "Assets/Feature (Demo)/Logger.cs:27")]
        [TestCase("(at D:/Work (Demo)/Assets/Logger.cs:8)", "D:/Work (Demo)/Assets/Logger.cs:8")]
        [TestCase("at Game.Log (Packages/com.game/Feature (Demo)/Logger.cs:9)",
            "Packages/com.game/Feature (Demo)/Logger.cs:9")]
        public void ErrorLogger_HighlightsCompleteScriptPath(string stackTrace, string expectedPath)
        {
            ErrorLogger logger = _owner.AddComponent<ErrorLogger>();
            logger.textMesh = _text;
            logger.logTypeFilter = LogTypeFilter.Warning;
            logger.showScriptPath = true;
            logger.addText = false;
            MethodInfo callback = typeof(ErrorLogger).GetMethod("HandleLog", PrivateInstance);

            // Exercise the Application callback with a deterministic Unity-formatted stack trace.
            Assert.That(callback, Is.Not.Null);
            callback.Invoke(logger, new object[] { "audit warning", stackTrace, LogType.Warning });

            Assert.That(_text.text, Does.Contain("\n     " + expectedPath.SetColor(Color.cyan)));
            Assert.That(_text.text, Does.Contain("audit warning"));
        }

        private void SetOverlayField(string name, object value)
        {
            FieldInfo field = typeof(NeoDebugOverlay).GetField(name, PrivateInstance);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(_overlay, value);
        }

        private void InvokeOverlay(string name)
        {
            MethodInfo callback = typeof(NeoDebugOverlay).GetMethod(name, PrivateInstance);
            Assert.That(callback, Is.Not.Null, name);
            callback.Invoke(_overlay, null);
        }
    }
}
