using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests
{
    public class NeoxiderMenuChangedBehaviorTests
    {
        [Test]
        public void GM_Menu_FromPause_RestoresTimeScale()
        {
            GameObject gmObject = new("GM");
            GM gm = gmObject.AddComponent<GM>();
            gm.useTimeScalePause = true;
            gm.State = GM.GameState.Game;
            Time.timeScale = 1f;

            gm.Pause();
            gm.Menu();

            Assert.AreEqual(GM.GameState.Menu, gm.State);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }
    }

    public class NeoxiderPagesChangedBehaviorTests
    {
        private static Type PMType;
        private static Type UIPageType;
        private static Type PageIdType;
        private static Type UIPageAnimationModeType;
        private const BindingFlags PrivateInstanceBinding = BindingFlags.Instance | BindingFlags.NonPublic;
        private static bool _pagesTypesResolved;
        private static bool _pagesTypesAvailable;

        [Test]
        public void PM_SetPage_DoesNotClosePopup_WhenClosePopupsDisabled()
        {
            EnsurePagesModuleAvailable();

            GameObject pmObject = new("PM");
            object pm = pmObject.AddComponent(PMType);

            object mainPage = CreateUiPage(pmObject, "PageMenu", false, true);
            object popupPage = CreateUiPage(pmObject, "PopupPage", true, true);
            object mainPageId = GetPrivateField(mainPage, "pageId");

            SetPrivateField(pm, "allPages", CreateArray(UIPageType, mainPage, popupPage));
            SetPrivateField(pm, "ignoredPageIds", CreateArray(PageIdType, (object)null));
            SetPrivateField(pm, "closePopupsOnExclusivePageChange", false);

            InvokeInstanceMethod(pm, "SetPage", mainPageId);

            Assert.IsTrue(((Component)mainPage).gameObject.activeSelf);
            Assert.IsTrue(((Component)popupPage).gameObject.activeSelf);
        }

        [Test]
        public void PM_SetPage_ClosesPopup_WhenClosePopupsEnabled()
        {
            EnsurePagesModuleAvailable();

            GameObject pmObject = new("PM");
            object pm = pmObject.AddComponent(PMType);

            object mainPage = CreateUiPage(pmObject, "PageMenu", false, true);
            object popupPage = CreateUiPage(pmObject, "PopupPage", true, true);
            object mainPageId = GetPrivateField(mainPage, "pageId");

            SetPrivateField(pm, "allPages", CreateArray(UIPageType, mainPage, popupPage));
            SetPrivateField(pm, "ignoredPageIds", CreateArray(PageIdType, (object)null));
            SetPrivateField(pm, "closePopupsOnExclusivePageChange", true);

            InvokeInstanceMethod(pm, "SetPage", mainPageId);

            Assert.IsTrue(((Component)mainPage).gameObject.activeSelf);
            Assert.IsFalse(((Component)popupPage).gameObject.activeSelf);
        }

        [Test]
        public void UIPage_HideAnimationDuration_ReturnsZero_WhenModeNone()
        {
            EnsurePagesModuleAvailable();

            GameObject pageObject = new("UI Page");
            object page = pageObject.AddComponent(UIPageType);

            SetPrivateField(page, "_animationMode", Enum.Parse(UIPageAnimationModeType, "None"));

            float hideAnimationDuration = (float)GetPropertyValue(page, "HideAnimationDuration");
            Assert.AreEqual(0f, hideAnimationDuration, 0.0001f);
        }

        [Test]
        public void UIPage_WaitForHideAnimation_DoesNotWait_WhenNoHideAnimation()
        {
            EnsurePagesModuleAvailable();

            GameObject pageObject = new("UI Page");
            object page = pageObject.AddComponent(UIPageType);

            var hideRoutine = (IEnumerator)InvokeInstanceMethod(page, "WaitForHideAnimation");

            Assert.IsFalse(hideRoutine.MoveNext());
        }

        public static object CreateUiPage(GameObject parent, string pageIdName, bool popup, bool active)
        {
            GameObject pageObject = new(pageIdName);
            pageObject.transform.SetParent(parent.transform);
            object page = pageObject.AddComponent(UIPageType);
            var pageId = (ScriptableObject)ScriptableObject.CreateInstance(PageIdType);
            pageId.name = pageIdName;

            SetPrivateField(page, "pageId", pageId);
            SetPrivateField(page, "popup", popup);
            pageObject.SetActive(active);

            return page;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstanceBinding);
            Assert.IsNotNull(field, $"Field '{fieldName}' should exist on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstanceBinding);
            Assert.IsNotNull(field, $"Field '{fieldName}' should exist on {target.GetType().Name}");
            return field.GetValue(target);
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            PropertyInfo property =
                target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property, $"Property '{propertyName}' should exist on {target.GetType().Name}");
            return property.GetValue(target);
        }

        private static object InvokeInstanceMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(method, $"Method '{methodName}' should exist on {target.GetType().Name}");
            return method.Invoke(target, args);
        }

        private static object CreateArray(Type elementType, params object[] values)
        {
            var array = Array.CreateInstance(elementType, values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                array.SetValue(values[i], i);
            }

            return array;
        }

        private static void EnsurePagesModuleAvailable()
        {
            if (_pagesTypesResolved)
            {
                if (!_pagesTypesAvailable)
                {
                    Assert.Ignore("Neoxider Pages module is not available in this test assembly.");
                }

                return;
            }

            _pagesTypesResolved = true;
            PMType = GetTypeOrNull("Neo.Pages.PM");
            UIPageType = GetTypeOrNull("Neo.Pages.UIPage");
            PageIdType = GetTypeOrNull("Neo.Pages.PageId");
            UIPageAnimationModeType = GetTypeOrNull("Neo.Pages.UIPageAnimationMode");

            _pagesTypesAvailable = PMType != null && UIPageType != null && PageIdType != null &&
                                   UIPageAnimationModeType != null;
            if (!_pagesTypesAvailable)
            {
                string message = $"Required Neoxider Pages types are missing. Missing: " +
                                 $"{(PMType == null ? "Neo.Pages.PM " : string.Empty)}" +
                                 $"{(UIPageType == null ? "Neo.Pages.UIPage " : string.Empty)}" +
                                 $"{(PageIdType == null ? "Neo.Pages.PageId " : string.Empty)}" +
                                 $"{(UIPageAnimationModeType == null ? "Neo.Pages.UIPageAnimationMode " : string.Empty)}";
                Assert.Ignore(message.Trim());
            }
        }

        private static Type GetTypeOrNull(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(type => type != null);
        }
    }
}
