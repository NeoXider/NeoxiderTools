using System;
using System.Collections.Generic;

namespace Neo.HtmlToUitk.Json
{
    public enum JsonKind
    {
        Null,
        Bool,
        Number,
        String,
        Array,
        Object
    }

    public sealed class JsonValue
    {
        public static readonly JsonValue Missing = new JsonValue(JsonKind.Null);

        private readonly bool _bool;
        private readonly double _number;
        private readonly string _string;
        private readonly List<JsonValue> _items;
        private readonly List<KeyValuePair<string, JsonValue>> _members;

        private JsonValue(JsonKind kind)
        {
            Kind = kind;
            if (kind == JsonKind.Array)
                _items = new List<JsonValue>();
            else if (kind == JsonKind.Object)
                _members = new List<KeyValuePair<string, JsonValue>>();
        }

        private JsonValue(bool value) : this(JsonKind.Bool)
        {
            _bool = value;
        }

        private JsonValue(double value) : this(JsonKind.Number)
        {
            _number = value;
        }

        private JsonValue(string value) : this(value == null ? JsonKind.Null : JsonKind.String)
        {
            _string = value;
        }

        public JsonKind Kind { get; }

        public bool IsMissingOrNull => Kind == JsonKind.Null;

        public static JsonValue FromBool(bool value) => new JsonValue(value);

        public static JsonValue FromNumber(double value) => new JsonValue(value);

        public static JsonValue FromString(string value) => new JsonValue(value);

        public static JsonValue NewArray() => new JsonValue(JsonKind.Array);

        public static JsonValue NewObject() => new JsonValue(JsonKind.Object);

        public static JsonValue FromStrings(IEnumerable<string> values)
        {
            JsonValue array = NewArray();
            if (values != null)
            {
                foreach (string value in values)
                    array.Add(FromString(value));
            }

            return array;
        }

        public int Count => Kind == JsonKind.Array ? _items.Count : Kind == JsonKind.Object ? _members.Count : 0;

        public IReadOnlyList<JsonValue> Items => Kind == JsonKind.Array ? (IReadOnlyList<JsonValue>)_items : Array.Empty<JsonValue>();

        public IReadOnlyList<KeyValuePair<string, JsonValue>> Members =>
            Kind == JsonKind.Object ? (IReadOnlyList<KeyValuePair<string, JsonValue>>)_members : Array.Empty<KeyValuePair<string, JsonValue>>();

        public JsonValue this[string key]
        {
            get
            {
                if (Kind != JsonKind.Object)
                    return Missing;
                for (int i = 0; i < _members.Count; i++)
                {
                    if (string.Equals(_members[i].Key, key, StringComparison.Ordinal))
                        return _members[i].Value;
                }

                return Missing;
            }
        }

        public JsonValue this[int index] => Kind == JsonKind.Array && index >= 0 && index < _items.Count ? _items[index] : Missing;

        public bool Has(string key)
        {
            if (Kind != JsonKind.Object)
                return false;
            for (int i = 0; i < _members.Count; i++)
            {
                if (string.Equals(_members[i].Key, key, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public bool AsBool(bool fallback = false) => Kind == JsonKind.Bool ? _bool : fallback;

        public double AsNumber(double fallback = 0) => Kind == JsonKind.Number ? _number : fallback;

        public int AsInt(int fallback = 0) => Kind == JsonKind.Number ? (int)Math.Round(_number) : fallback;

        public string AsString(string fallback = null) => Kind == JsonKind.String ? _string : fallback;

        public JsonValue Add(JsonValue value)
        {
            if (Kind != JsonKind.Array)
                throw new InvalidOperationException("Not an array.");
            _items.Add(value ?? FromString(null));
            return this;
        }

        public JsonValue Set(string key, JsonValue value)
        {
            if (Kind != JsonKind.Object)
                throw new InvalidOperationException("Not an object.");
            value = value ?? FromString(null);
            for (int i = 0; i < _members.Count; i++)
            {
                if (string.Equals(_members[i].Key, key, StringComparison.Ordinal))
                {
                    _members[i] = new KeyValuePair<string, JsonValue>(key, value);
                    return this;
                }
            }

            _members.Add(new KeyValuePair<string, JsonValue>(key, value));
            return this;
        }

        public bool Remove(string key)
        {
            if (Kind != JsonKind.Object)
                return false;
            for (int i = 0; i < _members.Count; i++)
            {
                if (string.Equals(_members[i].Key, key, StringComparison.Ordinal))
                {
                    _members.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public List<string> ToStringList()
        {
            List<string> result = new List<string>();
            if (Kind != JsonKind.Array)
                return result;
            for (int i = 0; i < _items.Count; i++)
            {
                string text = _items[i].AsString();
                if (text != null)
                    result.Add(text);
            }

            return result;
        }
    }
}
