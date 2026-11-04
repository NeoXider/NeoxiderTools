using System.Reflection;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit
{
    public sealed class ButtonScaleTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void SetPressed_PreservesBaseZScale()
        {
            _go = new GameObject("ButtonScale", typeof(RectTransform));
            RectTransform rect = (RectTransform)_go.transform;
            rect.localScale = new Vector3(1f, 1f, 1f);

            ButtonScale scale = _go.AddComponent<ButtonScale>();
            SetPrivateField(scale, "_pressedSize", new Vector2(0.85f, 0.85f));
            SetPrivateField(scale, "resizeDuration", 0f);
            InvokePrivate(scale, "Awake");

            // WHY: a zero resize duration makes ResizeButton run to completion inside StartCoroutine,
            // so the final scale is observable synchronously in EditMode.
            scale.SetPressed(true);

            Assert.That(rect.localScale.x, Is.EqualTo(0.85f).Within(0.0001f));
            Assert.That(rect.localScale.z, Is.EqualTo(1f).Within(0.0001f),
                "Pressing must not flatten localScale.z to 0.");

            scale.SetPressed(false);
            Assert.That(rect.localScale.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(rect.localScale.z, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Release_ReturnsToTheScaleTheButtonHadWhenThePressStarted()
        {
            _go = new GameObject("ButtonScale", typeof(RectTransform));
            RectTransform rect = (RectTransform)_go.transform;
            ButtonScale scale = _go.AddComponent<ButtonScale>();
            SetPrivateField(scale, "resizeDuration", 0f);
            InvokePrivate(scale, "Awake");
            scale.SetPressed(true);
            scale.SetPressed(false);

            Vector3 laidOut = new Vector3(1.3f, 1.3f, 1f);
            rect.localScale = laidOut;
            scale.SetPressed(true);
            Assert.That(rect.localScale.x, Is.EqualTo(0.85f).Within(0.0001f));
            scale.SetPressed(true);
            scale.SetPressed(false);

            Assert.That(rect.localScale, Is.EqualTo(laidOut),
                "A button rescaled after Awake must come back to its new scale, not the Awake snapshot.");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }
    }
}
