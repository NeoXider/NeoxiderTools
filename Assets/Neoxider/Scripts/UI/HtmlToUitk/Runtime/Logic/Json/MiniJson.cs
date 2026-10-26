using System;
using System.Globalization;
using System.Text;

namespace Neo.HtmlToUitk.Json
{
    public sealed class JsonParseException : Exception
    {
        public JsonParseException(string message, int line, int column)
            : base(message + " (line " + line.ToString(CultureInfo.InvariantCulture) + ", column " + column.ToString(CultureInfo.InvariantCulture) + ")")
        {
            Line = line;
            Column = column;
        }

        public int Line { get; }

        public int Column { get; }
    }

    public static class MiniJson
    {
        public static JsonValue Parse(string text)
        {
            if (text == null)
                throw new JsonParseException("Input is null", 1, 1);
            Reader reader = new Reader(text);
            reader.SkipBom();
            reader.SkipWhitespace();
            JsonValue value = reader.ReadValue(0);
            reader.SkipWhitespace();
            if (!reader.AtEnd)
                throw reader.Error("Unexpected trailing content");
            return value;
        }

        public static bool TryParse(string text, out JsonValue value, out string error)
        {
            try
            {
                value = Parse(text);
                error = null;
                return true;
            }
            catch (JsonParseException exception)
            {
                value = null;
                error = exception.Message;
                return false;
            }
        }

        public static string Write(JsonValue value, bool indent = true)
        {
            StringBuilder builder = new StringBuilder();
            WriteValue(builder, value ?? JsonValue.Missing, indent, 0);
            if (indent)
                builder.Append('\n');
            return builder.ToString();
        }

        public static string Quote(string text)
        {
            StringBuilder builder = new StringBuilder();
            WriteString(builder, text);
            return builder.ToString();
        }

        private static void WriteValue(StringBuilder builder, JsonValue value, bool indent, int depth)
        {
            switch (value.Kind)
            {
                case JsonKind.Null:
                    builder.Append("null");
                    break;
                case JsonKind.Bool:
                    builder.Append(value.AsBool() ? "true" : "false");
                    break;
                case JsonKind.Number:
                    builder.Append(FormatNumber(value.AsNumber()));
                    break;
                case JsonKind.String:
                    WriteString(builder, value.AsString());
                    break;
                case JsonKind.Array:
                    WriteArray(builder, value, indent, depth);
                    break;
                case JsonKind.Object:
                    WriteObject(builder, value, indent, depth);
                    break;
            }
        }

        private static void WriteArray(StringBuilder builder, JsonValue value, bool indent, int depth)
        {
            if (value.Count == 0)
            {
                builder.Append("[]");
                return;
            }

            bool inline = !indent || AllScalar(value);
            builder.Append('[');
            for (int i = 0; i < value.Count; i++)
            {
                if (i > 0)
                    builder.Append(inline ? (indent ? ", " : ",") : ",");
                if (!inline)
                    NewLine(builder, depth + 1);
                WriteValue(builder, value[i], indent, depth + 1);
            }

            if (!inline)
                NewLine(builder, depth);
            builder.Append(']');
        }

        private static void WriteObject(StringBuilder builder, JsonValue value, bool indent, int depth)
        {
            if (value.Count == 0)
            {
                builder.Append("{}");
                return;
            }

            builder.Append('{');
            for (int i = 0; i < value.Members.Count; i++)
            {
                if (i > 0)
                    builder.Append(',');
                if (indent)
                    NewLine(builder, depth + 1);
                WriteString(builder, value.Members[i].Key);
                builder.Append(indent ? ": " : ":");
                WriteValue(builder, value.Members[i].Value, indent, depth + 1);
            }

            if (indent)
                NewLine(builder, depth);
            builder.Append('}');
        }

        private static bool AllScalar(JsonValue array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                JsonKind kind = array[i].Kind;
                if (kind == JsonKind.Array || kind == JsonKind.Object)
                    return false;
            }

            return true;
        }

        private static void NewLine(StringBuilder builder, int depth)
        {
            builder.Append('\n');
            builder.Append(' ', depth * 2);
        }

        private static string FormatNumber(double number)
        {
            if (double.IsNaN(number) || double.IsInfinity(number))
                return "null";
            if (number == Math.Round(number) && Math.Abs(number) < 1e15)
                return Math.Round(number).ToString("0", CultureInfo.InvariantCulture);
            return number.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void WriteString(StringBuilder builder, string text)
        {
            if (text == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(c);
                        break;
                }
            }

            builder.Append('"');
        }

        private sealed class Reader
        {
            private const int MaxDepth = 128;
            private readonly string _text;
            private int _pos;
            private int _line = 1;
            private int _column = 1;

            public Reader(string text)
            {
                _text = text;
            }

            public bool AtEnd => _pos >= _text.Length;

            public JsonParseException Error(string message) => new JsonParseException(message, _line, _column);

            public void SkipBom()
            {
                if (_text.Length > 0 && _text[0] == '\uFEFF')
                    _pos = 1;
            }

            public void SkipWhitespace()
            {
                while (_pos < _text.Length)
                {
                    char c = _text[_pos];
                    if (c == ' ' || c == '\t' || c == '\r')
                        Advance();
                    else if (c == '\n')
                        Advance();
                    else
                        break;
                }
            }

            private void Advance()
            {
                if (_text[_pos] == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }

                _pos++;
            }

            public JsonValue ReadValue(int depth)
            {
                if (depth > MaxDepth)
                    throw Error("Nesting too deep");
                if (AtEnd)
                    throw Error("Unexpected end of input");
                char c = _text[_pos];
                switch (c)
                {
                    case '{': return ReadObject(depth);
                    case '[': return ReadArray(depth);
                    case '"': return JsonValue.FromString(ReadString());
                    case 't': Expect("true"); return JsonValue.FromBool(true);
                    case 'f': Expect("false"); return JsonValue.FromBool(false);
                    case 'n': Expect("null"); return JsonValue.FromString(null);
                    default:
                        if (c == '-' || (c >= '0' && c <= '9'))
                            return ReadNumber();
                        throw Error("Unexpected character '" + c + "'");
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_text, _pos, word, 0, word.Length) != 0)
                    throw Error("Invalid literal");
                for (int i = 0; i < word.Length; i++)
                    Advance();
            }

            private JsonValue ReadObject(int depth)
            {
                JsonValue result = JsonValue.NewObject();
                Advance();
                SkipWhitespace();
                if (!AtEnd && _text[_pos] == '}')
                {
                    Advance();
                    return result;
                }

                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _text[_pos] != '"')
                        throw Error("Expected a property name");
                    string key = ReadString();
                    SkipWhitespace();
                    if (AtEnd || _text[_pos] != ':')
                        throw Error("Expected ':'");
                    Advance();
                    SkipWhitespace();
                    result.Set(key, ReadValue(depth + 1));
                    SkipWhitespace();
                    if (AtEnd)
                        throw Error("Unterminated object");
                    if (_text[_pos] == ',')
                    {
                        Advance();
                        continue;
                    }

                    if (_text[_pos] == '}')
                    {
                        Advance();
                        return result;
                    }

                    throw Error("Expected ',' or '}'");
                }
            }

            private JsonValue ReadArray(int depth)
            {
                JsonValue result = JsonValue.NewArray();
                Advance();
                SkipWhitespace();
                if (!AtEnd && _text[_pos] == ']')
                {
                    Advance();
                    return result;
                }

                while (true)
                {
                    SkipWhitespace();
                    result.Add(ReadValue(depth + 1));
                    SkipWhitespace();
                    if (AtEnd)
                        throw Error("Unterminated array");
                    if (_text[_pos] == ',')
                    {
                        Advance();
                        continue;
                    }

                    if (_text[_pos] == ']')
                    {
                        Advance();
                        return result;
                    }

                    throw Error("Expected ',' or ']'");
                }
            }

            private string ReadString()
            {
                Advance();
                StringBuilder builder = new StringBuilder();
                while (true)
                {
                    if (AtEnd)
                        throw Error("Unterminated string");
                    char c = _text[_pos];
                    if (c == '"')
                    {
                        Advance();
                        return builder.ToString();
                    }

                    if (c == '\\')
                    {
                        Advance();
                        if (AtEnd)
                            throw Error("Unterminated escape");
                        char e = _text[_pos];
                        switch (e)
                        {
                            case '"': builder.Append('"'); break;
                            case '\\': builder.Append('\\'); break;
                            case '/': builder.Append('/'); break;
                            case 'b': builder.Append('\b'); break;
                            case 'f': builder.Append('\f'); break;
                            case 'n': builder.Append('\n'); break;
                            case 'r': builder.Append('\r'); break;
                            case 't': builder.Append('\t'); break;
                            case 'u':
                                if (_pos + 4 >= _text.Length)
                                    throw Error("Bad unicode escape");
                                if (!int.TryParse(_text.Substring(_pos + 1, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
                                    throw Error("Bad unicode escape");
                                builder.Append((char)code);
                                Advance();
                                Advance();
                                Advance();
                                Advance();
                                break;
                            default:
                                throw Error("Bad escape");
                        }

                        Advance();
                        continue;
                    }

                    if (c < 0x20)
                        throw Error("Control character in string");
                    builder.Append(c);
                    Advance();
                }
            }

            private JsonValue ReadNumber()
            {
                int start = _pos;
                if (_text[_pos] == '-')
                    Advance();
                if (AtEnd || _text[_pos] < '0' || _text[_pos] > '9')
                    throw Error("Expected a digit");
                if (_text[_pos] == '0')
                {
                    Advance();
                    if (!AtEnd && _text[_pos] >= '0' && _text[_pos] <= '9')
                        throw Error("Leading zero in number");
                }
                else
                {
                    while (!AtEnd && _text[_pos] >= '0' && _text[_pos] <= '9')
                        Advance();
                }

                if (!AtEnd && _text[_pos] == '.')
                {
                    Advance();
                    int fraction = _pos;
                    while (!AtEnd && _text[_pos] >= '0' && _text[_pos] <= '9')
                        Advance();
                    if (_pos == fraction)
                        throw Error("Expected a fractional digit");
                }

                if (!AtEnd && (_text[_pos] == 'e' || _text[_pos] == 'E'))
                {
                    Advance();
                    if (!AtEnd && (_text[_pos] == '+' || _text[_pos] == '-'))
                        Advance();
                    int exponent = _pos;
                    while (!AtEnd && _text[_pos] >= '0' && _text[_pos] <= '9')
                        Advance();
                    if (_pos == exponent)
                        throw Error("Expected an exponent digit");
                }

                string token = _text.Substring(start, _pos - start);
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
                    || double.IsNaN(number) || double.IsInfinity(number))
                    throw Error("Invalid number '" + token + "'");
                return JsonValue.FromNumber(number);
            }
        }
    }
}
