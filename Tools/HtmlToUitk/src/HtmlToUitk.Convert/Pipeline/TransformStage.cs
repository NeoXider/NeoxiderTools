using System;
using System.Collections.Generic;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Convert.Dialect;
using HtmlToUitk.Convert.Emulations;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Text;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert
{
    internal sealed class SheetOutput
    {
        public string GroupId;
        public SheetRole Role;
        public string SourcePath;
        public List<UssRule> Rules = new List<UssRule>();
        public List<UssRule> Tail = new List<UssRule>();
        public Stylesheet SkinSheet;
        public string SkinSrc;
    }

    internal sealed class PageOutput
    {
        public PageState Page;
        public UiDocument Document;
        public List<SheetOutput> Sheets = new List<SheetOutput>();
        public List<KeyValuePair<string, string>> NewTokens = new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<string, string>> PageTokens = new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<string, string>> SkinTokens = new List<KeyValuePair<string, string>>();
        public DocumentExtras Extras;
        public SpecSink Specs;
        public AssetSink Assets;
        public List<string> RuntimeComponents = new List<string>();
    }

    public sealed class TransformStage : IPipelineStage<(AnalysisResult Analysis, ConversionPlan Plan), TransformedModel>
    {
        private readonly PipelineState _state;

        internal TransformStage(PipelineState state)
        {
            _state = state;
        }

        public string Name => "transform";

        public TransformedModel Run((AnalysisResult Analysis, ConversionPlan Plan) input, StageContext context)
        {
            Dictionary<string, Construct> approved = new Dictionary<string, Construct>(StringComparer.Ordinal);
            foreach ((Construct Construct, TierDecision Decision) action in input.Plan.Actions)
            {
                TierDecision d = action.Decision;
                if (d == null || (d.Tier != Tier.ConvertTime && d.Tier != Tier.Runtime) || string.IsNullOrEmpty(d.EmulationId)) continue;
                if (!approved.ContainsKey(d.EmulationId)) approved[d.EmulationId] = action.Construct;
            }

            bool hasGradients = false;
            bool hasNavigation = false;
            foreach (PageState page in _state.Pages)
            {
                if (page.Nav != null && (page.Nav.Pages.Count > 0 || page.Nav.Modals.Count > 0 || page.Nav.Tabs.Count > 0)) hasNavigation = true;
                foreach (Stylesheet sheet in page.Cascade)
                    foreach (StyleRule rule in sheet.Rules)
                        foreach (Declaration declaration in rule.Declarations)
                            if (RuleOps.Text(declaration).Contains("gradient(")) hasGradients = true;
            }
            if (hasGradients && !approved.ContainsKey(EmulationIds.GradientBake)) approved[EmulationIds.GradientBake] = null;
            if (hasNavigation && !approved.ContainsKey("page-router")) approved["page-router"] = null;
            foreach (string id in approved.Keys)
            {
                bool registered = false;
                foreach (IEmulation candidate in _state.Services.Emulations.Ordered)
                    if (candidate.Id == id) registered = true;
                if (!registered) throw new InvalidOperationException("unsupported emulation: " + id + "; no backend registered");
            }
            foreach (Diagnostic diagnostic in context.Diagnostics.Items)
            {
                if (diagnostic.RuleId == "C011" && diagnostic.Tier == Tier.Native && diagnostic.Construct != null && diagnostic.Construct.Contains("gradient("))
                {
                    diagnostic.Tier = Tier.ConvertTime;
                    diagnostic.EmulationId = EmulationIds.GradientBake;
                    diagnostic.Message = "gradient baked: assembly capability metadata is not verified native rendering";
                }
            }
            // Assembly metadata does not prove native gradient rendering; default to the verified PNG baker.
            TransformExtras extras = new TransformExtras();
            List<PageOutput> outputs = new List<PageOutput>();
            foreach (PageState page in _state.Pages)
            {
                outputs.Add(TransformPage(page, approved, input.Plan, context, extras));
            }

            TransformedModel model = Assembler.Assemble(outputs, context, _state, extras);
            TransformExtras.Attach(model, extras);
            _state.Model = model;
            return model;
        }

        private sealed class Group
        {
            public string Id;
            public SheetRole Role;
            public string Path;
            public List<Stylesheet> Members = new List<Stylesheet>();
        }

        private static List<Group> BuildGroups(PageState page)
        {
            List<Group> groups = new List<Group>();
            Group current = null;
            foreach (Stylesheet sheet in page.Cascade)
            {
                if (sheet.Origin == StyleOrigin.Skin || sheet.Origin == StyleOrigin.UssPassThrough) continue;
                if (sheet.Origin == StyleOrigin.Page)
                {
                    if (current != null && current.Role == SheetRole.Page)
                    {
                        current.Members.Add(sheet);
                        continue;
                    }

                    current = new Group { Id = "page:" + page.Input.Path, Role = SheetRole.Page, Path = page.Input.Path };
                    current.Members.Add(sheet);
                    groups.Add(current);
                    continue;
                }

                current = new Group { Id = "css:" + sheet.Path, Role = SheetRole.Shared, Path = sheet.Path };
                current.Members.Add(sheet);
                groups.Add(current);
            }

            return groups;
        }

        private PageOutput TransformPage(PageState page, Dictionary<string, Construct> approved, ConversionPlan plan, StageContext context, TransformExtras extras)
        {
            ConversionConfig config = context.Config;
            CapabilityMatrix caps = context.Capabilities;
            PageOutput output = new PageOutput { Page = page, Document = page.Document };
            List<Group> groups = BuildGroups(page);
            SheetBuilder sheets = new SheetBuilder();
            string pageGroupId = null;
            foreach (Group group in groups)
            {
                sheets.ReplaceRules(group.Id, new List<StyleRule>());
                if (group.Role == SheetRole.Page) pageGroupId = group.Id;
                foreach (Stylesheet member in group.Members)
                {
                    foreach (StyleRule rule in member.Rules)
                    {
                        sheets.AddRule(group.Id, StripHints(rule));
                    }
                }
            }

            if (pageGroupId == null)
            {
                pageGroupId = "page:" + page.Input.Path;
                groups.Add(new Group { Id = pageGroupId, Role = SheetRole.Page, Path = page.Input.Path });
                sheets.ReplaceRules(pageGroupId, new List<StyleRule>());
            }

            foreach (StyleRule extra in page.Map.ExtraRules) sheets.AddRule(pageGroupId, extra);
            AssetSink assets = new AssetSink();
            SpecSink specs = new SpecSink();
            ElementEditor editor = new ElementEditor(page.Document);
            DocumentExtras docExtras = extras.Get(page.Input.Path);
            docExtras.KeyOrder.AddRange(page.Map.KeyOrder);
            foreach (KeyValuePair<string, string> pair in page.Map.Mocks) docExtras.KeyMocks[pair.Key] = pair.Value;
            docExtras.NewKeys.AddRange(page.Map.NewKeys);
            EmulationContext emuContext = new EmulationContext
            {
                Document = page.Document,
                Style = page.Style,
                Sheets = sheets,
                Assets = assets,
                Specs = specs,
                Config = config,
                Capabilities = caps,
                Names = new NameGenerator(),
                Editor = editor
            };
            TransformState state = new TransformState
            {
                Context = emuContext,
                Diagnostics = context.Diagnostics,
                Doc = docExtras,
                Extras = ConfigExtras.For(config),
                Fs = context.Fs,
                ProjectRoot = _state.ProjectRoot,
                GeneratedFolder = config.Emulation.BakedTextureFolder,
                ScreenName = page.ScreenName,
                AssetSink = assets,
                SpecSink = specs,
                Editor = editor,
                PrimarySheetId = pageGroupId,
                Loc = page.Loc,
                Decisions = plan.Actions,
                Page = page
            };
            TransformState.Register(emuContext, state);
            foreach (IEmulation emulation in _state.Services.Emulations.Ordered)
            {
                Construct construct;
                if (!approved.TryGetValue(emulation.Id, out construct)) continue;
                emulation.Apply(emuContext, construct);
                foreach (string component in emulation.RuntimeComponents)
                {
                    output.RuntimeComponents.Add(component);
                    docExtras.Runtime.Add(new RuntimeEntry { Id = component, Uses = emulation.Id });
                }
            }
            if (page.Nav != null && (page.Nav.Pages.Count > 0 || page.Nav.Modals.Count > 0 || page.Nav.Tabs.Count > 0))
            {
                if (!config.Emulation.AllowRuntime) throw new InvalidOperationException("navigation requires the UitkRouter runtime backend");

            }

            TranslateEnv env = new TranslateEnv
            {
                Caps = caps,
                Config = config,
                Fs = context.Fs,
                ProjectRoot = _state.ProjectRoot,
                BaseDirectory = PathUtil.Directory(page.Input.Path)
            };
            foreach (GeneratedAsset asset in assets.Assets) env.GeneratedAssets.Add(asset.RelativePath);
            List<KeyValuePair<string, string>> pageTokens = new List<KeyValuePair<string, string>>();
            foreach (Group group in groups)
            {
                SheetOutput sheetOutput = new SheetOutput { GroupId = group.Id, Role = group.Role, SourcePath = group.Path };
                List<KeyValuePair<string, string>> tokens = new List<KeyValuePair<string, string>>();
                List<UssRule> rules = TranslateGroup(sheets.Rules(group.Id), env, config, tokens, sheetOutput);
                rules = UssLayout.Dedupe(rules);
                List<UssRule> ordered = UssLayout.Order(rules, config);
                ordered = UssLayout.Dedupe(ordered);
                sheetOutput.Rules = ordered;
                output.Sheets.Add(sheetOutput);
                foreach (KeyValuePair<string, string> token in tokens) Upsert(pageTokens, token.Key, token.Value);
            }

            output.PageTokens = pageTokens;
            output.SkinTokens = SkinTokens(page);
            foreach (KeyValuePair<string, string> token in pageTokens)
            {
                string existing = null;
                foreach (KeyValuePair<string, string> skin in output.SkinTokens)
                {
                    if (skin.Key == token.Key) existing = skin.Value;
                }

                if (existing == null || Squash(existing) != Squash(token.Value)) output.NewTokens.Add(new KeyValuePair<string, string>(token.Key, CssText.NormaliseTokenValue(token.Value)));
            }

            output.Extras = docExtras;
            output.Specs = specs;
            output.Assets = assets;
            return output;
        }

        private static string Squash(string value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in value.ToLowerInvariant())
            {
                if (!char.IsWhiteSpace(c)) sb.Append(c);
            }

            return sb.ToString();
        }

        private static void Upsert(List<KeyValuePair<string, string>> list, string key, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Key == key)
                {
                    list[i] = new KeyValuePair<string, string>(key, value);
                    return;
                }
            }

            list.Add(new KeyValuePair<string, string>(key, value));
        }

        private static List<KeyValuePair<string, string>> SkinTokens(PageState page)
        {
            List<KeyValuePair<string, string>> tokens = new List<KeyValuePair<string, string>>();
            foreach (SkinRef skin in page.Skins)
            {
                if (skin.Sheet == null) continue;
                foreach (StyleRule rule in skin.Sheet.Rules)
                {
                    if (rule.AtContext != null && rule.AtContext.Count > 0) continue;
                    bool onlyRoot = rule.Selectors.Count > 0;
                    foreach (Selector s in rule.Selectors)
                    {
                        if (SelectorFormat.Text(s) != ":root") onlyRoot = false;
                    }

                    if (!onlyRoot) continue;
                    foreach (Declaration d in rule.Declarations)
                    {
                        if (d.Name.StartsWith("--", StringComparison.Ordinal)) Upsert(tokens, d.Name, d.Value == null ? "" : d.Value.ToText().Trim());
                    }
                }
            }

            return tokens;
        }

        private static StyleRule StripHints(StyleRule rule)
        {
            bool any = false;
            foreach (Declaration d in rule.Declarations)
            {
                if (d.Name.StartsWith("--anim-", StringComparison.Ordinal)) any = true;
            }

            if (!any) return rule;
            List<Declaration> kept = new List<Declaration>();
            foreach (Declaration d in rule.Declarations)
            {
                if (!d.Name.StartsWith("--anim-", StringComparison.Ordinal)) kept.Add(d);
            }

            return RuleOps.WithDeclarations(rule, kept);
        }

        private static bool SelectorSupported(Selector selector, CapabilityMatrix caps)
        {
            if (selector.Parts == null || selector.Parts.Count == 0) return false;
            for (int i = 0; i < selector.Parts.Count; i++)
            {
                char combinator = selector.Parts[i].Combinator;
                SimpleSelector part = selector.Parts[i].Part;
                if (i > 0 && combinator != ' ' && !caps.HasCombinator(combinator)) return false;
                if (!string.IsNullOrEmpty(part.Type)) return false;
                if (part.Attributes.Count > 0 || part.PseudoElements.Count > 0) return false;
                foreach (PseudoClass pseudo in part.Pseudos)
                {
                    if (!caps.HasPseudoClass(pseudo.Name)) return false;
                }
            }

            return true;
        }

        private static List<UssRule> TranslateGroup(IReadOnlyList<StyleRule> rules, TranslateEnv env, ConversionConfig config, List<KeyValuePair<string, string>> tokens, SheetOutput sheetOutput)
        {
            List<UssRule> result = new List<UssRule>();
            int order = 0;
            foreach (StyleRule rule in rules)
            {
                bool tail = false;
                if (rule.AtContext != null && rule.AtContext.Count > 0)
                {
                    if (rule.AtContext.Count == 1 && rule.AtContext[0] == "@htu-tail") tail = true;
                    else continue;
                }

                List<Selector> selectors = new List<Selector>();
                foreach (Selector selector in rule.Selectors)
                {
                    if (SelectorSupported(selector, env.Caps)) selectors.Add(selector);
                }

                if (selectors.Count == 0) continue;
                bool onlyRoot = true;
                foreach (Selector s in selectors)
                {
                    if (SelectorFormat.Text(s) != ":root") onlyRoot = false;
                }

                List<UssDecl> decls = DeclTranslator.Translate(rule.Declarations, env);
                if (onlyRoot)
                {
                    List<UssDecl> remaining = new List<UssDecl>();
                    foreach (UssDecl d in decls)
                    {
                        if (d.Name.StartsWith("--", StringComparison.Ordinal)) Upsert(tokens, d.Name, d.Value);
                        else remaining.Add(d);
                    }

                    decls = remaining;
                    if (decls.Count == 0) continue;
                }

                foreach (UssDecl d in decls)
                {
                    if (d.Name.StartsWith("--", StringComparison.Ordinal)) d.Value = CssText.NormaliseTokenValue(d.Value);
                }

                decls = UssLayout.CanonicalOrder(decls);
                UssRule ussRule = new UssRule { Selectors = selectors, Decls = decls, Order = order++, Span = rule.Span };
                if (tail) sheetOutput.Tail.Add(ussRule);
                else result.Add(ussRule);
            }

            return result;
        }
    }
}
