using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Neo.HtmlToUitk.Json;
using Neo.HtmlToUitk.Tooling;

namespace Neo.HtmlToUitk.PureLogicTests
{
    public class GlobTests
    {
        [TestCase("pages/*.html", "pages/home.html", true)]
        [TestCase("pages/*.html", "pages/sub/home.html", false)]
        [TestCase("pages/**/*.html", "pages/sub/deep/home.html", true)]
        [TestCase("pages/**/*.html", "pages/home.html", true)]
        [TestCase("**/*.html", "a/b/c.html", true)]
        [TestCase("page?.html", "page1.html", true)]
        [TestCase("page?.html", "page10.html", false)]
        [TestCase("a/*/c.html", "a/b/c.html", true)]
        [TestCase("a/*/c.html", "a/b/x/c.html", false)]
        [TestCase("PAGES/*.HTML", "pages/home.html", true)]
        [TestCase("p+q/*.html", "p+q/a.html", true)]
        public void Matches(string pattern, string path, bool expected)
        {
            Assert.That(GlobMatcher.IsMatch(pattern, path), Is.EqualTo(expected));
        }

        [TestCase("pages/**/*.html", "pages")]
        [TestCase("pages/sub/*.html", "pages/sub")]
        [TestCase("*.html", "")]
        [TestCase("D:/work/pages/*.html", "D:/work/pages")]
        public void StaticPrefix(string pattern, string expected)
        {
            Assert.That(GlobMatcher.StaticPrefix(pattern), Is.EqualTo(expected));
        }

        [Test]
        public void BackslashesAreNormalised()
        {
            Assert.That(GlobMatcher.IsMatch("pages\\*.html", "pages\\home.html"), Is.True);
        }
    }

    public class InputExpanderTests
    {
        private sealed class FakeFs : IFileView
        {
            private readonly List<string> _files;

            public FakeFs(params string[] files)
            {
                _files = files.ToList();
            }

            public bool FileExists(string path) => _files.Contains(path);

            public bool DirectoryExists(string path) => _files.Any(f => f.StartsWith(path.TrimEnd('/') + "/", StringComparison.Ordinal));

            public IEnumerable<string> EnumerateFiles(string directory, bool recursive)
            {
                string prefix = directory.TrimEnd('/') + "/";
                return _files.Where(f => f.StartsWith(prefix, StringComparison.Ordinal) && (recursive || f.IndexOf('/', prefix.Length) < 0));
            }
        }

        private static readonly FakeFs Fs = new FakeFs(
            "C:/proj/pages/home.html", "C:/proj/pages/menu.html", "C:/proj/pages/sub/deep.html", "C:/proj/pages/readme.txt", "C:/proj/extra/about.html");

        private static readonly string[] Html = { ".html", ".htm" };

        [Test]
        public void FileFolderAndGlobEntriesExpandRelativeToBase()
        {
            IReadOnlyList<ExpandedEntry> entries = InputExpander.Expand("C:/proj", new[] { "extra/about.html", "pages", "pages/*.html", "nothing/*.html", "missing.html" }, Fs, Html);
            Assert.That(entries[0].Kind, Is.EqualTo(EntryKind.File));
            Assert.That(entries[0].Files, Is.EqualTo(new[] { "extra/about.html" }));
            Assert.That(entries[1].Kind, Is.EqualTo(EntryKind.Folder));
            Assert.That(entries[1].Files, Is.EqualTo(new[] { "pages/home.html", "pages/menu.html", "pages/sub/deep.html" }));
            Assert.That(entries[2].Kind, Is.EqualTo(EntryKind.Glob));
            Assert.That(entries[2].Files, Is.EqualTo(new[] { "pages/home.html", "pages/menu.html" }));
            Assert.That(entries[3].IsEmpty, Is.True);
            Assert.That(entries[4].IsEmpty, Is.True);
        }

        [Test]
        public void FlattenDeduplicatesAndSorts()
        {
            IReadOnlyList<ExpandedEntry> entries = InputExpander.Expand("C:/proj", new[] { "pages/*.html", "pages", "extra/about.html" }, Fs, Html);
            Assert.That(InputExpander.Flatten(entries), Is.EqualTo(new[] { "extra/about.html", "pages/home.html", "pages/menu.html", "pages/sub/deep.html" }));
        }

        [Test]
        public void BackslashEntriesAndBlankEntriesAreTolerated()
        {
            IReadOnlyList<ExpandedEntry> entries = InputExpander.Expand("C:\\proj\\", new[] { "pages\\home.html", "  ", null }, Fs, Html);
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Files, Is.EqualTo(new[] { "pages/home.html" }));
        }

        [Test]
        public void AbsoluteEntryOutsideBaseKeepsAbsolutePath()
        {
            IReadOnlyList<ExpandedEntry> entries = InputExpander.Expand("C:/other", new[] { "C:/proj/extra/about.html" }, Fs, Html);
            Assert.That(entries[0].Files, Is.EqualTo(new[] { "C:/proj/extra/about.html" }));
        }

        [Test]
        public void MakeRelativeAndCombine()
        {
            Assert.That(InputExpander.MakeRelative("C:/proj", "C:/proj/a/b.html"), Is.EqualTo("a/b.html"));
            Assert.That(InputExpander.MakeRelative("C:/proj", "D:/x/b.html"), Is.EqualTo("D:/x/b.html"));
            Assert.That(InputExpander.Combine("C:/proj/", "a/b.html"), Is.EqualTo("C:/proj/a/b.html"));
            Assert.That(InputExpander.Combine("C:/proj", "D:/x/b.html"), Is.EqualTo("D:/x/b.html"));
        }
    }

    public class DiagnosticsReportTests
    {
        private const string Sample = @"{
  ""tool"": ""htmltouitk"", ""version"": ""0.1.0"", ""source"": ""pages/home.html"", ""verdict"": ""FAIL"",
  ""counts"": { ""error"": 1, ""warning"": 1, ""info"": 1 },
  ""tiers"": { ""native"": 12, ""convertTime"": 1, ""runtime"": 0, ""reject"": 1 },
  ""findings"": [
    { ""rule"": ""C006"", ""severity"": ""Error"", ""tier"": ""Reject"", ""file"": ""pages/home.html"", ""line"": 14, ""column"": 5,
      ""construct"": ""width: calc(100% - 20px)"", ""message"": ""calc() is not supported"", ""why"": ""USS values are literals"", ""suggestion"": ""Use flex-grow."", ""related"": [ { ""file"": ""pages/other.html"", ""line"": 3, ""column"": 1 } ] },
    { ""rule"": ""C002"", ""severity"": ""Warning"", ""tier"": ""ConvertTime"", ""emulation"": ""gap-margins"", ""file"": ""pages/menu.html"", ""line"": 9, ""column"": 5, ""construct"": ""gap: 8px"", ""message"": ""gap expanded into margins"" },
    { ""rule"": ""L003"", ""severity"": ""Info"", ""tier"": ""Native"", ""file"": ""pages\\home.html"", ""line"": 30, ""column"": 3 }
  ],
  ""newKeys"": [""home.title""]
}";

        [Test]
        public void ParsesTheSampleReport()
        {
            DiagnosticsReport report = DiagnosticsReport.Parse(Sample);
            Assert.That(report.Verdict, Is.EqualTo("FAIL"));
            Assert.That(report.Errors, Is.EqualTo(1));
            Assert.That(report.Warnings, Is.EqualTo(1));
            Assert.That(report.Infos, Is.EqualTo(1));
            Assert.That(report.Tiers.Single(t => t.Key == "convertTime").Value, Is.EqualTo(1));
            Assert.That(report.NewKeys, Is.EqualTo(new[] { "home.title" }));
            Finding first = report.Findings[0];
            Assert.That(first.Rule, Is.EqualTo("C006"));
            Assert.That(first.Severity, Is.EqualTo(FindingSeverity.Error));
            Assert.That(first.Line, Is.EqualTo(14));
            Assert.That(first.Suggestion, Is.EqualTo("Use flex-grow."));
            Assert.That(first.Related.Single().File, Is.EqualTo("pages/other.html"));
            Assert.That(report.Findings[1].Emulation, Is.EqualTo("gap-margins"));
        }

        [Test]
        public void StatusIsWorstSeverityPerFileIgnoringSlashStyle()
        {
            DiagnosticsReport report = DiagnosticsReport.Parse(Sample);
            Assert.That(report.StatusOf("pages/home.html"), Is.EqualTo(FileStatus.Fail));
            Assert.That(report.StatusOf("pages/menu.html"), Is.EqualTo(FileStatus.Warn));
            Assert.That(report.StatusOf("pages/clean.html"), Is.EqualTo(FileStatus.Pass));
            Assert.That(report.For("pages\\home.html").Count, Is.EqualTo(2));
            Assert.That(report.Files().Count(), Is.EqualTo(2));
        }

        [Test]
        public void CountsAreRebuiltWhenMissing()
        {
            DiagnosticsReport report = DiagnosticsReport.Parse(@"{ ""tool"": ""htmltouitk"", ""version"": ""1"", ""verdict"": ""PASS"",
              ""findings"": [ { ""rule"": ""L003"", ""severity"": ""Info"", ""tier"": ""Native"", ""file"": ""a.html"", ""line"": 1, ""column"": 1 } ] }");
            Assert.That(report.Infos, Is.EqualTo(1));
            Assert.That(report.Errors, Is.EqualTo(0));
        }

        [Test]
        public void FailureReportCarriesTheMessageAsError()
        {
            DiagnosticsReport report = DiagnosticsReport.Failure("process not found");
            Assert.That(report.Verdict, Is.EqualTo("FAIL"));
            Assert.That(report.Errors, Is.EqualTo(1));
            Assert.That(report.Findings[0].Message, Is.EqualTo("process not found"));
        }
    }

    public class JobDocumentTests
    {
        private const string Sample = @"{
  ""schema"": 1,
  ""name"": ""example-app"",
  ""config"": ""htmltouitk.config.json"",
  ""configOverrides"": { ""rules"": { ""minFontSize"": 14 } },
  ""inputs"": { ""html"": [""pages/*.html""], ""uss"": [""project/UI/Skin/Skin.uss""], ""futureKey"": 7 },
  ""output"": { ""root"": ""out"", ""assemble"": ""perFile"", ""folders"": { ""uxml"": ""Screens"", ""uss"": ""Screens"", ""shared"": ""Shared"", ""strings"": ""Localization"" },
                ""perFile"": { ""pages/home.html"": { ""name"": ""Home"" } } },
  ""watch"": { ""enabled"": false, ""debounceMs"": 400 },
  ""x-extension"": { ""keep"": true }
}";

        [Test]
        public void ParsesTypedFields()
        {
            JobDocument job = JobDocument.Parse(Sample);
            Assert.That(job.Name, Is.EqualTo("example-app"));
            Assert.That(job.Html, Is.EqualTo(new[] { "pages/*.html" }));
            Assert.That(job.Uss, Is.EqualTo(new[] { "project/UI/Skin/Skin.uss" }));
            Assert.That(job.OutputRoot, Is.EqualTo("out"));
            Assert.That(job.Folders.Count, Is.EqualTo(4));
            Assert.That(job.DebounceMs, Is.EqualTo(400));
            Assert.That(job.ValidateShape(), Is.Empty);
        }

        [Test]
        public void RoundTripPreservesUnknownKeysAndNestedBlocks()
        {
            JobDocument job = JobDocument.Parse(Sample);
            string written = job.ToJson();
            JsonValue root = MiniJson.Parse(written);
            Assert.That(root["x-extension"]["keep"].AsBool(), Is.True);
            Assert.That(root["inputs"]["futureKey"].AsInt(), Is.EqualTo(7));
            Assert.That(root["configOverrides"]["rules"]["minFontSize"].AsInt(), Is.EqualTo(14));
            Assert.That(root["output"]["perFile"]["pages/home.html"]["name"].AsString(), Is.EqualTo("Home"));
            Assert.That(JobDocument.Parse(written).ToJson(), Is.EqualTo(written));
        }

        [Test]
        public void EditedFieldsAreWrittenInSchemaOrder()
        {
            JobDocument job = JobDocument.Parse(Sample);
            job.Html.Add("extra/about.html");
            job.WatchEnabled = true;
            job.DebounceMs = 250;
            job.OutputRoot = "Assets/UI/Generated";
            JsonValue root = MiniJson.Parse(job.ToJson());
            Assert.That(root.Members.Select(m => m.Key).Take(6), Is.EqualTo(new[] { "schema", "name", "config", "configOverrides", "inputs", "output" }));
            Assert.That(root["inputs"]["html"].ToStringList(), Is.EqualTo(new[] { "pages/*.html", "extra/about.html" }));
            Assert.That(root["watch"]["enabled"].AsBool(), Is.True);
            Assert.That(root["watch"]["debounceMs"].AsInt(), Is.EqualTo(250));
            Assert.That(root["output"]["root"].AsString(), Is.EqualTo("Assets/UI/Generated"));
        }

        [Test]
        public void EmptyOptionalFieldsAreOmitted()
        {
            JobDocument job = new JobDocument { OutputRoot = "out" };
            job.Html.Add("a.html");
            JsonValue root = MiniJson.Parse(job.ToJson());
            Assert.That(root.Has("name"), Is.False);
            Assert.That(root.Has("config"), Is.False);
            Assert.That(root.Has("configOverrides"), Is.False);
            Assert.That(root["inputs"].Has("css"), Is.False);
            Assert.That(root["output"].Has("folders"), Is.False);
            Assert.That(root["schema"].AsInt(), Is.EqualTo(1));
        }

        [Test]
        public void ValidateShapeFlagsMissingInputsAndRoot()
        {
            Assert.That(new JobDocument().ValidateShape().Count, Is.EqualTo(2));
        }
    }

    public class CliAndDebounceTests
    {
        [Test]
        public void RunArgumentsMirrorTheDocumentedCommand()
        {
            Assert.That(CliCommandLine.BuildRunArguments("job.htujob.json", false, false), Is.EqualTo(new[] { "run", "job.htujob.json", "--format", "json" }));
            Assert.That(CliCommandLine.BuildRunArguments("job.htujob.json", true, true), Is.EqualTo(new[] { "run", "job.htujob.json", "--format", "json", "--validate-only", "--force" }));
            Assert.That(CliCommandLine.BuildRunArguments("job.htujob.json", false, false, true), Is.EqualTo(new[] { "run", "job.htujob.json", "--format", "json", "--strict" }));
        }

        [TestCase("plain", "plain")]
        [TestCase("with space", "\"with space\"")]
        [TestCase("quote\"d", "\"quote\\\"d\"")]
        [TestCase("trail\\", "trail\\")]
        [TestCase("C:\\a b\\", "\"C:\\a b\\\\\"")]
        [TestCase("", "\"\"")]
        public void ArgumentQuoting(string argument, string expected)
        {
            Assert.That(CliCommandLine.Quote(argument), Is.EqualTo(expected));
        }

        [Test]
        public void ExtractsJsonObjectFromNoisyOutput()
        {
            Assert.That(CliCommandLine.ExtractJsonObject("banner\n{\"a\": {\"b\": 1}}\ntrailer"), Is.EqualTo("{\"a\": {\"b\": 1}}"));
            Assert.That(CliCommandLine.ExtractJsonObject("no json"), Is.Null);
            Assert.That(CliCommandLine.ExtractJsonObject(null), Is.Null);
        }

        [Test]
        public void DebounceFiresOnceAfterQuietPeriodWithAllKeys()
        {
            DebounceGate gate = new DebounceGate(0.4);
            List<string> keys = new List<string>();
            gate.Touch(1.0, "b.html");
            gate.Touch(1.2, "a.html");
            gate.Touch(1.3, "A.HTML");
            Assert.That(gate.TryFire(1.5, keys), Is.False);
            Assert.That(gate.TryFire(1.69, keys), Is.False);
            Assert.That(gate.TryFire(1.71, keys), Is.True);
            Assert.That(keys.Count, Is.EqualTo(2));
            Assert.That(gate.HasPending, Is.False);
            Assert.That(gate.TryFire(5.0, keys), Is.False);
        }

        [Test]
        public void TouchDuringQuietPeriodExtendsIt()
        {
            DebounceGate gate = new DebounceGate(0.5);
            List<string> keys = new List<string>();
            gate.Touch(0.0, "a");
            gate.Touch(0.4, "a");
            Assert.That(gate.TryFire(0.8, keys), Is.False);
            Assert.That(gate.TryFire(0.95, keys), Is.True);
        }
    }

    public partial class ReportReaderTests
    {
        private const string Report = "{\"tool\":\"htmltouitk\",\"version\":\"0.1.0\",\"verdict\":\"PASS WITH WARNINGS\",\"counts\":{\"error\":0,\"warning\":1,\"info\":0},\"findings\":[{\"rule\":\"C002\",\"severity\":\"Warning\",\"tier\":\"ConvertTime\",\"file\":\"a.html\",\"line\":1,\"column\":1}]}";

        private static string NewTemp()
        {
            string path = Path.Combine(Path.GetTempPath(), "htu-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(path);
            return path.Replace('\\', '/');
        }

        [Test]
        public void PrefersTheFreshReportFileInTheOutputFolder()
        {
            string dir = NewTemp();
            try
            {
                File.WriteAllText(dir + "/diagnostics.json", Report);
                JobRunRequest request = new JobRunRequest { OutputRoot = dir };
                DiagnosticsReport report = ReportReader.FromRun("human readable text", string.Empty, 0, request, DateTime.UtcNow.AddSeconds(-5));
                Assert.That(report.Verdict, Is.EqualTo("PASS WITH WARNINGS"));
                Assert.That(report.Warnings, Is.EqualTo(1));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void StaleReportFileFallsBackToJsonOnStandardOutput()
        {
            string dir = NewTemp();
            try
            {
                File.WriteAllText(dir + "/diagnostics.json", Report.Replace("PASS WITH WARNINGS", "FAIL"));
                File.SetLastWriteTimeUtc(dir + "/diagnostics.json", DateTime.UtcNow.AddHours(-1));
                JobRunRequest request = new JobRunRequest { OutputRoot = dir };
                DiagnosticsReport report = ReportReader.FromRun("banner\n" + Report, string.Empty, 0, request, DateTime.UtcNow.AddSeconds(-5));
                Assert.That(report.Verdict, Is.EqualTo("PASS WITH WARNINGS"));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void MissingReportBecomesAFailureCarryingStderr()
        {
            string directory = NewTemp();
            try
            {
                DiagnosticsReport report = ReportReader.FromRun("text only", "usage: htmltouitk run <job>", 2, new JobRunRequest { OutputRoot = directory }, DateTime.UtcNow);
                Assert.That(report.Verdict, Is.EqualTo("FAIL"));
                Assert.That(report.Findings[0].Message, Does.Contain("exit code 2"));
                Assert.That(report.Findings[0].Message, Does.Contain("usage: htmltouitk run"));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void MalformedStandardOutputJsonIsReportedNotThrown()
        {
            DiagnosticsReport report = ReportReader.FromRun("{ \"verdict\": }", string.Empty, 1, new JobRunRequest(), DateTime.UtcNow);
            Assert.That(report.Verdict, Is.EqualTo("FAIL"));
            Assert.That(report.Errors, Is.EqualTo(1));
            Assert.That(report.Findings[0].Message, Does.Contain("malformed report"));
        }

        [Test]
        public void ReportPathIsTheOutputRootJoinedWithTheFixedName()
        {
            Assert.That(ReportReader.ReportPath(new JobRunRequest { OutputRoot = "C:\\out\\" }), Is.EqualTo("C:/out/diagnostics.json"));
            Assert.That(ReportReader.ReportPath(new JobRunRequest()), Is.Null);
        }
    }
}
