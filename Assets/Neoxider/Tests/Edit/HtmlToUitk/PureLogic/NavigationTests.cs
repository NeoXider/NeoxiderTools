using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Neo.HtmlToUitk.Navigation;

namespace Neo.HtmlToUitk.PureLogicTests
{
    public class NavigationTests
    {
        private const string SampleNav = @"{
  ""schema"": 1,
  ""document"": ""App"",
  ""start"": ""home"",
  ""pages"": [
    { ""id"": ""home"", ""element"": ""app-home"", ""focus"": ""app-home-play"", ""transition"": { ""in"": ""fade"", ""out"": ""fade"", ""ms"": 220 } },
    { ""id"": ""settings"", ""element"": ""app-settings"", ""focus"": ""app-settings-back"" },
    { ""id"": ""about"", ""element"": ""app-about"" }
  ],
  ""edges"": [
    { ""from"": ""home"", ""trigger"": ""app-home-settings"", ""action"": ""navigate"", ""to"": ""settings"", ""mode"": ""push"", ""transition"": ""slide-left"", ""ms"": 260 },
    { ""from"": ""settings"", ""trigger"": ""app-settings-back"", ""action"": ""back"" },
    { ""from"": ""home"", ""trigger"": ""app-home-quit"", ""action"": ""openModal"", ""to"": ""confirm"" },
    { ""from"": ""home"", ""trigger"": ""app-home-about"", ""action"": ""navigate"", ""to"": ""about"" },
    { ""from"": ""about"", ""trigger"": ""app-about-home"", ""action"": ""navigate"", ""to"": ""home"", ""mode"": ""show"" }
  ],
  ""modals"": [
    { ""id"": ""confirm"", ""element"": ""app-confirm"", ""ownerPage"": ""home"", ""closeTriggers"": [""app-confirm-cancel""] }
  ],
  ""tabs"": [
    { ""group"": ""settings-tabs"", ""page"": ""settings"", ""default"": ""app-tab-audio"", ""tabs"": [
      { ""tab"": ""app-tab-audio"", ""panel"": ""app-panel-audio"" },
      { ""tab"": ""app-tab-video"", ""panel"": ""app-panel-video"" } ] }
  ]
}";

        private static RouterCore NewRouter(RouterOptions options = null)
        {
            RouterCore router = new RouterCore(NavSpec.Parse(SampleNav), options);
            router.Start();
            return router;
        }

        private static string Describe(NavPlan plan)
        {
            return string.Join(",", plan.Steps.Select(s => s.Kind + ":" + s.Id));
        }

        [Test]
        public void ParsesSampleSpec()
        {
            NavSpec spec = NavSpec.Parse(SampleNav);
            Assert.That(spec.Start, Is.EqualTo("home"));
            Assert.That(spec.Pages.Count, Is.EqualTo(3));
            Assert.That(spec.Page("home").TransitionIn, Is.EqualTo(TransitionKind.Fade));
            Assert.That(spec.Page("home").Ms, Is.EqualTo(220));
            Assert.That(spec.Page("settings").TransitionIn, Is.Null);
            Assert.That(spec.Page("settings").Ms, Is.EqualTo(-1));
            Assert.That(spec.Edges[0].Mode, Is.EqualTo(NavMode.Push));
            Assert.That(spec.Edges[3].Mode, Is.EqualTo(NavMode.Push));
            Assert.That(spec.Edges[4].Mode, Is.EqualTo(NavMode.Show));
            Assert.That(spec.Edges[0].Transition, Is.EqualTo(TransitionKind.SlideLeft));
            Assert.That(spec.Edges[2].Action, Is.EqualTo(NavAction.OpenModal));
            Assert.That(spec.Modal("confirm").CloseTriggers, Is.EqualTo(new[] { "app-confirm-cancel" }));
            Assert.That(spec.Group("settings-tabs").InitialTab, Is.EqualTo("app-tab-audio"));
            Assert.That(spec.Validate(), Is.Empty);
        }

        [Test]
        public void ValidateReportsBrokenReferences()
        {
            NavSpec spec = NavSpec.Parse(@"{ ""schema"": 1, ""document"": ""D"", ""start"": ""nope"", ""pages"": [ { ""id"": ""a"", ""element"": ""a"" } ],
              ""edges"": [ { ""from"": ""a"", ""trigger"": ""t"", ""action"": ""navigate"", ""to"": ""zzz"" } ],
              ""modals"": [ { ""id"": ""m"", ""element"": ""m"", ""ownerPage"": ""gone"" } ] }");
            Assert.That(spec.Validate().Count, Is.EqualTo(3));
        }

        [Test]
        public void StartShowsStartPageAndInitialisesTabs()
        {
            RouterCore router = new RouterCore(NavSpec.Parse(SampleNav));
            NavPlan plan = router.Start();
            Assert.That(plan.Outcome, Is.EqualTo(NavOutcome.Done));
            Assert.That(router.Current, Is.EqualTo("home"));
            Assert.That(plan.Steps.Last().Kind, Is.EqualTo(NavStepKind.ShowPage));
            Assert.That(plan.Steps.Last().Transition, Is.EqualTo(TransitionKind.None));
            Assert.That(plan.Steps.Any(s => s.Kind == NavStepKind.SelectTab && s.Tab == "app-tab-audio"), Is.True);
            Assert.That(router.Start().Outcome, Is.EqualTo(NavOutcome.Ignored));
        }

        [Test]
        public void PushHidesPreviousAndBackReturnsWithReversedDirection()
        {
            RouterCore router = NewRouter();
            NavPlan push = router.ActivateTrigger("app-home-settings");
            Assert.That(Describe(push), Is.EqualTo("HidePage:home,ShowPage:settings"));
            Assert.That(push.Steps[0].Direction, Is.EqualTo(TransitionDirection.Forward));
            Assert.That(push.Steps[1].Transition, Is.EqualTo(TransitionKind.SlideLeft));
            Assert.That(push.Steps[1].DurationMs, Is.EqualTo(260));
            Assert.That(router.Stack, Is.EqualTo(new[] { "home", "settings" }));

            NavPlan back = router.ActivateTrigger("app-settings-back");
            Assert.That(Describe(back), Is.EqualTo("HidePage:settings,ShowPage:home"));
            Assert.That(back.Steps[0].Direction, Is.EqualTo(TransitionDirection.Backward));
            Assert.That(back.Steps[0].Transition, Is.EqualTo(TransitionKind.Fade));
            Assert.That(back.Steps[1].DurationMs, Is.EqualTo(220));
            Assert.That(router.Current, Is.EqualTo("home"));
            Assert.That(router.History, Is.Empty);
            Assert.That(router.VisitLog, Is.EqualTo(new[] { "home", "settings", "home" }));
        }

        [Test]
        public void ShowClearsHistoryLikeThePrototype()
        {
            RouterCore router = NewRouter();
            router.Navigate("settings", NavMode.Push);
            NavPlan plan = router.Navigate("about", NavMode.Show);
            Assert.That(Describe(plan), Is.EqualTo("HidePage:settings,ShowPage:about"));
            Assert.That(router.Stack, Is.EqualTo(new[] { "about" }));
            Assert.That(router.History, Is.Empty);
            Assert.That(router.Back().Outcome, Is.EqualTo(NavOutcome.BackOnRoot));
        }

        [Test]
        public void ReplaceTopOptionKeepsEarlierHistory()
        {
            RouterCore router = NewRouter(new RouterOptions { ShowClearsStack = false });
            router.Navigate("settings", NavMode.Push);
            NavPlan plan = router.Navigate("about", NavMode.Show);
            Assert.That(Describe(plan), Is.EqualTo("HidePage:settings,ShowPage:about"));
            Assert.That(router.Stack, Is.EqualTo(new[] { "home", "about" }));
            Assert.That(router.Back().Succeeded, Is.True);
            Assert.That(router.Current, Is.EqualTo("home"));
        }

        [Test]
        public void ShowClearsStackOptionMatchesLayeredHosts()
        {
            RouterCore router = NewRouter(new RouterOptions { ShowClearsStack = true, ShowToExistingPopsToIt = true, PushKeepsPreviousVisible = true });
            router.Navigate("settings", NavMode.Push);
            Assert.That(router.Stack, Is.EqualTo(new[] { "home", "settings" }));
            NavPlan plan = router.Navigate("about", NavMode.Show);
            Assert.That(router.Stack, Is.EqualTo(new[] { "about" }));
            Assert.That(plan.Steps.Count(s => s.Kind == NavStepKind.HidePage), Is.EqualTo(2));
            Assert.That(plan.Steps.Last().Id, Is.EqualTo("about"));
            Assert.That(router.Back().Outcome, Is.EqualTo(NavOutcome.BackOnRoot));
        }

        [Test]
        public void PushKeepingPreviousVisibleEmitsNoHideAndNoShowOnBack()
        {
            RouterCore router = NewRouter(new RouterOptions { PushKeepsPreviousVisible = true });
            NavPlan push = router.Navigate("settings", NavMode.Push);
            Assert.That(Describe(push), Is.EqualTo("ShowPage:settings"));
            NavPlan back = router.Back();
            Assert.That(Describe(back), Is.EqualTo("HidePage:settings"));
        }

        [Test]
        public void ShowToStartPageWithDefaultOptionsClearsHistory()
        {
            RouterCore router = NewRouter();
            router.Navigate("settings", NavMode.Push);
            router.Navigate("about", NavMode.Push);
            NavPlan plan = router.Navigate("home", NavMode.Show);
            Assert.That(router.Stack, Is.EqualTo(new[] { "home" }));
            Assert.That(router.History, Is.Empty);
            Assert.That(plan.Steps.First().Id, Is.EqualTo("about"));
            Assert.That(plan.Steps.Last().Id, Is.EqualTo("home"));
        }

        [Test]
        public void LayeredModeIgnoresPushOfAPageAlreadyOnTheStack()
        {
            RouterCore router = NewRouter(new RouterOptions { ShowClearsStack = true, PushKeepsPreviousVisible = true });
            router.Navigate("settings", NavMode.Push);
            NavPlan plan = router.Navigate("home", NavMode.Push);
            Assert.That(plan.Outcome, Is.EqualTo(NavOutcome.Ignored));
            Assert.That(router.Stack, Is.EqualTo(new[] { "home", "settings" }));
        }

        [Test]
        public void NavigationToCurrentPageAndUnknownPageIsRejected()
        {
            RouterCore router = NewRouter();
            Assert.That(router.Navigate("home", NavMode.Show).Outcome, Is.EqualTo(NavOutcome.Ignored));
            Assert.That(router.Navigate("ghost", NavMode.Push).Outcome, Is.EqualTo(NavOutcome.UnknownTarget));
            Assert.That(router.Stack, Is.EqualTo(new[] { "home" }));
        }

        [Test]
        public void BackOnRootReportsInsteadOfPopping()
        {
            RouterCore router = NewRouter();
            NavPlan plan = router.Back();
            Assert.That(plan.Outcome, Is.EqualTo(NavOutcome.BackOnRoot));
            Assert.That(router.Current, Is.EqualTo("home"));
        }

        [Test]
        public void ModalOpensOnOwnerPageAndBackClosesItFirst()
        {
            RouterCore router = NewRouter();
            NavPlan open = router.ActivateTrigger("app-home-quit");
            Assert.That(Describe(open), Is.EqualTo("OpenModal:confirm"));
            Assert.That(router.Modals, Is.EqualTo(new[] { "confirm" }));
            Assert.That(router.OpenModal("confirm").Outcome, Is.EqualTo(NavOutcome.Ignored));

            router.Navigate("settings", NavMode.Push);
            Assert.That(router.Modals, Is.Empty);

            router.Navigate("home", NavMode.Show);
            router.OpenModal("confirm");
            NavPlan back = router.Back();
            Assert.That(Describe(back), Is.EqualTo("CloseModal:confirm"));
            Assert.That(router.Current, Is.EqualTo("home"));
            Assert.That(router.Modals, Is.Empty);
        }

        [Test]
        public void ModalCannotOpenOnAnotherPage()
        {
            RouterCore router = NewRouter();
            router.Navigate("settings", NavMode.Push);
            Assert.That(router.OpenModal("confirm").Outcome, Is.EqualTo(NavOutcome.NotAllowed));
            Assert.That(router.OpenModal("missing").Outcome, Is.EqualTo(NavOutcome.UnknownTarget));
        }

        [Test]
        public void ModalCloseTriggerClosesOnlyThatModal()
        {
            RouterCore router = NewRouter();
            router.OpenModal("confirm");
            NavPlan plan = router.ActivateTrigger("app-confirm-cancel");
            Assert.That(Describe(plan), Is.EqualTo("CloseModal:confirm"));
            Assert.That(router.ActivateTrigger("app-confirm-cancel").Outcome, Is.EqualTo(NavOutcome.NoSuchTrigger));
        }

        [Test]
        public void NavigatingWhileModalIsOpenClosesItInTheSamePlan()
        {
            RouterCore router = NewRouter();
            router.OpenModal("confirm");
            NavPlan plan = router.ActivateTrigger("app-home-settings");
            Assert.That(Describe(plan), Is.EqualTo("CloseModal:confirm,HidePage:home,ShowPage:settings"));
        }

        [Test]
        public void TabSelectionTracksPreviousAndWorksWhileThePageIsHidden()
        {
            RouterCore router = NewRouter();
            Assert.That(router.SelectTab("settings-tabs", "app-tab-video").Outcome, Is.EqualTo(NavOutcome.Done));
            router.SelectTab("settings-tabs", "app-tab-audio");
            router.Navigate("settings", NavMode.Push);
            NavPlan plan = router.ActivateTrigger("app-tab-video");
            NavStep step = plan.Steps.Single();
            Assert.That(step.Tab, Is.EqualTo("app-tab-video"));
            Assert.That(step.Panel, Is.EqualTo("app-panel-video"));
            Assert.That(step.PreviousTab, Is.EqualTo("app-tab-audio"));
            Assert.That(step.PreviousPanel, Is.EqualTo("app-panel-audio"));
            Assert.That(router.SelectedTab("settings-tabs"), Is.EqualTo("app-tab-video"));
            Assert.That(router.SelectTab("settings-tabs", "app-tab-video").Outcome, Is.EqualTo(NavOutcome.Ignored));
            Assert.That(router.SelectTab("settings-tabs", "ghost").Outcome, Is.EqualTo(NavOutcome.UnknownTarget));
        }

        [Test]
        public void TabSelectionSurvivesLeavingAndReturning()
        {
            RouterCore router = NewRouter();
            router.Navigate("settings", NavMode.Push);
            router.SelectTab("settings-tabs", "app-tab-video");
            router.Back();
            router.Navigate("settings", NavMode.Push);
            Assert.That(router.SelectedTab("settings-tabs"), Is.EqualTo("app-tab-video"));
        }

        [Test]
        public void FocusMemoryPrefersRememberedElementOverPageDefault()
        {
            RouterCore router = NewRouter();
            Assert.That(router.FocusTarget("home"), Is.EqualTo("app-home-play"));
            router.RememberFocus("home", "app-home-settings");
            Assert.That(router.FocusTarget("home"), Is.EqualTo("app-home-settings"));
            Assert.That(router.FocusTarget("about"), Is.Null);
            router.RememberFocus(RouterCore.ModalFocusKey("confirm"), "app-confirm-ok");
            Assert.That(router.FocusTarget(RouterCore.ModalFocusKey("confirm")), Is.EqualTo("app-confirm-ok"));
        }

        [Test]
        public void FocusRestoreCanBeSwitchedOffToMatchThePrototype()
        {
            RouterCore router = NewRouter(new RouterOptions { RestoreFocus = false });
            router.RememberFocus("home", "app-home-settings");
            Assert.That(router.FocusTarget("home"), Is.EqualTo("app-home-play"));
        }

        [Test]
        public void TransitionResolutionPrefersExplicitThenTargetPageThenDefault()
        {
            RouterCore router = NewRouter(new RouterOptions { DefaultTransition = TransitionKind.Scale, DefaultMs = 300 });
            NavPlan toAbout = router.Navigate("about", NavMode.Push);
            Assert.That(toAbout.Steps[0].Transition, Is.EqualTo(TransitionKind.Scale));
            Assert.That(toAbout.Steps[1].Transition, Is.EqualTo(TransitionKind.Scale));
            Assert.That(toAbout.Steps[1].DurationMs, Is.EqualTo(300));
            NavPlan explicitPlan = router.Navigate("settings", NavMode.Push, TransitionKind.SlideUp, 90);
            Assert.That(explicitPlan.Steps[0].Transition, Is.EqualTo(TransitionKind.SlideUp));
            Assert.That(explicitPlan.Steps[1].Transition, Is.EqualTo(TransitionKind.SlideUp));
            Assert.That(explicitPlan.Steps[1].DurationMs, Is.EqualTo(90));
            router.Navigate("home", NavMode.Show);
            NavPlan again = router.Navigate("about", NavMode.Push);
            Assert.That(again.Steps[1].DurationMs, Is.EqualTo(300));
            NavPlan toHome = router.Navigate("home", NavMode.Show);
            Assert.That(toHome.Steps.Last().Transition, Is.EqualTo(TransitionKind.Fade));
            Assert.That(toHome.Steps.Last().DurationMs, Is.EqualTo(220));
        }

        [Test]
        public void BackReplaysTheLeavingPageTransitionBackwards()
        {
            RouterCore router = NewRouter();
            router.Navigate("about", NavMode.Push, TransitionKind.Scale);
            NavPlan back = router.Back();
            Assert.That(back.Steps[0].Transition, Is.EqualTo(TransitionKind.Fade));
            Assert.That(back.Steps[0].Direction, Is.EqualTo(TransitionDirection.Backward));
            Assert.That(back.Steps[1].Direction, Is.EqualTo(TransitionDirection.Backward));
        }

        [Test]
        public void ReducedMotionZeroesEveryDuration()
        {
            bool reduced = true;
            RouterCore router = NewRouter(new RouterOptions { ReducedMotion = () => reduced });
            NavPlan push = router.ActivateTrigger("app-home-settings");
            Assert.That(push.Steps.All(x => x.DurationMs == 0), Is.True);
            Assert.That(router.Back().Steps.All(x => x.DurationMs == 0), Is.True);
            Assert.That(router.OpenModal("confirm").Steps.Single().DurationMs, Is.EqualTo(0));
            reduced = false;
            router.CloseModal();
            Assert.That(router.ActivateTrigger("app-home-settings").Steps[0].DurationMs, Is.EqualTo(260));
        }

        [Test]
        public void NoneTransitionHasZeroDuration()
        {
            RouterCore router = NewRouter();
            NavPlan plan = router.Navigate("about", NavMode.Push, TransitionKind.None, 500);
            Assert.That(plan.Steps.All(s => s.DurationMs == 0), Is.True);
        }

        [Test]
        public void VisitLogIsCapped()
        {
            RouterCore router = NewRouter(new RouterOptions { VisitLogLimit = 3 });
            for (int i = 0; i < 5; i++)
            {
                router.Navigate("settings", NavMode.Push);
                router.Back();
            }

            Assert.That(router.VisitLog.Count, Is.EqualTo(3));
        }

        [Test]
        public void TriggerFallbackUsesUniqueEdgeFromAnyPage()
        {
            RouterCore router = NewRouter();
            router.Navigate("about", NavMode.Show);
            Assert.That(router.Stack, Is.EqualTo(new[] { "about" }));
            NavPlan plan = router.ActivateTrigger("app-home-settings");
            Assert.That(plan.Outcome, Is.EqualTo(NavOutcome.Done));
            Assert.That(router.Current, Is.EqualTo("settings"));
        }

        [TestCase(TransitionKind.Fade, true, TransitionDirection.Forward, 0.5f, 0.5f, 0f, 0f, 1f)]
        [TestCase(TransitionKind.Fade, false, TransitionDirection.Forward, 0.25f, 0.75f, 0f, 0f, 1f)]
        [TestCase(TransitionKind.SlideLeft, true, TransitionDirection.Forward, 0f, 1f, 100f, 0f, 1f)]
        [TestCase(TransitionKind.SlideLeft, true, TransitionDirection.Forward, 1f, 1f, 0f, 0f, 1f)]
        [TestCase(TransitionKind.SlideLeft, false, TransitionDirection.Forward, 1f, 1f, -100f, 0f, 1f)]
        [TestCase(TransitionKind.SlideLeft, true, TransitionDirection.Backward, 0f, 1f, -100f, 0f, 1f)]
        [TestCase(TransitionKind.SlideLeft, false, TransitionDirection.Backward, 1f, 1f, 100f, 0f, 1f)]
        [TestCase(TransitionKind.SlideRight, true, TransitionDirection.Forward, 0f, 1f, -100f, 0f, 1f)]
        [TestCase(TransitionKind.SlideUp, true, TransitionDirection.Forward, 0f, 1f, 0f, 100f, 1f)]
        [TestCase(TransitionKind.SlideUp, false, TransitionDirection.Forward, 1f, 1f, 0f, -100f, 1f)]
        [TestCase(TransitionKind.Scale, true, TransitionDirection.Forward, 0f, 0f, 0f, 0f, 0.92f)]
        [TestCase(TransitionKind.Scale, true, TransitionDirection.Forward, 1f, 1f, 0f, 0f, 1f)]
        [TestCase(TransitionKind.None, true, TransitionDirection.Forward, 0f, 1f, 0f, 0f, 1f)]
        [TestCase(TransitionKind.None, false, TransitionDirection.Forward, 0f, 0f, 0f, 0f, 1f)]
        public void TransitionFrames(TransitionKind kind, bool entering, TransitionDirection direction, float progress, float opacity, float x, float y, float scale)
        {
            TransitionFrame frame = TransitionMath.Evaluate(kind, entering, direction, progress);
            Assert.That(frame.Opacity, Is.EqualTo(opacity).Within(1e-4));
            Assert.That(frame.TranslateXPercent, Is.EqualTo(x).Within(1e-3));
            Assert.That(frame.TranslateYPercent, Is.EqualTo(y).Within(1e-3));
            Assert.That(frame.Scale, Is.EqualTo(scale).Within(1e-4));
        }

        [Test]
        public void EnteringFrameAtFullProgressIsIdentity()
        {
            foreach (TransitionKind kind in Enum.GetValues(typeof(TransitionKind)))
            {
                foreach (TransitionDirection direction in Enum.GetValues(typeof(TransitionDirection)))
                    Assert.That(TransitionMath.Evaluate(kind, true, direction, 1f).IsIdentity, Is.True, kind + " " + direction);
            }
        }
    }
}
