using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Neo.HtmlToUitk.Json;
using Neo.HtmlToUitk.Tooling;
using NUnit.Framework;
using UnityEngine;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class Audit20261023NeoHtmlToUitkEditorPipelineTests
    {
        private sealed class RecordingBridge : IJobBridge
        {
            public JobDocument Document;
            public JobRunRequest Request;
            public string Name => "audit-recording";

            public bool IsAvailable(out string reason)
            {
                reason = null;
                return true;
            }

            public JobRunResult Run(JobRunRequest request)
            {
                Request = request;
                Document = JobDocument.Parse(File.ReadAllText(request.JobPath));
                return Pass(Name);
            }
        }

        private sealed class BlockingBridge : IJobBridge, IDisposable
        {
            public readonly ManualResetEventSlim Entered = new ManualResetEventSlim();
            public readonly ManualResetEventSlim Release = new ManualResetEventSlim();
            public string Name => "audit-blocking";

            public bool IsAvailable(out string reason)
            {
                reason = null;
                return true;
            }

            public JobRunResult Run(JobRunRequest request)
            {
                Entered.Set();
                if (!Release.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("The audit test did not release its blocker.");
                return Pass(Name);
            }

            public void Dispose()
            {
                Entered.Dispose();
                Release.Dispose();
            }
        }

        [Test]
        public void ParentSegmentsResolveToTheSameOutputAndProjectAssetPath()
        {
            string project = HtmlToUitkPaths.ProjectRoot;
            JobDocument document = new JobDocument { OutputRoot = "../Assets/UI/./Generated" };
            string output = HtmlToUitkRunner.OutputRootAbsolute(document, project + "/Jobs/menu.htujob.json");

            Assert.That(output, Is.EqualTo(project + "/Assets/UI/Generated"));
            Assert.That(HtmlToUitkPaths.ToProjectRelative(output + "/../Generated/menu.uss"), Is.EqualTo("Assets/UI/Generated/menu.uss"));
            Assert.That(HtmlToUitkPaths.ToAbsolute("Assets/UI/Generated/../Generated/menu.uss"), Is.EqualTo(output + "/menu.uss"));
            Assert.That(HtmlToUitkPaths.IsInsideAssets(output), Is.True);
            Assert.That(HtmlToUitkPaths.IsInsideAssets(project + "/Assets"), Is.True);
            Assert.That(HtmlToUitkPaths.IsInsideAssets(project + "/Assets/../Outside"), Is.False);
            Assert.That(HtmlToUitkPaths.IsInsideAssets(project + "/AssetsSibling/menu.uss"), Is.False);
            Assert.That(HtmlToUitkPaths.ToProjectRelative(project + "/../outside/menu.uss"), Is.Null);
        }

        [Test]
        public void ProjectRelativeSubmissionGivesTheBridgeAbsolutePaths()
        {
            string relative = "Temp/Audit20261023NeoHtmlToUitkEditor-" + Guid.NewGuid().ToString("N");
            string directory = HtmlToUitkPaths.ToAbsolute(relative);
            Directory.CreateDirectory(directory);
            IJobBridge previous = HtmlToUitkRunner.BridgeOverride;
            RecordingBridge recorder = new RecordingBridge();
            try
            {
                HtmlToUitkRunner.BridgeOverride = recorder;
                JobRunResult result = HtmlToUitkRunner.Run(new JobDocument { Name = "relative", OutputRoot = "Generated" }, relative + "/main.htujob.json", new RunOptions());

                Assert.That(result.Succeeded, Is.True);
                Assert.That(recorder.Document.Name, Is.EqualTo("relative"));
                Assert.That(recorder.Request.WorkingDirectory, Is.EqualTo(directory));
                Assert.That(Path.IsPathRooted(recorder.Request.JobPath), Is.True);
                Assert.That(recorder.Request.OutputRoot, Is.EqualTo(directory + "/Generated"));
                Assert.That(File.Exists(recorder.Request.JobPath), Is.False);
            }
            finally
            {
                HtmlToUitkRunner.BridgeOverride = previous;
                Directory.Delete(directory);
            }
        }

        [TestCase(true, "{\"rules\":")]
        [TestCase(false, "{\"menu.html\":")]
        [TestCase(true, "[1]")]
        [TestCase(false, "false")]
        public void InvalidInspectorJsonRefusesSaveAndPreservesTheExistingFile(bool configOverrides, string invalidJson)
        {
            string directory = NewDirectory();
            string path = Path.Combine(directory, "main.htujob.json");
            string original = "{\"name\":\"original\"}";
            File.WriteAllText(path, original);
            HtmlToUitkJob job = ScriptableObject.CreateInstance<HtmlToUitkJob>();
            try
            {
                job.SetJsonPath(path);
                if (configOverrides)
                    job.configOverridesJson = invalidJson;
                else
                    job.perFileJson = invalidJson;

                bool saved = job.SaveJson(out string error);

                Assert.That(saved, Is.False);
                Assert.That(error, Is.Not.Null.And.Not.Empty);
                Assert.That(File.ReadAllText(path), Is.EqualTo(original));
                Assert.That(configOverrides ? job.configOverridesJson : job.perFileJson, Is.EqualTo(invalidJson));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(job);
                File.Delete(path);
                Directory.Delete(directory);
            }
        }

        [Test]
        public void ExplicitNullInspectorJsonStillClearsOptionalOverrides()
        {
            HtmlToUitkJob job = ScriptableObject.CreateInstance<HtmlToUitkJob>();
            try
            {
                job.configOverridesJson = "null";
                job.perFileJson = "null";
                JsonValue saved = MiniJson.Parse(job.ToJson());

                Assert.That(saved.Has("configOverrides"), Is.False);
                Assert.That(saved["output"].Has("perFile"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(job);
            }
        }

        [Test]
        public void QueuedAsyncRunUsesTheDocumentAndOptionsAtSubmission()
        {
            string directory = NewDirectory();
            IJobBridge previous = HtmlToUitkRunner.BridgeOverride;
            Task<JobRunResult> first = null;
            Task<JobRunResult> queued = null;
            using (BlockingBridge blocker = new BlockingBridge())
            {
                try
                {
                    HtmlToUitkRunner.BridgeOverride = blocker;
                    first = HtmlToUitkRunner.RunAsync(new JobDocument { OutputRoot = "blocker" }, Path.Combine(directory, "first.htujob.json"), new RunOptions());
                    Assert.That(blocker.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True, "The first job must hold the runner gate before submitting the next job.");

                    RecordingBridge recorder = new RecordingBridge();
                    HtmlToUitkRunner.BridgeOverride = recorder;
                    JobDocument document = new JobDocument
                    {
                        Name = "submitted",
                        OutputRoot = "original-output",
                        ConfigOverrides = MiniJson.Parse("{\"rules\":{\"minFontSize\":14}}")
                    };
                    document.Html.Add("original.html");
                    List<string> selected = new List<string> { "selected.html" };
                    RunOptions options = new RunOptions { ValidateOnly = true, Force = true, Strict = true, OnlyFiles = selected };
                    queued = HtmlToUitkRunner.RunAsync(document, Path.Combine(directory, "queued.htujob.json"), options);

                    document.Name = "edited";
                    document.OutputRoot = "edited-output";
                    document.Html.Clear();
                    document.ConfigOverrides["rules"].Set("minFontSize", JsonValue.FromNumber(99));
                    selected[0] = "edited.html";
                    options.ValidateOnly = false;
                    options.Force = false;
                    options.Strict = false;
                    blocker.Release.Set();

                    Assert.That(Task.WaitAll(new Task[] { first, queued }, TimeSpan.FromSeconds(10)), Is.True);
                    Assert.That(queued.Result.Succeeded, Is.True);
                    Assert.That(recorder.Document.Name, Is.EqualTo("submitted"));
                    Assert.That(recorder.Document.Html, Is.EqualTo(new[] { "selected.html" }));
                    Assert.That(recorder.Document.ConfigOverrides["rules"]["minFontSize"].AsInt(), Is.EqualTo(14));
                    Assert.That(recorder.Request.OutputRoot, Is.EqualTo(GlobMatcher.Normalize(directory) + "/original-output"));
                    Assert.That(recorder.Request.ValidateOnly, Is.True);
                    Assert.That(recorder.Request.Force, Is.True);
                    Assert.That(recorder.Request.Strict, Is.True);
                    Assert.That(File.Exists(recorder.Request.JobPath), Is.False, "The submitted temporary job must still be cleaned up.");
                }
                finally
                {
                    blocker.Release.Set();
                    try
                    {
                        if (first != null)
                            first.Wait(TimeSpan.FromSeconds(10));
                        if (queued != null)
                            queued.Wait(TimeSpan.FromSeconds(10));
                    }
                    finally
                    {
                        HtmlToUitkRunner.BridgeOverride = previous;
                        foreach (string file in Directory.EnumerateFiles(directory))
                            File.Delete(file);
                        Directory.Delete(directory);
                    }
                }
            }
        }

        private static JobRunResult Pass(string bridgeName)
        {
            return new JobRunResult { BridgeName = bridgeName, Report = DiagnosticsReport.Parse("{\"verdict\":\"PASS\",\"findings\":[]}") };
        }

        private static string NewDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(), "Audit20261023NeoHtmlToUitkEditor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
