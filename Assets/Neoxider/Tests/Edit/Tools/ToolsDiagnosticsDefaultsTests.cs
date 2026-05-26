using System;
using System.Reflection;
using Neo.Tools;
using Neo.Tools.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class ToolsDiagnosticsDefaultsTests
    {
        [TestCase(typeof(LeaderboardMove), "_debugLogging")]
        [TestCase(typeof(LeaderboardMove), "_logWarnings")]
        [TestCase(typeof(MouseEffect), "_logMissingManagerWarning")]
        [TestCase(typeof(MouseEffect), "_logMissingCameraWarning")]
        [TestCase(typeof(KeyboardMover), "_logInputFallbackWarnings")]
        [TestCase(typeof(MouseMover2D), "_logMissingCameraWarning")]
        [TestCase(typeof(MouseMover3D), "_logMissingCameraWarning")]
        [TestCase(typeof(SetText), "_logWarnings")]
        [TestCase(typeof(TimeToText), "_logWarnings")]
        [TestCase(typeof(MeshEmission), "debugLog")]
        [TestCase(typeof(MeshEmission), "_logWarnings")]
        public void DiagnosticsFlags_AreDisabledByDefault(Type componentType, string fieldName)
        {
            GameObject go = new(componentType.Name);
            try
            {
                Component component = go.AddComponent(componentType);
                FieldInfo field = componentType.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                Assert.That(field, Is.Not.Null, $"{componentType.Name}.{fieldName} field was not found.");
                Assert.That(field.FieldType, Is.EqualTo(typeof(bool)));
                Assert.That((bool)field.GetValue(component), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void LeaderboardMove_MoveWithoutLeaderboard_DoesNotLogByDefault()
        {
            GameObject go = new("LeaderboardMove_NoLeaderboard");
            try
            {
                LeaderboardMove move = go.AddComponent<LeaderboardMove>();

                move.Move();

                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SetText_MissingText_DoesNotLogByDefault()
        {
            GameObject go = new("SetText_NoText");
            try
            {
                SetText setText = go.AddComponent<SetText>();

                setText.Set("value");

                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void MouseEffect_SetTargetCamera_StoresExplicitCameraReference()
        {
            GameObject go = new("MouseEffect_Camera");
            GameObject cameraGo = new("MouseEffect_Camera_Target");
            try
            {
                MouseEffect effect = go.AddComponent<MouseEffect>();
                Camera camera = cameraGo.AddComponent<Camera>();

                effect.SetTargetCamera(camera);

                Assert.That(effect.TargetCamera, Is.SameAs(camera));
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(go);
            }
        }
    }
}
