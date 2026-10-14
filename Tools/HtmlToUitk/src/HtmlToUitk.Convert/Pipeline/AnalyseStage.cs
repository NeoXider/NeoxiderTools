using System;
using System.Collections.Generic;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Dialect;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Includes;
using HtmlToUitk.Core.Style;
using HtmlToUitk.Core.Text;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert
{
    public sealed class AnalyseStage : IPipelineStage<InputSet, AnalysisResult>
    {
        private readonly PipelineState _state;

        internal AnalyseStage(PipelineState state)
        {
            _state = state;
        }

        public string Name => "analyse";

        public AnalysisResult Run(InputSet input, StageContext context)
        {
            PipelineState state = _state;
            ConvertServices services = state.Services;
            ConversionConfig config = context.Config;
            var edges = new List<IncludeEdge>();
            var nodes = new List<string>();
            LoadConfigSheets(input, context, state, edges, nodes);
            var documents = new List<UiDocument>();
            var styleByDocument = new Dictionary<string, IStyleModel>(StringComparer.Ordinal);
            var allSheets = new List<Stylesheet>();
            var knownKeys = new List<string>();
            var keyUses = new List<KeyUse>();
            var newKeys = new List<string>();
            var tokenEntries = new List<TokenEntry>();
            var navs = new List<NavModel>();
            var anims = new List<AnimModel>();
            var analysisPages = new List<AnalysisPage>();
            foreach (PageInput pageInput in input.Pages)
            {
                var page = new PageState { Input = pageInput };
                state.Pages.Add(page);
                state.PageByPath[pageInput.Path] = page;
                HtmlDocument html = services.HtmlParser.Parse(pageInput.Text, pageInput.Path, context.Diagnostics);
                page.Html = html;
                page.ScreenName = ScreenName(html, pageInput.Path);
                pageInput.Name = page.ScreenName;
                BuildCascade(page, html, input, context, state, edges, nodes);
                var options = new StyleOptions();
                page.Style = services.StyleEngine.Build(page.Cascade, options, context.Diagnostics);
                styleByDocument[pageInput.Path] = page.Style;
                LocTable loc = LoadLocalisation(page, html, context, state);
                var mapContext = new MapContext
                {
                    Config = config,
                    Loc = loc,
                    PageDirectory = PathUtil.Directory(pageInput.Path),
                    Env = new Css.TranslateEnv
                    {
                        Caps = context.Capabilities,
                        Config = config,
                        Fs = context.Fs,
                        ProjectRoot = state.ProjectRoot,
                        BaseDirectory = PathUtil.Directory(pageInput.Path)
                    }
                };
                page.Map = UxmlMapper.Map(html, mapContext);
                var document = new UiDocument
                {
                    Name = page.ScreenName,
                    SourcePath = pageInput.Path,
                    Roots = page.Map.Roots
                };
                page.Document = document;
                documents.Add(document);
                foreach (UxmlElement root in page.Map.Roots) SetParents(root, null);
                knownKeys.AddRange(loc.AllKeys());
                keyUses.AddRange(page.Map.Uses);
                foreach (string key in page.Map.NewKeys)
                {
                    if (!newKeys.Contains(key)) newKeys.Add(key);
                }

                CollectTokens(page, tokenEntries);
                NavModel nav = NavCollector.Collect(page, config);
                document.Pages = NavCollector.PageDefs(nav, page);
                navs.Add(nav);
                page.Nav = nav;
                page.Anim = AnimCollector.Collect(page, context.Capabilities, config);
                anims.Add(page.Anim);
                allSheets.AddRange(page.Cascade);
                analysisPages.Add(new AnalysisPage { Path = pageInput.Path, Html = html, Document = document, Cascade = page.Cascade, Style = page.Style });
            }

            state.Includes = new IncludeGraph { Nodes = nodes, Edges = edges };
            var result = new AnalysisResult
            {
                Documents = documents,
                Style = documents.Count > 0 ? styleByDocument[input.Pages[0].Path] : null,
                StyleByDocument = styleByDocument,
                Tokens = new TokenTable { Entries = tokenEntries },
                Keys = new KeyTable { Uses = keyUses, NewKeys = newKeys },
                Nav = navs.Count > 0 ? navs[0] : new NavModel(),
                Anim = anims.Count > 0 ? anims[0] : new AnimModel()
            };
            var analysisInput = new AnalysisInput
            {
                Pages = analysisPages,
                Stylesheets = Distinct(allSheets),
                Config = config,
                Capabilities = context.Capabilities,
                Catalog = services.Catalog,
                Tokens = result.Tokens,
                Keys = result.Keys,
                Nav = result.Nav,
                Anim = result.Anim,
                Includes = state.Includes,
                KnownKeys = knownKeys
            };
            var constructs = new List<Construct>();
            foreach (IConstructDetector detector in services.Detectors) detector.Detect(analysisInput, constructs);
            result.Constructs = constructs;
            if (services.RuleEngine != null) services.RuleEngine.Run(analysisInput, context.Diagnostics);
            state.Analysis = result;
            return result;
        }

        private static IReadOnlyList<Stylesheet> Distinct(List<Stylesheet> sheets)
        {
            var seen = new HashSet<Stylesheet>();
            var list = new List<Stylesheet>();
            foreach (Stylesheet s in sheets)
            {
                if (seen.Add(s)) list.Add(s);
            }

            return list;
        }

        private static void SetParents(UxmlElement element, UxmlElement parent)
        {
            element.Parent = parent;
            foreach (UxmlElement child in element.Children) SetParents(child, element);
        }

        private static string ScreenName(HtmlDocument html, string path)
        {
            foreach (DomElement meta in html.Metas)
            {
                if (UxmlMapper.Attr(meta, "name") == "uitk:screen")
                {
                    string content = UxmlMapper.Attr(meta, "content");
                    if (!string.IsNullOrWhiteSpace(content)) return content.Trim();
                }
            }

            string data = UxmlMapper.Attr(html.Body, "data-screen");
            if (!string.IsNullOrWhiteSpace(data)) return data.Trim();
            return PathUtil.Stem(path);
        }

        private static void LoadConfigSheets(InputSet input, StageContext context, PipelineState state, List<IncludeEdge> edges, List<string> nodes)
        {
            foreach (StyleInput style in input.Styles)
            {
                Stylesheet root = state.Services.CssParser.Parse(style.Text, style.Path, style.Origin, context.Diagnostics);
                ImportExpansion expansion = ImportExpander.Expand(root, context.Fs, context.Diagnostics);
                state.ConfigSheets.AddRange(expansion.Sheets);
                nodes.AddRange(expansion.Graph.Nodes);
                edges.AddRange(expansion.Graph.Edges);
            }

            foreach (UssInput uss in input.UssPassThrough)
            {
                Stylesheet sheet = state.Services.CssParser.Parse(uss.Text, uss.Path, StyleOrigin.UssPassThrough, context.Diagnostics);
                state.UssSheets.Add(sheet);
                state.UssPaths.Add(uss.Path);
            }
        }

        private static void BuildCascade(PageState page, HtmlDocument html, InputSet input, StageContext context, PipelineState state, List<IncludeEdge> edges, List<string> nodes)
        {
            ConversionConfig config = context.Config;
            bool before = !string.Equals(state.Job.Spec.Inputs.CssOrder, "after", StringComparison.Ordinal);
            string pageDirectory = PathUtil.Directory(page.Input.Path);
            if (before) page.Cascade.AddRange(state.ConfigSheets);
            var hrefs = new List<string>();
            foreach (StyleSourceRef source in html.StyleOrder)
            {
                if (source.Kind == StyleSourceKind.Style)
                {
                    StyleBlock block = html.StyleBlocks[source.Index];
                    if (HasAttribute(block.Attributes, "data-uitk-harness")) continue;
                    Core.Css.CssParser concrete = state.Services.CssParser as Core.Css.CssParser;
                    Stylesheet sheet = concrete != null
                        ? concrete.Parse(block.Text, page.Input.Path, StyleOrigin.Page, context.Diagnostics, block.Span.Start)
                        : state.Services.CssParser.Parse(block.Text, page.Input.Path, StyleOrigin.Page, context.Diagnostics);
                    page.Cascade.Add(sheet);
                    continue;
                }

                LinkRef link = html.Links[source.Index];
                if (!string.Equals(link.Rel, "stylesheet", StringComparison.OrdinalIgnoreCase)) continue;
                if (HasAttribute(link.Attributes, "data-uitk-harness")) continue;
                string uss = AttributeValue(link.Attributes, "data-uss");
                if (uss != null)
                {
                    string screensDirectory = PathUtil.Combine(state.UiRootPath, config.Layout.Screens);
                    string skinPath = PathUtil.Combine(screensDirectory, uss.Trim());
                    var skin = new SkinRef { Src = uss.Trim(), Path = skinPath, Span = link.Span };
                    if (context.Fs.FileExists(skinPath))
                    {
                        skin.Sheet = state.Services.CssParser.Parse(context.Fs.ReadAllText(skinPath), skinPath, StyleOrigin.Skin, context.Diagnostics);
                        page.Cascade.Add(skin.Sheet);
                    }
                    else
                    {
                        context.Diagnostics.Report(new Diagnostic
                        {
                            RuleId = "D015",
                            Severity = Severity.Warning,
                            Tier = Tier.Native,
                            Span = link.Span,
                            Construct = "data-uss=\"" + uss + "\"",
                            Message = "skin stylesheet '" + uss + "' not found (looked at " + skinPath + ")",
                            Suggestion = "Check the data-uss path relative to the screens folder."
                        });
                    }

                    page.Skins.Add(skin);
                    continue;
                }

                string href = link.Href ?? "";
                if (ImportExpander.IsExternal(href))
                {
                    continue;
                }

                string cssPath = PathUtil.Combine(pageDirectory, href);
                hrefs.Add(cssPath);
                if (!context.Fs.FileExists(cssPath))
                {
                    context.Diagnostics.Report(new Diagnostic
                    {
                        RuleId = "J002",
                        Severity = Severity.Error,
                        Tier = Tier.Reject,
                        Span = link.Span,
                        Construct = "<link href=\"" + href + "\">",
                        Message = "stylesheet '" + href + "' was not found",
                        Suggestion = "Check the path of the stylesheet link."
                    });
                    continue;
                }

                edges.Add(new IncludeEdge { From = page.Input.Path, To = cssPath, Kind = "link", Span = link.Span });
                Stylesheet root = state.Services.CssParser.Parse(context.Fs.ReadAllText(cssPath), cssPath, StyleOrigin.Linked, context.Diagnostics);
                ImportExpansion expansion = ImportExpander.Expand(root, context.Fs, context.Diagnostics);
                page.Cascade.AddRange(expansion.Sheets);
                nodes.AddRange(expansion.Graph.Nodes);
                edges.AddRange(expansion.Graph.Edges);
            }

            page.StyleHrefOrder = hrefs;
            if (!before) page.Cascade.AddRange(state.ConfigSheets);
            page.Cascade.AddRange(state.UssSheets);
        }

        private static bool HasAttribute(IReadOnlyList<DomAttribute> attributes, string name)
        {
            return AttributeValue(attributes, name) != null;
        }

        private static string AttributeValue(IReadOnlyList<DomAttribute> attributes, string name)
        {
            foreach (DomAttribute a in attributes)
            {
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) return a.Value ?? "";
            }

            return null;
        }

        private static LocTable LoadLocalisation(PageState page, HtmlDocument html, StageContext context, PipelineState state)
        {
            ConversionConfig config = context.Config;
            var table = new LocTable();
            var paths = new List<string>();
            foreach (string template in config.Localization.Files)
            {
                string filled = template.Replace("{screen}", page.ScreenName).Replace("{skin}", config.Layout.Skin).Replace("{screens}", config.Layout.Screens).Replace("{strings}", config.Layout.Strings);
                paths.Add(PathUtil.Combine(state.ConfigDirectory, filled));
            }

            foreach (DomElement meta in html.Metas)
            {
                if (UxmlMapper.Attr(meta, "name") == "uitk:strings")
                {
                    string content = UxmlMapper.Attr(meta, "content");
                    if (!string.IsNullOrEmpty(content)) paths.Add(PathUtil.Combine(PathUtil.Directory(page.Input.Path), content));
                }
            }

            foreach (string path in paths)
            {
                if (context.Fs.FileExists(path)) table.LoadFile(path, context.Fs.ReadAllText(path), config.Localization.Format, null);
            }

            page.Loc = table;
            return table;
        }

        private static void CollectTokens(PageState page, List<TokenEntry> entries)
        {
            foreach (Stylesheet sheet in page.Cascade)
            {
                bool skin = sheet.Origin == StyleOrigin.Skin || sheet.Origin == StyleOrigin.UssPassThrough;
                foreach (StyleRule rule in sheet.Rules)
                {
                    if (rule.AtContext != null && rule.AtContext.Count > 0) continue;
                    bool onlyRoot = rule.Selectors.Count > 0;
                    foreach (Selector selector in rule.Selectors)
                    {
                        if (Css.SelectorFormat.Text(selector) != ":root") onlyRoot = false;
                    }

                    if (!onlyRoot) continue;
                    foreach (Declaration d in rule.Declarations)
                    {
                        if (!d.Name.StartsWith("--", StringComparison.Ordinal)) continue;
                        entries.Add(new TokenEntry { Name = d.Name, Value = d.Value, Scope = ":root", FromSkin = skin, Span = d.Span });
                    }
                }
            }
        }
    }
}
