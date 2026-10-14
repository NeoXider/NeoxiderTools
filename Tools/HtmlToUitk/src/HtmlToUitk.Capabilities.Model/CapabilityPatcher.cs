using System;
using System.Collections.Generic;
using HtmlToUitk.Core.Json;

namespace HtmlToUitk.Capabilities.Model
{
    public sealed class CapabilityPatchOp
    {
        public string Op;
        public string Path;
        public string Key;
        public JsonValue Value;
    }

    public static class CapabilityPatcher
    {
        public static IReadOnlyList<CapabilityPatchOp> ParseOps(JsonValue patch)
        {
            JsonArray array = patch == null ? null : patch.AsArray();
            if (array == null) throw new CapabilityFormatException("capabilityPatch must be an array");
            var ops = new List<CapabilityPatchOp>();
            for (int i = 0; i < array.Count; i++)
            {
                JsonObject o = array[i].AsObject();
                if (o == null) throw new CapabilityFormatException("capabilityPatch[" + i + "] must be an object");
                string op = o.GetString("op");
                if (op != "add" && op != "remove" && op != "replace") throw new CapabilityFormatException("capabilityPatch[" + i + "].op must be add, remove or replace");
                string path = o.GetString("path");
                if (string.IsNullOrEmpty(path)) throw new CapabilityFormatException("capabilityPatch[" + i + "].path is required");
                ops.Add(new CapabilityPatchOp { Op = op, Path = path, Key = o.GetString("key"), Value = o["value"] });
            }
            return ops;
        }

        public static string Apply(string capabilityJson, JsonValue patch)
        {
            return Apply(capabilityJson, ParseOps(patch));
        }

        public static string Apply(string capabilityJson, IReadOnlyList<CapabilityPatchOp> ops)
        {
            JsonValue root = JsonReader.Parse(capabilityJson);
            foreach (CapabilityPatchOp op in ops)
            {
                root = Walk(root, op.Path.Split('.'), 0, op);
            }
            return JsonWriter.Write(root, JsonWriteOptions.Pretty);
        }

        private static JsonValue Walk(JsonValue node, string[] path, int index, CapabilityPatchOp op)
        {
            if (index == path.Length) return Mutate(node, op);
            JsonObject obj = node.AsObject();
            if (obj == null) throw new CapabilityFormatException("path '" + op.Path + "' does not lead through an object");
            string segment = path[index];
            JsonValue child = obj[segment];
            if (child == null)
            {
                if (op.Op != "add") throw new CapabilityFormatException("path '" + op.Path + "' does not exist");
                child = op.Key != null ? (JsonValue)new JsonObject() : new JsonArray();
            }
            return WithMember(obj, segment, Walk(child, path, index + 1, op), true);
        }

        private static JsonValue Mutate(JsonValue container, CapabilityPatchOp op)
        {
            JsonObject obj = container.AsObject();
            if (obj != null)
            {
                if (op.Key == null) throw new CapabilityFormatException("patch on object '" + op.Path + "' needs a key");
                if (op.Op == "remove") return WithoutMember(obj, op.Key);
                if (op.Op == "replace" && !obj.Has(op.Key)) throw new CapabilityFormatException("cannot replace missing key '" + op.Key + "' in '" + op.Path + "'");
                return WithMember(obj, op.Key, op.Value ?? JsonValue.Null, true);
            }
            JsonArray array = container.AsArray();
            if (array == null) throw new CapabilityFormatException("path '" + op.Path + "' is neither an object nor an array");
            var items = new List<JsonValue>(array.Items);
            string wanted = op.Value == null ? null : op.Value.ToString();
            int found = -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].ToString(), wanted, StringComparison.Ordinal)) { found = i; break; }
            }
            if (op.Op == "remove")
            {
                if (found >= 0) items.RemoveAt(found);
            }
            else if (op.Op == "add")
            {
                if (found < 0) items.Add(op.Value ?? JsonValue.Null);
            }
            else
            {
                throw new CapabilityFormatException("replace is not supported on arrays ('" + op.Path + "')");
            }
            return new JsonArray(items);
        }

        private static JsonObject WithMember(JsonObject source, string key, JsonValue value, bool keepOrder)
        {
            var copy = new JsonObject();
            bool replaced = false;
            foreach (KeyValuePair<string, JsonValue> member in source.Members)
            {
                if (member.Key == key)
                {
                    copy.Set(key, value);
                    replaced = true;
                }
                else
                {
                    copy.Set(member.Key, member.Value);
                }
            }
            if (!replaced) copy.Set(key, value);
            return copy;
        }

        private static JsonObject WithoutMember(JsonObject source, string key)
        {
            var copy = new JsonObject();
            foreach (KeyValuePair<string, JsonValue> member in source.Members)
            {
                if (member.Key != key) copy.Set(member.Key, member.Value);
            }
            return copy;
        }
    }
}
