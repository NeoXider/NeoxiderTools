using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HtmlToUitk.Core.Json;

namespace HtmlToUitk.Convert.Spec
{
    internal static class SpecData
    {
        private static JsonObject _dialect;
        private static JsonObject _uss;
        private static readonly object Gate = new object();

        public static JsonObject DialectJson
        {
            get
            {
                lock (Gate)
                {
                    return _dialect ?? (_dialect = Load("spec.dialect.json"));
                }
            }
        }

        public static JsonObject UssJson
        {
            get
            {
                lock (Gate)
                {
                    return _uss ?? (_uss = Load("spec.uss.json"));
                }
            }
        }

        private static JsonObject Load(string resource)
        {
            Assembly assembly = typeof(SpecData).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(resource))
            {
                if (stream == null) throw new InvalidOperationException("embedded spec resource missing: " + resource);
                using (var reader = new StreamReader(stream, new System.Text.UTF8Encoding(false)))
                {
                    return (JsonObject)JsonReader.Parse(reader.ReadToEnd());
                }
            }
        }

        public static HashSet<string> Set(JsonObject root, string key)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            JsonArray array = root.GetArray(key);
            if (array != null)
            {
                for (int i = 0; i < array.Count; i++) set.Add(array[i].AsString());
            }
            return set;
        }

        public static Dictionary<string, string> Map(JsonObject root, string key)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            JsonObject obj = root.GetObject(key);
            if (obj != null)
            {
                foreach (KeyValuePair<string, JsonValue> member in obj.Members) map[member.Key] = member.Value.AsString();
            }
            return map;
        }
    }

    internal sealed class DialectSpec
    {
        public static readonly DialectSpec Instance = new DialectSpec();

        public readonly Dictionary<string, string> TagToUxml;
        public readonly Dictionary<string, string> InputTypes;
        public readonly Dictionary<string, string> TagAdvice;
        public readonly HashSet<string> DataUxml;
        public readonly Dictionary<string, string> TextAttribute;
        public readonly HashSet<string> Interactive;
        public readonly HashSet<string> TextElements;
        public readonly HashSet<string> InnerTextTags;
        public readonly HashSet<string> SafeAttributes;
        public readonly HashSet<string> VoidTags;
        public readonly HashSet<string> FocusableByDefault;

        private DialectSpec()
        {
            JsonObject root = SpecData.DialectJson;
            TagToUxml = SpecData.Map(root, "tagToUxml");
            InputTypes = SpecData.Map(root, "inputTypes");
            TagAdvice = SpecData.Map(root, "tagAdvice");
            DataUxml = SpecData.Set(root, "dataUxml");
            TextAttribute = SpecData.Map(root, "textAttribute");
            Interactive = SpecData.Set(root, "interactive");
            TextElements = SpecData.Set(root, "textElements");
            InnerTextTags = SpecData.Set(root, "innerTextTags");
            SafeAttributes = SpecData.Set(root, "safeAttributes");
            VoidTags = SpecData.Set(root, "voidTags");
            FocusableByDefault = SpecData.Set(root, "focusableByDefault");
        }
    }

    internal sealed class UssSpec
    {
        public static readonly UssSpec Instance = new UssSpec();

        public readonly HashSet<string> Standard;
        public readonly HashSet<string> Unity;
        public readonly HashSet<string> AnimatableFull;
        public readonly HashSet<string> LayoutProperties;
        public readonly HashSet<string> Inherited;
        public readonly HashSet<string> Easings;
        public readonly HashSet<string> BrowserEasings;
        public readonly HashSet<string> LengthProperties;
        public readonly HashSet<string> NumberProperties;
        public readonly HashSet<string> ColorProperties;
        public readonly Dictionary<string, HashSet<string>> Enums;

        public static bool IsEasing(string name)
        {
            return Instance.Easings.Contains(name);
        }

        private UssSpec()
        {
            JsonObject root = SpecData.UssJson;
            Standard = SpecData.Set(root, "standard");
            Unity = SpecData.Set(root, "unity");
            AnimatableFull = SpecData.Set(root, "animatableFull");
            LayoutProperties = SpecData.Set(root, "layoutProperties");
            Inherited = SpecData.Set(root, "inherited");
            Easings = SpecData.Set(root, "easings");
            BrowserEasings = SpecData.Set(root, "browserEasings");
            LengthProperties = SpecData.Set(root, "lengthProperties");
            NumberProperties = SpecData.Set(root, "numberProperties");
            ColorProperties = SpecData.Set(root, "colorProperties");
            Enums = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            JsonObject enums = root.GetObject("enums");
            if (enums != null)
            {
                foreach (KeyValuePair<string, JsonValue> member in enums.Members)
                {
                    var values = new HashSet<string>(StringComparer.Ordinal);
                    JsonArray array = member.Value.AsArray();
                    for (int i = 0; i < array.Count; i++) values.Add(array[i].AsString());
                    Enums[member.Key] = values;
                }
            }
        }
    }
}
