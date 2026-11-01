using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Neo.HtmlToUitk.Json;
using Neo.HtmlToUitk.Tooling;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Tests
{
    // Root runs this fixture in its isolated exclusive sequence; shared bridge/preferences are restored below.
    public sealed class Audit20261101NeoHtmlToUitkEditorDraftTests
    {
        private sealed class Submission
        {
            public JobRunRequest Request;
            public JobDocument Document;
        }

        private sealed class CaptureBridge : IJobBridge, IDisposable
        {
            public readonly ConcurrentQueue<Submission> Seen = new ConcurrentQueue<Submission>();
            public readonly ManualResetEventSlim Entered = new ManualResetEventSlim();
            private readonly ManualResetEventSlim _release = new ManualResetEventSlim(true);
            public Exception Failure;
            public string Name => "draft-regression";
            public bool IsAvailable(out string reason) { reason = null; return true; }
            public void Block() { Entered.Reset(); _release.Reset(); }
            public void Release() { _release.Set(); }

            public JobRunResult Run(JobRunRequest request)
            {
                Seen.Enqueue(new Submission { Request = request, Document = JobDocument.Parse(File.ReadAllText(request.JobPath)) });
                Entered.Set();
                if (!_release.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("Owned regression bridge was not released.");
                if (Failure != null)
                    throw Failure;
                return new JobRunResult
                {
                    BridgeName = Name, Faulted = true, ExitCode = 2,
                    Report = DiagnosticsReport.Failure("Owned global runner failure"),
                    StdOut = "owned stdout", StdErr = "owned stderr"
                };
            }

            public void Dispose() { _release.Dispose(); Entered.Dispose(); }
        }

        private HtmlToUitkWindow _window;
        private CaptureBridge _bridge;
        private IJobBridge _previousBridge;
        private string _lastJob;
        private string _cli;
        private string _directory;
        private string _assetRoot;
        private bool _watchWindowShown;

        [SetUp]
        public void SetUp()
        {
            string id = Guid.NewGuid().ToString("N");
            _directory = HtmlToUitkPaths.ProjectRoot + "/Temp/Audit20261101HtmlDraft-" + id;
            _assetRoot = "Assets/Audit20261101HtmlDraft-" + id;
            Directory.CreateDirectory(_directory);
            _lastJob = HtmlToUitkSettings.LastJobPath;
            _cli = HtmlToUitkSettings.CliPath;
            _previousBridge = HtmlToUitkRunner.BridgeOverride;
            HtmlToUitkSettings.LastJobPath = string.Empty;
            _bridge = new CaptureBridge();
            HtmlToUitkRunner.BridgeOverride = _bridge;
            _window = ScriptableObject.CreateInstance<HtmlToUitkWindow>();
            Command<object>("CreateGUI");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _bridge.Release();
                FinishWorker();
                if (_window != null)
                {
                    _window.DiscardChanges();
                    if (_watchWindowShown)
                        _window.Close();
                    else
                        UnityEngine.Object.DestroyImmediate(_window);
                }
            }
            finally
            {
                _watchWindowShown = false;
                HtmlToUitkRunner.BridgeOverride = _previousBridge;
                HtmlToUitkSettings.LastJobPath = _lastJob;
                HtmlToUitkSettings.CliPath = _cli;
                _bridge.Dispose();
                if (AssetDatabase.IsValidFolder(_assetRoot))
                    AssetDatabase.DeleteAsset(_assetRoot);
                if (Directory.Exists(_directory))
                    Directory.Delete(_directory, true);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FreshDraft_AddAndRebuild_DoesNotWriteAndKeepsOneHandler(bool addFolder)
        {
            string html = Write("sources/home.html", "<h1>Home</h1>");
            string virtualJob = HtmlToUitkPaths.ProjectRoot + "/Untitled.htujob.json";
            byte[] previousVirtual = Bytes(virtualJob);
            bool outputExisted = Directory.Exists(HtmlToUitkPaths.ProjectRoot + "/Assets/UI/Generated");
            InputListView stale = Inputs;
            stale.AddEntries(new[] { Relative(addFolder ? Path.GetDirectoryName(html) : html) });
            Command<object>("CreateGUI");
            Command<object>("CreateGUI");
            stale.AddEntries(new[] { "not-an-authored-page.html" });

            Assert.That(Inputs.Entries.Count, Is.EqualTo(1));
            Assert.That(Inputs.Files.Count, Is.EqualTo(1));
            Assert.That(Resolve(HtmlToUitkPaths.ProjectRoot, Inputs.Files[0]), Is.EqualTo(Absolute(html)));
            Assert.That(_window.hasUnsavedChanges, Is.True);
            Assert.That(Bytes(virtualJob), Is.EqualTo(previousVirtual));
            Assert.That(Directory.Exists(HtmlToUitkPaths.ProjectRoot + "/Assets/UI/Generated"), Is.EqualTo(outputExisted));
            Assert.That(Directory.GetFiles(_directory, "*.json", SearchOption.AllDirectories), Is.Empty);

            Command<object>("Run", true, false);
            FinishWorker();
            Command<object>("Poll");
            Assert.That(_bridge.Seen.Count, Is.EqualTo(1));
            Assert.That(_bridge.Seen.TryPeek(out Submission submitted), Is.True);
            Assert.That(submitted.Document.Html.Count, Is.EqualTo(1));
            Assert.That(submitted.Request.OutputRoot, Is.EqualTo(HtmlToUitkPaths.ProjectRoot + "/Assets/UI/Generated"));
            Assert.That(File.Exists(submitted.Request.JobPath), Is.False);
            Assert.That(Bytes(virtualJob), Is.EqualTo(previousVirtual));
        }

        [TestCase("explicit")]
        [TestCase("blank")]
        [TestCase("default")]
        public void DraftFirstSave_RebasesKnownPathsAndPerFileIdentitiesWithoutLosingPayload(string outputMode)
        {
            string html = Write("sources/home.html", "<h1>Home</h1>");
            string absoluteHtml = Write("absolute/other.html", "<p>Other</p>");
            string css = Write("sources/style.css", "h1 {}");
            string uss = Write("sources/pass.uss", "Label {}");
            string partial = Write("sources/header.html", "<header>Header</header>");
            string config = Write("config.json", "{}");
            JobDocument draft = ExtendedDraft();
            draft.Html.Add(Relative(html));
            draft.Html.Add(Absolute(absoluteHtml));
            draft.Html.Add(Relative(_directory) + "/sources/../sources/**/*.html");
            draft.Css.Add(Relative(css));
            draft.Uss.Add(Relative(uss));
            draft.Partials.Add(Relative(partial));
            draft.Config = Relative(config);
            draft.OutputRoot = outputMode == "blank" ? string.Empty : outputMode == "default" ? "Assets/UI/Generated" : Relative(_directory) + "/output";
            draft.Folders.Add(new KeyValuePair<string, string>("uxml", "Layouts"));
            draft.Folders.Add(new KeyValuePair<string, string>("x-custom", "Custom/Sub"));
            JsonValue value = MiniJson.Parse("{\"name\":\"Landing\",\"folder\":\"Special\",\"assemble\":\"perFile\",\"x-extension\":{\"items\":[1,null,{\"keep\":true}]}}");
            draft.PerFile = JsonValue.NewObject().Set(Relative(html), value).Set(Absolute(absoluteHtml), value)
                .Set(Relative(_directory) + "/sources/../sources/header.html", value);
            RestoreDraft(draft, true);
            JobDocument before = Document;
            EnsureAssetRoot();
            string jobs = _assetRoot + "/Jobs";
            Assert.That(AssetDatabase.CreateFolder(_assetRoot, "Jobs"), Is.Not.Empty);
            Assert.That(AssetDatabase.CreateFolder(jobs, "Nested"), Is.Not.Empty);
            string destination = HtmlToUitkPaths.ToAbsolute(jobs + "/Nested/site.htujob.json");
            ChooseSave(() => destination);

            Assert.That(Save(out string error), Is.True, error);
            JobDocument saved = JobDocument.Parse(File.ReadAllText(destination));
            string baseDirectory = HtmlToUitkPaths.DirectoryOf(destination);
            AssertTargets(before.Html, saved.Html, baseDirectory);
            AssertTargets(before.Css, saved.Css, baseDirectory);
            AssertTargets(before.Uss, saved.Uss, baseDirectory);
            AssertTargets(before.Partials, saved.Partials, baseDirectory);
            Assert.That(saved.Html[1], Is.EqualTo(Absolute(absoluteHtml)));
            Assert.That(saved.Html[2], Does.EndWith("/sources/../sources/**/*.html"));
            Assert.That(Resolve(baseDirectory, saved.Config), Is.EqualTo(Absolute(config)));
            string previousOutput = string.IsNullOrEmpty(before.OutputRoot) ? "Assets/UI/Generated" : before.OutputRoot;
            Assert.That(Resolve(baseDirectory, saved.OutputRoot), Is.EqualTo(Resolve(HtmlToUitkPaths.ProjectRoot, previousOutput)));
            string sourceKey = GlobMatcher.Normalize(Path.GetRelativePath(baseDirectory, html));
            Assert.That(MiniJson.Write(saved.PerFile[sourceKey], false), Is.EqualTo(MiniJson.Write(value, false)));
            Assert.That(MiniJson.Write(saved.PerFile[Absolute(absoluteHtml)], false), Is.EqualTo(MiniJson.Write(value, false)));
            Assert.That(MiniJson.Write(saved.PerFile[GlobMatcher.Normalize(Path.GetRelativePath(baseDirectory, partial))], false), Is.EqualTo(MiniJson.Write(value, false)));
            Assert.That(saved.PerFile.Has(Relative(html)), Is.False);
            Assert.That(Resolve(baseDirectory, sourceKey), Is.EqualTo(Absolute(html)));
            Assert.That(Resolve(Resolve(baseDirectory, saved.OutputRoot), saved.PerFile[sourceKey]["folder"].AsString() + "/" + saved.PerFile[sourceKey]["name"].AsString() + ".uxml"),
                Is.EqualTo(Resolve(Resolve(HtmlToUitkPaths.ProjectRoot, previousOutput), "Special/Landing.uxml")));
            Assert.That(saved.Folders, Is.EqualTo(before.Folders));
            AssertExtensions(before, saved);
            string assetPath = HtmlToUitkPaths.ToProjectRelative(destination);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            HtmlToUitkJob imported = AssetDatabase.LoadAssetAtPath<HtmlToUitkJob>(assetPath);
            Assert.That(imported, Is.Not.Null);
            Assert.That(imported.ToDocument().PerFile.Has(sourceKey), Is.True);
            Assert.That(_window.hasUnsavedChanges, Is.False);
        }

        [TestCase("collision")]
        [TestCase("dotCollision")]
        [TestCase("shape")]
        [TestCase("value")]
        [TestCase("field")]
        [TestCase("key")]
        public void DraftFirstSave_InvalidPerFileOrNormalizedCollisionFailsBeforeOverwriting(string problem)
        {
            JobDocument draft = ExtendedDraft();
            draft.Html.Add("pages/home.html");
            JsonValue value = JsonValue.NewObject().Set("name", JsonValue.FromString("Landing"));
            draft.PerFile = JsonValue.NewObject().Set("pages/home.html", value);
            if (problem == "collision") draft.PerFile.Set("pages\\home.html", value);
            if (problem == "dotCollision") draft.PerFile.Set("pages/sub/../home.html", value);
            if (problem == "shape") draft.PerFile = JsonValue.NewArray();
            if (problem == "value") draft.PerFile.Set("pages/other.html", JsonValue.FromNumber(42));
            if (problem == "field") value.Set("folder", JsonValue.FromNumber(42));
            if (problem == "key") draft.PerFile.Set("pages/*.html", value);
            RestoreDraft(ExtendedDraft(), true);
            // Invalid PerFile shapes are deliberately omitted by JobDocument.ToJson; validate the live input before that clone.
            Document.Html.Add("pages/home.html");
            Document.PerFile = draft.PerFile;
            string destination = Write("jobs/existing.htujob.json", "existing destination must survive");
            byte[] baseline = File.ReadAllBytes(destination);
            string originalValue = MiniJson.Write(Document.PerFile, false);
            ChooseSave(() => destination);

            Assert.That(Save(out string error), Is.False);
            Assert.That(error, Does.Contain("output.perFile"));
            Assert.That(File.ReadAllBytes(destination), Is.EqualTo(baseline));
            Assert.That(MiniJson.Write(Document.PerFile, false), Is.EqualTo(originalValue));
            Assert.That(_window.hasUnsavedChanges, Is.True);
            Assert.That(PathFromWindow(), Is.Empty);
            Assert.That(HtmlToUitkSettings.LastJobPath, Is.Empty);
        }

        [Test]
        public void DraftFirstSave_CancelFailureAndReplacementSaveCancelKeepTheLiveOwner()
        {
            JobDocument draft = ExtendedDraft();
            draft.Html.Add(Relative(Write("source.html", "<p>Owned</p>")));
            RestoreDraft(draft, true);
            string before = Document.ToJson();
            string existing = Write("jobs/existing.htujob.json", "unchanged");
            byte[] baseline = File.ReadAllBytes(existing);
            ChooseSave(() => string.Empty);
            Assert.That(Save(out string cancelled), Is.False);
            Assert.That(cancelled, Does.Contain("cancelled"));
            JobDocument replacement = new JobDocument { Name = "replacement", OutputRoot = "Generated" };
            Assert.That(Command<bool>("TryReplaceDocument", replacement, existing, null, false, new Func<int>(() => 0)), Is.False);

            ChooseSave(() => existing + "/cannot-write.htujob.json");
            Assert.That(Save(out string failure), Is.False);
            Assert.That(failure, Does.Contain("Cannot save"));
            Assert.That(Document.ToJson(), Is.EqualTo(before));
            Assert.That(PathFromWindow(), Is.Empty);
            Assert.That(_window.hasUnsavedChanges, Is.True);
            Assert.That(File.ReadAllBytes(existing), Is.EqualTo(baseline));
            Assert.That(HtmlToUitkSettings.LastJobPath, Is.Empty);
        }

        [Test]
        public void AlreadySavedJobSave_DoesNotRebasePerFileKeys()
        {
            JobDocument saved = ExtendedDraft();
            saved.Html.Add("pages/home.html");
            saved.PerFile = JsonValue.NewObject().Set("pages/home.html", JsonValue.NewObject().Set("folder", JsonValue.FromString("Special")));
            string path = Write("jobs/saved.htujob.json", saved.ToJson());
            Assert.That(Command<bool>("LoadJson", path, null, null), Is.True);
            Inputs.AddEntries(new[] { "pages/other.html" });
            ChooseSave(() => { Assert.Fail("An ordinary saved job must not ask for a draft location."); return null; });
            Assert.That(Save(out string error), Is.True, error);
            JobDocument after = JobDocument.Parse(File.ReadAllText(path));
            Assert.That(MiniJson.Write(after.PerFile, false), Is.EqualTo(MiniJson.Write(saved.PerFile, false)));
            Assert.That(after.Html[0], Is.EqualTo(saved.Html[0]));
            AssertExtensions(saved, after);
        }

        [Test]
        public void SerializedDirtyDraft_ReconstructsWithoutSavingOrLosingEdits()
        {
            RestoreDraft(ExtendedDraft(), false);
            Inputs.AddEntries(new[] { Relative(Write("source.html", "<p>Unsaved</p>")) });
            string before = Document.ToJson();
            string serialized = EditorJsonUtility.ToJson(_window);
            _window.DiscardChanges();
            UnityEngine.Object.DestroyImmediate(_window);
            _window = ScriptableObject.CreateInstance<HtmlToUitkWindow>();
            EditorJsonUtility.FromJsonOverwrite(serialized, _window);
            Command<object>("CreateGUI");
            Command<object>("CreateGUI");
            Assert.That(Document.ToJson(), Is.EqualTo(before));
            Assert.That(_window.hasUnsavedChanges, Is.True);
            Assert.That(PathFromWindow(), Is.Empty);
            Assert.That(Directory.GetFiles(_directory, "*.json", SearchOption.AllDirectories), Is.Empty);
            string path = _directory + "/restored.htujob.json";
            ChooseSave(() => path);
            Assert.That(Save(out string error), Is.True, error);
            AssertExtensions(JobDocument.Parse(before), JobDocument.Parse(File.ReadAllText(path)));
        }

        [Test]
        public void OpeningMissingJsonRegularAsset_DoesNotWriteAndExplicitSaveTouchesOnlyItsOwner()
        {
            EnsureAssetRoot();
            string ownedPath = _assetRoot + "/owned.asset";
            string foreignPath = _assetRoot + "/foreign.asset";
            string json = _directory + "/owned.htujob.json";
            HtmlToUitkJob job = ScriptableObject.CreateInstance<HtmlToUitkJob>();
            job.FromDocument(ExtendedDraft());
            job.SetJsonPath(json);
            AssetDatabase.CreateAsset(job, ownedPath);
            HtmlToUitkConfig foreign = ScriptableObject.CreateInstance<HtmlToUitkConfig>();
            AssetDatabase.CreateAsset(foreign, foreignPath);
            AssetDatabase.SaveAssetIfDirty(job);
            AssetDatabase.SaveAssetIfDirty(foreign);
            byte[] ownedBytes = File.ReadAllBytes(HtmlToUitkPaths.ToAbsolute(ownedPath));
            byte[] foreignBytes = File.ReadAllBytes(HtmlToUitkPaths.ToAbsolute(foreignPath));
            foreign.configJsonPath = "foreign-unsaved.json";
            EditorUtility.SetDirty(foreign);

            Command<object>("LoadAsset", job);
            Command<object>("CreateGUI");
            Assert.That(File.Exists(json), Is.False);
            Assert.That(File.ReadAllBytes(HtmlToUitkPaths.ToAbsolute(ownedPath)), Is.EqualTo(ownedBytes));
            string extra = Write("extra.html", "<p>Extra</p>");
            Inputs.AddEntries(new[] { "extra.html" });
            Assert.That(Save(out string error), Is.True, error);
            AssetDatabase.ImportAsset(ownedPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            HtmlToUitkJob persisted = AssetDatabase.LoadAssetAtPath<HtmlToUitkJob>(ownedPath);
            Assert.That(persisted.html.Count, Is.EqualTo(1));
            Assert.That(Resolve(HtmlToUitkPaths.DirectoryOf(json), persisted.html.Single()), Is.EqualTo(Absolute(extra)));
            AssertExtensions(ExtendedDraft(), JobDocument.Parse(File.ReadAllText(json)));
            Assert.That(File.ReadAllBytes(HtmlToUitkPaths.ToAbsolute(foreignPath)), Is.EqualTo(foreignBytes));
            Assert.That(foreign.configJsonPath, Is.EqualTo("foreign-unsaved.json"));
            Assert.That(EditorUtility.IsDirty(foreign), Is.True);
            Undo.ClearUndo(job);
        }

        [TestCase("rebuild")]
        [TestCase("replace")]
        [TestCase("save")]
        public void PendingRun_UiRebuildAndDocumentChangesKeepSubmissionOwnership(string transition)
        {
            JobDocument draft = ExtendedDraft();
            draft.Html.Add(Relative(Write("source.html", "<p>Run</p>")));
            draft.OutputRoot = Relative(_directory) + "/submitted-output";
            RestoreDraft(draft, true);
            _bridge.Block();
            Command<object>("Run", true, false);
            Assert.That(_bridge.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(_bridge.Seen.TryPeek(out Submission submitted), Is.True);
            Command<object>("CreateGUI");
            if (transition == "replace")
            {
                JobDocument replacement = new JobDocument { Name = "Replacement", OutputRoot = "different-output" };
                replacement.Html.Add("replacement.html");
                Assert.That(Command<bool>("TryReplaceDocument", replacement, _directory + "/replacement.htujob.json", null, false, new Func<int>(() => 2)), Is.True);
            }
            if (transition == "save")
            {
                ChooseSave(() => _directory + "/saved.htujob.json");
                Assert.That(Save(out string error), Is.True, error);
            }

            _bridge.Release();
            FinishWorker();
            Command<object>("CreateGUI");
            Command<object>("Run", true, false); // A completed task still belongs to Poll.
            FinishWorker();
            Assert.That(_bridge.Seen.Count, Is.EqualTo(1));
            Command<object>("Poll");
            Assert.That(submitted.Request.OutputRoot, Is.EqualTo(_directory + "/submitted-output"));
            Assert.That(File.Exists(submitted.Request.JobPath), Is.False);
            Assert.That(HtmlToUitkRunner.IsRunning, Is.False);
            if (transition == "rebuild")
                AssertGlobalFailure("Owned global runner failure");
            else
            {
                Assert.That(Findings, Is.Empty);
                Assert.That(_window.rootVisualElement.Q<Label>(className: "htu-note").text, Does.Contain("Previous job finished"));
                if (transition == "replace") Assert.That(Inputs.Entries, Is.EqualTo(new[] { "replacement.html" }));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BridgeException_FaultsRunnerTaskCleansTemporaryJobAndShowsItsExactError(bool throwsCancellationException)
        {
            Inputs.AddEntries(new[] { Relative(Write("source.html", "<p>Run</p>")) });
            _bridge.Failure = throwsCancellationException ? (Exception)new OperationCanceledException("Owned cancelled run") : new InvalidOperationException("Owned faulted run");
            Command<object>("Run", true, false);
            Task<JobRunResult> task = Field<Task<JobRunResult>>("_task");
            FinishWorker();
            Assert.That(task.Status, Is.EqualTo(TaskStatus.Faulted), "Throwing from this runner does not request cancellation of its task.");
            Assert.That(task.IsCanceled, Is.False);
            Assert.That(task.Exception.GetBaseException(), Is.SameAs(_bridge.Failure));
            Command<object>("Poll");
            AssertGlobalFailure(_bridge.Failure.Message);
            Assert.That(Findings.Single(finding => finding.Rule == "E000").Message, Is.EqualTo(_bridge.Failure.Message));
            Assert.That(_bridge.Seen.TryPeek(out Submission submitted), Is.True);
            Assert.That(File.Exists(submitted.Request.JobPath), Is.False);
            Assert.That(HtmlToUitkRunner.IsRunning, Is.False);
        }

        [Test]
        public void CanceledTaskCompletion_PollPublishesTheDistinctCancellationDiagnosticAndRestoresUi()
        {
            Inputs.AddEntries(new[] { Relative(Write("source.html", "<p>Completion</p>")) });
            // Exercise completion only: this does not cancel a runner, CLI process or an in-flight conversion.
            Task<JobRunResult> cancelled = Task.FromCanceled<JobRunResult>(new CancellationToken(true));
            SetField("_taskSource", Document);
            SetField("_task", cancelled);
            ProgressBar progress = _window.rootVisualElement.Q<ProgressBar>();
            Button convert = _window.rootVisualElement.Query<Button>().ToList().Single(button => button.text == "Convert All");
            progress.style.display = DisplayStyle.Flex;
            convert.SetEnabled(false);
            Assert.That(cancelled.Status, Is.EqualTo(TaskStatus.Canceled));
            Assert.That(cancelled.IsCanceled, Is.True);
            Assert.That(cancelled.IsFaulted, Is.False);

            Command<object>("Poll");

            Assert.That(Field<Task<JobRunResult>>("_task"), Is.Null);
            Assert.That(progress.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(convert.enabledSelf, Is.True);
            Label status = _window.rootVisualElement.Q<Label>(className: "htu-status");
            Assert.That(status.text, Does.StartWith("FAIL:"));
            Assert.That(status.ClassListContains("htu-status--fail"), Is.True);
            Assert.That(_window.hasUnsavedChanges, Is.True);
            Assert.That(_bridge.Seen, Is.Empty);
            AssertGlobalFailure("The run was cancelled.");
            Assert.That(Findings.Single(finding => finding.Rule == "E000").Message, Is.EqualTo("The run was cancelled."));
        }

        [Test]
        public void RunnerStartupFailure_IsVisibleWithoutSubmittingAJob()
        {
            Inputs.AddEntries(new[] { Relative(Write("source.html", "<p>Run</p>")) });
            HtmlToUitkRunner.BridgeOverride = null;
            HtmlToUitkSettings.CliPath = "invalid\0/path.dll";
            Command<object>("Run", true, false);
            AssertGlobalFailure("Cannot start runner");
            Assert.That(_bridge.Seen, Is.Empty);
        }

        [UnityTest]
        public IEnumerator DraftWatch_RebuildAndFirstSaveRunOnceAndIgnoreOutputAndUnrelatedFiles()
        {
            // BaseField.value sends ChangeEvent only on an attached panel in Unity 6.3.
            // Reuse the existing WatchDependencies fixture's test-owned window pattern.
            _watchWindowShown = true;
            _window.ShowUtility();
            double readyDeadline = EditorApplication.timeSinceStartup + 5;
            while (_window.rootVisualElement.panel == null && EditorApplication.timeSinceStartup < readyDeadline)
                yield return null;
            Assert.That(_window.rootVisualElement.panel, Is.Not.Null,
                "Watch regression requires its own attached EditorWindow in Root's graphical batch.");

            string source = Write("sources/home.html", "<p>Watch</p>");
            string output = Write("sources/output/generated.uss", "Label {}");
            string unrelated = Write("sources/unrelated.css", "Label {}");
            JobDocument draft = ExtendedDraft();
            draft.Html.Add(Relative(source));
            draft.OutputRoot = Relative(_directory) + "/sources/output";
            draft.WatchEnabled = true;
            draft.DebounceMs = 20;
            RestoreDraft(draft, true);
            HtmlToUitkWatcher previous = Field<HtmlToUitkWatcher>("_watcher");
            Assert.That(previous, Is.Not.Null);
            Command<object>("CreateGUI");
            HtmlToUitkWatcher current = Field<HtmlToUitkWatcher>("_watcher");
            Assert.That(current, Is.Not.Null);
            Assert.That(current, Is.Not.SameAs(previous));
            Assert.That(previous.WatchedDirectories, Is.Zero);
            previous.Notify(source);
            current.Notify(source);
            current.Notify(source);
            yield return AwaitRuns(1);
            AssertGlobalFailure("Owned global runner failure");

            string css = Write("new-styles/main.css", "p {}");
            TextField cssField = _window.rootVisualElement.Query<TextField>().ToList().Single(field => field.label == "CSS (one per line)");
            Assert.That(cssField.panel, Is.Not.Null,
                "CSS must be attached before assigning value so the native ChangeEvent edits the document.");
            HtmlToUitkWatcher beforeCss = Field<HtmlToUitkWatcher>("_watcher");
            Assert.That(beforeCss, Is.Not.Null);
            Assert.That(Document.Css, Is.Empty);
            cssField.value = Relative(css);
            Assert.That(Document.Css, Is.EqualTo(new[] { Relative(css) }), "The field ChangeEvent must update the live draft immediately.");
            Assert.That(_window.hasUnsavedChanges, Is.True);
            HtmlToUitkWatcher editedWatcher = Field<HtmlToUitkWatcher>("_watcher");
            Assert.That(editedWatcher, Is.Not.Null);
            Assert.That(editedWatcher, Is.Not.SameAs(beforeCss));
            Assert.That(beforeCss.WatchedDirectories, Is.Zero);
            Assert.That(editedWatcher.WatchedDirectories, Is.GreaterThan(0));
            Directory.CreateDirectory(_directory + "/jobs/nested");
            string savedPath = _directory + "/jobs/nested/watch.htujob.json";
            ChooseSave(() => savedPath);
            Assert.That(Save(out string error), Is.True, error);
            Assert.That(PathFromWindow(), Is.EqualTo(savedPath));
            Assert.That(_window.hasUnsavedChanges, Is.False);
            HtmlToUitkWatcher savedWatcher = Field<HtmlToUitkWatcher>("_watcher");
            Assert.That(savedWatcher, Is.Not.Null);
            Assert.That(savedWatcher, Is.Not.SameAs(editedWatcher));
            Assert.That(editedWatcher.WatchedDirectories, Is.Zero);
            Command<object>("CreateGUI");
            current = Field<HtmlToUitkWatcher>("_watcher");
            Assert.That(current, Is.Not.Null);
            Assert.That(current, Is.Not.SameAs(savedWatcher));
            Assert.That(savedWatcher.WatchedDirectories, Is.Zero);
            Assert.That(current.WatchedDirectories, Is.GreaterThan(0));
            Assert.That(current.DebounceSeconds, Is.EqualTo(0.02));
            Assert.That(Document.WatchEnabled, Is.True);
            Assert.That(Document.DebounceMs, Is.EqualTo(20));
            string savedDirectory = HtmlToUitkPaths.DirectoryOf(savedPath);
            Assert.That(Resolve(savedDirectory, Document.Css.Single()), Is.EqualTo(Absolute(css)));
            JobDocument persisted = JobDocument.Parse(File.ReadAllText(savedPath));
            Assert.That(Resolve(savedDirectory, persisted.Css.Single()), Is.EqualTo(Absolute(css)));
            Assert.That(persisted.WatchEnabled, Is.True);
            Assert.That(persisted.DebounceMs, Is.EqualTo(20));
            Assert.That(JobInputPaths.IsInput(Document, savedDirectory, css), Is.True);
            Assert.That(JobInputPaths.IsInput(Document, savedDirectory, output), Is.False);
            Assert.That(JobInputPaths.IsInput(Document, savedDirectory, unrelated), Is.False);
            previous.Notify(source);
            beforeCss.Notify(css);
            editedWatcher.Notify(css);
            savedWatcher.Notify(css);
            current.Notify(output);
            current.Notify(unrelated);
            yield return QuietPeriod();
            Assert.That(_bridge.Seen.Count, Is.EqualTo(1));
            current.Notify(css);
            current.Notify(css);
            yield return AwaitRuns(2);
            yield return QuietPeriod();
            Assert.That(_bridge.Seen.Count, Is.EqualTo(2));
            Submission last = _bridge.Seen.Last();
            Assert.That(Resolve(HtmlToUitkPaths.DirectoryOf(last.Request.JobPath), last.Document.Css.Single()), Is.EqualTo(Absolute(css)));
            Assert.That(File.Exists(last.Request.JobPath), Is.False);
            AssertGlobalFailure("Owned global runner failure");
        }

        private InputListView Inputs => _window.rootVisualElement.Q<InputListView>();
        private JobDocument Document => Field<JobDocument>("_doc");
        private IEnumerable<Finding> Findings => _window.rootVisualElement.Q<FindingsView>().Q<ListView>().itemsSource.Cast<Finding>();

        private void RestoreDraft(JobDocument document, bool dirty)
        {
            SetField("_doc", null);
            SetField("_jobPath", string.Empty);
            SetField("_documentJson", document.ToJson());
            SetField("_dirty", dirty);
            Command<object>("CreateGUI");
        }

        private void ChooseSave(Func<string> choose) { SetField("_chooseDraftSavePath", choose); }

        private bool Save(out string error)
        {
            object[] arguments = { null };
            bool saved = Command<bool>("TrySave", arguments);
            error = arguments[0] as string;
            return saved;
        }

        private string PathFromWindow()
        {
            using (SerializedObject serialized = new SerializedObject(_window))
            {
                serialized.Update();
                SerializedProperty path = serialized.FindProperty("_jobPath");
                Assert.That(path, Is.Not.Null, "The window must expose its serialized job path.");
                Assert.That(path.propertyType, Is.EqualTo(SerializedPropertyType.String));
                return path.stringValue;
            }
        }

        private void AssertGlobalFailure(string message)
        {
            Inputs.Query<ListView>().ToList().Single(list => list.selectionType == SelectionType.Multiple).SetSelection(0);
            Assert.That(Findings.Any(finding => finding.File == string.Empty && finding.Message.IndexOf(message, StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
            Command<object>("CreateGUI");
            Assert.That(Findings.Any(finding => finding.Message.IndexOf(message, StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
        }

        private void FinishWorker()
        {
            Task task = Field<Task<JobRunResult>>("_task");
            if (task == null) return;
            try { Assert.That(task.Wait(TimeSpan.FromSeconds(10)), Is.True, "Owned runner did not finish."); }
            catch (AggregateException) { Assert.That(task.IsCompleted, Is.True); }
        }

        private IEnumerator AwaitRuns(int count)
        {
            double deadline = EditorApplication.timeSinceStartup + 5;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                Task task = Field<Task<JobRunResult>>("_task");
                if (task != null && task.IsCompleted) Command<object>("Poll");
                if (_bridge.Seen.Count >= count && Field<Task<JobRunResult>>("_task") == null) break;
                yield return null;
            }
            HtmlToUitkWatcher watcher = Field<HtmlToUitkWatcher>("_watcher");
            Task pending = Field<Task<JobRunResult>>("_task");
            string state = $"panel={_window.rootVisualElement.panel != null}; watch={Document.WatchEnabled}; debounceMs={Document.DebounceMs}; "
                + $"directories={(watcher == null ? -1 : watcher.WatchedDirectories)}; css=[{string.Join(" | ", Document.Css)}]; "
                + $"task={(pending == null ? "none" : pending.Status.ToString())}; runnerBusy={HtmlToUitkRunner.IsRunning}";
            Assert.That(_bridge.Seen.Count, Is.EqualTo(count), state);
            Assert.That(pending, Is.Null, "Run was not collected before its deadline. " + state);
        }

        private static IEnumerator QuietPeriod()
        {
            double deadline = EditorApplication.timeSinceStartup + 0.2;
            while (EditorApplication.timeSinceStartup < deadline) yield return null;
        }

        private static JobDocument ExtendedDraft()
        {
            return JobDocument.Parse("{\"schema\":1,\"name\":\"Owned draft\",\"inputs\":{\"html\":[],\"x-input\":{\"keep\":true}},\"output\":{\"root\":\"\",\"x-output\":[1,2]},\"watch\":{\"enabled\":false,\"x-watch\":\"retained\"},\"x-top\":{\"nested\":[null,false,3]}}");
        }

        private static void AssertExtensions(JobDocument before, JobDocument after)
        {
            JsonValue original = before.ToValue();
            JsonValue saved = after.ToValue();
            Assert.That(MiniJson.Write(saved["x-top"], false), Is.EqualTo(MiniJson.Write(original["x-top"], false)));
            foreach (string section in new[] { "inputs", "output", "watch" })
            {
                string key = section == "inputs" ? "x-input" : section == "output" ? "x-output" : "x-watch";
                Assert.That(MiniJson.Write(saved[section][key], false), Is.EqualTo(MiniJson.Write(original[section][key], false)));
            }
        }

        private static void AssertTargets(IReadOnlyList<string> before, IReadOnlyList<string> after, string directory)
        {
            Assert.That(after.Count, Is.EqualTo(before.Count));
            for (int i = 0; i < before.Count; i++)
            {
                if (GlobMatcher.HasWildcards(before[i]))
                {
                    string originalPrefix = GlobMatcher.StaticPrefix(before[i]);
                    string savedPrefix = GlobMatcher.StaticPrefix(after[i]);
                    Assert.That(Resolve(directory, savedPrefix), Is.EqualTo(Resolve(HtmlToUitkPaths.ProjectRoot, originalPrefix)));
                    Assert.That(Directory.Exists(Resolve(directory, savedPrefix)), Is.True);
                    Assert.That(after[i].Substring(savedPrefix.Length), Is.EqualTo(before[i].Substring(originalPrefix.Length)));
                }
                else
                    Assert.That(Resolve(directory, after[i]), Is.EqualTo(Resolve(HtmlToUitkPaths.ProjectRoot, before[i])));
            }
        }

        private void EnsureAssetRoot()
        {
            if (!AssetDatabase.IsValidFolder(_assetRoot))
                Assert.That(AssetDatabase.CreateFolder("Assets", Path.GetFileName(_assetRoot)), Is.Not.Empty);
        }

        private string Write(string relative, string contents)
        {
            string path = _directory + "/" + relative;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, contents);
            return path;
        }

        private static byte[] Bytes(string path) => File.Exists(path) ? File.ReadAllBytes(path) : null;
        private static string Relative(string path) => GlobMatcher.Normalize(Path.GetRelativePath(HtmlToUitkPaths.ProjectRoot, path));
        private static string Absolute(string path) => GlobMatcher.Normalize(Path.GetFullPath(path));
        private static string Resolve(string directory, string entry) => Absolute(InputExpander.Combine(directory, entry));
        private T Field<T>(string name) => (T)typeof(HtmlToUitkWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_window);
        private void SetField(string name, object value) => typeof(HtmlToUitkWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_window, value);

        // Drives existing commands and the sole picker seam; assertions observe documents, files, submitted requests and visible findings.
        private T Command<T>(string name, params object[] arguments)
        {
            MethodInfo method = typeof(HtmlToUitkWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            return (T)method.Invoke(_window, arguments);
        }
    }
}
