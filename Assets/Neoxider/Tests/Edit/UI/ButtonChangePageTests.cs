using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UIManager = Neo.UI.UI;

namespace Neo.Tests.Edit
{
    public sealed class ButtonChangePageTests
    {
        private GameObject _root;
        private GameObject _pageA;
        private GameObject _pageB;
        private GameObject _eventSystem;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("UIRoot");
            _pageA = new GameObject("PageA");
            _pageB = new GameObject("PageB");
            _pageA.transform.SetParent(_root.transform);
            _pageB.transform.SetParent(_root.transform);
            _eventSystem = new GameObject("EventSystem");
            _eventSystem.AddComponent<EventSystem>();

            var ui = _root.AddComponent<UIManager>();
            SetPrivateField(ui, "_pages", new[] { _pageA, _pageB });
            InvokePrivate(ui, "Awake");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_eventSystem);
        }

        [Test]
        public void Submit_ChangesPageForKeyboardNavigation()
        {
            GameObject buttonGo = new("Button");
            buttonGo.transform.SetParent(_root.transform);
            try
            {
                var selectable = buttonGo.AddComponent<Button>();
                var changePage = buttonGo.AddComponent<ButtonChangePage>();
                SetPrivateField(changePage, "_selectable", selectable);
                SetPrivateField(changePage, "_idPage", 1);

                changePage.OnSubmit(new BaseEventData(EventSystem.current));

                Assert.That(_pageA.activeSelf, Is.False);
                Assert.That(_pageB.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(buttonGo);
            }
        }

        [Test]
        public void Submit_IgnoresNonInteractableSelectable()
        {
            GameObject buttonGo = new("Button");
            buttonGo.transform.SetParent(_root.transform);
            try
            {
                var selectable = buttonGo.AddComponent<Button>();
                selectable.interactable = false;
                var changePage = buttonGo.AddComponent<ButtonChangePage>();
                SetPrivateField(changePage, "_selectable", selectable);
                SetPrivateField(changePage, "_idPage", 1);

                changePage.OnSubmit(new BaseEventData(EventSystem.current));

                Assert.That(_pageA.activeSelf, Is.True);
                Assert.That(_pageB.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(buttonGo);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }
    }
}
