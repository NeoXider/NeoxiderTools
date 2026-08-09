using System;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Tools
{
    public sealed class OtherToolsBehaviorTests
    {
        [Test]
        public void RevertAmount_EmitsOneMinusInput()
        {
            GameObject gameObject = new GameObject("RevertAmountBehaviorTests");

            try
            {
                RevertAmount revertAmount = gameObject.AddComponent<RevertAmount>();
                revertAmount.OnChange = new UnityEvent<float>();
                float observed = float.NaN;
                revertAmount.OnChange.AddListener(value => observed = value);

                revertAmount.Amount(0.35f);

                Assert.That(observed, Is.EqualTo(0.65f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CameraShake_StopAndReset_CancelsShakeAndRestoresCapturedTransform()
        {
            GameObject gameObject = new GameObject("CameraShakeBehaviorTests");

            try
            {
                CameraShake cameraShake = gameObject.AddComponent<CameraShake>();
                cameraShake.OnShakeStart = new UnityEvent();
                cameraShake.OnShakeStop = new UnityEvent();
                cameraShake.OnShakeComplete = new UnityEvent();
                int started = 0;
                int stopped = 0;
                int completed = 0;
                cameraShake.OnShakeStart.AddListener(() => started++);
                cameraShake.OnShakeStop.AddListener(() => stopped++);
                cameraShake.OnShakeComplete.AddListener(() => completed++);

                cameraShake.StartShake(10f, 0f);
                FieldInfo sequenceField = typeof(CameraShake).GetField(
                    "_sequence",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(sequenceField, Is.Not.Null);
                object activeSequence = sequenceField.GetValue(cameraShake);
                Assert.That(activeSequence, Is.Not.Null);
                gameObject.transform.localPosition = Vector3.one;
                gameObject.transform.localRotation = Quaternion.Euler(10f, 20f, 30f);
                cameraShake.StopAndReset();

                Type tweenExtensions = activeSequence.GetType().Assembly.GetType("DG.Tweening.TweenExtensions");
                Assert.That(tweenExtensions, Is.Not.Null);
                MethodInfo complete = FindCompleteWithCallbacksMethod(tweenExtensions, activeSequence.GetType());
                Assert.That(complete, Is.Not.Null);
                complete.Invoke(null, new[] { activeSequence, (object)true });

                Assert.That(started, Is.EqualTo(1));
                Assert.That(stopped, Is.EqualTo(1));
                Assert.That(completed, Is.Zero, "Stopping a shake must suppress its completion callback.");
                Assert.That(cameraShake.IsShaking, Is.False);
                Assert.That(sequenceField.GetValue(cameraShake), Is.Null);
                Assert.That(gameObject.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(gameObject.transform.localRotation, Is.EqualTo(Quaternion.identity));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static MethodInfo FindCompleteWithCallbacksMethod(Type extensionsType, Type sequenceType)
        {
            MethodInfo[] methods = extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                if (method.Name != "Complete")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 2 &&
                    parameters[0].ParameterType.IsAssignableFrom(sequenceType) &&
                    parameters[1].ParameterType == typeof(bool))
                {
                    return method;
                }
            }

            return null;
        }

        [Test]
        public void AiNavigation_PublicConfigurationClampsInvalidValues()
        {
            GameObject gameObject = new GameObject("AiNavigationBehaviorTests");

            try
            {
#pragma warning disable CS0618
                AiNavigation navigation = gameObject.AddComponent<AiNavigation>();
#pragma warning restore CS0618

                navigation.StoppingDistance = -5f;
                navigation.WalkSpeed = 0f;
                navigation.RunSpeed = -1f;
                navigation.Acceleration = 0f;
                navigation.TurnSpeed = 0f;
                navigation.TriggerDistance = -2f;

                Assert.That(navigation.StoppingDistance, Is.EqualTo(0.01f));
                Assert.That(navigation.WalkSpeed, Is.EqualTo(0.1f));
                Assert.That(navigation.RunSpeed, Is.EqualTo(0.1f));
                Assert.That(navigation.Acceleration, Is.EqualTo(0.1f));
                Assert.That(navigation.TurnSpeed, Is.EqualTo(1f));
                Assert.That(navigation.TriggerDistance, Is.Zero);
                Assert.That(gameObject.GetComponent<NavMeshAgent>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
