using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Neo.HtmlToUitk.Navigation;
using Neo.HtmlToUitk.Responsive;

namespace Neo.HtmlToUitk.Tests
{
    public sealed partial class RuntimeOwnershipTests
    {
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
