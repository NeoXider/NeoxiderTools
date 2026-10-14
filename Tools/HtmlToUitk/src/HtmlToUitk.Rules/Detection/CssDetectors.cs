using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Core.Css;

namespace HtmlToUitk.Rules
{
    public static class SheetScope
    {
        public static IReadOnlyList<Stylesheet> Convertible(AnalysisInput input)
        {
            var result = new List<Stylesheet>();
            var seen = new HashSet<Stylesheet>();
            Action<IReadOnlyList<Stylesheet>> add = sheets =>
            {
                if (sheets == null) return;
                foreach (Stylesheet sheet in sheets)
                {
                    if (sheet == null || !seen.Add(sheet)) continue;
                    if (sheet.Origin == StyleOrigin.Skin || sheet.Origin == StyleOrigin.UssPassThrough) continue;
                    result.Add(sheet);
                }
            };
            add(input.Stylesheets);
            if (input.Pages != null)
            {
                foreach (AnalysisPage page in input.Pages) add(page.Cascade);
            }
            return result;
        }

        public static IReadOnlyList<Stylesheet> All(AnalysisInput input)
        {
            var result = new List<Stylesheet>();
            var seen = new HashSet<Stylesheet>();
            Action<IReadOnlyList<Stylesheet>> add = sheets =>
            {
                if (sheets == null) return;
                foreach (Stylesheet sheet in sheets)
                {
                    if (sheet != null && seen.Add(sheet)) result.Add(sheet);
                }
            };
            add(input.Stylesheets);
            if (input.Pages != null)
            {
                foreach (AnalysisPage page in input.Pages) add(page.Cascade);
            }
            return result;
        }

        public static string NormalName(string property)
        {
            return property == null ? "" : property.Trim().ToLowerInvariant();
        }
    }

    public static class PseudoNames
    {
        public static readonly HashSet<string> Functional = new HashSet<string>(StringComparer.Ordinal)
        {
            "not", "is", "where", "has", "matches", "any", "-webkit-any", "-moz-any", "host", "host-context", "slotted", "dir", "lang",
        };

        public static readonly HashSet<string> Structural = new HashSet<string>(StringComparer.Ordinal)
        {
            "nth-child", "nth-last-child", "nth-of-type", "nth-last-of-type", "first-child", "last-child", "only-child",
            "first-of-type", "last-of-type", "only-of-type", "empty",
        };

        public static readonly HashSet<string> LegacyPseudoElements = new HashSet<string>(StringComparer.Ordinal)
        {
            "before", "after", "first-line", "first-letter",
        };
    }

    public sealed class SelectorConstructDetector : IConstructDetector
    {
        public void Detect(AnalysisInput input, ICollection<Construct> output)
        {
            foreach (Stylesheet sheet in SheetScope.Convertible(input))
            {
                foreach (StyleRule rule in sheet.Rules)
                {
                    foreach (Selector selector in rule.Selectors) DetectSelector(input, rule, selector, output);
                }
            }
        }

        private static Construct PseudoElement(AnalysisInput input, StyleRule rule, Selector selector, string name, string text)
        {
            string kind = "none";
            SourceSpan span = selector.Span;
            if (rule != null)
            {
                foreach (Declaration d in rule.Declarations)
                {
                    if (SheetScope.NormalName(d.Name) != "content" || d.Value == null) continue;
                    var str = d.Value as CssString;
                    if (str != null)
                    {
                        if (str.Value.Length == 0)
                        {
                            kind = "empty";
                        }
                        else
                        {
                            bool letters = false;
                            foreach (char c in str.Value)
                            {
                                if (char.IsLetter(c)) { letters = true; break; }
                            }
                            bool allowGlyph = input.Config == null || input.Config.Localization.AllowGlyphOnly;
                            kind = letters || !allowGlyph ? "text" : "glyph";
                            if (kind == "text") span = d.Span;
                        }
                    }
                    else if (CssValues.FunctionName(d.Value) == "attr")
                    {
                        kind = "attr";
                    }
                    else
                    {
                        kind = "other";
                    }
                }
            }
            return Cx.Make(ConstructKind.Selector, ConstructKeys.PseudoElementPrefix + name, span, selector,
                ConstructKeys.DataText, text, ConstructKeys.DataSelector, text, ConstructKeys.DataName, name, ConstructKeys.DataContent, kind);
        }

        public static void DetectSelector(AnalysisInput input, StyleRule rule, Selector selector, ICollection<Construct> output)
        {
            string text = selector.Text ?? "";
            for (int i = 0; i < selector.Parts.Count; i++)
            {
                char combinator = selector.Parts[i].Combinator;
                SimpleSelector part = selector.Parts[i].Part;
                if (combinator == '+' || combinator == '~')
                {
                    output.Add(Cx.Make(ConstructKind.Selector, ConstructKeys.SelectorSibling, selector.Span, selector,
                        ConstructKeys.DataText, text, ConstructKeys.DataSelector, text, ConstructKeys.DataCombinator, combinator.ToString()));
                }
                if (part == null) continue;
                if (!string.IsNullOrEmpty(part.Type) && !part.Universal)
                {
                    output.Add(Cx.Make(ConstructKind.Selector, ConstructKeys.SelectorType, selector.Span, selector,
                        ConstructKeys.DataText, text, ConstructKeys.DataSelector, text, ConstructKeys.DataName, part.Type));
                }
                foreach (AttributeSelector attribute in part.Attributes)
                {
                    output.Add(Cx.Make(ConstructKind.Selector, ConstructKeys.SelectorAttribute, selector.Span, selector,
                        ConstructKeys.DataText, text, ConstructKeys.DataSelector, text, ConstructKeys.DataName, attribute.Name));
                }
                foreach (string element in part.PseudoElements)
                {
                    string name = (element ?? "").TrimStart(':').ToLowerInvariant();
                    output.Add(PseudoElement(input, rule, selector, name, text));
                }
                foreach (PseudoClass pseudo in part.Pseudos)
                {
                    string name = (pseudo.Name ?? "").ToLowerInvariant();
                    bool functional = PseudoNames.Functional.Contains(name);
                    bool structural = PseudoNames.Structural.Contains(name);
                    bool supported = input.Capabilities != null && input.Capabilities.HasPseudoClass(name);
                    if (supported && !functional && !structural) continue;
                    if (!functional && !structural && !PseudoNames.LegacyPseudoElements.Contains(name))
                    {
                        output.Add(Cx.Make(ConstructKind.Selector, ConstructKeys.PseudoPrefix + name, selector.Span, selector,
                            ConstructKeys.DataText, text, ConstructKeys.DataSelector, text, ConstructKeys.DataName, name));
                        continue;
                    }
                    if (PseudoNames.LegacyPseudoElements.Contains(name))
                    {
                        output.Add(PseudoElement(input, rule, selector, name, text));
                        continue;
                    }
                    output.Add(Cx.Make(ConstructKind.Selector, ConstructKeys.PseudoPrefix + name, selector.Span, selector,
                        ConstructKeys.DataText, text, ConstructKeys.DataSelector, text, ConstructKeys.DataName, name));
                }
            }
        }
    }

    public sealed class AtRuleConstructDetector : IConstructDetector
    {
        private static readonly Regex Feature = new Regex(@"\(\s*(?:(min|max)-)?([a-z-]+)\s*(?::[^)]*|[<>]=?[^)]*)?\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex Bare = new Regex(@"[a-z-]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public void Detect(AnalysisInput input, ICollection<Construct> output)
        {
            foreach (Stylesheet sheet in SheetScope.Convertible(input))
            {
                foreach (AtRule at in sheet.AtRules)
                {
                    string name = NormalName(at.Name);
                    if (name.Length == 0 || name == "charset" || name == "import") continue;
                    string text = ("@" + name + " " + (at.Prelude ?? "")).TrimEnd();
                    if (name == "media")
                    {
                        string unsupported = UnsupportedMediaFeature(at.Prelude ?? "");
                        output.Add(Cx.Make(ConstructKind.AtRule, ConstructKeys.AtPrefix + "media", at.Span, at,
                            ConstructKeys.DataText, text, ConstructKeys.DataName, name, ConstructKeys.DataFeature, unsupported));
                    }
                    else
                    {
                        output.Add(Cx.Make(ConstructKind.AtRule, ConstructKeys.AtPrefix + name, at.Span, at,
                            ConstructKeys.DataText, text, ConstructKeys.DataName, name,
                            ConstructKeys.DataInvalid, name == "keyframes" && KeyframesInvalid(input, at) ? "true" : null));
                    }
                }
            }
        }

        public static bool KeyframesInvalid(AnalysisInput input, AtRule at)
        {
            UssSpec uss = UssSpec.Embedded;
            bool allowLayout = input.Config != null && input.Config.Rules.AllowLayoutAnimation;
            foreach (StyleRule block in at.Rules)
            {
                foreach (Declaration d in block.Declarations)
                {
                    string name = SheetScope.NormalName(d.Name);
                    if (name.StartsWith("--", StringComparison.Ordinal) || name == "animation-timing-function") continue;
                    PropertyCapability capability = input.Capabilities == null ? null : input.Capabilities.Property(name);
                    if (capability == null || capability.Animatable == Animatable.None) return true;
                    if (!allowLayout && uss.LayoutProperties.Contains(name)) return true;
                }
            }
            return false;
        }

        public static string NormalName(string name)
        {
            string n = (name ?? "").Trim().TrimStart('@').ToLowerInvariant();
            if (n.StartsWith("-webkit-", StringComparison.Ordinal)) n = n.Substring(8);
            else if (n.StartsWith("-moz-", StringComparison.Ordinal)) n = n.Substring(5);
            return n;
        }

        public static string UnsupportedMediaFeature(string prelude)
        {
            string rest = Feature.Replace(prelude, " ");
            foreach (Match m in Feature.Matches(prelude))
            {
                string feature = m.Groups[2].Value.ToLowerInvariant();
                bool ranged = m.Groups[1].Success;
                switch (feature)
                {
                    case "width":
                    case "height":
                    case "aspect-ratio":
                        break;
                    case "orientation":
                        if (ranged) return m.Value;
                        break;
                    default:
                        return m.Value.Trim();
                }
            }
            foreach (Match w in Bare.Matches(rest))
            {
                string word = w.Value.ToLowerInvariant();
                if (word == "screen" || word == "all" || word == "and" || word == "only") continue;
                return word;
            }
            return null;
        }
    }

    public sealed class DeclarationConstructDetector : IConstructDetector
    {
        private static readonly HashSet<string> ExactMathFunctions = new HashSet<string>(StringComparer.Ordinal) { "calc", "min", "max", "clamp" };
        private static readonly HashSet<string> GradientFunctions = new HashSet<string>(StringComparer.Ordinal)
        {
            "linear-gradient", "repeating-linear-gradient", "radial-gradient", "repeating-radial-gradient", "conic-gradient", "repeating-conic-gradient",
        };
        private static readonly HashSet<string> RelativeUnits = new HashSet<string>(StringComparer.Ordinal)
        {
            "vw", "vh", "vmin", "vmax", "em", "rem", "ch", "ex", "pt", "pc", "cm", "mm", "in", "q", "svw", "svh", "lvw", "lvh", "dvw", "dvh",
        };
        private static readonly HashSet<string> GapProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            "gap", "row-gap", "column-gap", "grid-gap", "grid-row-gap", "grid-column-gap",
        };
        private static readonly HashSet<string> GridTemplateProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            "grid-template-columns", "grid-template-rows", "grid-template", "grid-auto-columns", "grid-auto-rows", "grid-auto-flow", "grid",
        };
        private static readonly HashSet<string> SingleTransformFunctions = new HashSet<string>(StringComparer.Ordinal)
        {
            "rotate", "rotatez", "translate", "translatex", "translatey", "scale", "scalex", "scaley",
        };
        private static readonly HashSet<string> DecomposableTransformFunctions = new HashSet<string>(StringComparer.Ordinal)
        {
            "rotate", "rotatez", "translate", "translatex", "translatey", "scale", "scalex", "scaley",
        };
        private static readonly Regex RepeatCount = new Regex(@"repeat\(\s*(\d+)\s*,", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public void Detect(AnalysisInput input, ICollection<Construct> output)
        {
            foreach (Stylesheet sheet in SheetScope.Convertible(input))
            {
                foreach (StyleRule rule in sheet.Rules)
                {
                    bool overflowSeen = false;
                    foreach (Declaration declaration in rule.Declarations)
                    {
                        var local = new List<Construct>();
                        DetectDeclaration(input, rule, declaration, local);
                        foreach (Construct construct in local)
                        {
                            if (construct.Key.StartsWith(ConstructKeys.OverflowPrefix, StringComparison.Ordinal))
                            {
                                if (overflowSeen) continue;
                                overflowSeen = true;
                            }
                            output.Add(construct);
                        }
                    }
                }
            }
        }

        public static void DetectDeclaration(AnalysisInput input, StyleRule rule, Declaration d, ICollection<Construct> output)
        {
            string name = SheetScope.NormalName(d.Name);
            if (name.StartsWith("--", StringComparison.Ordinal) || d.Value == null) return;
            CssValue value = d.Value;
            string valueText = value.ToText();
            string text = name + ": " + valueText;
            string keyword = CssValues.Keyword(value);

            if (keyword == "inherit" || keyword == "unset" || keyword == "revert" || keyword == "revert-layer")
            {
                output.Add(Cx.Make(ConstructKind.Value, ConstructKeys.KeywordPrefix + keyword, d.Span, d,
                    ConstructKeys.DataText, text, ConstructKeys.DataProperty, name, ConstructKeys.DataValue, keyword));
                return;
            }

            if (GapProperties.Contains(name))
            {
                if (RuleIsGrid(rule)) return;
                output.Add(Cx.Make(ConstructKind.Property, ConstructKeys.Gap, d.Span, d,
                    ConstructKeys.DataText, text, ConstructKeys.DataProperty, name,
                    ConstructKeys.DataApproximate, RuleWraps(rule) ? "true" : null));
                return;
            }

            switch (name)
            {
                case "display":
                    if (keyword == "grid" || keyword == "inline-grid") output.Add(GridConstruct(rule, d, text));
                    break;
                case "position":
                    if (keyword != null && keyword != "relative" && keyword != "absolute")
                    {
                        output.Add(Cx.Make(ConstructKind.Value, ConstructKeys.PositionPrefix + keyword, d.Span, d,
                            ConstructKeys.DataText, text, ConstructKeys.DataProperty, name, ConstructKeys.DataValue, keyword));
                    }
                    break;
                case "z-index":
                    output.Add(Cx.Make(ConstructKind.Property, ConstructKeys.ZIndex, d.Span, d, ConstructKeys.DataText, text, ConstructKeys.DataProperty, name));
                    break;
                case "overflow":
                case "overflow-x":
                case "overflow-y":
                    if (name != "overflow" || (keyword != "visible" && keyword != "hidden" && !CssValues.ContainsVar(value)))
                    {
                        output.Add(Cx.Make(ConstructKind.Value, ConstructKeys.OverflowPrefix + (name == "overflow" ? (keyword ?? "other") : "axis"), d.Span, d,
                            ConstructKeys.DataText, text, ConstructKeys.DataProperty, name, ConstructKeys.DataValue, keyword));
                    }
                    break;
                case "box-shadow":
                    DetectShadows(d, value, text, output);
                    break;
                case "filter":
                case "backdrop-filter":
                    DetectFilters(d, name, value, text, output);
                    break;
                case "transform":
                    DetectTransform(d, value, text, output);
                    break;
                case "inset":
                case "flex-flow":
                case "border":
                case "border-top":
                case "border-right":
                case "border-bottom":
                case "border-left":
                    output.Add(Cx.Make(ConstructKind.Property, ConstructKeys.ShorthandPrefix + name, d.Span, d, ConstructKeys.DataText, text, ConstructKeys.DataProperty, name));
                    break;
                case "animation":
                case "animation-name":
                case "animation-duration":
                case "animation-delay":
                case "animation-iteration-count":
                case "animation-direction":
                case "animation-fill-mode":
                case "animation-timing-function":
                    output.Add(Cx.Make(ConstructKind.Property, ConstructKeys.AnimationDeclaration, d.Span, d, ConstructKeys.DataText, text, ConstructKeys.DataProperty, name));
                    break;
            }

            if (name != "content") ScanValue(input, name, value, d, text, output);
        }

        private static bool RuleIsGrid(StyleRule rule)
        {
            foreach (Declaration other in rule.Declarations)
            {
                if (SheetScope.NormalName(other.Name) != "display") continue;
                string k = CssValues.Keyword(other.Value);
                if (k == "grid" || k == "inline-grid") return true;
            }
            return false;
        }

        private static bool RuleWraps(StyleRule rule)
        {
            foreach (Declaration other in rule.Declarations)
            {
                if (SheetScope.NormalName(other.Name) != "flex-wrap") continue;
                string k = CssValues.Keyword(other.Value);
                if (k == "wrap" || k == "wrap-reverse") return true;
            }
            return false;
        }

        private static Construct GridConstruct(StyleRule rule, Declaration d, string text)
        {
            string unresolvable = null;
            int tracks = 0;
            foreach (Declaration other in rule.Declarations)
            {
                string n = SheetScope.NormalName(other.Name);
                if (!GridTemplateProperties.Contains(n) || other.Value == null) continue;
                string t = other.Value.ToText().ToLowerInvariant();
                foreach (string feature in new[] { "auto-fill", "auto-fit", "minmax(", "subgrid", "dense" })
                {
                    if (unresolvable == null && t.IndexOf(feature, StringComparison.Ordinal) >= 0) unresolvable = feature.TrimEnd('(');
                }
                foreach (Match m in RepeatCount.Matches(t))
                {
                    int count;
                    if (int.TryParse(m.Groups[1].Value, out count) && count > tracks) tracks = count;
                }
            }
            return Cx.Make(ConstructKind.Value, ConstructKeys.DisplayGrid, d.Span, d, ConstructKeys.DataText, text, ConstructKeys.DataProperty, "display",
                ConstructKeys.DataUnresolvable, unresolvable, ConstructKeys.DataTracks, tracks > 0 ? tracks.ToString(System.Globalization.CultureInfo.InvariantCulture) : null);
        }

        private static void DetectShadows(Declaration d, CssValue value, string text, ICollection<Construct> output)
        {
            if (CssValues.Keyword(value) == "none") return;
            foreach (CssValue item in CssValues.CommaItems(value))
            {
                bool inset = false;
                foreach (CssValue token in CssValues.SpaceTokens(item))
                {
                    if (CssValues.Keyword(token) == "inset") inset = true;
                }
                output.Add(Cx.Make(ConstructKind.Value, inset ? ConstructKeys.ShadowInset : ConstructKeys.ShadowOuter, d.Span, d,
                    ConstructKeys.DataText, text, ConstructKeys.DataProperty, "box-shadow"));
            }
        }

        private static void DetectFilters(Declaration d, string property, CssValue value, string text, ICollection<Construct> output)
        {
            if (CssValues.Keyword(value) == "none") return;
            foreach (CssValue token in CssValues.SpaceTokens(value))
            {
                string fn = CssValues.FunctionName(token);
                if (token is CssUrl) fn = "url";
                if (fn == null) continue;
                output.Add(Cx.Make(ConstructKind.Value, ConstructKeys.FilterPrefix + fn, d.Span, d,
                    ConstructKeys.DataText, text, ConstructKeys.DataProperty, property, ConstructKeys.DataName, fn));
            }
        }

        private static void DetectTransform(Declaration d, CssValue value, string text, ICollection<Construct> output)
        {
            if (CssValues.Keyword(value) == "none") return;
            IReadOnlyList<CssValue> tokens = CssValues.SpaceTokens(value);
            bool allFunctions = tokens.Count > 0;
            bool allDecomposable = true;
            foreach (CssValue token in tokens)
            {
                string fn = CssValues.FunctionName(token);
                if (fn == null) { allFunctions = false; break; }
                if (!DecomposableTransformFunctions.Contains(fn)) allDecomposable = false;
            }
            string key;
            string order = null;
            if (!allFunctions || !allDecomposable)
            {
                key = ConstructKeys.TransformUnsupported;
            }
            else if (tokens.Count == 1 && SingleTransformFunctions.Contains(CssValues.FunctionName(tokens[0])))
            {
                key = ConstructKeys.TransformSingle;
            }
            else
            {
                key = ConstructKeys.TransformMulti;
                order = TransformOrderProblem(tokens);
            }
            output.Add(Cx.Make(ConstructKind.Value, key, d.Span, d, ConstructKeys.DataText, text, ConstructKeys.DataProperty, "transform", ConstructKeys.DataUnresolvable, order));
        }

        private static string TransformOrderProblem(IReadOnlyList<CssValue> tokens)
        {
            int last = -1;
            foreach (CssValue token in tokens)
            {
                string fn = CssValues.FunctionName(token);
                int rank = fn.StartsWith("translate", StringComparison.Ordinal) ? 0 : fn.StartsWith("rotate", StringComparison.Ordinal) ? 1 : 2;
                if (rank <= last) return rank == last ? "repeated " + fn : fn + " after a later group";
                last = rank;
            }
            return null;
        }

        private static void ScanValue(AnalysisInput input, string property, CssValue value, Declaration d, string text, ICollection<Construct> output)
        {
            bool skipUnits = property.StartsWith("grid", StringComparison.Ordinal) || property == "content" || property == "font-family";
            Walk(input, property, value, d, text, skipUnits, output);
        }

        private static void Walk(AnalysisInput input, string property, CssValue value, Declaration d, string text, bool skipUnits, ICollection<Construct> output)
        {
            if (value == null) return;
            var function = value as CssFunction;
            if (function != null)
            {
                string fn = function.Name.ToLowerInvariant();
                if (ExactMathFunctions.Contains(fn))
                {
                    output.Add(Cx.Make(ConstructKind.Value, ConstructKeys.FunctionPrefix + fn, d.Span, d,
                        ConstructKeys.DataText, text, ConstructKeys.DataProperty, property, ConstructKeys.DataName, fn,
                        ConstructKeys.DataFoldable, CalcAnalyzer.CanFold(function.ToText()) ? "true" : "false"));
                    return;
                }
                if (fn == "attr" || fn == "env")
                {
                    output.Add(Cx.Make(ConstructKind.Value, ConstructKeys.FunctionPrefix + fn, d.Span, d,
                        ConstructKeys.DataText, text, ConstructKeys.DataProperty, property, ConstructKeys.DataName, fn));
                    return;
                }
                if (GradientFunctions.Contains(fn))
                {
                    output.Add(Cx.Make(ConstructKind.Value, ConstructKeys.FunctionPrefix + fn, d.Span, d,
                        ConstructKeys.DataText, text, ConstructKeys.DataProperty, property, ConstructKeys.DataName, fn,
                        ConstructKeys.DataSupported, fn == "linear-gradient" && GradientSubset.IsSupported(function) ? "true" : "false"));
                    return;
                }
                foreach (CssValue arg in function.Args) Walk(input, property, arg, d, text, skipUnits, output);
                return;
            }
            var list = value as CssList;
            if (list != null)
            {
                foreach (CssValue item in list.Items) Walk(input, property, item, d, text, skipUnits, output);
                return;
            }
            var reference = value as CssVarRef;
            if (reference != null)
            {
                Walk(input, property, reference.Fallback, d, text, skipUnits, output);
                return;
            }
            var dimension = value as CssDimension;
            if (dimension != null && !skipUnits)
            {
                string unit = dimension.Unit.ToLowerInvariant();
                bool known = unit == "px" || unit == "deg" || unit == "s" || unit == "ms" || unit == "%" || unit.Length == 0;
                if (known) return;
                bool emulable = RelativeUnits.Contains(unit);
                bool inMatrix = input.Capabilities != null && input.Capabilities.HasUnit(unit);
                if (emulable || !inMatrix)
                {
                    output.Add(Cx.Make(ConstructKind.Value, ConstructKeys.UnitPrefix + unit, d.Span, d,
                        ConstructKeys.DataText, text, ConstructKeys.DataProperty, property, ConstructKeys.DataUnit, unit));
                }
            }
        }
    }

    public static class ConstructDetectors
    {
        public static IReadOnlyList<IConstructDetector> Default()
        {
            return new IConstructDetector[]
            {
                new SelectorConstructDetector(),
                new AtRuleConstructDetector(),
                new DeclarationConstructDetector(),
            };
        }

        public static IReadOnlyList<Construct> DetectAll(AnalysisInput input, IReadOnlyList<IConstructDetector> detectors = null)
        {
            var list = new List<Construct>();
            foreach (IConstructDetector detector in detectors ?? Default()) detector.Detect(input, list);
            return list;
        }
    }
}
