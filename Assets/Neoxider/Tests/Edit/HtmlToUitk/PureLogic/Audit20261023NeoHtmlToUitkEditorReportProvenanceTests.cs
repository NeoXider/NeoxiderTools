using System;
using System.IO;
using Neo.HtmlToUitk.Tooling;
using NUnit.Framework;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class Audit20261023NeoHtmlToUitkEditorReportProvenanceTests
    {
        private const string Pass = "{\"verdict\":\"PASS\",\"counts\":{\"error\":0},\"findings\":[]}";
        private const string Fail = "{\"verdict\":\"FAIL\",\"counts\":{\"error\":1},\"findings\":[{\"rule\":\"CURRENT\",\"severity\":\"Error\",\"message\":\"current source error\"}]}";

        [TestCase(false)]
        [TestCase(true)]
        public void CurrentFailureWinsOverRecentPassFile(bool validateOnly)
        {
            WithReport(Pass, (request, start, path) =>
            {
                request.ValidateOnly = validateOnly;
                DiagnosticsReport result = ReportReader.FromRun(Fail, string.Empty, 2, request, start);
                Assert.That(result.Verdict, Is.EqualTo("FAIL"));
                Assert.That(result.Errors, Is.EqualTo(1));
                Assert.That(result.Findings[0].Rule, Is.EqualTo("CURRENT"));
                Assert.That(result.Findings[0].Message, Is.EqualTo("current source error"));
                Assert.That(File.ReadAllText(path), Is.EqualTo(Pass));
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CurrentPassWinsOverRecentFailureFile(bool validateOnly)
        {
            WithReport(Fail, (request, start, path) =>
            {
                request.ValidateOnly = validateOnly;
                DiagnosticsReport report = ReportReader.FromRun(Pass, string.Empty, 0, request, start);
                JobRunResult result = new JobRunResult { Report = report };
                result.ValidateReport();
                Assert.That(result.Succeeded, Is.True);
                Assert.That(report.Errors, Is.Zero);
                Assert.That(report.Findings, Is.Empty);
                Assert.That(File.ReadAllText(path), Is.EqualTo(Fail));
            });
        }

        [Test]
        public void ValidateOnlyCannotBorrowEvenANewOutputFileWithoutCurrentStdout()
        {
            WithReport(Pass, (request, start, path) =>
            {
                request.ValidateOnly = true;
                File.SetLastWriteTimeUtc(path, start.AddSeconds(1));
                DiagnosticsReport report = ReportReader.FromRun(string.Empty, "no current report", 0, request, start);
                Assert.That(report.Verdict, Is.EqualTo("FAIL"));
                Assert.That(report.Findings[0].Message, Does.Contain("Validate-only"));
                Assert.That(report.Findings[0].Message, Does.Contain("no current report"));
            });
        }

        [Test]
        public void IdenticalSuccessfulRunsDoNotRequireRewritingTheReportFile()
        {
            WithReport(Pass, (request, start, path) =>
            {
                File.SetLastWriteTimeUtc(path, start.AddHours(-1));
                DateTime unchanged = File.GetLastWriteTimeUtc(path);
                DiagnosticsReport first = ReportReader.FromRun(Pass, string.Empty, 0, request, start);
                DiagnosticsReport second = ReportReader.FromRun(Pass, string.Empty, 0, request, start.AddSeconds(1));
                Assert.That(first.Verdict, Is.EqualTo("PASS"));
                Assert.That(second.Verdict, Is.EqualTo("PASS"));
                Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(unchanged));
            });
        }

        [Test]
        public void FileOnlyLegacyFallbackRejectsAReportOneSecondOlderThanTheRun()
        {
            WithReport(Pass, (request, start, path) =>
            {
                DiagnosticsReport report = ReportReader.FromRun("text only", string.Empty, 0, request, start);
                Assert.That(report.Verdict, Is.EqualTo("FAIL"));
                Assert.That(report.Findings[0].Message, Does.Contain("older than this run"));
            });
        }

        [Test]
        public void FileOnlyLegacyFallbackStillAcceptsAReportWrittenAfterRunStart()
        {
            WithReport(Pass, (request, start, path) =>
            {
                File.SetLastWriteTimeUtc(path, start.AddSeconds(1));
                DiagnosticsReport report = ReportReader.FromRun("text only", string.Empty, 0, request, start);
                Assert.That(report.Verdict, Is.EqualTo("PASS"));
            });
        }

        private static void WithReport(string json, Action<JobRunRequest, DateTime, string> inspect)
        {
            string directory = Path.Combine(Path.GetTempPath(), "Audit20261023NeoHtmlToUitkEditor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, ReportReader.ReportFileName);
            try
            {
                DateTime start = DateTime.UtcNow;
                File.WriteAllText(path, json);
                File.SetLastWriteTimeUtc(path, start.AddSeconds(-1));
                inspect(new JobRunRequest { OutputRoot = directory }, start, path);
            }
            finally
            {
                File.Delete(path);
                Directory.Delete(directory);
            }
        }
    }
}
