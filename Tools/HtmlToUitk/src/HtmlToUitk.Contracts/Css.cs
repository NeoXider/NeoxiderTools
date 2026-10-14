using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HtmlToUitk.Core.Css
{
    public enum StyleOrigin { Page, Linked, Imported, ConfigListed, UssPassThrough, Skin }

    public static class CssFormat
    {
        public static string Number(double value)
        {
            string text = Math.Round(value, 4, MidpointRounding.AwayFromZero).ToString("0.####", CultureInfo.InvariantCulture);
            if (text == "-0") return "0";
            return text;
        }

        public static string Quote(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\A ");
                else sb.Append(c);
            }
            sb.Append('"');
            return sb.ToString();
        }
    }

    public abstract class CssValue
    {
        public SourceSpan Span;
        public abstract string ToText();
        public override string ToString() => ToText();
    }

    public sealed class CssNumber : CssValue
    {
        public readonly double Value;
        public CssNumber(double value) { Value = value; }
        public override string ToText() => CssFormat.Number(Value);
    }

    public sealed class CssDimension : CssValue
    {
        public readonly double Value;
        public readonly string Unit;
        public CssDimension(double value, string unit) { Value = value; Unit = unit ?? ""; }
        public override string ToText()
        {
            string number = CssFormat.Number(Value);
            if (number == "0" && Unit == "px") return "0";
            return number + Unit;
        }
    }

    public sealed class CssPercent : CssValue
    {
        public readonly double Value;
        public CssPercent(double value) { Value = value; }
        public override string ToText() => CssFormat.Number(Value) + "%";
    }

    public sealed class CssColor : CssValue
    {
        public readonly int R, G, B;
        public readonly double A;
        public readonly string Authored;

        public CssColor(int r, int g, int b, double a, string authored)
        {
            R = r; G = g; B = b; A = a; Authored = authored;
        }

        public string ToCanonicalText()
        {
            if (A >= 0.9999) return "rgb(" + R.ToString(CultureInfo.InvariantCulture) + ", " + G.ToString(CultureInfo.InvariantCulture) + ", " + B.ToString(CultureInfo.InvariantCulture) + ")";
            string alpha = Math.Round(A, 3, MidpointRounding.AwayFromZero).ToString("0.###", CultureInfo.InvariantCulture);
            return "rgba(" + R.ToString(CultureInfo.InvariantCulture) + ", " + G.ToString(CultureInfo.InvariantCulture) + ", " + B.ToString(CultureInfo.InvariantCulture) + ", " + alpha + ")";
        }

        public override string ToText() => string.IsNullOrEmpty(Authored) ? ToCanonicalText() : Authored;
    }

    public sealed class CssIdent : CssValue
    {
        public readonly string Name;
        public CssIdent(string name) { Name = name; }
        public override string ToText() => Name;
    }

    public sealed class CssString : CssValue
    {
        public readonly string Value;
        public CssString(string value) { Value = value; }
        public override string ToText() => CssFormat.Quote(Value);
    }

    public sealed class CssUrl : CssValue
    {
        public readonly string Url;
        public CssUrl(string url) { Url = url; }
        public override string ToText() => "url(" + CssFormat.Quote(Url) + ")";
    }

    public sealed class CssFunction : CssValue
    {
        public readonly string Name;
        public readonly IReadOnlyList<CssValue> Args;
        public CssFunction(string name, IReadOnlyList<CssValue> args) { Name = name; Args = args ?? Array.Empty<CssValue>(); }
        public override string ToText()
        {
            var sb = new StringBuilder();
            sb.Append(Name).Append('(');
            for (int i = 0; i < Args.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(Args[i].ToText());
            }
            sb.Append(')');
            return sb.ToString();
        }
    }

    public sealed class CssVarRef : CssValue
    {
        public readonly string Name;
        public readonly CssValue Fallback;
        public CssVarRef(string name, CssValue fallback) { Name = name; Fallback = fallback; }
        public override string ToText() => Fallback == null ? "var(" + Name + ")" : "var(" + Name + ", " + Fallback.ToText() + ")";
    }

    public sealed class CssList : CssValue
    {
        public readonly IReadOnlyList<CssValue> Items;
        public readonly char Separator;
        public CssList(IReadOnlyList<CssValue> items, char separator) { Items = items ?? Array.Empty<CssValue>(); Separator = separator; }
        public override string ToText()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Items.Count; i++)
            {
                if (i > 0) sb.Append(Separator == ',' ? ", " : " ");
                sb.Append(Items[i].ToText());
            }
            return sb.ToString();
        }
    }

    public sealed class CssRaw : CssValue
    {
        public readonly string Text;
        public CssRaw(string text) { Text = text; }
        public override string ToText() => Text;
    }

    public sealed class Declaration
    {
        public string Name;
        public CssValue Value;
        public bool Important;
        public SourceSpan Span;
    }

    public sealed class Specificity : IComparable<Specificity>, IEquatable<Specificity>
    {
        public static readonly Specificity Zero = new Specificity(0, 0, 0);

        public readonly int Ids, Classes, Types;

        public Specificity(int ids, int classes, int types) { Ids = ids; Classes = classes; Types = types; }

        public Specificity Add(Specificity other) => new Specificity(Ids + other.Ids, Classes + other.Classes, Types + other.Types);

        public int CompareTo(Specificity other)
        {
            if (other == null) return 1;
            int c = Ids.CompareTo(other.Ids);
            if (c != 0) return c;
            c = Classes.CompareTo(other.Classes);
            if (c != 0) return c;
            return Types.CompareTo(other.Types);
        }

        public bool Equals(Specificity other) => other != null && Ids == other.Ids && Classes == other.Classes && Types == other.Types;
        public override bool Equals(object obj) => Equals(obj as Specificity);
        public override int GetHashCode() => (Ids * 10007) ^ (Classes * 101) ^ Types;
        public override string ToString() => "(" + Ids + "," + Classes + "," + Types + ")";
    }

    public sealed class PseudoClass
    {
        public string Name;
        public string Argument;
        public IReadOnlyList<Selector> Selectors = Array.Empty<Selector>();
        public bool HasNth;
        public int NthA;
        public int NthB;
    }

    public sealed class AttributeSelector
    {
        public string Name;
        public string Operator;
        public string Value;
        public bool CaseInsensitive;
    }

    public sealed class SimpleSelector
    {
        public string Type;
        public string Id;
        public IReadOnlyList<string> Classes = Array.Empty<string>();
        public IReadOnlyList<PseudoClass> Pseudos = Array.Empty<PseudoClass>();
        public IReadOnlyList<AttributeSelector> Attributes = Array.Empty<AttributeSelector>();
        public IReadOnlyList<string> PseudoElements = Array.Empty<string>();
        public bool Universal;
    }

    public sealed class Selector
    {
        public IReadOnlyList<(char Combinator, SimpleSelector Part)> Parts = Array.Empty<(char, SimpleSelector)>();
        public Specificity Specificity = Specificity.Zero;
        public string Text;
        public SourceSpan Span;
    }

    public sealed class StyleRule
    {
        public IReadOnlyList<Selector> Selectors = Array.Empty<Selector>();
        public IReadOnlyList<Declaration> Declarations = Array.Empty<Declaration>();
        public IReadOnlyList<string> AtContext = Array.Empty<string>();
        public SourceSpan Span;
        public int Order;
    }

    public sealed class AtRule
    {
        public string Name;
        public string Prelude;
        public IReadOnlyList<StyleRule> Rules = Array.Empty<StyleRule>();
        public IReadOnlyList<Declaration> Declarations = Array.Empty<Declaration>();
        public SourceSpan Span;
    }

    public sealed class ImportRef
    {
        public string Target;
        public SourceSpan Span;
    }

    public sealed class Stylesheet
    {
        public string Path;
        public StyleOrigin Origin;
        public IReadOnlyList<StyleRule> Rules = Array.Empty<StyleRule>();
        public IReadOnlyList<AtRule> AtRules = Array.Empty<AtRule>();
        public IReadOnlyList<ImportRef> Imports = Array.Empty<ImportRef>();
    }

    public interface ICssParser
    {
        Stylesheet Parse(string text, string path, StyleOrigin origin, IDiagnosticSink sink);
    }
}
