using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using HtmlToUitk.Bake;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Emulations;
using HtmlToUitk.Convert.Jobs;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Json;
using NUnit.Framework;

namespace HtmlToUitk.Convert.Tests
{
    public sealed class OutputCollisionTests
    {
        [Test]
        public void PageSuffixesReserveTheActualOutputNamesWithoutLosingAnyInput()
        {
            using JobFolder job = new JobFolder();
            job.Write("a/home.html", Page("home", "first-marker"));
            job.Write("b/home.html", Page("home", "second-marker"));
            job.Write("c/home-2.html", Page("home-2", "third-marker"));
            job.Write("job.json", "{\"schema\":1,\"inputs\":{\"html\":[\"a/home.html\",\"b/home.html\",\"c/home-2.html\"]},\"output\":{\"root\":\"out\"}}");

            Assert.That(job.Run(out string diagnostic), Is.Zero, diagnostic);

            string[] pages = Directory.GetFiles(job.Output, "*.uxml", SearchOption.AllDirectories);
            Assert.That(pages, Has.Length.EqualTo(3));
            Assert.That(pages.Select(Path.GetFileName), Is.EquivalentTo(new[] { "home.uxml", "home-2.uxml", "home-2-2.uxml" }));
            foreach (string marker in new[] { "first-marker", "second-marker", "third-marker" })
                Assert.That(pages.Count(path => File.ReadAllText(path).Contains("name=\"" + marker + "\"")), Is.EqualTo(1));
            Dictionary<string, string> before = pages.ToDictionary(Path.GetFileName, File.ReadAllText);
            Assert.That(job.Run(out diagnostic), Is.Zero, diagnostic);
            Assert.That(Directory.GetFiles(job.Output, "*.uxml", SearchOption.AllDirectories)
                .ToDictionary(Path.GetFileName, File.ReadAllText), Is.EquivalentTo(before));
        }

        [Test]
        public void DifferentSharedCssWithOneStemBothEmitAndRemainLinked()
        {
            using JobFolder job = new JobFolder();
            job.Write("page.html", "<html><head><meta name=\"uitk:screen\" content=\"page\"></head><body>" +
                                   "<div name=\"first-marker\" class=\"first-marker\"></div>" +
                                   "<div name=\"second-marker\" class=\"second-marker\"></div></body></html>");
            job.Write("styles/a/common.css", ".first-marker { opacity: 0.2; }");
            job.Write("styles/b/common.css", ".second-marker { opacity: 0.8; }");
            job.Write("job.json", "{\"schema\":1,\"inputs\":{\"html\":[\"page.html\"],\"css\":[\"styles/a/common.css\",\"styles/b/common.css\"]},\"output\":{\"root\":\"out\"}}");

            Assert.That(job.Run(out string diagnostic), Is.Zero, diagnostic);

            string page = Directory.GetFiles(job.Output, "*.uxml", SearchOption.AllDirectories).Single();
            string[] linked = XDocument.Load(page).Descendants().Where(element => element.Name.LocalName == "Style")
                .Select(element => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(page), element.Attribute("src").Value)))
                .ToArray();
            string[] shared = linked.Where(path => Path.GetFileName(path).StartsWith("common", StringComparison.Ordinal)).ToArray();
            Assert.That(shared, Has.Length.EqualTo(2));
            Assert.That(shared.Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(2));
            Assert.That(shared.All(File.Exists), Is.True);
            Assert.That(shared.Count(path => File.ReadAllText(path).Contains(".first-marker")), Is.EqualTo(1));
            Assert.That(shared.Count(path => File.ReadAllText(path).Contains(".second-marker")), Is.EqualTo(1));
        }

        [TestCase("Generated/a.png")]
        [TestCase("Generated\\a.png")]
        [TestCase("./generated/A.png")]
        public void AssetAliasesAreIdempotentOnlyForIdenticalContent(string alias)
        {
            AssetSink sink = new AssetSink();
            Assert.That(sink.Add(new GeneratedAsset { RelativePath = "Generated/a.png", Content = new byte[] { 1 } }), Is.EqualTo("Generated/a.png"));
            Assert.That(sink.Add(new GeneratedAsset { RelativePath = alias, Content = new byte[] { 1 } }), Is.EqualTo("Generated/a.png"));
            Assert.That(sink.Assets, Has.Count.EqualTo(1));
            Assert.Throws<FormatException>(() => sink.Add(new GeneratedAsset { RelativePath = alias, Content = new byte[] { 2 } }));
            Assert.That(sink.Assets[0].Content, Is.EqualTo(new byte[] { 1 }));
        }

        [TestCase("generated-assets.json")]
        [TestCase("../outside.png")]
        [TestCase("/outside.png")]
        public void AssetPathsCannotReplaceManifestsOrEscapeTheirOutput(string path)
        {
            AssetSink sink = new AssetSink();
            Assert.Throws<FormatException>(() => sink.Add(new GeneratedAsset { RelativePath = path, Content = new byte[] { 1 } }));
            Assert.That(sink.Assets, Is.Empty);
        }

        [Test]
        public void CanonicalAssetAliasesHaveOneMatchingManifestEntryAndFile()
        {
            byte[] png = PngEncoder.Encode(new RgbaImage(1, 1));
            OutputSet output = Emit(new TransformedModel
            {
                Assets = new[]
                {
                    new GeneratedAsset { RelativePath = "Generated/a.png", Content = png },
                    new GeneratedAsset { RelativePath = "Generated\\./A.png", Content = (byte[])png.Clone() }
                }
            });
            Assert.That(output.Files.Count(file => file.Kind == FileKind.Png), Is.EqualTo(1));
            OutputFile asset = output.Files.Single(file => file.Kind == FileKind.Png);
            JsonObject manifest = (JsonObject)JsonReader.Parse(Encoding.UTF8.GetString(output.Files.Single(file => file.RelativePath == "generated-assets.json").Content));
            JsonArray entries = manifest.GetArray("assets");
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(((JsonObject)entries[0])["path"].AsString(), Is.EqualTo(asset.RelativePath));
        }

        [TestCase("diagnostics.json")]
        [TestCase("./GENERATED-ASSETS.json")]
        [TestCase("Generated/a.png")]
        public void EmitRejectsReservedArtifactsAndCrossKindCollisions(string path)
        {
            TransformedModel model = new TransformedModel
            {
                Sheets = new SheetSet { Sheets = new[] { new SheetEntry { RelativePath = path, Role = SheetRole.Page,
                    Sheet = new Stylesheet { Path = path, Rules = Array.Empty<StyleRule>() } } } },
                Assets = new[] { new GeneratedAsset { RelativePath = "Generated/a.png", Content = PngEncoder.Encode(new RgbaImage(1, 1)) } }
            };
            Assert.Throws<FormatException>(() => Emit(model));
        }

        private static OutputSet Emit(TransformedModel model)
        {
            return new EmitStage(new PipelineState { Services = new ConvertServices() })
                .Run(model, new StageContext { Config = new ConversionConfig() });
        }

        private static string Page(string screen, string marker)
        {
            return "<html><head><meta name=\"uitk:screen\" content=\"" + screen + "\"></head><body><div name=\"" + marker + "\"></div></body></html>";
        }

        private sealed class JobFolder : IDisposable
        {
            private readonly string _root = Path.Combine(Path.GetTempPath(), "htu-collision-" + Guid.NewGuid().ToString("N"));
            internal string Output => Path.Combine(_root, "out");
            internal void Write(string path, string text)
            {
                string target = Path.Combine(_root, path);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.WriteAllText(target, text);
            }
            internal int Run(out string diagnostic)
            {
                using StringWriter stdout = new StringWriter();
                using StringWriter stderr = new StringWriter();
                int code = JobEntry.Run(new[] { "run", Path.Combine(_root, "job.json") }, stdout, stderr);
                diagnostic = stdout.ToString() + stderr.ToString();
                return code;
            }
            public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
        }
    }
}
