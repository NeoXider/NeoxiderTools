using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlToUitk.Bake;
using HtmlToUitk.Cli;
using NUnit.Framework;

namespace HtmlToUitk.Cli.Tests
{
    public sealed class ProductionPipelineTests
    {
        private static string ToolRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "HtmlToUitk.sln"))) directory = directory.Parent;
            Assert.That(directory, Is.Not.Null);
            return directory.FullName;
        }

        private static string CopyFixture()
        {
            string source = Path.Combine(ToolRoot(), "fixtures", "production-neutral");
            string target = Path.Combine(ToolRoot(), ".qa", Guid.NewGuid().ToString("N"));
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(source, file);
                if (relative.StartsWith("out" + Path.DirectorySeparatorChar, StringComparison.Ordinal)) continue;
                string destination = Path.Combine(target, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination);
            }
            return target;
        }

        private static int Run(string job, out string diagnostic)
        {
            using StringWriter stdout = new StringWriter();
            using StringWriter stderr = new StringWriter();
            int code = CliApp.Run(new[] { "run", Path.Combine(job, "neutral.htujob.json") }, stdout, stderr);
            diagnostic = stdout.ToString() + stderr.ToString();
            return code;
        }

        private static Dictionary<string, string> Hashes(string folder)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                result[Path.GetRelativePath(folder, file)] = System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
            return result;
        }

        private static RgbaImage ImageFor(string output, string uss, string selector)
        {
            string pattern = Regex.Escape(selector) + @"(?:\.[A-Za-z0-9_-]+)*\s*\{[^}]*background-image:\s*url\(""([^""]+)""\)";
            Match match = Regex.Match(uss, pattern, RegexOptions.Singleline);
            Assert.That(match.Success, Is.True, selector);
            return PngDecoder.Decode(File.ReadAllBytes(Path.GetFullPath(Path.Combine(output, "Screens", match.Groups[1].Value))));
        }

        [Test]
        public void ActualCliEmitsAllContractsAndStableReferencedGradientPixels()
        {
            string fixture = CopyFixture();
            Assert.That(Run(fixture, out string diagnostic), Is.EqualTo(0), diagnostic);
            string output = Path.Combine(fixture, "out");
            foreach (string suffix in new[] { "uxml", "uss", "nav.json", "mq.json", "anim.json", "runtime.json" })
                Assert.That(File.Exists(Path.Combine(output, "Screens", "Neutral." + suffix)), Is.True, suffix);
            Assert.That(File.Exists(Path.Combine(output, "Screens", "Profile.uxml")), Is.True);
            string uxml = File.ReadAllText(Path.Combine(output, "Screens", "Neutral.uxml"));
            Assert.That(uxml, Does.Contain("Button name=\"app-home-settings\""));
            Assert.That(uxml, Does.Contain("@loc:app.home.title").Or.Contain("app.home.title"));
            Assert.That(File.Exists(Path.Combine(output, "Localization", "Neutral.newkeys.json")), Is.True);
            string uss = File.ReadAllText(Path.Combine(output, "Screens", "Neutral.uss"));
            MatchCollection references = Regex.Matches(uss, "url\\(\"([^\"]+\\.png)\"\\)");
            Assert.That(references.Count, Is.EqualTo(7));
            List<RgbaImage> gradients = new List<RgbaImage>();
            foreach (Match reference in references)
            {
                string png = Path.GetFullPath(Path.Combine(output, "Screens", reference.Groups[1].Value));
                Assert.That(png.StartsWith(Path.GetFullPath(output) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
                Assert.That(File.Exists(png), Is.True);
                gradients.Add(PngDecoder.Decode(File.ReadAllBytes(png)));
            }
            RgbaImage linear = ImageFor(output, uss, ".app__linear");
            Assert.That(linear.Height, Is.EqualTo(64));
            Rgba8 left = linear.Get(0, 32);
            Rgba8 right = linear.Get(127, 32);
            Assert.That(left.R, Is.GreaterThan(250));
            Assert.That(left.B, Is.LessThan(5));
            Assert.That(left.A, Is.GreaterThan(250));
            Assert.That(right.B, Is.GreaterThan(250));
            Assert.That(right.A, Is.InRange(126, 130));
            RgbaImage radial = ImageFor(output, uss, ".app__radial");
            Assert.That(radial.Get(32, 32).R, Is.GreaterThan(245));
            Assert.That(radial.Get(0, 0).R, Is.LessThan(10));
            RgbaImage conic = ImageFor(output, uss, ".app__conic");
            Assert.That(conic.Pixels.Distinct().Count(), Is.GreaterThan(100));
            RgbaImage wide = ImageFor(output, uss, ".app__diagonal-wide");
            RgbaImage tall = ImageFor(output, uss, ".app__diagonal-tall");
            // CSS 45 degree projection at the top-left differs by aspect: t~1/3 versus t~2/3.
            Assert.That(wide.Get(0, 0).R, Is.InRange(168, 172));
            Assert.That(tall.Get(0, 0).R, Is.InRange(83, 87));
            RgbaImage repeating = ImageFor(output, uss, ".app__repeat");
            Assert.That(repeating.Get(2, 32).R, Is.EqualTo(255));
            Assert.That(repeating.Get(10, 32).B, Is.EqualTo(255));
            Assert.That(repeating.Get(2, 32), Is.EqualTo(repeating.Get(18, 32)));
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "generated-assets.json")));
            foreach (JsonElement asset in manifest.RootElement.GetProperty("assets").EnumerateArray())
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(output, asset.GetProperty("path").GetString()));
                Assert.That(System.Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), Is.EqualTo(asset.GetProperty("sha256").GetString()));
                Assert.That(asset.GetProperty("wrap").GetString(), Is.EqualTo("clamp"));
                Assert.That(asset.GetProperty("mipmaps").GetBoolean(), Is.False);
            }
            using JsonDocument mq = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "Screens", "Neutral.mq.json")));
            JsonElement media = mq.RootElement.GetProperty("classes")[0];
            Assert.That(media.GetProperty("selector").GetString(), Is.EqualTo(".app__tabs"));
            JsonElement query = media.GetProperty("query");
            Assert.That(query.GetProperty("minWidth").GetDouble(), Is.EqualTo(1200));
            Assert.That(query.GetProperty("minHeight").GetDouble(), Is.EqualTo(600));
            Assert.That(query.GetProperty("minAspect").GetDouble(), Is.EqualTo(16.0 / 9));
            Assert.That(uss, Does.Contain(".app__tabs." + media.GetProperty("class").GetString()));
            using JsonDocument nav = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "Screens", "Neutral.nav.json")));
            Assert.That(nav.RootElement.GetProperty("pages").GetArrayLength(), Is.EqualTo(2));
            Assert.That(nav.RootElement.GetProperty("modals").GetArrayLength(), Is.EqualTo(1));
            Assert.That(nav.RootElement.GetProperty("tabs").GetArrayLength(), Is.EqualTo(1));
            using JsonDocument animation = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "Screens", "Neutral.anim.json")));
            Assert.That(animation.RootElement.GetProperty("animations").GetArrayLength(), Is.EqualTo(1));
            Assert.That(animation.RootElement.GetProperty("bindings")[0].GetProperty("reducedMotion").GetString(), Is.EqualTo("skip"));
            Dictionary<string, string> before = Hashes(output);
            Assert.That(Run(fixture, out diagnostic), Is.EqualTo(0), diagnostic);
            Assert.That(Hashes(output), Is.EquivalentTo(before));
        }

        [TestCase("preview")]
        [TestCase("compare")]
        public void UnsupportedCommandsNeverClaimSuccess(string command)
        {
            using StringWriter error = new StringWriter();
            Assert.That(CliApp.Run(new[] { command }, TextWriter.Null, error), Is.Not.EqualTo(0));
            Assert.That(error.ToString(), Does.Contain("unsupported"));
        }

        [TestCase("@media print { .app__tabs { opacity: 0.5; } }", "cannot be mapped")]
        [TestCase(".app__linear { background-image: linear-gradient(in oklab, red, blue); }", "unsupported")]
        [TestCase(".app__linear { width: 100%; background-image: linear-gradient(45deg, red, blue); }", "dynamic gradient")]
        [TestCase(".app__radial { width: 100%; background-image: radial-gradient(circle, red, blue); }", "dynamic gradient")]
        [TestCase("@media (max-width: 900px) { .app__radial { width: 100px; } }", "dynamic gradient")]
        [TestCase(".app__linear { width: 50000px; background-image: linear-gradient(red, blue); }", "budget")]
        [TestCase(".app__linear { box-shadow: inset 0 2px 4px red; }", "shadow")]
        public void UnsupportedOrUnregisteredFeaturesFailWithConcreteDiagnostic(string css, string expected)
        {
            string fixture = CopyFixture();
            string page = Path.Combine(fixture, "input", "neutral.html");
            File.WriteAllText(page, File.ReadAllText(page).Replace("</style>", css + "</style>"));
            Assert.That(Run(fixture, out string diagnostic), Is.Not.EqualTo(0));
            Assert.That(diagnostic.ToLowerInvariant(), Does.Contain(expected));
            Assert.That(File.Exists(Path.Combine(fixture, "out", "Screens", "Neutral.uxml")), Is.False);
        }

        [TestCase(".card { width:100px; height:100px; background-image:linear-gradient(45deg,red,blue); } .wide { width:200px; }", "heterogeneous")]
        [TestCase(".card { width:50%; height:100px; background-image:linear-gradient(90deg,red 0px,blue 100px); }", "absolute stops")]
        [TestCase(".card { width:100px; height:100px; } @media(min-width:1200px) and (max-width:800px) { .card { opacity:0.5; } }", "contradictory media")]
        [TestCase(".card { width:100px; height:100px; } @media(min-height:900px) and (max-height:600px) { .card { opacity:0.5; } }", "contradictory media")]
        [TestCase(".card { width:100px; height:100px; } @media(min-aspect-ratio:2/1) and (max-aspect-ratio:1/1) { .card { opacity:0.5; } }", "contradictory media")]
        public void ReviewedFidelityAndMediaFailuresRefuseActualCliOutput(string css, string reason)
        {
            string fixture = CustomFixture(css);
            Assert.That(Run(fixture, out string diagnostic), Is.Not.EqualTo(0));
            Assert.That(diagnostic.ToLowerInvariant(), Does.Contain(reason));
            Assert.That(File.Exists(Path.Combine(fixture, "out", "Screens", "Neutral.uxml")), Is.False);
        }

        [TestCase(".card { width:100px; height:100px; background-image:linear-gradient(45deg,red,blue); } .wide { width:100px; }", true)]
        [TestCase(".card { width:100px; height:100px; background-image:linear-gradient(90deg,red 0px,blue 100px); }", true)]
        [TestCase(".card { width:50%; height:100px; background-image:linear-gradient(90deg,red 0%,blue 100%); }", true)]
        [TestCase(".card { width:100px; height:100px; } @media(min-width:800px) and (max-width:1200px) { .card { opacity:0.5; } }", false)]
        public void HomogeneousFixedStopsFluidPercentAndOrderedBoundsRemainSupported(string css, bool expectGradient)
        {
            string fixture = CustomFixture(css);
            Assert.That(Run(fixture, out string diagnostic), Is.EqualTo(0), diagnostic);
            string output = Path.Combine(fixture, "out");
            Assert.That(File.Exists(Path.Combine(output, "Screens", "Neutral.uxml")), Is.True);
            string uss = File.ReadAllText(Path.Combine(output, "Screens", "Neutral.uss"));
            if (expectGradient)
            {
                RgbaImage image = ImageFor(output, uss, ".card");
                Assert.That(image.Height, Is.EqualTo(100));
                Assert.That(image.Get(image.Width - 1, 0).B, Is.GreaterThan(250));
            }
            else
            {
                using JsonDocument mq = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "Screens", "Neutral.mq.json")));
                JsonElement query = mq.RootElement.GetProperty("classes")[0].GetProperty("query");
                Assert.That(query.GetProperty("minWidth").GetDouble(), Is.EqualTo(800));
                Assert.That(query.GetProperty("maxWidth").GetDouble(), Is.EqualTo(1200));
            }
        }

        private static string CustomFixture(string css)
        {
            string fixture = CopyFixture();
            string html = "<!doctype html><html><head><meta name=\"uitk:screen\" content=\"Neutral\"><style>" + css
                + "</style></head><body><div name=\"app-root\"><div name=\"app-card\" class=\"card\"></div><div name=\"app-wide\" class=\"card wide\"></div></div></body></html>";
            File.WriteAllText(Path.Combine(fixture, "input", "neutral.html"), html);
            return fixture;
        }
    }
}
