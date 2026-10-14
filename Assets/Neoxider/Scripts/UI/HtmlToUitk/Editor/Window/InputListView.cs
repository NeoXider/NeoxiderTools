using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Tooling
{
    public sealed class InputListView : VisualElement
    {
        private readonly ListView _sources;
        private readonly ListView _files;
        private readonly TextField _pattern;
        private readonly List<string> _entries = new List<string>();
        private readonly List<ExpandedEntry> _expanded = new List<ExpandedEntry>();
        private readonly List<string> _fileItems = new List<string>();
        private DiagnosticsReport _report;
        private string _baseDirectory = string.Empty;

        public InputListView()
        {
            AddToClassList("htu-inputs");

            VisualElement header = new VisualElement();
            header.AddToClassList("htu-row");
            Label title = new Label("Inputs");
            title.AddToClassList("htu-title");
            header.Add(title);
            header.Add(new Button(AddFile) { text = "Add file..." });
            header.Add(new Button(AddFolder) { text = "Add folder..." });
            Add(header);

            VisualElement patternRow = new VisualElement();
            patternRow.AddToClassList("htu-row");
            _pattern = new TextField { tooltip = "A file, a folder or a glob such as pages/**/*.html" };
            _pattern.AddToClassList("htu-grow");
            _pattern.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    AddPattern();
            });
            patternRow.Add(_pattern);
            patternRow.Add(new Button(AddPattern) { text = "Add pattern" });
            Add(patternRow);

            _sources = new ListView
            {
                fixedItemHeight = 22,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                makeItem = MakeSourceRow,
                bindItem = BindSourceRow,
                itemsSource = _entries
            };
            _sources.AddToClassList("htu-sources");
            Add(_sources);

            Label filesTitle = new Label("Files");
            filesTitle.AddToClassList("htu-subtitle");
            Add(filesTitle);

            _files = new ListView
            {
                fixedItemHeight = 22,
                selectionType = SelectionType.Multiple,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                makeItem = MakeFileRow,
                bindItem = BindFileRow,
                itemsSource = _fileItems
            };
            _files.AddToClassList("htu-files");
            _files.selectedIndicesChanged += _ => SelectionChanged?.Invoke(SelectedFiles());
            _files.itemsChosen += chosen =>
            {
                string file = chosen.OfType<string>().FirstOrDefault();
                if (file != null)
                    OpenRequested?.Invoke(file);
            };
            Add(_files);

            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DragLeaveEvent>(evt => RemoveFromClassList("htu-drop"));
        }

        public event Action<IReadOnlyList<string>> EntriesChanged;

        public event Action<IReadOnlyList<string>> SelectionChanged;

        public event Action<string> OpenRequested;

        public event Action<string> AddFileRequested;

        public Func<string, string> ToEntry { get; set; } = path => GlobMatcher.Normalize(path);

        public IReadOnlyList<string> Entries => _entries;

        public IReadOnlyList<string> Files => _fileItems;

        public IReadOnlyList<string> SelectedFiles()
        {
            List<string> selected = new List<string>();
            foreach (int index in _files.selectedIndices)
            {
                if (index >= 0 && index < _fileItems.Count)
                    selected.Add(_fileItems[index]);
            }

            return selected;
        }

        public void SetData(string baseDirectory, IReadOnlyList<string> entries, IReadOnlyList<ExpandedEntry> expanded, DiagnosticsReport report)
        {
            _baseDirectory = baseDirectory;
            _report = report;
            _entries.Clear();
            _entries.AddRange(entries);
            _expanded.Clear();
            _expanded.AddRange(expanded);
            _fileItems.Clear();
            _fileItems.AddRange(InputExpander.Flatten(expanded));
            _sources.Rebuild();
            _files.Rebuild();
        }

        public void SetReport(DiagnosticsReport report)
        {
            _report = report;
            _files.RefreshItems();
            _sources.RefreshItems();
        }

        public FileStatus StatusOf(string file)
        {
            return _report == null ? FileStatus.NotRun : _report.StatusOf(file);
        }

        private VisualElement MakeSourceRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("htu-row");
            row.AddToClassList("htu-source-row");
            Label kind = new Label();
            kind.AddToClassList("htu-kind");
            Label text = new Label();
            text.AddToClassList("htu-grow");
            Label count = new Label();
            count.AddToClassList("htu-count");
            Button remove = new Button { text = "x", tooltip = "Remove this entry" };
            remove.AddToClassList("htu-remove");
            row.Add(kind);
            row.Add(text);
            row.Add(count);
            row.Add(remove);
            return row;
        }

        private void BindSourceRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _entries.Count)
                return;
            string entry = _entries[index];
            ExpandedEntry expanded = index < _expanded.Count ? _expanded[index] : null;
            List<Label> labels = row.Query<Label>().ToList();
            labels[0].text = expanded == null ? "?" : expanded.Kind == EntryKind.Glob ? "glob" : expanded.Kind == EntryKind.Folder ? "dir" : "file";
            labels[1].text = entry;
            labels[2].text = expanded == null ? string.Empty : expanded.Files.Count.ToString();
            row.EnableInClassList("htu-empty", expanded != null && expanded.IsEmpty);
            row.tooltip = expanded != null && expanded.IsEmpty ? "Matches nothing (J010)" : entry;
            Button remove = row.Q<Button>();
            remove.clickable = new Clickable(() => RemoveEntry(entry));
        }

        private VisualElement MakeFileRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("htu-row");
            row.AddToClassList("htu-file-row");
            Label badge = new Label();
            badge.AddToClassList("htu-badge");
            Label path = new Label();
            path.AddToClassList("htu-grow");
            Label counts = new Label();
            counts.AddToClassList("htu-count");
            row.Add(badge);
            row.Add(path);
            row.Add(counts);
            return row;
        }

        private void BindFileRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _fileItems.Count)
                return;
            string file = _fileItems[index];
            List<Label> labels = row.Query<Label>().ToList();
            FileStatus status = StatusOf(file);
            ApplyBadge(labels[0], status);
            labels[1].text = file;
            labels[1].tooltip = file;
            IReadOnlyList<Finding> findings = _report == null ? Array.Empty<Finding>() : _report.For(file);
            int errors = findings.Count(f => f.Severity == FindingSeverity.Error);
            int warnings = findings.Count(f => f.Severity == FindingSeverity.Warning);
            labels[2].text = _report == null ? string.Empty : (errors > 0 ? errors + "E " : string.Empty) + (warnings > 0 ? warnings + "W" : string.Empty);
        }

        public static void ApplyBadge(Label badge, FileStatus status)
        {
            badge.RemoveFromClassList("htu-badge--pass");
            badge.RemoveFromClassList("htu-badge--warn");
            badge.RemoveFromClassList("htu-badge--fail");
            badge.RemoveFromClassList("htu-badge--notrun");
            switch (status)
            {
                case FileStatus.Pass:
                    badge.text = "PASS";
                    badge.AddToClassList("htu-badge--pass");
                    break;
                case FileStatus.Warn:
                    badge.text = "WARN";
                    badge.AddToClassList("htu-badge--warn");
                    break;
                case FileStatus.Fail:
                    badge.text = "FAIL";
                    badge.AddToClassList("htu-badge--fail");
                    break;
                default:
                    badge.text = "-";
                    badge.AddToClassList("htu-badge--notrun");
                    break;
            }

            badge.tooltip = status == FileStatus.NotRun ? "Not validated yet" : status.ToString();
        }

        private void RemoveEntry(string entry)
        {
            List<string> next = new List<string>(_entries);
            if (next.Remove(entry))
                EntriesChanged?.Invoke(next);
        }

        private void AddPattern()
        {
            string text = _pattern.value?.Trim();
            if (string.IsNullOrEmpty(text))
                return;
            _pattern.SetValueWithoutNotify(string.Empty);
            AddEntries(new[] { text });
        }

        private void AddFile()
        {
            AddFileRequested?.Invoke("file");
        }

        private void AddFolder()
        {
            AddFileRequested?.Invoke("folder");
        }

        public void AddEntries(IEnumerable<string> entries)
        {
            List<string> next = new List<string>(_entries);
            bool changed = false;
            foreach (string entry in entries)
            {
                string normalized = GlobMatcher.Normalize(entry);
                if (!string.IsNullOrEmpty(normalized) && !next.Contains(normalized))
                {
                    next.Add(normalized);
                    changed = true;
                }
            }

            if (changed)
                EntriesChanged?.Invoke(next);
        }

        private static IEnumerable<string> DroppedPaths()
        {
            List<string> paths = new List<string>();
            foreach (string path in DragAndDrop.paths)
            {
                if (!string.IsNullOrEmpty(path))
                    paths.Add(path);
            }

            if (paths.Count == 0)
            {
                foreach (UnityEngine.Object reference in DragAndDrop.objectReferences)
                {
                    string assetPath = AssetDatabase.GetAssetPath(reference);
                    if (!string.IsNullOrEmpty(assetPath))
                        paths.Add(assetPath);
                }
            }

            return paths;
        }

        private void OnDragEnter(DragEnterEvent evt)
        {
            if (DroppedPaths().Any())
                AddToClassList("htu-drop");
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = DroppedPaths().Any() ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            RemoveFromClassList("htu-drop");
            List<string> dropped = DroppedPaths().ToList();
            if (dropped.Count == 0)
                return;
            DragAndDrop.AcceptDrag();
            AddEntries(dropped.Select(path => ToEntry(HtmlToUitkPaths.ToAbsolute(path))));
            evt.StopPropagation();
        }
    }
}
