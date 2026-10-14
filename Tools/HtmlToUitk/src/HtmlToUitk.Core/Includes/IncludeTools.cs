using System;
using System.Collections.Generic;
using HtmlToUitk.Convert;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Core.Includes
{
    public sealed class IncludeRef
    {
        public string File;
        public string Id;

        public static IncludeRef Parse(string text, string fromFile)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string value = text.Trim();
            int hash = value.IndexOf('#');
            string file = hash < 0 ? value : value.Substring(0, hash);
            string id = hash < 0 ? null : value.Substring(hash + 1);
            if (string.IsNullOrEmpty(file)) file = fromFile;
            else file = PathUtil.Combine(PathUtil.Directory(fromFile ?? ""), file);
            if (string.IsNullOrEmpty(id)) id = null;
            return new IncludeRef { File = PathUtil.Normalize(file), Id = id };
        }

        public string Key => File + (Id == null ? "" : "#" + Id);
    }

    public static class IncludeGraphTools
    {
        public static IReadOnlyList<string> FindCycle(IncludeGraph graph)
        {
            var adjacency = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (IncludeEdge edge in graph.Edges)
            {
                List<string> list;
                if (!adjacency.TryGetValue(edge.From, out list))
                {
                    list = new List<string>();
                    adjacency[edge.From] = list;
                }
                list.Add(edge.To);
            }
            var color = new Dictionary<string, int>(StringComparer.Ordinal);
            var starts = new List<string>(graph.Nodes);
            foreach (string key in adjacency.Keys) if (!starts.Contains(key)) starts.Add(key);
            foreach (string start in starts)
            {
                var path = new List<string>();
                IReadOnlyList<string> cycle = Dfs(start, adjacency, color, path);
                if (cycle != null) return cycle;
            }
            return null;
        }

        private static IReadOnlyList<string> Dfs(string node, Dictionary<string, List<string>> adjacency, Dictionary<string, int> color, List<string> path)
        {
            int state;
            color.TryGetValue(node, out state);
            if (state == 2) return null;
            if (state == 1)
            {
                int start = path.IndexOf(node);
                var cycle = new List<string>(path.GetRange(start, path.Count - start)) { node };
                return cycle;
            }
            color[node] = 1;
            path.Add(node);
            List<string> next;
            if (adjacency.TryGetValue(node, out next))
            {
                foreach (string target in next)
                {
                    IReadOnlyList<string> cycle = Dfs(target, adjacency, color, path);
                    if (cycle != null) return cycle;
                }
            }
            path.RemoveAt(path.Count - 1);
            color[node] = 2;
            return null;
        }
    }

    public sealed class PartialIndex
    {
        private readonly Dictionary<string, DomElement> _byKey = new Dictionary<string, DomElement>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Keys => _byKey.Keys;

        public static PartialIndex Load(IFileSystem fs, IEnumerable<string> files, IDiagnosticSink sink)
        {
            var index = new PartialIndex();
            var parser = new HtmlParser();
            foreach (string file in files)
            {
                string normalized = PathUtil.Normalize(file);
                if (!fs.FileExists(normalized)) continue;
                HtmlDocument document = parser.Parse(fs.ReadAllText(normalized), normalized, sink);
                foreach (DomElement template in document.Templates)
                {
                    string id = template.GetAttribute("id");
                    if (!string.IsNullOrEmpty(id)) index._byKey[normalized + "#" + id] = template;
                }
            }
            return index;
        }

        public DomElement Find(IncludeRef reference)
        {
            DomElement element;
            return reference != null && _byKey.TryGetValue(reference.Key, out element) ? element : null;
        }
    }

    public static class DomClone
    {
        public static DomElement Clone(DomElement source, DomElement newParent = null)
        {
            var copy = new DomElement { Tag = source.Tag, Span = source.Span, Parent = newParent };
            var attributes = new List<DomAttribute>(source.Attributes.Count);
            foreach (DomAttribute a in source.Attributes) attributes.Add(new DomAttribute { Name = a.Name, Value = a.Value, Span = a.Span });
            copy.Attributes = attributes;
            var children = new List<DomNode>(source.Children.Count);
            foreach (DomNode node in source.Children)
            {
                var element = node as DomElement;
                if (element != null) children.Add(Clone(element, copy));
                else
                {
                    var text = (DomText)node;
                    children.Add(new DomText { Text = text.Text, Span = text.Span, Parent = copy });
                }
            }
            copy.Children = children;
            return copy;
        }
    }
}
