using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Neo.HtmlToUitk.Navigation;
using Neo.HtmlToUitk.Responsive;
using Neo.HtmlToUitk.Animation;
using Neo.HtmlToUitk.Localization;
using UnityEditor;

namespace Neo.HtmlToUitk.Tests
{
    public sealed partial class RuntimeOwnershipTests
    {
        private sealed class RedirectLocalizer : ILocalizer
        {
            public Action Redirect;
            public readonly List<string> Keys = new List<string>();
            public string Resolve(string key)
            {
                Keys.Add(key);
                Action redirect = Redirect;
                Redirect = null;
                redirect?.Invoke();
                return "translated " + key;
            }
        }

        [Test]
        public void LocalizationAcceptsConverterMixedCaseAndPreservesExplicitCustomKeys()
        {
            VisualElement root = new VisualElement();
            Label ordinary = new Label("ordinary display text");
            Label mixed = new Label("app.Home.Title");
            Label custom = new Label("custom source") { userData = new LocalizationKey("CUSTOM_KEY") };
            root.Add(ordinary); root.Add(mixed); root.Add(custom);
            RedirectLocalizer localizer = new RedirectLocalizer();

            Assert.That(UitkLocalization.Apply(root, localizer), Is.EqualTo(2));
            Assert.That(mixed.text, Is.EqualTo("translated app.Home.Title"));
            Assert.That(custom.text, Is.EqualTo("translated CUSTOM_KEY"));
            Assert.That(ordinary.text, Is.EqualTo("ordinary display text"));
            Assert.That(UitkLocalization.Apply(root, localizer), Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { "app.Home.Title", "CUSTOM_KEY", "app.Home.Title", "CUSTOM_KEY" }, localizer.Keys);
            Assert.That(UitkLocalization.LooksLikeKey("1.menu"), Is.True, "Retain the existing runtime key heuristic compatibility.");
        }

        [UnityTest]
        public IEnumerator LocalizerRootReplacementOwnsTheFinalProjectionAndOrdinaryReuse()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            GameObject host = new GameObject("HtmlToUitk-localizer-reentry");
            TextAsset firstNav = ScreenNavigation("first");
            TextAsset latestNav = ScreenNavigation("latest");
            TextAsset ordinaryNav = ScreenNavigation("ordinary");
            UitkScreen screen = host.AddComponent<UitkScreen>();
            try
            {
                window.Show();
                VisualElement first = ScreenTree("first");
                first.Add(new Label("app.title"));
                VisualElement latest = ScreenTree("latest");
                VisualElement ordinary = ScreenTree("ordinary");
                window.rootVisualElement.Add(first); window.rootVisualElement.Add(latest); window.rootVisualElement.Add(ordinary);
                yield return null;
                Assert.That(first.panel != null && latest.panel != null, Is.True);
                UitkRouter expected = null;
                screen.Localizer = new RedirectLocalizer { Redirect = () =>
                {
                    screen.ConfigureRoot(latest, latestNav);
                    expected = screen.Router;
                } };

                screen.ConfigureRoot(first, firstNav);

                Assert.That(expected, Is.Not.Null);
                Assert.That(screen.Root, Is.SameAs(latest));
                Assert.That(screen.Router, Is.SameAs(expected));
                Assert.That(screen.MissingElements, Is.Empty);
                Assert.That(screen.Router.Current, Is.EqualTo("latestHome"));
                Assert.That(screen.Router.Navigate("latestNext"), Is.True);
                Assert.That(latest.Q("latest-next").style.display.value, Is.Not.EqualTo(DisplayStyle.None));
                Assert.That(screen.LastError, Is.Null);
                screen.ConfigureRoot(ordinary, ordinaryNav);
                Assert.That(screen.Root, Is.SameAs(ordinary));
                Assert.That(screen.Router.Current, Is.EqualTo("ordinaryHome"));
                Assert.That(screen.MissingElements, Is.Empty);
            }
            finally
            {
                screen.Localizer = null;
                screen.ConfigureRoot(null, null);
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(firstNav); UnityEngine.Object.DestroyImmediate(latestNav); UnityEngine.Object.DestroyImmediate(ordinaryNav);
                window.Close(); UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator AnimationDisposalReentrySupersedesTheOuterRootReplacement()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            GameObject host = new GameObject("HtmlToUitk-disposal-reentry");
            TextAsset firstNav = ScreenNavigation("first");
            TextAsset outerNav = ScreenNavigation("outer");
            TextAsset latestNav = ScreenNavigation("latest");
            TextAsset animation = new TextAsset("{\"animations\":[{\"id\":\"fade\",\"duration\":10000,\"steps\":[{\"offset\":0,\"set\":{\"opacity\":\"0\"}},{\"offset\":1,\"set\":{\"opacity\":\"1\"}}]}],\"bindings\":[]}");
            UitkScreen screen = host.AddComponent<UitkScreen>();
            try
            {
                window.Show();
                VisualElement first = ScreenTree("first");
                VisualElement outer = ScreenTree("outer");
                VisualElement latest = ScreenTree("latest");
                window.rootVisualElement.Add(first); window.rootVisualElement.Add(outer); window.rootVisualElement.Add(latest);
                yield return null;
                Assert.That(first.panel != null && latest.panel != null, Is.True);
                screen.ConfigureRoot(first, firstNav, animation);
                UitkAnimator oldAnimator = screen.Animator;
                Assert.That(oldAnimator.PlayManual("fade", first.Q("first-home"), ReducedMotionMode.Off), Is.True);
                Assert.That(oldAnimator.ActiveCount, Is.EqualTo(1));
                UitkRouter expected = null;
                int cancellations = 0;
                oldAnimator.Cancelled += evt =>
                {
                    cancellations++;
                    screen.ConfigureRoot(latest, latestNav);
                    expected = screen.Router;
                };

                screen.ConfigureRoot(outer, outerNav);

                Assert.That(cancellations, Is.EqualTo(1));
                Assert.That(oldAnimator.ActiveCount, Is.Zero);
                Assert.That(expected, Is.Not.Null);
                Assert.That(screen.Root, Is.SameAs(latest));
                Assert.That(screen.Router, Is.SameAs(expected));
                Assert.That(screen.Animator, Is.Null);
                Assert.That(screen.MissingElements, Is.Empty);
                Assert.That(screen.Router.Navigate("latestNext"), Is.True);
                Assert.That(latest.Q("latest-next").style.display.value, Is.Not.EqualTo(DisplayStyle.None));
                yield return null;
                Assert.That(screen.Router, Is.SameAs(expected));
                Assert.That(cancellations, Is.EqualTo(1));
            }
            finally
            {
                screen.ConfigureRoot(null, null);
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(firstNav); UnityEngine.Object.DestroyImmediate(outerNav); UnityEngine.Object.DestroyImmediate(latestNav); UnityEngine.Object.DestroyImmediate(animation);
                window.Close(); UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private static VisualElement ScreenTree(string prefix)
        {
            VisualElement root = new VisualElement();
            root.Add(new VisualElement { name = prefix + "-home" });
            root.Add(new VisualElement { name = prefix + "-next" });
            return root;
        }

        private static TextAsset ScreenNavigation(string prefix)
        {
            return new TextAsset("{\"schema\":1,\"start\":\"" + prefix + "Home\",\"pages\":[{\"id\":\"" + prefix + "Home\",\"element\":\"" + prefix + "-home\"},{\"id\":\"" + prefix + "Next\",\"element\":\"" + prefix + "-next\"}],\"edges\":[],\"modals\":[],\"tabs\":[]}");
        }

        [UnityTest]
        public IEnumerator SameRootCancellationRebuildReleasesOldMediaAndPendingPageWorkFirst()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            GameObject host = new GameObject("HtmlToUitk-same-root-reentry");
            TextAsset nav = ScreenNavigation("same");
            TextAsset animation = ScreenAnimation();
            TextAsset oldMedia = ScreenMedia("old-wide");
            TextAsset newMedia = ScreenMedia("new-wide");
            UitkScreen screen = host.AddComponent<UitkScreen>();
            IDisposable action = null;
            try
            {
                window.Show();
                VisualElement root = ScreenTree("same");
                root.name = "app";
                root.AddToClassList("authored-keep");
                Button button = new Button { name = "same-action" };
                root.Add(button);
                window.rootVisualElement.Add(root);
                yield return null;
                Assert.That(root.panel, Is.Not.Null);
                int clicks = 0;
                action = screen.RegisterAction("same-action", () => clicks++);
                screen.ConfigureRoot(root, nav, animation, oldMedia);
                screen.Breakpoints.ApplyViewport(1280, 720);
                Assert.That(root.ClassListContains("old-wide"), Is.True);
                VisualElement home = root.Q("same-home");
                home.style.left = 13f;
                Assert.That(screen.Router.Navigate("sameNext", NavMode.Push, TransitionKind.SlideLeft, 10000), Is.True);
                Assert.That(screen.Router.IsTransitioning, Is.True, "The old native page handles must really be pending.");
                UitkAnimator oldAnimator = screen.Animator;
                Assert.That(oldAnimator.PlayManual("fade", home, ReducedMotionMode.Off), Is.True);
                UitkRouter expected = null;
                int cancellations = 0;
                oldAnimator.Cancelled += evt =>
                {
                    cancellations++;
                    screen.ConfigureRoot(root, nav, null, newMedia);
                    expected = screen.Router;
                };

                screen.ReloadRoot(root);

                Assert.That(cancellations, Is.EqualTo(1));
                Assert.That(expected, Is.Not.Null);
                Assert.That(screen.Router, Is.SameAs(expected));
                Assert.That(screen.Root, Is.SameAs(root));
                Assert.That(screen.LastError, Is.Null);
                Assert.That(screen.Breakpoints, Is.Not.Null);
                screen.Breakpoints.ApplyViewport(1280, 720);
                Assert.That(root.ClassListContains("old-wide"), Is.False);
                Assert.That(root.ClassListContains("new-wide"), Is.True);
                Assert.That(root.ClassListContains("authored-keep"), Is.True);
                Assert.That(screen.Router.Current, Is.EqualTo("sameHome"));
                Assert.That(home.style.display.value, Is.Not.EqualTo(DisplayStyle.None));
                Assert.That(home.style.left.value.value, Is.EqualTo(13f));
                Submit(button);
                Assert.That(clicks, Is.EqualTo(1));
                yield return null;
                Assert.That(screen.Router, Is.SameAs(expected));
                Assert.That(home.style.display.value, Is.Not.EqualTo(DisplayStyle.None), "Old completion must not hide the new home page.");
                Assert.That(screen.Router.Navigate("sameNext"), Is.True);
                Assert.That(root.Q("same-next").style.display.value, Is.Not.EqualTo(DisplayStyle.None));
            }
            finally
            {
                action?.Dispose();
                screen.ConfigureRoot(null, null);
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(nav); UnityEngine.Object.DestroyImmediate(animation);
                UnityEngine.Object.DestroyImmediate(oldMedia); UnityEngine.Object.DestroyImmediate(newMedia);
                window.Close(); UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator CancellationFailureStillUnwiresActionsAndReleasesTheResponsiveRoot()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            GameObject host = new GameObject("HtmlToUitk-cancellation-fault");
            TextAsset nav = ScreenNavigation("fault");
            TextAsset animation = ScreenAnimation();
            TextAsset media = ScreenMedia("fault-wide");
            UitkScreen screen = host.AddComponent<UitkScreen>();
            IDisposable action = null;
            try
            {
                window.Show();
                VisualElement root = ScreenTree("fault");
                root.name = "app";
                Button button = new Button { name = "fault-action" };
                root.Add(button);
                window.rootVisualElement.Add(root);
                yield return null;
                Assert.That(root.panel, Is.Not.Null);
                int clicks = 0;
                action = screen.RegisterAction("fault-action", () => clicks++);
                screen.ConfigureRoot(root, nav, animation, media);
                screen.Breakpoints.ApplyViewport(1280, 720);
                Assert.That(root.ClassListContains("fault-wide"), Is.True);
                Assert.That(screen.Animator.PlayManual("fade", root.Q("fault-home"), ReducedMotionMode.Off), Is.True);
                screen.Animator.Cancelled += evt => throw new InvalidOperationException("OwnedCancellationFault");

                InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => screen.ReloadRoot(root));

                Assert.That(failure.Message, Is.EqualTo("OwnedCancellationFault"));
                Assert.That(screen.Root, Is.Null);
                Assert.That(screen.Router, Is.Null);
                Assert.That(screen.Breakpoints, Is.Null);
                Assert.That(root.ClassListContains("fault-wide"), Is.False);
                Submit(button);
                Assert.That(clicks, Is.Zero, "A failed old cancellation must not leave the root's native action callback wired.");
                screen.ConfigureRoot(root, nav, null, media);
                Assert.That(screen.Router.Current, Is.EqualTo("faultHome"));
                Assert.That(screen.Breakpoints, Is.Not.Null, "The same responsive root lease must be reusable after the fault.");
                Submit(button);
                Assert.That(clicks, Is.EqualTo(1));
            }
            finally
            {
                action?.Dispose();
                screen.ConfigureRoot(null, null);
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(nav); UnityEngine.Object.DestroyImmediate(animation); UnityEngine.Object.DestroyImmediate(media);
                window.Close(); UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private static TextAsset ScreenAnimation()
        {
            return new TextAsset("{\"animations\":[{\"id\":\"fade\",\"duration\":10000,\"steps\":[{\"offset\":0,\"set\":{\"opacity\":\"0\"}},{\"offset\":1,\"set\":{\"opacity\":\"1\"}}]}],\"bindings\":[]}");
        }

        private static TextAsset ScreenMedia(string className)
        {
            return new TextAsset("{\"root\":\"app\",\"classes\":[{\"class\":\"" + className + "\",\"query\":{\"minWidth\":1000}}]}");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CurrentPlanCompletesAndFocusesOnlyAfterBothPageTransitions(bool showCompletesFirst)
        {
            DeferredBackend backend = new DeferredBackend();
            using (UitkRouter router = new UitkRouter(NavSpec.Parse(Navigation), backend))
            {
                List<string> completed = new List<string>();
                router.OnHidden += id => completed.Add("Hidden:" + id);
                router.OnShown += id => completed.Add("Shown:" + id);
                Assert.That(router.Start(), Is.True);
                backend.Handles[0].Complete();
                Assert.That(completed, Is.EqualTo(new[] { "Shown:home" }));
                Assert.That(backend.Focused, Is.EqualTo(new[] { "home-focus" }));
                completed.Clear();
                backend.Focused.Clear();

                Assert.That(router.Navigate("next"), Is.True);
                Assert.That(backend.Handles.Count, Is.EqualTo(3));
                backend.Handles[showCompletesFirst ? 2 : 1].Complete();
                Assert.That(completed, Is.Empty);
                Assert.That(backend.Focused, Is.Empty);
                Assert.That(router.IsTransitioning, Is.True);

                backend.Handles[showCompletesFirst ? 1 : 2].Complete();
                Assert.That(completed, Is.EqualTo(new[] { "Hidden:home", "Shown:next" }));
                Assert.That(backend.Focused, Is.EqualTo(new[] { "next-focus" }));
                Assert.That(router.IsTransitioning, Is.False);
                foreach (DeferredBackend.Handle handle in backend.Handles)
                    handle.Complete();
                Assert.That(completed, Is.EqualTo(new[] { "Hidden:home", "Shown:next" }));
                Assert.That(backend.Focused, Is.EqualTo(new[] { "next-focus" }));
            }
        }

        [Test]
        public void DisposingPendingPlanCompletesBackendHandlesWithoutPublishingOrFocusing()
        {
            DeferredBackend backend = new DeferredBackend();
            using (UitkRouter router = new UitkRouter(NavSpec.Parse(Navigation), backend))
            {
                List<string> completed = new List<string>();
                router.OnShown += completed.Add;
                router.OnHidden += completed.Add;
                Assert.That(router.Start(), Is.True);
                Assert.That(router.Navigate("next"), Is.True);
                Assert.That(router.IsTransitioning, Is.True);

                router.Dispose();

                Assert.That(backend.Handles.Count, Is.EqualTo(3));
                foreach (DeferredBackend.Handle handle in backend.Handles)
                    Assert.That(handle.IsDone, Is.True, "Disposal must release the backend transition too.");
                Assert.That(router.IsTransitioning, Is.False);
                Assert.That(completed, Is.Empty);
                Assert.That(backend.Focused, Is.Empty);
                Assert.That(router.Back(), Is.False);
                Assert.That(router.Current, Is.EqualTo("next"));
            }
        }

        [Test]
        public void RedirectingFromNavigatedDoesNotPublishWillHideFromTheSupersededPlan()
        {
            DeferredBackend backend = new DeferredBackend();
            using (UitkRouter router = new UitkRouter(NavSpec.Parse(Navigation), backend))
            {
                List<string> willHide = new List<string>();
                router.OnWillHide += willHide.Add;
                router.OnEvent += routerEvent =>
                {
                    if (routerEvent.Kind == RouterEventKind.Navigated && routerEvent.Id == "next")
                        router.Navigate("home", NavMode.Show);
                };
                Assert.That(router.Start(), Is.True);
                backend.Handles[0].Complete();

                router.Navigate("next");

                Assert.That(router.Current, Is.EqualTo("home"));
                Assert.That(router.Stack, Is.EqualTo(new[] { "home" }));
                Assert.That(willHide, Is.EqualTo(new[] { "next" }), "The redirected home page must not receive the superseded plan's WillHide.");
            }
        }

        [Test]
        public void AuthoredResponsiveClassesSurviveInactiveQueriesAndOwnerReplacement()
        {
            VisualElement root = new VisualElement { name = "app" };
            VisualElement authored = new VisualElement();
            VisualElement generated = new VisualElement();
            root.AddToClassList("wide");
            authored.AddToClassList("card");
            authored.AddToClassList("card-wide");
            generated.AddToClassList("card");
            root.Add(authored);
            root.Add(generated);
            BreakpointSet set = BreakpointSet.Parse("{\"root\":\"app\",\"classes\":[{\"class\":\"wide\",\"query\":{\"minWidth\":1000}},{\"class\":\"card-wide\",\"selector\":\".card\",\"query\":{\"minWidth\":1000}}]}");
            using (BreakpointController controller = new BreakpointController(root, set))
            {
                controller.ApplyViewport(1280, 720);
                Assert.That(generated.ClassListContains("card-wide"), Is.True);
                controller.ApplyViewport(600, 720);
                Assert.That(controller.Active, Is.Empty);
                Assert.That(root.ClassListContains("wide"), Is.True);
                Assert.That(authored.ClassListContains("card-wide"), Is.True);
                Assert.That(generated.ClassListContains("card-wide"), Is.False);
                controller.ApplyViewport(1280, 720);
            }

            Assert.That(root.ClassListContains("wide"), Is.True);
            Assert.That(authored.ClassListContains("card-wide"), Is.True);
            Assert.That(generated.ClassListContains("card-wide"), Is.False);
            using (BreakpointController replacement = new BreakpointController(root, set))
            {
                replacement.ApplyViewport(1280, 720);
                Assert.That(generated.ClassListContains("card-wide"), Is.True);
            }
            Assert.That(generated.ClassListContains("card-wide"), Is.False);
        }

        [UnityTest]
        public IEnumerator TriggerRewiringHasOneCallbackAndUnwireAndDisposeRemoveIt()
        {
            const string navigation = "{\"schema\":1,\"start\":\"home\",\"pages\":[{\"id\":\"home\",\"element\":\"home\"}],\"edges\":[{\"from\":\"home\",\"trigger\":\"back\",\"action\":\"back\"}],\"modals\":[],\"tabs\":[]}";
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            UitkRouter router = null;
            try
            {
                VisualElement root = new VisualElement();
                VisualElement home = new VisualElement { name = "home" };
                Button back = new Button { name = "back" };
                home.Add(back);
                root.Add(home);
                window.Show();
                window.rootVisualElement.Add(root);
                yield return null;
                Assert.That(back.panel, Is.Not.Null);
                router = new UitkRouter(NavSpec.Parse(navigation), new UiDocumentPageBackend(root));
                int calls = 0;
                router.OnBackOnRoot += () => calls++;
                Assert.That(router.Start(), Is.True);
                router.WireTriggers();
                router.WireTriggers();
                Submit(back);
                Assert.That(calls, Is.EqualTo(1));
                back.SetEnabled(false);
                Submit(back);
                Assert.That(calls, Is.EqualTo(1));
                back.SetEnabled(true);
                router.UnwireTriggers();
                Submit(back);
                Assert.That(calls, Is.EqualTo(1));
                router.WireTriggers();
                Submit(back);
                Assert.That(calls, Is.EqualTo(2));
                router.Dispose();
                Submit(back);
                Assert.That(calls, Is.EqualTo(2));
            }
            finally
            {
                router?.Dispose();
                window.Close();
            }
        }
    }
}
