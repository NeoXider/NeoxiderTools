using System;
using System.IO;
using NUnit.Framework;
using Neo.HtmlToUitk.Json;
using Neo.HtmlToUitk.Tooling;

namespace Neo.HtmlToUitk.Tests
{
    public partial class RunnerEditModeTests
    {
        [Test]
        public void BridgeFailureReleasesTemporaryJobAndRunningStateWithoutChangingTheSourceJob()
        {
            string directory = Path.Combine(Path.GetTempPath(), "htu-runner-failure-" + Guid.NewGuid().ToString("N")).Replace('\\', '/');
            string jobPath = directory + "/main.htujob.json";
            IJobBridge previousBridge = HtmlToUitkRunner.BridgeOverride;
            InvalidOperationException failure = new InvalidOperationException("Injected bridge failure after reading the temporary job.");
            StubJobBridge stub = new StubJobBridge { ExceptionToThrow = failure };
            try
            {
                Directory.CreateDirectory(directory);
                JobDocument document = JobDocument.Parse("{\"schema\":1,\"inputs\":{\"html\":[\"pages/*.html\"]},\"output\":{\"root\":\"out\"},\"x-extension\":{\"keep\":true}}");
                string originalJson = document.ToJson();
                File.WriteAllText(jobPath, originalJson);
                HtmlToUitkRunner.BridgeOverride = stub;

                try
                {
                    JobRunResult result = HtmlToUitkRunner.Run(document, jobPath, new RunOptions { OnlyFiles = new[] { "pages/selected.html" } });
                    Assert.That(result, Is.Not.Null);
                    Assert.That(result.Faulted, Is.True, "A bridge exception may become a structured failure, but cannot become success.");
                    Assert.That(result.Succeeded, Is.False);
                }
                catch (InvalidOperationException exception)
                {
                    Assert.That(exception, Is.SameAs(failure));
                }

                Assert.That(stub.TemporaryExisted, Is.True);
                Assert.That(stub.Seen.Count, Is.EqualTo(1));
                JsonValue temporaryJob = MiniJson.Parse(stub.Seen[0]);
                Assert.That(temporaryJob["inputs"]["html"].ToStringList(), Is.EqualTo(new[] { "pages/selected.html" }));
                Assert.That(temporaryJob["x-extension"]["keep"].AsBool(), Is.True);
                Assert.That(File.Exists(stub.Last.JobPath), Is.False, "Failure must release the selected-file job.");
                Assert.That(HtmlToUitkRunner.IsRunning, Is.False);
                Assert.That(document.ToJson(), Is.EqualTo(originalJson));
                Assert.That(File.ReadAllText(jobPath), Is.EqualTo(originalJson));
                Assert.That(Directory.GetFiles(directory).Length, Is.EqualTo(1));
            }
            finally
            {
                HtmlToUitkRunner.BridgeOverride = previousBridge;
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }
    }
}
