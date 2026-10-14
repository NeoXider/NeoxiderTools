using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HtmlToUitk.Core.Json;

namespace HtmlToUitk.Capabilities.Model
{
    public sealed class CapabilityFormatException : FormatException
    {
        public CapabilityFormatException(string message) : base(message) { }
    }

    public static class CapabilityMatrixLoader
    {
        public const int SupportedSchema = 1;

        public static CapabilityMatrix Load(string json)
        {
            return new CapabilityMatrix(Parse(json));
        }

        public static CapabilityMatrix LoadFile(string path)
        {
            return Load(File.ReadAllText(path, Encoding.UTF8));
        }

        public static IReadOnlyList<string> EmbeddedVersions()
        {
            var versions = new List<string>();
            foreach (string name in typeof(CapabilityMatrixLoader).Assembly.GetManifestResourceNames())
            {
                const string prefix = "spec/capabilities.";
                if (name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
                {
                    string version = name.Substring(prefix.Length, name.Length - prefix.Length - ".json".Length);
                    if (version != "schema") versions.Add(version);
                }
            }
            versions.Sort(StringComparer.Ordinal);
            return versions;
        }

        public static CapabilityMatrix LoadEmbedded(string version)
        {
            string text = ReadEmbedded(version);
            if (text == null) throw new CapabilityFormatException("no embedded capability matrix for Unity " + version);
            return Load(text);
        }

        public static bool TryLoadEmbedded(string version, out CapabilityMatrix matrix)
        {
            string text = ReadEmbedded(version);
            matrix = text == null ? null : Load(text);
            return matrix != null;
        }

        private static string ReadEmbedded(string version)
        {
            if (string.IsNullOrEmpty(version)) return null;
            string wanted = "spec/capabilities." + version + ".json";
            Assembly assembly = typeof(CapabilityMatrixLoader).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(wanted))
            {
                if (stream == null) return null;
                using (var reader = new StreamReader(stream, Encoding.UTF8)) return reader.ReadToEnd();
            }
        }

        public static CapabilityData Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            JsonValue root;
            try
            {
                root = JsonReader.Parse(json);
            }
            catch (JsonParseException ex)
            {
                throw new CapabilityFormatException("capability file is not valid JSON: " + ex.Message);
            }
            JsonObject obj = root.AsObject();
            if (obj == null) throw new CapabilityFormatException("capability file must be a JSON object");
            int schema = obj.GetInt("schema", -1);
            if (schema != SupportedSchema) throw new CapabilityFormatException("unsupported capability schema " + schema + " (expected " + SupportedSchema + ")");

            var data = new CapabilityData();
            data.UnityVersion = obj.GetString("unityVersion");
            if (string.IsNullOrEmpty(data.UnityVersion)) throw new CapabilityFormatException("capability file has no unityVersion");

            JsonObject shorthandObj = obj.GetObject("shorthands");
            var shorthands = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            if (shorthandObj != null)
            {
                foreach (KeyValuePair<string, JsonValue> member in shorthandObj.Members)
                {
                    shorthands[member.Key] = StringList(member.Value, "shorthands." + member.Key);
                }
            }
            data.Shorthands = shorthands;

            JsonObject props = obj.GetObject("properties");
            if (props == null) throw new CapabilityFormatException("capability file has no properties object");
            var properties = new Dictionary<string, PropertyCapability>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonValue> member in props.Members)
            {
                JsonObject p = member.Value.AsObject();
                if (p == null) throw new CapabilityFormatException("properties." + member.Key + " must be an object");
                var capability = new PropertyCapability
                {
                    Name = member.Key,
                    Id = p.GetString("id"),
                    Inherited = p.GetBool("inherited"),
                    Animatable = ParseAnimatable(p.GetString("animatable"), member.Key),
                    Evidence = p.GetString("evidence"),
                };
                if (p.Has("values")) capability.Values = StringList(p["values"], "properties." + member.Key + ".values");
                IReadOnlyList<string> longhands;
                if (shorthands.TryGetValue(member.Key, out longhands)) capability.Longhands = longhands;
                properties[member.Key] = capability;
            }
            data.Properties = properties;

            JsonObject selectors = obj.GetObject("selectors");
            if (selectors != null)
            {
                data.Combinators = Names(selectors, "combinators");
                data.SelectorTypes = Names(selectors, "types");
                data.PseudoClasses = Names(selectors, "pseudoClasses");
            }

            var functions = new Dictionary<string, bool>(StringComparer.Ordinal);
            var evidence = new Dictionary<string, string>(StringComparer.Ordinal);
            JsonObject functionObj = obj.GetObject("functions");
            if (functionObj != null)
            {
                foreach (KeyValuePair<string, JsonValue> member in functionObj.Members)
                {
                    JsonValue v = member.Value;
                    if (v.Kind == JsonKind.Bool)
                    {
                        functions[member.Key] = v.AsBool().Value;
                    }
                    else if (v.Kind == JsonKind.Object)
                    {
                        functions[member.Key] = true;
                        string e = v.AsObject().GetString("evidence");
                        if (e != null) evidence[member.Key] = e;
                    }
                    else
                    {
                        throw new CapabilityFormatException("functions." + member.Key + " must be a boolean or an object");
                    }
                }
            }
            data.Functions = functions;
            data.FunctionEvidence = evidence;

            data.Keywords = Names(obj, "keywords");
            data.Filters = Names(obj, "filters");
            data.Easings = Names(obj, "easings");

            JsonObject units = obj.GetObject("units");
            if (units != null)
            {
                data.LengthUnits = Names(units, "length");
                data.AngleUnits = Names(units, "angle");
                data.TimeUnits = Names(units, "time");
            }

            var conditions = new Dictionary<string, string>(StringComparer.Ordinal);
            JsonObject conditionObj = obj.GetObject("conditions");
            if (conditionObj != null)
            {
                foreach (KeyValuePair<string, JsonValue> member in conditionObj.Members)
                {
                    string text = member.Value.AsString();
                    if (text == null) throw new CapabilityFormatException("conditions." + member.Key + " must be a string");
                    conditions[member.Key] = text;
                }
            }
            data.Conditions = conditions;
            data.Warnings = Names(obj, "warnings");
            return data;
        }

        private static Animatable ParseAnimatable(string text, string property)
        {
            switch (text)
            {
                case null:
                case "none": return Animatable.None;
                case "discrete": return Animatable.Discrete;
                case "full": return Animatable.Full;
                default: throw new CapabilityFormatException("properties." + property + ".animatable has unknown value '" + text + "'");
            }
        }

        private static IReadOnlyList<string> Names(JsonObject parent, string key)
        {
            JsonValue value = parent[key];
            return value == null ? Array.Empty<string>() : StringList(value, key);
        }

        private static IReadOnlyList<string> StringList(JsonValue value, string where)
        {
            JsonArray array = value.AsArray();
            if (array == null) throw new CapabilityFormatException(where + " must be an array of strings");
            var list = new List<string>(array.Count);
            foreach (JsonValue item in array.Items)
            {
                string s = item.AsString();
                if (s == null) throw new CapabilityFormatException(where + " must contain only strings");
                list.Add(s);
            }
            return list;
        }
    }
}
