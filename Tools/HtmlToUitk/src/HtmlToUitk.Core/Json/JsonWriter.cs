using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HtmlToUitk.Core.Json
{
    public sealed class JsonWriteOptions
    {
        public int Indent = 2;
        public bool SortKeys;
        public string NewLine = "\n";
        public bool TrailingNewLine;

        public static JsonWriteOptions Compact => new JsonWriteOptions { Indent = 0 };
        public static JsonWriteOptions Pretty => new JsonWriteOptions { Indent = 2, TrailingNewLine = true };
        public static JsonWriteOptions Canonical => new JsonWriteOptions { Indent = 2, SortKeys = true, TrailingNewLine = true };
    }

    public static class JsonWriter
    {
        public static string Write(JsonValue value) => Write(value, JsonWriteOptions.Pretty);

        public static string Write(JsonValue value, JsonWriteOptions options)
        {
            if (value == null) value = JsonValue.Null;
            options = options ?? JsonWriteOptions.Pretty;
            var sb = new StringBuilder();
            WriteValue(sb, value, options, 0);
            if (options.TrailingNewLine) sb.Append(options.NewLine);
            return sb.ToString();
        }

        private static void NewLine(StringBuilder sb, JsonWriteOptions options, int depth)
        {
            if (options.Indent <= 0) return;
            sb.Append(options.NewLine);
            sb.Append(' ', options.Indent * depth);
        }

        private static void WriteValue(StringBuilder sb, JsonValue value, JsonWriteOptions options, int depth)
        {
            switch (value.Kind)
            {
                case JsonKind.Null:
                    sb.Append("null");
                    break;
                case JsonKind.Bool:
                    sb.Append(((JsonBool)value).Value ? "true" : "false");
                    break;
                case JsonKind.Number:
                    sb.Append(((JsonNumber)value).ToText());
                    break;
                case JsonKind.String:
                    WriteString(sb, ((JsonString)value).Value);
                    break;
                case JsonKind.Array:
                {
                    var array = (JsonArray)value;
                    if (array.Count == 0)
                    {
                        sb.Append("[]");
                        break;
                    }
                    sb.Append('[');
                    for (int i = 0; i < array.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        NewLine(sb, options, depth + 1);
                        WriteValue(sb, array[i], options, depth + 1);
                    }
                    NewLine(sb, options, depth);
                    sb.Append(']');
                    break;
                }
                case JsonKind.Object:
                {
                    var obj = (JsonObject)value;
                    if (obj.Count == 0)
                    {
                        sb.Append("{}");
                        break;
                    }
                    IReadOnlyList<KeyValuePair<string, JsonValue>> members = obj.Members;
                    if (options.SortKeys)
                    {
                        var sorted = new List<KeyValuePair<string, JsonValue>>(members);
                        sorted.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
                        members = sorted;
                    }
                    sb.Append('{');
                    for (int i = 0; i < members.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        NewLine(sb, options, depth + 1);
                        WriteString(sb, members[i].Key);
                        sb.Append(options.Indent > 0 ? ": " : ":");
                        WriteValue(sb, members[i].Value, options, depth + 1);
                    }
                    NewLine(sb, options, depth);
                    sb.Append('}');
                    break;
                }
            }
        }

        public static void WriteString(StringBuilder sb, string value)
        {
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20 || (char.IsSurrogate(c) && !IsPairedSurrogate(value, i)))
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private static bool IsPairedSurrogate(string value, int index)
        {
            char c = value[index];
            if (char.IsHighSurrogate(c)) return index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]);
            return index > 0 && char.IsHighSurrogate(value[index - 1]);
        }
    }
}
