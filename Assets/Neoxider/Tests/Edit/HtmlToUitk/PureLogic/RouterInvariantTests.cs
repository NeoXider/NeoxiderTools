using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Neo.HtmlToUitk.Navigation;

namespace Neo.HtmlToUitk.PureLogicTests
{
    public class RouterInvariantTests
    {
        private const string Nav = @"{
  ""schema"": 1, ""document"": ""D"", ""start"": ""a"",
  ""pages"": [ { ""id"": ""a"", ""element"": ""pa"" }, { ""id"": ""b"", ""element"": ""pb"" }, { ""id"": ""c"", ""element"": ""pc"" }, { ""id"": ""d"", ""element"": ""pd"" } ],
  ""edges"": [
    { ""from"": ""a"", ""trigger"": ""a-b"", ""action"": ""navigate"", ""to"": ""b"", ""mode"": ""push"", ""transition"": ""slide-left"" },
    { ""from"": ""b"", ""trigger"": ""b-c"", ""action"": ""navigate"", ""to"": ""c"", ""mode"": ""push"" },
    { ""from"": ""c"", ""trigger"": ""c-d"", ""action"": ""navigate"", ""to"": ""d"" },
    { ""from"": ""d"", ""trigger"": ""d-a"", ""action"": ""navigate"", ""to"": ""a"" },
    { ""from"": ""b"", ""trigger"": ""b-back"", ""action"": ""back"" },
    { ""from"": ""c"", ""trigger"": ""c-back"", ""action"": ""back"" } ],
  ""modals"": [ { ""id"": ""m1"", ""element"": ""pm1"", ""ownerPage"": ""a"" }, { ""id"": ""m2"", ""element"": ""pm2"", ""ownerPage"": ""a"" }, { ""id"": ""m3"", ""element"": ""pm3"", ""ownerPage"": ""c"" } ],
  ""tabs"": [ { ""group"": ""g"", ""page"": ""b"", ""tabs"": [ { ""tab"": ""t1"", ""panel"": ""p1"" }, { ""tab"": ""t2"", ""panel"": ""p2"" } ] } ]
}";

        private sealed class Model
        {
            public readonly HashSet<string> VisiblePages = new HashSet<string>();
            public readonly HashSet<string> OpenModals = new HashSet<string>();
            public readonly Dictionary<string, string> Tabs = new Dictionary<string, string>();

            public void Apply(NavPlan plan)
            {
                foreach (NavStep step in plan.Steps)
                {
                    switch (step.Kind)
                    {
                        case NavStepKind.ShowPage: VisiblePages.Add(step.Id); break;
                        case NavStepKind.HidePage: Assert.That(VisiblePages.Remove(step.Id), Is.True, "hid a page that was not visible: " + step.Id); break;
                        case NavStepKind.OpenModal: Assert.That(OpenModals.Add(step.Id), Is.True, "opened an open modal: " + step.Id); break;
                        case NavStepKind.CloseModal: Assert.That(OpenModals.Remove(step.Id), Is.True, "closed a closed modal: " + step.Id); break;
                        case NavStepKind.SelectTab: Tabs[step.Group] = step.Tab; break;
                    }
                }
            }
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(5, 3)]
        public void RandomOperationsKeepPlansAndStateConsistent(int seed, int variant)
        {
            bool layered = variant >= 2;
            RouterOptions options = variant == 0 ? new RouterOptions()
                : variant == 1 ? new RouterOptions { ShowClearsStack = false }
                : variant == 2 ? new RouterOptions { PushKeepsPreviousVisible = true }
                : new RouterOptions { ShowToExistingPopsToIt = true, PushKeepsPreviousVisible = true };
            RouterCore core = new RouterCore(NavSpec.Parse(Nav), options);
            Model model = new Model();
            model.Apply(core.Start());
            Random random = new Random(seed);
            string[] pages = { "a", "b", "c", "d" };
            string[] triggers = { "a-b", "b-c", "c-d", "d-a", "b-back", "c-back" };
            string[] modals = { "m1", "m2", "m3", "nope" };
            string[] tabs = { "t1", "t2", "t3" };

            for (int i = 0; i < 4000; i++)
            {
                NavPlan plan;
                switch (random.Next(7))
                {
                    case 0: plan = core.Navigate(pages[random.Next(pages.Length)], NavMode.Push); break;
                    case 1: plan = core.Navigate(pages[random.Next(pages.Length)], NavMode.Show); break;
                    case 2: plan = core.Back(); break;
                    case 3: plan = core.OpenModal(modals[random.Next(modals.Length)]); break;
                    case 4: plan = core.CloseModal(random.Next(2) == 0 ? null : modals[random.Next(modals.Length)]); break;
                    case 5: plan = core.SelectTab("g", tabs[random.Next(tabs.Length)]); break;
                    default: plan = core.ActivateTrigger(triggers[random.Next(triggers.Length)]); break;
                }

                if (plan.Succeeded)
                    model.Apply(plan);
                else
                    Assert.That(plan.Steps.Count, Is.EqualTo(0), "a failed plan must not carry steps");

                Assert.That(core.Current, Is.EqualTo(core.Stack.Last()));
                Assert.That(core.Depth, Is.GreaterThanOrEqualTo(1));
                if (layered)
                    Assert.That(model.VisiblePages.OrderBy(x => x), Is.EqualTo(core.Stack.Distinct().OrderBy(x => x)), "step " + i);
                else
                    Assert.That(model.VisiblePages.ToArray(), Is.EqualTo(new[] { core.Current }), "step " + i);
                Assert.That(model.OpenModals.OrderBy(x => x), Is.EqualTo(core.Modals.OrderBy(x => x)), "step " + i);
                foreach (string modal in core.Modals)
                    Assert.That(core.Spec.Modal(modal).OwnerPage, Is.EqualTo(core.Current), "modal outlived its page at step " + i);
                Assert.That(model.Tabs.TryGetValue("g", out string tab) ? tab : "t1", Is.EqualTo(core.SelectedTab("g")));
                Assert.That(core.VisitLog.Last(), Is.EqualTo(core.Current));
                Assert.That(core.History.Concat(new[] { core.Current }), Is.EqualTo(core.Stack));
            }
        }
    }

    public class UiKitMappingTests
    {
        private const string Nav = @"{
  ""schema"": 1, ""document"": ""App"", ""start"": ""home"",
  ""pages"": [
    { ""id"": ""home"", ""element"": ""app-home"", ""transition"": { ""in"": ""fade"", ""out"": ""slide-left"", ""ms"": 200 } },
    { ""id"": ""settings"", ""element"": ""app-settings"" } ],
  ""edges"": [
    { ""from"": ""home"", ""trigger"": ""button_settings"", ""action"": ""navigate"", ""to"": ""settings"", ""mode"": ""push"", ""transition"": ""slide-left"", ""ms"": 260 },
    { ""from"": ""settings"", ""trigger"": ""button_back"", ""action"": ""back"" },
    { ""from"": ""home"", ""trigger"": ""button_quit"", ""action"": ""openModal"", ""to"": ""confirm"" },
    { ""from"": ""home"", ""trigger"": ""button_about"", ""action"": ""navigate"", ""to"": ""settings"", ""mode"": ""show"" } ],
  ""modals"": [ { ""id"": ""confirm"", ""element"": ""popup_confirm"", ""ownerPage"": ""home"", ""closeTriggers"": [""button_cancel""] } ],
  ""tabs"": [ { ""group"": ""t"", ""page"": ""settings"", ""tabs"": [ { ""tab"": ""x"", ""panel"": ""y"" } ] } ]
}";

        [Test]
        public void MapsPagesPresetsAndStart()
        {
            UiKitConfigPlan plan = UiKitMapping.Map(NavSpec.Parse(Nav));
            Assert.That(plan.Pages.Count, Is.EqualTo(2));
            UiKitPagePlan home = plan.Pages.Single(p => p.PageId == "home");
            Assert.That(home.IsStart, Is.True);
            Assert.That(home.ShowPreset, Is.EqualTo("fade"));
            Assert.That(home.HidePreset, Is.EqualTo("slide-left"));
            Assert.That(plan.Pages.Single(p => p.PageId == "settings").ShowPreset, Is.Null);
        }

        [Test]
        public void MapsEdgesAndModalsToUiKitNavigation()
        {
            UiKitConfigPlan plan = UiKitMapping.Map(NavSpec.Parse(Nav));
            UiKitNavigationPlan push = plan.Navigation.Single(n => n.ButtonPath == "home/button_settings");
            Assert.That(push.Action, Is.EqualTo(UiKitAction.Push));
            Assert.That(push.TargetId, Is.EqualTo("settings"));
            Assert.That(plan.Navigation.Single(n => n.ButtonPath == "settings/button_back").Action, Is.EqualTo(UiKitAction.Pop));
            UiKitNavigationPlan open = plan.Navigation.Single(n => n.ButtonPath == "home/button_quit");
            Assert.That(open.Action, Is.EqualTo(UiKitAction.OpenPopup));
            Assert.That(open.TargetId, Is.EqualTo("home/popup_confirm"));
            UiKitNavigationPlan close = plan.Navigation.Single(n => n.ButtonPath == "home/popup_confirm/button_cancel");
            Assert.That(close.Action, Is.EqualTo(UiKitAction.ClosePopup));
            Assert.That(close.TargetId, Is.Empty);
            Assert.That(plan.Navigation.Single(n => n.ButtonPath == "home/button_about").Action, Is.EqualTo(UiKitAction.Show));
        }

        [Test]
        public void ReportsWhatUiKitCannotExpress()
        {
            UiKitConfigPlan plan = UiKitMapping.Map(NavSpec.Parse(Nav));
            Assert.That(plan.Warnings.Any(w => w.Contains("tabs")), Is.True);
            Assert.That(plan.Warnings.Any(w => w.Contains("per-edge")), Is.True);
            Assert.That(plan.Warnings.Any(w => w.Contains("ms")), Is.True);
        }

        [Test]
        public void CustomPopupNameResolverIsUsed()
        {
            UiKitConfigPlan plan = UiKitMapping.Map(NavSpec.Parse(Nav), modal => "dialog_" + modal.Id);
            Assert.That(plan.Navigation.Single(n => n.ButtonPath == "home/button_quit").TargetId, Is.EqualTo("home/dialog_confirm"));
        }

        [Test]
        public void UnknownTargetsProduceWarningsInsteadOfEntries()
        {
            UiKitConfigPlan plan = UiKitMapping.Map(NavSpec.Parse(@"{ ""schema"": 1, ""document"": ""D"", ""start"": ""a"", ""pages"": [ { ""id"": ""a"", ""element"": ""a"" } ],
              ""edges"": [ { ""from"": ""a"", ""trigger"": ""x"", ""action"": ""navigate"", ""to"": ""zzz"" }, { ""from"": ""a"", ""trigger"": ""y"", ""action"": ""openModal"", ""to"": ""none"" } ] }"));
            Assert.That(plan.Navigation, Is.Empty);
            Assert.That(plan.Warnings.Count, Is.EqualTo(2));
        }

        [Test]
        public void DuplicateButtonKeepsFirstEdge()
        {
            UiKitConfigPlan plan = UiKitMapping.Map(NavSpec.Parse(@"{ ""schema"": 1, ""document"": ""D"", ""start"": ""a"", ""pages"": [ { ""id"": ""a"", ""element"": ""a"" }, { ""id"": ""b"", ""element"": ""b"" } ],
              ""edges"": [ { ""from"": ""a"", ""trigger"": ""x"", ""action"": ""navigate"", ""to"": ""b"" }, { ""from"": ""a"", ""trigger"": ""x"", ""action"": ""navigate"", ""to"": ""a"", ""mode"": ""push"" } ] }"));
            Assert.That(plan.Navigation.Count, Is.EqualTo(1));
            Assert.That(plan.Navigation[0].TargetId, Is.EqualTo("b"));
            Assert.That(plan.Warnings.Any(w => w.Contains("more than one edge")), Is.True);
        }
    }
}
