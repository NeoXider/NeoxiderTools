using System;
using System.IO;
using System.Security.Cryptography;
using Neo.HtmlToUitk.Json;

namespace Neo.HtmlToUitk.Tooling
{
    /// <summary>Exact generated PNG ownership. A folder name alone does not confer ownership.</summary>
    public static class GeneratedTextureManifest
    {
        public const string FileName = "generated-assets.json";

        public static bool Owns(string json, string manifestDirectory, string texturePath)
        {
            JsonValue root = MiniJson.Parse(json);
            if (root["schema"].AsInt() != 1 || root["assets"].Kind != JsonKind.Array) return false;
            string directory = Path.GetFullPath(manifestDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string target = Path.GetFullPath(texturePath);
            string prefix = directory + Path.DirectorySeparatorChar;
            if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            JsonValue assets = root["assets"];
            for (int index = 0; index < assets.Count; index++)
            {
                JsonValue asset = assets[index];
                string relative = asset["path"].AsString();
                if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) continue;
                string absolute = Path.GetFullPath(Path.Combine(directory, relative));
                if (!absolute.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !string.Equals(absolute, target, StringComparison.OrdinalIgnoreCase)) continue;
                string expected = asset["sha256"].AsString();
                if (string.IsNullOrEmpty(expected) || expected.Length != 64 || !File.Exists(target) || !target.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return false;
                if (asset["width"].AsInt() <= 0 || asset["height"].AsInt() <= 0 || asset["kind"].AsString() != "gradient") return false;
                if (asset["sRGB"].Kind != JsonKind.Bool || !asset["sRGB"].AsBool() || asset["wrap"].AsString() != "clamp" || asset["mipmaps"].Kind != JsonKind.Bool || asset["mipmaps"].AsBool()) return false;
                using (FileStream stream = File.OpenRead(target))
                {
                    byte[] signature = new byte[8];
                    if (stream.Length > 64 * 1024 * 1024 || stream.Read(signature, 0, signature.Length) != signature.Length) return false;
                    byte[] png = { 137, 80, 78, 71, 13, 10, 26, 10 };
                    for (int byteIndex = 0; byteIndex < png.Length; byteIndex++) if (signature[byteIndex] != png[byteIndex]) return false;
                    stream.Position = 0;
                    using (SHA256 algorithm = SHA256.Create())
                    {
                        string actual = BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
                        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            return false;
        }
    }
}
