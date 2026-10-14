using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Neo.HtmlToUitk.Json;
using Neo.HtmlToUitk.Tooling;

namespace Neo.HtmlToUitk.Tests
{
    public class RunnerEditModeTests
    {
        private sealed class StubJobBridge : IJobBridge
        {
            public readonly List<string> Seen = new List<string>();
            public JobRunRequest Last;
            public bool TemporaryExisted;

            public string Name => "stub";

            public bool IsAvailable(out string reason)
            {
                reason = null;
                return true;
            }

            public JobRunResult Run(JobRunRequest request)
            {
                Last = request;
                TemporaryExisted = File.Exists(request.JobPath);
                Seen.Add(File.ReadAllText(request.JobPath));
                DiagnosticsReport report = DiagnosticsReport.Parse("{\"tool\":\"htmltouitk\",\"version\":\"0\",\"verdict\":\"PASS\",\"counts\":{\"error\":0,\"warning\":0,\"info\":0},\"findings\":[]}");
                return new JobRunResult { Report = report, BridgeName = Name };
            }
        }

        [Test]
        public void SelectedFilesRunThroughATemporaryJobThatIsDeletedAfterwards()
        {
            string dir = Path.Combine(Path.GetTempPath(), "htu-runner-" + Guid.NewGuid().ToString("N").Substring(0, 8)).Replace('\\', '/');
            Directory.CreateDirectory(dir);
            StubJobBridge stub = new StubJobBridge();
            HtmlToUitkRunner.BridgeOverride = stub;
            try
            {
                JobDocument document = new JobDocument { OutputRoot = "out" };
                document.Html.Add("pages/*.html");
                JobRunResult result = HtmlToUitkRunner.Run(document, dir + "/main.htujob.json", new RunOptions { Strict = true, OnlyFiles = new[] { "pages/a.html" } });
                Assert.That(result.Report.Verdict, Is.EqualTo("PASS"));
                Assert.That(stub.TemporaryExisted, Is.True);
                Assert.That(stub.Last.Strict, Is.True);
                Assert.That(stub.Last.OutputRoot, Is.EqualTo(dir + "/out"));
                Assert.That(MiniJson.Parse(stub.Seen[0])["inputs"]["html"].ToStringList(), Is.EqualTo(new[] { "pages/a.html" }));
                Assert.That(Path.GetFileName(stub.Last.JobPath), Does.StartWith(".htu-run-"));
                Assert.That(File.Exists(stub.Last.JobPath), Is.False);
                Assert.That(HtmlToUitkRunner.IsRunning, Is.False);
            }
            finally
            {
                HtmlToUitkRunner.BridgeOverride = null;
                Directory.Delete(dir, true);
            }
        }
    }
}
