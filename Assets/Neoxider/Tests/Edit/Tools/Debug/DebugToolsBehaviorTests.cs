using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Neo.Editor.Tests.Tools
{
    public sealed class DebugToolsBehaviorTests
    {
        [Test]
        public void ErrorLogger_UpdateAndAppendText_ModifyAssignedOutput()
        {
            GameObject gameObject = new GameObject("ErrorLoggerBehaviorTests");
            gameObject.SetActive(false);

            try
            {
                TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
                ErrorLogger logger = gameObject.AddComponent<ErrorLogger>();
                logger.textMesh = text;

                logger.UpdateText("first");
                logger.AppendText(" second");

                Assert.That(text.text, Is.EqualTo("first second"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ErrorLogger_TextMethods_AreSafeWithoutAssignedOutput()
        {
            GameObject gameObject = new GameObject("ErrorLoggerNullOutputBehaviorTests");
            gameObject.SetActive(false);

            try
            {
                ErrorLogger logger = gameObject.AddComponent<ErrorLogger>();

                Assert.DoesNotThrow(() => logger.UpdateText("ignored"));
                Assert.DoesNotThrow(() => logger.AppendText("ignored"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ErrorLogger_OnEnable_IsSafeWithoutAssignedOutput()
        {
            GameObject gameObject = new GameObject("ErrorLoggerNullEnableBehaviorTests");
            gameObject.SetActive(false);

            try
            {
                ErrorLogger logger = gameObject.AddComponent<ErrorLogger>();
                MethodInfo onEnable = typeof(ErrorLogger).GetMethod(
                    "OnEnable",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo onDisable = typeof(ErrorLogger).GetMethod(
                    "OnDisable",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(onEnable, Is.Not.Null);
                Assert.That(onDisable, Is.Not.Null);
                Assert.DoesNotThrow(() => onEnable.Invoke(logger, null));
                Assert.DoesNotThrow(() => onDisable.Invoke(logger, null));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Fps_PublicSettings_UpdateEngineFramerateState()
        {
            GameObject gameObject = new GameObject("FpsBehaviorTests");
            gameObject.SetActive(false);
            int originalTargetFramerate = Application.targetFrameRate;
            int originalVSync = QualitySettings.vSyncCount;

            try
            {
                FPS fps = gameObject.AddComponent<FPS>();

                fps.SetTargetFramerate(75);
                fps.SetVSync(true);

                Assert.That(Application.targetFrameRate, Is.EqualTo(75));
                Assert.That(QualitySettings.vSyncCount, Is.EqualTo(1));

                fps.SetVSync(false);
                Assert.That(QualitySettings.vSyncCount, Is.Zero);
            }
            finally
            {
                Application.targetFrameRate = originalTargetFramerate;
                QualitySettings.vSyncCount = originalVSync;
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
