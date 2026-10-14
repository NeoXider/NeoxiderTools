using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Rules;
using HtmlToUitk.TestSupport;
using NUnit.Framework;

namespace HtmlToUitk.Rules.Tests;

[TestFixture]
public class CatalogTests
{
    private static readonly string[] ReportedByRules =
    {
        "A001", "A002", "A003", "A004", "A005", "A006",
        "C001", "C002", "C003", "C004", "C005", "C006", "C007", "C008", "C009", "C010", "C011", "C012", "C013", "C014", "C015", "C016", "C017", "C018",
        "C019", "C020", "C021", "C022", "C023", "C024", "C025", "C026", "C030", "C031", "C034", "C035",
        "D001", "D002", "D003", "D004", "D005", "D006", "D007", "D008", "D009", "D010", "D011", "D012", "D013",
        "E001", "J005", "J008",
        "L001", "L002", "L003", "L004", "L005", "L006", "L007",
        "N001", "N002", "N003", "N004", "N005", "N006", "N007",
        "P001", "P002", "P003", "P004", "P005", "P006", "P007", "P008", "P009", "P010", "P011",
        "R001", "R002", "R003", "R004", "R006", "R007", "R008", "R009", "T001",
    };

    private static readonly string[] ReportedByOtherStages =
    {
        "C028", "C029", "C033", "J001", "J002", "J003", "J004", "J006", "J007", "J009", "J010", "J011", "J012", "D014", "D015", "D016", "X001", "X002",
        "H001", "H002", "H003", "H004", "H005", "U001",
    };

    [Test]
    public void EmbeddedCatalogLoads()
    {
        Assert.That(RuleCatalog.Embedded.All.Count, Is.GreaterThan(90));
        Assert.That(RuleCatalog.Embedded, Is.SameAs(RuleCatalog.Embedded));
    }

    [Test]
    public void IdsAreUniqueAndSorted()
    {
        var ids = RuleCatalog.Embedded.All.Select(r => r.Id).ToList();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
        Assert.That(ids, Is.EqualTo(ids.OrderBy(i => i, StringComparer.Ordinal).ToList()));
    }

    [Test]
    public void EveryRuleHasTextAndAKnownFamily()
    {
        foreach (RuleInfo rule in RuleCatalog.Embedded.All)
        {
            Assert.That(rule.Title, Is.Not.Empty, rule.Id);
            Assert.That(rule.Why, Is.Not.Empty, rule.Id);
            Assert.That(rule.Fix, Is.Not.Empty, rule.Id);
            Assert.That("ACDEHJLNPRTUX".IndexOf(rule.Id[0]), Is.GreaterThanOrEqualTo(0), rule.Id);
            Assert.That(rule.Id, Does.Match("^[A-Z][0-9]{3}$"));
        }
    }

    [Test]
    public void EveryCatalogRuleHasAnOwner()
    {
        var owned = new HashSet<string>(ReportedByRules.Concat(ReportedByOtherStages));
        var missing = RuleCatalog.Embedded.All.Select(r => r.Id).Where(id => !owned.Contains(id)).ToList();
        Assert.That(missing, Is.Empty, "rules without an owner: " + string.Join(", ", missing));
    }

    [TestCaseSource(nameof(ReportedByRules))]
    public void ReportedRulesExistInTheCatalog(string id)
    {
        Assert.That(RuleCatalog.Embedded.Get(id), Is.Not.Null);
    }

    [TestCase("C001", Severity.Error, Tier.ConvertTime, "grid-flatten", Severity.Info)]
    [TestCase("C002", Severity.Error, Tier.ConvertTime, "gap-margins", Severity.Info)]
    [TestCase("C007", Severity.Error, Tier.Runtime, "media-breakpoints", Severity.Warning)]
    [TestCase("C010", Severity.Warning, Tier.Native, null, Severity.Warning)]
    [TestCase("N002", Severity.Warning, Tier.Native, null, null)]
    [TestCase("L001", Severity.Error, Tier.Reject, null, null)]
    [TestCase("L003", Severity.Info, Tier.Native, null, null)]
    [TestCase("P011", Severity.Info, Tier.Runtime, "page-router", Severity.Info)]
    [TestCase("E001", Severity.Error, Tier.Reject, null, null)]
    public void CatalogRowsCarryTierAndEmulation(string id, Severity severity, Tier tier, string emulation, Severity? emulated)
    {
        RuleInfo info = RuleCatalog.Embedded.Get(id);
        Assert.That(info.DefaultSeverity, Is.EqualTo(severity));
        Assert.That(info.DefaultTier, Is.EqualTo(tier));
        Assert.That(info.Emulation, Is.EqualTo(emulation));
        Assert.That(RuleCatalog.Embedded.EmulatedSeverity(id), Is.EqualTo(emulated));
    }

    [Test]
    public void UnknownIdsAreNull()
    {
        Assert.That(RuleCatalog.Embedded.Get("Z999"), Is.Null);
        Assert.That(RuleCatalog.Embedded.Get(null), Is.Null);
        Assert.That(RuleCatalog.Embedded.Has("C001"), Is.True);
        Assert.That(RuleCatalog.Embedded.Has("Z999"), Is.False);
        Assert.That(RuleCatalog.Embedded.EmulatedSeverity("Z999"), Is.Null);
    }

    [TestCase("{\"rules\":[{\"id\":\"X001\",\"severity\":\"FAIL\"},{\"id\":\"X001\",\"severity\":\"FAIL\"}]}")]
    [TestCase("{\"rules\":[{\"id\":\"X001\",\"severity\":\"LOUD\"}]}")]
    [TestCase("{\"rules\":[{\"id\":\"X001\",\"severity\":\"FAIL\",\"tier\":\"Maybe\"}]}")]
    [TestCase("{\"rules\":[{\"severity\":\"FAIL\"}]}")]
    [TestCase("{\"nothing\":1}")]
    [TestCase("[]")]
    public void BrokenCatalogsAreRejected(string json)
    {
        Assert.That(() => RuleCatalog.Load(json), Throws.Exception);
    }

    [Test]
    public void SeverityNamesAreAccepted()
    {
        RuleCatalog c = RuleCatalog.Load("{\"rules\":[{\"id\":\"X001\",\"severity\":\"Warning\",\"tier\":\"Runtime\",\"emulatedSeverity\":\"Error\",\"title\":\"t\",\"why\":\"w\",\"fix\":\"f\"}]}");
        Assert.That(c.Get("X001").DefaultSeverity, Is.EqualTo(Severity.Warning));
        Assert.That(c.EmulatedSeverity("X001"), Is.EqualTo(Severity.Error));
    }
}

[TestFixture]
public class SpecDataTests
{
    [Test]
    public void UssSpecLoads()
    {
        UssSpec uss = UssSpec.Embedded;
        Assert.That(uss.LengthProperties, Does.Contain("width"));
        Assert.That(uss.NumberProperties, Does.Contain("opacity"));
        Assert.That(uss.ColorProperties, Does.Contain("color"));
        Assert.That(uss.LayoutProperties, Does.Contain("margin"));
        Assert.That(uss.Enums.ContainsKey("justify-content"), Is.True);
    }

    [TestCase("z-index", "later")]
    [TestCase("line-height", "height")]
    [TestCase("-webkit-box-shadow", "Vendor-prefixed")]
    [TestCase("grid-template-areas", "flex")]
    [TestCase("completely-unknown", "Check the spelling")]
    public void AdviceIsFoundByNameThenPrefix(string property, string fragment)
    {
        Assert.That(UssSpec.Embedded.AdviceFor(property), Does.Contain(fragment));
    }

    [Test]
    public void DialectSpecLoads()
    {
        DialectSpec d = DialectSpec.Embedded;
        Assert.That(d.TagToUxml["div"], Is.EqualTo("VisualElement"));
        Assert.That(d.TagToUxml["button"], Is.EqualTo("Button"));
        Assert.That(d.InputTypes["checkbox"], Is.EqualTo("Toggle"));
        Assert.That(d.DataUxml, Does.Contain("ScrollView"));
        Assert.That(d.TextAttribute["Label"], Is.EqualTo("text"));
        Assert.That(d.Interactive, Does.Contain("Button"));
        Assert.That(d.InnerTextTags, Does.Contain("span"));
        Assert.That(d.VoidTags, Does.Contain("img"));
        Assert.That(d.FocusableByDefault, Does.Contain("Toggle"));
        Assert.That(d.TagAdvice["table"], Is.Not.Empty);
    }

    [Test]
    public void BrokenSpecsAreRejected()
    {
        Assert.That(() => UssSpec.Load("[]"), Throws.Exception);
        Assert.That(() => DialectSpec.Load("[]"), Throws.Exception);
    }
}

[TestFixture]
public class TierPlannerTests
{
    private static void Expect(TierDecision d, Tier tier, string emulation, string rule)
    {
        Assert.That(d.Tier, Is.EqualTo(tier), d.Reason);
        Assert.That(d.EmulationId, Is.EqualTo(emulation));
        Assert.That(d.RuleId, Is.EqualTo(rule));
    }

    [TestCase("display:grid", Tier.ConvertTime, "grid-flatten", "C001")]
    [TestCase("prop:gap", Tier.ConvertTime, "gap-margins", "C002")]
    [TestCase("prop:z-index", Tier.ConvertTime, "z-order", "C021")]
    [TestCase("selector:sibling", Tier.ConvertTime, "static-selectors", "C015")]
    [TestCase("selector:attribute", Tier.ConvertTime, "static-selectors", "C015")]
    [TestCase("selector:type", Tier.Reject, null, "C016")]
    [TestCase("pseudo:not", Tier.ConvertTime, "static-selectors", "C003")]
    [TestCase("pseudo:is", Tier.ConvertTime, "static-selectors", "C003")]
    [TestCase("pseudo:where", Tier.ConvertTime, "static-selectors", "C003")]
    [TestCase("pseudo:has", Tier.Reject, null, "C003")]
    [TestCase("pseudo:nth-child", Tier.ConvertTime, "static-selectors", "C004")]
    [TestCase("pseudo:nth-of-type", Tier.ConvertTime, "static-selectors", "C004")]
    [TestCase("pseudo:first-child", Tier.ConvertTime, "static-selectors", "C004")]
    [TestCase("pseudo:last-child", Tier.ConvertTime, "static-selectors", "C004")]
    [TestCase("pseudo:only-child", Tier.ConvertTime, "static-selectors", "C004")]
    [TestCase("pseudo:empty", Tier.ConvertTime, "static-selectors", "C004")]
    [TestCase("pseudo:focus-visible", Tier.Reject, null, "C035")]
    [TestCase("pseudo:focus-within", Tier.Reject, null, "C035")]
    [TestCase("pseudo:visited", Tier.Reject, null, "C035")]
    [TestCase("pseudo:lang", Tier.Reject, null, "C035")]
    [TestCase("pseudo-element:before", Tier.ConvertTime, "pseudo-elements", "C005")]
    [TestCase("pseudo-element:after", Tier.ConvertTime, "pseudo-elements", "C005")]
    [TestCase("pseudo-element:placeholder", Tier.Reject, null, "C005")]
    [TestCase("pseudo-element:selection", Tier.Reject, null, "C005")]
    [TestCase("at:keyframes", Tier.Runtime, "keyframes-animator", "C007")]
    [TestCase("at:font-face", Tier.Reject, null, "C007")]
    [TestCase("at:supports", Tier.Reject, null, "C007")]
    [TestCase("at:container", Tier.Reject, null, "C007")]
    [TestCase("at:layer", Tier.Reject, null, "C007")]
    [TestCase("at:page", Tier.Reject, null, "C007")]
    [TestCase("fn:attr", Tier.Reject, null, "C006")]
    [TestCase("fn:env", Tier.Native, null, "C006")]
    [TestCase("fn:radial-gradient", Tier.ConvertTime, "gradient-bake", "C011")]
    [TestCase("fn:conic-gradient", Tier.ConvertTime, "gradient-bake", "C011")]
    [TestCase("fn:repeating-linear-gradient", Tier.ConvertTime, "gradient-bake", "C011")]
    [TestCase("unit:vw", Tier.ConvertTime, "viewport-units", "C008")]
    [TestCase("unit:vh", Tier.ConvertTime, "viewport-units", "C008")]
    [TestCase("unit:vmin", Tier.ConvertTime, "viewport-units", "C008")]
    [TestCase("unit:vmax", Tier.ConvertTime, "viewport-units", "C008")]
    [TestCase("unit:em", Tier.ConvertTime, "relative-units", "C008")]
    [TestCase("unit:rem", Tier.ConvertTime, "relative-units", "C008")]
    [TestCase("unit:ch", Tier.ConvertTime, "relative-units", "C008")]
    [TestCase("unit:pt", Tier.ConvertTime, "relative-units", "C008")]
    [TestCase("unit:cm", Tier.ConvertTime, "relative-units", "C008")]
    [TestCase("unit:fr", Tier.Reject, null, "C008")]
    [TestCase("unit:x", Tier.Reject, null, "C008")]
    [TestCase("shadow:outer", Tier.Native, null, "C013")]
    [TestCase("shadow:inset", Tier.Reject, null, "C013")]
    [TestCase("overflow:auto", Tier.ConvertTime, "overflow-scrollview", "C020")]
    [TestCase("overflow:scroll", Tier.ConvertTime, "overflow-scrollview", "C020")]
    [TestCase("overflow:axis", Tier.ConvertTime, "overflow-scrollview", "C020")]
    [TestCase("overflow:clip", Tier.Reject, null, "C020")]
    [TestCase("position:fixed", Tier.ConvertTime, "fixed-to-root", "C009")]
    [TestCase("position:sticky", Tier.Reject, null, "C009")]
    [TestCase("position:static", Tier.Reject, null, "C009")]
    [TestCase("transform:single", Tier.ConvertTime, "shorthand-expand", null)]
    [TestCase("transform:multi", Tier.ConvertTime, "transform-decompose", "C022")]
    [TestCase("transform:unsupported", Tier.Reject, null, "C022")]
    [TestCase("keyword:inherit", Tier.Native, null, "C018")]
    [TestCase("keyword:unset", Tier.Native, null, "C018")]
    [TestCase("keyword:revert", Tier.Reject, null, "C018")]
    [TestCase("keyword:revert-layer", Tier.Reject, null, "C018")]
    [TestCase("shorthand:inset", Tier.ConvertTime, "shorthand-expand", null)]
    [TestCase("shorthand:border", Tier.ConvertTime, "shorthand-expand", null)]
    [TestCase("shorthand:flex-flow", Tier.ConvertTime, "shorthand-expand", null)]
    [TestCase("anim:declaration", Tier.Runtime, "keyframes-animator", null)]
    [TestCase("something:unknown", Tier.Native, null, null)]
    public void DefaultMatrixDecisions(string key, Tier tier, string emulation, string rule)
    {
        Expect(Q.Decide(key), tier, emulation, rule);
    }

    [Test]
    public void GridWithUnresolvableTemplateIsE001()
    {
        Expect(Q.Decide("display:grid", new[] { "unresolvable", "auto-fill" }), Tier.Reject, null, "E001");
    }

    [TestCase("24", Tier.ConvertTime)]
    [TestCase("25", Tier.Reject)]
    [TestCase("3", Tier.ConvertTime)]
    public void GridTrackLimitComesFromTheConfig(string tracks, Tier tier)
    {
        Assert.That(Q.Decide("display:grid", new[] { "tracks", tracks }).Tier, Is.EqualTo(tier));
    }

    [Test]
    public void GridTrackLimitIsConfigurable()
    {
        Assert.That(Q.Decide("display:grid", new[] { "tracks", "30" }, null, "{\"emulation\":{\"gridMaxTracks\":40}}").Tier, Is.EqualTo(Tier.ConvertTime));
    }

    [TestCase("gap", true)]
    [TestCase("row-gap", true)]
    [TestCase("column-gap", true)]
    public void GapBecomesNativeWhenTheMatrixHasTheProperty(string property, bool native)
    {
        TierDecision d = Q.Decide("prop:gap", new[] { "property", property }, Q.WithGap());
        Assert.That(d.Tier == Tier.Native, Is.EqualTo(native));
        Assert.That(d.RuleId, Is.EqualTo("C002"));
        Assert.That(d.EmulationId, Is.Null);
    }

    [Test]
    public void GapStaysConvertTimeOnTheDefaultMatrix()
    {
        Expect(Q.Decide("prop:gap", new[] { "property", "row-gap" }), Tier.ConvertTime, "gap-margins", "C002");
    }

    [Test]
    public void SiblingCombinatorIsNativeWhenTheMatrixListsIt()
    {
        CapabilityMatrix caps = Q.Caps("{\"op\":\"add\",\"path\":\"selectors.combinators\",\"value\":\"+\"}");
        Assert.That(Q.Decide("selector:sibling", new[] { "combinator", "+" }, caps).Tier, Is.EqualTo(Tier.Native));
        Assert.That(Q.Decide("selector:sibling", new[] { "combinator", "~" }, caps).Tier, Is.EqualTo(Tier.ConvertTime));
    }

    [Test]
    public void StructuralPseudoClassIsNativeWhenTheMatrixListsIt()
    {
        CapabilityMatrix caps = Q.Caps("{\"op\":\"add\",\"path\":\"selectors.pseudoClasses\",\"value\":\"first-child\"}");
        Expect(Q.Decide("pseudo:first-child", null, caps), Tier.Native, null, "C004");
        Expect(Q.Decide("pseudo:last-child", null, caps), Tier.ConvertTime, "static-selectors", "C004");
    }

    [Test]
    public void PseudoElementWithTextContentIsAnL001Reject()
    {
        Expect(Q.Decide("pseudo-element:after", new[] { "content", "text" }), Tier.Reject, null, "L001");
        Expect(Q.Decide("pseudo-element:after", new[] { "content", "glyph" }), Tier.ConvertTime, "pseudo-elements", "C005");
        Expect(Q.Decide("pseudo-element:before", new[] { "content", "empty" }), Tier.ConvertTime, "pseudo-elements", "C005");
    }

    [Test]
    public void MediaQueriesNeedTheBreakpointWatcher()
    {
        TierDecision d = Q.Decide("at:media");
        Expect(d, Tier.Runtime, "media-breakpoints", "C007");
        Assert.That(d.RequiredRuntime, Is.EqualTo(new[] { "BreakpointWatcher" }));
        Expect(Q.Decide("at:media", new[] { "feature", "(hover: none)" }), Tier.Reject, null, "C007");
    }

    [Test]
    public void KeyframesNeedTheAnimatorAndInvalidOnesAreSilentRejects()
    {
        TierDecision d = Q.Decide("at:keyframes");
        Assert.That(d.RequiredRuntime, Is.EqualTo(new[] { "UitkAnimator" }));
        TierDecision bad = Q.Decide("at:keyframes", new[] { "invalid", "true" });
        Assert.That(bad.Tier, Is.EqualTo(Tier.Reject));
        Assert.That(bad.RuleId, Is.Null);
    }

    [TestCase("calc", "true", Tier.ConvertTime, "calc-fold")]
    [TestCase("calc", "false", Tier.Reject, null)]
    [TestCase("min", "true", Tier.ConvertTime, "calc-fold")]
    [TestCase("max", "false", Tier.Reject, null)]
    [TestCase("clamp", "true", Tier.ConvertTime, "calc-fold")]
    public void MathFunctionsFoldOnlyWhenConstant(string fn, string foldable, Tier tier, string emulation)
    {
        Expect(Q.Decide("fn:" + fn, new[] { "foldable", foldable }), tier, emulation, "C006");
    }

    [Test]
    public void EnvIsRejectedWhenTheMatrixLacksIt()
    {
        CapabilityMatrix caps = Q.Caps("{\"op\":\"add\",\"path\":\"functions\",\"key\":\"env\",\"value\":false}");
        Expect(Q.Decide("fn:env", null, caps), Tier.Reject, null, "C006");
    }

    [Test]
    public void LinearGradientIsNativeOnlyInsideTheSupportedSubset()
    {
        Expect(Q.Decide("fn:linear-gradient", new[] { "supported", "true" }), Tier.Native, null, "C011");
        Expect(Q.Decide("fn:linear-gradient", new[] { "supported", "false" }), Tier.ConvertTime, "gradient-bake", "C011");
        CapabilityMatrix caps = Q.Caps("{\"op\":\"add\",\"path\":\"functions\",\"key\":\"linear-gradient\",\"value\":false}");
        Expect(Q.Decide("fn:linear-gradient", new[] { "supported", "true" }, caps), Tier.ConvertTime, "gradient-bake", "C011");
    }

    [Test]
    public void ViewportUnitIsNativeWhenTheMatrixListsIt()
    {
        CapabilityMatrix caps = Q.Caps("{\"op\":\"add\",\"path\":\"units.length\",\"value\":\"vw\"}");
        Assert.That(Q.Decide("unit:vw", null, caps).Tier, Is.EqualTo(Tier.Native));
        Assert.That(Q.Decide("unit:vh", null, caps).Tier, Is.EqualTo(Tier.ConvertTime));
    }

    [TestCase("filter", "blur")]
    [TestCase("backdrop-filter", "blur")]
    [TestCase("filter", "grayscale")]
    [TestCase("filter", "hue-rotate")]
    [TestCase("backdrop-filter", "opacity")]
    public void FiltersInTheMatrixAreNative(string property, string fn)
    {
        Expect(Q.Decide("filter:" + fn, new[] { "property", property, "name", fn }), Tier.Native, null, "C010");
    }

    [Test]
    public void FilterWithoutMatrixSupportIsEmulated()
    {
        CapabilityMatrix caps = Q.WithoutFilters();
        Expect(Q.Decide("filter:blur", new[] { "property", "filter", "name", "blur" }, caps), Tier.ConvertTime, "filter-bake", "C010");
        TierDecision runtime = Q.Decide("filter:blur", new[] { "property", "backdrop-filter", "name", "blur" }, caps);
        Expect(runtime, Tier.Runtime, "backdrop-blur", "C010");
        Assert.That(runtime.RequiredRuntime, Is.EqualTo(new[] { "BackdropBlur" }));
    }

    [TestCase("brightness", "filter", Tier.ConvertTime)]
    [TestCase("saturate", "filter", Tier.ConvertTime)]
    [TestCase("brightness", "backdrop-filter", Tier.Reject)]
    [TestCase("url", "filter", Tier.Reject)]
    public void UnknownFilterFunctionsAreBakedOrRejected(string fn, string property, Tier tier)
    {
        Assert.That(Q.Decide("filter:" + fn, new[] { "property", property, "name", fn }).Tier, Is.EqualTo(tier));
    }

    [Test]
    public void OuterShadowFallsBackToBakingWithoutDropShadow()
    {
        Expect(Q.Decide("shadow:outer", null, Q.WithoutFilters()), Tier.ConvertTime, "shadow-bake", "C013");
    }

    [Test]
    public void TransformOrderProblemIsE001()
    {
        Expect(Q.Decide("transform:multi", new[] { "unresolvable", "repeated rotate" }), Tier.Reject, null, "E001");
    }

    [Test]
    public void StrictNativeRejectsEveryEmulation()
    {
        foreach (string key in new[] { "display:grid", "prop:gap", "pseudo:not", "unit:vw", "at:media", "fn:radial-gradient", "overflow:auto" })
        {
            TierDecision d = Q.Decide(key, null, null, "{\"emulation\":{\"strictNative\":true}}");
            Assert.That(d.Tier, Is.EqualTo(Tier.Reject), key);
            Assert.That(d.EmulationId, Is.Null, key);
            Assert.That(d.Reason, Does.Contain("strictNative"));
        }
    }

    [Test]
    public void StrictNativeKeepsNativeDecisions()
    {
        Assert.That(Q.Decide("shadow:outer", null, null, "{\"emulation\":{\"strictNative\":true}}").Tier, Is.EqualTo(Tier.Native));
        Assert.That(Q.Decide("prop:gap", null, Q.WithGap(), "{\"emulation\":{\"strictNative\":true}}").Tier, Is.EqualTo(Tier.Native));
    }

    [Test]
    public void DisabledEmulationBecomesReject()
    {
        TierDecision d = Q.Decide("display:grid", null, null, "{\"emulation\":{\"disable\":[\"grid-flatten\"]}}");
        Assert.That(d.Tier, Is.EqualTo(Tier.Reject));
        Assert.That(d.RuleId, Is.EqualTo("C001"));
        Assert.That(d.Reason, Does.Contain("grid-flatten"));
        Assert.That(Q.Decide("prop:gap", null, null, "{\"emulation\":{\"disable\":[\"grid-flatten\"]}}").Tier, Is.EqualTo(Tier.ConvertTime));
    }

    [Test]
    public void RuntimeEmulationsNeedAllowRuntime()
    {
        string cfg = "{\"emulation\":{\"allowRuntime\":false}}";
        Assert.That(Q.Decide("at:media", null, null, cfg).Tier, Is.EqualTo(Tier.Reject));
        Assert.That(Q.Decide("at:keyframes", null, null, cfg).Tier, Is.EqualTo(Tier.Reject));
        Assert.That(Q.Decide("display:grid", null, null, cfg).Tier, Is.EqualTo(Tier.ConvertTime));
    }

    [Test]
    public void PlannerRejectsNullArguments()
    {
        var planner = new TierPlanner();
        Assert.That(() => planner.Decide(null, MiniAnalyzer.DefaultCapabilities, new ConversionConfig()), Throws.ArgumentNullException);
        Assert.That(() => planner.Decide(Q.Construct("prop:gap"), null, new ConversionConfig()), Throws.ArgumentNullException);
    }

    [Test]
    public void NullConfigUsesDefaults()
    {
        Assert.That(new TierPlanner().Decide(Q.Construct("display:grid"), MiniAnalyzer.DefaultCapabilities, null).Tier, Is.EqualTo(Tier.ConvertTime));
    }
}
