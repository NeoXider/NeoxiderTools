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
            var rect = (RectTransform)_go.transform;
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
