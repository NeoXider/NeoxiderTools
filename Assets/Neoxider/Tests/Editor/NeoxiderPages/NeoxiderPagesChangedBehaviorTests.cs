using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Audio;
using Neo.Level;
using Neo.Shop;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Neo.Tests
{
    public class NeoxiderMenuChangedBehaviorTests
    {
        [Test]
        public void GM_Menu_FromPause_RestoresTimeScale()
        {
            GameObject gmObject = new("GM");
            try
            {
                GM gm = gmObject.AddComponent<GM>();
                gm.useTimeScalePause = true;
                gm.State = GM.GameState.Game;
                Time.timeScale = 1f;

                gm.Pause();
                gm.Menu();

                Assert.AreEqual(GM.GameState.Menu, gm.State);
                Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            }
            finally
            {
                Time.timeScale = 1f;
                UnityEngine.Object.DestroyImmediate(gmObject);
            }
        }
    }

    /// <summary>
    ///     Behaviour of the NeoxiderPages sample (PM, UIPage, BtnChangePage, FakeLoad, PageSubscriber, UIKit). The
    ///     sample compiles into Assembly-CSharp only while it is visible, so every type is resolved by reflection and
    ///     the tests are ignored when the sample is hidden in Samples~.
    /// </summary>
    public class NeoxiderPagesChangedBehaviorTests
    {
        private static Type PMType;
        private static Type UIPageType;
        private static Type PageIdType;
        private static Type UIPageAnimationModeType;
        private const BindingFlags PrivateInstanceBinding = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstanceBinding = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags PrivateStaticBinding = BindingFlags.Static | BindingFlags.NonPublic;
        private static bool _pagesTypesResolved;
        private static bool _pagesTypesAvailable;

        // WHY: edit-mode tests share the open scene; a PM left behind would be found by PM.I in later tests.
        private static readonly List<UnityEngine.Object> Created = new();

        [TearDown]
        public void DestroyCreatedObjects()
        {
            for (int i = Created.Count - 1; i >= 0; i--)
            {
                if (Created[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(Created[i]);
                }
            }

            Created.Clear();
            ResetPageManagerSingleton();
        }

        [Test]
        public void PM_SetPage_DoesNotClosePopup_WhenClosePopupsDisabled()
        {
            EnsurePagesModuleAvailable();

            GameObject pmObject = NewObject("PM");
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

            GameObject pmObject = NewObject("PM");
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
        public void PM_SetPage_RaisesOnPageChangedOnce()
        {
            EnsurePagesModuleAvailable();

            object pm = CreatePageManager(out GameObject pmObject);
            object menuPage = CreateUiPage(pmObject, "PageMenu", false, false);
            CreateUiPage(pmObject, "PageGame", false, true);
            List<object> changes = ListenToPageChanges(pm);

            InvokeInstanceMethod(pm, "SetPage", GetPrivateField(menuPage, "pageId"));

            Assert.AreEqual(1, changes.Count, "SetPage used to raise OnPageChanged twice (ActivatePages + SetPage).");
            Assert.AreSame(menuPage, changes[0]);
            Assert.AreSame(menuPage, GetPublicField(pm, "currentUiPage"));
        }

        [Test]
        public void PM_SetPage_UnknownPage_KeepsCurrentAndPreviousPages()
        {
            EnsurePagesModuleAvailable();

            object pm = CreatePageManager(out GameObject pmObject);
            object menuPage = CreateUiPage(pmObject, "PageMenu", false, false);
            object gamePage = CreateUiPage(pmObject, "PageGame", false, false);
            InvokeInstanceMethod(pm, "SetPage", GetPrivateField(menuPage, "pageId"));
            InvokeInstanceMethod(pm, "SetPage", GetPrivateField(gamePage, "pageId"));
            List<object> changes = ListenToPageChanges(pm);

            InvokeInstanceMethod(pm, "SetPage", CreatePageId("PageNowhere"));

            Assert.AreSame(gamePage, GetPublicField(pm, "currentUiPage"), "A failed switch must not blank the current page.");
            Assert.AreSame(menuPage, GetPublicField(pm, "previousUiPage"));
            Assert.AreEqual(0, changes.Count);
            Assert.IsTrue(((Component)gamePage).gameObject.activeSelf);
        }

        [Test]
        public void PM_ActivePage_UnknownPage_KeepsCurrentPage()
        {
            EnsurePagesModuleAvailable();

            object pm = CreatePageManager(out GameObject pmObject);
            object menuPage = CreateUiPage(pmObject, "PageMenu", false, false);
            InvokeInstanceMethod(pm, "SetPage", GetPrivateField(menuPage, "pageId"));

            bool errorsWereEnabled = NeoDiagnostics.RuntimeErrorsEnabled;
            try
            {
                NeoDiagnostics.Configure(errors: true);
                LogAssert.Expect(LogType.Error, new Regex(@"\[PM\] PageId PageNowhere not found"));
                InvokeInstanceMethod(pm, "ActivePage", CreatePageId("PageNowhere"));
            }
            finally
            {
                NeoDiagnostics.Configure(errors: errorsWereEnabled);
            }

            Assert.AreSame(menuPage, GetPublicField(pm, "currentUiPage"));
        }

        [Test]
        public void PM_SetPage_ReopeningCurrentPage_KeepsHistoryForBack()
        {
            EnsurePagesModuleAvailable();

            object pm = CreatePageManager(out GameObject pmObject);
            object menuPage = CreateUiPage(pmObject, "PageMenu", false, false);
            object shopPage = CreateUiPage(pmObject, "PageShop", false, false);
            object shopId = GetPrivateField(shopPage, "pageId");
            InvokeInstanceMethod(pm, "SetPage", GetPrivateField(menuPage, "pageId"));
            InvokeInstanceMethod(pm, "SetPage", shopId);

            InvokeInstanceMethod(pm, "SetPage", shopId);
            Assert.AreSame(menuPage, GetPublicField(pm, "previousUiPage"),
                "Reopening the current page must not replace the history with itself.");

            InvokeInstanceMethod(pm, "SwitchToPreviousPage", false);

            Assert.AreSame(menuPage, GetPublicField(pm, "currentUiPage"));
            Assert.IsTrue(((Component)menuPage).gameObject.activeSelf, "Back must show the menu, not an empty screen.");
            Assert.IsFalse(((Component)shopPage).gameObject.activeSelf);
        }

        [Test]
        public void PM_CloseCurrentPage_AfterPopup_MakesUnderlyingPageCurrent()
        {
            EnsurePagesModuleAvailable();

            object pm = CreatePageManager(out GameObject pmObject);
            object gamePage = CreateUiPage(pmObject, "PageGame", false, false);
            object pausePage = CreateUiPage(pmObject, "PagePause", true, false);
            InvokeInstanceMethod(pm, "SetPage", GetPrivateField(gamePage, "pageId"));
            InvokeInstanceMethod(pm, "ActivePage", GetPrivateField(pausePage, "pageId"));
            Assert.AreSame(pausePage, GetPublicField(pm, "currentUiPage"));
            List<object> changes = ListenToPageChanges(pm);

            InvokeInstanceMethod(pm, "CloseCurrentPage");

            Assert.IsFalse(((Component)pausePage).gameObject.activeSelf);
            Assert.IsTrue(((Component)gamePage).gameObject.activeSelf);
            Assert.AreSame(gamePage, GetPublicField(pm, "currentUiPage"), "The page under the closed popup is current.");
            Assert.IsNull(GetPublicField(pm, "previousUiPage"));
            Assert.AreEqual(1, changes.Count);
            Assert.AreSame(gamePage, changes[0]);
        }

        [Test]
        public void PM_FindPage_IgnoresStaleCachedPage()
        {
            EnsurePagesModuleAvailable();

            object pm = CreatePageManager(out GameObject pmObject);
            object page = CreateUiPage(pmObject, "PageMenu", false, false);
            object oldId = GetPrivateField(page, "pageId");
            Assert.AreSame(page, InvokeInstanceMethod(pm, "FindPage", oldId));

            object newId = CreatePageId("PageTitle");
            SetPrivateField(page, "pageId", newId);

            Assert.IsNull(InvokeInstanceMethod(pm, "FindPage", oldId),
                "A cached page that now uses another PageId must not be returned for the old one.");
            Assert.AreSame(page, InvokeInstanceMethod(pm, "FindPage", newId));
        }

        [Test]
        public void PM_TryGetPageIdByName_MatchesAssetNameThenGameObjectName()
        {
            EnsurePagesModuleAvailable();

            object pm = CreatePageManager(out GameObject pmObject);
            object page = CreateUiPage(pmObject, "PageMenu", false, false);
            ((Component)page).gameObject.name = "Menu Screen";
            object menuId = GetPrivateField(page, "pageId");

            Assert.AreSame(menuId, TryGetPageIdByName(pm, "PageMenu", out bool byAsset));
            Assert.IsTrue(byAsset);
            Assert.AreSame(menuId, TryGetPageIdByName(pm, "Menu Screen", out bool byObject));
            Assert.IsTrue(byObject);
            Assert.IsNull(TryGetPageIdByName(pm, "PageMissing", out bool missing));
            Assert.IsFalse(missing);
        }

        [Test]
        public void PM_TryGetPageIdByName_FindsPageAddedAfterStartup()
        {
            EnsurePagesModuleAvailable();

            object pm = CreatePageManager(out GameObject pmObject);
            CreateUiPage(pmObject, "PageMenu", false, false);
            Assert.IsNotNull(TryGetPageIdByName(pm, "PageMenu", out _));

            object latePage = CreateUiPage(pmObject, "PageBonus", false, false);

            Assert.AreSame(GetPrivateField(latePage, "pageId"), TryGetPageIdByName(pm, "PageBonus", out bool found));
            Assert.IsTrue(found);
        }

        [Test]
        public void UIPage_HideAnimationDuration_ReturnsZero_WhenModeNone()
        {
            EnsurePagesModuleAvailable();

            GameObject pageObject = NewObject("UI Page");
            object page = pageObject.AddComponent(UIPageType);

            SetPrivateField(page, "_animationMode", Enum.Parse(UIPageAnimationModeType, "None"));

            float hideAnimationDuration = (float)GetPropertyValue(page, "HideAnimationDuration");
            Assert.AreEqual(0f, hideAnimationDuration, 0.0001f);
        }

        [Test]
        public void UIPage_WaitForHideAnimation_DoesNotWait_WhenNoHideAnimation()
        {
            EnsurePagesModuleAvailable();

            GameObject pageObject = NewObject("UI Page");
            object page = pageObject.AddComponent(UIPageType);

            IEnumerator hideRoutine = (IEnumerator)InvokeInstanceMethod(page, "WaitForHideAnimation");

            Assert.IsFalse(hideRoutine.MoveNext());
        }

        [Test]
        public void UIPage_AnimationTimingHooks_AreOverridable()
        {
            EnsurePagesModuleAvailable();

            foreach (string propertyName in new[] { "HasShowAnimation", "ShowAnimationDuration", "HideAnimationDuration" })
            {
                PropertyInfo property = UIPageType.GetProperty(propertyName, PublicInstanceBinding);
                Assert.IsNotNull(property, propertyName);
                MethodInfo getter = property.GetGetMethod();
                Assert.IsTrue(getter.IsVirtual && !getter.IsFinal,
                    propertyName + " must be virtual so animated pages can make PM wait for them.");
            }
        }

        [Test]
        public void PM_SwitchToPreviousPage_KeepsCurrentActive_WhenNoHistory()
        {
            EnsurePagesModuleAvailable();

            GameObject pmObject = NewObject("PM");
            object pm = pmObject.AddComponent(PMType);

            object mainPage = CreateUiPage(pmObject, "PageMenu", false, true);
            SetPrivateField(pm, "allPages", CreateArray(UIPageType, mainPage));
            SetPublicField(pm, "currentUiPage", mainPage);
            SetPublicField(pm, "previousUiPage", null);

            InvokeInstanceMethod(pm, "SwitchToPreviousPage", false);

            Assert.AreSame(mainPage, GetPublicField(pm, "currentUiPage"));
            Assert.IsTrue(((Component)mainPage).gameObject.activeSelf);
        }

        [Test]
        public void BtnChangePage_ChangePage_WithoutPageManager_DoesNotThrow()
        {
            EnsurePagesModuleAvailable();
            Type buttonType = RequireType("Neo.Pages.BtnChangePage");
            Assume.That(UnityEngine.Object.FindAnyObjectByType(PMType, FindObjectsInactive.Include), Is.Null,
                "The open scene already contains a PM.");
            ResetPageManagerSingleton();

            GameObject buttonObject = NewObject("Button");
            object button = buttonObject.AddComponent(buttonType);
            SetPrivateField(button, "targetPageId", CreatePageId("PageMenu"));

            Assert.DoesNotThrow(() => InvokeInstanceMethod(button, "ChangePage"),
                "A page button in a scene without PM must not throw a NullReferenceException.");
        }

        [Test]
        public void BtnChangePage_ChangePage_LegacyActionValueWithTarget_OpensTargetAndWarnsOnce()
        {
            EnsurePagesModuleAvailable();
            ResetPageManagerSingleton();

            object pm = CreatePageManager(out GameObject pmObject);
            object menuPage = CreateUiPage(pmObject, "PageMenu", false, false);
            object pausePage = CreateUiPage(pmObject, "PagePause", false, false);
            InvokeInstanceMethod(pm, "SetPage", GetPrivateField(menuPage, "pageId"));

            // WHY: 12 was UIKit.Page.Pause; such buttons opened their target page before the Action enum existed.
            object button = CreateLegacyActionButton(12, GetPrivateField(pausePage, "pageId"));
            Assert.IsTrue((bool)GetPropertyValue(button, "HasLegacyAction"));

            List<string> warnings = new();
            Application.LogCallback collect = (message, _, type) =>
            {
                if (type == LogType.Warning && message.Contains("legacy action value 12; it opens"))
                {
                    warnings.Add(message);
                }
            };

            Application.logMessageReceived += collect;
            try
            {
                InvokeInstanceMethod(button, "ChangePage");
                Assert.AreSame(pausePage, GetPublicField(pm, "currentUiPage"),
                    "A legacy action value must keep opening the Target PageId, as it always did.");
                Assert.IsTrue(((Component)pausePage).gameObject.activeSelf);

                InvokeInstanceMethod(pm, "SetPage", GetPrivateField(menuPage, "pageId"));
                InvokeInstanceMethod(button, "ChangePage");
                Assert.AreSame(pausePage, GetPublicField(pm, "currentUiPage"));
            }
            finally
            {
                Application.logMessageReceived -= collect;
            }

            Assert.AreEqual(1, warnings.Count, "The legacy value warns once per button, even with warnings off.");
        }

        [Test]
        public void BtnChangePage_ChangePage_LegacyActionValueWithoutTarget_DoesNotSwitch()
        {
            EnsurePagesModuleAvailable();
            ResetPageManagerSingleton();

            object pm = CreatePageManager(out GameObject pmObject);
            object menuPage = CreateUiPage(pmObject, "PageMenu", false, false);
            InvokeInstanceMethod(pm, "SetPage", GetPrivateField(menuPage, "pageId"));
            object button = CreateLegacyActionButton(3, null);

            Assert.DoesNotThrow(() => InvokeInstanceMethod(button, "ChangePage"));

            Assert.AreSame(menuPage, GetPublicField(pm, "currentUiPage"));
            Assert.IsTrue(((Component)menuPage).gameObject.activeSelf);
        }

        [Test]
        public void BtnChangePage_OnPointerClick_ReactsToThePrimaryButtonOnly()
        {
            EnsurePagesModuleAvailable();
            Type buttonType = RequireType("Neo.Pages.BtnChangePage");
            ResetPageManagerSingleton();

            object pm = CreatePageManager(out GameObject pmObject);
            object menuPage = CreateUiPage(pmObject, "PageMenu", false, false);
            object gamePage = CreateUiPage(pmObject, "PageGame", false, false);
            InvokeInstanceMethod(pm, "SetPage", GetPrivateField(menuPage, "pageId"));

            GameObject buttonObject = NewObject("Button");
            object button = buttonObject.AddComponent(buttonType);
            SetPrivateField(button, "targetPageId", GetPrivateField(gamePage, "pageId"));

            InvokeInstanceMethod(button, "OnPointerClick", Click(PointerEventData.InputButton.Right));
            InvokeInstanceMethod(button, "OnPointerClick", Click(PointerEventData.InputButton.Middle));
            Assert.AreSame(menuPage, GetPublicField(pm, "currentUiPage"), "Right and middle clicks must not switch pages.");

            InvokeInstanceMethod(button, "OnPointerClick", Click(PointerEventData.InputButton.Left));
            Assert.AreSame(gamePage, GetPublicField(pm, "currentUiPage"));
        }

        [Test]
        public void BtnChangePage_AddComponent_DoesNotCreatePageIdAssets()
        {
            EnsurePagesModuleAvailable();
            Type buttonType = RequireType("Neo.Pages.BtnChangePage");
            int pageIdAssetsBefore = AssetDatabase.FindAssets("t:PageId").Length;

            GameObject buttonObject = NewObject("Button");
            object button = buttonObject.AddComponent(buttonType);
            InvokePrivateMethod(button, "Reset");

            Assert.IsNull(GetPrivateField(button, "targetPageId"), "Reset used to create and assign a 'PageOpen' asset.");
            Assert.AreEqual(pageIdAssetsBefore, AssetDatabase.FindAssets("t:PageId").Length);
        }

        [Test]
        public void SamplePrefabs_PageButtons_UseDefinedActions()
        {
            EnsurePagesModuleAvailable();
            Type buttonType = RequireType("Neo.Pages.BtnChangePage");
            FieldInfo actionField = buttonType.GetField("action", PrivateInstanceBinding);
            Assert.IsNotNull(actionField);

            int checkedButtons = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("/NeoxiderPages/Prefabs/", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                foreach (Component button in prefab.GetComponentsInChildren(buttonType, true))
                {
                    object action = actionField.GetValue(button);
                    Assert.IsTrue(Enum.IsDefined(actionField.FieldType, action),
                        path + ": " + button.name + " has the legacy action value " + Convert.ToInt32(action) +
                        " (old UIKit.Page enum); give it a defined Action and a Target PageId.");
                    checkedButtons++;
                }
            }

            if (checkedButtons == 0)
            {
                Assert.Ignore("No NeoxiderPages prefabs are visible to the AssetDatabase.");
            }
        }

        [Test]
        public void FakeLoad_EndLoad_EmitsFullProgress()
        {
            Type fakeLoadType = GetTypeOrNull("Neo.Pages.FakeLoad");
            if (fakeLoadType == null)
            {
                Assert.Ignore("Neo.Pages.FakeLoad is not available in this test assembly.");
            }

            GameObject loadObject = NewObject("FakeLoad");
            object fakeLoad = loadObject.AddComponent(fakeLoadType);

            int percent = -1;
            float normalized = -1f;
            UnityAction<int> percentListener = value => percent = value;
            UnityAction<float> normalizedListener = value => normalized = value;

            UnityEventBase onPercent = (UnityEventBase)GetPublicField(fakeLoad, "OnChangePercent");
            UnityEventBase onChange = (UnityEventBase)GetPublicField(fakeLoad, "OnChange");
            onPercent.GetType().GetMethod("AddListener").Invoke(onPercent, new object[] { percentListener });
            onChange.GetType().GetMethod("AddListener").Invoke(onChange, new object[] { normalizedListener });

            InvokeInstanceMethod(fakeLoad, "EndLoad");

            Assert.AreEqual(100, percent);
            Assert.AreEqual(1f, normalized, 0.0001f);
        }

        [Test]
        public void FakeLoad_Load_InOneShotModeAfterFirstRun_StillFinishes()
        {
            Type fakeLoadType = GetTypeOrNull("Neo.Pages.FakeLoad");
            if (fakeLoadType == null)
            {
                Assert.Ignore("Neo.Pages.FakeLoad is not available in this test assembly.");
            }

            FieldInfo initialized = fakeLoadType.GetField("isInitialized", PrivateStaticBinding);
            Assert.IsNotNull(initialized);
            object previous = initialized.GetValue(null);
            try
            {
                initialized.SetValue(null, true);
                GameObject loadObject = NewObject("FakeLoad");
                object fakeLoad = loadObject.AddComponent(fakeLoadType);
                int finished = 0;
                UnityEvent onFinish = (UnityEvent)GetPublicField(fakeLoad, "OnFinisLoad");
                onFinish.AddListener(() => finished++);

                InvokeInstanceMethod(fakeLoad, "Load");

                Assert.AreEqual(1, finished,
                    "A skipped one-shot load must still finish; otherwise the loading page never closes.");
            }
            finally
            {
                initialized.SetValue(null, previous);
            }
        }

        [Test]
        public void PageSubscriber_ResolvePageIds_UsesThePageManagerPages()
        {
            EnsurePagesModuleAvailable();
            Type subscriberType = RequireType("Neo.Pages.PageSubscriber");

            object pm = CreatePageManager(out GameObject pmObject);
            object gamePage = CreateUiPage(pmObject, "PageGame", false, false);
            object winPage = CreateUiPage(pmObject, "PageWin", true, false);

            GameObject subscriberObject = NewObject("PageSubscriber");
            object subscriber = subscriberObject.AddComponent(subscriberType);
            SetPrivateField(subscriber, "_pm", pm);
            InvokePrivateMethod(subscriber, "ResolvePageIds");

            Assert.AreSame(GetPrivateField(gamePage, "pageId"), GetPrivateField(subscriber, "gamePageId"));
            Assert.AreSame(GetPrivateField(winPage, "pageId"), GetPrivateField(subscriber, "winPageId"));
            Assert.IsNull(GetPrivateField(subscriber, "losePageId"), "No PageLose page exists under the PM.");
        }

        [Test]
        public void PageSubscriber_GameEvents_WithGmButNoEm_ReadAsMissingWithoutThrowing()
        {
            EnsurePagesModuleAvailable();
            Type subscriberType = RequireType("Neo.Pages.PageSubscriber");
            Assume.That(UnityEngine.Object.FindAnyObjectByType<EM>(FindObjectsInactive.Include), Is.Null,
                "The open scene already contains an EM.");
            ResetSingleton(typeof(GM));
            ResetSingleton(typeof(EM));
            int frameRate = Application.targetFrameRate;
            try
            {
                NewObject("GM").AddComponent<GM>();
                MethodInfo missing = subscriberType.GetMethod("GameEventsMissing", PrivateStaticBinding);
                Assert.IsNotNull(missing);

                bool eventsMissing = true;
                Assert.DoesNotThrow(() => eventsMissing = (bool)missing.Invoke(null, null),
                    "GM without EM used to throw a NullReferenceException.");
                Assert.IsTrue(eventsMissing);

                GameObject subscriberObject = NewObject("PageSubscriber");
                object subscriber = subscriberObject.AddComponent(subscriberType);
                Assert.IsFalse((bool)GetPropertyValue(subscriber, "IsSubscribed"));
            }
            finally
            {
                Application.targetFrameRate = frameRate;
                ResetSingleton(typeof(GM));
                ResetSingleton(typeof(EM));
            }
        }

        [Test]
        public void PageSubscriber_OnDestroy_RemovesItsListenersFromTheGameEvents()
        {
            EnsurePagesModuleAvailable();
            Type subscriberType = RequireType("Neo.Pages.PageSubscriber");
            Assume.That(UnityEngine.Object.FindAnyObjectByType<EM>(FindObjectsInactive.Include), Is.Null,
                "The open scene already contains an EM.");
            ResetSingleton(typeof(EM));
            try
            {
                EM em = NewObject("EM").AddComponent<EM>();
                em.OnGameStart ??= new UnityEvent();
                em.OnRestart ??= new UnityEvent();
                em.OnWin ??= new UnityEvent();
                em.OnLose ??= new UnityEvent();
                em.OnEnd ??= new UnityEvent();

                object pm = CreatePageManager(out GameObject pmObject);
                CreateUiPage(pmObject, "PageGame", false, false);
                object winPage = CreateUiPage(pmObject, "PageWin", true, false);
                GameObject subscriberObject = NewObject("PageSubscriber");
                object subscriber = subscriberObject.AddComponent(subscriberType);
                SetPrivateField(subscriber, "_pm", pm);

                InvokePrivateMethod(subscriber, "Init");
                Assert.IsTrue((bool)GetPropertyValue(subscriber, "IsSubscribed"));
                em.OnWin.Invoke();
                Assert.IsTrue(((Component)winPage).gameObject.activeSelf, "The subscriber opens PageWin on EM.OnWin.");

                ((Component)winPage).gameObject.SetActive(false);
                InvokePrivateMethod(subscriber, "OnDestroy");
                Assert.IsFalse((bool)GetPropertyValue(subscriber, "IsSubscribed"));
                em.OnWin.Invoke();

                Assert.IsFalse(((Component)winPage).gameObject.activeSelf,
                    "A destroyed subscriber must not stay on a persistent EM and keep switching pages.");
            }
            finally
            {
                ResetSingleton(typeof(EM));
            }
        }

        [Test]
        public void ToggleAudio_WithoutAMSettingsOrToggle_DoesNotThrow()
        {
            EnsurePagesModuleAvailable();
            Type toggleType = RequireType("Neo.Pages.ToggleAudio");
            Assume.That(UnityEngine.Object.FindAnyObjectByType<AMSettings>(FindObjectsInactive.Include), Is.Null,
                "The open scene already contains an AMSettings.");
            ResetSingleton(typeof(AMSettings));

            GameObject toggleObject = NewObject("ToggleAudio");
            object toggle = toggleObject.AddComponent(toggleType);

            Assert.DoesNotThrow(() => InvokeInstanceMethod(toggle, "SyncFromSettings"));
            MethodInfo apply = toggleType.GetMethod("ApplyToSettings", PrivateInstanceBinding);
            Assert.IsNotNull(apply);
            Assert.DoesNotThrow(() => apply.Invoke(toggle, new object[] { false }));
        }

        [Test]
        public void AudioFacade_WithoutAMSettings_ReadsOnAndIgnoresWrites()
        {
            EnsurePagesModuleAvailable();
            Type audioType = RequireType("Neo.Pages.Audio");
            Assume.That(UnityEngine.Object.FindAnyObjectByType<AMSettings>(FindObjectsInactive.Include), Is.Null,
                "The open scene already contains an AMSettings.");
            Assume.That(UnityEngine.Object.FindAnyObjectByType<AM>(FindObjectsInactive.Include), Is.Null,
                "The open scene already contains an AM.");
            ResetSingleton(typeof(AMSettings));
            ResetSingleton(typeof(AM));

            Assert.IsFalse((bool)GetStaticPropertyValue(audioType, "IsAvailable"));
            Assert.IsTrue((bool)GetStaticPropertyValue(audioType, "IsActiveSound"));
            Assert.IsTrue((bool)GetStaticPropertyValue(audioType, "IsActiveMusic"));
            Assert.DoesNotThrow(() => audioType.GetProperty("IsActiveSound").SetValue(null, false));
            Assert.DoesNotThrow(() => audioType.GetProperty("IsActiveMusic").SetValue(null, false));
            Assert.DoesNotThrow(() => audioType.GetMethod("PlayUI").Invoke(null, null));
        }

        [Test]
        public void ManagerFacades_WithoutTheirManagers_ReturnDefaults()
        {
            EnsurePagesModuleAvailable();
            Assume.That(UnityEngine.Object.FindAnyObjectByType<Money>(FindObjectsInactive.Include), Is.Null,
                "The open scene already contains a Money.");
            Assume.That(UnityEngine.Object.FindAnyObjectByType<ScoreManager>(FindObjectsInactive.Include), Is.Null,
                "The open scene already contains a ScoreManager.");
            Assume.That(UnityEngine.Object.FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include), Is.Null,
                "The open scene already contains a LevelManager.");
            Assume.That(UnityEngine.Object.FindAnyObjectByType<EM>(FindObjectsInactive.Include), Is.Null,
                "The open scene already contains an EM.");

            Type walletType = RequireType("Neo.Pages.Wallet");
            Type scoreType = RequireType("Neo.Pages.Score");
            Type levelType = RequireType("Neo.Pages.Level");
            Type gameType = RequireType("Neo.Pages.G");

            Assert.AreEqual(0f, (float)GetStaticPropertyValue(walletType, "Value"));
            Assert.AreEqual(0f, (float)GetStaticPropertyValue(walletType, "LevelValue"));
            Assert.IsNull(GetStaticPropertyValue(walletType, "OnValueChange"));
            Assert.AreEqual(0, (int)GetStaticPropertyValue(scoreType, "Current"));
            Assert.AreEqual(0, (int)GetStaticPropertyValue(scoreType, "Best"));
            Assert.IsNull(GetStaticPropertyValue(scoreType, "OnCurrentChange"));
            Assert.AreEqual(0, (int)GetStaticPropertyValue(levelType, "Current"));
            Assert.AreEqual(0, (int)GetStaticPropertyValue(levelType, "Best"));
            Assert.IsNull(GetStaticPropertyValue(levelType, "OnBestChange"));
            Assert.IsNull(GetStaticPropertyValue(gameType, "OnWin"));
        }

        [Test]
        public void PageIdGenerator_ToAssetName_PrefixesPageOnce()
        {
            Type generatorType = GetTypeOrNull("Neo.Pages.Editor.PageIdGenerator");
            if (generatorType == null)
            {
                Assert.Ignore("Neo.Pages.Editor.PageIdGenerator is not available in this test assembly.");
            }

            MethodInfo toAssetName = generatorType.GetMethod("ToAssetName", BindingFlags.Static | BindingFlags.Public);
            MethodInfo getAssetPath = generatorType.GetMethod("GetAssetPath", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(toAssetName);
            Assert.IsNotNull(getAssetPath);

            Assert.AreEqual("PageMenu", toAssetName.Invoke(null, new object[] { "Menu" }));
            Assert.AreEqual("PageGame", toAssetName.Invoke(null, new object[] { "PageGame" }));
            Assert.AreEqual("PageShop", toAssetName.Invoke(null, new object[] { "  Shop " }));
            Assert.AreEqual(string.Empty, toAssetName.Invoke(null, new object[] { " " }));
            Assert.AreEqual("Assets/Pages/PageMenu.asset", getAssetPath.Invoke(null, new object[] { "Menu", "Assets/Pages" }));
        }

        [Test]
        public void PagesEditorGUI_UnlistedPageId_IsNotShownAsNone()
        {
            EnsurePagesModuleAvailable();
            Type editorGuiType = GetTypeOrNull("Neo.Pages.Editor.PagesEditorGUI");
            if (editorGuiType == null)
            {
                Assert.Ignore("Neo.Pages.Editor.PagesEditorGUI is not available in this test assembly.");
            }

            MethodInfo tryGetIndex = editorGuiType.GetMethod("TryGetPopupIndex", PrivateStaticBinding);
            Assert.IsNotNull(tryGetIndex);

            GameObject pageObject = NewObject("Page");
            Component page = pageObject.AddComponent(UIPageType);
            SerializedObject serialized = new(page);
            SerializedProperty pageIdProperty = serialized.FindProperty("pageId");
            Assert.IsNotNull(pageIdProperty);

            object[] args = { pageIdProperty, null };
            Assert.IsTrue((bool)tryGetIndex.Invoke(null, args), "An empty field maps to <None>.");
            Assert.AreEqual(0, (int)args[1]);

            // WHY: a PageId that is not an asset is never in the project-wide list, like one in an unscanned folder.
            pageIdProperty.objectReferenceValue = (UnityEngine.Object)CreatePageId("PageUnlisted");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            args[1] = null;
            Assert.IsFalse((bool)tryGetIndex.Invoke(null, args),
                "An unlisted PageId must keep its own field instead of reading as <None> and being cleared.");
        }

        [UnityTest]
        public IEnumerator FakeLoad_OneShotLoadOnInactiveStartupPage_OpensTheNextPage()
        {
            EnsurePagesModuleAvailable();
            RequireType("Neo.Pages.FakeLoad");
            yield return new EnterPlayMode();
            // WHY: entering Play Mode may reload the domain, which clears the resolved type cache.
            EnsurePagesModuleAvailable();

            OneShotStartupLoad scene = new(false);
            try
            {
                for (int frame = 0; frame < 4; frame++)
                {
                    yield return null;
                }

                scene.AssertNextPageOpened();
            }
            finally
            {
                scene.Dispose();
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator FakeLoad_OneShotLoadOnActiveStartupPage_SurvivesPmStartAndOpensTheNextPage()
        {
            EnsurePagesModuleAvailable();
            RequireType("Neo.Pages.FakeLoad");
            yield return new EnterPlayMode();
            EnsurePagesModuleAvailable();

            OneShotStartupLoad scene = new(true);
            try
            {
                for (int frame = 0; frame < 4; frame++)
                {
                    yield return null;
                }

                scene.AssertNextPageOpened();
            }
            finally
            {
                scene.Dispose();
            }

            yield return new ExitPlayMode();
        }

        [Test]
        public void UIKit_StaticReset_DropsListenersOfThePreviousPlaySession()
        {
            EnsurePagesModuleAvailable();
            Type uiKitType = RequireType("Neo.Pages.UIKit");
            FieldInfo showPageByNameField = uiKitType.GetField("OnShowPageByName", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(showPageByNameField);
            UnityEvent<string> showPageByName = (UnityEvent<string>)showPageByNameField.GetValue(null);
            int calls = 0;
            showPageByName.AddListener(_ => calls++);

            MethodInfo reset = uiKitType.GetMethod("ResetStaticState", PrivateStaticBinding);
            Assert.IsNotNull(reset);
            reset.Invoke(null, null);
            uiKitType.GetMethod("ShowPage", new[] { typeof(string) }).Invoke(null, new object[] { "PageMenu" });

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void PM_OnValidate_DefersEditorPreviewToDelayCall()
        {
            EnsurePagesModuleAvailable();

            GameObject pmObject = NewObject("PM");
            object pm = null;
            object menuPage = null;
            object gamePage = null;
            try
            {
                pm = pmObject.AddComponent(PMType);
                menuPage = CreateUiPage(pmObject, "PageMenu", false, true);
                gamePage = CreateUiPage(pmObject, "PageGame", false, false);
                ConfigureEditorPreview(pm, GetPrivateField(gamePage, "pageId"));
                CancelEditorPreview(pm);
                Assert.AreEqual(0, CountScheduledEditorPreviews(pm));

                InvokePrivateMethod(pm, "OnValidate");

                Assert.IsTrue(((Component)menuPage).gameObject.activeSelf,
                    "OnValidate must not toggle pages synchronously.");
                Assert.IsFalse(((Component)gamePage).gameObject.activeSelf,
                    "OnValidate must not toggle pages synchronously.");
                Assert.AreEqual(1, CountScheduledEditorPreviews(pm),
                    "OnValidate must defer the preview to EditorApplication.delayCall.");

                InvokePrivateMethod(pm, "OnValidate");
                Assert.AreEqual(1, CountScheduledEditorPreviews(pm), "Repeated OnValidate must not stack refreshes.");

                InvokePrivateMethod(pm, "RefreshEditorPreview");

                Assert.IsTrue(((Component)gamePage).gameObject.activeSelf, "The deferred refresh shows the preview page.");
                Assert.IsFalse(((Component)menuPage).gameObject.activeSelf, "The deferred refresh hides the other pages.");
            }
            finally
            {
                CancelEditorPreview(pm);
                DestroyPageIds(menuPage, gamePage);
                UnityEngine.Object.DestroyImmediate(pmObject);
            }
        }

        [Test]
        public void PM_DeferredEditorPreview_IsNoOp_WhenManagerDestroyed()
        {
            EnsurePagesModuleAvailable();

            GameObject pmObject = NewObject("PM");
            object pm = null;
            object menuPage = null;
            object gamePage = null;
            try
            {
                pm = pmObject.AddComponent(PMType);
                menuPage = CreateUiPage(pmObject, "PageMenu", false, true);
                gamePage = CreateUiPage(pmObject, "PageGame", false, false);
                ConfigureEditorPreview(pm, GetPrivateField(gamePage, "pageId"));
                CancelEditorPreview(pm);
                InvokePrivateMethod(pm, "OnValidate");
                EditorApplication.CallbackFunction pending = CreateEditorPreviewCallback(pm);

                UnityEngine.Object.DestroyImmediate((Component)pm);

                Assert.DoesNotThrow(() => pending(), "A delayCall that outlives its PM must be a no-op.");
                Assert.IsTrue(((Component)menuPage).gameObject.activeSelf);
                Assert.IsFalse(((Component)gamePage).gameObject.activeSelf);
            }
            finally
            {
                CancelEditorPreview(pm);
                DestroyPageIds(menuPage, gamePage);
                UnityEngine.Object.DestroyImmediate(pmObject);
            }
        }

        [UnityTest]
        public IEnumerator PM_OnValidate_InPlayMode_NeitherSchedulesNorTogglesPages()
        {
            EnsurePagesModuleAvailable();
            yield return new EnterPlayMode();
            // WHY: entering Play Mode may reload the domain, which clears the resolved type cache.
            EnsurePagesModuleAvailable();

            GameObject pmObject = new("PM");
            object pm = null;
            object menuPage = null;
            object gamePage = null;
            try
            {
                pm = pmObject.AddComponent(PMType);
                menuPage = CreateUiPage(pmObject, "PageMenu", false, true);
                gamePage = CreateUiPage(pmObject, "PageGame", false, false);
                ConfigureEditorPreview(pm, GetPrivateField(gamePage, "pageId"));
                CancelEditorPreview(pm);

                InvokePrivateMethod(pm, "OnValidate");
                Assert.AreEqual(0, CountScheduledEditorPreviews(pm), "Play Mode must not schedule the editor preview.");

                InvokePrivateMethod(pm, "RefreshEditorPreview");
                Assert.IsTrue(((Component)menuPage).gameObject.activeSelf, "Play Mode pages belong to the game.");
                Assert.IsFalse(((Component)gamePage).gameObject.activeSelf, "Play Mode pages belong to the game.");
            }
            finally
            {
                CancelEditorPreview(pm);
                DestroyPageIds(menuPage, gamePage);
                UnityEngine.Object.DestroyImmediate(pmObject);
            }

            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator TearDownPlayMode()
        {
            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }
        }

        private static GameObject NewObject(string name)
        {
            GameObject created = new(name);
            Created.Add(created);
            return created;
        }

        private static object CreatePageManager(out GameObject pmObject)
        {
            pmObject = NewObject("PM");
            object pm = pmObject.AddComponent(PMType);
            SetPrivateField(pm, "ignoredPageIds", CreateArray(PageIdType));
            SetPrivateField(pm, "allPages", CreateArray(UIPageType));
            return pm;
        }

        private static object CreatePageId(string name)
        {
            ScriptableObject pageId = ScriptableObject.CreateInstance(PageIdType);
            pageId.name = name;
            Created.Add(pageId);
            return pageId;
        }

        private static List<object> ListenToPageChanges(object pm)
        {
            List<object> changes = new();
            FieldInfo eventField = PMType.GetField("OnPageChanged", PublicInstanceBinding);
            Assert.IsNotNull(eventField);
            UnityEventBase onPageChanged = (UnityEventBase)eventField.GetValue(pm);
            if (onPageChanged == null)
            {
                onPageChanged = (UnityEventBase)Activator.CreateInstance(eventField.FieldType);
                eventField.SetValue(pm, onPageChanged);
            }

            // WHY: UnityAction<UIPage> cannot be named without referencing the sample; List<object>.Add(object)
            // binds to it through parameter contravariance.
            Type actionType = typeof(UnityAction<>).MakeGenericType(UIPageType);
            MethodInfo addMethod = typeof(List<object>).GetMethod(nameof(List<object>.Add));
            Delegate listener = Delegate.CreateDelegate(actionType, changes, addMethod);
            onPageChanged.GetType().GetMethod("AddListener").Invoke(onPageChanged, new object[] { listener });
            return changes;
        }

        private static object TryGetPageIdByName(object pm, string name, out bool found)
        {
            MethodInfo method = PMType.GetMethod("TryGetPageIdByName", PublicInstanceBinding);
            Assert.IsNotNull(method, "PM.TryGetPageIdByName must exist.");
            object[] args = { name, null };
            found = (bool)method.Invoke(pm, args);
            return args[1];
        }

        private static void ResetPageManagerSingleton()
        {
            if (PMType == null)
            {
                return;
            }

            // WHY: Singleton<PM> remembers a failed scene search; without a reset PM.I keeps returning null even
            // after a later test adds a PM.
            ResetSingleton(PMType);
        }

        private static void ResetSingleton(Type singletonComponentType)
        {
            Type singletonType = typeof(Singleton<>).MakeGenericType(singletonComponentType);
            MethodInfo reset = singletonType.GetMethod("ResetStaticStateForRuntime", PrivateStaticBinding);
            reset?.Invoke(null, null);
        }

        private static object GetStaticPropertyValue(Type type, string propertyName)
        {
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(property, $"Property '{propertyName}' should exist on {type.Name}");
            return property.GetValue(null);
        }

        private static PointerEventData Click(PointerEventData.InputButton button)
        {
            return new PointerEventData(null) { button = button };
        }

        private static object CreateLegacyActionButton(int actionValue, object targetPageId)
        {
            Type buttonType = RequireType("Neo.Pages.BtnChangePage");
            GameObject buttonObject = NewObject("Legacy Button");
            object button = buttonObject.AddComponent(buttonType);
            FieldInfo actionField = buttonType.GetField("action", PrivateInstanceBinding);
            Assert.IsNotNull(actionField);
            actionField.SetValue(button, Enum.ToObject(actionField.FieldType, actionValue));
            SetPrivateField(button, "targetPageId", targetPageId);
            return button;
        }

        /// <summary>
        ///     Play Mode setup of a loading screen: a PM whose startup page "PageLoad" carries a FakeLoad that already
        ///     ran this session (one-shot mode), with On Finis Load opening "PageMenu". The PM starts inactive, so its
        ///     pages wake up together when it is activated, as in a loaded scene.
        /// </summary>
        private sealed class OneShotStartupLoad : IDisposable
        {
            private readonly Component _fakeLoad;
            private readonly FieldInfo _initializedField;
            private readonly Component _loadingPage;
            private readonly Component _menuPage;
            private readonly object _pm;
            private readonly GameObject _pmObject;
            private readonly object _previousInitialized;
            private int _finished;

            public OneShotStartupLoad(bool loadingPageActive)
            {
                Type fakeLoadType = RequireType("Neo.Pages.FakeLoad");
                _initializedField = fakeLoadType.GetField("isInitialized", PrivateStaticBinding);
                Assert.IsNotNull(_initializedField);
                _previousInitialized = _initializedField.GetValue(null);
                _initializedField.SetValue(null, true);
                ResetPageManagerSingleton();

                _pmObject = new GameObject("PM");
                _pmObject.SetActive(false);
                _pm = _pmObject.AddComponent(PMType);
                SetPrivateField(_pm, "integrateWithGM", false);
                SetPrivateField(_pm, "refreshPagesInEditor", false);
                SetPrivateField(_pm, "ignoredPageIds", CreateArray(PageIdType));
                _loadingPage = (Component)CreateUiPage(_pmObject, "PageLoad", false, loadingPageActive);
                _menuPage = (Component)CreateUiPage(_pmObject, "PageMenu", false, false);
                SetPrivateField(_pm, "startupPage", GetPrivateField(_loadingPage, "pageId"));

                _fakeLoad = _loadingPage.gameObject.AddComponent(fakeLoadType);
                UnityEvent onFinish = (UnityEvent)GetPublicField(_fakeLoad, "OnFinisLoad");
                object menuId = GetPrivateField(_menuPage, "pageId");
                onFinish.AddListener(() =>
                {
                    _finished++;
                    InvokeInstanceMethod(_pm, "ChangePage", menuId);
                });

                _pmObject.SetActive(true);
            }

            public void AssertNextPageOpened()
            {
                Assert.AreEqual(1, _finished, "On Finis Load must fire once.");
                Assert.AreSame(_menuPage, GetPublicField(_pm, "currentUiPage"),
                    "The page opened from On Finis Load must stay current; PM's startup switch used to undo it.");
                Assert.IsTrue(_menuPage.gameObject.activeSelf, "The next page must be visible.");
                Assert.IsFalse(_loadingPage.gameObject.activeSelf, "The loading page must close.");
                Assert.IsFalse((bool)GetPropertyValue(_fakeLoad, "IsLoading"));
            }

            public void Dispose()
            {
                _initializedField.SetValue(null, _previousInitialized);
                DestroyPageIds(_loadingPage, _menuPage);
                if (_pmObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(_pmObject);
                }

                ResetPageManagerSingleton();
            }
        }

        private static void ConfigureEditorPreview(object pm, object previewPageId)
        {
            SetPrivateField(pm, "refreshPagesInEditor", true);
            SetPrivateField(pm, "autoSelectEditorPage", false);
            SetPrivateField(pm, "editorActivePageId", previewPageId);
        }

        private static EditorApplication.CallbackFunction CreateEditorPreviewCallback(object pm)
        {
            MethodInfo method = pm.GetType().GetMethod("RefreshEditorPreview", PrivateInstanceBinding);
            Assert.IsNotNull(method, "PM must keep its deferred RefreshEditorPreview.");
            return (EditorApplication.CallbackFunction)Delegate.CreateDelegate(
                typeof(EditorApplication.CallbackFunction), pm, method);
        }

        private static int CountScheduledEditorPreviews(object pm)
        {
            EditorApplication.CallbackFunction pending = EditorApplication.delayCall;
            if (pending == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Delegate callback in pending.GetInvocationList())
            {
                if (ReferenceEquals(callback.Target, pm) && callback.Method.Name == "RefreshEditorPreview")
                {
                    count++;
                }
            }

            return count;
        }

        private static void CancelEditorPreview(object pm)
        {
            if (pm == null)
            {
                return;
            }

            EditorApplication.delayCall -= CreateEditorPreviewCallback(pm);
        }

        private static void DestroyPageIds(params object[] pages)
        {
            foreach (object page in pages)
            {
                if (page is Component component && component != null &&
                    GetPrivateField(page, "pageId") is ScriptableObject pageId && pageId != null)
                {
                    UnityEngine.Object.DestroyImmediate(pageId);
                }
            }
        }

        private static object InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstanceBinding);
            Assert.IsNotNull(method, $"Method '{methodName}' should exist on {target.GetType().Name}");
            return method.Invoke(target, null);
        }

        public static object CreateUiPage(GameObject parent, string pageIdName, bool popup, bool active)
        {
            GameObject pageObject = new(pageIdName);
            pageObject.transform.SetParent(parent.transform);
            object page = pageObject.AddComponent(UIPageType);
            ScriptableObject pageId = ScriptableObject.CreateInstance(PageIdType);
            pageId.name = pageIdName;
            Created.Add(pageId);

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

        private static void SetPublicField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PublicInstanceBinding);
            Assert.IsNotNull(field, $"Field '{fieldName}' should exist on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static object GetPublicField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PublicInstanceBinding);
            Assert.IsNotNull(field, $"Field '{fieldName}' should exist on {target.GetType().Name}");
            return field.GetValue(target);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstanceBinding);
            Assert.IsNotNull(field, $"Field '{fieldName}' should exist on {target.GetType().Name}");
            return field.GetValue(target);
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, PublicInstanceBinding);
            Assert.IsNotNull(property, $"Property '{propertyName}' should exist on {target.GetType().Name}");
            return property.GetValue(target);
        }

        private static object InvokeInstanceMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = FindPublicMethod(target.GetType(), methodName, args);
            Assert.IsNotNull(method, $"Method '{methodName}' should exist on {target.GetType().Name}");
            return method.Invoke(target, args);
        }

        private static MethodInfo FindPublicMethod(Type type, string methodName, object[] args)
        {
            // WHY: PM.ChangePage is overloaded (PageId / KnownPage); pick the overload whose parameters accept the
            // given arguments.
            foreach (MethodInfo method in type.GetMethods(PublicInstanceBinding))
            {
                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                bool matches = true;
                for (int i = 0; i < parameters.Length && matches; i++)
                {
                    matches = args[i] == null
                        ? !parameters[i].ParameterType.IsValueType
                        : parameters[i].ParameterType.IsInstanceOfType(args[i]);
                }

                if (matches)
                {
                    return method;
                }
            }

            return null;
        }

        private static object CreateArray(Type elementType, params object[] values)
        {
            Array array = Array.CreateInstance(elementType, values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                array.SetValue(values[i], i);
            }

            return array;
        }

        private static Type RequireType(string typeName)
        {
            Type type = GetTypeOrNull(typeName);
            if (type == null)
            {
                Assert.Ignore(typeName + " is not available in this test assembly.");
            }

            return type;
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
            // WHY: resolve by assembly name: AppDomain.GetAssemblies() can hand back unloaded assemblies (UAC0005).
            foreach (UnityEditor.Compilation.Assembly assembly in UnityEditor.Compilation.CompilationPipeline.GetAssemblies())
            {
                Type type = Type.GetType(typeName + ", " + assembly.name);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
