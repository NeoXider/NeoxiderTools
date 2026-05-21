using System.Collections;
using System.Reflection;
using Neo.Pages;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests
{
    public class NeoxiderPagesChangedBehaviorTests
    {
        private const BindingFlags PrivateInstanceBinding = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void PM_SetPage_DoesNotClosePopup_WhenClosePopupsDisabled()
        {
            GameObject pmObject = new("PM");
            PM pm = pmObject.AddComponent<PM>();

            UIPage mainPage = CreateUiPage(pmObject, "PageMenu", false, active: true);
            UIPage popupPage = CreateUiPage(pmObject, "PopupPage", true, active: true);
            PageId mainPageId = GetPrivateField<PageId>(mainPage, "pageId");

            SetPrivateField(pm, "allPages", new[] { mainPage, popupPage });
            SetPrivateField(pm, "ignoredPageIds", new PageId[] { null });
            SetPrivateField(pm, "closePopupsOnExclusivePageChange", false);

            pm.SetPage(mainPageId);

            Assert.IsTrue(mainPage.gameObject.activeSelf);
            Assert.IsTrue(popupPage.gameObject.activeSelf);
        }

        [Test]
        public void PM_SetPage_ClosesPopup_WhenClosePopupsEnabled()
        {
            GameObject pmObject = new("PM");
            PM pm = pmObject.AddComponent<PM>();

            UIPage mainPage = CreateUiPage(pmObject, "PageMenu", false, active: true);
            UIPage popupPage = CreateUiPage(pmObject, "PopupPage", true, active: true);
            PageId mainPageId = GetPrivateField<PageId>(mainPage, "pageId");

            SetPrivateField(pm, "allPages", new[] { mainPage, popupPage });
            SetPrivateField(pm, "ignoredPageIds", new PageId[] { null });
            SetPrivateField(pm, "closePopupsOnExclusivePageChange", true);

            pm.SetPage(mainPageId);

            Assert.IsTrue(mainPage.gameObject.activeSelf);
            Assert.IsFalse(popupPage.gameObject.activeSelf);
        }

        [Test]
        public void UIPage_HideAnimationDuration_ReturnsZero_WhenModeNone()
        {
            GameObject pageObject = new("UI Page");
            UIPage page = pageObject.AddComponent<UIPage>();

            SetPrivateField(page, "_animationMode", UIPageAnimationMode.None);

            Assert.AreEqual(0f, page.HideAnimationDuration, 0.0001f);
        }

        [Test]
        public void UIPage_WaitForHideAnimation_DoesNotWait_WhenNoHideAnimation()
        {
            GameObject pageObject = new("UI Page");
            UIPage page = pageObject.AddComponent<UIPage>();

            IEnumerator hideRoutine = page.WaitForHideAnimation();

            Assert.IsFalse(hideRoutine.MoveNext());
        }

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

        private static UIPage CreateUiPage(GameObject parent, string pageIdName, bool popup, bool active)
        {
            GameObject pageObject = new(pageIdName);
            pageObject.transform.SetParent(parent.transform);
            UIPage page = pageObject.AddComponent<UIPage>();
            PageId pageId = ScriptableObject.CreateInstance<PageId>();
            pageId.name = pageIdName;

            SetPrivateField(page, "pageId", pageId);
            SetPrivateField(page, "popup", popup);
            pageObject.SetActive(active);

            return page;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstanceBinding);
            Assert.IsNotNull(field, $"Field '{fieldName}' should exist on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstanceBinding);
            Assert.IsNotNull(field, $"Field '{fieldName}' should exist on {target.GetType().Name}");
            return (T)field.GetValue(target);
        }
    }
}
