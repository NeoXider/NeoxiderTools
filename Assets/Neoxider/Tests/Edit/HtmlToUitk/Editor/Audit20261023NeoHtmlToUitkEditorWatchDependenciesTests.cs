using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Neo.HtmlToUitk.Tooling;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class Audit20261023NeoHtmlToUitkEditorWatchDependenciesTests
    {
        private sealed class CaptureBridge : IJobBridge
        {
            public readonly ConcurrentQueue<JobDocument> Seen = new ConcurrentQueue<JobDocument>();
            public string Name => "watch-source-probe";
            public bool IsAvailable(out string reason) { reason = null; return true; }
            public JobRunResult Run(JobRunRequest request)
            {
                Seen.Enqueue(JobDocument.Parse(File.ReadAllText(request.JobPath)));
                return new JobRunResult { BridgeName = Name, Faulted = true, ExitCode = 2, Report = DiagnosticsReport.Failure("Test bridge writes no generated assets.") };
            }
        }

        [UnityTest]
        public IEnumerator EditingCssInTheWindowWatchesTheNewExternalDirectoryAndIgnoresUnrelatedOutput()
        {
            string directory = Path.Combine(Path.GetTempPath(), "Audit20261023NeoHtmlToUitkEditor-watch-" + Guid.NewGuid().ToString("N"));
            string oldDirectory = Path.Combine(directory, "old");
            string newDirectory = Path.Combine(directory, "new");
            string jobDirectory = Path.Combine(directory, "job");
            Directory.CreateDirectory(oldDirectory);
            Directory.CreateDirectory(newDirectory);
            Directory.CreateDirectory(jobDirectory);
            string oldCss = Path.Combine(oldDirectory, "main.css");
            string newCss = Path.Combine(newDirectory, "main.css");
            File.WriteAllText(oldCss, "button {}");
            File.WriteAllText(newCss, "button {}");
            string previousLastJob = HtmlToUitkSettings.LastJobPath;
            IJobBridge previousBridge = HtmlToUitkRunner.BridgeOverride;
            HtmlToUitkWindow window = null;
            CaptureBridge bridge = new CaptureBridge();
            try
            {
                HtmlToUitkSettings.LastJobPath = string.Empty;
                HtmlToUitkRunner.BridgeOverride = bridge;
                window = ScriptableObject.CreateInstance<HtmlToUitkWindow>();
                window.ShowUtility();
                double readyDeadline = EditorApplication.timeSinceStartup + 5;
                while (window.rootVisualElement.Query<TextField>().ToList().Count == 0 && EditorApplication.timeSinceStartup < readyDeadline)
                    yield return null;

                JobDocument document = new JobDocument { OutputRoot = "Generated", WatchEnabled = true, DebounceMs = 100 };
                document.Html.Add("source.html");
                document.Css.Add(GlobMatcher.Normalize(oldCss));
                document.Uss.Add("**/*.uss");
                MethodInfo replace = typeof(HtmlToUitkWindow).GetMethod("TryReplaceDocument", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That((bool)replace.Invoke(window, new object[] { document, Path.Combine(jobDirectory, "main.htujob.json"), null, false, null }), Is.True);
                TextField css = window.rootVisualElement.Query<TextField>().ToList().Single(field => field.label == "CSS (one per line)");
                css.value = GlobMatcher.Normalize(newCss);
                Assert.That(window.hasUnsavedChanges, Is.True, "Editing CSS must mark the native window unsaved.");
                File.WriteAllText(newCss, "button { color: red; }");
                double deadline = EditorApplication.timeSinceStartup + 5;
                while (bridge.Seen.IsEmpty && EditorApplication.timeSinceStartup < deadline)
                    yield return null;

                Assert.That(bridge.Seen.TryPeek(out JobDocument submitted), Is.True);
                Assert.That(submitted.Css, Is.EqualTo(new[] { GlobMatcher.Normalize(newCss) }));
                while (HtmlToUitkRunner.IsRunning && EditorApplication.timeSinceStartup < deadline)
                    yield return null;
                yield return null;
                int initialCount = bridge.Seen.Count;
                File.WriteAllText(oldCss, "old dependency changed");
                File.WriteAllText(Path.Combine(newDirectory, "unrelated.css"), "unrelated {}");
                string generated = Path.Combine(jobDirectory, "Generated");
                Directory.CreateDirectory(generated);
                File.WriteAllText(Path.Combine(generated, "generated.uss"), "generated {}");
                double quietDeadline = EditorApplication.timeSinceStartup + 0.3;
                while (EditorApplication.timeSinceStartup < quietDeadline)
                    yield return null;
                Assert.That(bridge.Seen.Count, Is.EqualTo(initialCount));
                Assert.That(initialCount, Is.EqualTo(1));
            }
            finally
            {
                try
                {
                    if (window != null)
                    {
                        try
                        {
                            Task pending = (Task)typeof(HtmlToUitkWindow).GetField("_task", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
                            if (pending != null)
                                Assert.That(pending.Wait(TimeSpan.FromSeconds(5)), Is.True, "The fixture's submitted bridge work did not finish.");
                        }
                        finally
                        {
                            window.DiscardChanges();
                            window.Close();
                        }
                    }
                }
                finally
                {
                    HtmlToUitkRunner.BridgeOverride = previousBridge;
                    HtmlToUitkSettings.LastJobPath = previousLastJob;
                    // All paths belong to this fixture; no package or sample files are used.
                    foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)) File.Delete(file);
                    foreach (string child in Directory.EnumerateDirectories(jobDirectory)) Directory.Delete(child);
                    Directory.Delete(oldDirectory);
                    Directory.Delete(newDirectory);
                    Directory.Delete(jobDirectory);
                    Directory.Delete(directory);
                }
            }
        }

        [UnityTest]
        public IEnumerator UssImportAndRenameAwayPublishDeduplicatedOldInputPaths()
        {
            string folder = "Assets/Audit20261023NeoHtmlToUitkEditor-" + Guid.NewGuid().ToString("N");
            string source = folder + "/skin.uss";
            string target = folder + "/skin.txt";
            List<IReadOnlyList<string>> batches = new List<IReadOnlyList<string>>();
            Action<IReadOnlyList<string>> collect = paths => batches.Add(new List<string>(paths));
            try
            {
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
                GeneratedAssetPostprocessor.InputsChanged += collect;
                File.WriteAllText(HtmlToUitkPaths.ToAbsolute(source), "Label { color: red; }");
                AssetDatabase.ImportAsset(source, ImportAssetOptions.ForceSynchronousImport);
                double deadline = EditorApplication.timeSinceStartup + 5;
                while (!batches.Any(batch => batch.Contains(HtmlToUitkPaths.ToAbsolute(source))) && EditorApplication.timeSinceStartup < deadline)
                    yield return null;
                Assert.That(batches.Any(batch => batch.Contains(HtmlToUitkPaths.ToAbsolute(source))), Is.True);
                batches.Clear();

                Assert.That(AssetDatabase.MoveAsset(source, target), Is.Empty);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                deadline = EditorApplication.timeSinceStartup + 5;
                while (!batches.Any(batch => batch.Contains(HtmlToUitkPaths.ToAbsolute(source))) && EditorApplication.timeSinceStartup < deadline)
                    yield return null;
                Assert.That(batches.Any(batch => batch.Contains(HtmlToUitkPaths.ToAbsolute(source))), Is.True);
                foreach (IReadOnlyList<string> batch in batches)
                {
                    Assert.That(new HashSet<string>(batch, StringComparer.OrdinalIgnoreCase).Count, Is.EqualTo(batch.Count));
                    Assert.That(batch, Does.Not.Contain(HtmlToUitkPaths.ToAbsolute(target)));
                }
            }
            finally
            {
                GeneratedAssetPostprocessor.InputsChanged -= collect;
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
