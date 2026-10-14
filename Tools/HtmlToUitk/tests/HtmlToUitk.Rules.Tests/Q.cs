using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Includes;
using HtmlToUitk.Core.Json;
using HtmlToUitk.Rules;
using HtmlToUitk.TestSupport;

namespace HtmlToUitk.Rules.Tests;

internal static class Q
{
    public static MiniAnalysis Css(string css, string body = "<div name=\"a\" class=\"x\"></div>", ConversionConfig config = null, CapabilityMatrix caps = null, IFileSystem fs = null)
    {
        return MiniAnalyzer.Html("<html><head><style>\n" + css + "\n</style></head><body>" + body + "</body></html>", "page.html", config, caps, fs);
    }

    public static MiniAnalysis Decl(string declarations, ConversionConfig config = null, CapabilityMatrix caps = null, IFileSystem fs = null)
    {
        return Css(".x { " + declarations + " }", "<div name=\"a\" class=\"x\"></div>", config, caps, fs);
    }

    public static MiniAnalysis Body(string body, string css = "", ConversionConfig config = null, IFileSystem fs = null, IStringTables tables = null)
    {
        return MiniAnalyzer.Html("<html><head><style>\n" + css + "\n</style></head><body>" + body + "</body></html>", "page.html", config, null, fs, tables);
    }

    public static MiniAnalysis Head(string head, string body, ConversionConfig config = null)
    {
        return MiniAnalyzer.Html("<html><head>" + head + "</head><body>" + body + "</body></html>", "page.html", config);
    }

    public static ConversionConfig Cfg(string json)
    {
        return TestConfig.Load(json);
    }

    public static bool Has(MiniAnalysis m, string rule)
    {
        return m.Bag.Items.Any(d => d.RuleId == rule);
    }

    public static int Count(MiniAnalysis m, string rule)
    {
        return m.Bag.Items.Count(d => d.RuleId == rule);
    }

    public static Diagnostic First(MiniAnalysis m, string rule)
    {
        return m.Bag.Sorted().First(d => d.RuleId == rule);
    }

    public static string Ids(MiniAnalysis m)
    {
        return string.Join(" ", m.Bag.Sorted().Select(d => d.RuleId + ":" + d.Severity.ToString()[0] + ":" + d.Tier.ToString()[0]));
    }

    public static string Keys(MiniAnalysis m)
    {
        return string.Join(" ", m.Constructs.Select(c => c.Key).OrderBy(k => k, StringComparer.Ordinal));
    }

    public static CapabilityMatrix Caps(params string[] ops)
    {
        string text = File.ReadAllText(RepoPaths.Spec("capabilities.6000.6.json"));
        if (ops.Length == 0) return CapabilityMatrixLoader.Load(text);
        JsonArray patch = new JsonArray();
        foreach (string op in ops) patch.Add(JsonReader.Parse(op));
        return CapabilityMatrixLoader.Load(CapabilityPatcher.Apply(text, patch));
    }

    public static CapabilityMatrix WithGap()
    {
        return Caps("{\"op\":\"add\",\"path\":\"properties\",\"key\":\"row-gap\",\"value\":{\"id\":\"RowGap\",\"inherited\":false,\"animatable\":\"full\",\"evidence\":\"override\"}}",
            "{\"op\":\"add\",\"path\":\"properties\",\"key\":\"column-gap\",\"value\":{\"id\":\"ColumnGap\",\"inherited\":false,\"animatable\":\"full\",\"evidence\":\"override\"}}");
    }

    public static CapabilityMatrix WithoutFilters()
    {
        return Caps("{\"op\":\"remove\",\"path\":\"filters\",\"value\":\"blur\"}", "{\"op\":\"remove\",\"path\":\"filters\",\"value\":\"drop-shadow\"}");
    }

    public static Construct Construct(string key, params string[] pairs)
    {
        var data = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i + 1 < pairs.Length; i += 2) data[pairs[i]] = pairs[i + 1];
        return new Construct { Kind = ConstructKind.Value, Key = key, Data = data };
    }

    public static TierDecision Decide(string key, string[] pairs = null, CapabilityMatrix caps = null, string configJson = null)
    {
        return new TierPlanner().Decide(Construct(key, pairs ?? new string[0]), caps ?? MiniAnalyzer.DefaultCapabilities, configJson == null ? new ConversionConfig() : Cfg(configJson));
    }

    public static MemoryFileSystem Fs(params (string Path, string Text)[] files)
    {
        var fs = new MemoryFileSystem();
        foreach ((string path, string text) in files) fs.AddFile(path, text);
        return fs;
    }
}
