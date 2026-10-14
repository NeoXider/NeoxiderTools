using System;
using System.Collections.Generic;

namespace HtmlToUitk.Core.Text
{
    public sealed class SourceMap
    {
        private readonly int[] _lineStarts;
        private readonly int _length;
        private readonly int _baseOffset;
        private readonly int _baseLine;
        private readonly int _baseColumn;

        public SourceMap(string text, string file) : this(text, file, default(SourcePos))
        {
        }

        public SourceMap(string text, string file, SourcePos origin)
        {
            File = file;
            text = text ?? "";
            _length = text.Length;
            _baseOffset = origin.Line == 0 ? 0 : origin.Offset;
            _baseLine = origin.Line == 0 ? 1 : origin.Line;
            _baseColumn = origin.Line == 0 ? 1 : origin.Column;
            var starts = new List<int>(Math.Max(16, text.Length / 24)) { 0 };
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n') starts.Add(i + 1);
                else if (c == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                    starts.Add(i + 1);
                }
            }
            _lineStarts = starts.ToArray();
        }

        public string File { get; }

        public SourcePos Pos(int offset)
        {
            if (offset < 0) offset = 0;
            if (offset > _length) offset = _length;
            int lo = 0;
            int hi = _lineStarts.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) >> 1;
                if (_lineStarts[mid] <= offset) lo = mid;
                else hi = mid - 1;
            }
            int column = offset - _lineStarts[lo] + 1;
            if (lo == 0) column += _baseColumn - 1;
            return new SourcePos(offset + _baseOffset, lo + _baseLine, column);
        }

        public SourceSpan Span(int start, int end)
        {
            if (end < start) end = start;
            return new SourceSpan(File, Pos(start), Pos(end));
        }
    }
}
