using System;
using System.Reflection;
using Neo.Animations;
using Neo.Audio;
using Neo.NPC;
using Neo.Tools;
using Neo.Tools.View;
using NUnit.Framework;
using UnityEngine;

#pragma warning disable CS0618

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class InspectorButtonCoverageTests
    {
        private static readonly object[] PlayModeButtonCases =
        {
            new object[] { typeof(ColorAnimator), nameof(ColorAnimator.Pause), Type.EmptyTypes, null },
            new object[] { typeof(ColorAnimator), nameof(ColorAnimator.Resume), Type.EmptyTypes, null },
            new object[] { typeof(FloatAnimator), nameof(FloatAnimator.Pause), Type.EmptyTypes, null },
            new object[] { typeof(FloatAnimator), nameof(FloatAnimator.Resume), Type.EmptyTypes, null },
            new object[] { typeof(Vector3Animator), nameof(Vector3Animator.Pause), Type.EmptyTypes, null },
            new object[] { typeof(Vector3Animator), nameof(Vector3Animator.Resume), Type.EmptyTypes, null },
            new object[] { typeof(TransformAnimator), nameof(TransformAnimator.Pause), Type.EmptyTypes, null },
            new object[] { typeof(TransformAnimator), nameof(TransformAnimator.Resume), Type.EmptyTypes, null },
            new object[] { typeof(LightAnimator), nameof(LightAnimator.Pause), Type.EmptyTypes, null },
            new object[] { typeof(LightAnimator), nameof(LightAnimator.Resume), Type.EmptyTypes, null },
            new object[] { typeof(RandomMusicController), nameof(RandomMusicController.Pause), Type.EmptyTypes, null },
            new object[] { typeof(RandomMusicController), nameof(RandomMusicController.Resume), Type.EmptyTypes, null },
            new object[] { typeof(AiNavigation), nameof(AiNavigation.Resume), Type.EmptyTypes, null },
            new object[] { typeof(NpcNavigation), nameof(NpcNavigation.Stop), Type.EmptyTypes, null },
            new object[] { typeof(NpcNavigation), nameof(NpcNavigation.Resume), Type.EmptyTypes, null },
            new object[] { typeof(ToggleObject), nameof(ToggleObject.Toggle), Type.EmptyTypes, null },
            new object[] { typeof(ToggleObject), nameof(ToggleObject.Set), new Type[] { typeof(bool) }, null },
            new object[] { typeof(CameraShake), nameof(CameraShake.StartShake), Type.EmptyTypes, null },
            new object[] { typeof(CameraShake), nameof(CameraShake.StopShake), Type.EmptyTypes, null },
            new object[] { typeof(CameraShake), nameof(CameraShake.StopAndReset), Type.EmptyTypes, "Stop And Reset" },
            new object[]
                { typeof(PlayerController2DPhysics), nameof(PlayerController2DPhysics.SetJumpInput), Type.EmptyTypes, "Jump" },
            new object[]
                { typeof(PlayerController3DPhysics), nameof(PlayerController3DPhysics.SetJumpInput), Type.EmptyTypes, "Jump" },
            new object[] { typeof(TypewriterEffectComponent), nameof(TypewriterEffectComponent.Clear), Type.EmptyTypes, null }
        };

        [TestCaseSource(nameof(PlayModeButtonCases))]
        public void RuntimeAction_HasPlayModeOnlyInspectorButton(Type componentType, string methodName,
            Type[] parameterTypes, string expectedButtonName)
        {
            MethodInfo method = componentType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, null,
                parameterTypes, null);

            Assert.That(method, Is.Not.Null, $"{componentType.FullName}.{methodName} was not found");

            ButtonAttribute attribute = method.GetCustomAttribute<ButtonAttribute>();

            Assert.That(attribute, Is.Not.Null, $"{componentType.FullName}.{methodName} has no ButtonAttribute");
            Assert.That(attribute.PlayModeOnly, Is.True,
                $"{componentType.FullName}.{methodName} must be disabled outside Play Mode");
            Assert.That(attribute.ButtonName, Is.EqualTo(expectedButtonName));
        }

        [Test]
        public void ToggleObject_DebugFlag_DoesNotInvokeEventsInEditMode()
        {
            GameObject gameObject = new GameObject("ToggleObjectEditModeSafety");
            try
            {
                ToggleObject toggleObject = gameObject.AddComponent<ToggleObject>();
                int invocationCount = 0;
                toggleObject.ON = new UnityEngine.Events.UnityEvent();
                toggleObject.OFF = new UnityEngine.Events.UnityEvent();
                toggleObject.OnChangeFlip = new UnityEngine.Events.UnityEvent<bool>();
                toggleObject.ON.AddListener(() => invocationCount++);
                toggleObject.OFF.AddListener(() => invocationCount++);
                toggleObject.OnChangeFlip.AddListener(_ => invocationCount++);
                toggleObject.toggleDebug = true;

                MethodInfo onValidate = typeof(ToggleObject).GetMethod("OnValidate",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                onValidate?.Invoke(toggleObject, null);

                Assert.That(toggleObject.toggleDebug, Is.False);
                Assert.That(invocationCount, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}

#pragma warning restore CS0618
