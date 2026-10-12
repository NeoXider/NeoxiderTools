using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

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

        [Test]
        public void PM_SwitchToPreviousPage_KeepsCurrentActive_WhenNoHistory()
        {
            EnsurePagesModuleAvailable();

            GameObject pmObject = new("PM");
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
        public void FakeLoad_EndLoad_EmitsFullProgress()
        {
            Type fakeLoadType = GetTypeOrNull("Neo.Pages.FakeLoad");
            if (fakeLoadType == null)
            {
                Assert.Ignore("Neo.Pages.FakeLoad is not available in this test assembly.");
            }

            GameObject loadObject = new("FakeLoad");
            object fakeLoad = loadObject.AddComponent(fakeLoadType);

            int percent = -1;
            float normalized = -1f;
            UnityAction<int> percentListener = value => percent = value;
            UnityAction<float> normalizedListener = value => normalized = value;

            var onPercent = (UnityEventBase)GetPublicField(fakeLoad, "OnChangePercent");
            var onChange = (UnityEventBase)GetPublicField(fakeLoad, "OnChange");
            onPercent.GetType().GetMethod("AddListener").Invoke(onPercent, new object[] { percentListener });
            onChange.GetType().GetMethod("AddListener").Invoke(onChange, new object[] { normalizedListener });

            InvokeInstanceMethod(fakeLoad, "EndLoad");

            Assert.AreEqual(100, percent);
            Assert.AreEqual(1f, normalized, 0.0001f);
        }

        [Test]
        public void PM_OnValidate_DefersEditorPreviewToDelayCall()
        {
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
                UnityEditor.EditorApplication.CallbackFunction pending = CreateEditorPreviewCallback(pm);

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

        private static void ConfigureEditorPreview(object pm, object previewPageId)
        {
            SetPrivateField(pm, "refreshPagesInEditor", true);
            SetPrivateField(pm, "autoSelectEditorPage", false);
            SetPrivateField(pm, "editorActivePageId", previewPageId);
        }

        private static UnityEditor.EditorApplication.CallbackFunction CreateEditorPreviewCallback(object pm)
        {
            MethodInfo method = pm.GetType().GetMethod("RefreshEditorPreview", PrivateInstanceBinding);
            Assert.IsNotNull(method, "PM must keep its deferred RefreshEditorPreview.");
            return (UnityEditor.EditorApplication.CallbackFunction)Delegate.CreateDelegate(
                typeof(UnityEditor.EditorApplication.CallbackFunction), pm, method);
        }

        private static int CountScheduledEditorPreviews(object pm)
        {
            UnityEditor.EditorApplication.CallbackFunction pending = UnityEditor.EditorApplication.delayCall;
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

            UnityEditor.EditorApplication.delayCall -= CreateEditorPreviewCallback(pm);
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

        private static void SetPublicField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(field, $"Field '{fieldName}' should exist on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static object GetPublicField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
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
            Array array = Array.CreateInstance(elementType, values.Length);
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
            // WHY: resolve by assembly name: AppDomain.GetAssemblies() can hand back unloaded assemblies (UAC0005).
            return UnityEditor.Compilation.CompilationPipeline.GetAssemblies()
                .Select(assembly => Type.GetType(typeName + ", " + assembly.name))
                .FirstOrDefault(type => type != null);
        }
    }
}
