using System;
using System.Collections.Generic;
using Neo.HtmlToUitk.Json;

namespace Neo.HtmlToUitk.Tooling
{
    public sealed class JobDocument
    {
        private static readonly string[] FolderKeys = { "uxml", "uss", "shared", "tokens", "strings", "generated", "components" };

        private readonly List<KeyValuePair<string, JsonValue>> _unknownTop = new List<KeyValuePair<string, JsonValue>>();
        private readonly List<KeyValuePair<string, JsonValue>> _unknownInputs = new List<KeyValuePair<string, JsonValue>>();
        private readonly List<KeyValuePair<string, JsonValue>> _unknownOutput = new List<KeyValuePair<string, JsonValue>>();
        private readonly List<KeyValuePair<string, JsonValue>> _unknownWatch = new List<KeyValuePair<string, JsonValue>>();

        public string SchemaUri;
        public string Name = string.Empty;
        public string Config = string.Empty;
        public JsonValue ConfigOverrides;
        public List<string> Html = new List<string>();
        public List<string> Css = new List<string>();
        public string CssOrder = string.Empty;
        public List<string> Uss = new List<string>();
        public List<string> Partials = new List<string>();
        public string OutputRoot = string.Empty;
        public string Assemble = string.Empty;
        public string PartialsMode = string.Empty;
        public List<KeyValuePair<string, string>> Folders = new List<KeyValuePair<string, string>>();
        public JsonValue PerFile;
        public bool WatchEnabled;
        public int DebounceMs = 400;

        public static IReadOnlyList<string> KnownFolderKeys => FolderKeys;

        public static JobDocument Parse(string json)
        {
            JsonValue root = MiniJson.Parse(json);
            JobDocument doc = new JobDocument();
            for (int i = 0; i < root.Members.Count; i++)
            {
                string key = root.Members[i].Key;
                JsonValue value = root.Members[i].Value;
                switch (key)
                {
                    case "$schema": doc.SchemaUri = value.AsString(); break;
                    case "schema": break;
                    case "name": doc.Name = value.AsString(string.Empty); break;
                    case "config": doc.Config = value.AsString(string.Empty); break;
                    case "configOverrides": doc.ConfigOverrides = value; break;
                    case "inputs": doc.ReadInputs(value); break;
                    case "output": doc.ReadOutput(value); break;
                    case "watch": doc.ReadWatch(value); break;
                    default: doc._unknownTop.Add(root.Members[i]); break;
                }
            }

            return doc;
        }

        public string ToJson()
        {
            return MiniJson.Write(ToValue(), true);
        }

        public JsonValue ToValue()
        {
            JsonValue root = JsonValue.NewObject();
            if (!string.IsNullOrEmpty(SchemaUri))
                root.Set("$schema", JsonValue.FromString(SchemaUri));
            root.Set("schema", JsonValue.FromNumber(1));
            if (!string.IsNullOrEmpty(Name))
                root.Set("name", JsonValue.FromString(Name));
            if (!string.IsNullOrEmpty(Config))
                root.Set("config", JsonValue.FromString(Config));
            if (ConfigOverrides != null && ConfigOverrides.Kind == JsonKind.Object && ConfigOverrides.Count > 0)
                root.Set("configOverrides", ConfigOverrides);

            JsonValue inputs = JsonValue.NewObject();
            inputs.Set("html", JsonValue.FromStrings(Html));
            if (Css.Count > 0)
                inputs.Set("css", JsonValue.FromStrings(Css));
            if (!string.IsNullOrEmpty(CssOrder))
                inputs.Set("cssOrder", JsonValue.FromString(CssOrder));
            if (Uss.Count > 0)
                inputs.Set("uss", JsonValue.FromStrings(Uss));
            if (Partials.Count > 0)
                inputs.Set("partials", JsonValue.FromStrings(Partials));
            AppendUnknown(inputs, _unknownInputs);
            root.Set("inputs", inputs);

            JsonValue output = JsonValue.NewObject();
            output.Set("root", JsonValue.FromString(OutputRoot ?? string.Empty));
            if (!string.IsNullOrEmpty(Assemble))
                output.Set("assemble", JsonValue.FromString(Assemble));
            JsonValue folders = JsonValue.NewObject();
            for (int i = 0; i < Folders.Count; i++)
            {
                if (!string.IsNullOrEmpty(Folders[i].Value))
                    folders.Set(Folders[i].Key, JsonValue.FromString(Folders[i].Value));
            }

            if (folders.Count > 0)
                output.Set("folders", folders);
            if (PerFile != null && PerFile.Kind == JsonKind.Object && PerFile.Count > 0)
                output.Set("perFile", PerFile);
            if (!string.IsNullOrEmpty(PartialsMode))
                output.Set("partials", JsonValue.FromString(PartialsMode));
            AppendUnknown(output, _unknownOutput);
            root.Set("output", output);

            JsonValue watch = JsonValue.NewObject();
            watch.Set("enabled", JsonValue.FromBool(WatchEnabled));
            watch.Set("debounceMs", JsonValue.FromNumber(DebounceMs));
            AppendUnknown(watch, _unknownWatch);
            root.Set("watch", watch);

            AppendUnknown(root, _unknownTop);
            return root;
        }

        public IReadOnlyList<string> ValidateShape()
        {
            List<string> problems = new List<string>();
            if (Html.Count == 0)
                problems.Add("inputs.html is empty");
            if (string.IsNullOrWhiteSpace(OutputRoot))
                problems.Add("output.root is empty");
            if (DebounceMs < 0)
                problems.Add("watch.debounceMs is negative");
            return problems;
        }

        private void ReadInputs(JsonValue value)
        {
            for (int i = 0; i < value.Members.Count; i++)
            {
                string key = value.Members[i].Key;
                JsonValue v = value.Members[i].Value;
                switch (key)
                {
                    case "html": Html = v.ToStringList(); break;
                    case "css": Css = v.ToStringList(); break;
                    case "cssOrder": CssOrder = v.AsString(string.Empty); break;
                    case "uss": Uss = v.ToStringList(); break;
                    case "partials": Partials = v.ToStringList(); break;
                    default: _unknownInputs.Add(value.Members[i]); break;
                }
            }
        }

        private void ReadOutput(JsonValue value)
        {
            for (int i = 0; i < value.Members.Count; i++)
            {
                string key = value.Members[i].Key;
                JsonValue v = value.Members[i].Value;
                switch (key)
                {
                    case "root": OutputRoot = v.AsString(string.Empty); break;
                    case "assemble": Assemble = v.AsString(string.Empty); break;
                    case "partials": PartialsMode = v.AsString(string.Empty); break;
                    case "perFile": PerFile = v; break;
                    case "folders":
                        Folders = new List<KeyValuePair<string, string>>();
                        for (int f = 0; f < v.Members.Count; f++)
                            Folders.Add(new KeyValuePair<string, string>(v.Members[f].Key, v.Members[f].Value.AsString(string.Empty)));
                        break;
                    default: _unknownOutput.Add(value.Members[i]); break;
                }
            }
        }

        private void ReadWatch(JsonValue value)
        {
            for (int i = 0; i < value.Members.Count; i++)
            {
                string key = value.Members[i].Key;
                JsonValue v = value.Members[i].Value;
                switch (key)
                {
                    case "enabled": WatchEnabled = v.AsBool(); break;
                    case "debounceMs": DebounceMs = v.AsInt(400); break;
                    default: _unknownWatch.Add(value.Members[i]); break;
                }
            }
        }

        private static void AppendUnknown(JsonValue target, List<KeyValuePair<string, JsonValue>> unknown)
        {
            for (int i = 0; i < unknown.Count; i++)
                target.Set(unknown[i].Key, unknown[i].Value);
        }
    }
}
