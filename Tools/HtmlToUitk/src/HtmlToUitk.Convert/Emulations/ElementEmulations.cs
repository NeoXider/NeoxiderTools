using System;
using System.Collections.Generic;
using System.Globalization;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Convert.Dialect;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Style;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert.Emulations
{
    public sealed class OverflowScrollViewEmulation : EmulationBase
    {
        public override string Id => "overflow-scrollview";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "overflow:auto", "overflow:scroll", "overflow:overlay" };

        public override int Order => 90;

        private static bool IsScroll(ResolvedValue value)
        {
            if (value == null || value.Value == null) return false;
            string text = value.Value.ToText().Trim().ToLowerInvariant();
            return text == "auto" || text == "scroll" || text == "overlay";
        }

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            var elements = new List<UxmlElement>(state.Elements());
            foreach (UxmlElement element in elements)
            {
                if (element.UxmlType == "ScrollView") continue;
                ResolvedValue xv = state.PropSource(element, "overflow-x");
                ResolvedValue yv = state.PropSource(element, "overflow-y");
                bool x = IsScroll(xv);
                bool y = IsScroll(yv);
                if (!x && !y) continue;
                bool xExplicit = x && xv.Source != null && xv.Source.Name != "overflow";
                bool yExplicit = y && yv.Source != null && yv.Source.Name != "overflow";
                string mode = null;
                if (xExplicit && (yExplicit || y)) mode = "VerticalAndHorizontal";
                else if (xExplicit) mode = "Horizontal";
                var replacement = new UxmlElement
                {
                    UxmlType = "ScrollView",
                    Source = element.Source,
                    LocKey = element.LocKey,
                    MockText = element.MockText,
                    GeneratedBy = element.GeneratedBy
                };
                IReadOnlyList<UxmlAttribute> attributes = element.Attributes;
                if (mode != null) attributes = AttrOrder.With(attributes, "mode", mode);
                replacement.Attributes = attributes;
                replacement.Children = new List<UxmlElement>(element.Children);
                foreach (UxmlElement child in replacement.Children) child.Parent = replacement;
                state.Editor.ReplaceElement(element, replacement);
                if (state.Handled.Remove(element)) state.Handled.Add(replacement);
            }
        }
    }

    public sealed class FixedToRootEmulation : EmulationBase
    {
        public override string Id => "fixed-to-root";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "position:fixed" };

        public override int Order => 100;

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            RuleOps.MapRules(state.Sheets, (sheetId, rule) =>
            {
                bool changed = false;
                var declarations = new List<Declaration>(rule.Declarations.Count);
                foreach (Declaration d in rule.Declarations)
                {
                    if (d.Name == "position" && RuleOps.Text(d).ToLowerInvariant() == "fixed")
                    {
                        changed = true;
                        declarations.Add(new Declaration { Name = "position", Value = new CssRaw("absolute"), Important = d.Important, Span = d.Span });
                    }
                    else declarations.Add(d);
                }

                return changed ? new[] { RuleOps.WithDeclarations(rule, declarations) } : null;
            });

            if (state.Document.Roots.Count == 0) return;
            UxmlElement root = state.Document.Roots[0];
            var moved = new List<UxmlElement>();
            foreach (UxmlElement element in new List<UxmlElement>(state.Elements()))
            {
                if (ReferenceEquals(element, root) || element.Parent == null) continue;
                string position = (state.Prop(element, "position") ?? "").ToLowerInvariant();
                if (position == "fixed") moved.Add(element);
            }

            foreach (UxmlElement element in moved)
            {
                if (ReferenceEquals(element.Parent, root) && ReferenceEquals(root.Children[root.Children.Count - 1], element)) continue;
                state.Editor.RemoveElement(element);
                state.Editor.AppendChild(root, element);
            }
        }
    }

    public sealed class ZOrderEmulation : EmulationBase
    {
        public override string Id => "z-order";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "prop:z-index" };

        public override int Order => 110;

        private sealed class Keyed
        {
            public UxmlElement Element;
            public int Group;
            public int Z;
            public int Index;
        }

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            var parents = new List<UxmlElement>();
            foreach (UxmlElement element in state.Elements())
            {
                if (element.Children.Count > 1) parents.Add(element);
            }

            foreach (UxmlElement parent in parents)
            {
                IReadOnlyList<UxmlElement> sorted = Sort(state, parent.Children);
                if (sorted != null)
                {
                    parent.Children = sorted;
                }
            }

            if (state.Document.Roots.Count > 1)
            {
                IReadOnlyList<UxmlElement> sortedRoots = Sort(state, state.Document.Roots);
                if (sortedRoots != null) state.Document.Roots = sortedRoots;
            }
        }

        private static IReadOnlyList<UxmlElement> Sort(TransformState state, IReadOnlyList<UxmlElement> children)
        {
            var keyed = new List<Keyed>();
            bool any = false;
            for (int i = 0; i < children.Count; i++)
            {
                string text = state.Prop(children[i], "z-index");
                int z = 0;
                int group = 1;
                if (text != null && text.ToLowerInvariant() != "auto" && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out z))
                {
                    group = z < 0 ? 0 : z > 0 ? 2 : 1;
                    any = true;
                }

                keyed.Add(new Keyed { Element = children[i], Group = group, Z = z, Index = i });
            }

            if (!any) return null;
            keyed.Sort((a, b) =>
            {
                int c = a.Group.CompareTo(b.Group);
                if (c != 0) return c;
                if (a.Group != 1)
                {
                    c = a.Z.CompareTo(b.Z);
                    if (c != 0) return c;
                }

                return a.Index.CompareTo(b.Index);
            });
            var result = new List<UxmlElement>(keyed.Count);
            foreach (Keyed k in keyed) result.Add(k.Element);
            return result;
        }
    }

    public sealed class TransformDecomposeEmulation : EmulationBase
    {
        public override string Id => "transform-decompose";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "transform:multi" };

        public override int Order => 120;

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            RuleOps.MapRules(state.Sheets, (sheetId, rule) =>
            {
                bool changed = false;
                var declarations = new List<Declaration>();
                foreach (Declaration d in rule.Declarations)
                {
                    if (d.Name != "transform")
                    {
                        declarations.Add(d);
                        continue;
                    }

                    string text = RuleOps.Text(d);
                    List<string> functions = TransformText.Functions(text);
                    if (functions == null || functions.Count < 2)
                    {
                        declarations.Add(d);
                        continue;
                    }

                    List<KeyValuePair<string, string>> parts;
                    if (!TransformText.Decompose(functions, out parts))
                    {
                        changed = true;
                        if (TransformText.IsReducibleSyntax(functions))
                        {
                            state.Report("E001", Severity.Error, Tier.Reject, d.Span, "transform: " + text, "the transform functions are not in translate, rotate, scale order", "Reorder the functions or split the transform.", "transform-decompose");
                        }

                        continue;
                    }

                    changed = true;
                    foreach (KeyValuePair<string, string> part in parts) declarations.Add(new Declaration { Name = part.Key, Value = new CssRaw(part.Value), Important = d.Important, Span = d.Span });
                }

                return changed ? new[] { RuleOps.WithDeclarations(rule, declarations) } : null;
            });
        }
    }

    internal static class TransformText
    {
        public static List<string> Functions(string text)
        {
            var list = new List<string>();
            foreach (string token in CssText.SplitWs(text))
            {
                if (token.IndexOf('(') < 0) return null;
                list.Add(token);
            }

            return list;
        }

        private static string Name(string function)
        {
            return function.Substring(0, function.IndexOf('(')).ToLowerInvariant();
        }

        private static List<string> Args(string function)
        {
            int open = function.IndexOf('(');
            string inner = function.Substring(open + 1, function.Length - open - 2);
            var args = new List<string>();
            foreach (string part in CssText.SplitTop(inner, ',')) args.Add(part.Trim());
            return args;
        }

        public static bool IsReducibleSyntax(List<string> functions)
        {
            foreach (string f in functions)
            {
                string n = Name(f);
                if (n != "translate" && n != "translatex" && n != "translatey" && n != "rotate" && n != "rotatez" && n != "scale" && n != "scalex" && n != "scaley") return false;
            }

            return true;
        }

        public static bool Decompose(List<string> functions, out List<KeyValuePair<string, string>> output)
        {
            output = new List<KeyValuePair<string, string>>();
            int stage = -1;
            string tx = null, ty = null;
            string rotate = null;
            string sx = null, sy = null;
            foreach (string f in functions)
            {
                string n = Name(f);
                List<string> args = Args(f);
                int kind;
                if (n == "translate" || n == "translatex" || n == "translatey") kind = 0;
                else if (n == "rotate" || n == "rotatez") kind = 1;
                else if (n == "scale" || n == "scalex" || n == "scaley") kind = 2;
                else return false;
                if (kind < stage) return false;
                if (kind == stage && kind != 0 && kind != 2) return false;
                stage = kind;
                if (kind == 0)
                {
                    if (n == "translate")
                    {
                        if (tx != null || ty != null || args.Count < 1 || args.Count > 2) return false;
                        tx = Length(args[0]);
                        ty = args.Count == 2 ? Length(args[1]) : "0";
                        if (tx == null || ty == null) return false;
                    }
                    else if (n == "translatex")
                    {
                        if (tx != null || args.Count != 1) return false;
                        tx = Length(args[0]);
                        if (tx == null) return false;
                    }
                    else
                    {
                        if (ty != null || args.Count != 1) return false;
                        ty = Length(args[0]);
                        if (ty == null) return false;
                    }
                }
                else if (kind == 1)
                {
                    double number;
                    string unit;
                    if (args.Count != 1 || !CssText.TryNumber(args[0], out number, out unit) || unit != "deg") return false;
                    rotate = CssText.FormatLength(number, "deg");
                }
                else
                {
                    if (n == "scale")
                    {
                        if (sx != null || sy != null || args.Count < 1 || args.Count > 2) return false;
                        sx = Scalar(args[0]);
                        sy = args.Count == 2 ? Scalar(args[1]) : sx;
                        if (sx == null || sy == null) return false;
                    }
                    else if (n == "scalex")
                    {
                        if (sx != null || args.Count != 1) return false;
                        sx = Scalar(args[0]);
                        if (sx == null) return false;
                    }
                    else
                    {
                        if (sy != null || args.Count != 1) return false;
                        sy = Scalar(args[0]);
                        if (sy == null) return false;
                    }
                }
            }

            if (tx != null || ty != null) output.Add(new KeyValuePair<string, string>("translate", (tx ?? "0") + " " + (ty ?? "0")));
            if (rotate != null) output.Add(new KeyValuePair<string, string>("rotate", rotate));
            if (sx != null || sy != null)
            {
                string a = sx ?? "1";
                string b = sy ?? "1";
                output.Add(new KeyValuePair<string, string>("scale", a == b ? a : a + " " + b));
            }

            return output.Count > 0;
        }

        private static string Length(string token)
        {
            double number;
            string unit;
            if (!CssText.TryNumber(token, out number, out unit)) return null;
            if (unit != "px" && unit != "%" && !(unit == "" && number == 0)) return null;
            return CssText.FormatLength(number, unit);
        }

        private static string Scalar(string token)
        {
            double number;
            string unit;
            if (!CssText.TryNumber(token, out number, out unit) || unit != "") return null;
            return CssText.FormatNumber(number);
        }
    }
}
