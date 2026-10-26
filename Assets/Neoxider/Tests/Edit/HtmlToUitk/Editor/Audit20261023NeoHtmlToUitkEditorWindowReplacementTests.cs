using System;
using System.IO;
using System.Reflection;
using Neo.HtmlToUitk.Json;
using Neo.HtmlToUitk.Tooling;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class Audit20261023NeoHtmlToUitkEditorWindowReplacementTests
    {
        private HtmlToUitkWindow _window;
        private string _directory;
        private string _lastJob;

        [SetUp]
        public void SetUp()
        {
            _lastJob = HtmlToUitkSettings.LastJobPath;
            _directory = Path.Combine(Path.GetTempPath(), "Audit20261023NeoHtmlToUitkEditor-window-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _window = ScriptableObject.CreateInstance<HtmlToUitkWindow>();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_window != null)
                {
                    _window.DiscardChanges();
                    UnityEngine.Object.DestroyImmediate(_window);
                }
            }
            finally
            {
                HtmlToUitkSettings.LastJobPath = _lastJob;
                foreach (string file in Directory.EnumerateFiles(_directory)) File.Delete(file);
                foreach (string directory in Directory.EnumerateDirectories(_directory)) Directory.Delete(directory);
                Directory.Delete(_directory);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancelRetainsEditedDocumentAndPathAndDoesNotWriteANewJob(bool createNew)
        {
            string first = Path.Combine(_directory, "first.htujob.json");
            string second = Path.Combine(_directory, "second.htujob.json");
            File.WriteAllText(first, Document("original").ToJson());
            Seed(Document("edited"), first);

            bool replaced = Call<bool>("TryReplaceDocument", Document("replacement"), second, null, createNew, new Func<int>(() => 1));

            Assert.That(replaced, Is.False);
            Assert.That(_window.hasUnsavedChanges, Is.True);
            Assert.That(File.Exists(second), Is.False);
            Assert.That(ReadName(first), Is.EqualTo("original"));
            _window.SaveChanges();
            Assert.That(ReadName(first), Is.EqualTo("edited"), "Saving after Cancel must still target the previous document and file.");
            Assert.That(File.Exists(second), Is.False);
        }

        [Test]
        public void FailedSaveBlocksNewAndCloseSaveWithoutClearingUnsavedState()
        {
            string blocked = Path.Combine(_directory, "blocked.htujob.json");
            string second = Path.Combine(_directory, "second.htujob.json");
            Directory.CreateDirectory(blocked);
            Seed(Document("retained"), blocked);

            Assert.That(Call<bool>("TryReplaceDocument", Document("replacement"), second, null, true, new Func<int>(() => 0)), Is.False);
            Assert.That(_window.hasUnsavedChanges, Is.True);
            Assert.That(File.Exists(second), Is.False);
            Assert.Throws<IOException>(() => _window.SaveChanges());
            Assert.That(_window.hasUnsavedChanges, Is.True);

            Directory.Delete(blocked);
            _window.SaveChanges();
            Assert.That(ReadName(blocked), Is.EqualTo("retained"));
            Assert.That(_window.hasUnsavedChanges, Is.False);
            Assert.That(File.Exists(second), Is.False);
        }

        [Test]
        public void SaveThenReloadUsesSavedEditsInsteadOfTheCandidateReadBeforeTheDecision()
        {
            string path = Path.Combine(_directory, "first.htujob.json");
            File.WriteAllText(path, Document("old disk state").ToJson());
            Seed(Document("edited"), path);

            Assert.That(Call<bool>("LoadJson", path, null, new Func<int>(() => 0)), Is.True);
            _window.SaveChanges();

            Assert.That(ReadName(path), Is.EqualTo("edited"));
            Assert.That(_window.hasUnsavedChanges, Is.False);
        }

        [Test]
        public void InvalidCandidateIsReadBeforePromptAndCannotSaveOrReplaceTheCurrentDocument()
        {
            string first = Path.Combine(_directory, "first.htujob.json");
            string broken = Path.Combine(_directory, "broken.htujob.json");
            File.WriteAllText(first, Document("original").ToJson());
            File.WriteAllText(broken, "[]");
            Seed(Document("edited"), first);
            int prompts = 0;

            Assert.That(Call<bool>("LoadJson", broken, null, new Func<int>(() => { prompts++; return 0; })), Is.False);
            Assert.That(prompts, Is.Zero);
            Assert.That(_window.hasUnsavedChanges, Is.True);
            Assert.That(ReadName(first), Is.EqualTo("original"));
            _window.SaveChanges();
            Assert.That(ReadName(first), Is.EqualTo("edited"));
            Assert.That(File.ReadAllText(broken), Is.EqualTo("[]"));
        }

        [TestCase(0)]
        [TestCase(2)]
        public void SaveOrDiscardSwitchesToTheCandidateWithTheChosenOldFileOutcome(int decision)
        {
            string first = Path.Combine(_directory, "first.htujob.json");
            string second = Path.Combine(_directory, "second.htujob.json");
            File.WriteAllText(first, Document("original").ToJson());
            File.WriteAllText(second, Document("candidate").ToJson());
            Seed(Document("edited"), first);

            Assert.That(Call<bool>("LoadJson", second, null, new Func<int>(() => decision)), Is.True);
            _window.SaveChanges();

            Assert.That(ReadName(first), Is.EqualTo(decision == 0 ? "edited" : "original"));
            Assert.That(ReadName(second), Is.EqualTo("candidate"));
            Assert.That(_window.hasUnsavedChanges, Is.False);
        }

        [TestCase(false, false, TestName = "SaveChangesPersistsRegularJobAndJson")]
        [TestCase(false, true, TestName = "SaveChangesPersistsRegularJobWithoutSavingForeignDirtyAsset")]
        [TestCase(true, true, TestName = "ReplacementSavePersistsRegularJobWithoutSavingForeignDirtyAsset")]
        public void RegularJobSavePersistsOnlyTheAddressedAsset(bool replaceDocument, bool dirtyForeign)
        {
            string folderName = "Audit20261023NeoHtmlToUitkEditor-save-" + Guid.NewGuid().ToString("N");
            string assetFolder = "Assets/" + folderName;
            string ownedPath = assetFolder + "/owned.asset";
            string foreignPath = assetFolder + "/foreign.asset";
            string first = Path.Combine(_directory, "owned.htujob.json");
            string second = Path.Combine(_directory, "candidate.htujob.json");
            HtmlToUitkJob owned = null;
            HtmlToUitkConfig foreign = null;
            try
            {
                Assert.That(AssetDatabase.CreateFolder("Assets", folderName), Is.Not.Empty);
                owned = ScriptableObject.CreateInstance<HtmlToUitkJob>();
                owned.FromDocument(Document("original"));
                owned.SetJsonPath(first);
                AssetDatabase.CreateAsset(owned, ownedPath);
                foreign = ScriptableObject.CreateInstance<HtmlToUitkConfig>();
                foreign.configJsonPath = "foreign-baseline.json";
                AssetDatabase.CreateAsset(foreign, foreignPath);
                EditorUtility.SetDirty(owned);
                EditorUtility.SetDirty(foreign);
                AssetDatabase.SaveAssetIfDirty(owned);
                AssetDatabase.SaveAssetIfDirty(foreign);
                File.WriteAllText(first, Document("original").ToJson());
                File.WriteAllText(second, Document("candidate").ToJson());
                byte[] ownedBaseline = File.ReadAllBytes(HtmlToUitkPaths.ToAbsolute(ownedPath));
                byte[] foreignBaseline = File.ReadAllBytes(HtmlToUitkPaths.ToAbsolute(foreignPath));
                byte[] jsonBaseline = File.ReadAllBytes(first);
                Assert.That(owned.IsImportedFromJson, Is.False);
                Assert.That(EditorUtility.IsDirty(owned), Is.False);
                Assert.That(EditorUtility.IsDirty(foreign), Is.False);

                Seed(Document("edited"), first, owned);
                if (dirtyForeign)
                {
                    foreign.configJsonPath = "foreign-unsaved.json";
                    EditorUtility.SetDirty(foreign);
                    Assert.That(EditorUtility.IsDirty(foreign), Is.True);
                }

                if (replaceDocument)
                {
                    int prompts = 0;
                    Assert.That(Call<bool>("LoadJson", second, null, new Func<int>(() => { prompts++; return 0; })), Is.True);
                    Assert.That(prompts, Is.EqualTo(1));
                    Assert.That(ReadName(second), Is.EqualTo("candidate"));
                }
                else
                    _window.SaveChanges();

                Assert.That(_window.hasUnsavedChanges, Is.False);
                Assert.That(File.ReadAllBytes(first), Is.Not.EqualTo(jsonBaseline));
                Assert.That(ReadName(first), Is.EqualTo("edited"));
                Assert.That(File.ReadAllBytes(HtmlToUitkPaths.ToAbsolute(ownedPath)), Is.Not.EqualTo(ownedBaseline));
                AssetDatabase.ImportAsset(ownedPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                HtmlToUitkJob persisted = AssetDatabase.LoadAssetAtPath<HtmlToUitkJob>(ownedPath);
                Assert.That(persisted, Is.Not.Null);
                Assert.That(persisted.jobName, Is.EqualTo("edited"), "The regular job must survive reimport from its saved asset.");
                Assert.That(File.ReadAllBytes(HtmlToUitkPaths.ToAbsolute(foreignPath)), Is.EqualTo(foreignBaseline), "Saving this job must not write the foreign asset.");
                Assert.That(foreign.configJsonPath, Is.EqualTo(dirtyForeign ? "foreign-unsaved.json" : "foreign-baseline.json"));
                Assert.That(EditorUtility.IsDirty(foreign), Is.EqualTo(dirtyForeign), "The foreign asset must retain its own unsaved state.");
            }
            finally
            {
                if (owned != null)
                    Undo.ClearUndo(owned);
                if (AssetDatabase.IsValidFolder(assetFolder))
                    AssetDatabase.DeleteAsset(assetFolder);
                if (owned != null && !EditorUtility.IsPersistent(owned))
                    UnityEngine.Object.DestroyImmediate(owned);
                if (foreign != null && !EditorUtility.IsPersistent(foreign))
                    UnityEngine.Object.DestroyImmediate(foreign);
            }
        }

        private void Seed(JobDocument document, string path, HtmlToUitkJob asset = null)
        {
            Assert.That(Call<bool>("TryReplaceDocument", document, path, asset, false, null), Is.True);
            Call<object>("MarkDirty");
            Assert.That(_window.hasUnsavedChanges, Is.True, "The real edit command must mark the native window unsaved.");
        }

        // Only drives private editor commands/decisions; assertions observe saved bytes and public unsaved state.
        private T Call<T>(string command, params object[] arguments)
        {
            MethodInfo method = typeof(HtmlToUitkWindow).GetMethod(command, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (T)method.Invoke(_window, arguments);
        }

        private static JobDocument Document(string name) => new JobDocument { Name = name, OutputRoot = "Generated" };
        private static string ReadName(string path) => MiniJson.Parse(File.ReadAllText(path))["name"].AsString();
    }
}
