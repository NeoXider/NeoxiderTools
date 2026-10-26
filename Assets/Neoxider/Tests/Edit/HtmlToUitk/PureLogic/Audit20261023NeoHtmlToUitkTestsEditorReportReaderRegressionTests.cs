using System;
using System.IO;
using NUnit.Framework;
using Neo.HtmlToUitk.Tooling;

namespace Neo.HtmlToUitk.PureLogicTests
{
    public partial class ReportReaderTests
    {
        [TestCase("{\"verdict\": }")]
        [TestCase("{}")]
        public void InvalidFreshReportFileFallsBackToValidStandardOutput(string invalidReport)
        {
            string directory = NewTemp();
            try
            {
                JobRunRequest request = new JobRunRequest { OutputRoot = directory };
                string reportPath = ReportReader.ReportPath(request);
                DateTime runStartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                File.WriteAllText(reportPath, invalidReport);
                File.SetLastWriteTimeUtc(reportPath, runStartUtc);

                DiagnosticsReport report = ReportReader.FromRun("banner\n" + Report, string.Empty, 0, request, runStartUtc);

                Assert.That(report.Verdict, Is.EqualTo("PASS WITH WARNINGS"));
                Assert.That(report.Warnings, Is.EqualTo(1));
                Assert.That(report.Findings[0].Rule, Is.EqualTo("C002"));
                Assert.That(File.ReadAllText(reportPath), Is.EqualTo(invalidReport), "Reading diagnostics must not rewrite the converter output.");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void InvalidFreshReportWithoutJsonOutputRetainsTheParseReasonAndStderr()
        {
            string directory = NewTemp();
            try
            {
                JobRunRequest request = new JobRunRequest { OutputRoot = directory };
                DateTime runStartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                string reportPath = ReportReader.ReportPath(request);
                File.WriteAllText(reportPath, "{\"verdict\": }");
                File.SetLastWriteTimeUtc(reportPath, runStartUtc);

                DiagnosticsReport report = ReportReader.FromRun("conversion stopped", "converter could not finish", 2, request, runStartUtc);

                Assert.That(report.Verdict, Is.EqualTo("FAIL"));
                Assert.That(report.Errors, Is.EqualTo(1));
                Assert.That(report.Findings[0].Message, Does.Contain("report file is malformed"));
                Assert.That(report.Findings[0].Message, Does.Contain("exit code 2"));
                Assert.That(report.Findings[0].Message, Does.Contain("converter could not finish"));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
