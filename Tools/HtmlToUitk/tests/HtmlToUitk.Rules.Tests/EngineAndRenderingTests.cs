using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Json;
using HtmlToUitk.Rules;
using HtmlToUitk.TestSupport;
using NUnit.Framework;

namespace HtmlToUitk.Rules.Tests;

[TestFixture]
public class EngineTests
{
    [Test]
    public void EngineFillsMissingConfigCatalogAndCapabilities()
    {
        var input = new AnalysisInput();
        var bag = new DiagnosticBag();
        RuleEngine.CreateDefault().Run(input, bag);
        Assert.That(input.Config, Is.Not.Null);
        Assert.That(input.Catalog, Is.Not.Null);
        Assert.That(input.Capabilities.UnityVersion, Is.EqualTo("6000.6.0f1"));
        Assert.That(bag.Count(Severity.Error), Is.EqualTo(0));
    }

    [Test]
    public void UnknownUnityVersionWithoutMatrixThrows()
    {
        var input = new AnalysisInput { Config = new ConversionConfig() };
        input.Config.Unity.Version = "1999.1";
        Assert.That(() => RuleEngine.CreateDefault().Run(input, new DiagnosticBag()), Throws.InvalidOperationException);
    }

    [Test]
    public void ConfigVersionSelectsTheEmbeddedMatrix()
    {
        var input = new AnalysisInput { Config = new ConversionConfig() };
        input.Config.Unity.Version = "6000.6.0f1";
        RuleEngine.CreateDefault().Run(input, new DiagnosticBag());
        Assert.That(input.Capabilities, Is.Not.Null);
    }

    [Test]
    public void RunNeedsInputAndSink()
    {
        RuleEngine engine = RuleEngine.CreateDefault();
        Assert.That(() => engine.Run(null, new DiagnosticBag()), Throws.ArgumentNullException);
        Assert.That(() => engine.Run(new AnalysisInput(), null), Throws.ArgumentNullException);
    }

    [Test]
    public void ConstructorsRejectNulls()
    {
        Assert.That(() => new RuleEngine(null, new TierPlanner(), ConstructDetectors.Default(), DefaultRules.Create()), Throws.ArgumentNullException);
        Assert.That(() => new RuleEngine(RuleCatalog.Embedded, null, ConstructDetectors.Default(), DefaultRules.Create()), Throws.ArgumentNullException);
        Assert.That(() => new RuleEngine(RuleCatalog.Embedded, new TierPlanner(), null, DefaultRules.Create()), Throws.ArgumentNullException);
        Assert.That(() => new RuleEngine(RuleCatalog.Embedded, new TierPlanner(), ConstructDetectors.Default(), null), Throws.ArgumentNullException);
    }

    [Test]
    public void OutputIsDeterministic()
    {
        string css = ".a { display: grid; gap: 1px; width: 10vw; color: inherit; }\n.b:not(.c) { z-index: 1; }";
        string a = DiagnosticsJson.Write(DiagnosticsReport.Build(Q.Css(css).Bag));
        string b = DiagnosticsJson.Write(DiagnosticsReport.Build(Q.Css(css).Bag));
        Assert.That(b, Is.EqualTo(a));
    }

    [Test]
    public void PlanProducesOneDecisionPerConstruct()
    {
        MiniAnalysis m = Q.Css(".a { display: grid; width: 10vw; }\n.b:first-child { z-index: 1; }");
        Assert.That(m.Decisions.Count, Is.EqualTo(m.Constructs.Count));
        Assert.That(m.Constructs.Count, Is.EqualTo(4));
    }

    [Test]
    public void CustomRuleListIsRun()
    {
        var seen = new List<string>();
        var engine = new RuleEngine(RuleCatalog.Embedded, new TierPlanner(), new IConstructDetector[0], new IRule[] { new Probe(seen) });
        engine.Run(new AnalysisInput(), new DiagnosticBag());
        Assert.That(seen, Is.EqualTo(new[] { "probe" }));
    }

    private sealed class Probe : IRule
    {
        private readonly List<string> _seen;

        public Probe(List<string> seen)
        {
            _seen = seen;
        }

        public string Id => "probe";

        public void Evaluate(RuleContext context)
        {
            _seen.Add(Id);
            Assert.That(context.Constructs, Is.Empty);
            Assert.That(context.Decisions, Is.Empty);
            Assert.That(context.Catalog, Is.Not.Null);
            Assert.That(context.Capabilities, Is.Not.Null);
        }
    }

    [Test]
    public void DisablingEveryConstructRuleSilencesThePlan()
    {
        ConversionConfig cfg = Q.Cfg("{\"rules\":{\"disable\":[\"C001\",\"C002\",\"C008\",\"C021\"]}}");
        MiniAnalysis m = Q.Css(".a { display: grid; gap: 1px; width: 10vw; z-index: 1; }", "<div></div>", cfg);
        Assert.That(m.Bag.Items.Where(d => d.RuleId.StartsWith("C0")).Select(d => d.RuleId), Is.Empty);
    }

    [Test]
    public void SeveritiesAreOrderedErrorsFirst()
    {
        MiniAnalysis m = Q.Css(".a { display: grid; color: inherit; position: sticky; }");
        string[] order = m.Bag.Sorted().Select(d => d.Severity.ToString()).ToArray();
        Assert.That(order, Is.EqualTo(order.OrderByDescending(s => s == "Error" ? 2 : s == "Warning" ? 1 : 0).ToArray()));
    }

    [Test]
    public void DetectAndPlanAreAvailableWithoutRunning()
    {
        MiniAnalysis m = Q.Css(".a { display: grid; }");
        RuleEngine engine = RuleEngine.CreateDefault();
        IReadOnlyList<Construct> constructs = engine.Detect(m.Input);
        Assert.That(engine.Plan(m.Input, constructs).Single().Decision.EmulationId, Is.EqualTo("grid-flatten"));
    }
}

[TestFixture]
public class ReporterTests
{
    private static RuleReporter Make(out DiagnosticBag bag, string configJson = null)
    {
        bag = new DiagnosticBag();
        return new RuleReporter(RuleCatalog.Embedded, configJson == null ? new ConversionConfig() : Q.Cfg(configJson), bag);
    }

    [Test]
    public void ReportUsesCatalogDefaults()
    {
        RuleReporter r = Make(out DiagnosticBag bag);
        Diagnostic d = r.Report("N002", new SourceSpan("a.html", new SourcePos(0, 3, 4), new SourcePos(5, 3, 9)), "name=\"X\"", "bad");
        Assert.That(d.Severity, Is.EqualTo(Severity.Warning));
        Assert.That(d.Tier, Is.EqualTo(Tier.Native));
        Assert.That(d.Why, Is.Not.Empty);
        Assert.That(d.Suggestion, Is.Not.Empty);
        Assert.That(bag.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public void ExplicitValuesOverrideTheDefaults()
    {
        RuleReporter r = Make(out DiagnosticBag bag);
        Diagnostic d = r.Report("N002", default(SourceSpan), "c", "m", "custom fix", Severity.Error, Tier.Reject, "emu", new[] { new SourceSpan("b", new SourcePos(1, 2, 3), new SourcePos(1, 2, 3)) });
        Assert.That(d.Severity, Is.EqualTo(Severity.Error));
        Assert.That(d.Tier, Is.EqualTo(Tier.Reject));
        Assert.That(d.Suggestion, Is.EqualTo("custom fix"));
        Assert.That(d.EmulationId, Is.EqualTo("emu"));
        Assert.That(d.Related.Count, Is.EqualTo(1));
    }

    [Test]
    public void EmptyMessageFallsBackToTheTitle()
    {
        RuleReporter r = Make(out DiagnosticBag bag);
        Assert.That(r.Report("N002", default(SourceSpan), "c", "").Message, Is.EqualTo(RuleCatalog.Embedded.Get("N002").Title));
    }

    [Test]
    public void DisabledRulesReturnNull()
    {
        RuleReporter r = Make(out DiagnosticBag bag, "{\"rules\":{\"disable\":[\"N002\"]}}");
        Assert.That(r.IsDisabled("N002"), Is.True);
        Assert.That(r.Report("N002", default(SourceSpan), "c", "m"), Is.Null);
        Assert.That(bag.Items, Is.Empty);
    }

    [Test]
    public void UnknownRulesThrow()
    {
        RuleReporter r = Make(out DiagnosticBag bag);
        Assert.That(() => r.Report("Z999", default(SourceSpan), "c", "m"), Throws.InvalidOperationException);
        Assert.That(() => r.Rule("Z999"), Throws.InvalidOperationException);
    }

    [Test]
    public void SilentDecisionsReportNothing()
    {
        RuleReporter r = Make(out DiagnosticBag bag);
        var decision = new TierDecision { Tier = Tier.ConvertTime, EmulationId = "shorthand-expand", RuleId = null };
        Assert.That(r.ReportDecision(Q.Construct("shorthand:inset"), decision), Is.Null);
        Assert.That(bag.Items, Is.Empty);
    }

    [Test]
    public void ConstructorRejectsNulls()
    {
        Assert.That(() => new RuleReporter(null, new ConversionConfig(), new DiagnosticBag()), Throws.ArgumentNullException);
        Assert.That(() => new RuleReporter(RuleCatalog.Embedded, new ConversionConfig(), null), Throws.ArgumentNullException);
    }

    [Test]
    public void ReporterExposesItsInputs()
    {
        RuleReporter r = Make(out DiagnosticBag bag);
        Assert.That(r.Catalog, Is.SameAs(RuleCatalog.Embedded));
        Assert.That(r.Config, Is.Not.Null);
        Assert.That(r.EmulatedSeverity("C002"), Is.EqualTo(Severity.Info));
    }

    [Test]
    public void ApproximateConvertTimeIsAWarning()
    {
        RuleReporter r = Make(out DiagnosticBag bag);
        Construct c = Q.Construct("prop:gap", "text", "gap: 4px", "approximate", "true");
        var decision = new TierDecision { Tier = Tier.ConvertTime, EmulationId = "gap-margins", RuleId = "C002", Reason = "r" };
        Assert.That(r.ReportDecision(c, decision).Severity, Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void RejectDecisionsUseTheRuleSeverityAndFix()
    {
        RuleReporter r = Make(out DiagnosticBag bag);
        var decision = new TierDecision { Tier = Tier.Reject, RuleId = "C016", Reason = "no" };
        Diagnostic d = r.ReportDecision(Q.Construct("selector:type", "text", "div"), decision);
        Assert.That(d.Severity, Is.EqualTo(Severity.Error));
        Assert.That(d.Suggestion, Is.EqualTo(RuleCatalog.Embedded.Get("C016").Fix));
        Assert.That(d.EmulationId, Is.Null);
    }
}

[TestFixture]
public class RenderingTests
{
    private static Diagnostic D(string rule, Severity severity, Tier tier, string file, int line, int column, string message, string emulation = null, string construct = "c", string suggestion = null)
    {
        return new Diagnostic
        {
            RuleId = rule,
            Severity = severity,
            Tier = tier,
            Span = new SourceSpan(file, new SourcePos(0, line, column), new SourcePos(0, line, column + 3)),
            Message = message,
            Construct = construct,
            Why = "why " + rule,
            Suggestion = suggestion ?? "fix " + rule,
            EmulationId = emulation,
        };
    }

    private static IEnumerable<Diagnostic> Sample()
    {
        yield return D("C001", Severity.Info, Tier.ConvertTime, "pages/home.html", 12, 5, "grid is flattened", "grid-flatten", "display: grid");
        yield return D("C016", Severity.Error, Tier.Reject, "pages/home.html", 3, 1, "type selector");
        yield return D("N002", Severity.Warning, Tier.Native, "pages/about.html", 7, 2, "not kebab");
    }

    [Test]
    public void VerdictsMapFromSeverities()
    {
        Assert.That(DiagnosticsReport.Build(new Diagnostic[0]).Verdict, Is.EqualTo(Verdict.Pass));
        Assert.That(DiagnosticsReport.Build(new[] { D("L003", Severity.Info, Tier.Native, "a", 1, 1, "m") }).Verdict, Is.EqualTo(Verdict.Pass));
        Assert.That(DiagnosticsReport.Build(new[] { D("N002", Severity.Warning, Tier.Native, "a", 1, 1, "m") }).Verdict, Is.EqualTo(Verdict.PassWithWarnings));
        Assert.That(DiagnosticsReport.Build(Sample()).Verdict, Is.EqualTo(Verdict.Fail));
    }

    [TestCase("PASS", 0, false)]
    [TestCase("PASS", 0, true)]
    [TestCase("WARN", 0, false)]
    [TestCase("WARN", 1, true)]
    [TestCase("FAIL", 1, false)]
    [TestCase("FAIL", 1, true)]
    public void ExitCodesFollowTheVerdict(string kind, int code, bool strict)
    {
        var items = new List<Diagnostic>();
        if (kind == "WARN") items.Add(D("N002", Severity.Warning, Tier.Native, "a", 1, 1, "m"));
        if (kind == "FAIL") items.Add(D("C016", Severity.Error, Tier.Reject, "a", 1, 1, "m"));
        Assert.That(DiagnosticsReport.Build(items).ExitCode(strict), Is.EqualTo(code));
    }

    [Test]
    public void ExitCodeConstants()
    {
        Assert.That(ExitCodes.Pass, Is.EqualTo(0));
        Assert.That(ExitCodes.Warnings, Is.EqualTo(1));
        Assert.That(ExitCodes.Fail, Is.EqualTo(1));
        Assert.That(ExitCodes.Usage, Is.EqualTo(2));
    }

    [Test]
    public void CountsAndOrdering()
    {
        DiagnosticsReport r = DiagnosticsReport.Build(Sample());
        Assert.That(r.Errors, Is.EqualTo(1));
        Assert.That(r.Warnings, Is.EqualTo(1));
        Assert.That(r.Infos, Is.EqualTo(1));
        Assert.That(r.Findings.Select(f => f.RuleId), Is.EqualTo(new[] { "C016", "N002", "C001" }));
    }

    [Test]
    public void DuplicatesAreRemoved()
    {
        Diagnostic d = D("C016", Severity.Error, Tier.Reject, "a", 1, 1, "m");
        Diagnostic copy = D("C016", Severity.Error, Tier.Reject, "a", 1, 1, "m");
        Assert.That(DiagnosticsReport.Build(new[] { d, copy, d }).Findings.Count, Is.EqualTo(1));
        Assert.That(DiagnosticsReport.Build(new[] { d, D("C016", Severity.Error, Tier.Reject, "a", 2, 1, "m") }).Findings.Count, Is.EqualTo(2));
    }

    [Test]
    public void TierTallyFollowsFindingsOrTheGivenTally()
    {
        DiagnosticsReport r = DiagnosticsReport.Build(Sample());
        Assert.That((r.Tiers.Native, r.Tiers.ConvertTime, r.Tiers.Runtime, r.Tiers.Reject), Is.EqualTo((1, 1, 0, 1)));
        var tally = new TierTally { Native = 41, ConvertTime = 6, Runtime = 1, Reject = 2 };
        Assert.That(DiagnosticsReport.Build(Sample(), new ReportOptions { Tiers = tally }).Tiers, Is.SameAs(tally));
    }

    [Test]
    public void TierTallyFromDecisions()
    {
        MiniAnalysis m = Q.Css(".a { display: grid; width: 10vw; position: sticky; }");
        TierTally t = TierTally.FromDecisions(m.Decisions);
        Assert.That(t.ConvertTime, Is.EqualTo(2));
        Assert.That(t.Reject, Is.EqualTo(1));
    }

    [Test]
    public void JsonMatchesTheDiagnosticsSchema()
    {
        string json = DiagnosticsJson.Write(DiagnosticsReport.Build(Sample(), new ReportOptions { NewKeys = new[] { "a.b" }, Source = "pages" }));
        string schema = File.ReadAllText(Path.Combine(RepoPaths.Root, "spec", "schemas", "diagnostics.schema.json"));
        Assert.That(MiniJsonSchema.Validate(schema, json), Is.Empty);
    }

    [Test]
    public void EmptyReportMatchesTheSchemaToo()
    {
        string json = DiagnosticsJson.Write(DiagnosticsReport.Build(new Diagnostic[0]));
        string schema = File.ReadAllText(Path.Combine(RepoPaths.Root, "spec", "schemas", "diagnostics.schema.json"));
        Assert.That(MiniJsonSchema.Validate(schema, json), Is.Empty);
        Assert.That(json, Does.Contain("\"verdict\": \"PASS\""));
        Assert.That(json, Does.Contain("\"findings\": []"));
    }

    [Test]
    public void JsonGolden()
    {
        string json = DiagnosticsJson.Write(DiagnosticsReport.Build(new[] { D("C001", Severity.Info, Tier.ConvertTime, "pages\\home.html", 12, 5, "grid is flattened", "grid-flatten", "display: grid") }));
        const string expected = "{\n  \"tool\": \"htmltouitk\",\n  \"version\": \"0.1.0\",\n  \"verdict\": \"PASS\",\n  \"counts\": {\n    \"error\": 0,\n    \"warning\": 0,\n    \"info\": 1\n  },\n  \"tiers\": {\n    \"native\": 0,\n    \"convertTime\": 1,\n    \"runtime\": 0,\n    \"reject\": 0\n  },\n  \"findings\": [\n    {\n      \"rule\": \"C001\",\n      \"severity\": \"Info\",\n      \"tier\": \"ConvertTime\",\n      \"emulation\": \"grid-flatten\",\n      \"file\": \"pages/home.html\",\n      \"line\": 12,\n      \"column\": 5,\n      \"endLine\": 12,\n      \"endColumn\": 8,\n      \"construct\": \"display: grid\",\n      \"message\": \"grid is flattened\",\n      \"why\": \"why C001\",\n      \"suggestion\": \"fix C001\",\n      \"related\": []\n    }\n  ],\n  \"newKeys\": []\n}\n";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void JsonCanOmitTiers()
    {
        string json = DiagnosticsJson.Write(DiagnosticsReport.Build(Sample()), false);
        Assert.That(json, Does.Not.Contain("\"tiers\""));
    }

    [Test]
    public void JsonListsRelatedSpans()
    {
        Diagnostic d = D("J001", Severity.Warning, Tier.Native, "p.html", 8, 1, "m");
        d.Related = new[] { new SourceSpan("css/a.css", new SourcePos(0, 3, 1), new SourcePos(0, 3, 2)) };
        string json = DiagnosticsJson.Write(DiagnosticsReport.Build(new[] { d }));
        Assert.That(json, Does.Contain("\"file\": \"css/a.css\""));
        Assert.That(json, Does.Contain("\"line\": 3"));
    }

    [Test]
    public void JsonUsesPosixPathsAndOmitsEmptyOptionalFields()
    {
        Diagnostic d = D("N002", Severity.Warning, Tier.Native, "a\\b.html", 1, 1, "m", null, "", "");
        d.Why = "";
        d.Suggestion = "";
        string json = DiagnosticsJson.Write(DiagnosticsReport.Build(new[] { d }));
        Assert.That(json, Does.Contain("a/b.html"));
        Assert.That(json, Does.Not.Contain("\"construct\""));
        Assert.That(json, Does.Not.Contain("\"emulation\""));
        Assert.That(json, Does.Not.Contain("\"suggestion\""));
    }

    [Test]
    public void TextRenderingGolden()
    {
        string text = DiagnosticsText.Render(DiagnosticsReport.Build(Sample()));
        const string expected =
            "pages/home.html:3:1: error C016 [Reject] type selector\n" +
            "    construct: c\n" +
            "    why: why C016\n" +
            "    fix: fix C016\n" +
            "pages/about.html:7:2: warning N002 [Native] not kebab\n" +
            "    construct: c\n" +
            "    why: why N002\n" +
            "    fix: fix N002\n" +
            "pages/home.html:12:5: info C001 [ConvertTime via grid-flatten] grid is flattened\n" +
            "    construct: display: grid\n" +
            "    why: why C001\n" +
            "    fix: fix C001\n" +
            "FAIL (1 error, 1 warning, 1 info)\n";
        Assert.That(text, Is.EqualTo(expected));
    }

    [Test]
    public void TextRenderingCanBeBrief()
    {
        string text = DiagnosticsText.Render(DiagnosticsReport.Build(Sample()), false);
        Assert.That(text.Split('\n').Length, Is.EqualTo(5));
        Assert.That(text, Does.Not.Contain("why:"));
    }

    [Test]
    public void TextRenderingShowsRelatedSpans()
    {
        Diagnostic d = D("J001", Severity.Warning, Tier.Native, "p.html", 8, 1, "m");
        d.Related = new[] { new SourceSpan("css/a.css", new SourcePos(0, 3, 1), new SourcePos(0, 3, 2)) };
        Assert.That(DiagnosticsText.Render(DiagnosticsReport.Build(new[] { d })), Does.Contain("see: css/a.css:3:1"));
    }

    [Test]
    public void SummaryUsesPlurals()
    {
        Assert.That(DiagnosticsText.Summary(DiagnosticsReport.Build(new Diagnostic[0])), Is.EqualTo("PASS (0 errors, 0 warnings, 0 infos)"));
        Assert.That(DiagnosticsText.Summary(DiagnosticsReport.Build(Sample())), Is.EqualTo("FAIL (1 error, 1 warning, 1 info)"));
    }

    [Test]
    public void ExpectedFormIsNormalisedAndSorted()
    {
        string json = DiagnosticsJson.WriteExpected(Sample(), false);
        string schema = File.ReadAllText(Path.Combine(RepoPaths.Root, "spec", "schemas", "expected-diagnostics.schema.json"));
        Assert.That(MiniJsonSchema.Validate(schema, json), Is.Empty);
        JsonArray array = JsonReader.Parse(json).AsArray();
        Assert.That(array.Items.Select(v => v.AsObject().GetString("rule")), Is.EqualTo(new[] { "C016", "N002", "C001" }));
        Assert.That(array[0].AsObject().Has("column"), Is.False);
        Assert.That(JsonReader.Parse(DiagnosticsJson.WriteExpected(Sample(), true)).AsArray()[0].AsObject().Has("column"), Is.True);
    }

    [Test]
    public void VersionAndSourceAreConfigurable()
    {
        string json = DiagnosticsJson.Write(DiagnosticsReport.Build(new Diagnostic[0], new ReportOptions { Version = "9.9.9", Source = "my job" }));
        Assert.That(json, Does.Contain("\"version\": \"9.9.9\""));
        Assert.That(json, Does.Contain("\"source\": \"my job\""));
    }

    [Test]
    public void RealFixtureOutputMatchesTheSchema()
    {
        MiniAnalysis m = Q.Css(".a { display: grid; position: sticky; width: 10; }\n.b:has(.c) { color: inherit; }");
        string json = DiagnosticsJson.Write(DiagnosticsReport.Build(m.Bag, new ReportOptions { Tiers = TierTally.FromDecisions(m.Decisions) }));
        string schema = File.ReadAllText(Path.Combine(RepoPaths.Root, "spec", "schemas", "diagnostics.schema.json"));
        Assert.That(MiniJsonSchema.Validate(schema, json), Is.Empty);
    }
}
