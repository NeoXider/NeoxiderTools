using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert;
using HtmlToUitk.Core.Css;

namespace HtmlToUitk.Rules
{
    public sealed class CssValueRules : IRule
    {
        private static readonly HashSet<string> HandledByEmulation = new HashSet<string>(StringComparer.Ordinal)
        {
            "gap", "row-gap", "column-gap", "grid-gap", "grid-row-gap", "grid-column-gap", "z-index", "box-shadow", "overflow-x", "overflow-y",
            "animation", "animation-name", "animation-duration", "animation-delay", "animation-iteration-count", "animation-direction",
            "animation-fill-mode", "animation-timing-function",
        };

        private static readonly HashSet<string> MathFunctions = new HashSet<string>(StringComparer.Ordinal) { "calc", "min", "max", "clamp", "env", "attr" };
        private static readonly Regex GradientName = new Regex(@"^(repeating-)?(linear|radial|conic)-gradient$", RegexOptions.CultureInvariant);
        private static readonly HashSet<string> BadBorderStyles = new HashSet<string>(StringComparer.Ordinal) { "dashed", "dotted", "double", "groove", "ridge", "inset", "outset" };
        private static readonly HashSet<string> BorderStyles = new HashSet<string>(StringComparer.Ordinal)
        {
            "none", "hidden", "solid", "dashed", "dotted", "double", "groove", "ridge", "inset", "outset",
        };
        private static readonly HashSet<string> UnsupportedColorFunctions = new HashSet<string>(StringComparer.Ordinal)
        {
            "hsl", "hsla", "hwb", "lab", "lch", "oklab", "oklch", "color", "color-mix",
        };
        private static readonly HashSet<string> NoPercentLengths = new HashSet<string>(StringComparer.Ordinal)
        {
            "border-width", "border-top-width", "border-right-width", "border-bottom-width", "border-left-width", "font-size", "letter-spacing", "word-spacing",
        };
        private static readonly HashSet<string> NoAutoLengths = new HashSet<string>(StringComparer.Ordinal)
        {
            "border-radius", "font-size", "letter-spacing", "word-spacing",
        };
        private static readonly Dictionary<string, int[]> LengthCounts = new Dictionary<string, int[]>(StringComparer.Ordinal)
        {
            { "margin", new[] { 1, 4 } }, { "padding", new[] { 1, 4 } }, { "border-width", new[] { 1, 4 } }, { "border-radius", new[] { 1, 4 } },
        };
        private static readonly Dictionary<string, string> TextAlignMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "left", "left" }, { "start", "left" }, { "center", "center" }, { "right", "right" }, { "end", "right" }, { "justify", "left" },
        };

        private readonly IFileSystem _fs;

        public CssValueRules(IFileSystem fs = null)
        {
            _fs = fs;
        }

        public string Id => "css-values";

        public void Evaluate(RuleContext context)
        {
            AnalysisInput input = context.Input;
            var reporter = new RuleReporter(context.Catalog, context.Config, context.Sink);
            var checker = new Checker(context, reporter, _fs);
            foreach (Stylesheet sheet in SheetScope.Convertible(input))
            {
                foreach (StyleRule rule in sheet.Rules)
                {
                    checker.CheckRule(sheet, rule);
                }
            }
            checker.CheckTokens(input);
        }

        private sealed class Checker
        {
            private readonly RuleContext _context;
            private readonly RuleReporter _r;
            private readonly CapabilityMatrix _caps;
            private readonly UssSpec _uss = UssSpec.Embedded;
            private readonly IFileSystem _fs;

            public Checker(RuleContext context, RuleReporter reporter, IFileSystem fs)
            {
                _context = context;
                _r = reporter;
                _caps = context.Capabilities;
                _fs = fs;
            }

            public void CheckRule(Stylesheet sheet, StyleRule rule)
            {
                var byName = new Dictionary<string, Declaration>(StringComparer.Ordinal);
                foreach (Declaration d in rule.Declarations) byName[SheetScope.NormalName(d.Name)] = d;
                bool pseudoElementRule = false;
                foreach (Selector selector in rule.Selectors)
                {
                    foreach (var part in selector.Parts)
                    {
                        if (part.Part != null && (part.Part.PseudoElements.Count > 0 || HasLegacyPseudoElement(part.Part))) pseudoElementRule = true;
                    }
                }
                bool fontHandled = false;
                foreach (Declaration d in rule.Declarations)
                {
                    string name = SheetScope.NormalName(d.Name);
                    if (d.Important) _r.Report("C017", d.Span, name + ": " + CssValues.Text(d.Value) + " !important", "!important is not available in USS");
                    if (name.StartsWith("--", StringComparison.Ordinal) || d.Value == null) continue;
                    if (name.StartsWith("transition", StringComparison.Ordinal)) continue;
                    CheckDeclaration(sheet, rule, d, name, byName, pseudoElementRule, ref fontHandled);
                }
                if (_context.Config.Rules.ColorLiterals == "tokens-only" && !IsTokenRule(rule))
                {
                    foreach (Declaration d in rule.Declarations)
                    {
                        if (d.Name.StartsWith("--", StringComparison.Ordinal) || d.Value == null) continue;
                        foreach (CssValue token in CssValues.SpaceTokens(d.Value))
                        {
                            if (IsColorLiteral(token))
                            {
                                _r.Report("R004", d.Span, d.Name + ": " + CssValues.Text(d.Value), d.Name + " uses a colour literal outside the token block");
                                break;
                            }
                        }
                    }
                }
            }

            private static bool HasLegacyPseudoElement(SimpleSelector part)
            {
                foreach (PseudoClass p in part.Pseudos)
                {
                    if (PseudoNames.LegacyPseudoElements.Contains((p.Name ?? "").ToLowerInvariant())) return true;
                }
                return false;
            }

            private static bool IsTokenRule(StyleRule rule)
            {
                foreach (Selector selector in rule.Selectors)
                {
                    if ((selector.Text ?? "").Trim() != ":root") return false;
                }
                return rule.Selectors.Count > 0;
            }

            private static bool IsColorLiteral(CssValue token)
            {
                if (token is CssColor) return true;
                string fn = CssValues.FunctionName(token);
                return fn == "rgb" || fn == "rgba" || fn == "hsl" || fn == "hsla";
            }

            private void CheckDeclaration(Stylesheet sheet, StyleRule rule, Declaration d, string name, Dictionary<string, Declaration> byName, bool pseudoElementRule, ref bool fontHandled)
            {
                CssValue value = d.Value;
                string text = name + ": " + CssValues.Text(value);
                string keyword = CssValues.Keyword(value);
                if (HasMathOrGradient(value)) return;
                if (keyword == "inherit" || keyword == "unset" || keyword == "revert" || keyword == "revert-layer") return;
                if (HandledByEmulation.Contains(name) || name == "grid" || name.StartsWith("grid-", StringComparison.Ordinal)) return;
                bool hasVar = CssValues.ContainsVar(value);

                switch (name)
                {
                    case "display":
                        if (keyword == "grid" || keyword == "inline-grid") return;
                        if (!hasVar && keyword != "flex" && keyword != "none")
                        {
                            _r.Report("C019", d.Span, text, "display: " + CssValues.Text(value) + " is not available in USS");
                        }
                        return;
                    case "position":
                    case "overflow":
                        return;
                    case "box-sizing":
                        if (keyword != "border-box") _r.Report("C025", d.Span, text, "box-sizing: " + CssValues.Text(value) + " is not available; UI Toolkit always uses border-box");
                        return;
                    case "filter":
                    case "backdrop-filter":
                        return;
                    case "background":
                        CheckBackgroundShorthand(d, name, value, text, keyword, hasVar);
                        return;
                    case "background-image":
                        CheckBackgroundImage(sheet, d, value, text, keyword, hasVar);
                        return;
                    case "background-clip":
                    case "background-origin":
                    case "background-attachment":
                    case "background-blend-mode":
                        _r.Report("C012", d.Span, text, name + " has no USS equivalent");
                        return;
                    case "text-shadow":
                        CheckTextShadow(d, value, text);
                        return;
                    case "border":
                    case "border-top":
                    case "border-right":
                    case "border-bottom":
                    case "border-left":
                        CheckBorder(d, name, value, text);
                        return;
                    case "border-style":
                        if (keyword == "none" || keyword == "hidden" || keyword == "solid") return;
                        _r.Report("C024", d.Span, text, "border-style: " + CssValues.Text(value) + " is not available; USS borders are always solid");
                        return;
                    case "border-top-style":
                    case "border-right-style":
                    case "border-bottom-style":
                    case "border-left-style":
                        if (keyword != "solid" && keyword != "none") _r.Report("C024", d.Span, text, name + ": " + CssValues.Text(value) + " is not available; USS borders are always solid");
                        return;
                    case "transform":
                        CheckTransform(d, value, text);
                        return;
                    case "inset":
                        CheckLengths(d, name, CssValues.SpaceTokens(value), text, true, true, new[] { 1, 4 });
                        return;
                    case "flex-flow":
                        CheckFlexFlow(d, value, text);
                        return;
                    case "font-weight":
                    case "font-style":
                        if (fontHandled) return;
                        fontHandled = true;
                        CheckFont(byName);
                        return;
                    case "font-family":
                        CheckFontFamily(d, value, text);
                        return;
                    case "text-align":
                        CheckTextAlign(d, value, text, keyword, byName);
                        return;
                    case "content":
                        if (pseudoElementRule) return;
                        break;
                }

                if (name == "border-radius" && CssValues.Text(value).IndexOf('/') >= 0)
                {
                    _r.Report("C023", d.Span, text, "elliptical border-radius (a / b) is not supported by USS");
                    return;
                }

                if (!_caps.HasProperty(name))
                {
                    _r.Report("C014", d.Span, name + ": " + Truncate(CssValues.Text(value), 60), "'" + name + "' is not a USS property", _uss.AdviceFor(name));
                    return;
                }

                CheckPropertyValue(d, name, value, text, keyword, hasVar);
            }

            private static string Truncate(string text, int length)
            {
                return text.Length <= length ? text : text.Substring(0, length);
            }

            private static bool HasMathOrGradient(CssValue value)
            {
                foreach (CssValue inner in CssValues.Descend(value))
                {
                    string fn = CssValues.FunctionName(inner);
                    if (fn != null && (MathFunctions.Contains(fn) || GradientName.IsMatch(fn))) return true;
                }
                return false;
            }

            private void CheckBackgroundShorthand(Declaration d, string name, CssValue value, string text, string keyword, bool hasVar)
            {
                if (keyword == "none" || keyword == "transparent") return;
                IReadOnlyList<CssValue> tokens = CssValues.SpaceTokens(value);
                if (CssValues.CommaItems(value).Count == 1 && tokens.Count == 1 && (hasVar || IsColorLike(tokens[0])))
                {
                    CheckColor(d, name, tokens[0], text);
                    return;
                }
                _r.Report("C012", d.Span, text, "the background shorthand takes a single colour only in USS");
            }

            private static bool IsColorLike(CssValue token)
            {
                if (token is CssColor) return true;
                string fn = CssValues.FunctionName(token);
                if (fn == "rgb" || fn == "rgba" || UnsupportedColorFunctions.Contains(fn ?? "")) return true;
                string k = CssValues.Keyword(token);
                return k != null && k != "none" && k != "initial";
            }

            private void CheckBackgroundImage(Stylesheet sheet, Declaration d, CssValue value, string text, string keyword, bool hasVar)
            {
                if (CssValues.CommaItems(value).Count > 1)
                {
                    _r.Report("C012", d.Span, Truncate(text, 80), "USS has a single background-image; use child elements for extra layers");
                    return;
                }
                if (keyword == "none" || hasVar) return;
                var url = value as CssUrl;
                if (url == null)
                {
                    _r.Report("C023", d.Span, Truncate(text, 80), "expected none or a single url()");
                    return;
                }
                CheckUrl(sheet, d.Span, url.Url);
            }

            public void CheckUrl(Stylesheet sheet, SourceSpan span, string raw)
            {
                UrlChecks.Check(_r, _fs, sheet == null || sheet.Path == null ? null : PathMath.DirectoryOf(sheet.Path), span, raw);
            }

            private void CheckTextShadow(Declaration d, CssValue value, string text)
            {
                if (CssValues.CommaItems(value).Count > 1)
                {
                    _r.Report("C026", d.Span, Truncate(text, 80), "USS text-shadow takes exactly one shadow");
                    return;
                }
                foreach (CssValue token in CssValues.SpaceTokens(value))
                {
                    double n;
                    string unit;
                    if (CssValues.TryDimension(token, out n, out unit))
                    {
                        if (!CheckLength(d, "text-shadow", token, text, false, false)) return;
                    }
                    else if (!CheckColor(d, "text-shadow", token, text)) return;
                }
            }

            private void CheckBorder(Declaration d, string name, CssValue value, string text)
            {
                string style = null;
                CssValue width = null;
                CssValue color = null;
                foreach (CssValue token in CssValues.SpaceTokens(value))
                {
                    string k = CssValues.Keyword(token);
                    double n;
                    string unit;
                    if (k != null && BorderStyles.Contains(k)) style = k;
                    else if (CssValues.TryDimension(token, out n, out unit)) width = token;
                    else color = token;
                }
                if (style != null && BadBorderStyles.Contains(style))
                {
                    _r.Report("C024", d.Span, text, name + ": " + style + " is not available; USS borders are always solid");
                    return;
                }
                if (width != null && !CheckLength(d, name, width, text, false, false)) return;
                if (color != null) CheckColor(d, name, color, text);
            }

            private void CheckTransform(Declaration d, CssValue value, string text)
            {
                IReadOnlyList<CssValue> tokens = CssValues.SpaceTokens(value);
                foreach (CssValue token in tokens)
                {
                    var f = token as CssFunction;
                    if (f == null) return;
                    string fn = f.Name.ToLowerInvariant();
                    if (fn == "rotate" || fn == "rotatez")
                    {
                        double n;
                        string unit;
                        if (f.Args.Count != 1 || !CssValues.TryDimension(f.Args[0], out n, out unit) || unit != "deg")
                        {
                            _r.Report("C023", d.Span, text, "rotation must be in deg");
                            return;
                        }
                    }
                    else if (fn.StartsWith("translate", StringComparison.Ordinal))
                    {
                        foreach (CssValue arg in f.Args)
                        {
                            if (!CheckLength(d, "transform", arg, text, true, false)) return;
                        }
                    }
                    else if (fn.StartsWith("scale", StringComparison.Ordinal))
                    {
                        foreach (CssValue arg in f.Args)
                        {
                            double n;
                            string unit;
                            if (!CssValues.TryDimension(arg, out n, out unit) || unit != "")
                            {
                                _r.Report("C023", d.Span, text, "scale takes plain numbers");
                                return;
                            }
                        }
                    }
                }
            }

            private void CheckFlexFlow(Declaration d, CssValue value, string text)
            {
                var direction = _caps.Property("flex-direction");
                var wrap = _caps.Property("flex-wrap");
                foreach (CssValue token in CssValues.SpaceTokens(value))
                {
                    string k = CssValues.Keyword(token);
                    bool ok = k != null && ((direction != null && Contains(direction.Values, k)) || (wrap != null && Contains(wrap.Values, k)));
                    if (!ok)
                    {
                        _r.Report("C023", d.Span, text, "'" + CssValues.Text(token) + "' is not a flex-direction or flex-wrap value");
                        return;
                    }
                }
            }

            private static bool Contains(IReadOnlyList<string> list, string value)
            {
                if (list == null) return false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (string.Equals(list[i], value, StringComparison.Ordinal)) return true;
                }
                return false;
            }

            private void CheckFont(Dictionary<string, Declaration> byName)
            {
                Declaration weight;
                if (byName.TryGetValue("font-weight", out weight) && weight.Value != null && !CssValues.ContainsVar(weight.Value))
                {
                    string k = CssValues.Keyword(weight.Value);
                    double n;
                    string unit;
                    bool ok = k == "bold" || k == "bolder" || k == "normal" || k == "lighter"
                        || (CssValues.TryDimension(weight.Value, out n, out unit) && unit == "" && n == Math.Floor(n));
                    if (!ok) _r.Report("C023", weight.Span, "font-weight: " + CssValues.Text(weight.Value), "font-weight is not a keyword or an integer");
                }
                Declaration style;
                if (byName.TryGetValue("font-style", out style) && style.Value != null && !CssValues.ContainsVar(style.Value))
                {
                    string k = CssValues.Keyword(style.Value);
                    if (k != "italic" && k != "oblique" && k != "normal") _r.Report("C023", style.Span, "font-style: " + CssValues.Text(style.Value), "font-style must be normal, italic or oblique");
                }
            }

            private void CheckFontFamily(Declaration d, CssValue value, string text)
            {
                IReadOnlyList<CssValue> items = CssValues.CommaItems(value);
                string family = "";
                if (items.Count > 0)
                {
                    var s = items[0] as CssString;
                    family = s != null ? s.Value : CssValues.Text(items[0]);
                    family = family.Trim().Trim('"', '\'');
                }
                if (_context.Config.Fonts != null && _context.Config.Fonts.ContainsKey(family)) return;
                _r.Report("C031", d.Span, text, "font-family '" + family + "' has no font asset mapping");
            }

            private void CheckTextAlign(Declaration d, CssValue value, string text, string keyword, Dictionary<string, Declaration> byName)
            {
                if (byName.ContainsKey("-unity-text-align")) return;
                if (keyword != null && TextAlignMap.ContainsKey(keyword))
                {
                    _r.Report("T001", d.Span, text, "text-align: " + keyword + " maps to -unity-text-align: middle-" + TextAlignMap[keyword]);
                    return;
                }
                if (CssValues.ContainsVar(value)) return;
                _r.Report("C023", d.Span, text, "text-align: " + CssValues.Text(value) + " is not valid");
            }

            private void CheckPropertyValue(Declaration d, string name, CssValue value, string text, string keyword, bool hasVar)
            {
                if (hasVar || keyword == "initial") return;
                PropertyCapability capability = _caps.Property(name);
                IReadOnlyList<CssValue> tokens = CssValues.SpaceTokens(value);
                IReadOnlyList<string> allowed = null;
                if (_uss.Enums.ContainsKey(name))
                {
                    allowed = capability != null && capability.Values.Count > 0 ? capability.Values : _uss.Enums[name];
                }
                if (allowed != null)
                {
                    string k = tokens.Count == 1 ? CssValues.Keyword(tokens[0]) : null;
                    if (k == null || !Contains(allowed, k))
                    {
                        var sorted = new List<string>(allowed);
                        sorted.Sort(StringComparer.Ordinal);
                        _r.Report("C023", d.Span, text, "'" + CssValues.Text(value) + "' is not valid for '" + name + "' (expected " + string.Join(", ", sorted) + ")");
                    }
                    return;
                }
                if (_uss.ColorProperties.Contains(name))
                {
                    int max = name == "border-color" ? 4 : 1;
                    if (tokens.Count < 1 || tokens.Count > max)
                    {
                        _r.Report("C023", d.Span, text, name == "border-color" ? "1 to 4 colours expected" : "one colour expected");
                        return;
                    }
                    foreach (CssValue token in tokens)
                    {
                        if (!CheckColor(d, name, token, text)) return;
                    }
                    return;
                }
                if (_uss.LengthProperties.Contains(name))
                {
                    int[] count;
                    LengthCounts.TryGetValue(name, out count);
                    bool ok = CheckLengths(d, name, tokens, text, !NoPercentLengths.Contains(name), !NoAutoLengths.Contains(name), count);
                    if (ok && name == "font-size" && tokens.Count == 1)
                    {
                        double n;
                        string unit;
                        if (CssValues.TryDimension(tokens[0], out n, out unit) && unit == "px" && n < _context.Config.Rules.MinFontSize)
                        {
                            _r.Report("R001", d.Span, "font-size: " + CssValues.Text(tokens[0]), "font-size " + CssValues.Text(tokens[0]) + " is below the minimum " + CssFormat.Number(_context.Config.Rules.MinFontSize) + "px");
                        }
                    }
                    return;
                }
                if (_uss.NumberProperties.Contains(name))
                {
                    if (name == "aspect-ratio") return;
                    double n;
                    string unit;
                    if (tokens.Count != 1 || !CssValues.TryDimension(tokens[0], out n, out unit) || (unit != "" && unit != "%"))
                    {
                        _r.Report("C023", d.Span, text, "a number is expected");
                    }
                    return;
                }
                switch (name)
                {
                    case "flex":
                        CheckFlex(d, tokens, text, keyword);
                        return;
                    case "rotate":
                    {
                        double n;
                        string unit;
                        if (tokens.Count != 1 || !CssValues.TryDimension(tokens[0], out n, out unit) || unit != "deg")
                        {
                            _r.Report("C023", d.Span, text, "rotation must be in deg");
                        }
                        return;
                    }
                    case "scale":
                    {
                        foreach (CssValue token in tokens)
                        {
                            double n;
                            string unit;
                            if (!CssValues.TryDimension(token, out n, out unit) || unit != "")
                            {
                                _r.Report("C023", d.Span, text, "scale takes plain numbers");
                                return;
                            }
                        }
                        return;
                    }
                    case "translate":
                    case "transform-origin":
                    case "background-position":
                    case "background-position-x":
                    case "background-position-y":
                    case "background-size":
                    {
                        foreach (CssValue token in tokens)
                        {
                            double n;
                            string unit;
                            if (!CssValues.TryDimension(token, out n, out unit)) continue;
                            if (!KnownLengthUnit(unit)) continue;
                            if (unit == "" && n != 0)
                            {
                                _r.Report("C023", d.Span, text, "unitless length '" + CssValues.Text(token) + "' is invalid CSS");
                                return;
                            }
                        }
                        return;
                    }
                }
            }

            private void CheckFlex(Declaration d, IReadOnlyList<CssValue> tokens, string text, string keyword)
            {
                if (keyword == "none" || keyword == "initial") return;
                if (keyword == "auto")
                {
                    _r.Report("C023", d.Span, "flex: auto", "'flex: auto' is not accepted by USS", "Use flex: 1 1 auto.");
                    return;
                }
                foreach (CssValue token in tokens)
                {
                    double n;
                    string unit;
                    if (!CssValues.TryDimension(token, out n, out unit))
                    {
                        if (CssValues.Keyword(token) == "auto") continue;
                        _r.Report("C023", d.Span, text, "'" + CssValues.Text(token) + "' is not valid in flex");
                        return;
                    }
                }
            }

            private static bool KnownLengthUnit(string unit)
            {
                return unit == "" || unit == "px" || unit == "%";
            }

            private bool CheckLengths(Declaration d, string name, IReadOnlyList<CssValue> tokens, string text, bool allowPercent, bool allowAuto, int[] count)
            {
                int used = 0;
                foreach (CssValue token in tokens)
                {
                    string k = CssValues.Keyword(token);
                    if (CssValues.ContainsVar(token)) { used++; continue; }
                    if (k == "initial" || (allowAuto && (k == "auto" || k == "none")) || (k == "none" && name.StartsWith("max-", StringComparison.Ordinal)))
                    {
                        used++;
                        continue;
                    }
                    if (!CheckLength(d, name, token, text, allowPercent, allowAuto)) return false;
                    used++;
                }
                if (count != null && (used < count[0] || used > count[1]))
                {
                    _r.Report("C023", d.Span, text, "'" + name + "' takes " + count[0] + " to " + count[1] + " values");
                    return false;
                }
                return true;
            }

            private bool CheckLength(Declaration d, string name, CssValue token, string text, bool allowPercent, bool allowAuto)
            {
                if (CssValues.ContainsVar(token)) return true;
                double n;
                string unit;
                if (!CssValues.TryDimension(token, out n, out unit))
                {
                    string fn = CssValues.FunctionName(token);
                    if (fn != null && MathFunctions.Contains(fn)) return true;
                    _r.Report("C023", d.Span, text, "'" + CssValues.Text(token) + "' is not a valid length for '" + name + "'");
                    return false;
                }
                if (unit == "" && n != 0)
                {
                    _r.Report("C023", d.Span, text, "unitless length '" + CssValues.Text(token) + "' is invalid CSS; write " + CssFormat.Number(n) + "px");
                    return false;
                }
                if (unit == "%" && !allowPercent)
                {
                    _r.Report("C023", d.Span, text, "percentages are not valid for '" + name + "'");
                    return false;
                }
                if (unit == "deg" || unit == "s" || unit == "ms")
                {
                    _r.Report("C023", d.Span, text, "unit '" + unit + "' is not valid for '" + name + "'");
                    return false;
                }
                return true;
            }

            private bool CheckColor(Declaration d, string name, CssValue token, string text)
            {
                if (CssValues.ContainsVar(token)) return true;
                string k = CssValues.Keyword(token);
                if (k == "currentcolor" || k == "inherit")
                {
                    _r.Report("C023", d.Span, text, "'currentColor' is not available in USS", "Write the colour explicitly or use a variable.");
                    return false;
                }
                string fn = CssValues.FunctionName(token);
                if (fn != null && UnsupportedColorFunctions.Contains(fn))
                {
                    _r.Report("C023", d.Span, text, "colour function is not supported by USS (use hex, rgb() or rgba())");
                    return false;
                }
                if (token is CssColor || fn == "rgb" || fn == "rgba") return true;
                if (k != null && k.Length > 0)
                {
                    foreach (char c in k)
                    {
                        if (c < 'a' || c > 'z')
                        {
                            _r.Report("C023", d.Span, text, "'" + CssValues.Text(token) + "' is not a valid colour");
                            return false;
                        }
                    }
                    return true;
                }
                _r.Report("C023", d.Span, text, "'" + CssValues.Text(token) + "' is not a valid colour");
                return false;
            }

            public void CheckTokens(AnalysisInput input)
            {
                if (input.Tokens == null) return;
                var skin = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (HtmlToUitk.Convert.TokenEntry entry in input.Tokens.Entries)
                {
                    if (entry.FromSkin) skin[entry.Name] = Normal(CssValues.Text(entry.Value));
                }
                foreach (HtmlToUitk.Convert.TokenEntry entry in input.Tokens.Entries)
                {
                    string skinValue;
                    if (entry.FromSkin || !skin.TryGetValue(entry.Name, out skinValue)) continue;
                    string own = Normal(CssValues.Text(entry.Value));
                    if (own != skinValue)
                    {
                        _r.Report("C034", entry.Span, entry.Name + ": " + CssValues.Text(entry.Value), "token " + entry.Name + " is '" + skinValue + "' in the skin and '" + own + "' here");
                    }
                }
            }

            private static string Normal(string text)
            {
                return Regex.Replace(text ?? "", @"\s+", "").ToLowerInvariant();
            }
        }
    }

    internal static class PathMath
    {
        public static string DirectoryOf(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string p = path.Replace('\\', '/');
            int slash = p.LastIndexOf('/');
            return slash < 0 ? "" : p.Substring(0, slash);
        }

        public static string Combine(string directory, string relative)
        {
            string rel = (relative ?? "").Replace('\\', '/');
            if (rel.Length == 0) return directory;
            var parts = new List<string>();
            string joined = string.IsNullOrEmpty(directory) ? rel : directory.TrimEnd('/') + "/" + rel;
            bool rooted = joined.StartsWith("/", StringComparison.Ordinal);
            string prefix = "";
            string rest = joined;
            if (joined.Length >= 2 && joined[1] == ':') { prefix = joined.Substring(0, 2); rest = joined.Substring(2); rooted = rest.StartsWith("/", StringComparison.Ordinal); }
            foreach (string segment in rest.Split('/'))
            {
                if (segment.Length == 0 || segment == ".") continue;
                if (segment == ".." && parts.Count > 0 && parts[parts.Count - 1] != "..") { parts.RemoveAt(parts.Count - 1); continue; }
                parts.Add(segment);
            }
            return prefix + (rooted ? "/" : "") + string.Join("/", parts);
        }
    }
}
