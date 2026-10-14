using System;
using System.Collections.Generic;

namespace HtmlToUitk.Core.Css
{
    public sealed class ShorthandOptions
    {
        public bool ResetOmitted;
        public static readonly ShorthandOptions Default = new ShorthandOptions();
        public static readonly ShorthandOptions Cascade = new ShorthandOptions { ResetOmitted = true };
    }

    public static class ShorthandExpander
    {
        private static readonly string[] Sides = { "top", "right", "bottom", "left" };
        private static readonly string[] Corners = { "top-left", "top-right", "bottom-right", "bottom-left" };
        private static readonly HashSet<string> WideKeywords = new HashSet<string>(StringComparer.Ordinal) { "inherit", "initial", "unset", "revert" };
        private static readonly HashSet<string> BorderStyles = new HashSet<string>(StringComparer.Ordinal) { "none", "hidden", "dotted", "dashed", "solid", "double", "groove", "ridge", "inset", "outset" };
        private static readonly HashSet<string> BorderWidths = new HashSet<string>(StringComparer.Ordinal) { "thin", "medium", "thick" };
        private static readonly HashSet<string> TimingKeywords = new HashSet<string>(StringComparer.Ordinal) { "ease", "ease-in", "ease-out", "ease-in-out", "linear", "step-start", "step-end" };
        private static readonly HashSet<string> Directions = new HashSet<string>(StringComparer.Ordinal) { "normal", "reverse", "alternate", "alternate-reverse" };
        private static readonly HashSet<string> FillModes = new HashSet<string>(StringComparer.Ordinal) { "none", "forwards", "backwards", "both" };
        private static readonly HashSet<string> PlayStates = new HashSet<string>(StringComparer.Ordinal) { "running", "paused" };
        private static readonly HashSet<string> FlexDirections = new HashSet<string>(StringComparer.Ordinal) { "row", "row-reverse", "column", "column-reverse" };
        private static readonly HashSet<string> FlexWraps = new HashSet<string>(StringComparer.Ordinal) { "nowrap", "wrap", "wrap-reverse" };
        private static readonly HashSet<string> RepeatKeywords = new HashSet<string>(StringComparer.Ordinal) { "repeat", "repeat-x", "repeat-y", "no-repeat", "space", "round" };
        private static readonly HashSet<string> Attachments = new HashSet<string>(StringComparer.Ordinal) { "scroll", "fixed", "local" };
        private static readonly HashSet<string> Boxes = new HashSet<string>(StringComparer.Ordinal) { "border-box", "padding-box", "content-box" };
        private static readonly HashSet<string> PositionWords = new HashSet<string>(StringComparer.Ordinal) { "left", "right", "top", "bottom", "center" };
        private static readonly HashSet<string> FontStyles = new HashSet<string>(StringComparer.Ordinal) { "italic", "oblique" };
        private static readonly HashSet<string> FontWeights = new HashSet<string>(StringComparer.Ordinal) { "bold", "bolder", "lighter" };
        private static readonly HashSet<string> FontStretches = new HashSet<string>(StringComparer.Ordinal) { "ultra-condensed", "extra-condensed", "condensed", "semi-condensed", "semi-expanded", "expanded", "extra-expanded", "ultra-expanded" };
        private static readonly HashSet<string> FontSizeWords = new HashSet<string>(StringComparer.Ordinal) { "xx-small", "x-small", "small", "medium", "large", "x-large", "xx-large", "xxx-large", "larger", "smaller" };
        private static readonly HashSet<string> SystemFonts = new HashSet<string>(StringComparer.Ordinal) { "caption", "icon", "menu", "message-box", "small-caption", "status-bar" };
        private static readonly HashSet<string> ColorWords = new HashSet<string>(StringComparer.Ordinal) { "currentcolor", "transparent" };
        private static readonly HashSet<string> ColorFunctions = new HashSet<string>(StringComparer.Ordinal) { "rgb", "rgba", "hsl", "hsla", "hwb", "lab", "lch", "oklab", "oklch", "color", "color-mix" };

        private static readonly HashSet<string> Names = new HashSet<string>(StringComparer.Ordinal)
        {
            "margin", "padding", "inset", "margin-inline", "margin-block", "padding-inline", "padding-block", "inset-inline", "inset-block",
            "border-width", "border-color", "border-style", "border-radius", "border", "border-top", "border-right", "border-bottom", "border-left",
            "flex", "flex-flow", "overflow", "gap", "place-items", "place-content", "place-self", "background", "font", "transition", "animation"
        };

        public static bool IsShorthand(string name) => name != null && Names.Contains(name);

        public static IReadOnlyCollection<string> ShorthandNames => Names;

        public static IReadOnlyList<Declaration> Expand(Declaration declaration, ShorthandOptions options = null)
        {
            IReadOnlyList<Declaration> longhands;
            string problem;
            if (TryExpand(declaration, options, out longhands, out problem)) return longhands;
            return new[] { declaration };
        }

        public static bool TryExpand(Declaration declaration, ShorthandOptions options, out IReadOnlyList<Declaration> longhands, out string problem)
        {
            longhands = null;
            problem = null;
            options = options ?? ShorthandOptions.Default;
            if (declaration == null || declaration.Value == null || !IsShorthand(declaration.Name))
            {
                problem = "not a shorthand";
                return false;
            }
            var output = new List<KeyValuePair<string, CssValue>>();
            var wide = declaration.Value as CssIdent;
            string name = declaration.Name;
            if (wide != null && WideKeywords.Contains(wide.Name.ToLowerInvariant()))
            {
                foreach (string longhand in LonghandNames(name)) output.Add(new KeyValuePair<string, CssValue>(longhand, wide));
            }
            else if (!Dispatch(name, declaration.Value, options, output, out problem)) return false;
            var result = new List<Declaration>(output.Count);
            foreach (KeyValuePair<string, CssValue> pair in output)
            {
                pair.Value.Span = declaration.Value.Span;
                result.Add(new Declaration { Name = pair.Key, Value = pair.Value, Important = declaration.Important, Span = declaration.Span });
            }
            longhands = result;
            return true;
        }

        public static IReadOnlyList<string> LonghandNames(string name)
        {
            switch (name)
            {
                case "margin": return PerSide("margin-", "");
                case "padding": return PerSide("padding-", "");
                case "inset": return Sides;
                case "margin-inline": return new[] { "margin-left", "margin-right" };
                case "margin-block": return new[] { "margin-top", "margin-bottom" };
                case "padding-inline": return new[] { "padding-left", "padding-right" };
                case "padding-block": return new[] { "padding-top", "padding-bottom" };
                case "inset-inline": return new[] { "left", "right" };
                case "inset-block": return new[] { "top", "bottom" };
                case "border-width": return PerSide("border-", "-width");
                case "border-color": return PerSide("border-", "-color");
                case "border-style": return PerSide("border-", "-style");
                case "border-radius": return PerCorner();
                case "border": return AllBorder();
                case "border-top": return new[] { "border-top-width", "border-top-style", "border-top-color" };
                case "border-right": return new[] { "border-right-width", "border-right-style", "border-right-color" };
                case "border-bottom": return new[] { "border-bottom-width", "border-bottom-style", "border-bottom-color" };
                case "border-left": return new[] { "border-left-width", "border-left-style", "border-left-color" };
                case "flex": return new[] { "flex-grow", "flex-shrink", "flex-basis" };
                case "flex-flow": return new[] { "flex-direction", "flex-wrap" };
                case "overflow": return new[] { "overflow-x", "overflow-y" };
                case "gap": return new[] { "row-gap", "column-gap" };
                case "place-items": return new[] { "align-items", "justify-items" };
                case "place-content": return new[] { "align-content", "justify-content" };
                case "place-self": return new[] { "align-self", "justify-self" };
                case "background": return new[] { "background-color", "background-image", "background-position", "background-size", "background-repeat", "background-attachment", "background-origin", "background-clip" };
                case "font": return new[] { "font-style", "font-variant", "font-weight", "font-stretch", "font-size", "line-height", "font-family" };
                case "transition": return new[] { "transition-property", "transition-duration", "transition-timing-function", "transition-delay" };
                case "animation": return new[] { "animation-name", "animation-duration", "animation-timing-function", "animation-delay", "animation-iteration-count", "animation-direction", "animation-fill-mode", "animation-play-state" };
                default: return Array.Empty<string>();
            }
        }

        private static string[] PerSide(string prefix, string suffix)
        {
            var names = new string[4];
            for (int i = 0; i < 4; i++) names[i] = prefix + Sides[i] + suffix;
            return names;
        }

        private static string[] PerCorner()
        {
            var names = new string[4];
            for (int i = 0; i < 4; i++) names[i] = "border-" + Corners[i] + "-radius";
            return names;
        }

        private static string[] AllBorder()
        {
            var names = new List<string>();
            foreach (string side in Sides)
            {
                names.Add("border-" + side + "-width");
                names.Add("border-" + side + "-style");
                names.Add("border-" + side + "-color");
            }
            return names.ToArray();
        }

        private static bool Dispatch(string name, CssValue value, ShorthandOptions options, List<KeyValuePair<string, CssValue>> output, out string problem)
        {
            problem = null;
            List<CssValue> tokens = Tokens(value);
            switch (name)
            {
                case "margin": return Box(tokens, PerSide("margin-", ""), output, out problem);
                case "padding": return Box(tokens, PerSide("padding-", ""), output, out problem);
                case "inset": return Box(tokens, Sides, output, out problem);
                case "border-width": return Box(tokens, PerSide("border-", "-width"), output, out problem);
                case "border-color": return Box(tokens, PerSide("border-", "-color"), output, out problem);
                case "border-style": return Box(tokens, PerSide("border-", "-style"), output, out problem);
                case "margin-inline": return Pair(tokens, "margin-left", "margin-right", output, out problem);
                case "margin-block": return Pair(tokens, "margin-top", "margin-bottom", output, out problem);
                case "padding-inline": return Pair(tokens, "padding-left", "padding-right", output, out problem);
                case "padding-block": return Pair(tokens, "padding-top", "padding-bottom", output, out problem);
                case "inset-inline": return Pair(tokens, "left", "right", output, out problem);
                case "inset-block": return Pair(tokens, "top", "bottom", output, out problem);
                case "overflow": return Pair(tokens, "overflow-x", "overflow-y", output, out problem);
                case "gap": return Pair(tokens, "row-gap", "column-gap", output, out problem);
                case "place-items": return Pair(tokens, "align-items", "justify-items", output, out problem);
                case "place-content": return Pair(tokens, "align-content", "justify-content", output, out problem);
                case "place-self": return Pair(tokens, "align-self", "justify-self", output, out problem);
                case "border-radius": return Radius(tokens, output, out problem);
                case "border": return Border(tokens, new[] { "top", "right", "bottom", "left" }, options, output, out problem);
                case "border-top": return Border(tokens, new[] { "top" }, options, output, out problem);
                case "border-right": return Border(tokens, new[] { "right" }, options, output, out problem);
                case "border-bottom": return Border(tokens, new[] { "bottom" }, options, output, out problem);
                case "border-left": return Border(tokens, new[] { "left" }, options, output, out problem);
                case "flex": return Flex(tokens, output, out problem);
                case "flex-flow": return FlexFlow(tokens, options, output, out problem);
                case "background": return Background(value, options, output, out problem);
                case "font": return Font(value, options, output, out problem);
                case "transition": return Transition(value, output, out problem);
                case "animation": return Animation(value, output, out problem);
                default:
                    problem = "unknown shorthand";
                    return false;
            }
        }

        public static List<CssValue> Tokens(CssValue value)
        {
            var list = value as CssList;
            if (list != null && list.Separator == ' ') return new List<CssValue>(list.Items);
            return new List<CssValue> { value };
        }

        private static List<List<CssValue>> Layers(CssValue value)
        {
            var layers = new List<List<CssValue>>();
            var list = value as CssList;
            if (list != null && list.Separator == ',')
            {
                foreach (CssValue item in list.Items) layers.Add(Tokens(item));
            }
            else layers.Add(Tokens(value));
            return layers;
        }

        private static CssValue Join(List<CssValue> items)
        {
            if (items.Count == 1) return items[0];
            return new CssList(items, ',');
        }

        private static string Ident(CssValue value)
        {
            var ident = value as CssIdent;
            return ident == null ? null : ident.Name.ToLowerInvariant();
        }

        private static bool IsKeyword(CssValue value, HashSet<string> set)
        {
            string ident = Ident(value);
            return ident != null && set.Contains(ident);
        }

        private static bool IsTime(CssValue value)
        {
            var dimension = value as CssDimension;
            return dimension != null && (dimension.Unit == "s" || dimension.Unit == "ms");
        }

        private static bool IsLengthLike(CssValue value)
        {
            if (value is CssDimension || value is CssPercent) return true;
            var number = value as CssNumber;
            if (number != null) return number.Value == 0;
            var function = value as CssFunction;
            if (function != null)
            {
                string fn = function.Name.ToLowerInvariant();
                return fn == "calc" || fn == "min" || fn == "max" || fn == "clamp";
            }
            return false;
        }

        private static bool IsColorLike(CssValue value)
        {
            if (value is CssColor) return true;
            string ident = Ident(value);
            if (ident != null) return ColorWords.Contains(ident) || ColorParser.IsNamedColor(ident);
            var function = value as CssFunction;
            return function != null && ColorFunctions.Contains(function.Name.ToLowerInvariant());
        }

        private static bool HasVar(List<CssValue> tokens)
        {
            foreach (CssValue token in tokens)
            {
                if (CssValueParser.HasVarRef(token)) return true;
            }
            return false;
        }

        private static bool Box(List<CssValue> tokens, string[] names, List<KeyValuePair<string, CssValue>> output, out string problem)
        {
            problem = null;
            if (tokens.Count < 1 || tokens.Count > 4)
            {
                problem = "expected 1 to 4 values";
                return false;
            }
            CssValue top = tokens[0];
            CssValue right = tokens.Count > 1 ? tokens[1] : top;
            CssValue bottom = tokens.Count > 2 ? tokens[2] : top;
            CssValue left = tokens.Count > 3 ? tokens[3] : right;
            output.Add(new KeyValuePair<string, CssValue>(names[0], top));
            output.Add(new KeyValuePair<string, CssValue>(names[1], right));
            output.Add(new KeyValuePair<string, CssValue>(names[2], bottom));
            output.Add(new KeyValuePair<string, CssValue>(names[3], left));
            return true;
        }

        private static bool Pair(List<CssValue> tokens, string first, string second, List<KeyValuePair<string, CssValue>> output, out string problem)
        {
            problem = null;
            if (tokens.Count < 1 || tokens.Count > 2)
            {
                problem = "expected 1 or 2 values";
                return false;
            }
            output.Add(new KeyValuePair<string, CssValue>(first, tokens[0]));
            output.Add(new KeyValuePair<string, CssValue>(second, tokens.Count > 1 ? tokens[1] : tokens[0]));
            return true;
        }

        private static bool Radius(List<CssValue> tokens, List<KeyValuePair<string, CssValue>> output, out string problem)
        {
            problem = null;
            foreach (CssValue token in tokens)
            {
                var op = token as CssIdent;
                if (op != null && op.Name == "/")
                {
                    problem = "elliptical radii are not expandable";
                    return false;
                }
            }
            if (tokens.Count < 1 || tokens.Count > 4)
            {
                problem = "expected 1 to 4 values";
                return false;
            }
            CssValue tl = tokens[0];
            CssValue tr = tokens.Count > 1 ? tokens[1] : tl;
            CssValue br = tokens.Count > 2 ? tokens[2] : tl;
            CssValue bl = tokens.Count > 3 ? tokens[3] : tr;
            string[] names = PerCorner();
            output.Add(new KeyValuePair<string, CssValue>(names[0], tl));
            output.Add(new KeyValuePair<string, CssValue>(names[1], tr));
            output.Add(new KeyValuePair<string, CssValue>(names[2], br));
            output.Add(new KeyValuePair<string, CssValue>(names[3], bl));
            return true;
        }

        private static bool Border(List<CssValue> tokens, string[] sides, ShorthandOptions options, List<KeyValuePair<string, CssValue>> output, out string problem)
        {
            problem = null;
            if (tokens.Count < 1 || tokens.Count > 3 || HasVar(tokens))
            {
                problem = tokens.Count > 3 ? "expected at most 3 components" : "components cannot be classified";
                return false;
            }
            CssValue width = null, style = null, color = null;
            foreach (CssValue token in tokens)
            {
                if (style == null && IsKeyword(token, BorderStyles)) style = token;
                else if (width == null && (IsLengthLike(token) || IsKeyword(token, BorderWidths))) width = token;
                else if (color == null && IsColorLike(token)) color = token;
                else
                {
                    problem = "unrecognised border component '" + token.ToText() + "'";
                    return false;
                }
            }
            foreach (string side in sides)
            {
                Emit(output, "border-" + side + "-width", width, options.ResetOmitted);
                Emit(output, "border-" + side + "-style", style, options.ResetOmitted);
                Emit(output, "border-" + side + "-color", color, options.ResetOmitted);
            }
            return true;
        }

        private static void Emit(List<KeyValuePair<string, CssValue>> output, string name, CssValue value, bool resetOmitted)
        {
            if (value != null) output.Add(new KeyValuePair<string, CssValue>(name, value));
            else if (resetOmitted) output.Add(new KeyValuePair<string, CssValue>(name, new CssIdent("initial")));
        }

        private static bool Flex(List<CssValue> tokens, List<KeyValuePair<string, CssValue>> output, out string problem)
        {
            problem = null;
            CssValue grow, shrink, basis;
            if (tokens.Count == 1)
            {
                string ident = Ident(tokens[0]);
                if (ident == "none")
                {
                    grow = new CssNumber(0);
                    shrink = new CssNumber(0);
                    basis = new CssIdent("auto");
                }
                else if (ident == "auto")
                {
                    grow = new CssNumber(1);
                    shrink = new CssNumber(1);
                    basis = new CssIdent("auto");
                }
                else if (tokens[0] is CssNumber)
                {
                    grow = tokens[0];
                    shrink = new CssNumber(1);
                    basis = new CssPercent(0);
                }
                else if (IsLengthLike(tokens[0]) || ident == "content" || tokens[0] is CssVarRef)
                {
                    grow = new CssNumber(1);
                    shrink = new CssNumber(1);
                    basis = tokens[0];
                }
                else
                {
                    problem = "unrecognised flex value";
                    return false;
                }
            }
            else if (tokens.Count == 2)
            {
                if (!(tokens[0] is CssNumber) || !(tokens[1] is CssNumber || IsLengthLike(tokens[1]) || Ident(tokens[1]) == "auto" || tokens[1] is CssVarRef))
                {
                    problem = "unrecognised flex value";
                    return false;
                }
                grow = tokens[0];
                if (tokens[1] is CssNumber)
                {
                    shrink = tokens[1];
                    basis = new CssPercent(0);
                }
                else
                {
                    shrink = new CssNumber(1);
                    basis = tokens[1];
                }
            }
            else if (tokens.Count == 3)
            {
                if (!(tokens[0] is CssNumber) || !(tokens[1] is CssNumber))
                {
                    problem = "unrecognised flex value";
                    return false;
                }
                grow = tokens[0];
                shrink = tokens[1];
                basis = tokens[2];
            }
            else
            {
                problem = "expected 1 to 3 values";
                return false;
            }
            output.Add(new KeyValuePair<string, CssValue>("flex-grow", grow));
            output.Add(new KeyValuePair<string, CssValue>("flex-shrink", shrink));
            output.Add(new KeyValuePair<string, CssValue>("flex-basis", basis));
            return true;
        }

        private static bool FlexFlow(List<CssValue> tokens, ShorthandOptions options, List<KeyValuePair<string, CssValue>> output, out string problem)
        {
            problem = null;
            if (tokens.Count < 1 || tokens.Count > 2)
            {
                problem = "expected 1 or 2 values";
                return false;
            }
            CssValue direction = null, wrap = null;
            foreach (CssValue token in tokens)
            {
                if (direction == null && IsKeyword(token, FlexDirections)) direction = token;
                else if (wrap == null && IsKeyword(token, FlexWraps)) wrap = token;
                else
                {
                    problem = "unrecognised flex-flow component '" + token.ToText() + "'";
                    return false;
                }
            }
            Emit(output, "flex-direction", direction, options.ResetOmitted);
            Emit(output, "flex-wrap", wrap, options.ResetOmitted);
            return true;
        }

        private static bool Background(CssValue value, ShorthandOptions options, List<KeyValuePair<string, CssValue>> output, out string problem)
        {
            problem = null;
            List<List<CssValue>> layers = Layers(value);
            if (layers.Count != 1)
            {
                problem = "several background layers are not expandable";
                return false;
            }
            List<CssValue> tokens = layers[0];
            if (HasVar(tokens))
            {
                problem = "components cannot be classified";
                return false;
            }
            CssValue color = null, image = null, repeat = null, attachment = null, origin = null, clip = null, size = null;
            var position = new List<CssValue>();
            var repeatTokens = new List<CssValue>();
            var boxTokens = new List<CssValue>();
            bool afterSlash = false;
            var sizeTokens = new List<CssValue>();
            foreach (CssValue token in tokens)
            {
                var op = token as CssIdent;
                if (op != null && op.Name == "/")
                {
                    afterSlash = true;
                    continue;
                }
                if (afterSlash && (IsLengthLike(token) || Ident(token) == "auto" || Ident(token) == "cover" || Ident(token) == "contain"))
                {
                    sizeTokens.Add(token);
                    continue;
                }
                afterSlash = false;
                string ident = Ident(token);
                if (token is CssUrl || (token is CssFunction fn && fn.Name.ToLowerInvariant().EndsWith("gradient", StringComparison.Ordinal)) || ident == "none")
                {
                    if (image != null)
                    {
                        problem = "two background images";
                        return false;
                    }
                    image = token;
                }
                else if (IsKeyword(token, RepeatKeywords)) repeatTokens.Add(token);
                else if (IsKeyword(token, Attachments)) attachment = token;
                else if (IsKeyword(token, Boxes)) boxTokens.Add(token);
                else if (IsKeyword(token, PositionWords) || IsLengthLike(token)) position.Add(token);
                else if (IsColorLike(token))
                {
                    if (color != null)
                    {
                        problem = "two background colours";
                        return false;
                    }
                    color = token;
                }
                else
                {
                    problem = "unrecognised background component '" + token.ToText() + "'";
                    return false;
                }
            }
            if (repeatTokens.Count > 2 || boxTokens.Count > 2 || position.Count > 4 || sizeTokens.Count > 2)
            {
                problem = "too many components";
                return false;
            }
            if (repeatTokens.Count > 0) repeat = repeatTokens.Count == 1 ? repeatTokens[0] : new CssList(repeatTokens, ' ');
            if (sizeTokens.Count > 0) size = sizeTokens.Count == 1 ? sizeTokens[0] : new CssList(sizeTokens, ' ');
            if (boxTokens.Count > 0)
            {
                origin = boxTokens[0];
                clip = boxTokens.Count == 2 ? boxTokens[1] : boxTokens[0];
            }
            CssValue positionValue = position.Count == 0 ? null : position.Count == 1 ? position[0] : new CssList(position, ' ');
            Emit(output, "background-color", color, options.ResetOmitted);
            Emit(output, "background-image", image, options.ResetOmitted);
            Emit(output, "background-position", positionValue, options.ResetOmitted);
            Emit(output, "background-size", size, options.ResetOmitted);
            Emit(output, "background-repeat", repeat, options.ResetOmitted);
            Emit(output, "background-attachment", attachment, options.ResetOmitted);
            Emit(output, "background-origin", origin, options.ResetOmitted);
            Emit(output, "background-clip", clip, options.ResetOmitted);
            return true;
        }

        private static bool Font(CssValue value, ShorthandOptions options, List<KeyValuePair<string, CssValue>> output, out string problem)
        {
            problem = null;
            var extraFamilies = new List<CssValue>();
            List<CssValue> tokens;
            var commaList = value as CssList;
            if (commaList != null && commaList.Separator == ',')
            {
                tokens = Tokens(commaList.Items[0]);
                for (int k = 1; k < commaList.Items.Count; k++) extraFamilies.Add(commaList.Items[k]);
            }
            else tokens = Tokens(value);
            if (tokens.Count < 2 || HasVar(tokens))
            {
                problem = tokens.Count < 2 ? "expected at least a size and a family" : "components cannot be classified";
                return false;
            }
            if (tokens.Count == 1 && SystemFonts.Contains(Ident(tokens[0]) ?? ""))
            {
                problem = "system font keywords are not expandable";
                return false;
            }
            CssValue style = null, variant = null, weight = null, stretch = null;
            int i = 0;
            int normals = 0;
            while (i < tokens.Count)
            {
                CssValue token = tokens[i];
                string ident = Ident(token);
                if (ident == "normal")
                {
                    normals++;
                    i++;
                    continue;
                }
                if (style == null && ident != null && FontStyles.Contains(ident))
                {
                    style = token;
                    i++;
                    if (ident == "oblique" && i < tokens.Count && tokens[i] is CssDimension angle && angle.Unit == "deg")
                    {
                        style = new CssList(new[] { token, tokens[i] }, ' ');
                        i++;
                    }
                    continue;
                }
                if (variant == null && ident == "small-caps")
                {
                    variant = token;
                    i++;
                    continue;
                }
                if (weight == null && (token is CssNumber wn && wn.Value >= 1 && wn.Value <= 1000 || (ident != null && FontWeights.Contains(ident))))
                {
                    weight = token;
                    i++;
                    continue;
                }
                if (stretch == null && ident != null && FontStretches.Contains(ident))
                {
                    stretch = token;
                    i++;
                    continue;
                }
                break;
            }
            if (i >= tokens.Count)
            {
                problem = "font size is missing";
                return false;
            }
            CssValue sizeToken = tokens[i];
            if (!(IsLengthLike(sizeToken) || IsKeyword(sizeToken, FontSizeWords)))
            {
                problem = "invalid font size '" + sizeToken.ToText() + "'";
                return false;
            }
            i++;
            CssValue lineHeight = null;
            if (i < tokens.Count && Ident(tokens[i]) == "/")
            {
                if (i + 1 >= tokens.Count)
                {
                    problem = "line height is missing after '/'";
                    return false;
                }
                lineHeight = tokens[i + 1];
                i += 2;
            }
            if (i >= tokens.Count)
            {
                problem = "font family is missing";
                return false;
            }
            var familyTokens = new List<CssValue>();
            for (; i < tokens.Count; i++) familyTokens.Add(tokens[i]);
            CssValue family = familyTokens.Count == 1 ? familyTokens[0] : new CssList(familyTokens, ' ');
            if (extraFamilies.Count > 0)
            {
                var all = new List<CssValue> { family };
                all.AddRange(extraFamilies);
                family = new CssList(all, ',');
            }
            Emit(output, "font-style", style ?? (options.ResetOmitted ? new CssIdent("normal") : null), false);
            Emit(output, "font-variant", variant ?? (options.ResetOmitted ? new CssIdent("normal") : null), false);
            Emit(output, "font-weight", weight ?? (options.ResetOmitted ? new CssIdent("normal") : null), false);
            Emit(output, "font-stretch", stretch ?? (options.ResetOmitted ? new CssIdent("normal") : null), false);
            output.Add(new KeyValuePair<string, CssValue>("font-size", sizeToken));
            Emit(output, "line-height", lineHeight ?? (options.ResetOmitted ? new CssIdent("normal") : null), false);
            output.Add(new KeyValuePair<string, CssValue>("font-family", family));
            return true;
        }

        private static bool Transition(CssValue value, List<KeyValuePair<string, CssValue>> output, out string problem)
        {
            problem = null;
            List<List<CssValue>> layers = Layers(value);
            var properties = new List<CssValue>();
            var durations = new List<CssValue>();
            var timings = new List<CssValue>();
            var delays = new List<CssValue>();
            foreach (List<CssValue> layer in layers)
            {
                if (layer.Count == 0 || layer.Count > 4 || HasVar(layer))
                {
                    problem = layer.Count > 4 ? "expected at most 4 components per transition" : "components cannot be classified";
                    return false;
                }
                CssValue property = null, duration = null, timing = null, delay = null;
                foreach (CssValue token in layer)
                {
                    if (IsTime(token))
                    {
                        if (duration == null) duration = token;
                        else if (delay == null) delay = token;
                        else
                        {
                            problem = "too many times";
                            return false;
                        }
                    }
                    else if (timing == null && (IsKeyword(token, TimingKeywords) || token is CssFunction tf && (tf.Name.ToLowerInvariant() == "cubic-bezier" || tf.Name.ToLowerInvariant() == "steps"))) timing = token;
                    else if (property == null && token is CssIdent) property = token;
                    else
                    {
                        problem = "unrecognised transition component '" + token.ToText() + "'";
                        return false;
                    }
                }
                properties.Add(property ?? new CssIdent("all"));
                durations.Add(duration ?? new CssDimension(0, "s"));
                timings.Add(timing ?? new CssIdent("ease"));
                delays.Add(delay ?? new CssDimension(0, "s"));
            }
            output.Add(new KeyValuePair<string, CssValue>("transition-property", Join(properties)));
            output.Add(new KeyValuePair<string, CssValue>("transition-duration", Join(durations)));
            output.Add(new KeyValuePair<string, CssValue>("transition-timing-function", Join(timings)));
            output.Add(new KeyValuePair<string, CssValue>("transition-delay", Join(delays)));
            return true;
        }

        private static bool Animation(CssValue value, List<KeyValuePair<string, CssValue>> output, out string problem)
        {
            problem = null;
            List<List<CssValue>> layers = Layers(value);
            var names = new List<CssValue>();
            var durations = new List<CssValue>();
            var timings = new List<CssValue>();
            var delays = new List<CssValue>();
            var counts = new List<CssValue>();
            var directions = new List<CssValue>();
            var fills = new List<CssValue>();
            var states = new List<CssValue>();
            foreach (List<CssValue> layer in layers)
            {
                if (layer.Count == 0 || layer.Count > 8 || HasVar(layer))
                {
                    problem = layer.Count > 8 ? "expected at most 8 components per animation" : "components cannot be classified";
                    return false;
                }
                CssValue name = null, duration = null, timing = null, delay = null, count = null, direction = null, fill = null, state = null;
                foreach (CssValue token in layer)
                {
                    string ident = Ident(token);
                    if (IsTime(token))
                    {
                        if (duration == null) duration = token;
                        else if (delay == null) delay = token;
                        else
                        {
                            problem = "too many times";
                            return false;
                        }
                    }
                    else if (timing == null && (IsKeyword(token, TimingKeywords) || token is CssFunction tf && (tf.Name.ToLowerInvariant() == "cubic-bezier" || tf.Name.ToLowerInvariant() == "steps"))) timing = token;
                    else if (count == null && (token is CssNumber || ident == "infinite")) count = token;
                    else if (direction == null && ident != null && Directions.Contains(ident)) direction = token;
                    else if (fill == null && ident != null && FillModes.Contains(ident)) fill = token;
                    else if (state == null && ident != null && PlayStates.Contains(ident)) state = token;
                    else if (name == null && token is CssIdent) name = token;
                    else
                    {
                        problem = "unrecognised animation component '" + token.ToText() + "'";
                        return false;
                    }
                }
                names.Add(name ?? new CssIdent("none"));
                durations.Add(duration ?? new CssDimension(0, "s"));
                timings.Add(timing ?? new CssIdent("ease"));
                delays.Add(delay ?? new CssDimension(0, "s"));
                counts.Add(count ?? new CssNumber(1));
                directions.Add(direction ?? new CssIdent("normal"));
                fills.Add(fill ?? new CssIdent("none"));
                states.Add(state ?? new CssIdent("running"));
            }
            output.Add(new KeyValuePair<string, CssValue>("animation-name", Join(names)));
            output.Add(new KeyValuePair<string, CssValue>("animation-duration", Join(durations)));
            output.Add(new KeyValuePair<string, CssValue>("animation-timing-function", Join(timings)));
            output.Add(new KeyValuePair<string, CssValue>("animation-delay", Join(delays)));
            output.Add(new KeyValuePair<string, CssValue>("animation-iteration-count", Join(counts)));
            output.Add(new KeyValuePair<string, CssValue>("animation-direction", Join(directions)));
            output.Add(new KeyValuePair<string, CssValue>("animation-fill-mode", Join(fills)));
            output.Add(new KeyValuePair<string, CssValue>("animation-play-state", Join(states)));
            return true;
        }
    }
}
