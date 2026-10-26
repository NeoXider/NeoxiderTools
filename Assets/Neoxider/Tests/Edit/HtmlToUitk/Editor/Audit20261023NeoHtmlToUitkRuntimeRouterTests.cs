using System;
using System.Collections.Generic;
using NUnit.Framework;
using Neo.HtmlToUitk.Navigation;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class Audit20261023NeoHtmlToUitkRuntimeRouterTests
    {
        private const string Spec = @"{
            ""start"":""home"",
            ""pages"":[{""id"":""home"",""element"":""home"",""focus"":""home-default""},
                       {""id"":""next"",""element"":""next"",""focus"":""next-default""}],
            ""tabs"":[{""group"":""group"",""page"":""home"",""tabs"":[{""tab"":""tab"",""panel"":""panel""}]}],
            ""modals"":[{""id"":""outer"",""element"":""outer"",""ownerPage"":""home""},
                        {""id"":""inner"",""element"":""inner"",""ownerPage"":""home""}]
        }";

        private sealed class Backend : IPageBackend
        {
            public readonly HashSet<string> VisiblePages = new HashSet<string>(StringComparer.Ordinal);
            public readonly List<string> Shown = new List<string>();
            public readonly List<string> Focused = new List<string>();
            public string CurrentFocus;
            public string FocusedElementName => CurrentFocus;
            public void Bind(NavSpec spec) => VisiblePages.Clear();
            public VisualElement FindElement(string name) => null;
            public IPageTransitionHandle ShowPage(NavStep step, Action done)
            {
                VisiblePages.Add(step.Id);
                Shown.Add(step.Id);
                done();
                return CompletedTransition.Instance;
            }
            public IPageTransitionHandle HidePage(NavStep step, Action done)
            {
                VisiblePages.Remove(step.Id);
                done();
                return CompletedTransition.Instance;
            }
            public IPageTransitionHandle OpenModal(NavStep step, Action done) { done(); return CompletedTransition.Instance; }
            public IPageTransitionHandle CloseModal(NavStep step, Action done) { done(); return CompletedTransition.Instance; }
            public void SelectTab(NavStep step) { }
            public bool Focus(string name) { Focused.Add(name); return true; }
            public bool FocusModal(string name) { Focused.Add("modal:" + name); return true; }
        }

        [Test]
        public void RedirectFromInitialTabCallbackCannotShowTheSupersededPage()
        {
            Backend backend = new Backend();
            using (UitkRouter router = new UitkRouter(NavSpec.Parse(Spec), backend))
            {
                router.OnTabSelected += (group, tab) => router.Navigate("next", NavMode.Show, TransitionKind.None);
                router.Start();
                Assert.That(router.Current, Is.EqualTo("next"));
                Assert.That(backend.VisiblePages, Is.EquivalentTo(new[] { "next" }));
                Assert.That(backend.Shown, Is.EqualTo(new[] { "next" }));
            }
        }

        [Test]
        public void DisposingInGenericEventSuppressesTheTypedEventAndProjection()
        {
            Backend backend = new Backend();
            using (UitkRouter router = new UitkRouter(NavSpec.Parse(Spec), backend))
            {
                int typedEvents = 0;
                router.OnWillShow += id => typedEvents++;
                router.OnEvent += evt => { if (evt.Kind == RouterEventKind.WillShow) router.Dispose(); };
                router.Start();
                Assert.That(typedEvents, Is.Zero);
                Assert.That(backend.VisiblePages, Is.Empty);
                Assert.That(backend.Focused, Is.Empty);
            }
        }

        [Test]
        public void RedirectFromNavigatedEventCannotPublishOldWillShow()
        {
            Backend backend = new Backend();
            using (UitkRouter router = new UitkRouter(NavSpec.Parse(Spec), backend))
            {
                List<string> willShow = new List<string>();
                router.OnWillShow += willShow.Add;
                router.OnEvent += evt =>
                {
                    if (evt.Kind == RouterEventKind.Navigated && evt.Id == "home")
                        router.Navigate("next", NavMode.Show, TransitionKind.None);
                };
                router.Start();
                Assert.That(willShow, Is.EqualTo(new[] { "next" }));
                Assert.That(backend.VisiblePages, Is.EquivalentTo(new[] { "next" }));
            }
        }

        [Test]
        public void ClosingTopModalKeepsFocusInTheRemainingModal()
        {
            Backend backend = new Backend();
            using (UitkRouter router = new UitkRouter(NavSpec.Parse(Spec), backend))
            {
                router.Start();
                router.OpenModal("outer");
                router.OpenModal("inner");
                backend.Focused.Clear();
                router.CloseModal();
                Assert.That(router.Modals, Is.EqualTo(new[] { "outer" }));
                Assert.That(backend.Focused, Is.EqualTo(new[] { "modal:outer" }));
            }
        }

        [Test]
        public void NestedModalsRememberTheirOwnFocusAndPreservePageFocus()
        {
            Backend backend = new Backend();
            using (UitkRouter router = new UitkRouter(NavSpec.Parse(Spec), backend))
            {
                router.Start();
                backend.CurrentFocus = "home-button";
                router.OpenModal("outer");
                backend.CurrentFocus = "outer-button";
                router.OpenModal("inner");
                backend.CurrentFocus = "inner-button";
                backend.Focused.Clear();
                router.CloseModal();
                router.CloseModal();
                Assert.That(backend.Focused, Is.EqualTo(new[] { "outer-button", "home-button" }));
            }
        }

        [Test]
        public void NavigatingAwayFromAModalDoesNotRememberItsButtonAsPageFocus()
        {
            Backend backend = new Backend();
            using (UitkRouter router = new UitkRouter(NavSpec.Parse(Spec), backend))
            {
                router.Start();
                backend.CurrentFocus = "home-button";
                router.OpenModal("outer");
                backend.CurrentFocus = "modal-button";
                router.Navigate("next");
                backend.CurrentFocus = "next-button";
                backend.Focused.Clear();
                router.Back();
                Assert.That(router.Current, Is.EqualTo("home"));
                Assert.That(backend.VisiblePages, Is.EquivalentTo(new[] { "home" }));
                Assert.That(backend.Focused, Is.EqualTo(new[] { "home-button" }));
            }
        }
    }
}
