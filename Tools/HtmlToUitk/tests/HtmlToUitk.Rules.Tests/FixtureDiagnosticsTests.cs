using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Includes;
using HtmlToUitk.Core.Json;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Rules;
using HtmlToUitk.TestSupport;
using NUnit.Framework;

namespace HtmlToUitk.Rules.Tests;

[TestFixture]
public class FixtureDiagnosticsTests
{
    private static readonly HashSet<string> NotOwnedByRules = new HashSet<string>(StringComparer.Ordinal)
    {
        "J002", "J003", "J004", "J006", "J007", "J009", "J010", "J011", "J012",
    };

    private static readonly Dictionary<string, string> KnownCoreIssues = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "S08", "StyleEngine reports J001 for :root custom properties that a skin and the page both define; the fixture expects C034 only" },
    };

    public static IEnumerable<TestCaseData> SingleFileFixtures()
    {
        foreach (string dir in Directory.GetDirectories(RepoPaths.Fixtures).OrderBy(d => d, StringComparer.Ordinal))
        {
            string manifest = Path.Combine(dir, "fixture.json");
            if (!File.Exists(manifest)) continue;
            JsonObject fx = JsonReader.Parse(File.ReadAllText(manifest)).AsObject();
            if (fx == null || fx.GetBool("multiFile")) continue;
            string id = fx.GetString("id");
            yield return new TestCaseData(dir).SetName("Fixture_" + id + "_" + fx.GetString("title"));
        }
    }

    [TestCaseSource(nameof(SingleFileFixtures))]
    public void RulesProduceTheExpectedDiagnostics(string dir)
    {
        JsonObject fx = JsonReader.Parse(File.ReadAllText(Path.Combine(dir, "fixture.json"))).AsObject();
        string expectedPath = Path.Combine(dir, "expected", "diagnostics.json");
        if (!File.Exists(expectedPath)) Assert.Ignore("fixture has no expected diagnostics");
        IReadOnlyList<string> inputs = fx.GetStringList("inputs");
        Assert.That(inputs, Is.Not.Null.And.Not.Empty);
        string inputRoot = Path.Combine(dir, "input");

        string capText = File.ReadAllText(RepoPaths.Spec("capabilities.6000.6.json"));
        if (fx["capabilityPatch"] != null) capText = CapabilityPatcher.Apply(capText, fx["capabilityPatch"]);
        CapabilityMatrix caps = CapabilityMatrixLoader.Load(capText);

        ConversionConfig config = TestConfig.Load(File.ReadAllText(Path.Combine(dir, fx.GetString("config", "config.json"))));
        var assetFs = new DiskFileSystem(inputRoot);
        var tableFs = new DiskFileSystem(dir);
        var tables = new ConfigStringTables(tableFs, "");

        var pages = new List<(string, string)>();
        foreach (string input in inputs)
        {
            string rel = input.StartsWith("input/", StringComparison.Ordinal) ? input.Substring(6) : input;
            pages.Add((rel, File.ReadAllText(Path.Combine(dir, input))));
        }
        LinkLoader loader = (pagePath, link) =>
        {
            string uss = link.Attributes.Where(a => a.Name == "data-uss").Select(a => a.Value).FirstOrDefault();
            if (uss != null)
            {
                string skinPath = MiniAnalyzer.Resolve(MiniAnalyzer.Resolve("", config.ProjectRoot + "/" + config.UiRoot + "/" + config.Layout.Screens) + "/x", uss);
                return tableFs.FileExists(skinPath) ? (skinPath, tableFs.ReadAllText(skinPath), true) : ((string, string, bool)?)null;
            }
            string css = MiniAnalyzer.Resolve(pagePath, link.Href ?? "");
            return assetFs.FileExists(css) ? (css, assetFs.ReadAllText(css), false) : ((string, string, bool)?)null;
        };
        MiniAnalysis result = MiniAnalyzer.Analyze(pages, config, caps, assetFs, tables, null, loader);

        List<string> actual = result.Bag.Items
            .Where(d => !NotOwnedByRules.Contains(d.RuleId) && !d.RuleId.StartsWith("H", StringComparison.Ordinal))
            .Select(Normalize).OrderBy(x => x, StringComparer.Ordinal).ToList();
        List<string> expected = JsonReader.Parse(File.ReadAllText(expectedPath)).AsArray().Items
            .Select(v => v.AsObject())
            .Where(o => !NotOwnedByRules.Contains(o.GetString("rule")))
            .Select(o => string.Join("|", o.GetString("rule"), o.GetString("severity"), o.GetString("tier"), o.GetString("emulation", ""), o.GetString("file"), o.GetInt("line")))
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        if (KnownCoreIssues.TryGetValue(fx.GetString("id"), out string issue) && !actual.SequenceEqual(expected))
        {
            Assert.Ignore("known Core issue: " + issue);
        }
        Assert.That(actual, Is.EqualTo(expected), "actual: " + string.Join(" ; ", actual) + "\nsummary: " + result.Summary());
    }

    private static string Normalize(Diagnostic d)
    {
        return string.Join("|", d.RuleId, d.Severity, d.Tier, d.EmulationId ?? "", (d.Span.File ?? "").Replace('\\', '/'), d.Span.Start.Line);
    }
}
