using System;
using System.Globalization;
using System.Text;

namespace HtmlToUitk.Core.Json
{
    public enum DuplicateKeyPolicy { Error, LastWins, FirstWins }

    public sealed class JsonReadOptions
    {
        public int MaxDepth = 64;
        public bool AllowComments;
        public bool AllowTrailingCommas;
        public DuplicateKeyPolicy DuplicateKeys = DuplicateKeyPolicy.Error;
        public int MaxLength = 64 * 1024 * 1024;
    }

    public sealed class JsonParseException : FormatException
    {
        public int Offset { get; }
        public int Line { get; }
        public int Column { get; }

        public JsonParseException(string message, int offset, int line, int column)
            : base(message + " (line " + line.ToString(CultureInfo.InvariantCulture) + ", column " + column.ToString(CultureInfo.InvariantCulture) + ")")
        {
            Offset = offset;
            Line = line;
            Column = column;
        }
    }

    public static class JsonReader
    {
        public static JsonValue Parse(string text) => Parse(text, new JsonReadOptions());

        public static JsonValue Parse(string text, JsonReadOptions options)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var parser = new Parser(text, options ?? new JsonReadOptions());
            return parser.ParseDocument();
        }

        public static bool TryParse(string text, out JsonValue value, out string error)
        {
            return TryParse(text, new JsonReadOptions(), out value, out error);
        }

        public static bool TryParse(string text, JsonReadOptions options, out JsonValue value, out string error)
        {
            try
            {
                value = Parse(text, options);
                error = null;
                return true;
            }
            catch (JsonParseException ex)
            {
                value = null;
                error = ex.Message;
                return false;
            }
        }

        private sealed class Parser
        {
            private readonly string _text;
            private readonly JsonReadOptions _options;
            private int _pos;

            public Parser(string text, JsonReadOptions options)
            {
                _text = text;
                _options = options;
            }

            public JsonValue ParseDocument()
            {
                if (_text.Length > _options.MaxLength) throw Error("document is longer than the allowed " + _options.MaxLength.ToString(CultureInfo.InvariantCulture) + " characters", 0);
                if (_text.Length > 0 && _text[0] == (char)0xFEFF) _pos = 1;
                SkipTrivia();
                if (_pos >= _text.Length) throw Error("unexpected end of input", _pos);
                JsonValue value = ParseValue(0);
                SkipTrivia();
                if (_pos < _text.Length) throw Error("unexpected content after the JSON value", _pos);
                return value;
            }

            private JsonParseException Error(string message, int offset)
            {
                int line = 1;
                int column = 1;
                int limit = Math.Min(offset, _text.Length);
                for (int i = 0; i < limit; i++)
                {
                    if (_text[i] == '\n') { line++; column = 1; }
                    else column++;
                }
                return new JsonParseException(message, offset, line, column);
            }

            private void SkipTrivia()
            {
                while (_pos < _text.Length)
                {
                    char c = _text[_pos];
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    {
                        _pos++;
                        continue;
                    }
                    if (_options.AllowComments && c == '/' && _pos + 1 < _text.Length)
                    {
                        char next = _text[_pos + 1];
                        if (next == '/')
                        {
                            _pos += 2;
                            while (_pos < _text.Length && _text[_pos] != '\n') _pos++;
                            continue;
                        }
                        if (next == '*')
                        {
                            int end = _text.IndexOf("*/", _pos + 2, StringComparison.Ordinal);
                            if (end < 0) throw Error("unterminated comment", _pos);
                            _pos = end + 2;
                            continue;
                        }
                    }
                    break;
                }
            }

            private JsonValue ParseValue(int depth)
            {
                if (_pos >= _text.Length) throw Error("unexpected end of input", _pos);
                char c = _text[_pos];
                switch (c)
                {
                    case '{': return ParseObject(depth + 1);
                    case '[': return ParseArray(depth + 1);
                    case '"': return new JsonString(ParseString());
                    case 't': ExpectWord("true"); return JsonValue.True;
                    case 'f': ExpectWord("false"); return JsonValue.False;
                    case 'n': ExpectWord("null"); return JsonValue.Null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber();
                        throw Error("unexpected character '" + c + "'", _pos);
                }
            }

            private void ExpectWord(string word)
            {
                if (string.CompareOrdinal(_text, _pos, word, 0, word.Length) != 0) throw Error("invalid literal, expected '" + word + "'", _pos);
                int end = _pos + word.Length;
                if (end < _text.Length && (char.IsLetterOrDigit(_text[end]) || _text[end] == '_')) throw Error("invalid literal", _pos);
                _pos = end;
            }

            private JsonValue ParseNumber()
            {
                int start = _pos;
                if (_text[_pos] == '-') _pos++;
                if (_pos >= _text.Length) throw Error("invalid number", start);
                if (_text[_pos] == '0')
                {
                    _pos++;
                    if (_pos < _text.Length && _text[_pos] >= '0' && _text[_pos] <= '9') throw Error("leading zeros are not allowed", start);
                }
                else if (_text[_pos] >= '1' && _text[_pos] <= '9')
                {
                    while (_pos < _text.Length && _text[_pos] >= '0' && _text[_pos] <= '9') _pos++;
                }
                else throw Error("invalid number", start);
                if (_pos < _text.Length && _text[_pos] == '.')
                {
                    _pos++;
                    int digits = _pos;
                    while (_pos < _text.Length && _text[_pos] >= '0' && _text[_pos] <= '9') _pos++;
                    if (digits == _pos) throw Error("digits expected after the decimal point", start);
                }
                if (_pos < _text.Length && (_text[_pos] == 'e' || _text[_pos] == 'E'))
                {
                    _pos++;
                    if (_pos < _text.Length && (_text[_pos] == '+' || _text[_pos] == '-')) _pos++;
                    int digits = _pos;
                    while (_pos < _text.Length && _text[_pos] >= '0' && _text[_pos] <= '9') _pos++;
                    if (digits == _pos) throw Error("digits expected in the exponent", start);
                }
                string raw = _text.Substring(start, _pos - start);
                double value;
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.IsInfinity(value) || double.IsNaN(value))
                    throw Error("number is out of range", start);
                return new JsonNumber(value, raw);
            }

            private string ParseString()
            {
                int start = _pos;
                _pos++;
                StringBuilder sb = null;
                int run = _pos;
                while (true)
                {
                    if (_pos >= _text.Length) throw Error("unterminated string", start);
                    char c = _text[_pos];
                    if (c == '"')
                    {
                        string result;
                        if (sb == null) result = _text.Substring(run, _pos - run);
                        else
                        {
                            sb.Append(_text, run, _pos - run);
                            result = sb.ToString();
                        }
                        _pos++;
                        return result;
                    }
                    if (c < 0x20) throw Error("control character in string", _pos);
                    if (c == '\\')
                    {
                        if (sb == null) sb = new StringBuilder();
                        sb.Append(_text, run, _pos - run);
                        _pos++;
                        if (_pos >= _text.Length) throw Error("unterminated escape", _pos);
                        char e = _text[_pos];
                        switch (e)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (_pos + 4 >= _text.Length) throw Error("truncated unicode escape", _pos);
                                int code = 0;
                                for (int i = 1; i <= 4; i++)
                                {
                                    int digit = HexValue(_text[_pos + i]);
                                    if (digit < 0) throw Error("invalid unicode escape", _pos);
                                    code = (code << 4) | digit;
                                }
                                sb.Append((char)code);
                                _pos += 4;
                                break;
                            default:
                                throw Error("invalid escape '\\" + e + "'", _pos - 1);
                        }
                        _pos++;
                        run = _pos;
                        continue;
                    }
                    _pos++;
                }
            }

            private static int HexValue(char c)
            {
                if (c >= '0' && c <= '9') return c - '0';
                if (c >= 'a' && c <= 'f') return c - 'a' + 10;
                if (c >= 'A' && c <= 'F') return c - 'A' + 10;
                return -1;
            }

            private JsonValue ParseArray(int depth)
            {
                if (depth > _options.MaxDepth) throw Error("nesting is deeper than " + _options.MaxDepth.ToString(CultureInfo.InvariantCulture), _pos);
                _pos++;
                var array = new JsonArray();
                SkipTrivia();
                if (_pos < _text.Length && _text[_pos] == ']')
                {
                    _pos++;
                    return array;
                }
                while (true)
                {
                    SkipTrivia();
                    array.Add(ParseValue(depth));
                    SkipTrivia();
                    if (_pos >= _text.Length) throw Error("unterminated array", _pos);
                    char c = _text[_pos];
                    if (c == ',')
                    {
                        _pos++;
                        SkipTrivia();
                        if (_options.AllowTrailingCommas && _pos < _text.Length && _text[_pos] == ']')
                        {
                            _pos++;
                            return array;
                        }
                        continue;
                    }
                    if (c == ']')
                    {
                        _pos++;
                        return array;
                    }
                    throw Error("expected ',' or ']'", _pos);
                }
            }

            private JsonValue ParseObject(int depth)
            {
                if (depth > _options.MaxDepth) throw Error("nesting is deeper than " + _options.MaxDepth.ToString(CultureInfo.InvariantCulture), _pos);
                _pos++;
                var obj = new JsonObject();
                SkipTrivia();
                if (_pos < _text.Length && _text[_pos] == '}')
                {
                    _pos++;
                    return obj;
                }
                while (true)
                {
                    SkipTrivia();
                    if (_pos >= _text.Length) throw Error("unterminated object", _pos);
                    if (_text[_pos] != '"') throw Error("property name must be a string", _pos);
                    int keyStart = _pos;
                    string key = ParseString();
                    SkipTrivia();
                    if (_pos >= _text.Length || _text[_pos] != ':') throw Error("expected ':' after the property name", _pos);
                    _pos++;
                    SkipTrivia();
                    JsonValue value = ParseValue(depth);
                    if (obj.Has(key))
                    {
                        if (_options.DuplicateKeys == DuplicateKeyPolicy.Error) throw Error("duplicate property '" + key + "'", keyStart);
                        if (_options.DuplicateKeys == DuplicateKeyPolicy.LastWins) obj.Set(key, value);
                    }
                    else obj.Set(key, value);
                    SkipTrivia();
                    if (_pos >= _text.Length) throw Error("unterminated object", _pos);
                    char c = _text[_pos];
                    if (c == ',')
                    {
                        _pos++;
                        SkipTrivia();
                        if (_options.AllowTrailingCommas && _pos < _text.Length && _text[_pos] == '}')
                        {
                            _pos++;
                            return obj;
                        }
                        continue;
                    }
                    if (c == '}')
                    {
                        _pos++;
                        return obj;
                    }
                    throw Error("expected ',' or '}'", _pos);
                }
            }
        }
    }
}
