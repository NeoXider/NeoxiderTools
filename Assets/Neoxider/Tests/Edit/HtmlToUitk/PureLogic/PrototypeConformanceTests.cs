using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Neo.HtmlToUitk.Navigation;

namespace Neo.HtmlToUitk.PureLogicTests
{
    public class PrototypeConformanceTests
    {
        private const string DemoNav = @"{
  ""schema"": 1, ""document"": ""App"", ""start"": ""home"",
  ""pages"": [
    { ""id"": ""home"", ""element"": ""pd-home"", ""focus"": ""pd-home-settings"", ""transition"": { ""in"": ""fade"", ""out"": ""fade"", ""ms"": 220 } },
    { ""id"": ""settings"", ""element"": ""pd-settings"", ""focus"": ""pd-tab-audio"" },
    { ""id"": ""about"", ""element"": ""pd-about"", ""focus"": ""pd-about-back"", ""transition"": { ""in"": ""slide-up"", ""out"": ""slide-up"" } } ],
  ""edges"": [
    { ""from"": ""home"", ""trigger"": ""pd-home-settings"", ""action"": ""navigate"", ""to"": ""settings"", ""mode"": ""push"", ""transition"": ""slide-left"", ""ms"": 260 },
    { ""from"": ""home"", ""trigger"": ""pd-home-about"", ""action"": ""navigate"", ""to"": ""about"", ""mode"": ""push"", ""transition"": ""scale"" },
    { ""from"": ""home"", ""trigger"": ""pd-home-quit"", ""action"": ""openModal"", ""to"": ""confirm"" },
    { ""from"": ""settings"", ""trigger"": ""pd-settings-back"", ""action"": ""back"" },
    { ""from"": ""about"", ""trigger"": ""pd-about-back"", ""action"": ""back"" } ],
  ""modals"": [ { ""id"": ""confirm"", ""element"": ""pd-confirm"", ""ownerPage"": ""home"", ""closeTriggers"": [""pd-confirm-ok"", ""pd-confirm-cancel""] } ],
  ""tabs"": [ { ""group"": ""settings-tabs"", ""page"": ""settings"", ""default"": ""pd-tab-audio"", ""tabs"": [
    { ""tab"": ""pd-tab-audio"", ""panel"": ""pd-panel-audio"" }, { ""tab"": ""pd-tab-video"", ""panel"": ""pd-panel-video"" } ] } ]
}";

        private sealed class Row
        {
            public string Name;
            public Func<RouterCore, NavPlan> Act;
            public NavOutcome Outcome;
            public string Steps;
            public string Current;
            public string History;
            public string Modals;

            public override string ToString() => Name;
        }

        private static string Summary(NavPlan plan)
        {
            return string.Join("|", plan.Steps.Select(s =>
            {
                string dir = s.Direction == TransitionDirection.Forward ? "F" : "B";
                switch (s.Kind)
                {
                    case NavStepKind.SelectTab: return "Tab:" + s.Group + ":" + s.Tab;
                    case NavStepKind.OpenModal: return "OpenModal:" + s.Id + ":" + Kebab(s.Transition) + ":" + s.DurationMs + ":" + dir;
                    case NavStepKind.CloseModal: return "CloseModal:" + s.Id + ":" + Kebab(s.Transition) + ":" + s.DurationMs + ":" + dir;
                    case NavStepKind.HidePage: return "Hide:" + s.Id + ":" + Kebab(s.Transition) + ":" + s.DurationMs + ":" + dir;
                    default: return "Show:" + s.Id + ":" + Kebab(s.Transition) + ":" + s.DurationMs + ":" + dir;
                }
            }));
        }

        private static string Kebab(TransitionKind kind)
        {
            switch (kind)
            {
                case TransitionKind.SlideLeft: return "slide-left";
                case TransitionKind.SlideRight: return "slide-right";
                case TransitionKind.SlideUp: return "slide-up";
                default: return kind.ToString().ToLowerInvariant();
            }
        }

        private static Row Row_(string name, Func<RouterCore, NavPlan> act, NavOutcome outcome, string steps, string current, string history, string modals)
        {
            return new Row { Name = name, Act = act, Outcome = outcome, Steps = steps, Current = current, History = history, Modals = modals };
        }

        private static IEnumerable<Row> Scenarios()
        {
            yield return Row_("push settings (edge slide-left 260)", r => r.ActivateTrigger("pd-home-settings"), NavOutcome.Done,
                "Hide:home:slide-left:260:F|Show:settings:slide-left:260:F", "settings", "home", "");
            yield return Row_("tab switch to video", r => r.ActivateTrigger("pd-tab-video"), NavOutcome.Done,
                "Tab:settings-tabs:pd-tab-video", "settings", "home", "");
            yield return Row_("back replays the leaving page transition (default fade 220) backwards", r => r.ActivateTrigger("pd-settings-back"), NavOutcome.Done,
                "Hide:settings:fade:220:B|Show:home:fade:220:B", "home", "", "");
            yield return Row_("back from the start page does nothing", r => r.Back(), NavOutcome.BackOnRoot, "", "home", "", "");
            yield return Row_("open modal (fade 150)", r => r.ActivateTrigger("pd-home-quit"), NavOutcome.Done,
                "OpenModal:confirm:fade:150:F", "home", "", "confirm");
            yield return Row_("opening an open modal is ignored", r => r.OpenModal("confirm"), NavOutcome.Ignored, "", "home", "", "confirm");
            yield return Row_("escape closes the modal first", r => r.Back(), NavOutcome.Done,
                "CloseModal:confirm:fade:150:B", "home", "", "");
            yield return Row_("open modal again", r => r.ActivateTrigger("pd-home-quit"), NavOutcome.Done,
                "OpenModal:confirm:fade:150:F", "home", "", "confirm");
            yield return Row_("data-close (cancel) closes the modal", r => r.ActivateTrigger("pd-confirm-cancel"), NavOutcome.Done,
                "CloseModal:confirm:fade:150:B", "home", "", "");
            yield return Row_("open modal a third time", r => r.ActivateTrigger("pd-home-quit"), NavOutcome.Done,
                "OpenModal:confirm:fade:150:F", "home", "", "confirm");
            yield return Row_("data-close (ok) closes the modal", r => r.ActivateTrigger("pd-confirm-ok"), NavOutcome.Done,
                "CloseModal:confirm:fade:150:B", "home", "", "");
            yield return Row_("open modal for the navigation case", r => r.ActivateTrigger("pd-home-quit"), NavOutcome.Done,
                "OpenModal:confirm:fade:150:F", "home", "", "confirm");
            yield return Row_("navigating closes every open modal in the same step list (edge scale, page ms default 220)", r => r.ActivateTrigger("pd-home-about"), NavOutcome.Done,
                "CloseModal:confirm:fade:150:B|Hide:home:scale:220:F|Show:about:scale:220:F", "about", "home", "");
            yield return Row_("back uses the about page slide-up", r => r.ActivateTrigger("pd-about-back"), NavOutcome.Done,
                "Hide:about:slide-up:220:B|Show:home:slide-up:220:B", "home", "", "");
            yield return Row_("navigating to the current page is ignored", r => r.Navigate("home", NavMode.Push), NavOutcome.Ignored, "", "home", "", "");
            yield return Row_("navigating to an unknown page is rejected", r => r.Navigate("nowhere", NavMode.Push), NavOutcome.UnknownTarget, "", "home", "", "");
            yield return Row_("push about with the page transition (no edge override)", r => r.Navigate("about", NavMode.Push), NavOutcome.Done,
                "Hide:home:slide-up:220:F|Show:about:slide-up:220:F", "about", "home", "");
            yield return Row_("show mode (flow overlay) clears history", r => r.Navigate("settings", NavMode.Show, TransitionKind.Fade), NavOutcome.Done,
                "Hide:about:fade:220:F|Show:settings:fade:220:F", "settings", "", "");
            yield return Row_("back after show has nothing to return to", r => r.Back(), NavOutcome.BackOnRoot, "", "settings", "", "");
            yield return Row_("tab state survives leaving the page", r => r.SelectTab("settings-tabs", "pd-tab-video"), NavOutcome.Ignored, "", "settings", "", "");
        }

        [Test]
        public void ReplaysTheDemoFlowLikeTheJavaScriptPrototype()
        {
            RouterCore core = new RouterCore(NavSpec.Parse(DemoNav));
            NavPlan start = core.Start();
            Assert.That(core.Current, Is.EqualTo("home"));
            Assert.That(core.History, Is.Empty);
            Assert.That(core.Modals, Is.Empty);
            Assert.That(core.SelectedTab("settings-tabs"), Is.EqualTo("pd-tab-audio"));
            Assert.That(Summary(start), Is.EqualTo("Tab:settings-tabs:pd-tab-audio|Show:home:none:0:F"));

            foreach (Row row in Scenarios())
            {
                NavPlan plan = row.Act(core);
                Assert.That(plan.Outcome, Is.EqualTo(row.Outcome), row.Name);
                Assert.That(Summary(plan), Is.EqualTo(row.Steps), row.Name);
                Assert.That(core.Current, Is.EqualTo(row.Current), row.Name);
                Assert.That(string.Join(",", core.History), Is.EqualTo(row.History), row.Name);
                Assert.That(string.Join(",", core.Modals), Is.EqualTo(row.Modals), row.Name);
            }
        }

        [Test]
        public void SamePageNavigationLeavesOpenModalsAlone()
        {
            RouterCore core = new RouterCore(NavSpec.Parse(DemoNav));
            core.Start();
            core.OpenModal("confirm");
            Assert.That(core.Navigate("home", NavMode.Push).Outcome, Is.EqualTo(NavOutcome.Ignored));
            Assert.That(core.Modals, Is.EqualTo(new[] { "confirm" }));
        }

        [Test]
        public void ReducedMotionMakesEveryTransitionInstantLikeTheQueryFlag()
        {
            RouterCore core = new RouterCore(NavSpec.Parse(DemoNav), new RouterOptions { ReducedMotion = () => true });
            core.Start();
            Assert.That(Summary(core.ActivateTrigger("pd-home-settings")), Is.EqualTo("Hide:home:slide-left:0:F|Show:settings:slide-left:0:F"));
            Assert.That(Summary(core.OpenModal("confirm")), Is.EqualTo(string.Empty));
            core.Back();
            Assert.That(Summary(core.ActivateTrigger("pd-home-quit")), Is.EqualTo("OpenModal:confirm:fade:0:F"));
        }

        [Test]
        public void ModalOnAnotherPageIsRejectedUnlikeThePrototypeApi()
        {
            RouterCore core = new RouterCore(NavSpec.Parse(DemoNav));
            core.Start();
            core.ActivateTrigger("pd-home-settings");
            Assert.That(core.OpenModal("confirm").Outcome, Is.EqualTo(NavOutcome.NotAllowed));
        }

        [Test]
        public void StartPageIsTheDataStartPage()
        {
            NavSpec spec = NavSpec.Parse(DemoNav);
            Assert.That(spec.Start, Is.EqualTo("home"));
            Assert.That(spec.Validate(), Is.Empty);
        }
    }
}
