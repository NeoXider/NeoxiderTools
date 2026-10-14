using System;
using System.Collections.Generic;
using System.Globalization;

namespace HtmlToUitk.Core.Json
{
    public enum JsonKind { Null, Bool, Number, String, Array, Object }

    public abstract class JsonValue
    {
        public abstract JsonKind Kind { get; }

        public static readonly JsonValue Null = new JsonNull();
        public static readonly JsonValue True = new JsonBool(true);
        public static readonly JsonValue False = new JsonBool(false);

        public static JsonValue From(bool value) => value ? True : False;
        public static JsonValue From(string value) => value == null ? Null : new JsonString(value);
        public static JsonValue From(int value) => new JsonNumber(value, value.ToString(CultureInfo.InvariantCulture));
        public static JsonValue From(long value) => new JsonNumber(value, value.ToString(CultureInfo.InvariantCulture));
        public static JsonValue From(double value) => new JsonNumber(value, null);

        public bool IsNull => Kind == JsonKind.Null;
        public string AsString() => (this as JsonString)?.Value;
        public bool? AsBool() => (this as JsonBool)?.Value;
        public double? AsDouble() => (this as JsonNumber)?.Value;

        public int? AsInt()
        {
            var number = this as JsonNumber;
            if (number == null || !number.IsInteger) return null;
            if (number.Value < int.MinValue || number.Value > int.MaxValue) return null;
            return (int)number.Value;
        }

        public long? AsLong()
        {
            var number = this as JsonNumber;
            if (number == null || !number.IsInteger) return null;
            if (number.Value < -9007199254740992d || number.Value > 9007199254740992d) return null;
            return (long)number.Value;
        }

        public JsonArray AsArray() => this as JsonArray;
        public JsonObject AsObject() => this as JsonObject;

        public override string ToString() => JsonWriter.Write(this, JsonWriteOptions.Compact);
    }

    public sealed class JsonNull : JsonValue
    {
        internal JsonNull() { }
        public override JsonKind Kind => JsonKind.Null;
    }

    public sealed class JsonBool : JsonValue
    {
        public readonly bool Value;
        internal JsonBool(bool value) { Value = value; }
        public override JsonKind Kind => JsonKind.Bool;
    }

    public sealed class JsonNumber : JsonValue
    {
        public readonly double Value;
        public readonly string Raw;

        public JsonNumber(double value, string raw)
        {
            Value = value;
            Raw = raw;
        }

        public override JsonKind Kind => JsonKind.Number;

        public bool IsInteger => !double.IsInfinity(Value) && !double.IsNaN(Value) && Value == Math.Floor(Value) && Math.Abs(Value) < 9007199254740992d;

        public string ToText()
        {
            if (Raw != null) return Raw;
            if (double.IsNaN(Value) || double.IsInfinity(Value)) throw new InvalidOperationException("JSON cannot represent a non-finite number");
            if (Value == Math.Floor(Value) && Math.Abs(Value) < 1e15) return ((long)Value).ToString(CultureInfo.InvariantCulture);
            return Value.ToString("R", CultureInfo.InvariantCulture);
        }
    }

    public sealed class JsonString : JsonValue
    {
        public readonly string Value;
        public JsonString(string value) { Value = value ?? ""; }
        public override JsonKind Kind => JsonKind.String;
    }

    public sealed class JsonArray : JsonValue
    {
        private readonly List<JsonValue> _items = new List<JsonValue>();

        public JsonArray() { }

        public JsonArray(IEnumerable<JsonValue> items)
        {
            foreach (JsonValue item in items) _items.Add(item ?? Null);
        }

        public override JsonKind Kind => JsonKind.Array;
        public int Count => _items.Count;
        public IReadOnlyList<JsonValue> Items => _items;
        public JsonValue this[int index] => _items[index];

        public JsonArray Add(JsonValue value)
        {
            _items.Add(value ?? Null);
            return this;
        }

        public JsonArray Add(string value) => Add(From(value));
    }

    public sealed class JsonObject : JsonValue
    {
        private readonly List<KeyValuePair<string, JsonValue>> _members = new List<KeyValuePair<string, JsonValue>>();
        private readonly Dictionary<string, int> _index = new Dictionary<string, int>(StringComparer.Ordinal);

        public override JsonKind Kind => JsonKind.Object;
        public int Count => _members.Count;
        public IReadOnlyList<KeyValuePair<string, JsonValue>> Members => _members;

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (KeyValuePair<string, JsonValue> member in _members) yield return member.Key;
            }
        }

        public bool Has(string key) => _index.ContainsKey(key);

        public JsonValue this[string key]
        {
            get
            {
                int position;
                return _index.TryGetValue(key, out position) ? _members[position].Value : null;
            }
        }

        public JsonObject Set(string key, JsonValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            value = value ?? Null;
            int position;
            if (_index.TryGetValue(key, out position)) _members[position] = new KeyValuePair<string, JsonValue>(key, value);
            else
            {
                _index[key] = _members.Count;
                _members.Add(new KeyValuePair<string, JsonValue>(key, value));
            }
            return this;
        }

        public JsonObject Set(string key, string value) => Set(key, From(value));
        public JsonObject Set(string key, bool value) => Set(key, From(value));
        public JsonObject Set(string key, int value) => Set(key, From(value));
        public JsonObject Set(string key, double value) => Set(key, From(value));

        public string GetString(string key, string fallback = null) => this[key]?.AsString() ?? fallback;
        public bool GetBool(string key, bool fallback = false) => this[key]?.AsBool() ?? fallback;
        public int GetInt(string key, int fallback = 0) => this[key]?.AsInt() ?? fallback;
        public double GetDouble(string key, double fallback = 0) => this[key]?.AsDouble() ?? fallback;
        public JsonArray GetArray(string key) => this[key] as JsonArray;
        public JsonObject GetObject(string key) => this[key] as JsonObject;

        public IReadOnlyList<string> GetStringList(string key)
        {
            JsonArray array = GetArray(key);
            if (array == null) return null;
            var list = new List<string>(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                string text = array[i].AsString();
                if (text != null) list.Add(text);
            }
            return list;
        }

        public static JsonValue Canonicalize(JsonValue value)
        {
            var obj = value as JsonObject;
            if (obj != null)
            {
                var keys = new List<string>(obj.Keys);
                keys.Sort(StringComparer.Ordinal);
                var copy = new JsonObject();
                foreach (string key in keys) copy.Set(key, Canonicalize(obj[key]));
                return copy;
            }
            var array = value as JsonArray;
            if (array != null)
            {
                var copy = new JsonArray();
                for (int i = 0; i < array.Count; i++) copy.Add(Canonicalize(array[i]));
                return copy;
            }
            return value;
        }
    }
}
