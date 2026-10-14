using System;
using System.Collections.Generic;
using System.Linq;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Includes;
using HtmlToUitk.Core.Style;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.TestSupport;

public sealed class MiniAnalysis
{
    public DiagnosticBag Bag;
    public AnalysisInput Input;
    public RuleEngine Engine;
    public IReadOnlyList<Construct> Constructs;
    public IReadOnlyList<(Construct Construct, TierDecision Decision)> Decisions;

    public IEnumerable<Diagnostic> Rule(string id)
    {
        return Bag.Items.Where(d => d.RuleId == id);
    }

    public string Summary()
    {
        return string.Join(" ", Bag.Sorted().Select(d => d.RuleId + ":" + d.Severity.ToString()[0] + ":" + d.Tier.ToString()[0] + ":" + d.Span.File + ":" + d.Span.Start.Line + (d.EmulationId != null ? ":" + d.EmulationId : "")));
    }
}

public delegate (string Path, string Text, bool Skin)? LinkLoader(string pagePath, LinkRef link);

public static class MiniAnalyzer
{
    private static CapabilityMatrix _default;

    public static CapabilityMatrix DefaultCapabilities
    {
        get
        {
            if (_default == null) _default = CapabilityMatrixLoader.LoadFile(RepoPaths.Spec("capabilities.6000.6.json"));
            return _default;
        }
    }

    public static MiniAnalysis Html(string html, string path = "page.html", ConversionConfig config = null, CapabilityMatrix caps = null, IFileSystem fs = null, IStringTables tables = null)
    {
        return Analyze(new[] { (path, html) }, config, caps, fs, tables, null);
    }

    public static MiniAnalysis Analyze(IReadOnlyList<(string Path, string Html)> pages, ConversionConfig config, CapabilityMatrix caps, IFileSystem fs, IStringTables tables, IReadOnlyDictionary<string, string> files, LinkLoader loader = null)
    {
        config = config ?? new ConversionConfig();
        caps = caps ?? DefaultCapabilities;
        var bag = new DiagnosticBag();
        var parser = new HtmlParser();
        var analysisPages = new List<AnalysisPage>();
        var allSheets = new List<Stylesheet>();
        var tokens = new List<TokenEntry>();
        var inherited = caps.Data.Properties.Values.Where(p => p.Inherited).Select(p => p.Name).ToList();
        foreach ((string path, string text) in pages)
        {
            HtmlDocument doc = parser.Parse(text, path, bag);
            var cascade = new List<Stylesheet>();
            foreach (StyleSourceRef source in doc.StyleOrder)
            {
                if (source.Kind == StyleSourceKind.Style)
                {
                    StyleBlock block = doc.StyleBlocks[source.Index];
                    if (block.Attributes.Any(a => a.Name == "data-uitk-harness")) continue;
                    cascade.Add(CssParser.Instance.Parse(block.Text, path, StyleOrigin.Page, bag, block.Span.Start));
                }
                else
                {
                    LinkRef link = doc.Links[source.Index];
                    if ((link.Rel ?? "").ToLowerInvariant() != "stylesheet") continue;
                    if (link.Attributes.Any(a => a.Name == "data-uitk-harness")) continue;
                    string href = link.Href ?? "";
                    string resolved = Resolve(path, href);
                    string css;
                    if (loader != null)
                    {
                        (string Path, string Text, bool Skin)? loaded = loader(path, link);
                        if (loaded.HasValue) cascade.Add(CssParser.Instance.Parse(loaded.Value.Text, loaded.Value.Path, loaded.Value.Skin ? StyleOrigin.Skin : StyleOrigin.Linked, bag));
                    }
                    else if (files != null && files.TryGetValue(resolved, out css))
                    {
                        bool skin = link.Attributes.Any(a => a.Name == "data-uss");
                        cascade.Add(CssParser.Instance.Parse(css, resolved, skin ? StyleOrigin.Skin : StyleOrigin.Linked, bag));
                    }
                }
            }
            foreach (Stylesheet sheet in cascade)
            {
                if (!allSheets.Contains(sheet)) allSheets.Add(sheet);
                foreach (StyleRule rule in sheet.Rules)
                {
                    if (rule.Selectors.Count == 0 || rule.Selectors.Any(s => (s.Text ?? "").Trim() != ":root")) continue;
                    foreach (Declaration d in rule.Declarations)
                    {
                        if (d.Name.StartsWith("--", StringComparison.Ordinal))
                        {
                            tokens.Add(new TokenEntry { Name = d.Name, Value = d.Value, Scope = ":root", FromSkin = sheet.Origin == StyleOrigin.Skin, Span = d.Span });
                        }
                    }
                }
            }
            IStyleModel model = new StyleEngine().Build(cascade, new StyleOptions { InheritedProperties = inherited }, bag);
            string screen = doc.Metas.Where(m => m.GetAttribute("name") == "uitk:screen").Select(m => m.GetAttribute("content")).FirstOrDefault();
            analysisPages.Add(new AnalysisPage
            {
                Path = path,
                Html = doc,
                Document = new UiDocument { Name = screen ?? StemOf(path), SourcePath = path },
                Cascade = cascade,
                Style = model,
            });
        }
        var catalog = RuleCatalog.Embedded;
        var input = new AnalysisInput
        {
            Pages = analysisPages,
            Stylesheets = allSheets,
            Config = config,
            Capabilities = caps,
            Catalog = catalog,
            Tokens = new TokenTable { Entries = tokens },
        };
        RuleEngine engine = RuleEngine.CreateDefault(catalog, fs, tables);
        var constructs = engine.Detect(input);
        var decisions = engine.Plan(input, constructs);
        engine.Run(input, bag);
        return new MiniAnalysis { Bag = bag, Input = input, Engine = engine, Constructs = constructs, Decisions = decisions };
    }

    public static string StemOf(string path)
    {
        string p = path.Replace('\\', '/');
        int slash = p.LastIndexOf('/');
        if (slash >= 0) p = p.Substring(slash + 1);
        int dot = p.LastIndexOf('.');
        string stem = dot > 0 ? p.Substring(0, dot) : p;
        return stem.Length == 0 ? stem : char.ToUpperInvariant(stem[0]) + stem.Substring(1);
    }

    public static string Resolve(string pagePath, string href)
    {
        string dir = pagePath.Replace('\\', '/');
        int slash = dir.LastIndexOf('/');
        dir = slash < 0 ? "" : dir.Substring(0, slash);
        var parts = new List<string>();
        string joined = (dir.Length == 0 ? "" : dir + "/") + href.Replace('\\', '/');
        foreach (string segment in joined.Split('/'))
        {
            if (segment.Length == 0 || segment == ".") continue;
            if (segment == ".." && parts.Count > 0) { parts.RemoveAt(parts.Count - 1); continue; }
            parts.Add(segment);
        }
        return string.Join("/", parts);
    }
}
