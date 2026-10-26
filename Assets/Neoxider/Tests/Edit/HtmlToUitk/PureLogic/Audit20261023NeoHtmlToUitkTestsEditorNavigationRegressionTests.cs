using System.Linq;
using NUnit.Framework;
using Neo.HtmlToUitk.Navigation;

namespace Neo.HtmlToUitk.PureLogicTests
{
    public sealed class Audit20261023NeoHtmlToUitkTestsEditorNavigationRegressionTests
    {
        private const string Navigation = "{\"schema\":1,\"start\":\"home\",\"pages\":[{\"id\":\"home\",\"element\":\"home\"},{\"id\":\"middle\",\"element\":\"middle\"},{\"id\":\"top\",\"element\":\"top\"}],\"edges\":[],\"modals\":[],\"tabs\":[]}";

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ShowingExistingMiddlePagePreservesEarlierHistory(bool layered, bool showClearsStack)
        {
            RouterOptions options = new RouterOptions
            {
                ShowToExistingPopsToIt = true,
                PushKeepsPreviousVisible = layered,
                ShowClearsStack = showClearsStack
            };
            RouterCore core = new RouterCore(NavSpec.Parse(Navigation), options);
            Assert.That(core.Start().Succeeded, Is.True);
            Assert.That(core.Navigate("middle", NavMode.Push).Succeeded, Is.True);
            Assert.That(core.Navigate("top", NavMode.Push).Succeeded, Is.True);

            NavPlan plan = core.Navigate("middle", NavMode.Show);

            Assert.That(plan.Outcome, Is.EqualTo(NavOutcome.Done));
            Assert.That(core.Stack, Is.EqualTo(new[] { "home", "middle" }));
            Assert.That(core.History, Is.EqualTo(new[] { "home" }));
            Assert.That(plan.Steps.Select(step => step.Kind + ":" + step.Id), Is.EqualTo(layered
                ? new[] { "HidePage:top" }
                : new[] { "HidePage:top", "ShowPage:middle" }));
            Assert.That(core.Back().Succeeded, Is.True);
            Assert.That(core.Current, Is.EqualTo("home"));
            Assert.That(core.VisitLog, Is.EqualTo(new[] { "home", "middle", "top", "middle", "home" }));
        }
    }
}
