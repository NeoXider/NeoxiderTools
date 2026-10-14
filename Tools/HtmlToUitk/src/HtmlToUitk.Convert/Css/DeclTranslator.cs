using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Spec;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Convert.Css
{
    internal sealed class UssDecl
    {
        public string Name;
        public string Value;
        public SourceSpan Span;

        public UssDecl(string name, string value, SourceSpan span)
        {
            Name = name;
            Value = value;
            Span = span;
        }
    }

    internal sealed class TranslateEnv
    {
        public CapabilityMatrix Caps;
        public ConversionConfig Config;
        public IFileSystem Fs;
        public string ProjectRoot = ".";
        public string BaseDirectory = "";
        public HashSet<string> GeneratedAssets = new HashSet<string>(StringComparer.Ordinal);
    }

    internal static class DeclTranslator
    {
        private static readonly Regex UrlRe = new Regex(@"^url\(\s*(.*?)\s*\)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private static readonly Regex MathRe = new Regex(@"\b(calc|min|max|clamp|env|attr)\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex GradientRe = new Regex(@"\b(?:repeating-)?(?:linear|radial|conic)-gradient\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex PureNumber = new Regex(@"^\d+$", RegexOptions.CultureInvariant);
        private static readonly Regex TransformRe = new Regex(@"^(rotate|rotatez|translate|translatex|translatey|scale|scalex|scaley)\(([^()]*)\)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex TimeRe = new Regex(@"^([+-]?(?:\d+\.?\d*|\.\d+))(s|ms)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> BadUnits = new HashSet<string>(StringComparer.Ordinal)
        {
            "em", "rem", "vh", "vw", "vmin", "vmax", "pt", "ch", "ex", "fr", "cm", "mm", "in", "pc"
        };

        private static readonly HashSet<string> CssWide = new HashSet<string>(StringComparer.Ordinal) { "inherit", "unset", "revert", "revert-layer" };
        private static readonly HashSet<string> BorderStyles = new HashSet<string>(StringComparer.Ordinal) { "none", "hidden", "solid", "dashed", "dotted", "double", "groove", "ridge", "inset", "outset" };
        private static readonly HashSet<string> BadBorderStyles = new HashSet<string>(StringComparer.Ordinal) { "dashed", "dotted", "double", "groove", "ridge", "inset", "outset" };

        private static readonly HashSet<string> FlexAnimationNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "animation", "animation-name", "animation-duration", "animation-delay", "animation-iteration-count",
            "animation-direction", "animation-fill-mode", "animation-timing-function", "animation-play-state"
        };

        private static readonly Dictionary<string, string> TextAlignMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "left", "left" }, { "start", "left" }, { "center", "center" }, { "right", "right" }, { "end", "right" }, { "justify", "left" }
        };

        public static bool IsAnimationProperty(string name)
        {
            return FlexAnimationNames.Contains(name);
        }

        public static List<UssDecl> Translate(IReadOnlyList<Declaration> declarations, TranslateEnv env)
        {
            List<UssDecl> input = new List<UssDecl>(declarations.Count);
            Dictionary<string, Declaration> names = new Dictionary<string, Declaration>(StringComparer.Ordinal);
            for (int i = 0; i < declarations.Count; i++)
            {
                names[declarations[i].Name] = declarations[i];
            }

            List<UssDecl> output = new List<UssDecl>();
            bool handledFont = false;
            for (int i = 0; i < declarations.Count; i++)
            {
                Declaration decl = declarations[i];
                string name = decl.Name;
                string value = decl.Value == null ? "" : decl.Value.ToText().Trim();
                if (name == "-unity-background-scale-mode" && value == "stretch-to-fill")
                {
                    output.Add(new UssDecl(name, value, decl.Span));
                    continue;
                }
                if (name.StartsWith("--", StringComparison.Ordinal))
                {
                    output.Add(new UssDecl(name, value, decl.Span));
                    continue;
                }

                if (!CommonValueChecks(env, value)) continue;
                if (name == "gap" || name == "row-gap" || name == "column-gap" || name == "grid-gap" || name == "grid-row-gap" || name == "grid-column-gap")
                {
                    if (env.Caps.HasProperty(name)) output.Add(new UssDecl(name, value, decl.Span));
                    continue;
                }

                string low = value.ToLowerInvariant();
                if (name == "display")
                {
                    if (low == "flex" || low == "none" || low.Contains("var(")) output.Add(new UssDecl(name, low.Contains("var(") ? value : low, decl.Span));
                    continue;
                }

                if (name == "position")
                {
                    if (low == "relative" || low == "absolute" || low.Contains("var(")) output.Add(new UssDecl(name, low.Contains("var(") ? value : low, decl.Span));
                    continue;
                }

                if (name == "z-index" || name == "box-shadow") continue;
                if (name == "overflow" || name == "overflow-x" || name == "overflow-y")
                {
                    if (name == "overflow" && (low == "visible" || low == "hidden")) output.Add(new UssDecl(name, low, decl.Span));
                    else if (name == "overflow" && low.Contains("var(")) output.Add(new UssDecl(name, value, decl.Span));
                    continue;
                }

                if (name == "box-sizing") continue;
                if (name == "filter" || name == "backdrop-filter")
                {
                    if (env.Caps.HasProperty(name)) output.Add(new UssDecl(name, value, decl.Span));
                    continue;
                }

                if (name == "background")
                {
                    if (low == "none" || low == "transparent") output.Add(new UssDecl("background-color", "rgba(0, 0, 0, 0)", decl.Span));
                    else if (CssText.SplitWs(value).Count == 1 && (IsColor(value) || low.Contains("var(")))
                    {
                        string norm = NormColor(value.Trim());
                        if (norm != null) output.Add(new UssDecl("background-color", norm, decl.Span));
                    }

                    continue;
                }

                if (name == "background-image")
                {
                    if (CssText.SplitTop(value, ',').Count > 1) continue;
                    if (low == "none" || low.Contains("var(")) output.Add(new UssDecl(name, low == "none" ? "none" : value, decl.Span));
                    else
                    {
                        Match match = UrlRe.Match(value.Trim());
                        if (match.Success)
                        {
                            string url = ResolveUrl(env, match.Groups[1].Value, decl);
                            if (url != null) output.Add(new UssDecl(name, url, decl.Span));
                        }
                        else if (GradientNative(env, low)) output.Add(new UssDecl(name, value, decl.Span));
                    }

                    continue;
                }

                if (name == "background-clip" || name == "background-origin" || name == "background-attachment" || name == "background-blend-mode") continue;
                if (name == "text-shadow")
                {
                    if (CssText.SplitTop(value, ',').Count > 1) continue;
                    List<string> fixedTokens = new List<string>();
                    bool ok = true;
                    foreach (string token in CssText.SplitWs(value))
                    {
                        double n;
                        string u;
                        if (CssText.TryNumber(token, out n, out u))
                        {
                            List<string> got = CheckLengths(token, false, false, 0, 0);
                            if (got == null)
                            {
                                ok = false;
                                break;
                            }

                            fixedTokens.Add(got[0]);
                        }
                        else
                        {
                            string norm = NormColor(token);
                            if (norm == null)
                            {
                                ok = false;
                                break;
                            }

                            fixedTokens.Add(norm);
                        }
                    }

                    if (ok) output.Add(new UssDecl(name, string.Join(" ", fixedTokens), decl.Span));
                    continue;
                }

                if (name == "border" || name == "border-top" || name == "border-right" || name == "border-bottom" || name == "border-left")
                {
                    HandleBorder(env, decl, name, value, output);
                    continue;
                }

                if (name == "border-style")
                {
                    if (low == "none" || low == "hidden") output.Add(new UssDecl("border-width", "0", decl.Span));
                    continue;
                }

                if (name == "border-top-style" || name == "border-right-style" || name == "border-bottom-style" || name == "border-left-style") continue;
                if (name == "border-radius" && value.Contains("/")) continue;
                if (name == "transform")
                {
                    HandleTransform(decl, value, output);
                    continue;
                }

                if (name == "inset")
                {
                    List<string> fixedValues = CheckLengths(value, true, true, 1, 4);
                    if (fixedValues != null && fixedValues.Count > 0)
                    {
                        List<string> sides = ExpandSides(fixedValues);
                        string[] keys = { "top", "right", "bottom", "left" };
                        for (int k = 0; k < 4; k++) output.Add(new UssDecl(keys[k], sides[k], decl.Span));
                    }

                    continue;
                }

                if (name == "flex-flow")
                {
                    foreach (string token in CssText.SplitWs(value))
                    {
                        string t = token.ToLowerInvariant();
                        if (EnumAllows("flex-direction", t, env)) output.Add(new UssDecl("flex-direction", t, decl.Span));
                        else if (EnumAllows("flex-wrap", t, env)) output.Add(new UssDecl("flex-wrap", t, decl.Span));
                        else break;
                    }

                    continue;
                }

                if (name == "font-weight" || name == "font-style")
                {
                    if (handledFont) continue;
                    handledFont = true;
                    Declaration weight, style;
                    names.TryGetValue("font-weight", out weight);
                    names.TryGetValue("font-style", out style);
                    bool bold = false;
                    bool italic = false;
                    bool ok = true;
                    if (weight != null)
                    {
                        string wl = weight.Value.ToText().Trim().ToLowerInvariant();
                        if (wl == "bold" || wl == "bolder") bold = true;
                        else if (wl == "normal" || wl == "lighter") bold = false;
                        else if (PureNumber.IsMatch(wl)) bold = int.Parse(wl, System.Globalization.CultureInfo.InvariantCulture) >= 600;
                        else ok = false;
                    }

                    if (style != null)
                    {
                        string sl = style.Value.ToText().Trim().ToLowerInvariant();
                        if (sl == "italic" || sl == "oblique") italic = true;
                        else if (sl != "normal") ok = false;
                    }

                    if (ok) output.Add(new UssDecl("-unity-font-style", bold && italic ? "bold-and-italic" : bold ? "bold" : italic ? "italic" : "normal", decl.Span));
                    continue;
                }

                if (name == "font-family")
                {
                    string family = CssText.SplitTop(value, ',')[0].Trim().Trim('"', '\'');
                    string path;
                    if (env.Config.Fonts != null && env.Config.Fonts.TryGetValue(family, out path)) output.Add(new UssDecl("-unity-font-definition", "url(\"" + path + "\")", decl.Span));
                    continue;
                }

                if (name == "text-align")
                {
                    if (names.ContainsKey("-unity-text-align")) continue;
                    string mapped;
                    if (TextAlignMap.TryGetValue(low, out mapped)) output.Add(new UssDecl("-unity-text-align", "middle-" + mapped, decl.Span));
                    continue;
                }

                if (name.StartsWith("transition", StringComparison.Ordinal))
                {
                    HandleTransition(env, decl, name, value, output);
                    continue;
                }

                if (FlexAnimationNames.Contains(name)) continue;
                if (!env.Caps.HasProperty(name)) continue;
                List<UssDecl> checkedDecls = CheckPropertyValue(env, decl, name, value);
                if (checkedDecls != null) output.AddRange(checkedDecls);
            }

            return DedupeDecls(output);
        }

        private static bool CommonValueChecks(TranslateEnv env, string value)
        {
            if (MathRe.IsMatch(value)) return false;
            if (GradientRe.IsMatch(value))
            {
                return GradientNative(env, value.ToLowerInvariant());
            }

            string trimmed = value.Trim().ToLowerInvariant();
            if (CssWide.Contains(trimmed)) return env.Caps.HasKeyword(trimmed) && trimmed != "revert" && trimmed != "revert-layer";
            return true;
        }

        private static bool GradientNative(TranslateEnv env, string lowValue)
        {
            string text = lowValue.TrimStart();
            return text.StartsWith("linear-gradient(", StringComparison.Ordinal) && env.Caps.HasFunction("linear-gradient");
        }

        private static bool IsColor(string value)
        {
            ColorValue color;
            return CssText.TryColor(value, out color);
        }

        private static string NormColor(string token)
        {
            string low = token.ToLowerInvariant();
            if (low.Contains("var(")) return token;
            if (low == "currentcolor" || low == "inherit") return null;
            if (low.StartsWith("hsl(", StringComparison.Ordinal) || low.StartsWith("hsla(", StringComparison.Ordinal) || low.StartsWith("hwb(", StringComparison.Ordinal)
                || low.StartsWith("lab(", StringComparison.Ordinal) || low.StartsWith("lch(", StringComparison.Ordinal) || low.StartsWith("oklab(", StringComparison.Ordinal)
                || low.StartsWith("oklch(", StringComparison.Ordinal) || low.StartsWith("color(", StringComparison.Ordinal) || low.StartsWith("color-mix(", StringComparison.Ordinal)) return null;
            ColorValue parsed;
            if (!CssText.TryColor(token, out parsed))
            {
                if (Regex.IsMatch(low, "^[a-z]+$")) return low;
                return null;
            }

            if (token.StartsWith("#", StringComparison.Ordinal) && (token.Length == 4 || token.Length == 7)) return token;
            return CssText.FormatColor(parsed);
        }

        private static List<string> CheckLengths(string value, bool allowPercent, bool allowAuto, int minCount, int maxCount, string propertyName = null)
        {
            List<string> tokens = CssText.SplitWs(value);
            List<string> output = new List<string>();
            foreach (string token in tokens)
            {
                string low = token.ToLowerInvariant();
                if (low.Contains("var("))
                {
                    output.Add(token);
                    continue;
                }

                if (low == "initial" || (allowAuto && (low == "auto" || low == "none")) || (low == "none" && propertyName != null && propertyName.StartsWith("max-", StringComparison.Ordinal)))
                {
                    output.Add(low);
                    continue;
                }

                double n;
                string unit;
                if (!CssText.TryNumber(token, out n, out unit)) return null;
                if (BadUnits.Contains(unit)) return null;
                if (unit == "" && n != 0) return null;
                if (unit != "" && unit != "px" && unit != "%") return null;
                if (unit == "%" && !allowPercent) return null;
                output.Add(CssText.FormatLength(n, unit));
            }

            if (maxCount > 0 && !(minCount <= output.Count && output.Count <= maxCount)) return null;
            return output;
        }

        private static List<string> ExpandSides(List<string> values)
        {
            if (values.Count == 1) return new List<string> { values[0], values[0], values[0], values[0] };
            if (values.Count == 2) return new List<string> { values[0], values[1], values[0], values[1] };
            if (values.Count == 3) return new List<string> { values[0], values[1], values[2], values[1] };
            return new List<string> { values[0], values[1], values[2], values[3] };
        }

        private static bool EnumAllows(string property, string value, TranslateEnv env)
        {
            PropertyCapability cap = env.Caps.Property(property);
            if (cap != null && cap.Values != null && cap.Values.Count > 0)
            {
                for (int i = 0; i < cap.Values.Count; i++) if (cap.Values[i] == value) return true;
                return false;
            }

            HashSet<string> set;
            return UssSpec.Instance.Enums.TryGetValue(property, out set) && set.Contains(value);
        }

        public static string ResolveUrl(TranslateEnv env, string raw, Declaration decl)
        {
            string text = raw.Trim();
            if (text.Length >= 2 && (text[0] == '"' || text[0] == '\'') && text[text.Length - 1] == text[0]) text = text.Substring(1, text.Length - 2);
            if (text.StartsWith(HtmlToUitk.Convert.Writers.UssText.GeneratedMarker, StringComparison.Ordinal))
            {
                string generated = text.Substring(HtmlToUitk.Convert.Writers.UssText.GeneratedMarker.Length);
                if (!env.GeneratedAssets.Contains(generated)) throw new InvalidOperationException("unregistered generated asset reference: " + generated);
                return "url(\"" + text + "\")";
            }
            string low = text.ToLowerInvariant();
            if (low.StartsWith("project://", StringComparison.Ordinal) || low.StartsWith("/", StringComparison.Ordinal) || low.StartsWith("resource(", StringComparison.Ordinal))
            {
                return "url(\"" + text + "\")";
            }

            if (low.StartsWith("http:", StringComparison.Ordinal) || low.StartsWith("https:", StringComparison.Ordinal) || low.StartsWith("data:", StringComparison.Ordinal)
                || low.StartsWith("file:", StringComparison.Ordinal) || low.StartsWith("//", StringComparison.Ordinal)) return null;
            string baseDir = env.BaseDirectory;
            if (decl != null && decl.Span.File != null)
            {
                baseDir = PathUtil.Directory(decl.Span.File);
            }

            string full = PathUtil.Combine(baseDir, text);
            string rel = PathUtil.RelativeTo(env.ProjectRoot, full);
            if (rel.StartsWith("..", StringComparison.Ordinal)) return null;
            if (env.Fs != null && !env.Fs.FileExists(full)) return null;
            return "url(\"/" + rel + "\")";
        }

        private static void HandleBorder(TranslateEnv env, Declaration decl, string name, string value, List<UssDecl> output)
        {
            string side = name == "border" ? null : name.Substring("border-".Length);
            string width = null;
            string style = null;
            string color = null;
            foreach (string token in CssText.SplitWs(value))
            {
                string low = token.ToLowerInvariant();
                double n;
                string u;
                if (BorderStyles.Contains(low)) style = low;
                else if (CssText.TryNumber(token, out n, out u)) width = token;
                else color = token;
            }

            if (style != null && BadBorderStyles.Contains(style)) return;
            double parsedWidth = 0;
            bool hasWidth = false;
            if (width != null)
            {
                string unit;
                hasWidth = CssText.TryNumber(width, out parsedWidth, out unit);
            }

            if (style == "none" || style == "hidden" || (hasWidth && parsedWidth == 0))
            {
                width = "0";
                color = null;
            }
            else if (width == null && style == "solid") width = "3px";

            string widthName = side == null ? "border-width" : "border-" + side + "-width";
            string colorName = side == null ? "border-color" : "border-" + side + "-color";
            if (width != null)
            {
                List<string> fixedWidth = CheckLengths(width, false, false, 0, 0);
                if (fixedWidth == null) return;
                output.Add(new UssDecl(widthName, fixedWidth[0], decl.Span));
            }

            if (color != null)
            {
                string norm = NormColor(color);
                if (norm == null) return;
                output.Add(new UssDecl(colorName, norm, decl.Span));
            }
        }

        private static void HandleTransform(Declaration decl, string value, List<UssDecl> output)
        {
            string text = value.Trim();
            if (text.ToLowerInvariant() == "none")
            {
                output.Add(new UssDecl("rotate", "0deg", decl.Span));
                return;
            }

            Match match = TransformRe.Match(text);
            if (!match.Success) return;
            string func = match.Groups[1].Value.ToLowerInvariant();
            List<string> args = new List<string>();
            foreach (string part in match.Groups[2].Value.Split(',')) args.Add(part.Trim());
            if (func == "rotate" || func == "rotatez")
            {
                double n;
                string u;
                if (args.Count != 1 || !CssText.TryNumber(args[0], out n, out u) || u != "deg") return;
                output.Add(new UssDecl("rotate", CssText.FormatLength(n, "deg"), decl.Span));
            }
            else if (func.StartsWith("translate", StringComparison.Ordinal))
            {
                List<string> values = new List<string>();
                foreach (string arg in args)
                {
                    List<string> fixedArg = CheckLengths(arg, true, false, 0, 0);
                    if (fixedArg == null || fixedArg.Count == 0) return;
                    values.Add(fixedArg[0]);
                }

                if (func == "translatex") values = new List<string> { values[0], "0" };
                else if (func == "translatey") values = new List<string> { "0", values[0] };
                output.Add(new UssDecl("translate", string.Join(" ", values), decl.Span));
            }
            else
            {
                List<string> values = new List<string>();
                foreach (string arg in args)
                {
                    double n;
                    string u;
                    if (!CssText.TryNumber(arg, out n, out u) || u != "") return;
                    values.Add(CssText.FormatNumber(n));
                }

                if (func == "scalex") values = new List<string> { values[0], "1" };
                else if (func == "scaley") values = new List<string> { "1", values[0] };
                output.Add(new UssDecl("scale", string.Join(" ", values), decl.Span));
            }
        }

        private static string NormTime(string token)
        {
            Match match = TimeRe.Match(token.Trim());
            if (!match.Success) return null;
            double n = double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            return CssText.FormatLength(n, match.Groups[2].Value.ToLowerInvariant());
        }

        private static bool TranslateTransitionList(TranslateEnv env, List<string> items, out List<string> props, out List<string> durations, out List<string> timings, out List<string> delays)
        {
            props = new List<string>();
            durations = new List<string>();
            timings = new List<string>();
            delays = new List<string>();
            foreach (string item in items)
            {
                string prop = null;
                List<string> times = new List<string>();
                string timing = null;
                foreach (string token in CssText.SplitWs(item))
                {
                    string low = token.ToLowerInvariant();
                    string time = NormTime(token);
                    if (time != null) times.Add(time);
                    else if (UssSpec.Instance.Easings.Contains(low) || low.StartsWith("cubic-bezier(", StringComparison.Ordinal) || low.StartsWith("steps(", StringComparison.Ordinal) || low == "step-start" || low == "step-end") timing = low;
                    else if (prop == null) prop = low;
                    else return false;
                }

                if (prop == null) prop = "all";
                if (times.Count > 2) return false;
                if (timing != null && !EasingKnown(env, timing)) return false;
                List<string> expanded = prop == "transform" ? new List<string> { "translate", "rotate", "scale" } : new List<string> { prop };
                foreach (string single in expanded)
                {
                    bool known = single == "all" || single == "none" || single == "initial" || single == "ignored" || env.Caps.HasProperty(single) || single.StartsWith("--", StringComparison.Ordinal);
                    if (!known) return false;
                    props.Add(single);
                    durations.Add(times.Count > 0 ? times[0] : "0s");
                    timings.Add(timing ?? "ease");
                    delays.Add(times.Count > 1 ? times[1] : "0s");
                }
            }

            return true;
        }

        private static bool EasingKnown(TranslateEnv env, string easing)
        {
            if (env.Caps.HasEasing(easing)) return true;
            return UssSpec.Instance.Easings.Contains(easing);
        }

        private static void HandleTransition(TranslateEnv env, Declaration decl, string name, string value, List<UssDecl> output)
        {
            List<string> items = new List<string>();
            foreach (string part in CssText.SplitTop(value, ','))
            {
                if (part.Trim().Length > 0) items.Add(part.Trim());
            }

            if (name == "transition")
            {
                if (items.Count == 0 || value.Trim().ToLowerInvariant() == "none")
                {
                    output.Add(new UssDecl("transition-property", "none", decl.Span));
                    return;
                }

                List<string> props, durations, timings, delays;
                if (!TranslateTransitionList(env, items, out props, out durations, out timings, out delays)) return;
                output.Add(new UssDecl("transition-property", string.Join(", ", props), decl.Span));
                output.Add(new UssDecl("transition-duration", string.Join(", ", durations), decl.Span));
                if (timings.Exists(t => t != "ease")) output.Add(new UssDecl("transition-timing-function", string.Join(", ", timings), decl.Span));
                if (delays.Exists(d => d != "0s")) output.Add(new UssDecl("transition-delay", string.Join(", ", delays), decl.Span));
                return;
            }

            if (name == "transition-property")
            {
                List<string> fake = new List<string>();
                foreach (string part in items) fake.Add(part + " 0s");
                List<string> props, durations, timings, delays;
                if (TranslateTransitionList(env, fake, out props, out durations, out timings, out delays)) output.Add(new UssDecl("transition-property", string.Join(", ", props), decl.Span));
                return;
            }

            if (name == "transition-duration" || name == "transition-delay")
            {
                List<string> values = new List<string>();
                foreach (string part in items)
                {
                    string norm = part.ToLowerInvariant() != "initial" ? NormTime(part) : "0s";
                    if (norm == null) return;
                    values.Add(norm);
                }

                output.Add(new UssDecl(name, string.Join(", ", values), decl.Span));
                return;
            }

            if (name == "transition-timing-function")
            {
                List<string> values = new List<string>();
                foreach (string part in items)
                {
                    string low = part.ToLowerInvariant();
                    if (!EasingKnown(env, low)) return;
                    values.Add(low);
                }

                output.Add(new UssDecl(name, string.Join(", ", values), decl.Span));
            }
        }

        private static List<UssDecl> CheckPropertyValue(TranslateEnv env, Declaration decl, string name, string value)
        {
            string low = value.ToLowerInvariant();
            UssSpec spec = UssSpec.Instance;
            if (low.Contains("var(")) return One(name, value, decl);
            if (low == "initial") return One(name, "initial", decl);
            PropertyCapability cap = env.Caps.Property(name);
            bool enumProp = (cap != null && cap.Values != null && cap.Values.Count > 0) || spec.Enums.ContainsKey(name);
            if (enumProp)
            {
                List<string> tokens = CssText.SplitWs(low);
                if (tokens.Count != 1 || !EnumAllows(name, tokens[0], env)) return null;
                return One(name, tokens[0], decl);
            }

            if (spec.ColorProperties.Contains(name))
            {
                List<string> tokens = CssText.SplitWs(value);
                if (name == "border-color")
                {
                    if (tokens.Count < 1 || tokens.Count > 4) return null;
                }
                else if (tokens.Count != 1) return null;

                List<string> fixedTokens = new List<string>();
                foreach (string token in tokens)
                {
                    string norm = NormColor(token);
                    if (norm == null) return null;
                    fixedTokens.Add(norm);
                }

                return One(name, string.Join(" ", fixedTokens), decl);
            }

            if (spec.LengthProperties.Contains(name))
            {
                int min = 0, max = 0;
                if (name == "margin" || name == "padding" || name == "border-width" || name == "border-radius")
                {
                    min = 1;
                    max = 4;
                }

                bool allowPercent = !(name == "border-width" || name == "border-top-width" || name == "border-right-width" || name == "border-bottom-width"
                    || name == "border-left-width" || name == "font-size" || name == "letter-spacing" || name == "word-spacing");
                bool allowAuto = !(name == "border-radius" || name == "font-size" || name == "letter-spacing" || name == "word-spacing");
                List<string> fixedValues = CheckLengths(value, allowPercent, allowAuto, min, max, name);
                if (fixedValues == null) return null;
                return One(name, string.Join(" ", fixedValues), decl);
            }

            if (spec.NumberProperties.Contains(name))
            {
                if (name == "aspect-ratio") return One(name, value, decl);
                double n;
                string u;
                if (!CssText.TryNumber(value, out n, out u) || (u != "" && u != "%")) return null;
                double number = u == "%" ? n / 100.0 : n;
                return One(name, CssText.FormatNumber(number), decl);
            }

            if (name == "flex")
            {
                List<string> tokens = CssText.SplitWs(low);
                if (tokens.Count == 1 && (tokens[0] == "none" || tokens[0] == "initial")) return One(name, tokens[0], decl);
                if (tokens.Count == 1 && tokens[0] == "auto") return null;
                List<string> fixedTokens = new List<string>();
                foreach (string token in tokens)
                {
                    double n;
                    string u;
                    bool isNumber = CssText.TryNumber(token, out n, out u);
                    if (!isNumber && token != "auto") return null;
                    if (isNumber && BadUnits.Contains(u)) return null;
                    fixedTokens.Add(isNumber ? CssText.FormatLength(n, u) : token);
                }

                return One(name, string.Join(" ", fixedTokens), decl);
            }

            if (name == "rotate")
            {
                double n;
                string u;
                if (!CssText.TryNumber(value, out n, out u) || u != "deg") return null;
                return One(name, CssText.FormatLength(n, "deg"), decl);
            }

            if (name == "translate" || name == "transform-origin" || name == "background-position" || name == "background-position-x"
                || name == "background-position-y" || name == "background-size")
            {
                List<string> fixedTokens = new List<string>();
                foreach (string token in CssText.SplitWs(value))
                {
                    double n;
                    string u;
                    if (!CssText.TryNumber(token, out n, out u))
                    {
                        fixedTokens.Add(token.ToLowerInvariant());
                        continue;
                    }

                    if (BadUnits.Contains(u)) return null;
                    if (u == "" && n != 0) return null;
                    fixedTokens.Add(CssText.FormatLength(n, u));
                }

                return One(name, string.Join(" ", fixedTokens), decl);
            }

            if (name == "scale")
            {
                List<string> fixedTokens = new List<string>();
                foreach (string token in CssText.SplitWs(value))
                {
                    double n;
                    string u;
                    if (!CssText.TryNumber(token, out n, out u) || u != "") return null;
                    fixedTokens.Add(CssText.FormatNumber(n));
                }

                return One(name, string.Join(" ", fixedTokens), decl);
            }

            return One(name, value, decl);
        }

        private static List<UssDecl> One(string name, string value, Declaration decl)
        {
            return new List<UssDecl> { new UssDecl(name, value, decl.Span) };
        }

        private static List<UssDecl> DedupeDecls(List<UssDecl> output)
        {
            Dictionary<string, int> last = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < output.Count; i++) last[output[i].Name] = i;
            List<UssDecl> result = new List<UssDecl>();
            for (int i = 0; i < output.Count; i++)
            {
                if (last[output[i].Name] == i) result.Add(output[i]);
            }

            return result;
        }
    }
}
