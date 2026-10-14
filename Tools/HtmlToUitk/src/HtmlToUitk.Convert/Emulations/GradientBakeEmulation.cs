using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HtmlToUitk.Bake;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Text;
using HtmlToUitk.Rules;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Convert.Writers;

namespace HtmlToUitk.Convert.Emulations
{
    public sealed class GradientBakeEmulation : EmulationBase
    {
        public override string Id => EmulationIds.GradientBake;
        public override Tier Tier => Tier.ConvertTime;
        public override IReadOnlyList<string> Handles => new[] { "linear-gradient", "radial-gradient", "conic-gradient" };
        public override int Order => 70;

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            long totalPixels = 0;
            RuleOps.MapRules(state.Sheets, (sheet, rule) =>
            {
                List<Declaration> declarations = new List<Declaration>();
                foreach (Declaration declaration in rule.Declarations)
                {
                    string text = RuleOps.Text(declaration);
                    if (!Regex.IsMatch(text, @"^(?:repeating-)?(?:linear|radial|conic)-gradient\(", RegexOptions.IgnoreCase))
                    {
                        declarations.Add(declaration);
                        continue;
                    }
                    if (state.Config.Emulation.StrictNative || state.Config.Emulation.Disable.Contains(Id))
                        throw new InvalidOperationException("gradient-bake is disabled or strictNative forbids baked gradients");
                    if (declaration.Name != "background" && declaration.Name != "background-image")
                        throw new InvalidOperationException("gradient backend only supports background-image: " + declaration.Name);
                    List<UxmlElement> matchedElements = new List<UxmlElement>();
                    foreach (Selector selector in rule.Selectors)
                        foreach (UxmlElement element in state.MatchDom(selector))
                            if (!matchedElements.Contains(element)) matchedElements.Add(element);
                    UxmlElement matched = matchedElements.Count == 0 ? null : matchedElements[0];
                    string widthText = EffectiveDimension(state, rule, matched, "width");
                    string heightText = EffectiveDimension(state, rule, matched, "height");
                    int width = Dimension(widthText, state.Extras.ReferenceWidth);
                    int height = Dimension(heightText, state.Extras.ReferenceHeight);
                    bool fixedDimensions = FixedPixels(widthText) && FixedPixels(heightText);
                    foreach (UxmlElement element in matchedElements)
                    {
                        string elementWidth = EffectiveDimension(state, rule, element, "width");
                        string elementHeight = EffectiveDimension(state, rule, element, "height");
                        if (Dimension(elementWidth, state.Extras.ReferenceWidth) != width || Dimension(elementHeight, state.Extras.ReferenceHeight) != height
                            || (FixedPixels(elementWidth) && FixedPixels(elementHeight)) != fixedDimensions)
                            throw new InvalidOperationException("unsupported heterogeneous effective gradient geometry: all elements matched by one baked rule must have the same dimensions; split the selector into homogeneous rules");
                    }
                    totalPixels += (long)width * height;
                    if (totalPixels > 32L * 1024 * 1024) throw new InvalidOperationException("gradient bake exceeds aggregate 32 megapixel budget");
                    if (text.IndexOf("var(", StringComparison.OrdinalIgnoreCase) >= 0 && matched != null)
                        text = state.Prop(matched, "background-image") ?? text;
                    GradientSpec spec = CssGradient.Parse(text, width, height);
                    bool geometrySensitive = spec.Kind != GradientKind.Linear || Math.Abs(spec.AngleDegrees % 90) > 0.000001 || spec.Repeating || CssGradient.HasAbsoluteStops(text);
                    if (geometrySensitive && (!fixedDimensions || MediaChangesGeometry(state, rule)))
                        throw new InvalidOperationException("unsupported dynamic gradient aspect/geometry: angled, radial, conic and repeating gradients and absolute stops require fixed pixel dimensions without media size overrides");
                    byte[] png = PngEncoder.Encode(GradientBaker.Bake(spec, width, height));
                    string path = PathUtil.Combine(state.GeneratedFolder, "gradient-" + Hash(text + "|" + width + "x" + height) + ".png");
                    state.AssetSink.Add(new GeneratedAsset { RelativePath = path, Content = png });
                    declarations.Add(RuleOps.Decl("background-image", "url(\"" + UssText.GeneratedMarker + path + "\")", declaration.Span));
                    declarations.Add(RuleOps.Decl("-unity-background-scale-mode", "stretch-to-fill", declaration.Span));
                    if (widthText == null || heightText == null)
                        state.Report("C011", Severity.Warning, Tier.ConvertTime, declaration.Span, text,
                            "gradient baked at reference dimensions " + width + "x" + height + "; resize geometry requires native visual review", "provide explicit pixel dimensions for exact geometry", Id);
                }
                return new[] { RuleOps.WithDeclarations(rule, declarations) };
            });
        }

        private static string EffectiveDimension(TransformState state, StyleRule rule, UxmlElement element, string property)
        {
            string computed = element == null ? null : state.Prop(element, property);
            if (computed != null) return computed;
            Declaration authored = RuleOps.Find(rule, property);
            return authored == null ? null : RuleOps.Text(authored);
        }

        private static bool FixedPixels(string text)
        {
            return text != null && CssText.TryNumber(text, out double value, out string unit) && value > 0 && (unit == "px" || unit == "");
        }

        private static bool MediaChangesGeometry(TransformState state, StyleRule original)
        {
            foreach (Stylesheet sheet in state.Page.Cascade)
                foreach (StyleRule rule in sheet.Rules)
                {
                    if (rule.AtContext.Count == 0) continue;
                    bool geometry = false;
                    foreach (Declaration declaration in rule.Declarations)
                        if (declaration.Name == "width" || declaration.Name == "height" || declaration.Name == "min-width" || declaration.Name == "max-width" || declaration.Name == "min-height" || declaration.Name == "max-height" || declaration.Name == "aspect-ratio") geometry = true;
                    if (!geometry) continue;
                    foreach (Selector candidate in rule.Selectors)
                        foreach (Selector selector in original.Selectors)
                            foreach (UxmlElement element in state.MatchDom(selector))
                                if (state.SelectorMatches(candidate, element)) return true;
                }
            return false;
        }

        private static string Hash(string text)
        {
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }

        private static int Dimension(string text, int reference)
        {
            double size = reference;
            if (!string.IsNullOrWhiteSpace(text) && text != "auto")
            {
                double value;
                string unit;
                if (!CssText.TryNumber(text, out value, out unit)) throw new InvalidOperationException("unresolved gradient dimension: " + text);
                if (unit == "%") size = reference * value / 100.0;
                else if (unit == "px" || unit == "") size = value;
                else throw new InvalidOperationException("unsupported gradient dimension: " + text);
            }
            if (double.IsNaN(size) || double.IsInfinity(size) || size <= 0 || size > 4096) throw new InvalidOperationException("gradient dimension outside 1..4096 budget");
            int result = (int)Math.Ceiling(size);
            if ((long)result * reference > 16L * 1024 * 1024) throw new InvalidOperationException("gradient geometry exceeds budget");
            return result;
        }
    }

    internal static class CssGradient
    {
        public static bool HasAbsoluteStops(string text)
        {
            int opening = text.IndexOf('(');
            List<string> parts = CssText.SplitTop(text.Substring(opening + 1, text.Length - opening - 2), ',');
            foreach (string part in parts)
            {
                List<string> tokens = CssText.SplitWs(part.Trim());
                if (tokens.Count == 0 || !ColorParser.TryParse(CssValueParser.Parse(tokens[0]), out CssColor color)) continue;
                for (int i = 1; i < tokens.Count; i++)
                    if (CssText.TryNumber(tokens[i], out double number, out string unit) && unit != "%" && (unit != "" || number != 0)) return true;
            }
            return false;
        }

        public static GradientSpec Parse(string text, int width, int height)
        {
            int opening = text.IndexOf('(');
            if (opening < 0 || !text.EndsWith(")", StringComparison.Ordinal)) throw new InvalidOperationException("invalid CSS gradient: " + text);
            string function = text.Substring(0, opening).ToLowerInvariant();
            GradientSpec spec = new GradientSpec { Repeating = function.StartsWith("repeating-", StringComparison.Ordinal) };
            if (function.EndsWith("linear-gradient", StringComparison.Ordinal)) { spec.Kind = GradientKind.Linear; spec.AngleDegrees = 180; }
            else if (function.EndsWith("radial-gradient", StringComparison.Ordinal)) spec.Kind = GradientKind.Radial;
            else if (function.EndsWith("conic-gradient", StringComparison.Ordinal)) spec.Kind = GradientKind.Conic;
            else throw new InvalidOperationException("unsupported gradient function: " + function);
            List<string> parts = CssText.SplitTop(text.Substring(opening + 1, text.Length - opening - 2), ',');
            if (parts.Count < 2) throw new InvalidOperationException("gradient requires two color stops");
            int firstStop = 0;
            if (!TryStop(parts[0], spec, width, height, out List<UnresolvedStop> ignored))
            {
                ParseGeometry(parts[0].Trim().ToLowerInvariant(), spec, width, height);
                firstStop = 1;
            }
            List<UnresolvedStop> stops = new List<UnresolvedStop>();
            for (int i = firstStop; i < parts.Count; i++)
            {
                if (!TryStop(parts[i], spec, width, height, out List<UnresolvedStop> parsed))
                    throw new InvalidOperationException("unsupported gradient stop/color/interpolation: " + parts[i]);
                stops.AddRange(parsed);
            }
            spec.Stops = GradientBaker.ResolveStops(stops);
            return spec;
        }

        private static bool TryStop(string text, GradientSpec spec, int width, int height, out List<UnresolvedStop> stops)
        {
            stops = new List<UnresolvedStop>();
            List<string> tokens = CssText.SplitWs(text.Trim());
            if (tokens.Count == 0 || tokens.Count > 3) return false;
            if (!ColorParser.TryParse(CssValueParser.Parse(tokens[0]), out CssColor color)) return false;
            Rgba8 rgba = new Rgba8((byte)color.R, (byte)color.G, (byte)color.B, (byte)Math.Round(color.A * 255, MidpointRounding.AwayFromZero));
            if (tokens.Count == 1) stops.Add(new UnresolvedStop(null, rgba));
            else
                for (int i = 1; i < tokens.Count; i++) stops.Add(new UnresolvedStop(StopPosition(tokens[i], spec, width, height), rgba));
            return true;
        }

        private static double StopPosition(string text, GradientSpec spec, int width, int height)
        {
            if (!CssText.TryNumber(text, out double value, out string unit) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("invalid gradient stop position: " + text);
            if (unit == "%") return value / 100;
            if (spec.Kind == GradientKind.Conic) return Angle(text) / 360;
            if (unit == "px" || unit == "")
            {
                if (spec.Kind == GradientKind.Radial) throw new InvalidOperationException("radial pixel stops require a verified radial ray-length backend; use percentages");
                return value / GradientBaker.LinearLineLength(spec.AngleDegrees, width, height);
            }
            throw new InvalidOperationException("unsupported gradient stop unit: " + text);
        }

        private static double Angle(string text)
        {
            if (!CssText.TryNumber(text, out double value, out string unit)) throw new InvalidOperationException("invalid gradient angle: " + text);
            switch (unit)
            {
                case "deg": return value;
                case "turn": return value * 360;
                case "rad": return value * 180 / Math.PI;
                case "grad": return value * 0.9;
                case "": if (value == 0) return 0; break;
            }
            throw new InvalidOperationException("unsupported gradient angle: " + text);
        }

        private static void ParseGeometry(string text, GradientSpec spec, int width, int height)
        {
            if (spec.Kind == GradientKind.Linear)
            {
                if (!text.StartsWith("to ", StringComparison.Ordinal)) { spec.AngleDegrees = Angle(text); return; }
                string horizontal = null, vertical = null;
                foreach (string token in text.Substring(3).Split(' '))
                    if (token == "left" || token == "right") horizontal = token;
                    else if (token == "top" || token == "bottom") vertical = token;
                    else throw new InvalidOperationException("invalid gradient direction: " + text);
                spec.AngleDegrees = GradientBaker.CornerAngle(horizontal, vertical, width, height);
                return;
            }
            string geometry = text;
            int at = text.IndexOf("at ", StringComparison.Ordinal);
            if (at >= 0)
            {
                geometry = text.Substring(0, at).Trim();
                List<string> positions = CssText.SplitWs(text.Substring(at + 3));
                if (positions.Count != 2) throw new InvalidOperationException("gradient position requires two percentage/keyword coordinates");
                spec.CenterX = Position(positions[0], true);
                spec.CenterY = Position(positions[1], false);
            }
            if (spec.Kind == GradientKind.Conic)
            {
                if (geometry.Length == 0) return;
                if (!geometry.StartsWith("from ", StringComparison.Ordinal)) throw new InvalidOperationException("unsupported conic geometry: " + geometry);
                spec.AngleDegrees = Angle(geometry.Substring(5));
                return;
            }
            foreach (string token in CssText.SplitWs(geometry))
                switch (token)
                {
                    case "circle": spec.Shape = RadialShape.Circle; break;
                    case "ellipse": spec.Shape = RadialShape.Ellipse; break;
                    case "closest-side": spec.Extent = RadialExtent.ClosestSide; break;
                    case "closest-corner": spec.Extent = RadialExtent.ClosestCorner; break;
                    case "farthest-side": spec.Extent = RadialExtent.FarthestSide; break;
                    case "farthest-corner": spec.Extent = RadialExtent.FarthestCorner; break;
                    default: throw new InvalidOperationException("unsupported radial geometry: " + geometry);
                }
        }

        private static double Position(string text, bool horizontal)
        {
            if (text == "center") return 0.5;
            if (text == (horizontal ? "left" : "top")) return 0;
            if (text == (horizontal ? "right" : "bottom")) return 1;
            if (CssText.TryNumber(text, out double value, out string unit) && unit == "%") return value / 100;
            throw new InvalidOperationException("unsupported gradient position: " + text);
        }
    }
}
