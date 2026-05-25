using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class PropertyAttributeEditModeTests
    {
        private interface ITestContract
        {
        }

        private enum TestMode
        {
            First,
            Second
        }

        private class NotInterface
        {
        }

        private sealed class TestContractComponent : MonoBehaviour, ITestContract
        {
        }

        private sealed class TestContractScriptableObject : ScriptableObject, ITestContract
        {
        }

        private sealed class PlainScriptableObject : ScriptableObject
        {
        }

        private sealed class RequireInterfaceHolder : ScriptableObject
        {
            [RequireInterface(typeof(ITestContract))]
            public Object Reference;
        }

        private sealed class ButtonTarget : ScriptableObject
        {
            [Button("Explicit", 88f)]
            private void FirstButton()
            {
            }

            [Button]
            public static void SecondButton()
            {
            }

            public void OrdinaryMethod()
            {
            }
        }

        [Test]
        public void NeoDocAttribute_NormalizesRelativePath()
        {
            NeoDocAttribute attribute = new("\\Tools\\Components\\Counter.md");

            Assert.AreEqual("Tools/Components/Counter.md", attribute.DocPath);
        }

        [Test]
        public void ButtonAttribute_StoresNameAndWidth()
        {
            ButtonAttribute attribute = new("Run", 160f);

            Assert.AreEqual("Run", attribute.ButtonName);
            Assert.AreEqual(160f, attribute.Width);
        }

        [Test]
        public void GUIColorAttribute_StoresRgbaColor()
        {
            GUIColorAttribute attribute = new(0.1, 0.2, 0.3, 0.4);

            Assert.AreEqual(new Color(0.1f, 0.2f, 0.3f, 0.4f), attribute.color);
        }

        [Test]
        public void RequireInterface_AcceptsOnlyInterfaceTypes()
        {
            RequireInterface valid = new(typeof(ITestContract));

            Assert.AreEqual(typeof(ITestContract), valid.RequireType);
            Assert.Throws<ArgumentException>(() => new RequireInterface(typeof(NotInterface)));
        }

        [Test]
        public void RequireInterfaceDrawer_ValidatesGameObjectAndScriptableObjectReferences()
        {
            GameObject validGameObject = new("RequireInterface_Valid");
            GameObject invalidGameObject = new("RequireInterface_Invalid");
            TestContractScriptableObject validSo = ScriptableObject.CreateInstance<TestContractScriptableObject>();
            PlainScriptableObject invalidSo = ScriptableObject.CreateInstance<PlainScriptableObject>();

            try
            {
                validGameObject.AddComponent<TestContractComponent>();

                Assert.That(RequireInterfaceDrawer.IsReferenceValid(validGameObject, typeof(ITestContract)), Is.True);
                Assert.That(RequireInterfaceDrawer.IsReferenceValid(invalidGameObject, typeof(ITestContract)), Is.False);
                Assert.That(RequireInterfaceDrawer.IsReferenceValid(validSo, typeof(ITestContract)), Is.True);
                Assert.That(RequireInterfaceDrawer.IsReferenceValid(invalidSo, typeof(ITestContract)), Is.False);
                Assert.That(RequireInterfaceDrawer.IsReferenceValid(null, typeof(ITestContract)), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(validGameObject);
                Object.DestroyImmediate(invalidGameObject);
                Object.DestroyImmediate(validSo);
                Object.DestroyImmediate(invalidSo);
            }
        }

        [Test]
        public void RequireInterfaceDrawer_AcceptsObjectReferenceSerializedProperty()
        {
            RequireInterfaceHolder holder = ScriptableObject.CreateInstance<RequireInterfaceHolder>();
            try
            {
                SerializedObject serializedObject = new(holder);
                SerializedProperty reference = serializedObject.FindProperty(nameof(RequireInterfaceHolder.Reference));

                Assert.That(RequireInterfaceDrawer.IsValidProperty(reference, typeof(ITestContract)), Is.True);
                Assert.That(RequireInterfaceDrawer.IsValidProperty(reference, typeof(NotInterface)), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void ButtonAttributeDrawer_FindsPrivateAndStaticButtonMethods()
        {
            MethodInfo method = ButtonAttributeDrawer.FindButtonMethod(typeof(ButtonTarget));
            ButtonAttribute attribute = method.GetCustomAttribute<ButtonAttribute>();

            Assert.That(method, Is.Not.Null);
            Assert.That(method.Name, Is.EqualTo("FirstButton"));
            Assert.That(attribute, Is.Not.Null);
            Assert.That(ButtonAttributeDrawer.GetButtonText(method, attribute), Is.EqualTo("Explicit"));
            Assert.That(attribute.Width, Is.EqualTo(88f));
        }

        [Test]
        public void ButtonAttributeDrawer_UsesMethodNameWhenButtonNameIsEmpty()
        {
            MethodInfo method = typeof(ButtonTarget).GetMethod(nameof(ButtonTarget.SecondButton),
                BindingFlags.Public | BindingFlags.Static);
            ButtonAttribute attribute = method.GetCustomAttribute<ButtonAttribute>();

            Assert.That(ButtonAttributeDrawer.GetButtonText(method, attribute), Is.EqualTo(nameof(ButtonTarget.SecondButton)));
        }

        [Test]
        public void ButtonAttributeDrawer_DefaultValuesMatchParameterTypes()
        {
            Assert.That(ButtonAttributeDrawer.GetDefaultValue(typeof(int)), Is.EqualTo(0));
            Assert.That(ButtonAttributeDrawer.GetDefaultValue(typeof(float)), Is.EqualTo(0f));
            Assert.That(ButtonAttributeDrawer.GetDefaultValue(typeof(bool)), Is.EqualTo(false));
            Assert.That(ButtonAttributeDrawer.GetDefaultValue(typeof(TestMode)), Is.EqualTo(TestMode.First));
            Assert.That(ButtonAttributeDrawer.GetDefaultValue(typeof(GameObject)), Is.Null);
            Assert.That(ButtonAttributeDrawer.GetDefaultValue(typeof(string)), Is.Null);
        }

        [Test]
        public void GUIColorAttributeDrawer_ResolvesAttributeColorOrFallback()
        {
            GUIColorAttribute attribute = new(0.25, 0.5, 0.75, 0.9);
            Color fallback = Color.magenta;

            Assert.That(GUIColorAttributeDrawer.GetColor(attribute, fallback), Is.EqualTo(attribute.color));
            Assert.That(GUIColorAttributeDrawer.GetColor(null, fallback), Is.EqualTo(fallback));
        }
    }
}
