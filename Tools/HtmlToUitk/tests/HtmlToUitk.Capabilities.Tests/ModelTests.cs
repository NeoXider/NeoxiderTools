using System;
using System.IO;
using System.Linq;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Core.Json;
using HtmlToUitk.TestSupport;
using NUnit.Framework;

namespace HtmlToUitk.Capabilities.Tests;

[TestFixture]
public class MatrixLoaderTests
{
    private static string Spec => File.ReadAllText(RepoPaths.Spec("capabilities.6000.6.json"));

    private static CapabilityMatrix Matrix => CapabilityMatrixLoader.Load(Spec);

    private const string Tiny = "{\"schema\":1,\"unityVersion\":\"1.0.0f1\",\"properties\":{\"color\":{\"id\":\"Color\",\"inherited\":true,\"animatable\":\"full\"}}}";

    [Test]
    public void CheckedInFileLoads()
    {
        Assert.That(Matrix.UnityVersion, Is.EqualTo("6000.6.0f1"));
        Assert.That(Matrix.Data.Properties.Count, Is.EqualTo(102));
        Assert.That(Matrix.Data.Warnings, Is.Empty);
    }

    [TestCase("justify-content", true)]
    [TestCase("-unity-font-style", true)]
    [TestCase("backdrop-filter", true)]
    [TestCase("gap", false)]
    [TestCase("row-gap", false)]
    [TestCase("z-index", false)]
    [TestCase("box-shadow", false)]
    [TestCase("line-height", false)]
    [TestCase("", false)]
    public void PropertyLookup(string name, bool expected)
    {
        Assert.That(Matrix.HasProperty(name), Is.EqualTo(expected));
        Assert.That(Matrix.Property(name) != null, Is.EqualTo(expected));
    }

    [Test]
    public void NullLookupsAreSafe()
    {
        Assert.That(Matrix.HasProperty(null), Is.False);
        Assert.That(Matrix.Property(null), Is.Null);
        Assert.That(Matrix.HasFunction(null), Is.False);
        Assert.That(Matrix.HasKeyword(null), Is.False);
        Assert.That(Matrix.HasFilter(null), Is.False);
        Assert.That(Matrix.HasEasing(null), Is.False);
        Assert.That(Matrix.HasUnit(null), Is.False);
        Assert.That(Matrix.HasPseudoClass(null), Is.False);
        Assert.That(Matrix.Supports(null), Is.False);
        Assert.That(Matrix.Condition(null), Is.Null);
        Assert.That(Matrix.FunctionEvidence(null), Is.Null);
    }

    [Test]
    public void PropertyDetails()
    {
        PropertyCapability color = Matrix.Property("color");
        Assert.That(color.Inherited, Is.True);
        Assert.That(color.Animatable, Is.EqualTo(Animatable.Full));
        Assert.That(color.Id, Is.EqualTo("Color"));
        Assert.That(Matrix.Property("justify-content").Animatable, Is.EqualTo(Animatable.Discrete));
        Assert.That(Matrix.Property("display").Animatable, Is.EqualTo(Animatable.None));
        Assert.That(Matrix.Property("margin").Longhands, Is.EqualTo(new[] { "margin-top", "margin-right", "margin-bottom", "margin-left" }));
        Assert.That(Matrix.Property("justify-content").Values, Does.Contain("space-evenly"));
        Assert.That(Matrix.Property("opacity").Values, Is.Empty);
        Assert.That(Matrix.Property("opacity").Evidence, Is.EqualTo("assembly+manual"));
    }

    [TestCase("hover", true)]
    [TestCase("active", true)]
    [TestCase("focus", true)]
    [TestCase("disabled", true)]
    [TestCase("checked", true)]
    [TestCase("root", true)]
    [TestCase("enabled", true)]
    [TestCase("inactive", true)]
    [TestCase("not", false)]
    [TestCase("first-child", false)]
    [TestCase("focus-visible", false)]
    public void PseudoClasses(string name, bool expected)
    {
        Assert.That(Matrix.HasPseudoClass(name), Is.EqualTo(expected));
        Assert.That(Matrix.Supports("pseudo:" + name), Is.EqualTo(expected));
    }

    [TestCase(' ', true)]
    [TestCase('>', true)]
    [TestCase('+', false)]
    [TestCase('~', false)]
    public void Combinators(char c, bool expected)
    {
        Assert.That(Matrix.HasCombinator(c), Is.EqualTo(expected));
        Assert.That(Matrix.Supports("combinator:" + c), Is.EqualTo(expected));
    }

    [TestCase("var", true)]
    [TestCase("env", true)]
    [TestCase("linear-gradient", true)]
    [TestCase("url", true)]
    [TestCase("calc", false)]
    [TestCase("radial-gradient", false)]
    [TestCase("cubic-bezier", false)]
    [TestCase("unknown", false)]
    public void Functions(string name, bool expected)
    {
        Assert.That(Matrix.HasFunction(name), Is.EqualTo(expected));
        Assert.That(Matrix.Supports("function:" + name), Is.EqualTo(expected));
    }

    [Test]
    public void FunctionEvidenceIsKept()
    {
        Assert.That(Matrix.FunctionEvidence("linear-gradient"), Is.EqualTo("assembly-only"));
        Assert.That(Matrix.FunctionEvidence("var"), Is.EqualTo("assembly+manual"));
        Assert.That(Matrix.FunctionEvidence("calc"), Is.Null);
    }

    [TestCase("inherit", true)]
    [TestCase("initial", true)]
    [TestCase("unset", true)]
    [TestCase("auto", true)]
    [TestCase("revert", false)]
    public void Keywords(string name, bool expected)
    {
        Assert.That(Matrix.HasKeyword(name), Is.EqualTo(expected));
        Assert.That(Matrix.Supports("keyword:" + name), Is.EqualTo(expected));
    }

    [TestCase("blur", true)]
    [TestCase("drop-shadow", true)]
    [TestCase("hue-rotate", true)]
    [TestCase("brightness", false)]
    [TestCase("saturate", false)]
    public void Filters(string name, bool expected)
    {
        Assert.That(Matrix.HasFilter(name), Is.EqualTo(expected));
        Assert.That(Matrix.Supports("filter:" + name), Is.EqualTo(expected));
    }

    [TestCase("ease", true)]
    [TestCase("ease-out-cubic", true)]
    [TestCase("ease-in-out-bounce", true)]
    [TestCase("linear", true)]
    [TestCase("cubic-bezier", false)]
    [TestCase("steps", false)]
    public void Easings(string name, bool expected)
    {
        Assert.That(Matrix.HasEasing(name), Is.EqualTo(expected));
        Assert.That(Matrix.Supports("easing:" + name), Is.EqualTo(expected));
    }

    [TestCase("px", true)]
    [TestCase("%", true)]
    [TestCase("deg", true)]
    [TestCase("s", true)]
    [TestCase("ms", true)]
    [TestCase("vh", false)]
    [TestCase("rem", false)]
    public void Units(string name, bool expected)
    {
        Assert.That(Matrix.HasUnit(name), Is.EqualTo(expected));
        Assert.That(Matrix.Supports("unit:" + name), Is.EqualTo(expected));
    }

    [TestCase("css:gap", false)]
    [TestCase("css:margin", true)]
    [TestCase("property:opacity", true)]
    [TestCase("uss:linear-gradient", true)]
    [TestCase("uss:hover", true)]
    [TestCase("uss:flex-grow", true)]
    [TestCase("nothing", false)]
    [TestCase(":x", false)]
    [TestCase("bogus:x", false)]
    public void SupportsKeys(string key, bool expected)
    {
        Assert.That(Matrix.Supports(key), Is.EqualTo(expected));
    }

    [Test]
    public void ConditionsAreAvailable()
    {
        Assert.That(Matrix.Condition("backdrop-filter"), Does.Contain("ScreenSpaceOverlay"));
        Assert.That(Matrix.Condition("filter"), Is.Null);
    }

    [Test]
    public void MinimalFileLoads()
    {
        CapabilityMatrix m = CapabilityMatrixLoader.Load(Tiny);
        Assert.That(m.HasProperty("color"), Is.True);
        Assert.That(m.Data.Easings, Is.Empty);
        Assert.That(m.HasFunction("var"), Is.False);
    }

    [TestCase("{\"schema\":2,\"unityVersion\":\"1\",\"properties\":{}}")]
    [TestCase("{\"unityVersion\":\"1\",\"properties\":{}}")]
    [TestCase("{\"schema\":1,\"properties\":{}}")]
    [TestCase("{\"schema\":1,\"unityVersion\":\"1\"}")]
    [TestCase("[]")]
    [TestCase("not json")]
    [TestCase("{\"schema\":1,\"unityVersion\":\"1\",\"properties\":{\"a\":{\"animatable\":\"sometimes\"}}}")]
    [TestCase("{\"schema\":1,\"unityVersion\":\"1\",\"properties\":{\"a\":5}}")]
    [TestCase("{\"schema\":1,\"unityVersion\":\"1\",\"properties\":{},\"easings\":\"ease\"}")]
    [TestCase("{\"schema\":1,\"unityVersion\":\"1\",\"properties\":{},\"easings\":[1]}")]
    [TestCase("{\"schema\":1,\"unityVersion\":\"1\",\"properties\":{},\"functions\":{\"x\":5}}")]
    [TestCase("{\"schema\":1,\"unityVersion\":\"1\",\"properties\":{},\"conditions\":{\"x\":5}}")]
    public void BrokenFilesAreRejected(string json)
    {
        Assert.That(() => CapabilityMatrixLoader.Load(json), Throws.TypeOf<CapabilityFormatException>());
    }

    [Test]
    public void LoadNeedsText()
    {
        Assert.That(() => CapabilityMatrixLoader.Load(null), Throws.ArgumentNullException);
    }

    [Test]
    public void LoadFileReadsFromDisk()
    {
        using var dir = new TempDir();
        string path = dir.File("caps.json", Tiny);
        Assert.That(CapabilityMatrixLoader.LoadFile(path).UnityVersion, Is.EqualTo("1.0.0f1"));
    }

    [Test]
    public void EmbeddedMatrixIsAvailable()
    {
        Assert.That(CapabilityMatrixLoader.EmbeddedVersions(), Does.Contain("6000.6"));
        CapabilityMatrix m = CapabilityMatrixLoader.LoadEmbedded("6000.6");
        Assert.That(m.UnityVersion, Is.EqualTo("6000.6.0f1"));
        Assert.That(m.Data.Properties.Count, Is.EqualTo(102));
    }

    [Test]
    public void EmbeddedMatrixEqualsTheCheckedInFile()
    {
        CapabilityMatrix embedded = CapabilityMatrixLoader.LoadEmbedded("6000.6");
        Assert.That(embedded.Data.Properties.Keys.OrderBy(k => k, StringComparer.Ordinal), Is.EqualTo(Matrix.Data.Properties.Keys.OrderBy(k => k, StringComparer.Ordinal)));
    }

    [Test]
    public void UnknownEmbeddedVersions()
    {
        Assert.That(() => CapabilityMatrixLoader.LoadEmbedded("1999.1"), Throws.TypeOf<CapabilityFormatException>());
        Assert.That(CapabilityMatrixLoader.TryLoadEmbedded("1999.1", out CapabilityMatrix none), Is.False);
        Assert.That(none, Is.Null);
        Assert.That(CapabilityMatrixLoader.TryLoadEmbedded(null, out CapabilityMatrix nothing), Is.False);
        Assert.That(CapabilityMatrixLoader.TryLoadEmbedded("6000.6", out CapabilityMatrix some), Is.True);
        Assert.That(some, Is.Not.Null);
    }

    [Test]
    public void EmbeddedVersionsDoNotListTheOverridesOrSchema()
    {
        Assert.That(CapabilityMatrixLoader.EmbeddedVersions(), Does.Not.Contain("overrides"));
        Assert.That(CapabilityMatrixLoader.EmbeddedVersions(), Does.Not.Contain("schema"));
    }

    [Test]
    public void SchemaFlagsAreStrict()
    {
        Assert.That(CapabilityMatrixLoader.SupportedSchema, Is.EqualTo(1));
    }
}

[TestFixture]
public class CapabilityPatcherTests
{
    private static string Spec => File.ReadAllText(RepoPaths.Spec("capabilities.6000.6.json"));

    private static CapabilityMatrix Patch(string ops)
    {
        return CapabilityMatrixLoader.Load(CapabilityPatcher.Apply(Spec, JsonReader.Parse(ops)));
    }

    [Test]
    public void AddingAPropertyRowMakesItAvailable()
    {
        CapabilityMatrix m = Patch("[{\"op\":\"add\",\"path\":\"properties\",\"key\":\"row-gap\",\"value\":{\"id\":\"RowGap\",\"inherited\":false,\"animatable\":\"full\",\"evidence\":\"override\"}}]");
        Assert.That(m.HasProperty("row-gap"), Is.True);
        Assert.That(m.Property("row-gap").Animatable, Is.EqualTo(Animatable.Full));
        Assert.That(m.Property("row-gap").Evidence, Is.EqualTo("override"));
        Assert.That(m.HasProperty("column-gap"), Is.False);
    }

    [Test]
    public void RemovingAPropertyRow()
    {
        Assert.That(Patch("[{\"op\":\"remove\",\"path\":\"properties\",\"key\":\"filter\"}]").HasProperty("filter"), Is.False);
    }

    [Test]
    public void ReplacingAPropertyRow()
    {
        CapabilityMatrix m = Patch("[{\"op\":\"replace\",\"path\":\"properties\",\"key\":\"opacity\",\"value\":{\"id\":\"Opacity\",\"inherited\":true,\"animatable\":\"none\",\"evidence\":\"override\"}}]");
        Assert.That(m.Property("opacity").Inherited, Is.True);
        Assert.That(m.Property("opacity").Animatable, Is.EqualTo(Animatable.None));
    }

    [Test]
    public void ArrayItemsCanBeAddedAndRemoved()
    {
        CapabilityMatrix m = Patch("[{\"op\":\"remove\",\"path\":\"filters\",\"value\":\"blur\"},{\"op\":\"add\",\"path\":\"filters\",\"value\":\"brightness\"},{\"op\":\"add\",\"path\":\"selectors.combinators\",\"value\":\"+\"}]");
        Assert.That(m.HasFilter("blur"), Is.False);
        Assert.That(m.HasFilter("brightness"), Is.True);
        Assert.That(m.HasCombinator('+'), Is.True);
    }

    [Test]
    public void AddingAnExistingArrayItemIsIdempotent()
    {
        CapabilityMatrix m = Patch("[{\"op\":\"add\",\"path\":\"filters\",\"value\":\"blur\"}]");
        Assert.That(m.Data.Filters.Count(f => f == "blur"), Is.EqualTo(1));
    }

    [Test]
    public void RemovingAMissingArrayItemIsHarmless()
    {
        Assert.That(Patch("[{\"op\":\"remove\",\"path\":\"filters\",\"value\":\"nothing\"}]").Data.Filters.Count, Is.EqualTo(9));
    }

    [Test]
    public void FunctionsCanBeSwitchedOffAndOn()
    {
        CapabilityMatrix m = Patch("[{\"op\":\"replace\",\"path\":\"functions\",\"key\":\"linear-gradient\",\"value\":false},{\"op\":\"add\",\"path\":\"functions\",\"key\":\"calc\",\"value\":true}]");
        Assert.That(m.HasFunction("linear-gradient"), Is.False);
        Assert.That(m.HasFunction("calc"), Is.True);
    }

    [Test]
    public void ConditionsAndUnitsCanBePatched()
    {
        CapabilityMatrix m = Patch("[{\"op\":\"add\",\"path\":\"units.length\",\"value\":\"vh\"},{\"op\":\"add\",\"path\":\"conditions\",\"key\":\"filter\",\"value\":\"x\"}]");
        Assert.That(m.HasUnit("vh"), Is.True);
        Assert.That(m.Condition("filter"), Is.EqualTo("x"));
    }

    [Test]
    public void OpsApplyInOrder()
    {
        CapabilityMatrix m = Patch("[{\"op\":\"add\",\"path\":\"properties\",\"key\":\"zzz\",\"value\":{\"id\":\"Zzz\",\"inherited\":false,\"animatable\":\"none\"}},{\"op\":\"remove\",\"path\":\"properties\",\"key\":\"zzz\"}]");
        Assert.That(m.HasProperty("zzz"), Is.False);
    }

    [Test]
    public void AddingUnderANewPathCreatesIt()
    {
        string patched = CapabilityPatcher.Apply(Spec, JsonReader.Parse("[{\"op\":\"add\",\"path\":\"extra\",\"key\":\"a\",\"value\":1}]"));
        Assert.That(patched, Does.Contain("\"extra\""));
    }

    [TestCase("{}")]
    [TestCase("[1]")]
    [TestCase("[{\"op\":\"move\",\"path\":\"a\"}]")]
    [TestCase("[{\"op\":\"add\"}]")]
    [TestCase("[{\"op\":\"remove\",\"path\":\"nowhere\",\"key\":\"a\"}]")]
    [TestCase("[{\"op\":\"replace\",\"path\":\"properties\",\"key\":\"nothing\",\"value\":1}]")]
    [TestCase("[{\"op\":\"add\",\"path\":\"properties\",\"value\":1}]")]
    [TestCase("[{\"op\":\"replace\",\"path\":\"filters\",\"value\":\"x\"}]")]
    [TestCase("[{\"op\":\"add\",\"path\":\"schema\",\"key\":\"a\",\"value\":1}]")]
    public void BadPatchesAreRejected(string ops)
    {
        Assert.That(() => CapabilityPatcher.Apply(Spec, JsonReader.Parse(ops)), Throws.TypeOf<CapabilityFormatException>());
    }

    [Test]
    public void PatchedOutputIsStableJson()
    {
        JsonValue ops = JsonReader.Parse("[{\"op\":\"remove\",\"path\":\"filters\",\"value\":\"blur\"}]");
        Assert.That(CapabilityPatcher.Apply(Spec, ops), Is.EqualTo(CapabilityPatcher.Apply(Spec, ops)));
    }

    [Test]
    public void ParsedOpsKeepTheirFields()
    {
        var ops = CapabilityPatcher.ParseOps(JsonReader.Parse("[{\"op\":\"add\",\"path\":\"properties\",\"key\":\"k\",\"value\":3}]"));
        Assert.That(ops.Single().Op, Is.EqualTo("add"));
        Assert.That(ops.Single().Path, Is.EqualTo("properties"));
        Assert.That(ops.Single().Key, Is.EqualTo("k"));
        Assert.That(ops.Single().Value.AsInt(), Is.EqualTo(3));
    }
}
