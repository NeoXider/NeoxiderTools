using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.HtmlToUitk.Json;
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
        [SerializeField] private string _documentJson = string.Empty;
        [SerializeField] private HtmlToUitkJob _jobAsset;
        [SerializeField] private bool _dirty;
        [SerializeField] private int _selectedTab;
        [SerializeField] private bool _forceRun;
        [SerializeField] private bool _strictRun;

        private JobDocument _doc;
        private DiagnosticsReport _report;
        private IReadOnlyList<ExpandedEntry> _expanded = Array.Empty<ExpandedEntry>();
        private HtmlToUitkWatcher _watcher;
        private Task<JobRunResult> _task;
        private JobDocument _taskDocument;
        private JobDocument _taskSource;
        private string _taskJobPath;
        private bool _taskValidateOnly;
        private bool _building;

        private ObjectField _jobField;
        private ObjectField _configField;
        private Label _jobLabel;
        private Label _bridgeLabel;
        private Label _outputHint;
        private readonly List<Button> _tabButtons = new List<Button>();
        private readonly List<VisualElement> _tabPanels = new List<VisualElement>();
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
        private TextField _runnerDetails;
        private string _runnerOutput = string.Empty;
        // Optional deterministic picker for draft-save regression tests; production uses the native dialog.
        private Func<string> _chooseDraftSavePath = null;

        [MenuItem("Neoxider/UI/HTML to UI Toolkit")]
        public static void Open()
        {
            HtmlToUitkWindow window = GetWindow<HtmlToUitkWindow>();
            window.titleContent = new GUIContent("HTML to UITK");
            window.minSize = new Vector2(400, 420);
            window.Show();
        }

        public static void OpenWithJob(HtmlToUitkJob job)
        {
            HtmlToUitkWindow window = GetWindow<HtmlToUitkWindow>();
            window.titleContent = new GUIContent("HTML to UITK");
            window.minSize = new Vector2(400, 420);
            window.Show();
            window.LoadAsset(job);
        }

        public static void OpenWithJson(string assetOrAbsolutePath)
        {
            HtmlToUitkWindow window = GetWindow<HtmlToUitkWindow>();
            window.titleContent = new GUIContent("HTML to UITK");
            window.minSize = new Vector2(400, 420);
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
            string note = _note != null ? _note.tooltip : string.Empty;
            string status = _status != null ? _status.text : "Idle";
            UnbindViews();
            StopWatching();
            rootVisualElement.Clear();
            _tabButtons.Clear();
            _tabPanels.Clear();
            saveChangesMessage = "The job has unsaved changes. Save them before closing?";
            rootVisualElement.AddToClassList("htu-root");
            StyleSheet sheet = LoadStyleSheet();
            if (sheet != null && !rootVisualElement.styleSheets.Contains(sheet))
                rootVisualElement.styleSheets.Add(sheet);
            BuildToolbar();
            BuildBody();
            BuildActionBar();
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnWindowGeometryChanged);
            rootVisualElement.EnableInClassList("htu-narrow", position.width < 620);
            if (_doc == null && !string.IsNullOrEmpty(_documentJson))
            {
                try
                {
                    _doc = JobDocument.Parse(_documentJson);
                }
                catch (Exception exception)
                {
                    note = "Cannot restore the window's job: " + exception.Message;
                }
            }

            if (_doc != null)
            {
                hasUnsavedChanges = _dirty;
                ApplyDocument(_doc);
            }
            else
            {
                string path = !string.IsNullOrEmpty(_jobPath) ? _jobPath : HtmlToUitkSettings.LastJobPath;
                if (string.IsNullOrEmpty(path) || !File.Exists(path) || !LoadJson(path, null))
                {
                    _doc = new JobDocument { Name = "Untitled", OutputRoot = "Assets/UI/Generated" };
                    _jobPath = string.Empty;
                    _jobAsset = null;
                    ApplyDocument(_doc);
                }
            }

            _runnerDetails.SetValueWithoutNotify(_runnerOutput);
            _status.text = status;
            if (_report != null)
                _status.AddToClassList(StatusClass(_report));
            _progress.style.display = _task != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (!string.IsNullOrEmpty(note))
                SetNote(note);
            SelectTab(_selectedTab);
            UpdateBridgeLabel();
        }

        private void OnEnable()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            GeneratedAssetPostprocessor.InputsChanged -= OnProjectInputsChanged;
            GeneratedAssetPostprocessor.InputsChanged += OnProjectInputsChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Poll;
            GeneratedAssetPostprocessor.InputsChanged -= OnProjectInputsChanged;
            if (_doc != null)
                _documentJson = _doc.ToJson();
            _dirty = hasUnsavedChanges;
            UnbindViews();
            StopWatching();
        }

        private void UnbindViews()
        {
            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnWindowGeometryChanged);
            if (_inputs != null)
            {
                _inputs.EntriesChanged -= OnEntriesChanged;
                _inputs.SelectionChanged -= OnInputSelectionChanged;
                _inputs.AddFileRequested -= OnAddRequested;
                _inputs.OpenRequested -= OnInputOpenRequested;
                _inputs.ToEntry = GlobMatcher.Normalize;
            }

            if (_findings != null)
                _findings.OpenRequested -= OpenFinding;
        }

        private void OnWindowGeometryChanged(GeometryChangedEvent evt)
        {
            rootVisualElement.EnableInClassList("htu-narrow", evt.newRect.width < 620);
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
            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("htu-row");
            toolbar.AddToClassList("htu-toolbar");
            _jobField = new ObjectField("Job") { objectType = typeof(HtmlToUitkJob), allowSceneObjects = false };
            _jobField.AddToClassList("htu-job-field");
            _jobField.RegisterValueChangedCallback(evt =>
            {
                if (!_building && evt.newValue is HtmlToUitkJob job)
                {
                    LoadAsset(job);
                    _jobField.SetValueWithoutNotify(_jobAsset);
                }
            });
            toolbar.Add(_jobField);
            toolbar.Add(new ToolbarButton(NewJob) { text = "New" });
            toolbar.Add(new ToolbarButton(OpenJobFile) { text = "Open..." });
            toolbar.Add(new ToolbarButton(Save) { text = "Save" });
            toolbar.Add(new ToolbarButton(Reload) { text = "Reload" });
            rootVisualElement.Add(toolbar);

            _jobLabel = new Label();
            _jobLabel.AddToClassList("htu-job-label");
            rootVisualElement.Add(_jobLabel);
        }

        private void BuildBody()
        {
            VisualElement tabs = new VisualElement();
            tabs.AddToClassList("htu-row");
            tabs.AddToClassList("htu-tabs");
            string[] titles = { "Sources", "Settings", "Results" };
            for (int i = 0; i < titles.Length; i++)
            {
                int index = i;
                Button button = new Button(() => SelectTab(index)) { text = titles[i], name = "htu-tab-" + titles[i].ToLowerInvariant() };
                button.AddToClassList("htu-tab");
                _tabButtons.Add(button);
                tabs.Add(button);
            }

            rootVisualElement.Add(tabs);
            ScrollView sources = MakeTabPanel("sources");
            sources.Add(new HelpBox("Add a local HTML file or folder, then Convert All. URL/site import and arbitrary JavaScript execution are not implemented.", HelpBoxMessageType.Info));
            BuildOutput(sources);
            _inputs = new InputListView { ToEntry = ToEntryRelativeToJob };
            _inputs.EntriesChanged += OnEntriesChanged;
            _inputs.SelectionChanged += OnInputSelectionChanged;
            _inputs.AddFileRequested += OnAddRequested;
            _inputs.OpenRequested += OnInputOpenRequested;
            sources.Add(_inputs);

            ScrollView settings = MakeTabPanel("settings");
            settings.Add(new HelpBox("Relative input and output paths resolve from the job folder. An unsaved job uses the Unity project root. Saving an unsaved job preserves these locations.", HelpBoxMessageType.Info));
            BuildSettings(settings);

            ScrollView results = MakeTabPanel("results");
            _findings = new FindingsView();
            _findings.OpenRequested += OpenFinding;
            results.Add(_findings);
            Foldout details = new Foldout { text = "Runner details", value = true };
            _runnerDetails = new TextField { multiline = true, isReadOnly = true };
            _runnerDetails.AddToClassList("htu-runner-details");
            details.Add(_runnerDetails);
            results.Add(details);
        }

        private ScrollView MakeTabPanel(string name)
        {
            ScrollView panel = new ScrollView(ScrollViewMode.Vertical) { name = "htu-panel-" + name };
            panel.AddToClassList("htu-tab-panel");
            panel.contentContainer.AddToClassList("htu-tab-content");
            _tabPanels.Add(panel);
            rootVisualElement.Add(panel);
            return panel;
        }

        private void SelectTab(int index)
        {
            _selectedTab = Mathf.Clamp(index, 0, _tabPanels.Count - 1);
            for (int i = 0; i < _tabPanels.Count; i++)
            {
                _tabPanels[i].style.display = i == _selectedTab ? DisplayStyle.Flex : DisplayStyle.None;
                _tabButtons[i].EnableInClassList("htu-tab--selected", i == _selectedTab);
            }
        }

        private void BuildOutput(VisualElement parent)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList("htu-section");
            VisualElement outputRow = new VisualElement();
            outputRow.AddToClassList("htu-row");
            outputRow.AddToClassList("htu-output-row");
            _outputRoot = new TextField("Output root");
            _outputRoot.AddToClassList("htu-grow");
            _outputRoot.tooltip = "Leave empty to use Generated beside a saved job, or Assets/UI/Generated for an unsaved job.";
            _outputRoot.RegisterValueChangedCallback(evt =>
            {
                Edit(d => d.OutputRoot = evt.newValue);
                UpdateOutputHint();
                if (!_building && _watcher != null)
                    StartWatching();
            });
            outputRow.Add(_outputRoot);
            outputRow.Add(new Button(PickOutput) { text = "..." });
            outputRow.Add(new Button(RevealOutput) { text = "Reveal" });
            section.Add(outputRow);
            _outputHint = new Label();
            _outputHint.AddToClassList("htu-summary");
            section.Add(_outputHint);
            parent.Add(section);
        }

        private void BuildSettings(VisualElement parent)
        {
            _configField = new ObjectField("Config") { objectType = typeof(HtmlToUitkConfig), allowSceneObjects = false,
                tooltip = "Optional configuration asset for a saved job asset; its other properties remain editable in the Inspector." };
            _configField.RegisterValueChangedCallback(evt =>
            {
                if (_jobAsset != null && !_building)
                {
                    Undo.RecordObject(_jobAsset, "Change HTML to UITK config");
                    _jobAsset.config = evt.newValue as HtmlToUitkConfig;
                    EditorUtility.SetDirty(_jobAsset);
                }
            });
            parent.Add(_configField);
            Foldout advanced = new Foldout { text = "Advanced conversion settings", value = false };
            advanced.AddToClassList("htu-section");
            VisualElement optionRow = new VisualElement();
            optionRow.AddToClassList("htu-row");
            optionRow.AddToClassList("htu-option-row");
            _assemble = MakeDropdown("Assemble", AssembleChoices, (d, v) => d.Assemble = v);
            _partialsMode = MakeDropdown("Partials", PartialsChoices, (d, v) => d.PartialsMode = v);
            _cssOrder = MakeDropdown("CSS order", CssOrderChoices, (d, v) => d.CssOrder = v);
            optionRow.Add(_assemble);
            optionRow.Add(_partialsMode);
            optionRow.Add(_cssOrder);
            advanced.Add(optionRow);

            Foldout listsFoldout = new Foldout { text = "Style and partial inputs, folders", value = false };
            _cssList = MakeLines("CSS (one per line)", (d, v) => d.Css = v);
            _ussList = MakeLines("USS pass-through", (d, v) => d.Uss = v);
            _partialsList = MakeLines("Partials", (d, v) => d.Partials = v);
            listsFoldout.Add(_cssList);
            listsFoldout.Add(_ussList);
            listsFoldout.Add(_partialsList);
            _folderFields = new VisualElement();
            listsFoldout.Add(_folderFields);
            advanced.Add(listsFoldout);
            _force = new Toggle("Force") { value = _forceRun, tooltip = "Write the valid rest even when the verdict is FAIL" };
            _strict = new Toggle("Strict") { value = _strictRun, tooltip = "Treat warnings as failures (exit code 1)" };
            _force.RegisterValueChangedCallback(evt => _forceRun = evt.newValue);
            _strict.RegisterValueChangedCallback(evt => _strictRun = evt.newValue);
            advanced.Add(_force);
            advanced.Add(_strict);
            parent.Add(advanced);

            Foldout watch = new Foldout { text = "Watch inputs", value = false };
            watch.AddToClassList("htu-section");
            VisualElement watchRow = new VisualElement();
            watchRow.AddToClassList("htu-row");
            watchRow.AddToClassList("htu-option-row");
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
            watch.Add(watchRow);
            parent.Add(watch);

            VisualElement runner = new VisualElement();
            runner.AddToClassList("htu-section");
            _bridgeLabel = new Label();
            _bridgeLabel.AddToClassList("htu-summary");
            runner.Add(_bridgeLabel);
            runner.Add(new Button(PickCli) { text = "Choose CLI..." });
            Label runnerHelp = new Label("The existing runner discovers the project CLI or an available in-process core. A CLI DLL requires dotnet on PATH.");
            runnerHelp.AddToClassList("htu-summary");
            runner.Add(runnerHelp);
            parent.Add(runner);
        }

        private void BuildActionBar()
        {
            VisualElement bar = new VisualElement();
            bar.AddToClassList("htu-row");
            bar.AddToClassList("htu-action-bar");
            _validateButton = new Button(() => Run(true, false)) { text = "Validate Only" };
            _convertSelectedButton = new Button(() => Run(false, true)) { text = "Convert Selected" };
            _convertAllButton = new Button(() => Run(false, false)) { text = "Convert All" };
            _convertAllButton.AddToClassList("htu-primary");
            bar.Add(_convertAllButton);
            bar.Add(_validateButton);
            bar.Add(_convertSelectedButton);
            rootVisualElement.Add(bar);
            VisualElement statusRow = new VisualElement();
            statusRow.AddToClassList("htu-row");
            _progress = new ProgressBar();
            _progress.AddToClassList("htu-progress");
            _progress.style.display = DisplayStyle.None;
            statusRow.Add(_progress);
            _status = new Label("Idle");
            _status.AddToClassList("htu-status");
            statusRow.Add(_status);
            rootVisualElement.Add(statusRow);
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
            field.RegisterValueChangedCallback(evt =>
            {
                Edit(d => apply(d, SplitLines(evt.newValue)));
                if (!_building && _watcher != null)
                    StartWatching();
            });
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
            _documentJson = _doc.ToJson();
            UpdateJobLabel();
        }

        public override void SaveChanges()
        {
            if (!TrySave(out string error))
                throw new IOException(error);
            base.SaveChanges();
        }

        private void UpdateJobLabel()
        {
            if (_jobLabel == null)
                return;
            _jobLabel.text = (_dirty ? "* " : string.Empty) + (string.IsNullOrEmpty(_jobPath)
                ? "Unsaved job. Add HTML to start; Save chooses a JSON location."
                : _jobPath);
            _jobLabel.tooltip = _jobLabel.text;
        }

        private void UpdateBridgeLabel()
        {
            try
            {
                IJobBridge bridge = HtmlToUitkRunner.SelectBridge(out string note);
                _bridgeLabel.text = bridge == null ? "Runner: unavailable. Choose CLI below." : "Runner: " + bridge.Name;
                _bridgeLabel.tooltip = note ?? string.Empty;
                if (bridge == null)
                    SetNote(note + " Open Settings to choose a CLI executable or DLL.");
            }
            catch (Exception exception)
            {
                _bridgeLabel.text = "Runner: configuration error";
                _bridgeLabel.tooltip = exception.Message;
                SetNote("Runner configuration error: " + exception.Message + ". Open Settings to choose a CLI.");
            }
        }

        private string BaseDirectory => string.IsNullOrEmpty(_jobPath) ? HtmlToUitkPaths.ProjectRoot : HtmlToUitkPaths.DirectoryOf(_jobPath);

        // Only a base location for the runner's existing temporary-job mechanism; this file is never created.
        private string RunJobPath => string.IsNullOrEmpty(_jobPath) ? HtmlToUitkPaths.ProjectRoot + "/Untitled.htujob.json" : _jobPath;

        private JobDocument WithDefaultOutput(JobDocument document)
        {
            if (!string.IsNullOrWhiteSpace(document.OutputRoot))
                return document;
            JobDocument prepared = JobDocument.Parse(document.ToJson());
            prepared.OutputRoot = string.IsNullOrEmpty(_jobPath) ? "Assets/UI/Generated" : "Generated";
            return prepared;
        }

        private void UpdateOutputHint()
        {
            if (_outputHint == null || _doc == null)
                return;
            try
            {
                string output = HtmlToUitkRunner.OutputRootAbsolute(WithDefaultOutput(_doc), RunJobPath);
                _outputHint.text = (string.IsNullOrWhiteSpace(_doc.OutputRoot) ? "Default output: " : "Output: ") + output;
                _outputHint.tooltip = output;
            }
            catch (Exception exception)
            {
                _outputHint.text = "Invalid output path: " + exception.Message;
            }
        }

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
            string absolute = HtmlToUitkPaths.ToAbsolute(path);
            TryReplaceDocument(doc, absolute, null, true);
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
                SetNote(string.Empty);
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
                try
                {
                    if (TryReplaceDocument(job.ToDocument(), path, job, false))
                        SetNote("Loaded the job asset in memory. Save to create its missing JSON: " + path);
                }
                catch (Exception exception)
                {
                    SetNote("Cannot read job asset: " + exception.Message);
                }
                return;
            }

            LoadJson(path, job);
        }

        private bool LoadJson(string absolutePath, HtmlToUitkJob asset, Func<int> choose = null)
        {
            try
            {
                JobDocument candidate = JobDocument.Parse(File.ReadAllText(absolutePath));
                return TryReplaceDocument(candidate, absolutePath, asset, false, choose);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is Neo.HtmlToUitk.Json.JsonParseException)
            {
                SetNote("Cannot read " + absolutePath + ": " + exception.Message);
                return false;
            }
        }

        // The optional decision callback exercises the same file/state transition without a native dialog.
        // DisplayDialogComplex returns 0 = Save, 1 = Cancel, 2 = Discard.
        private bool TryReplaceDocument(JobDocument candidate, string path, HtmlToUitkJob asset, bool createFile, Func<int> choose = null)
        {
            try
            {
                string absolute = HtmlToUitkPaths.ToAbsolute(path);
                if (_doc != null && hasUnsavedChanges)
                {
                    int decision = choose != null ? choose() : EditorUtility.DisplayDialogComplex("Unsaved HTML to UITK job", "Save changes to " + _jobPath + " before replacing it?", "Save", "Cancel", "Discard");
                    if (decision == 1 || decision != 0 && decision != 2)
                        return false;
                    if (decision == 0)
                    {
                        if (!TrySave(out string _))
                            return false;
                        // Reload after Save must read the saved edits, not the candidate read before the decision.
                        if (!createFile && string.Equals(absolute, _jobPath, StringComparison.OrdinalIgnoreCase))
                            candidate = JobDocument.Parse(File.ReadAllText(absolute));
                    }
                }

                if (createFile)
                {
                    using (FileStream stream = new FileStream(absolute, FileMode.CreateNew, FileAccess.Write))
                    using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
                        writer.Write(candidate.ToJson());
                    if (HtmlToUitkPaths.IsInsideAssets(absolute))
                        AssetDatabase.Refresh();
                }

                string relative = HtmlToUitkPaths.ToProjectRelative(absolute);
                HtmlToUitkJob resolved = asset != null ? asset : relative != null ? AssetDatabase.LoadAssetAtPath<HtmlToUitkJob>(relative) : null;
                _doc = candidate;
                _jobPath = absolute;
                _jobAsset = resolved;
                HtmlToUitkSettings.LastJobPath = absolute;
                _dirty = false;
                hasUnsavedChanges = false;
                _documentJson = _doc.ToJson();
                ClearReport();
                SetNote(string.Empty);
                if (_jobField != null)
                    ApplyDocument(_doc);
                else
                    StopWatching();
                return true;
            }
            catch (Exception exception)
            {
                SetNote("Cannot replace job: " + exception.Message);
                return false;
            }
        }

        private void Reload()
        {
            if (!string.IsNullOrEmpty(_jobPath) && File.Exists(_jobPath))
                LoadJson(_jobPath, _jobAsset);
        }

        private void Save()
        {
            TrySave(out string _);
        }

        private bool TrySave(out string error)
        {
            try
            {
                if (_doc == null)
                    throw new InvalidOperationException("No job is loaded.");
                string path = _jobPath;
                bool draft = string.IsNullOrEmpty(path);
                if (draft)
                {
                    path = _chooseDraftSavePath != null ? _chooseDraftSavePath()
                        : EditorUtility.SaveFilePanelInProject("Save HTML to UI Toolkit job", "NewJob", "htujob.json", "Input and output locations will be preserved when saving this unsaved job.");
                    if (string.IsNullOrEmpty(path))
                    {
                        error = "Saving cancelled; the job is still unsaved.";
                        SetNote(error);
                        return false;
                    }

                    path = HtmlToUitkPaths.ToAbsolute(path);
                }

                string prefix = draft ? GlobMatcher.Normalize(Path.GetRelativePath(HtmlToUitkPaths.DirectoryOf(path), HtmlToUitkPaths.ProjectRoot)) : null;
                // Validate before ToJson, which intentionally omits non-object PerFile values.
                JsonValue perFile = draft ? RebaseDraftPerFile(prefix, _doc.PerFile) : null;
                JobDocument prepared = JobDocument.Parse(_doc.ToJson());
                if (draft)
                {
                    // Rebase the directory prefix rather than the entry, preserving wildcard patterns verbatim.
                    prepared.Html = prepared.Html.Select(entry => RebaseDraftEntry(prefix, entry)).ToList();
                    prepared.Css = prepared.Css.Select(entry => RebaseDraftEntry(prefix, entry)).ToList();
                    prepared.Uss = prepared.Uss.Select(entry => RebaseDraftEntry(prefix, entry)).ToList();
                    prepared.Partials = prepared.Partials.Select(entry => RebaseDraftEntry(prefix, entry)).ToList();
                    if (!string.IsNullOrWhiteSpace(prepared.Config))
                        prepared.Config = RebaseDraftEntry(prefix, prepared.Config);
                    prepared.OutputRoot = RebaseDraftEntry(prefix, WithDefaultOutput(prepared).OutputRoot);
                    prepared.PerFile = perFile;
                }

                HtmlToUitkConfig config = _jobAsset != null ? _jobAsset.config : null;
                if (config != null)
                    config.ApplyPanelSettings(prepared);
                string json = prepared.ToJson();
                File.WriteAllText(path, json, new UTF8Encoding(false));
                if (_jobAsset != null && !_jobAsset.IsImportedFromJson)
                {
                    Undo.RecordObject(_jobAsset, "Save job");
                    _jobAsset.FromDocument(prepared);
                    EditorUtility.SetDirty(_jobAsset);
                    AssetDatabase.SaveAssetIfDirty(_jobAsset);
                }

                string relative = HtmlToUitkPaths.ToProjectRelative(path);
                if (relative != null && HtmlToUitkPaths.IsInsideAssets(path))
                    AssetDatabase.ImportAsset(relative);
                _jobPath = path;
                HtmlToUitkSettings.LastJobPath = path;
                if (draft && relative != null)
                    _jobAsset = AssetDatabase.LoadAssetAtPath<HtmlToUitkJob>(relative);
                _doc = prepared;
                _dirty = false;
                hasUnsavedChanges = false;
                _documentJson = prepared.ToJson();
                if (draft)
                    ClearReport();
                if (_jobField != null)
                    ApplyDocument(_doc);
                UpdateJobLabel();
                error = null;
                SetNote("Saved " + _jobPath);
                return true;
            }
            catch (Exception exception)
            {
                error = "Cannot save " + _jobPath + ": " + exception.Message;
                SetNote(error);
                return false;
            }
        }

        private static string RebaseDraftEntry(string prefix, string entry)
        {
            return string.IsNullOrWhiteSpace(entry) ? entry : InputExpander.Combine(prefix, entry);
        }

        private static JsonValue RebaseDraftPerFile(string prefix, JsonValue perFile)
        {
            if (perFile == null || perFile.Kind == JsonKind.Null)
                return perFile;
            if (perFile.Kind != JsonKind.Object)
                throw new InvalidOperationException("output.perFile must be an object or null.");
            JsonValue rebased = JsonValue.NewObject();
            foreach (KeyValuePair<string, JsonValue> member in perFile.Members)
            {
                string key = member.Key;
                if (string.IsNullOrWhiteSpace(key) || GlobMatcher.HasWildcards(key) || key.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    throw new InvalidOperationException("output.perFile requires an exact source path: " + key);
                if (member.Value == null || member.Value.Kind != JsonKind.Object)
                    throw new InvalidOperationException("output.perFile[" + key + "] must be an object.");
                foreach (string property in new[] { "name", "folder", "assemble" })
                {
                    JsonValue value = member.Value[property];
                    if (value.Kind != JsonKind.Null && value.Kind != JsonKind.String)
                        throw new InvalidOperationException("output.perFile[" + key + "]." + property + " must be a string or null.");
                }

                string identity = NormalizeSourceIdentity(RebaseDraftEntry(prefix, key));
                if (rebased.Has(identity))
                    throw new InvalidOperationException("output.perFile source paths collide after rebasing: " + identity);
                rebased.Set(identity, member.Value);
            }

            return rebased;
        }

        // Match the converter's PathUtil.Normalize without requiring its optional Core DLL in the Editor assembly.
        private static string NormalizeSourceIdentity(string path)
        {
            string normalized = GlobMatcher.Normalize(path);
            string root = normalized.StartsWith("/", StringComparison.Ordinal) ? "/"
                : normalized.Length >= 2 && normalized[1] == ':' && char.IsLetter(normalized[0]) ? normalized.Substring(0, 2) : string.Empty;
            if (root.Length == 2 && normalized.Length > 2 && normalized[2] == '/')
                root += "/";
            List<string> parts = new List<string>();
            foreach (string part in normalized.Substring(root.Length).Split('/'))
            {
                if (part.Length == 0 || part == ".")
                    continue;
                if (part == ".." && parts.Count > 0 && parts[parts.Count - 1] != "..")
                    parts.RemoveAt(parts.Count - 1);
                else if (part != ".." || root.Length == 0)
                    parts.Add(part);
            }

            return root + string.Join("/", parts);
        }

        private void ClearReport()
        {
            _report = null;
            _runnerOutput = string.Empty;
            _runnerDetails?.SetValueWithoutNotify(string.Empty);
            if (_status == null)
                return;
            _status.text = "Idle";
            _status.RemoveFromClassList("htu-status--pass");
            _status.RemoveFromClassList("htu-status--warn");
            _status.RemoveFromClassList("htu-status--fail");
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
            UpdateOutputHint();
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

            try
            {
                _expanded = InputExpander.Expand(BaseDirectory, _doc.Html, DiskFileView.Instance, HtmlExtensions);
            }
            catch (Exception exception)
            {
                _expanded = Array.Empty<ExpandedEntry>();
                SetNote("Cannot list HTML inputs: " + exception.Message);
            }
            _inputs.SetData(BaseDirectory, _doc.Html, _expanded, _report);
            OnInputSelectionChanged(_inputs.SelectedFiles());
        }

        private void OnInputSelectionChanged(IReadOnlyList<string> files)
        {
            // Runner failures have no source file; a selected-file filter must not hide their explanation.
            _findings.SetSelectedFiles(_report != null && _report.Findings.Any(f => string.IsNullOrEmpty(f.File)) ? Array.Empty<string>() : files);
            UpdateButtons();
        }

        private void OnInputOpenRequested(string file)
        {
            OpenFinding(file, 1, 1);
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
                : EditorUtility.OpenFilePanelWithFilters("Add HTML file", start, new[] { "HTML files", "html,htm" });
            if (!string.IsNullOrEmpty(path))
                _inputs.AddEntries(new[] { ToEntryRelativeToJob(path) });
        }

        private void PickOutput()
        {
            if (_doc == null)
                return;
            string start = BaseDirectory;
            try
            {
                start = HtmlToUitkRunner.OutputRootAbsolute(WithDefaultOutput(_doc), RunJobPath);
            }
            catch (Exception exception)
            {
                SetNote("Choose a valid output folder: " + exception.Message);
            }

            string path = EditorUtility.OpenFolderPanel("Output folder", Directory.Exists(start) ? start : BaseDirectory, string.Empty);
            if (string.IsNullOrEmpty(path))
                return;
            string value = ToEntryRelativeToJob(path);
            _outputRoot.value = value;
        }

        private void RevealOutput()
        {
            if (_doc == null)
                return;
            try
            {
                string output = HtmlToUitkRunner.OutputRootAbsolute(WithDefaultOutput(_doc), RunJobPath);
                if (Directory.Exists(output))
                    EditorUtility.RevealInFinder(output);
                else
                    SetNote("Output folder does not exist yet: " + output);
            }
            catch (Exception exception)
            {
                SetNote("Cannot reveal output: " + exception.Message);
            }
        }

        private void Run(bool validateOnly, bool selectedOnly)
        {
            if (_doc == null)
            {
                SetNote("Create or open a job first.");
                return;
            }

            if (_task != null)
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

            JobDocument prepared = WithDefaultOutput(_doc);
            string problem = prepared.ValidateShape().FirstOrDefault();
            if (problem != null)
            {
                SetNote("Job is incomplete: " + problem);
                return;
            }

            _taskValidateOnly = validateOnly;
            _taskSource = _doc;
            try
            {
                _taskDocument = JobDocument.Parse(prepared.ToJson());
                HtmlToUitkConfig config = _jobAsset != null ? _jobAsset.config : null;
                if (config != null)
                    config.ApplyPanelSettings(_taskDocument);
                _taskJobPath = RunJobPath;
                _task = HtmlToUitkRunner.RunAsync(_taskDocument, _taskJobPath, new RunOptions { ValidateOnly = validateOnly, Force = _force.value, Strict = _strict.value, OnlyFiles = only });
            }
            catch (Exception exception)
            {
                OnRunComplete(new JobRunResult { Faulted = true, ExitCode = 2, Report = DiagnosticsReport.Failure("Cannot start runner: " + exception.Message) });
                return;
            }

            SetNote(string.Empty);
            SelectTab(2);
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
            JobRunResult result = task.IsFaulted || task.IsCanceled
                ? new JobRunResult { Faulted = true, ExitCode = 2, Report = DiagnosticsReport.Failure(task.IsCanceled ? "The run was cancelled." : task.Exception?.GetBaseException().Message ?? "The run failed") }
                : task.Result;
            OnRunComplete(result);
        }

        private void OnRunComplete(JobRunResult result)
        {
            if (!_taskValidateOnly && result.Succeeded && _taskDocument != null)
                HtmlToUitkRunner.RefreshAssets(_taskDocument, _taskJobPath);
            bool isCurrentDocument = ReferenceEquals(_doc, _taskSource);
            _taskDocument = null;
            _taskSource = null;
            _taskJobPath = null;
            if (!isCurrentDocument)
            {
                _status.text = "Idle";
                SetNote("Previous job finished: " + HtmlToUitkRunner.Summary(result));
                UpdateButtons();
                return;
            }

            _report = result.Report;
            _inputs.SetReport(_report);
            _findings.SetReport(_report);
            OnInputSelectionChanged(_inputs.SelectedFiles());
            _status.text = HtmlToUitkRunner.Summary(result);
            _status.RemoveFromClassList("htu-status--pass");
            _status.RemoveFromClassList("htu-status--warn");
            _status.RemoveFromClassList("htu-status--fail");
            _status.AddToClassList(StatusClass(_report));
            List<string> notes = new List<string>();
            if (_report.NewKeys.Count > 0)
                notes.Add(_report.NewKeys.Count + " new localisation keys");
            if (!string.IsNullOrWhiteSpace(result.StdErr))
                notes.Add(result.StdErr.Trim());
            Finding error = _report.Findings.FirstOrDefault(f => f.Severity == FindingSeverity.Error);
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                notes.Add(error.Message);
            _runnerOutput = HtmlToUitkRunner.Summary(result) + "\nExit code: " + result.ExitCode
                + "\n\n" + (error != null ? error.Message + "\n\n" : string.Empty) + "stdout:\n" + result.StdOut + "\nstderr:\n" + result.StdErr;
            _runnerDetails.SetValueWithoutNotify(_runnerOutput);
            SelectTab(2);
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
            bool busy = _task != null;
            bool ready = _doc != null && !busy;
            _validateButton?.SetEnabled(ready);
            _convertSelectedButton?.SetEnabled(ready && _inputs != null && _inputs.SelectedFiles().Count > 0);
            _convertAllButton?.SetEnabled(ready);
        }

        private void SetNote(string text)
        {
            if (_note != null)
            {
                string message = text ?? string.Empty;
                _note.text = message.Length > 320 ? message.Substring(0, 320) + "... (hover for the full message)" : message;
                _note.tooltip = message;
            }
        }

        private void StartWatching()
        {
            StopWatching();
            if (_doc == null)
                return;
            IReadOnlyList<string> directories = JobInputPaths.GetWatchDirectories(_doc, BaseDirectory, DiskFileView.Instance);
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

        private bool IsRelevantInput(string path)
        {
            if (_doc == null)
                return false;
            return JobInputPaths.IsInput(WithDefaultOutput(_doc), BaseDirectory, path);
        }

        private void OnWatchFired(IReadOnlyList<string> changed)
        {
            if (!changed.Any(IsRelevantInput))
                return;
            if (_doc == null || _task != null)
            {
                if (_watcher != null)
                {
                    foreach (string path in changed)
                        if (IsRelevantInput(path))
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
                if (IsRelevantInput(path))
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
