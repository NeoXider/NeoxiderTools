using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Neo.HtmlToUitk.Actions;
using Neo.HtmlToUitk.Responsive;
using Neo.HtmlToUitk.Animation;
using Neo.HtmlToUitk.Navigation;
using Neo.HtmlToUitk.Tooling;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class RuntimeOwnershipTests
    {
        private bool _enteredPlayMode;
        private sealed class HostWindow : EditorWindow { }
        private sealed class InvocationCounter
        {
            public int Calls { get; private set; }
            public void Increment() { Calls++; }
        }
        private const string Navigation = "{\"schema\":1,\"start\":\"home\",\"pages\":[{\"id\":\"home\",\"element\":\"home\",\"focus\":\"home-focus\"},{\"id\":\"next\",\"element\":\"next\",\"focus\":\"next-focus\"}],\"edges\":[],\"modals\":[],\"tabs\":[]}";

        private sealed class DeferredBackend : IPageBackend
        {
            public sealed class Handle : IPageTransitionHandle
            {
                private readonly Action _done;
                public Handle(Action done) { _done = done; }
                public bool IsDone { get; private set; }
                public void Complete() { if (IsDone) return; IsDone = true; _done(); }
            }
            public readonly List<Handle> Handles = new List<Handle>();
            public readonly List<string> Focused = new List<string>();
            public string FocusedElementName => null;
            public void Bind(NavSpec spec) { }
            public VisualElement FindElement(string name) => null;
            public IPageTransitionHandle ShowPage(NavStep step, Action done) => Add(done);
            public IPageTransitionHandle HidePage(NavStep step, Action done) => Add(done);
            public IPageTransitionHandle OpenModal(NavStep step, Action done) => Add(done);
            public IPageTransitionHandle CloseModal(NavStep step, Action done) => Add(done);
            public void SelectTab(NavStep step) { }
            public bool Focus(string name) { Focused.Add(name); return true; }
            public bool FocusModal(string name) { Focused.Add(name); return true; }
            private Handle Add(Action done) { Handle handle = new Handle(done); Handles.Add(handle); return handle; }
        }

        private static void Submit(Button button)
        {
            using (NavigationSubmitEvent evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }

        [UnityTest]
        public IEnumerator ConfiguringDisabledScreenAndWatcherDoesNotBindUntilEnable()
        {
            yield return new EnterPlayMode();
            _enteredPlayMode = true;
            GameObject panelHost = null;
            GameObject host = null;
            PanelSettings panelSettings = null;
            RenderTexture target = null;
            TextAsset media = null;
            IDisposable action = null;
            InvocationCounter counter = new InvocationCounter();
            try
            {
                panelHost = new GameObject("HtmlToUitk-lifecycle-panel"); panelHost.SetActive(false);
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                target = new RenderTexture(1280, 720, 0); target.Create();
                panelSettings.targetTexture = target;
                UIDocument document = panelHost.AddComponent<UIDocument>();
                document.panelSettings = panelSettings;
                panelHost.SetActive(true);
                yield return null;
                VisualElement root = document.rootVisualElement;
                Assert.That(root, Is.Not.Null, "Lifecycle gate requires a real runtime UIDocument root.");
                Assert.That(root.panel, Is.Not.Null, "UIDocument must remain attached while the screen is disabled.");
                root.name = "app";
                Button play = new Button { name = "play" }; root.Add(play);
                host = new GameObject("HtmlToUitk-disabled-configure"); host.SetActive(false);
                UitkScreen screen = host.AddComponent<UitkScreen>();
                BreakpointWatcher watcher = host.AddComponent<BreakpointWatcher>();
                media = new TextAsset("{\"classes\":[]}");
                action = screen.RegisterAction("play", counter.Increment);
                screen.ConfigureRoot(root, null);
                screen.ReloadRoot(root);
                watcher.ConfigureRoot(root, media);
                yield return null;
                Submit(play); Assert.That(counter.Calls, Is.EqualTo(0));
                Assert.That(screen.Root, Is.Null); Assert.That(watcher.Controller, Is.Null);
                host.SetActive(true);
                yield return null;
                Submit(play); Assert.That(counter.Calls, Is.EqualTo(1));
                Assert.That(screen.Root, Is.SameAs(root)); Assert.That(watcher.Controller, Is.Not.Null);
                host.SetActive(false);
                screen.ConfigureRoot(root, null); screen.ReloadRoot(root); watcher.ConfigureRoot(root, media);
                yield return null;
                Submit(play); Assert.That(counter.Calls, Is.EqualTo(1));
                Assert.That(screen.Root, Is.Null); Assert.That(watcher.Controller, Is.Null);
                host.SetActive(true);
                yield return null;
                Submit(play); Assert.That(counter.Calls, Is.EqualTo(2));
            }
            finally
            {
                action?.Dispose();
                if (host != null) { host.SetActive(false); UnityEngine.Object.Destroy(host); }
                if (panelHost != null) { panelHost.SetActive(false); UnityEngine.Object.Destroy(panelHost); }
                if (media != null) UnityEngine.Object.Destroy(media);
                if (panelSettings != null) UnityEngine.Object.Destroy(panelSettings);
                if (target != null) { target.Release(); UnityEngine.Object.Destroy(target); }
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreOwnedPlayMode()
        {
            if (!_enteredPlayMode) yield break;
            _enteredPlayMode = false;
            yield return new ExitPlayMode();
        }

        [Test]
        public void CanonicalCliAssemblyIsDetectedAndMissingExternalRunnerIsVisible()
        {
            string directory = Path.Combine(Path.GetTempPath(), "htu-cli-discovery-" + Guid.NewGuid().ToString("N"));
            string publish = Path.Combine(directory, "Tools", "HtmlToUitk", "bin");
            Directory.CreateDirectory(publish);
            string canonical = Path.Combine(publish, "htmltouitk.dll"); File.WriteAllBytes(canonical, new byte[0]);
            try
            {
                string resolved = HtmlToUitkSettings.ResolveCliPath(null, directory);
                Assert.That(resolved, Is.EqualTo(Path.GetFullPath(canonical)));
                Assert.That(File.Exists(resolved), Is.True);
                Assert.That(HtmlToUitkSettings.ResolveCliPath("Tools/HtmlToUitk/bin/htmltouitk.dll", directory), Is.EqualTo(Path.GetFullPath(canonical)));
                Assert.That(HtmlToUitkSettings.ResolveCliPath("htmltouitk", directory), Is.EqualTo("htmltouitk"));
                string missing = Path.Combine(directory, "missing.dll");
                Assert.That(HtmlToUitkSettings.ResolveCliPath(missing, directory), Is.EqualTo(missing));
                CliProcessBridge bridge = new CliProcessBridge(missing);
                Assert.That(bridge.IsAvailable(out string reason), Is.False);
                Assert.That(reason, Does.Contain("not found"));
            }
            finally { File.Delete(canonical); Directory.Delete(publish); Directory.Delete(Path.GetDirectoryName(publish)); Directory.Delete(Path.Combine(directory, "Tools")); Directory.Delete(directory); }
        }

        [UnityTest]
        public IEnumerator ChildChangesRootReattachReplacementAndDisabledActionsHaveOneOwner()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            VisualElement root = new VisualElement { name = "app" };
            VisualElement child = new VisualElement { name = "child" };
            Button play = new Button { name = "play" };
            Button other = new Button { name = "other" };
            child.Add(play); root.Add(child); root.Add(other);
            window.Show(); window.rootVisualElement.Add(root);
            UitkActionBindings bindings = new UitkActionBindings(root);
            int playCalls = 0;
            int otherCalls = 0;
            IDisposable stale = bindings.Bind("#play", () => playCalls += 100);
            IDisposable current = bindings.Bind("play", () => playCalls++);
            bindings.Bind("other", () => otherCalls++);
            stale.Dispose();
            try
            {
                yield return null;
                Submit(play); Assert.AreEqual(1, playCalls);
                play.SetEnabled(false); Submit(play); Assert.AreEqual(1, playCalls);
                play.SetEnabled(true);
                child.RemoveFromHierarchy();
                yield return null;
                Submit(other); Assert.AreEqual(1, otherCalls, "Removing a child must not suspend all root actions.");
                Submit(play); Assert.AreEqual(1, playCalls, "Detached child must lose its callback.");
                root.Add(child);
                yield return null;
                Submit(play); Assert.AreEqual(2, playCalls);
                root.RemoveFromHierarchy();
                yield return null;
                Submit(play); Assert.AreEqual(2, playCalls);
                window.rootVisualElement.Add(root);
                yield return null;
                Submit(play); Assert.AreEqual(3, playCalls);
                bindings.Rebind(root); bindings.Rebind(root);
                Submit(play); Assert.AreEqual(4, playCalls);
                current.Dispose(); Submit(play); Assert.AreEqual(4, playCalls);
                bindings.Dispose(); Submit(other); Assert.AreEqual(1, otherCalls);
            }
            finally { bindings.Dispose(); window.Close(); }
        }

        [Test]
        public void PerElementMediaClassesResizePreserveAuthoredClassesAndReleaseOwnership()
        {
            const string json = "{\"root\":\"app\",\"classes\":[{\"class\":\"wide\",\"query\":{\"minWidth\":1000}},{\"class\":\"card-wide\",\"selector\":\".card\",\"query\":{\"minWidth\":1000,\"minHeight\":600,\"minAspect\":1.4}}]}";
            VisualElement root = new VisualElement { name = "app" };
            VisualElement card = new VisualElement { name = "card" };
            card.AddToClassList("card"); card.AddToClassList("authored"); root.Add(card);
            BreakpointSet set = BreakpointSet.Parse(json);
            BreakpointController controller = new BreakpointController(root, set);
            try
            {
                controller.ApplyViewport(0, 0); Assert.IsEmpty(controller.Active);
                controller.ApplyViewport(1280, 720);
                Assert.IsTrue(root.ClassListContains("wide"));
                Assert.IsTrue(card.ClassListContains("card-wide"));
                Assert.IsFalse(root.ClassListContains("card-wide"));
                Assert.Throws<InvalidOperationException>(() => new BreakpointController(root, set));
                controller.ApplyViewport(600, 1200);
                Assert.IsFalse(root.ClassListContains("wide")); Assert.IsFalse(card.ClassListContains("card-wide"));
                controller.ApplyViewport(3440, 1440); Assert.IsTrue(card.ClassListContains("card-wide"));
            }
            finally { controller.Dispose(); }
            Assert.IsTrue(card.ClassListContains("authored"));
            Assert.IsFalse(card.ClassListContains("card-wide"));
            using (BreakpointController replacement = new BreakpointController(root, set)) replacement.ApplyViewport(1920, 1080);
        }

        [Test]
        public void InvalidMediaSelectorIsRejectedBeforeAcquiringRootLease()
        {
            VisualElement root = new VisualElement();
            BreakpointSet invalid = BreakpointSet.Parse("{\"classes\":[{\"class\":\"bad\",\"selector\":\"a:hover\",\"query\":{}}]}");
            Assert.Throws<FormatException>(() => new BreakpointController(root, invalid));
            using (BreakpointController good = new BreakpointController(root, BreakpointSet.Parse("{\"classes\":[]}"))) good.ApplyViewport(1280, 720);
        }

        [Test]
        public void DisposedAndSupersededTransitionsCannotPublishOrFocusStalePages()
        {
            DeferredBackend backend = new DeferredBackend();
            UitkRouter router = new UitkRouter(NavSpec.Parse(Navigation), backend);
            List<string> shown = new List<string>();
            router.OnShown += shown.Add;
            router.Start();
            router.Navigate("next");
            Assert.That(shown, Is.Empty, "Completing the superseded start must not focus or publish home.");
            Assert.That(backend.Focused, Is.Empty);
            router.Dispose();
            foreach (DeferredBackend.Handle handle in backend.Handles) handle.Complete();
            Assert.That(shown, Is.Empty);
            Assert.That(backend.Focused, Is.Empty);
            Assert.That(router.Navigate("home"), Is.False);
        }

        [Test]
        public void RepeatedAnimatorRouterBindingHasOneOwnerAndDisposeRemovesIt()
        {
            VisualElement root = new VisualElement();
            root.Add(new VisualElement { name = "home" }); root.Add(new VisualElement { name = "next" });
            UitkRouter first = new UitkRouter(NavSpec.Parse(Navigation), new UiDocumentPageBackend(root));
            UitkRouter second = new UitkRouter(NavSpec.Parse(Navigation), new UiDocumentPageBackend(root));
            UitkAnimator animator = new UitkAnimator(root, AnimSpec.Parse("{\"animations\":[],\"bindings\":[]}"));
            int firstCalls = 0; int secondCalls = 0;
            try
            {
                animator.AttachRouter(first, id => { firstCalls++; return root; });
                animator.AttachRouter(first, id => { firstCalls++; return root; });
                first.Start(); first.Navigate("next");
                Assert.That(firstCalls, Is.EqualTo(3));
                animator.AttachRouter(second, id => { secondCalls++; return root; });
                first.Back(); Assert.That(firstCalls, Is.EqualTo(3));
                second.Start(); Assert.That(secondCalls, Is.EqualTo(1));
                animator.Dispose(); second.Navigate("next");
                Assert.That(secondCalls, Is.EqualTo(1));
                Assert.That(animator.ActiveCount, Is.EqualTo(0));
            }
            finally { animator.Dispose(); first.Dispose(); second.Dispose(); }
        }

        [UnityTest]
        public IEnumerator AnimatedExitRestoresAuthoredInlineLayoutAndTransformOnTeardown()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            VisualElement root = new VisualElement { name = "app" };
            VisualElement home = new VisualElement { name = "home" };
            home.style.position = Position.Relative; home.style.left = 12; home.style.top = 17;
            home.style.opacity = 0.7f; home.style.translate = new Translate(3, 5);
            root.Add(home); root.Add(new VisualElement { name = "next" });
            root.style.width = 1280; root.style.height = 720;
            window.Show(); window.rootVisualElement.Add(root);
            UitkRouter router = new UitkRouter(NavSpec.Parse(Navigation), new UiDocumentPageBackend(root));
            try
            {
                yield return null;
                router.Start();
                Assert.That(home.style.opacity.value, Is.EqualTo(0.7f));
                router.Navigate("next", NavMode.Push, TransitionKind.SlideLeft, 10000);
                Assert.That(home.style.position.value, Is.EqualTo(Position.Absolute));
                router.Dispose();
                Assert.That(home.style.position.value, Is.EqualTo(Position.Relative));
                Assert.That(home.style.left.value.value, Is.EqualTo(12));
                Assert.That(home.style.top.value.value, Is.EqualTo(17));
                Assert.That(home.style.translate.value.x.value, Is.EqualTo(3));
                Assert.That(home.style.translate.value.y.value, Is.EqualTo(5));
                Assert.That(home.style.opacity.value, Is.EqualTo(0.7f));
            }
            finally { router.Dispose(); window.Close(); }
        }
    }
}
