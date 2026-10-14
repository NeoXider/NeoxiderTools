using System;
using System.Collections.Generic;
using HtmlToUitk.Convert;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Core.Includes
{
    public sealed class ImportExpansion
    {
        public IReadOnlyList<Stylesheet> Sheets = Array.Empty<Stylesheet>();
        public IncludeGraph Graph = new IncludeGraph();
    }

    public static class ImportExpander
    {
        public static ImportExpansion Expand(Stylesheet root, IFileSystem fs, IDiagnosticSink sink)
        {
            var state = new State(fs, sink);
            state.Visit(root, new List<string>());
            return new ImportExpansion { Sheets = state.Sheets, Graph = new IncludeGraph { Nodes = state.Nodes, Edges = state.Edges } };
        }

        public static ImportExpansion ExpandText(string text, string path, StyleOrigin origin, IFileSystem fs, IDiagnosticSink sink)
        {
            Stylesheet root = CssParser.Instance.Parse(text, path, origin, sink);
            return Expand(root, fs, sink);
        }

        public static bool IsExternal(string target)
        {
            if (string.IsNullOrEmpty(target)) return false;
            return target.StartsWith("http:", StringComparison.OrdinalIgnoreCase) || target.StartsWith("https:", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("//", StringComparison.Ordinal) || target.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || target.StartsWith("file:", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class State
        {
            private readonly IFileSystem _fs;
            private readonly IDiagnosticSink _sink;
            private readonly HashSet<string> _emitted = new HashSet<string>(StringComparer.Ordinal);
            private readonly HashSet<string> _nodes = new HashSet<string>(StringComparer.Ordinal);

            public readonly List<Stylesheet> Sheets = new List<Stylesheet>();
            public readonly List<string> Nodes = new List<string>();
            public readonly List<IncludeEdge> Edges = new List<IncludeEdge>();

            public State(IFileSystem fs, IDiagnosticSink sink)
            {
                _fs = fs;
                _sink = sink;
            }

            private void Node(string path)
            {
                if (_nodes.Add(path)) Nodes.Add(path);
            }

            private void Report(string ruleId, ImportRef import, string construct, string message, string suggestion)
            {
                if (_sink == null) return;
                _sink.Report(new Diagnostic
                {
                    RuleId = ruleId,
                    Severity = Severity.Error,
                    Tier = Tier.Reject,
                    Span = import.Span,
                    Construct = construct,
                    Message = message,
                    Why = ruleId == "J003" ? "An @import chain that returns to a file already being imported has no valid order." : "The cascade cannot be built without the file.",
                    Suggestion = suggestion
                });
            }

            public void Visit(Stylesheet sheet, List<string> chain)
            {
                string key = PathUtil.Normalize(sheet.Path ?? "");
                Node(key);
                chain.Add(key);
                foreach (ImportRef import in sheet.Imports)
                {
                    if (IsExternal(import.Target))
                    {
                        Report("J002", import, "@import " + import.Target, "External stylesheet '" + import.Target + "' cannot be loaded", "Copy the file into the project and import it by relative path.");
                        continue;
                    }
                    string target = PathUtil.Combine(PathUtil.Directory(key), import.Target);
                    Node(target);
                    Edges.Add(new IncludeEdge { From = key, To = target, Kind = "import", Span = import.Span });
                    if (chain.Contains(target))
                    {
                        int start = chain.IndexOf(target);
                        var cycle = new List<string>(chain.GetRange(start, chain.Count - start)) { target };
                        Report("J003", import, "@import " + import.Target, "Circular @import: " + string.Join(" -> ", cycle), "Remove one of the @import rules in the cycle.");
                        continue;
                    }
                    if (_emitted.Contains(target)) continue;
                    if (!_fs.FileExists(target))
                    {
                        Report("J002", import, "@import " + import.Target, "Imported stylesheet '" + target + "' does not exist", "Create the file or fix the @import path.");
                        continue;
                    }
                    string text = _fs.ReadAllText(target);
                    Stylesheet imported = CssParser.Instance.Parse(text, target, StyleOrigin.Imported, _sink);
                    Visit(imported, chain);
                }
                chain.RemoveAt(chain.Count - 1);
                if (_emitted.Add(key)) Sheets.Add(sheet);
            }
        }
    }
}
