using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using Neo.HtmlToUitk.Responsive;
using Neo.HtmlToUitk.Tooling;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class OwnershipLogicTests
    {
        [TestCase("{}")] [TestCase("{\"verdict\":\"UNKNOWN\"}")]
        public void MissingOrUnknownVerdictCannotBecomeSuccess(string json)
        {
            Assert.Throws<Neo.HtmlToUitk.Json.JsonParseException>(() => DiagnosticsReport.Parse(json));
            DiagnosticsReport report = ReportReader.FromRun(json, string.Empty, 0, new JobRunRequest(), DateTime.UtcNow);
            Assert.That(report.Verdict, Is.EqualTo("FAIL"));
        }

        [TestCase(0, "FAIL", 1)] [TestCase(2, "PASS", 0)] [TestCase(1, "PASS", 0)]
        public void ContradictoryProcessAndDiagnosticsResultsFailClosed(int code, string verdict, int errors)
        {
            JobRunResult result = new JobRunResult { ExitCode = code, Report = DiagnosticsReport.Parse("{\"verdict\":\"" + verdict + "\",\"counts\":{\"error\":" + errors + "}}") };
            result.ValidateReport();
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Faulted, Is.True);
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
        }

        [TestCase("{\"minWidth\":\"wide\"}")] [TestCase("{\"minWidth\":2000,\"maxWidth\":1000}")]
        [TestCase("{\"orientation\":\"sideways\"}")] [TestCase("{\"maxHeight\":-1}")]
        public void MalformedMediaIsRejectedRatherThanSilentlyDroppingConstraints(string query)
        {
            Assert.Throws<FormatException>(() => BreakpointSet.Parse("{\"classes\":[{\"class\":\"wide\",\"query\":" + query + "}]}"));
        }

        [Test]
        public void ZeroAndNonfiniteViewportDoNotMatchAndInclusiveBoundsUseHeightAndAspect()
        {
            BreakpointQuery query = new BreakpointQuery { MinWidth = 1280, MaxWidth = 1920, MinHeight = 720, MaxHeight = 1080, MinAspect = 1.7, MaxAspect = 1.8 };
            Assert.That(query.Matches(1280, 720), Is.True);
            Assert.That(query.Matches(1920, 1080), Is.True);
            Assert.That(query.Matches(1920, 720), Is.False);
            Assert.That(query.Matches(0, 0), Is.False);
            Assert.That(query.Matches(double.NaN, 720), Is.False);
            Assert.That(query.Matches(1280, double.PositiveInfinity), Is.False);
        }

        [Test]
        public void GeneratedTextureRequiresExactManifestPathHashAndPolicy()
        {
            string directory = Path.Combine(Path.GetTempPath(), "htu-manifest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string image = Path.Combine(directory, "gradient.png");
            byte[] png = { 137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3 };
            File.WriteAllBytes(image, png);
            try
            {
                string hash;
                using (SHA256 algorithm = SHA256.Create()) hash = BitConverter.ToString(algorithm.ComputeHash(png)).Replace("-", string.Empty);
                string json = "{\"schema\":1,\"assets\":[{\"path\":\"gradient.png\",\"sha256\":\"" + hash + "\",\"width\":1,\"height\":1,\"kind\":\"gradient\",\"sRGB\":true,\"wrap\":\"clamp\",\"mipmaps\":false}]}";
                Assert.That(GeneratedTextureManifest.Owns(json, directory, image), Is.True);
                Assert.That(GeneratedTextureManifest.Owns(json.Replace(hash, new string('0', 64)), directory, image), Is.False);
                Assert.That(GeneratedTextureManifest.Owns(json.Replace("gradient.png", "../gradient.png"), directory, image), Is.False);
                Assert.That(GeneratedTextureManifest.Owns(json.Replace("\"mipmaps\":false", "\"mipmaps\":true"), directory, image), Is.False);
                File.WriteAllBytes(image, new byte[] { 1, 2, 3 });
                Assert.That(GeneratedTextureManifest.Owns(json, directory, image), Is.False);
            }
            finally { File.Delete(image); Directory.Delete(directory); }
        }
    }
}
