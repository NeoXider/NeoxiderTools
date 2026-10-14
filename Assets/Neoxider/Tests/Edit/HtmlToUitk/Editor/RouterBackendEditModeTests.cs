using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using Neo.HtmlToUitk.Navigation;

namespace Neo.HtmlToUitk.Tests
{
    public class RouterBackendEditModeTests
    {
        private const string NavJson = @"{
  ""schema"": 1, ""document"": ""App"", ""start"": ""home"",
  ""pages"": [
    { ""id"": ""home"", ""element"": ""app-home"", ""focus"": ""app-home-settings"" },
    { ""id"": ""settings"", ""element"": ""app-settings"" } ],
  ""edges"": [
    { ""from"": ""home"", ""trigger"": ""app-home-settings"", ""action"": ""navigate"", ""to"": ""settings"", ""mode"": ""push"", ""transition"": ""slide-left"", ""ms"": 200 },
    { ""from"": ""settings"", ""trigger"": ""app-settings-back"", ""action"": ""back"" },
    { ""from"": ""home"", ""trigger"": ""app-home-quit"", ""action"": ""openModal"", ""to"": ""confirm"" } ],
  ""modals"": [ { ""id"": ""confirm"", ""element"": ""app-confirm"", ""ownerPage"": ""home"", ""closeTriggers"": [""app-confirm-cancel""] } ],
  ""tabs"": [ { ""group"": ""settings-tabs"", ""page"": ""settings"", ""default"": ""app-tab-audio"", ""tabs"": [
    { ""tab"": ""app-tab-audio"", ""panel"": ""app-panel-audio"" }, { ""tab"": ""app-tab-video"", ""panel"": ""app-panel-video"" } ] } ]
}";

        private sealed class Recorder : IRouterHook
        {
            public readonly List<string> Events = new List<string>();

            public void OnRouterEvent(in RouterEvent routerEvent)
            {
                Events.Add(routerEvent.Kind + ":" + routerEvent.Id);
            }
        }

        private static VisualElement BuildTree()
        {
            VisualElement root = new VisualElement { name = "app-root" };
            VisualElement home = new VisualElement { name = "app-home" };
            home.Add(new Button { name = "app-home-settings" });
            home.Add(new Button { name = "app-home-quit" });
            home.Add(new VisualElement { name = "app-confirm" });
            VisualElement settings = new VisualElement { name = "app-settings" };
            settings.Add(new Button { name = "app-settings-back" });
            settings.Add(new Button { name = "app-tab-audio" });
            settings.Add(new Button { name = "app-tab-video" });
            settings.Add(new VisualElement { name = "app-panel-audio" });
            settings.Add(new VisualElement { name = "app-panel-video" });
            root.Add(home);
            root.Add(settings);
            return root;
        }

        private static bool Visible(VisualElement root, string name)
        {
            VisualElement element = root.Q<VisualElement>(name);
            return !element.ClassListContains("is-hidden") && element.style.display != DisplayStyle.None;
        }

        [Test]
        public void StartShowsOnlyTheStartPageAndTheDefaultTab()
        {
            VisualElement root = BuildTree();
            UitkRouter router = new UitkRouter(NavSpec.Parse(NavJson), new UiDocumentPageBackend(root));
            Assert.That(router.Start(), Is.True);
            Assert.That(Visible(root, "app-home"), Is.True);
            Assert.That(Visible(root, "app-settings"), Is.False);
            Assert.That(Visible(root, "app-confirm"), Is.False);
            Assert.That(Visible(root, "app-panel-audio"), Is.True);
            Assert.That(Visible(root, "app-panel-video"), Is.False);
            Assert.That(root.Q<VisualElement>("app-tab-audio").ClassListContains("is-active"), Is.True);
        }

        [Test]
        public void PushAndBackSwitchPagesAndFireEventsInOrder()
        {
            VisualElement root = BuildTree();
            UitkRouter router = new UitkRouter(NavSpec.Parse(NavJson), new UiDocumentPageBackend(root));
            Recorder recorder = new Recorder();
            router.AddHook(recorder);
            router.Start();
            recorder.Events.Clear();

            Assert.That(router.Activate("app-home-settings"), Is.True);
            Assert.That(router.Current, Is.EqualTo("settings"));
            Assert.That(Visible(root, "app-settings"), Is.True);
            Assert.That(Visible(root, "app-home"), Is.False);
            Assert.That(recorder.Events, Is.EqualTo(new[] { "Navigated:settings", "WillHide:home", "WillShow:settings", "Hidden:home", "Shown:settings" }));

            Assert.That(router.Back(), Is.True);
            Assert.That(router.Current, Is.EqualTo("home"));
            Assert.That(Visible(root, "app-home"), Is.True);
            Assert.That(router.Back(), Is.False);
        }

        [Test]
        public void ModalAndTabsFollowTheSpec()
        {
            VisualElement root = BuildTree();
            UitkRouter router = new UitkRouter(NavSpec.Parse(NavJson), new UiDocumentPageBackend(root));
            router.Start();
            Assert.That(router.Activate("app-home-quit"), Is.True);
            Assert.That(Visible(root, "app-confirm"), Is.True);
            Assert.That(router.Activate("app-confirm-cancel"), Is.True);
            Assert.That(Visible(root, "app-confirm"), Is.False);

            router.Activate("app-home-settings");
            Assert.That(router.Activate("app-tab-video"), Is.True);
            Assert.That(Visible(root, "app-panel-video"), Is.True);
            Assert.That(Visible(root, "app-panel-audio"), Is.False);
            Assert.That(root.Q<VisualElement>("app-tab-audio").ClassListContains("is-active"), Is.False);
        }

        [Test]
        public void WireTriggersFindsEveryTriggerElement()
        {
            VisualElement root = BuildTree();
            UitkRouter router = new UitkRouter(NavSpec.Parse(NavJson), new UiDocumentPageBackend(root));
            router.Start();
            router.WireTriggers();
            Assert.That(router.MissingTriggers, Is.EqualTo(new[] { "app-confirm-cancel" }));
            root.Q<VisualElement>("app-confirm").Add(new Button { name = "app-confirm-cancel" });
            router.WireTriggers();
            Assert.That(router.MissingTriggers, Is.Empty);
            router.UnwireTriggers();
        }

        [Test]
        public void MissingElementsAreReportedByTheBackend()
        {
            VisualElement root = new VisualElement();
            UiDocumentPageBackend backend = new UiDocumentPageBackend(root);
            new UitkRouter(NavSpec.Parse(NavJson), backend);
            Assert.That(backend.MissingElements, Contains.Item("app-home"));
        }
    }
}
