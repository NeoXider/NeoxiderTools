using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HtmlToUitk.Capabilities.Reader;
using HtmlToUitk.TestSupport;
using NUnit.Framework;

namespace HtmlToUitk.Capabilities.Tests;

[TestFixture]
public class NamingTests
{
    [TestCase("AlignContent", "align-content")]
    [TestCase("BorderBottomLeftRadius", "border-bottom-left-radius")]
    [TestCase("Color", "color")]
    [TestCase("BackgroundPositionX", "background-position-x")]
    [TestCase("EaseInOutBounce", "ease-in-out-bounce")]
    [TestCase("HueRotate", "hue-rotate")]
    [TestCase("SDF", "sdf")]
    [TestCase("UIElements", "ui-elements")]
    [TestCase("PreWrap", "pre-wrap")]
    [TestCase("ID", "id")]
    [TestCase("TextOverflowPosition", "text-overflow-position")]
    [TestCase("X2Y", "x2-y")]
    public void KebabConvertsMembers(string member, string expected)
    {
        Assert.That(Naming.Kebab(member), Is.EqualTo(expected));
    }

    [TestCase("UnityAnimationClip", "-unity-animation-clip")]
    [TestCase("UnitySliceScale", "-unity-slice-scale")]
    [TestCase("UnityTextOutlineColor", "-unity-text-outline-color")]
    [TestCase("Unity", "unity")]
    [TestCase("Unitymess", "unitymess")]
    [TestCase("BorderTopWidth", "border-top-width")]
    public void PropertyNamePrefixesUnityMembers(string member, string expected)
    {
        Assert.That(Naming.PropertyName(member), Is.EqualTo(expected));
    }
}

[TestFixture]
public class JsonTextTests
{
    [Test]
    public void ScalarsAreQuotedAndEscaped()
    {
        Assert.That(JsonText.Quote("a\"b\\c\n\t"), Is.EqualTo("\"a\\\"b\\\\c\\n\\t\""));
        Assert.That(JsonText.Quote("\u0001"), Is.EqualTo("\"\\u0001\""));
    }

    [Test]
    public void ShortScalarArraysStayOnOneLine()
    {
        var text = JsonText.Write(new JObj().Add("a", new object[] { "x", "y", 3 }));
        Assert.That(text, Is.EqualTo("{\n  \"a\": [\"x\", \"y\", 3]\n}\n"));
    }

    [Test]
    public void LongArraysWrapAtWidth()
    {
        var items = Enumerable.Range(0, 60).Select(i => (object)("item-number-" + i)).ToList();
        var text = JsonText.Write(new JObj().Add("a", items));
        var lines = text.Split('\n');
        Assert.That(lines.Length, Is.GreaterThan(4));
        Assert.That(lines.All(l => l.Length <= 112), Is.True);
        using var doc = JsonDocument.Parse(text);
        Assert.That(doc.RootElement.GetProperty("a").GetArrayLength(), Is.EqualTo(60));
    }

    [Test]
    public void InlineObjectsRenderOnOneLine()
    {
        var text = JsonText.Write(new JObj().Add("o", new JObj().Add("k", "v").Add("n", 1).Inlined()));
        Assert.That(text, Is.EqualTo("{\n  \"o\": { \"k\": \"v\", \"n\": 1 }\n}\n"));
    }

    [Test]
    public void EmptyContainersAndNullRender()
    {
        var text = JsonText.Write(new JObj().Add("a", new object[0]).Add("o", new JObj()).Add("n", null));
        Assert.That(text, Is.EqualTo("{\n  \"a\": [],\n  \"o\": {},\n  \"n\": null\n}\n"));
    }

    [Test]
    public void OutputUsesLfOnly()
    {
        Assert.That(JsonText.Write(new JObj().Add("a", 1)).Contains('\r'), Is.False);
    }

    [Test]
    public void BooleansAndNumbersRenderInvariant()
    {
        var text = JsonText.Write(new JObj().Add("t", true).Add("f", false).Add("d", 1.5).Add("l", 7L));
        Assert.That(text, Does.Contain("\"t\": true").And.Contain("\"f\": false").And.Contain("\"d\": 1.5").And.Contain("\"l\": 7"));
    }
}

[TestFixture]
public class UnityLocatorTests
{
    [TestCase("D:/Unity/Editor/6000.6.0f1/Editor/Data/Managed/UnityEngine", "6000.6.0f1")]
    [TestCase("D:\\Unity\\Editor\\6000.6.0f1\\Editor\\Data\\Managed", "6000.6.0f1")]
    [TestCase("/Applications/Unity/Hub/Editor/2022.3.12f1/Unity.app", "2022.3.12f1")]
    [TestCase("C:/Program Files/Unity/Hub/Editor/6000.0.58b2/Editor", "6000.0.58b2")]
    public void VersionIsDerivedFromPath(string path, string expected)
    {
        Assert.That(UnityLocator.VersionFromPath(path), Is.EqualTo(expected));
    }

    [Test]
    public void VersionIsNullWhenAbsent()
    {
        Assert.That(UnityLocator.VersionFromPath("C:/tools/unity"), Is.Null);
    }

    [TestCase("6000.6.0f1", "capabilities.6000.6.json")]
    [TestCase("2022.3.12f1", "capabilities.2022.3.json")]
    [TestCase("custom", "capabilities.custom.json")]
    public void FileNameUsesMajorMinor(string version, string expected)
    {
        Assert.That(UnityLocator.FileNameForVersion(version), Is.EqualTo(expected));
    }
}

[TestFixture]
public class MetadataAssemblyTests
{
    private TempDir _dir;
    private string _path;

    [OneTimeSetUp]
    public void Build()
    {
        _dir = new TempDir();
        _path = FakeUnityAssembly.UiElementsSample().WriteTo(_dir.Path, "UnityEngine.UIElementsModule.dll");
    }

    [OneTimeTearDown]
    public void Clean()
    {
        _dir.Dispose();
    }

    [Test]
    public void EnumMembersAreReadInDeclarationOrder()
    {
        using var asm = MetadataAssembly.Open(_path);
        var t = asm.FindTypes("Justify").Single();
        Assert.That(t.Members, Is.EqualTo(new[] { "FlexStart", "Center", "FlexEnd", "SpaceBetween", "SpaceAround", "SpaceEvenly" }));
        Assert.That(t.FullName, Is.EqualTo("UnityEngine.UIElements.Justify"));
    }

    [Test]
    public void FullNameLookupFiltersByNamespace()
    {
        using var asm = MetadataAssembly.Open(_path);
        Assert.That(asm.FindTypes("UnityEngine.UIElements.Align"), Has.Count.EqualTo(1));
        Assert.That(asm.FindTypes("Other.Align"), Is.Empty);
    }

    [Test]
    public void MissingTypeYieldsNothing()
    {
        using var asm = MetadataAssembly.Open(_path);
        Assert.That(asm.FindTypes("Nope"), Is.Empty);
    }

    [Test]
    public void UserStringsContainEmittedLiterals()
    {
        using var asm = MetadataAssembly.Open(_path);
        Assert.That(asm.HasUserString("justify-content"), Is.True);
        Assert.That(asm.HasUserString("not-there"), Is.False);
    }

    [Test]
    public void ShaIsStableForSameBytes()
    {
        using var a = MetadataAssembly.Open(_path);
        using var b = MetadataAssembly.FromBytes("x.dll", File.ReadAllBytes(_path));
        Assert.That(a.Sha256, Is.EqualTo(b.Sha256));
        Assert.That(a.Sha256, Has.Length.EqualTo(64));
    }

    [Test]
    public void NonManagedFileIsRejected()
    {
        var bad = _dir.File("junk.dll", "not an assembly");
        Assert.That(() => MetadataAssembly.Open(bad), Throws.Exception);
    }
}

[TestFixture]
public class ReaderOnFakeAssemblyTests
{
    private const string Config = @"{
  ""schema"": 1,
  ""primary"": ""UnityEngine.UIElementsModule.dll"",
  ""assemblies"": [""UnityEngine.UIElementsModule.dll"", ""Missing.Module.dll""],
  ""sets"": {
    ""properties"": { ""type"": ""StylePropertyId"", ""skip"": [""Unknown"", ""Custom""] },
    ""easings"": { ""type"": ""EasingMode"" },
    ""filters"": { ""type"": ""FilterFunctionType"", ""skip"": [""None"", ""Custom"", ""Count""] },
    ""functions"": { ""type"": ""StyleValueFunction"", ""only"": true, ""names"": { ""Var"": ""var"", ""Env"": ""env"", ""LinearGradient"": ""linear-gradient"" } },
    ""keywords"": { ""type"": ""StyleValueKeyword"" },
    ""pseudoClasses"": { ""type"": ""PseudoStates"", ""skip"": [""None""] },
    ""combinators"": { ""type"": ""StyleSelectorRelationship"", ""only"": true, ""names"": { ""Child"": "">"", ""Descendent"": "" "" } },
    ""selectorTypes"": { ""type"": ""StyleSelectorType"", ""only"": true, ""names"": { ""Wildcard"": ""*"", ""Type"": ""type"", ""Class"": ""class"", ""ID"": ""id"" } }
  },
  ""valueEnums"": [
    { ""property"": ""justify-content"", ""type"": ""Justify"" },
    { ""property"": ""align-content"", ""type"": ""Align"", ""skip"": [""Auto""] },
    { ""property"": ""flex-wrap"", ""type"": ""Wrap"", ""names"": { ""NoWrap"": ""nowrap"" } },
    { ""property"": ""white-space"", ""type"": ""WhiteSpace"", ""names"": { ""NoWrap"": ""nowrap"" } },
    { ""property"": ""background-repeat"", ""type"": ""Repeat"", ""add"": [""repeat-x"", ""repeat-y""] },
    { ""property"": ""not-a-property"", ""type"": ""FlexDirection"" },
    { ""property"": ""missing-type"", ""type"": ""NoSuchEnum"" }
  ]
}";

    private const string Overrides = @"{
  ""schema"": 1,
  ""propertyNames"": { ""UnityFontStyleAndWeight"": ""-unity-font-style"" },
  ""properties"": {
    ""align-content"": { ""inherited"": false, ""animatable"": ""discrete"", ""documented"": true },
    ""border-bottom-left-radius"": { ""inherited"": false, ""animatable"": ""full"", ""documented"": true },
    ""color"": { ""inherited"": true, ""animatable"": ""full"", ""documented"": true },
    ""flex"": { ""inherited"": false, ""animatable"": ""full"", ""documented"": true, ""longhands"": [""flex-grow"", ""flex-shrink"", ""flex-basis""] },
    ""justify-content"": { ""inherited"": false, ""animatable"": ""discrete"", ""documented"": true },
    ""background-repeat"": { ""inherited"": false, ""animatable"": ""discrete"", ""documented"": true },
    ""margin"": { ""inherited"": false, ""animatable"": ""full"", ""documented"": true, ""longhands"": [""margin-top"", ""margin-right"", ""margin-bottom"", ""margin-left""] },
    ""-unity-font-style"": { ""inherited"": true, ""animatable"": ""discrete"", ""documented"": true },
    ""zzz-manual"": { ""inherited"": false, ""animatable"": ""none"", ""documented"": true }
  },
  ""manualFunctions"": [""var"", ""url""],
  ""absentFunctions"": [""calc"", ""var""],
  ""manualPseudoClasses"": [""enabled"", ""hover""],
  ""manualKeywords"": [""cover""],
  ""units"": { ""time"": [""s"", ""ms""], ""length"": [""px"", ""%""] },
  ""conditions"": { ""backdrop-filter"": ""panel.renderMode == ScreenSpaceOverlay"" }
}";

    private TempDir _dir;
    private ReadResult _result;
    private JsonDocument _doc;

    [OneTimeSetUp]
    public void Read()
    {
        _dir = new TempDir();
        FakeUnityAssembly.UiElementsSample().WriteTo(_dir.Path, "UnityEngine.UIElementsModule.dll");
        _result = CapabilityReader.Read(_dir.Path, "6000.6.0f1", ReaderConfig.Parse(Config), OverridesFile.Parse(Overrides));
        _doc = JsonDocument.Parse(_result.Json);
    }

    [OneTimeTearDown]
    public void Clean()
    {
        _doc.Dispose();
        _dir.Dispose();
    }

    private JsonElement Prop(string name)
    {
        return _doc.RootElement.GetProperty("properties").GetProperty(name);
    }

    [Test]
    public void SchemaVersionAndUnityVersionAreWritten()
    {
        Assert.That(_doc.RootElement.GetProperty("schema").GetInt32(), Is.EqualTo(1));
        Assert.That(_doc.RootElement.GetProperty("unityVersion").GetString(), Is.EqualTo("6000.6.0f1"));
    }

    [Test]
    public void PropertyNamesAreDerivedFromMembers()
    {
        Assert.That(Prop("border-bottom-left-radius").GetProperty("id").GetString(), Is.EqualTo("BorderBottomLeftRadius"));
        Assert.That(Prop("-unity-background-scale-mode").GetProperty("id").GetString(), Is.EqualTo("UnityBackgroundScaleMode"));
        Assert.That(Prop("-unity-slice-top").GetProperty("id").GetString(), Is.EqualTo("UnitySliceTop"));
    }

    [Test]
    public void ExceptionNamesAreAppliedFromOverrides()
    {
        var p = Prop("-unity-font-style");
        Assert.That(p.GetProperty("id").GetString(), Is.EqualTo("UnityFontStyleAndWeight"));
        Assert.That(_doc.RootElement.GetProperty("properties").TryGetProperty("-unity-font-style-and-weight", out _), Is.False);
    }

    [Test]
    public void UnknownAndCustomMembersAreSkipped()
    {
        var names = _doc.RootElement.GetProperty("properties").EnumerateObject().Select(x => x.Name).ToList();
        Assert.That(names, Does.Not.Contain("unknown"));
        Assert.That(names, Does.Not.Contain("custom"));
    }

    [Test]
    public void PropertiesAreSortedOrdinal()
    {
        var names = _doc.RootElement.GetProperty("properties").EnumerateObject().Select(x => x.Name).ToList();
        Assert.That(names, Is.EqualTo(names.OrderBy(n => n, StringComparer.Ordinal).ToList()));
    }

    [Test]
    public void OverridesSupplyInheritedAndAnimatable()
    {
        var color = Prop("color");
        Assert.That(color.GetProperty("inherited").GetBoolean(), Is.True);
        Assert.That(color.GetProperty("animatable").GetString(), Is.EqualTo("full"));
        Assert.That(Prop("justify-content").GetProperty("animatable").GetString(), Is.EqualTo("discrete"));
    }

    [Test]
    public void DocumentedPropertiesGetCombinedEvidence()
    {
        Assert.That(Prop("justify-content").GetProperty("evidence").GetString(), Is.EqualTo("assembly+manual"));
    }

    [Test]
    public void PropertyWithoutOverrideIsAssemblyOnlyAndWarns()
    {
        Assert.That(Prop("transition-duration").GetProperty("evidence").GetString(), Is.EqualTo("assembly-only"));
        Assert.That(_result.Warnings, Has.Some.Contains("transition-duration has no entry"));
    }

    [Test]
    public void ManualOnlyPropertyIsKeptWithWarning()
    {
        var p = Prop("zzz-manual");
        Assert.That(p.GetProperty("evidence").GetString(), Is.EqualTo("manual"));
        Assert.That(p.GetProperty("id").GetString(), Is.EqualTo("ZzzManual"));
        Assert.That(_result.Warnings, Has.Some.Contains("zzz-manual is in the overrides file but not in the assembly"));
    }

    [Test]
    public void NameMissingFromHeapIsReported()
    {
        Assert.That(_result.Warnings, Has.Some.Contains("mystery").And.Contains("user-string heap"));
    }

    [Test]
    public void MissingOptionalAssemblyIsReported()
    {
        Assert.That(_result.Warnings, Has.Some.Contains("assembly not found: Missing.Module.dll"));
    }

    [Test]
    public void MissingTypeIsReported()
    {
        Assert.That(_result.Warnings, Has.Some.Contains("type not found: NoSuchEnum"));
    }

    [Test]
    public void ValueEnumForUnknownPropertyIsReported()
    {
        Assert.That(_result.Warnings, Has.Some.Contains("value enum for unknown property not-a-property"));
    }

    [Test]
    public void EnumValuesKeepDeclarationOrderAndKebabNames()
    {
        var v = Prop("justify-content").GetProperty("values").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.That(v, Is.EqualTo(new[] { "flex-start", "center", "flex-end", "space-between", "space-around", "space-evenly" }));
    }

    [Test]
    public void ValueSkipRemovesMembers()
    {
        var v = Prop("align-content").GetProperty("values").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.That(v, Does.Not.Contain("auto"));
        Assert.That(v, Is.EqualTo(new[] { "flex-start", "center", "flex-end", "stretch" }));
    }

    [Test]
    public void ValueNamesMapNoWrapToCssSpelling()
    {
        Assert.That(Prop("flex-wrap").GetProperty("values").EnumerateArray().Select(x => x.GetString()), Does.Contain("nowrap"));
        Assert.That(Prop("white-space").GetProperty("values").EnumerateArray().Select(x => x.GetString()), Is.EqualTo(new[] { "normal", "nowrap", "pre", "pre-wrap" }));
    }

    [Test]
    public void ValueAddAppendsExtraKeywords()
    {
        var v = Prop("background-repeat").GetProperty("values").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.That(v, Is.EqualTo(new[] { "no-repeat", "space", "round", "repeat", "repeat-x", "repeat-y" }));
    }

    [Test]
    public void ShorthandsAreListedTwice()
    {
        var sh = _doc.RootElement.GetProperty("shorthands");
        Assert.That(sh.GetProperty("margin").EnumerateArray().Select(x => x.GetString()), Is.EqualTo(new[] { "margin-top", "margin-right", "margin-bottom", "margin-left" }));
        Assert.That(Prop("margin").TryGetProperty("longhands", out _), Is.False);
        Assert.That(sh.GetProperty("flex").GetArrayLength(), Is.EqualTo(3));
    }

    [Test]
    public void FunctionsKeepOnlyNamedMembers()
    {
        var f = _doc.RootElement.GetProperty("functions");
        Assert.That(f.GetProperty("var").GetProperty("evidence").GetString(), Is.EqualTo("assembly+manual"));
        Assert.That(f.GetProperty("linear-gradient").GetProperty("evidence").GetString(), Is.EqualTo("assembly-only"));
        Assert.That(f.GetProperty("url").GetProperty("evidence").GetString(), Is.EqualTo("manual-only"));
        Assert.That(f.TryGetProperty("none-filter", out _), Is.False);
        Assert.That(f.TryGetProperty("filter-blur", out _), Is.False);
    }

    [Test]
    public void AbsentFunctionsAreFalseUnlessPresent()
    {
        var f = _doc.RootElement.GetProperty("functions");
        Assert.That(f.GetProperty("calc").ValueKind, Is.EqualTo(JsonValueKind.False));
        Assert.That(f.GetProperty("var").ValueKind, Is.EqualTo(JsonValueKind.Object));
    }

    [Test]
    public void FilterNamesSkipNoneCustomAndCount()
    {
        var v = _doc.RootElement.GetProperty("filters").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.That(v, Is.EqualTo(new[] { "tint", "opacity", "blur", "hue-rotate", "drop-shadow" }));
    }

    [Test]
    public void EasingsFollowEnumOrder()
    {
        var v = _doc.RootElement.GetProperty("easings").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.That(v, Is.EqualTo(new[] { "ease", "ease-in", "ease-out", "ease-in-out", "linear", "ease-in-sine", "ease-in-out-bounce" }));
    }

    [Test]
    public void KeywordsAreSortedAndMergedWithManual()
    {
        var v = _doc.RootElement.GetProperty("keywords").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.That(v, Is.EqualTo(new[] { "auto", "cover", "inherit", "initial", "none", "unset" }));
    }

    [Test]
    public void SelectorFactsComeFromEnums()
    {
        var s = _doc.RootElement.GetProperty("selectors");
        Assert.That(s.GetProperty("combinators").EnumerateArray().Select(x => x.GetString()), Is.EqualTo(new[] { " ", ">" }));
        Assert.That(s.GetProperty("types").EnumerateArray().Select(x => x.GetString()), Is.EqualTo(new[] { "*", "type", "class", "id" }));
        Assert.That(s.GetProperty("pseudoClasses").EnumerateArray().Select(x => x.GetString()),
            Is.EqualTo(new[] { "active", "hover", "checked", "disabled", "focus", "root", "enabled" }));
    }

    [Test]
    public void UnitsAndConditionsAreCopiedSorted()
    {
        var names = _doc.RootElement.GetProperty("units").EnumerateObject().Select(x => x.Name).ToList();
        Assert.That(names, Is.EqualTo(new[] { "length", "time" }));
        Assert.That(_doc.RootElement.GetProperty("conditions").GetProperty("backdrop-filter").GetString(), Does.Contain("ScreenSpaceOverlay"));
    }

    [Test]
    public void GeneratedFromListsOnlyAssembliesUsed()
    {
        var arr = _doc.RootElement.GetProperty("generatedFrom");
        Assert.That(arr.GetArrayLength(), Is.EqualTo(1));
        Assert.That(arr[0].GetProperty("assembly").GetString(), Is.EqualTo("UnityEngine.UIElementsModule.dll"));
        Assert.That(arr[0].GetProperty("sha256").GetString(), Has.Length.EqualTo(64));
    }

    [Test]
    public void WarningsAreEmbeddedInDocument()
    {
        Assert.That(_doc.RootElement.GetProperty("warnings").GetArrayLength(), Is.EqualTo(_result.Warnings.Count));
    }

    [Test]
    public void OutputIsReproducible()
    {
        var again = CapabilityReader.Read(_dir.Path, "6000.6.0f1", ReaderConfig.Parse(Config), OverridesFile.Parse(Overrides));
        Assert.That(again.Json, Is.EqualTo(_result.Json));
    }

    [Test]
    public void OutputHasNoBomCrOrTimestamp()
    {
        Assert.That(_result.Json.StartsWith("\uFEFF", StringComparison.Ordinal), Is.False);
        Assert.That(_result.Json.Contains('\r'), Is.False);
        Assert.That(_result.Json, Does.EndWith("}\n"));
        Assert.That(_result.Json, Does.Not.Contain("generatedAt"));
    }

    [Test]
    public void OutputValidatesAgainstSchema()
    {
        var errors = MiniJsonSchema.Validate(File.ReadAllText(Path.Combine(RepoPaths.Root, "spec", "schemas", "capabilities.schema.json")), _result.Json);
        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    [Test]
    public void MissingPrimaryAssemblyThrows()
    {
        using var empty = new TempDir();
        Assert.That(() => CapabilityReader.Read(empty.Path, "6000.6.0f1", ReaderConfig.Parse(Config), OverridesFile.Empty()),
            Throws.TypeOf<FileNotFoundException>());
    }
}

[TestFixture]
public class ReaderCliTests
{
    private TempDir _dir;
    private string _managed;

    [OneTimeSetUp]
    public void Setup()
    {
        _dir = new TempDir();
        _managed = Path.Combine(_dir.Path, "6000.6.0f1", "Managed");
        FakeUnityAssembly.UiElementsSample().WriteTo(_managed, "UnityEngine.UIElementsModule.dll");
        _dir.File("config.json", @"{ ""schema"": 1, ""primary"": ""UnityEngine.UIElementsModule.dll"", ""assemblies"": [""UnityEngine.UIElementsModule.dll""], ""sets"": { ""easings"": { ""type"": ""EasingMode"" } } }");
    }

    [OneTimeTearDown]
    public void Clean()
    {
        _dir.Dispose();
    }

    private (int code, string output, string error) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        var code = ReaderCli.Run(args, o, e);
        return (code, o.ToString(), e.ToString());
    }

    private string Config => Path.Combine(_dir.Path, "config.json");

    [Test]
    public void HelpExitsZero()
    {
        var r = Run("--help");
        Assert.That(r.code, Is.EqualTo(0));
        Assert.That(r.output, Does.Contain("usage:"));
    }

    [Test]
    public void UnknownArgumentIsUsageError()
    {
        var r = Run("--bogus");
        Assert.That(r.code, Is.EqualTo(2));
        Assert.That(r.error, Does.Contain("unknown argument"));
    }

    [Test]
    public void MissingValueIsUsageError()
    {
        Assert.That(Run("--out").code, Is.EqualTo(2));
    }

    [Test]
    public void MissingManagedDirectoryIsUsageError()
    {
        var r = Run("--unity-managed", Path.Combine(_dir.Path, "nope"));
        Assert.That(r.code, Is.EqualTo(2));
    }

    [Test]
    public void VersionIsDerivedFromManagedPath()
    {
        var target = Path.Combine(_dir.Path, "out-derived");
        var r = Run("--unity-managed", _managed, "--config", Config, "--out", target + "/", "--quiet");
        Assert.That(r.code, Is.EqualTo(0), r.error);
        Assert.That(File.Exists(Path.Combine(target, "capabilities.6000.6.json")), Is.True);
    }

    [Test]
    public void MissingVersionIsUsageError()
    {
        var plain = Path.Combine(_dir.Path, "plain");
        FakeUnityAssembly.UiElementsSample().WriteTo(plain, "UnityEngine.UIElementsModule.dll");
        var r = Run("--unity-managed", plain, "--config", Config);
        Assert.That(r.code, Is.EqualTo(2));
        Assert.That(r.error, Does.Contain("--unity-version"));
    }

    [Test]
    public void ExplicitVersionAndFileOutputWork()
    {
        var plain = Path.Combine(_dir.Path, "plain2");
        FakeUnityAssembly.UiElementsSample().WriteTo(plain, "UnityEngine.UIElementsModule.dll");
        var file = Path.Combine(_dir.Path, "single.json");
        var r = Run("--unity-managed", plain, "--unity-version", "6000.6.0f1", "--config", Config, "--out", file, "--quiet");
        Assert.That(r.code, Is.EqualTo(0), r.error);
        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        Assert.That(doc.RootElement.GetProperty("unityVersion").GetString(), Is.EqualTo("6000.6.0f1"));
    }

    [Test]
    public void WrittenFileHasNoBom()
    {
        var file = Path.Combine(_dir.Path, "bom.json");
        Run("--unity-managed", _managed, "--config", Config, "--out", file, "--quiet");
        var bytes = File.ReadAllBytes(file);
        Assert.That(bytes.Take(3).ToArray(), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
    }

    [Test]
    public void CheckSucceedsOnFreshFileAndFailsAfterEdit()
    {
        var file = Path.Combine(_dir.Path, "check.json");
        Run("--unity-managed", _managed, "--config", Config, "--out", file, "--quiet");
        var ok = Run("--unity-managed", _managed, "--config", Config, "--check", file, "--quiet");
        Assert.That(ok.code, Is.EqualTo(0), ok.error);
        File.AppendAllText(file, " ");
        var bad = Run("--unity-managed", _managed, "--config", Config, "--check", file, "--quiet");
        Assert.That(bad.code, Is.EqualTo(1));
        Assert.That(bad.error, Does.Contain("differs"));
    }

    [Test]
    public void CheckToleratesCrLfInStoredFile()
    {
        var file = Path.Combine(_dir.Path, "crlf.json");
        Run("--unity-managed", _managed, "--config", Config, "--out", file, "--quiet");
        File.WriteAllText(file, File.ReadAllText(file).Replace("\n", "\r\n"));
        Assert.That(Run("--unity-managed", _managed, "--config", Config, "--check", file, "--quiet").code, Is.EqualTo(0));
    }

    [Test]
    public void CheckOfMissingFileIsUsageError()
    {
        var r = Run("--unity-managed", _managed, "--config", Config, "--check", Path.Combine(_dir.Path, "absent.json"), "--quiet");
        Assert.That(r.code, Is.EqualTo(2));
    }

    [Test]
    public void WarningsGoToStandardErrorUnlessQuiet()
    {
        var file = Path.Combine(_dir.Path, "warn.json");
        var loud = Run("--unity-managed", _managed, "--out", file);
        Assert.That(loud.error, Does.Contain("warning:"));
        var quiet = Run("--unity-managed", _managed, "--out", file, "--quiet");
        Assert.That(quiet.error, Does.Not.Contain("warning:"));
    }

    [Test]
    public void BrokenConfigIsUsageError()
    {
        var bad = _dir.File("bad-config.json", "{ not json");
        var r = Run("--unity-managed", _managed, "--config", bad, "--out", Path.Combine(_dir.Path, "x.json"));
        Assert.That(r.code, Is.EqualTo(2));
    }
}

[TestFixture]
public class CheckedInCapabilitiesTests
{
    private static string Path6 => RepoPaths.Spec("capabilities.6000.6.json");

    [Test]
    public void FileValidatesAgainstSchema()
    {
        var errors = MiniJsonSchema.Validate(File.ReadAllText(Path.Combine(RepoPaths.Root, "spec", "schemas", "capabilities.schema.json")), File.ReadAllText(Path6));
        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    [Test]
    public void FileUsesLfAndNoBom()
    {
        var bytes = File.ReadAllBytes(Path6);
        Assert.That(bytes.Take(3).ToArray(), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
        Assert.That(bytes, Does.Not.Contain((byte)'\r'));
    }

    [Test]
    public void OverridesFileParses()
    {
        var o = OverridesFile.Load(RepoPaths.Spec("capabilities.overrides.json"));
        Assert.That(o.Properties.Count, Is.EqualTo(102));
        Assert.That(o.PropertyNames["UnityFontStyleAndWeight"], Is.EqualTo("-unity-font-style"));
    }

    [Test]
    public void FileCoversTheDocumentedFacts()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path6));
        var root = doc.RootElement;
        var props = root.GetProperty("properties");
        Assert.That(props.EnumerateObject().Count(), Is.EqualTo(102));
        foreach (var name in new[] { "justify-content", "backdrop-filter", "filter", "-unity-slice-top", "transition-timing-function", "translate", "rotate", "scale", "text-shadow" })
        {
            Assert.That(props.TryGetProperty(name, out _), Is.True, name);
        }

        foreach (var name in new[] { "gap", "row-gap", "column-gap", "z-index", "box-shadow", "line-height", "text-transform", "grid-template-columns", "inset" })
        {
            Assert.That(props.TryGetProperty(name, out _), Is.False, name);
        }

        Assert.That(root.GetProperty("easings").GetArrayLength(), Is.EqualTo(23));
        Assert.That(root.GetProperty("filters").GetArrayLength(), Is.EqualTo(9));
        Assert.That(root.GetProperty("functions").GetProperty("linear-gradient").GetProperty("evidence").GetString(), Is.EqualTo("assembly-only"));
        Assert.That(root.GetProperty("functions").GetProperty("calc").ValueKind, Is.EqualTo(JsonValueKind.False));
        Assert.That(props.GetProperty("justify-content").GetProperty("values").EnumerateArray().Select(x => x.GetString()), Does.Contain("space-evenly"));
        Assert.That(root.GetProperty("selectors").GetProperty("combinators").EnumerateArray().Select(x => x.GetString()), Is.EqualTo(new[] { " ", ">" }));
        Assert.That(root.GetProperty("warnings").GetArrayLength(), Is.EqualTo(0));
    }

    [Test]
    public void FreshReadMatchesCheckedInFileWhenUnityIsPresent()
    {
        var managed = RepoPaths.UnityManagedOrNull();
        if (managed == null)
        {
            Assert.Ignore("UNITY_MANAGED_DIR is not set; the real assembly is not available");
        }

        var o = new StringWriter();
        var e = new StringWriter();
        var code = ReaderCli.Run(new[] { "--unity-managed", managed, "--overrides", RepoPaths.Spec("capabilities.overrides.json"), "--check", Path6, "--quiet" }, o, e);
        Assert.That(code, Is.EqualTo(0), e.ToString());
    }

    [Test]
    public void RealAssemblyReadIsReproducibleWhenUnityIsPresent()
    {
        var managed = RepoPaths.UnityManagedOrNull();
        if (managed == null)
        {
            Assert.Ignore("UNITY_MANAGED_DIR is not set; the real assembly is not available");
        }

        var overrides = OverridesFile.Load(RepoPaths.Spec("capabilities.overrides.json"));
        var a = CapabilityReader.Read(managed, "6000.6.0f1", ReaderConfig.Default(), overrides);
        var b = CapabilityReader.Read(managed, "6000.6.0f1", ReaderConfig.Default(), overrides);
        Assert.That(a.Json, Is.EqualTo(b.Json));
        Assert.That(a.Warnings, Is.Empty);
    }
}
