using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Tooling
{
    public sealed class HtmlToUitkWindow : EditorWindow
    {
        private static readonly string[] HtmlExtensions = { ".html", ".htm" };
        private static readonly string[] AssembleChoices = { "(default)", "perFile", "singleDocument" };
        private static readonly string[] PartialsChoices = { "(default)", "templates", "inline" };
        private static readonly string[] CssOrderChoices = { "(default)", "before", "after" };

        [SerializeField] private string _jobPath = string.Empty;

        private JobDocument _doc;
        private HtmlToUitkJob _jobAsset;
        private DiagnosticsReport _report;
        private IReadOnlyList<ExpandedEntry> _expanded = Array.Empty<ExpandedEntry>();
        private HtmlToUitkWatcher _watcher;
        private Task<JobRunResult> _task;
        private bool _taskValidateOnly;
        private bool _dirty;
        private bool _building;

        private ObjectField _jobField;
        private ObjectField _configField;
        private Label _jobLabel;
        private Label _bridgeLabel;
        private InputListView _inputs;
        private FindingsView _findings;
        private TextField _outputRoot;
        private DropdownField _assemble;
        private DropdownField _partialsMode;
        private DropdownField _cssOrder;
        private TextField _cssList;
        private TextField _ussList;
        private TextField _partialsList;
        private VisualElement _folderFields;
        private Toggle _watchToggle;
        private IntegerField _debounce;
        private Toggle _force;
        private Toggle _strict;
        private Button _validateButton;
        private Button _convertSelectedButton;
        private Button _convertAllButton;
        private ProgressBar _progress;
        private Label _status;
        private Label _note;

        [MenuItem("Neoxider/UI/HTML to UI Toolkit")]
        public static void Open()
        {
            HtmlToUitkWindow window = GetWindow<HtmlToUitkWindow>();
            window.titleContent = new GUIContent("HTML to UITK");
            window.minSize = new Vector2(760, 520);
            window.Show();
        }

        public static void OpenWithJob(HtmlToUitkJob job)
        {
            HtmlToUitkWindow window = GetWindow<HtmlToUitkWindow>();
            window.titleContent = new GUIContent("HTML to UITK");
            window.minSize = new Vector2(760, 520);
            window.Show();
            window.LoadAsset(job);
        }

        public static void OpenWithJson(string assetOrAbsolutePath)
        {
            HtmlToUitkWindow window = GetWindow<HtmlToUitkWindow>();
            window.titleContent = new GUIContent("HTML to UITK");
            window.minSize = new Vector2(760, 520);
            window.Show();
            window.LoadJson(HtmlToUitkPaths.ToAbsolute(assetOrAbsolutePath), null);
        }

        [MenuItem("Assets/Open in HTML to UI Toolkit", false, 2000)]
        private static void OpenSelectedJob()
        {
            if (Selection.activeObject is HtmlToUitkJob job)
                OpenWithJob(job);
            else if (SelectedJobJsonPath() != null)
                OpenWithJson(SelectedJobJsonPath());
        }

        [MenuItem("Assets/Open in HTML to UI Toolkit", true)]
        private static bool ValidateOpenSelectedJob()
        {
            return Selection.activeObject is HtmlToUitkJob || SelectedJobJsonPath() != null;
        }

        private static string SelectedJobJsonPath()
        {
            if (Selection.activeObject == null)
                return null;
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && path.EndsWith(HtmlToUitkJob.JsonExtension, StringComparison.OrdinalIgnoreCase) ? path : null;
        }

        private void CreateGUI()
        {
            saveChangesMessage = "The job has unsaved changes. Save them before closing?";
            rootVisualElement.AddToClassList("htu-root");
            StyleSheet sheet = LoadStyleSheet();
            if (sheet != null)
                rootVisualElement.styleSheets.Add(sheet);
            BuildToolbar();
            BuildBody();
            BuildActionBar();
            string path = !string.IsNullOrEmpty(_jobPath) ? _jobPath : HtmlToUitkSettings.LastJobPath;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                LoadJson(path, null);
            else
                ApplyDocument(null);
            UpdateBridgeLabel();
        }

        private void OnEnable()
        {
            EditorApplication.update += Poll;
            GeneratedAssetPostprocessor.InputsChanged += OnProjectInputsChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Poll;
            GeneratedAssetPostprocessor.InputsChanged -= OnProjectInputsChanged;
            StopWatching();
        }

        private static StyleSheet LoadStyleSheet()
        {
            foreach (string guid in AssetDatabase.FindAssets("HtmlToUitkWindow t:StyleSheet"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Replace('\\', '/').Contains("/Editor/Window/"))
                    return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            }

            return null;
        }

        private void BuildToolbar()
        {
            Toolbar toolbar = new Toolbar();
            _jobField = new ObjectField("Job") { objectType = typeof(HtmlToUitkJob), allowSceneObjects = false };
            _jobField.style.minWidth = 240;
            _jobField.RegisterValueChangedCallback(evt =>
            {
                if (!_building && evt.newValue is HtmlToUitkJob job)
                    LoadAsset(job);
            });
            toolbar.Add(_jobField);
            toolbar.Add(new ToolbarButton(NewJob) { text = "New" });
            toolbar.Add(new ToolbarButton(OpenJobFile) { text = "Open..." });
            toolbar.Add(new ToolbarButton(Save) { text = "Save" });
            toolbar.Add(new ToolbarButton(Reload) { text = "Reload" });
            _configField = new ObjectField("Config") { objectType = typeof(HtmlToUitkConfig), allowSceneObjects = false };
            _configField.style.minWidth = 220;
            _configField.RegisterValueChangedCallback(evt =>
            {
                if (_jobAsset != null && !_building)
                {
                    _jobAsset.config = evt.newValue as HtmlToUitkConfig;
                    EditorUtility.SetDirty(_jobAsset);
                }
            });
            toolbar.Add(_configField);
            VisualElement spacer = new VisualElement();
            spacer.AddToClassList("htu-grow");
            toolbar.Add(spacer);
            _bridgeLabel = new Label();
            toolbar.Add(_bridgeLabel);
            toolbar.Add(new ToolbarButton(PickCli) { text = "CLI..." });
            rootVisualElement.Add(toolbar);

            _jobLabel = new Label();
            _jobLabel.style.marginTop = 4;
            rootVisualElement.Add(_jobLabel);
        }

        private void BuildBody()
        {
            TwoPaneSplitView split = new TwoPaneSplitView(0, 380, TwoPaneSplitViewOrientation.Horizontal);
            split.AddToClassList("htu-grow");
            _inputs = new InputListView { ToEntry = ToEntryRelativeToJob };
            _inputs.EntriesChanged += OnEntriesChanged;
            _inputs.SelectionChanged += files => _findings.SetSelectedFiles(files);
            _inputs.AddFileRequested += OnAddRequested;
            _inputs.OpenRequested += file => OpenFinding(file, 1, 1);
            _findings = new FindingsView();
            _findings.OpenRequested += OpenFinding;
            split.Add(_inputs);
            split.Add(_findings);
            rootVisualElement.Add(split);

            VisualElement section = new VisualElement();
            section.AddToClassList("htu-section");

            VisualElement outputRow = new VisualElement();
            outputRow.AddToClassList("htu-row");
            _outputRoot = new TextField("Output root");
            _outputRoot.AddToClassList("htu-grow");
            _outputRoot.RegisterValueChangedCallback(evt => Edit(d => d.OutputRoot = evt.newValue));
            outputRow.Add(_outputRoot);
            outputRow.Add(new Button(PickOutput) { text = "..." });
            outputRow.Add(new Button(RevealOutput) { text = "Reveal" });
            section.Add(outputRow);

            VisualElement optionRow = new VisualElement();
            optionRow.AddToClassList("htu-row");
            _assemble = MakeDropdown("Assemble", AssembleChoices, (d, v) => d.Assemble = v);
            _partialsMode = MakeDropdown("Partials", PartialsChoices, (d, v) => d.PartialsMode = v);
            _cssOrder = MakeDropdown("CSS order", CssOrderChoices, (d, v) => d.CssOrder = v);
            optionRow.Add(_assemble);
            optionRow.Add(_partialsMode);
            optionRow.Add(_cssOrder);
            section.Add(optionRow);

            Foldout listsFoldout = new Foldout { text = "Style and partial inputs, folders", value = false };
            _cssList = MakeLines("CSS (one per line)", (d, v) => d.Css = v);
            _ussList = MakeLines("USS pass-through", (d, v) => d.Uss = v);
            _partialsList = MakeLines("Partials", (d, v) => d.Partials = v);
            listsFoldout.Add(_cssList);
            listsFoldout.Add(_ussList);
            listsFoldout.Add(_partialsList);
            _folderFields = new VisualElement();
            listsFoldout.Add(_folderFields);
            section.Add(listsFoldout);

            VisualElement watchRow = new VisualElement();
            watchRow.AddToClassList("htu-row");
            _watchToggle = new Toggle("Watch inputs");
            _watchToggle.RegisterValueChangedCallback(evt =>
            {
                Edit(d => d.WatchEnabled = evt.newValue);
                if (evt.newValue)
                    StartWatching();
                else
                    StopWatching();
            });
            _debounce = new IntegerField("Debounce ms") { value = 400 };
            _debounce.RegisterValueChangedCallback(evt =>
            {
                int value = Mathf.Max(0, evt.newValue);
                Edit(d => d.DebounceMs = value);
                if (_watcher != null)
                    _watcher.DebounceSeconds = value / 1000.0;
            });
            watchRow.Add(_watchToggle);
            watchRow.Add(_debounce);
            section.Add(watchRow);
            rootVisualElement.Add(section);
        }

        private void BuildActionBar()
        {
            VisualElement bar = new VisualElement();
            bar.AddToClassList("htu-row");
            bar.AddToClassList("htu-action-bar");
            _validateButton = new Button(() => Run(true, false)) { text = "Validate Only" };
            _convertSelectedButton = new Button(() => Run(false, true)) { text = "Convert Selected" };
            _convertAllButton = new Button(() => Run(false, false)) { text = "Convert All" };
            _force = new Toggle("Force") { tooltip = "Write the valid rest even when the verdict is FAIL" };
            _strict = new Toggle("Strict") { tooltip = "Treat warnings as failures (exit code 1)" };
            bar.Add(_validateButton);
            bar.Add(_convertSelectedButton);
            bar.Add(_convertAllButton);
            bar.Add(_force);
            bar.Add(_strict);
            _progress = new ProgressBar();
            _progress.AddToClassList("htu-progress");
            _progress.style.display = DisplayStyle.None;
            bar.Add(_progress);
            _status = new Label("Idle");
            _status.AddToClassList("htu-status");
            bar.Add(_status);
            rootVisualElement.Add(bar);
            _note = new Label();
            _note.AddToClassList("htu-note");
            rootVisualElement.Add(_note);
        }

        private DropdownField MakeDropdown(string label, string[] choices, Action<JobDocument, string> apply)
        {
            DropdownField dropdown = new DropdownField(label, new List<string>(choices), 0);
            dropdown.RegisterValueChangedCallback(evt => Edit(d => apply(d, evt.newValue == choices[0] ? string.Empty : evt.newValue)));
            return dropdown;
        }

        private TextField MakeLines(string label, Action<JobDocument, List<string>> apply)
        {
            TextField field = new TextField(label) { multiline = true };
            field.style.minHeight = 36;
            field.RegisterValueChangedCallback(evt => Edit(d => apply(d, SplitLines(evt.newValue))));
            return field;
        }

        private static List<string> SplitLines(string text)
        {
            return (text ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => GlobMatcher.Normalize(l.Trim())).Where(l => l.Length > 0).ToList();
        }

        private void Edit(Action<JobDocument> change)
        {
            if (_building || _doc == null)
                return;
            change(_doc);
            MarkDirty();
        }

        private void MarkDirty()
        {
            _dirty = true;
            hasUnsavedChanges = true;
            UpdateJobLabel();
        }

        public override void SaveChanges()
        {
            Save();
            base.SaveChanges();
        }

        private void UpdateJobLabel()
        {
            if (_jobLabel == null)
                return;
            _jobLabel.text = string.IsNullOrEmpty(_jobPath) ? "No job loaded. Create one with New, open a .htujob.json with Open..., or drop a job asset above." : (_dirty ? "* " : string.Empty) + _jobPath;
        }

        private void UpdateBridgeLabel()
        {
            IJobBridge bridge = HtmlToUitkRunner.SelectBridge(out string note);
            _bridgeLabel.text = bridge == null ? "Runner: none" : "Runner: " + bridge.Name;
            _bridgeLabel.tooltip = note ?? string.Empty;
            if (bridge == null)
                SetNote(note);
        }

        private string BaseDirectory => string.IsNullOrEmpty(_jobPath) ? HtmlToUitkPaths.ProjectRoot : HtmlToUitkPaths.DirectoryOf(_jobPath);

        private string ToEntryRelativeToJob(string absolute)
        {
            string baseDir = BaseDirectory;
            string normalized = GlobMatcher.Normalize(absolute);
            try
            {
                string relative = GlobMatcher.Normalize(Path.GetRelativePath(baseDir, normalized));
                return InputExpander.IsRooted(relative) ? normalized : relative;
            }
            catch (ArgumentException)
            {
                return normalized;
            }
        }

        private void NewJob()
        {
            string path = EditorUtility.SaveFilePanelInProject("New HTML to UI Toolkit job", "NewJob", "htujob.json", "Choose where the job file lives. Relative paths in it resolve from that folder.");
            if (string.IsNullOrEmpty(path))
                return;
            JobDocument doc = new JobDocument { Name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path)), OutputRoot = "Generated" };
            doc.Html.Add("pages/*.html");
            string absolute = HtmlToUitkPaths.ToAbsolute(path);
            File.WriteAllText(absolute, doc.ToJson(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            LoadJson(absolute, null);
        }

        private void OpenJobFile()
        {
            string start = string.IsNullOrEmpty(_jobPath) ? Application.dataPath : HtmlToUitkPaths.DirectoryOf(_jobPath);
            string path = EditorUtility.OpenFilePanel("Open job", start, "json");
            if (!string.IsNullOrEmpty(path))
                LoadJson(GlobMatcher.Normalize(path), null);
        }

        private void PickCli()
        {
            string current = HtmlToUitkSettings.CliPath;
            string start = string.IsNullOrEmpty(current) ? Application.dataPath : Path.GetDirectoryName(current) ?? Application.dataPath;
            string path = EditorUtility.OpenFilePanel("htmltouitk CLI (executable or htmltouitk.dll)", start, string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                HtmlToUitkSettings.CliPath = path;
                UpdateBridgeLabel();
            }
        }

        private void LoadAsset(HtmlToUitkJob job)
        {
            if (job == null)
                return;
            string path = job.JsonAbsolutePath;
            if (string.IsNullOrEmpty(path))
            {
                SetNote("The job asset has no JSON path. Save it first.");
                return;
            }

            if (!File.Exists(path) && !job.IsImportedFromJson)
            {
                if (!job.SaveJson(out string error))
                {
                    SetNote(error);
                    return;
                }

                AssetDatabase.Refresh();
            }

            LoadJson(path, job);
        }

        private void LoadJson(string absolutePath, HtmlToUitkJob asset)
        {
            try
            {
                _doc = JobDocument.Parse(File.ReadAllText(absolutePath));
            }
            catch (Exception exception) when (exception is IOException || exception is Neo.HtmlToUitk.Json.JsonParseException)
            {
                SetNote("Cannot read " + absolutePath + ": " + exception.Message);
                return;
            }

            _jobPath = GlobMatcher.Normalize(absolutePath);
            HtmlToUitkSettings.LastJobPath = _jobPath;
            string relative = HtmlToUitkPaths.ToProjectRelative(_jobPath);
            _jobAsset = asset != null ? asset : relative != null ? AssetDatabase.LoadAssetAtPath<HtmlToUitkJob>(relative) : null;
            _dirty = false;
            hasUnsavedChanges = false;
            _report = null;
            ApplyDocument(_doc);
        }

        private void Reload()
        {
            if (!string.IsNullOrEmpty(_jobPath) && File.Exists(_jobPath))
                LoadJson(_jobPath, _jobAsset);
        }

        private void Save()
        {
            if (_doc == null || string.IsNullOrEmpty(_jobPath))
                return;
            HtmlToUitkConfig config = _jobAsset != null ? _jobAsset.config : null;
            if (config != null)
                config.ApplyPanelSettings(_doc);
            if (_jobAsset != null && !_jobAsset.IsImportedFromJson)
            {
                Undo.RecordObject(_jobAsset, "Save job");
                _jobAsset.FromDocument(_doc);
                EditorUtility.SetDirty(_jobAsset);
                AssetDatabase.SaveAssets();
            }

            File.WriteAllText(_jobPath, _doc.ToJson(), new UTF8Encoding(false));
            _dirty = false;
            hasUnsavedChanges = false;
            UpdateJobLabel();
            string relative = HtmlToUitkPaths.ToProjectRelative(_jobPath);
            if (relative != null)
                AssetDatabase.ImportAsset(relative);
            SetNote("Saved " + _jobPath);
        }

        private void ApplyDocument(JobDocument doc)
        {
            _building = true;
            try
            {
                _jobField.SetValueWithoutNotify(_jobAsset);
                _configField.SetValueWithoutNotify(_jobAsset != null ? _jobAsset.config : null);
                _configField.SetEnabled(_jobAsset != null);
                bool hasDoc = doc != null;
                _outputRoot.SetValueWithoutNotify(hasDoc ? doc.OutputRoot : string.Empty);
                _assemble.SetValueWithoutNotify(ChoiceFor(AssembleChoices, hasDoc ? doc.Assemble : string.Empty));
                _partialsMode.SetValueWithoutNotify(ChoiceFor(PartialsChoices, hasDoc ? doc.PartialsMode : string.Empty));
                _cssOrder.SetValueWithoutNotify(ChoiceFor(CssOrderChoices, hasDoc ? doc.CssOrder : string.Empty));
                _cssList.SetValueWithoutNotify(hasDoc ? string.Join("\n", doc.Css) : string.Empty);
                _ussList.SetValueWithoutNotify(hasDoc ? string.Join("\n", doc.Uss) : string.Empty);
                _partialsList.SetValueWithoutNotify(hasDoc ? string.Join("\n", doc.Partials) : string.Empty);
                _watchToggle.SetValueWithoutNotify(hasDoc && doc.WatchEnabled);
                _debounce.SetValueWithoutNotify(hasDoc ? doc.DebounceMs : 400);
                BuildFolderFields(doc);
            }
            finally
            {
                _building = false;
            }

            RebuildInputs();
            _findings.SetReport(_report);
            UpdateJobLabel();
            UpdateButtons();
            if (doc != null && doc.WatchEnabled)
                StartWatching();
            else
                StopWatching();
        }

        private void BuildFolderFields(JobDocument doc)
        {
            _folderFields.Clear();
            if (doc == null)
                return;
            foreach (string key in JobDocument.KnownFolderKeys)
            {
                string folderKey = key;
                int index = doc.Folders.FindIndex(p => p.Key == folderKey);
                TextField field = new TextField("folders." + folderKey) { value = index >= 0 ? doc.Folders[index].Value : string.Empty };
                field.RegisterValueChangedCallback(evt => Edit(d =>
                {
                    int at = d.Folders.FindIndex(p => p.Key == folderKey);
                    KeyValuePair<string, string> pair = new KeyValuePair<string, string>(folderKey, evt.newValue);
                    if (at >= 0)
                        d.Folders[at] = pair;
                    else
                        d.Folders.Add(pair);
                }));
                _folderFields.Add(field);
            }
        }

        private static string ChoiceFor(string[] choices, string value)
        {
            return string.IsNullOrEmpty(value) ? choices[0] : choices.Contains(value) ? value : choices[0];
        }

        private void RebuildInputs()
        {
            if (_doc == null)
            {
                _expanded = Array.Empty<ExpandedEntry>();
                _inputs.SetData(BaseDirectory, Array.Empty<string>(), _expanded, null);
                return;
            }

            _expanded = InputExpander.Expand(BaseDirectory, _doc.Html, DiskFileView.Instance, HtmlExtensions);
            _inputs.SetData(BaseDirectory, _doc.Html, _expanded, _report);
            _findings.SetSelectedFiles(_inputs.SelectedFiles());
        }

        private void OnEntriesChanged(IReadOnlyList<string> entries)
        {
            if (_doc == null)
            {
                SetNote("Create or open a job first.");
                return;
            }

            _doc.Html = new List<string>(entries);
            MarkDirty();
            RebuildInputs();
            if (_watcher != null)
                StartWatching();
        }

        private void OnAddRequested(string kind)
        {
            if (_doc == null)
            {
                SetNote("Create or open a job first.");
                return;
            }

            string start = BaseDirectory;
            string path = kind == "folder"
                ? EditorUtility.OpenFolderPanel("Add folder of HTML files", start, string.Empty)
                : EditorUtility.OpenFilePanel("Add HTML file", start, "html");
            if (!string.IsNullOrEmpty(path))
                _inputs.AddEntries(new[] { ToEntryRelativeToJob(path) });
        }

        private void PickOutput()
        {
            if (_doc == null)
                return;
            string start = HtmlToUitkRunner.OutputRootAbsolute(_doc, _jobPath);
            string path = EditorUtility.OpenFolderPanel("Output folder", Directory.Exists(start) ? start : BaseDirectory, string.Empty);
            if (string.IsNullOrEmpty(path))
                return;
            string value = ToEntryRelativeToJob(path);
            _outputRoot.value = value;
        }

        private void RevealOutput()
        {
            if (_doc == null || string.IsNullOrEmpty(_jobPath))
                return;
            string output = HtmlToUitkRunner.OutputRootAbsolute(_doc, _jobPath);
            if (Directory.Exists(output))
                EditorUtility.RevealInFinder(output);
            else
                SetNote("Output folder does not exist yet: " + output);
        }

        private void Run(bool validateOnly, bool selectedOnly)
        {
            if (_doc == null || string.IsNullOrEmpty(_jobPath))
            {
                SetNote("Create or open a job first.");
                return;
            }

            if (_task != null && !_task.IsCompleted)
                return;
            IReadOnlyList<string> only = null;
            if (selectedOnly)
            {
                only = _inputs.SelectedFiles();
                if (only.Count == 0)
                {
                    SetNote("Select one or more files in the list first.");
                    return;
                }
            }

            string problem = _doc.ValidateShape().FirstOrDefault();
            if (problem != null)
            {
                SetNote("Job is incomplete: " + problem);
                return;
            }

            HtmlToUitkConfig config = _jobAsset != null ? _jobAsset.config : null;
            if (config != null)
                config.ApplyPanelSettings(_doc);
            _taskValidateOnly = validateOnly;
            _task = HtmlToUitkRunner.RunAsync(_doc, _jobPath, new RunOptions { ValidateOnly = validateOnly, Force = _force.value, Strict = _strict.value, OnlyFiles = only });
            SetNote(string.Empty);
            _status.text = validateOnly ? "Validating..." : "Converting...";
            _status.RemoveFromClassList("htu-status--pass");
            _status.RemoveFromClassList("htu-status--warn");
            _status.RemoveFromClassList("htu-status--fail");
            _progress.style.display = DisplayStyle.Flex;
            UpdateButtons();
        }

        private void Poll()
        {
            if (_task == null)
                return;
            if (!_task.IsCompleted)
            {
                _progress.value = (float)(EditorApplication.timeSinceStartup % 2.0) * 50f;
                _progress.title = _status.text;
                return;
            }

            Task<JobRunResult> task = _task;
            _task = null;
            _progress.style.display = DisplayStyle.None;
            JobRunResult result = task.IsFaulted
                ? new JobRunResult { Faulted = true, ExitCode = 2, Report = DiagnosticsReport.Failure(task.Exception?.GetBaseException().Message ?? "The run failed") }
                : task.Result;
            OnRunComplete(result);
        }

        private void OnRunComplete(JobRunResult result)
        {
            _report = result.Report;
            _inputs.SetReport(_report);
            _findings.SetReport(_report);
            _status.text = HtmlToUitkRunner.Summary(result);
            _status.AddToClassList(StatusClass(_report));
            if (!_taskValidateOnly && result.Succeeded && _doc != null)
                HtmlToUitkRunner.RefreshAssets(_doc, _jobPath);
            List<string> notes = new List<string>();
            if (_report.NewKeys.Count > 0)
                notes.Add(_report.NewKeys.Count + " new localisation keys");
            if (!string.IsNullOrWhiteSpace(result.StdErr))
                notes.Add(result.StdErr.Trim());
            SetNote(string.Join(" | ", notes));
            Debug.Log("[HtmlToUitk] " + _status.text);
            UpdateButtons();
        }

        public static string StatusClass(DiagnosticsReport report)
        {
            if (report.Verdict == "FAIL" || report.Errors > 0)
                return "htu-status--fail";
            if (report.Verdict == "PASS WITH WARNINGS" || report.Warnings > 0)
                return "htu-status--warn";
            return "htu-status--pass";
        }

        private void UpdateButtons()
        {
            bool busy = _task != null && !_task.IsCompleted;
            bool ready = _doc != null && !busy;
            _validateButton?.SetEnabled(ready);
            _convertSelectedButton?.SetEnabled(ready);
            _convertAllButton?.SetEnabled(ready);
        }

        private void SetNote(string text)
        {
            if (_note != null)
                _note.text = text ?? string.Empty;
        }

        private void StartWatching()
        {
            StopWatching();
            if (_doc == null || string.IsNullOrEmpty(_jobPath))
                return;
            List<string> directories = new List<string>();
            string baseDir = BaseDirectory;
            foreach (ExpandedEntry entry in _expanded)
            {
                string full = InputExpander.Combine(baseDir, entry.Entry);
                if (entry.Kind == EntryKind.Folder)
                    directories.Add(full);
                else if (entry.Kind == EntryKind.Glob)
                    directories.Add(GlobMatcher.StaticPrefix(full));
                else
                    directories.Add(HtmlToUitkPaths.DirectoryOf(full));
            }

            foreach (string extra in _doc.Css.Concat(_doc.Uss).Concat(_doc.Partials))
                directories.Add(HtmlToUitkPaths.DirectoryOf(InputExpander.Combine(baseDir, GlobMatcher.StaticPrefix(extra) + "/x")));
            _watcher = new HtmlToUitkWatcher(Math.Max(0, _doc.DebounceMs) / 1000.0, new[] { ".html", ".htm", ".css", ".uss" }, OnWatchFired);
            _watcher.Watch(directories);
            if (_watchToggle != null && !_watchToggle.value)
                _watchToggle.SetValueWithoutNotify(true);
        }

        private void StopWatching()
        {
            _watcher?.Dispose();
            _watcher = null;
        }

        private bool IsUnderOutput(string path)
        {
            if (_doc == null || string.IsNullOrEmpty(_jobPath))
                return false;
            string output = HtmlToUitkRunner.OutputRootAbsolute(_doc, _jobPath).TrimEnd('/') + "/";
            return GlobMatcher.Normalize(path).StartsWith(output, StringComparison.OrdinalIgnoreCase);
        }

        private void OnWatchFired(IReadOnlyList<string> changed)
        {
            if (changed.All(IsUnderOutput))
                return;
            if (_doc == null || _task != null && !_task.IsCompleted)
            {
                if (_watcher != null)
                {
                    foreach (string path in changed)
                        _watcher.Notify(path);
                }

                return;
            }

            RebuildInputs();
            Run(false, false);
        }

        private void OnProjectInputsChanged(IReadOnlyList<string> changed)
        {
            if (_watcher == null)
                return;
            foreach (string path in changed)
            {
                if (!IsUnderOutput(path))
                    _watcher.Notify(path);
            }
        }

        private void OpenFinding(string file, int line, int column)
        {
            string absolute = InputExpander.Combine(BaseDirectory, file);
            if (!File.Exists(absolute))
            {
                Debug.LogWarning("[HtmlToUitk] File not found: " + absolute);
                return;
            }

            string relative = HtmlToUitkPaths.ToProjectRelative(absolute);
            if (relative != null)
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relative);
                if (asset != null && AssetDatabase.OpenAsset(asset, Mathf.Max(1, line), Mathf.Max(1, column)))
                    return;
            }

            if (!UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(absolute, Mathf.Max(1, line), Mathf.Max(1, column)))
                EditorUtility.OpenWithDefaultApp(absolute);
        }
    }
}
