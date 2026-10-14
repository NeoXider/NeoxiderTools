using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HtmlToUitk.Core.Json;

namespace HtmlToUitk.Rules
{
    public static class EmbeddedSpec
    {
        public static string ReadText(string name)
        {
            Assembly assembly = typeof(EmbeddedSpec).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream("spec/" + name))
            {
                if (stream == null) throw new FileNotFoundException("embedded spec resource not found: " + name);
                using (var reader = new StreamReader(stream, Encoding.UTF8)) return reader.ReadToEnd();
            }
        }
    }

    public sealed class UssSpec
    {
        private static UssSpec _embedded;

        public HashSet<string> LengthProperties { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> NumberProperties { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> ColorProperties { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> LayoutProperties { get; } = new HashSet<string>(StringComparer.Ordinal);
        public Dictionary<string, IReadOnlyList<string>> Enums { get; } = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        public Dictionary<string, string> PropertyAdvice { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public List<KeyValuePair<string, string>> PrefixAdvice { get; } = new List<KeyValuePair<string, string>>();
        public string DefaultAdvice { get; private set; } = "";

        public static UssSpec Embedded => _embedded ?? (_embedded = Load(EmbeddedSpec.ReadText("uss.json")));

        public static UssSpec Load(string json)
        {
            JsonObject root = JsonReader.Parse(json).AsObject();
            if (root == null) throw new FormatException("uss.json must be an object");
            var spec = new UssSpec();
            Fill(spec.LengthProperties, root, "lengthProperties");
            Fill(spec.NumberProperties, root, "numberProperties");
            Fill(spec.ColorProperties, root, "colorProperties");
            Fill(spec.LayoutProperties, root, "layoutProperties");
            JsonObject enums = root.GetObject("enums");
            if (enums != null)
            {
                foreach (KeyValuePair<string, JsonValue> member in enums.Members)
                {
                    spec.Enums[member.Key] = enums.GetStringList(member.Key) ?? new List<string>();
                }
            }
            JsonObject advice = root.GetObject("propertyAdvice");
            if (advice != null)
            {
                foreach (KeyValuePair<string, JsonValue> member in advice.Members) spec.PropertyAdvice[member.Key] = member.Value.AsString() ?? "";
            }
            JsonArray prefixes = root.GetArray("prefixAdvice");
            if (prefixes != null)
            {
                foreach (JsonValue item in prefixes.Items)
                {
                    JsonObject o = item.AsObject();
                    if (o != null) spec.PrefixAdvice.Add(new KeyValuePair<string, string>(o.GetString("prefix", ""), o.GetString("text", "")));
                }
            }
            spec.DefaultAdvice = root.GetString("defaultAdvice", "");
            return spec;
        }

        public string AdviceFor(string property)
        {
            string text;
            if (property != null && PropertyAdvice.TryGetValue(property, out text)) return text;
            foreach (KeyValuePair<string, string> prefix in PrefixAdvice)
            {
                if (property != null && property.StartsWith(prefix.Key, StringComparison.Ordinal)) return prefix.Value;
            }
            return DefaultAdvice;
        }

        private static void Fill(HashSet<string> target, JsonObject root, string key)
        {
            IReadOnlyList<string> list = root.GetStringList(key);
            if (list == null) return;
            foreach (string item in list) target.Add(item);
        }
    }

    public sealed class DialectSpec
    {
        private static DialectSpec _embedded;

        public Dictionary<string, string> TagToUxml { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public Dictionary<string, string> InputTypes { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public Dictionary<string, string> TagAdvice { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public Dictionary<string, string> TextAttribute { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public HashSet<string> DataUxml { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> Interactive { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> TextElements { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> InnerTextTags { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> SafeAttributes { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> VoidTags { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> FocusableByDefault { get; } = new HashSet<string>(StringComparer.Ordinal);

        public static DialectSpec Embedded => _embedded ?? (_embedded = Load(EmbeddedSpec.ReadText("dialect.json")));

        public static DialectSpec Load(string json)
        {
            JsonObject root = JsonReader.Parse(json).AsObject();
            if (root == null) throw new FormatException("dialect.json must be an object");
            var spec = new DialectSpec();
            Map(spec.TagToUxml, root.GetObject("tagToUxml"));
            Map(spec.InputTypes, root.GetObject("inputTypes"));
            Map(spec.TagAdvice, root.GetObject("tagAdvice"));
            Map(spec.TextAttribute, root.GetObject("textAttribute"));
            Fill(spec.DataUxml, root, "dataUxml");
            Fill(spec.Interactive, root, "interactive");
            Fill(spec.TextElements, root, "textElements");
            Fill(spec.InnerTextTags, root, "innerTextTags");
            Fill(spec.SafeAttributes, root, "safeAttributes");
            Fill(spec.VoidTags, root, "voidTags");
            Fill(spec.FocusableByDefault, root, "focusableByDefault");
            return spec;
        }

        private static void Map(Dictionary<string, string> target, JsonObject source)
        {
            if (source == null) return;
            foreach (KeyValuePair<string, JsonValue> member in source.Members) target[member.Key] = member.Value.AsString() ?? "";
        }

        private static void Fill(HashSet<string> target, JsonObject root, string key)
        {
            IReadOnlyList<string> list = root.GetStringList(key);
            if (list == null) return;
            foreach (string item in list) target.Add(item);
        }
    }
}
