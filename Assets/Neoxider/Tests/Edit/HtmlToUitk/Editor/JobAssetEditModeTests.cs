using NUnit.Framework;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using Neo.HtmlToUitk.Json;
using Neo.HtmlToUitk.Tooling;

namespace Neo.HtmlToUitk.Tests
{
    public class JobAssetEditModeTests
    {
        private const string Sample = @"{
  ""schema"": 1, ""name"": ""demo"", ""config"": ""htmltouitk.config.json"",
  ""configOverrides"": { ""rules"": { ""minFontSize"": 14 } },
  ""inputs"": { ""html"": [""pages/*.html""], ""uss"": [""skin/Skin.uss""] },
  ""output"": { ""root"": ""out"", ""folders"": { ""uxml"": ""Screens"" } },
  ""watch"": { ""enabled"": true, ""debounceMs"": 250 },
  ""x-extension"": 1
}";

        [Test]
        public void JobAssetRoundTripsTheJsonIncludingUnknownKeys()
        {
            HtmlToUitkJob job = ScriptableObject.CreateInstance<HtmlToUitkJob>();
            try
            {
                job.FromJson(Sample);
                Assert.That(job.html, Is.EqualTo(new[] { "pages/*.html" }));
                Assert.That(job.watchEnabled, Is.True);
                Assert.That(job.debounceMs, Is.EqualTo(250));
                job.html.Add("extra/about.html");
                JsonValue root = MiniJson.Parse(job.ToJson());
                Assert.That(root["inputs"]["html"].Count, Is.EqualTo(2));
                Assert.That(root["x-extension"].AsInt(), Is.EqualTo(1));
                Assert.That(root["configOverrides"]["rules"]["minFontSize"].AsInt(), Is.EqualTo(14));
            }
            finally
            {
                Object.DestroyImmediate(job);
            }
        }

        [TestCase("Assets/UI/Generated/shadow.png", false)]
        [TestCase("Assets/UI/Generated/sub/gradient.png", false)]
        [TestCase("Assets/UI/Screens/Home.uxml", false)]
        [TestCase("Assets/Art/hero.png", false)]
        [TestCase("Assets/UI/Generated/readme.txt", false)]
        public void UnownedGeneratedFolderDoesNotChangeTextureImport(string path, bool expected)
        {
            Assert.That(GeneratedAssetPostprocessor.IsGeneratedTexturePath(path, null), Is.EqualTo(expected));
        }

        [Test]
        public void ConfigMatchesCustomFolderNames()
        {
            HtmlToUitkConfig config = ScriptableObject.CreateInstance<HtmlToUitkConfig>();
            try
            {
                config.generatedFolderNames = new System.Collections.Generic.List<string> { "Baked" };
                Assert.That(config.MatchesGeneratedPath("Assets/UI/Baked/a.png"), Is.True);
                Assert.That(GeneratedAssetPostprocessor.IsGeneratedTexturePath("Assets/UI/Baked/a.png", config), Is.False, "Folder policy cannot prove ownership without a manifest.");
                Assert.That(GeneratedAssetPostprocessor.IsGeneratedTexturePath("Assets/UI/Generated/a.png", config), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ConfigRequiresExactManifestAndCanOnlyNarrowGeneratedTextureOwnership()
        {
            string directory = Path.Combine(HtmlToUitkPaths.ProjectRoot, "Temp", "htu-owned-png-" + System.Guid.NewGuid().ToString("N"));
            string generated = Path.Combine(directory, "Generated");
            string image = Path.Combine(generated, "gradient.png");
            string unrelated = Path.Combine(generated, "photo.png");
            string manifest = Path.Combine(directory, GeneratedTextureManifest.FileName);
            Directory.CreateDirectory(generated);
            byte[] png = System.Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aWQAAAABJRU5ErkJggg==");
            File.WriteAllBytes(image, png); File.WriteAllBytes(unrelated, png);
            HtmlToUitkConfig config = ScriptableObject.CreateInstance<HtmlToUitkConfig>();
            try
            {
                string hash;
                using (SHA256 algorithm = SHA256.Create()) hash = System.BitConverter.ToString(algorithm.ComputeHash(png)).Replace("-", string.Empty);
                string json = "{\"schema\":1,\"assets\":[{\"path\":\"Generated/gradient.png\",\"sha256\":\"" + hash + "\",\"width\":1,\"height\":1,\"kind\":\"gradient\",\"sRGB\":true,\"wrap\":\"clamp\",\"mipmaps\":false}]}";
                Assert.That(GeneratedAssetPostprocessor.IsGeneratedTexturePath(image, config), Is.False, "Default config cannot claim an existing Generated PNG without a manifest.");
                File.WriteAllText(manifest, json);
                Assert.That(GeneratedAssetPostprocessor.IsGeneratedTexturePath(image, config), Is.True);
                Assert.That(GeneratedAssetPostprocessor.IsGeneratedTexturePath(image, null), Is.True);
                Assert.That(GeneratedAssetPostprocessor.IsGeneratedTexturePath(unrelated, config), Is.False, "A matching folder and identical bytes do not replace the exact manifest path.");
                File.WriteAllText(manifest, json.Replace(hash, new string('0', 64)));
                Assert.That(GeneratedAssetPostprocessor.IsGeneratedTexturePath(image, config), Is.False);
                File.WriteAllText(manifest, json.Replace("Generated/gradient.png", "../Generated/gradient.png"));
                Assert.That(GeneratedAssetPostprocessor.IsGeneratedTexturePath(image, config), Is.False);
                File.WriteAllText(manifest, json);
                config.generatedFolderNames = new System.Collections.Generic.List<string> { "Baked" };
                Assert.That(GeneratedAssetPostprocessor.IsGeneratedTexturePath(image, config), Is.False, "Config narrows proven ownership.");
                config.generatedFolderNames = new System.Collections.Generic.List<string> { "Generated" };
                config.manageTextureImport = false;
                Assert.That(GeneratedAssetPostprocessor.IsGeneratedTexturePath(image, config), Is.False);
                Assert.That(File.ReadAllBytes(unrelated), Is.EqualTo(png), "Ownership resolution must not mutate unrelated content.");
            }
            finally
            {
                Object.DestroyImmediate(config);
                if (File.Exists(manifest)) File.Delete(manifest);
                File.Delete(image); File.Delete(unrelated);
                Directory.Delete(generated); Directory.Delete(directory);
            }
        }
    }
}
