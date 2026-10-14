using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HtmlToUitk.Capabilities.Reader;

public sealed class JObj
{
    public readonly List<KeyValuePair<string, object>> Items = new List<KeyValuePair<string, object>>();
    public bool Inline;

    public JObj Add(string key, object value)
    {
        Items.Add(new KeyValuePair<string, object>(key, value));
        return this;
    }

    public JObj Inlined()
    {
        Inline = true;
        return this;
    }
}

public static class JsonText
{
    private const int WrapWidth = 110;
    private const int InlineObjectWidth = 190;

    public static string Write(object value)
    {
        var sb = new StringBuilder();
        WriteValue(sb, value, 0);
        sb.Append('\n');
        return sb.ToString();
    }

    public static string Quote(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                    {
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }

    private static bool IsScalar(object v)
    {
        return v == null || v is string || v is bool || v is int || v is long || v is double;
    }

    private static string Scalar(object v)
    {
        switch (v)
        {
            case null: return "null";
            case string s: return Quote(s);
            case bool b: return b ? "true" : "false";
            case int i: return i.ToString(CultureInfo.InvariantCulture);
            case long l: return l.ToString(CultureInfo.InvariantCulture);
            case double d: return d.ToString("0.####", CultureInfo.InvariantCulture);
            default: throw new InvalidOperationException("unsupported json scalar " + v.GetType());
        }
    }

    private static List<object> AsList(object v)
    {
        var list = new List<object>();
        foreach (var item in (IEnumerable)v)
        {
            list.Add(item);
        }

        return list;
    }

    private static string InlineText(object v)
    {
        if (IsScalar(v))
        {
            return Scalar(v);
        }

        if (v is JObj o)
        {
            if (o.Items.Count == 0)
            {
                return "{}";
            }

            var parts = new List<string>();
            foreach (var kv in o.Items)
            {
                parts.Add(Quote(kv.Key) + ": " + InlineText(kv.Value));
            }

            return "{ " + string.Join(", ", parts) + " }";
        }

        var items = AsList(v);
        if (items.Count == 0)
        {
            return "[]";
        }

        var texts = new List<string>();
        foreach (var item in items)
        {
            texts.Add(InlineText(item));
        }

        return "[" + string.Join(", ", texts) + "]";
    }

    private static bool CanInline(object v)
    {
        if (IsScalar(v))
        {
            return true;
        }

        if (v is JObj o)
        {
            foreach (var kv in o.Items)
            {
                if (!CanInline(kv.Value))
                {
                    return false;
                }
            }

            return true;
        }

        foreach (var item in AsList(v))
        {
            if (!IsScalar(item))
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteValue(StringBuilder sb, object v, int depth)
    {
        if (IsScalar(v))
        {
            sb.Append(Scalar(v));
            return;
        }

        var pad = new string(' ', depth * 2);
        var innerPad = new string(' ', (depth + 1) * 2);
        if (v is JObj o)
        {
            if (o.Items.Count == 0)
            {
                sb.Append("{}");
                return;
            }

            if (o.Inline && CanInline(o))
            {
                var text = InlineText(o);
                if (text.Length + innerPad.Length <= InlineObjectWidth)
                {
                    sb.Append(text);
                    return;
                }
            }

            sb.Append("{\n");
            for (var i = 0; i < o.Items.Count; i++)
            {
                sb.Append(innerPad).Append(Quote(o.Items[i].Key)).Append(": ");
                WriteValue(sb, o.Items[i].Value, depth + 1);
                sb.Append(i + 1 < o.Items.Count ? ",\n" : "\n");
            }

            sb.Append(pad).Append('}');
            return;
        }

        var items = AsList(v);
        if (items.Count == 0)
        {
            sb.Append("[]");
            return;
        }

        if (CanInline(v))
        {
            var text = InlineText(v);
            if (text.Length + innerPad.Length <= WrapWidth)
            {
                sb.Append(text);
                return;
            }

            sb.Append("[\n");
            var line = new StringBuilder(innerPad);
            var first = true;
            for (var i = 0; i < items.Count; i++)
            {
                var piece = InlineText(items[i]) + (i + 1 < items.Count ? "," : "");
                if (!first && line.Length + 1 + piece.Length > WrapWidth)
                {
                    sb.Append(line).Append('\n');
                    line.Clear().Append(innerPad);
                    first = true;
                }

                if (!first)
                {
                    line.Append(' ');
                }

                line.Append(piece);
                first = false;
            }

            sb.Append(line).Append('\n').Append(pad).Append(']');
            return;
        }

        sb.Append("[\n");
        for (var i = 0; i < items.Count; i++)
        {
            sb.Append(innerPad);
            WriteValue(sb, items[i], depth + 1);
            sb.Append(i + 1 < items.Count ? ",\n" : "\n");
        }

        sb.Append(pad).Append(']');
    }
}
