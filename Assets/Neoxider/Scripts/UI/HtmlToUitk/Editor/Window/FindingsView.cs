using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Tooling
{
    public sealed class FindingsView : VisualElement
    {
        private readonly ListView _list;
        private readonly Toggle _errors;
        private readonly Toggle _warnings;
        private readonly Toggle _infos;
        private readonly Toggle _selectedOnly;
        private readonly Label _summary;
        private readonly VisualElement _detail;
        private readonly List<Finding> _items = new List<Finding>();
        private DiagnosticsReport _report;
        private IReadOnlyList<string> _selectedFiles = Array.Empty<string>();

        public FindingsView()
        {
            AddToClassList("htu-findings");

            VisualElement header = new VisualElement();
            header.AddToClassList("htu-row");
            Label title = new Label("Findings");
            title.AddToClassList("htu-title");
            header.Add(title);
            _errors = MakeToggle("Errors", true, header);
            _warnings = MakeToggle("Warnings", true, header);
            _infos = MakeToggle("Info", false, header);
            _selectedOnly = MakeToggle("Selected files", true, header);
            Add(header);

            _summary = new Label();
            _summary.AddToClassList("htu-summary");
            Add(_summary);

            _list = new ListView
            {
                fixedItemHeight = 22,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                makeItem = MakeRow,
                bindItem = BindRow,
                itemsSource = _items
            };
            _list.AddToClassList("htu-grow");
            _list.selectedIndicesChanged += _ => ShowDetail();
            _list.itemsChosen += chosen =>
            {
                Finding finding = chosen.OfType<Finding>().FirstOrDefault();
                if (finding != null)
                    OpenRequested?.Invoke(finding.File, finding.Line, finding.Column);
            };
            Add(_list);

            _detail = new VisualElement();
            _detail.AddToClassList("htu-detail");
            Add(_detail);
        }

        public event Action<string, int, int> OpenRequested;

        public void SetReport(DiagnosticsReport report)
        {
            _report = report;
            Refresh();
        }

        public void SetSelectedFiles(IReadOnlyList<string> files)
        {
            _selectedFiles = files ?? Array.Empty<string>();
            Refresh();
        }

        public void Refresh()
        {
            _items.Clear();
            if (_report != null)
            {
                bool restrict = _selectedOnly.value && _selectedFiles.Count > 0;
                HashSet<string> allowed = restrict ? new HashSet<string>(_selectedFiles.Select(DiagnosticsReport.Key), StringComparer.OrdinalIgnoreCase) : null;
                IEnumerable<Finding> ordered = _report.Findings
                    .Where(f => Allowed(f.Severity))
                    .Where(f => allowed == null || allowed.Contains(DiagnosticsReport.Key(f.File)))
                    .OrderByDescending(f => (int)f.Severity)
                    .ThenBy(f => f.File, StringComparer.Ordinal)
                    .ThenBy(f => f.Line)
                    .ThenBy(f => f.Column);
                _items.AddRange(ordered);
                _summary.text = _report.Verdict + ": " + _report.Errors + " errors, " + _report.Warnings + " warnings, " + _report.Infos + " info; showing " + _items.Count;
            }
            else
            {
                _summary.text = "No report yet. Run Validate Only or Convert.";
            }

            _list.Rebuild();
            ShowDetail();
        }

        private bool Allowed(FindingSeverity severity)
        {
            switch (severity)
            {
                case FindingSeverity.Error: return _errors.value;
                case FindingSeverity.Warning: return _warnings.value;
                default: return _infos.value;
            }
        }

        private Toggle MakeToggle(string text, bool initial, VisualElement parent)
        {
            Toggle toggle = new Toggle(text) { value = initial };
            toggle.RegisterValueChangedCallback(_ => Refresh());
            parent.Add(toggle);
            return toggle;
        }

        private VisualElement MakeRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("htu-row");
            Label severity = new Label();
            severity.AddToClassList("htu-badge");
            Label rule = new Label();
            rule.AddToClassList("htu-rule");
            Label location = new Label();
            location.AddToClassList("htu-location");
            Label message = new Label();
            message.AddToClassList("htu-grow");
            row.Add(severity);
            row.Add(rule);
            row.Add(location);
            row.Add(message);
            return row;
        }

        private void BindRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _items.Count)
                return;
            Finding finding = _items[index];
            List<Label> labels = row.Query<Label>().ToList();
            InputListView.ApplyBadge(labels[0], finding.Severity == FindingSeverity.Error ? FileStatus.Fail : finding.Severity == FindingSeverity.Warning ? FileStatus.Warn : FileStatus.Pass);
            labels[0].text = finding.Severity == FindingSeverity.Info ? "INFO" : labels[0].text;
            labels[1].text = finding.Rule;
            labels[2].text = finding.File + ":" + finding.Line + ":" + finding.Column;
            labels[3].text = finding.Message ?? finding.Construct ?? string.Empty;
            row.tooltip = finding.Construct;
        }

        private void ShowDetail()
        {
            _detail.Clear();
            int index = _list.selectedIndex;
            if (index < 0 || index >= _items.Count)
                return;
            Finding finding = _items[index];
            AddLine("Construct", finding.Construct);
            AddLine("Why", finding.Why);
            AddLine("Rewrite", finding.Suggestion);
            if (!string.IsNullOrEmpty(finding.Emulation))
                AddLine("Emulation", finding.Emulation + " (" + finding.Tier + ")");
            VisualElement buttons = new VisualElement();
            buttons.AddToClassList("htu-row");
            buttons.Add(new Button(() => OpenRequested?.Invoke(finding.File, finding.Line, finding.Column)) { text = "Open at line" });
            foreach (FindingLocation related in finding.Related)
            {
                FindingLocation target = related;
                buttons.Add(new Button(() => OpenRequested?.Invoke(target.File, target.Line, target.Column)) { text = "Related " + target.File + ":" + target.Line });
            }

            _detail.Add(buttons);
        }

        private void AddLine(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            Label label = new Label(name + ": " + value);
            label.AddToClassList("htu-detail-line");
            label.style.whiteSpace = WhiteSpace.Normal;
            _detail.Add(label);
        }
    }
}
