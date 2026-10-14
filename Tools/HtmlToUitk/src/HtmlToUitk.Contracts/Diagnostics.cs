using System;
using System.Collections.Generic;

namespace HtmlToUitk
{
    internal static class Empty
    {
        internal static IReadOnlyDictionary<string, string> Strings { get; } = new Dictionary<string, string>(0);
    }

    public readonly struct SourcePos : IEquatable<SourcePos>
    {
        public readonly int Offset;
        public readonly int Line;
        public readonly int Column;

        public SourcePos(int offset, int line, int column)
        {
            Offset = offset;
            Line = line;
            Column = column;
        }

        public bool Equals(SourcePos other) => Offset == other.Offset && Line == other.Line && Column == other.Column;
        public override bool Equals(object obj) => obj is SourcePos other && Equals(other);
        public override int GetHashCode() => (Offset * 397) ^ (Line * 31) ^ Column;
        public static bool operator ==(SourcePos left, SourcePos right) => left.Equals(right);
        public static bool operator !=(SourcePos left, SourcePos right) => !left.Equals(right);
        public override string ToString() => Line + ":" + Column;
    }

    public readonly struct SourceSpan : IEquatable<SourceSpan>
    {
        public readonly string File;
        public readonly SourcePos Start;
        public readonly SourcePos End;

        public SourceSpan(string file, SourcePos start, SourcePos end)
        {
            File = file;
            Start = start;
            End = end;
        }

        public static SourceSpan None => default(SourceSpan);
        public bool IsNone => File == null && Start.Line == 0;

        public bool Equals(SourceSpan other) => string.Equals(File, other.File, StringComparison.Ordinal) && Start.Equals(other.Start) && End.Equals(other.End);
        public override bool Equals(object obj) => obj is SourceSpan other && Equals(other);
        public override int GetHashCode() => ((File == null ? 0 : StringComparer.Ordinal.GetHashCode(File)) * 397) ^ Start.GetHashCode();
        public static bool operator ==(SourceSpan left, SourceSpan right) => left.Equals(right);
        public static bool operator !=(SourceSpan left, SourceSpan right) => !left.Equals(right);
        public override string ToString() => (File ?? "") + ":" + Start;
    }

    public enum Severity { Info = 0, Warning = 1, Error = 2 }

    public enum Tier { Native = 0, ConvertTime = 1, Runtime = 2, Reject = 3 }

    public enum Verdict { Pass = 0, PassWithWarnings = 1, Fail = 2 }

    public sealed class Diagnostic
    {
        public string RuleId;
        public Severity Severity;
        public Tier Tier;
        public SourceSpan Span;
        public IReadOnlyList<SourceSpan> Related = Array.Empty<SourceSpan>();
        public string Construct;
        public string Message;
        public string Why;
        public string Suggestion;
        public string EmulationId;
    }

    public interface IDiagnosticSink
    {
        void Report(Diagnostic diagnostic);
    }

    public sealed class DiagnosticBag : IDiagnosticSink
    {
        private readonly List<Diagnostic> _items = new List<Diagnostic>();

        public IReadOnlyList<Diagnostic> Items => _items;

        public void Report(Diagnostic diagnostic)
        {
            if (diagnostic == null) throw new ArgumentNullException(nameof(diagnostic));
            _items.Add(diagnostic);
        }

        public int Count(Severity severity)
        {
            int count = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Severity == severity) count++;
            }
            return count;
        }

        public Verdict Verdict
        {
            get
            {
                bool warning = false;
                for (int i = 0; i < _items.Count; i++)
                {
                    Severity severity = _items[i].Severity;
                    if (severity == Severity.Error) return Verdict.Fail;
                    if (severity == Severity.Warning) warning = true;
                }
                return warning ? Verdict.PassWithWarnings : Verdict.Pass;
            }
        }

        public IReadOnlyList<Diagnostic> Sorted()
        {
            var keyed = new List<KeyValuePair<int, Diagnostic>>(_items.Count);
            for (int i = 0; i < _items.Count; i++) keyed.Add(new KeyValuePair<int, Diagnostic>(i, _items[i]));
            keyed.Sort(CompareKeyed);
            var result = new List<Diagnostic>(keyed.Count);
            for (int i = 0; i < keyed.Count; i++) result.Add(keyed[i].Value);
            return result;
        }

        private static int CompareKeyed(KeyValuePair<int, Diagnostic> a, KeyValuePair<int, Diagnostic> b)
        {
            Diagnostic x = a.Value;
            Diagnostic y = b.Value;
            int c = ((int)y.Severity).CompareTo((int)x.Severity);
            if (c != 0) return c;
            c = string.CompareOrdinal(x.Span.File ?? "", y.Span.File ?? "");
            if (c != 0) return c;
            c = x.Span.Start.Line.CompareTo(y.Span.Start.Line);
            if (c != 0) return c;
            c = x.Span.Start.Column.CompareTo(y.Span.Start.Column);
            if (c != 0) return c;
            c = string.CompareOrdinal(x.RuleId ?? "", y.RuleId ?? "");
            if (c != 0) return c;
            return a.Key.CompareTo(b.Key);
        }
    }

    public sealed class RuleInfo
    {
        public string Id;
        public Severity DefaultSeverity;
        public Tier DefaultTier;
        public string Title, Why, Fix, Emulation;
    }

    public interface IRuleCatalog
    {
        RuleInfo Get(string id);
        IReadOnlyList<RuleInfo> All { get; }
    }
}
