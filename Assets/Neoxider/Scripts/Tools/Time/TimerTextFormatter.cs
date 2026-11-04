using System;
using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     Formats up to three numbers with a .NET composite format string (for example <c>"{0:F1}s"</c> or
    ///     <c>"{0:00}:{1:00}"</c>) into a reusable char buffer without boxing or string allocation, and reports
    ///     whether the text changed since the previous call. The parsed format is cached until the format string
    ///     changes. Formats this parser does not understand fall back to <see cref="string.Format(string, object[])" />
    ///     with identical output (and identical exceptions for invalid formats).
    /// </summary>
    /// <example>
    ///     <code>
    /// TimerTextFormatter formatter = new TimerTextFormatter();
    /// if (formatter.Format("{0:F1}s", remainingSeconds, 0f, 0f))
    /// {
    ///     label.SetText(formatter.Buffer, 0, formatter.Length);
    /// }
    /// </code>
    /// </example>
    public sealed class TimerTextFormatter
    {
        private const int MaxArguments = 3;

        private readonly List<Segment> _segments = new();
        private char[] _literals = Array.Empty<char>();
        private string _parsedFormat;
        private bool _hasParsedFormat;
        private bool _supported;

        private char[] _work = new char[32];
        private int _workLength;
        private char[] _published = new char[32];
        private int _publishedLength;
        private bool _hasPublished;

        /// <summary>Characters of the last formatted text. Valid in the range [0, <see cref="Length" />).</summary>
        public char[] Buffer => _published;

        /// <summary>Number of valid characters in <see cref="Buffer" />.</summary>
        public int Length => _publishedLength;

        /// <summary>
        ///     True when the last parsed format is handled by the allocation-free path; false when it falls back to
        ///     <see cref="string.Format(string, object[])" />.
        /// </summary>
        public bool IsAllocationFree => _supported;

        /// <summary>Forgets the previous output so the next <c>Format</c> call reports a change.</summary>
        public void Invalidate()
        {
            _hasPublished = false;
        }

        /// <summary>Formats three floats. Returns true when the text differs from the previous call.</summary>
        public bool Format(string format, float arg0, float arg1, float arg2)
        {
            EnsureParsed(format);
            if (!_supported)
            {
                return PublishFallback(string.Format(format ?? string.Empty, arg0, arg1, arg2));
            }

            _workLength = 0;
            for (int i = 0; i < _segments.Count; i++)
            {
                Segment segment = _segments[i];
                if (segment.ArgumentIndex < 0)
                {
                    AppendLiteral(segment);
                    continue;
                }

                float value = segment.ArgumentIndex == 0 ? arg0 : segment.ArgumentIndex == 1 ? arg1 : arg2;
                int start = _workLength;
                ReadOnlySpan<char> itemFormat = GetItemFormat(format, segment);
                int written;
                while (!value.TryFormat(_work.AsSpan(start), out written, itemFormat, null))
                {
                    Grow(_work.Length * 2);
                }

                _workLength = start + written;
                ApplyAlignment(start, segment.Alignment);
            }

            return PublishWork();
        }

        /// <summary>Formats three integers. Returns true when the text differs from the previous call.</summary>
        public bool Format(string format, int arg0, int arg1, int arg2)
        {
            EnsureParsed(format);
            if (!_supported)
            {
                return PublishFallback(string.Format(format ?? string.Empty, arg0, arg1, arg2));
            }

            _workLength = 0;
            for (int i = 0; i < _segments.Count; i++)
            {
                Segment segment = _segments[i];
                if (segment.ArgumentIndex < 0)
                {
                    AppendLiteral(segment);
                    continue;
                }

                int value = segment.ArgumentIndex == 0 ? arg0 : segment.ArgumentIndex == 1 ? arg1 : arg2;
                int start = _workLength;
                ReadOnlySpan<char> itemFormat = GetItemFormat(format, segment);
                int written;
                while (!value.TryFormat(_work.AsSpan(start), out written, itemFormat, null))
                {
                    Grow(_work.Length * 2);
                }

                _workLength = start + written;
                ApplyAlignment(start, segment.Alignment);
            }

            return PublishWork();
        }

        /// <summary>Returns the last formatted text as a new string (allocates; intended for tests and tooling).</summary>
        public override string ToString()
        {
            return new string(_published, 0, _publishedLength);
        }

        private static ReadOnlySpan<char> GetItemFormat(string format, Segment segment)
        {
            return segment.FormatLength > 0
                ? format.AsSpan(segment.FormatStart, segment.FormatLength)
                : ReadOnlySpan<char>.Empty;
        }

        private void AppendLiteral(Segment segment)
        {
            EnsureCapacity(_workLength + segment.LiteralLength);
            Array.Copy(_literals, segment.LiteralStart, _work, _workLength, segment.LiteralLength);
            _workLength += segment.LiteralLength;
        }

        private void ApplyAlignment(int start, int alignment)
        {
            if (alignment == 0)
            {
                return;
            }

            int written = _workLength - start;
            int width = alignment < 0 ? -alignment : alignment;
            int padding = width - written;
            if (padding <= 0)
            {
                return;
            }

            EnsureCapacity(_workLength + padding);
            if (alignment > 0)
            {
                Array.Copy(_work, start, _work, start + padding, written);
                for (int i = 0; i < padding; i++)
                {
                    _work[start + i] = ' ';
                }
            }
            else
            {
                for (int i = 0; i < padding; i++)
                {
                    _work[_workLength + i] = ' ';
                }
            }

            _workLength += padding;
        }

        private bool PublishWork()
        {
            if (_hasPublished && _workLength == _publishedLength)
            {
                bool same = true;
                for (int i = 0; i < _workLength; i++)
                {
                    if (_work[i] != _published[i])
                    {
                        same = false;
                        break;
                    }
                }

                if (same)
                {
                    return false;
                }
            }

            // WHY: Swapping keeps both buffers alive, so steady-state formatting never allocates.
            (_work, _published) = (_published, _work);
            _publishedLength = _workLength;
            _hasPublished = true;
            if (_work.Length < _published.Length)
            {
                _work = new char[_published.Length];
            }

            return true;
        }

        private bool PublishFallback(string text)
        {
            _workLength = 0;
            EnsureCapacity(text.Length);
            text.CopyTo(0, _work, 0, text.Length);
            _workLength = text.Length;
            return PublishWork();
        }

        private void EnsureCapacity(int required)
        {
            if (required > _work.Length)
            {
                Grow(Math.Max(required, _work.Length * 2));
            }
        }

        private void Grow(int size)
        {
            char[] grown = new char[size];
            Array.Copy(_work, grown, _workLength);
            _work = grown;
        }

        private void EnsureParsed(string format)
        {
            if (_hasParsedFormat && ReferenceEquals(format, _parsedFormat))
            {
                return;
            }

            if (_hasParsedFormat && format != null && _parsedFormat != null && format == _parsedFormat)
            {
                _parsedFormat = format;
                return;
            }

            _parsedFormat = format;
            _hasParsedFormat = true;
            _supported = TryParse(format ?? string.Empty);
        }

        private bool TryParse(string format)
        {
            _segments.Clear();
            char[] literals = new char[format.Length];
            int literalCount = 0;
            int literalStart = 0;
            int i = 0;
            while (i < format.Length)
            {
                char c = format[i];
                if (c == '{')
                {
                    if (i + 1 < format.Length && format[i + 1] == '{')
                    {
                        literals[literalCount++] = '{';
                        i += 2;
                        continue;
                    }

                    AddLiteralSegment(literalStart, literalCount - literalStart);
                    literalStart = literalCount;
                    if (!TryParseItem(format, ref i))
                    {
                        _segments.Clear();
                        return false;
                    }

                    continue;
                }

                if (c == '}')
                {
                    if (i + 1 < format.Length && format[i + 1] == '}')
                    {
                        literals[literalCount++] = '}';
                        i += 2;
                        continue;
                    }

                    _segments.Clear();
                    return false;
                }

                literals[literalCount++] = c;
                i++;
            }

            AddLiteralSegment(literalStart, literalCount - literalStart);
            _literals = literals;
            return true;
        }

        private void AddLiteralSegment(int start, int length)
        {
            if (length > 0)
            {
                _segments.Add(new Segment { ArgumentIndex = -1, LiteralStart = start, LiteralLength = length });
            }
        }

        private bool TryParseItem(string format, ref int i)
        {
            i++;
            int index = 0;
            int digits = 0;
            while (i < format.Length && format[i] >= '0' && format[i] <= '9')
            {
                index = index * 10 + (format[i] - '0');
                digits++;
                i++;
                if (index >= MaxArguments)
                {
                    return false;
                }
            }

            if (digits == 0)
            {
                return false;
            }

            int alignment = 0;
            if (i < format.Length && format[i] == ',')
            {
                i++;
                bool negative = i < format.Length && format[i] == '-';
                if (negative)
                {
                    i++;
                }

                int alignmentDigits = 0;
                while (i < format.Length && format[i] >= '0' && format[i] <= '9')
                {
                    alignment = alignment * 10 + (format[i] - '0');
                    alignmentDigits++;
                    i++;
                    if (alignment > 1000)
                    {
                        return false;
                    }
                }

                if (alignmentDigits == 0)
                {
                    return false;
                }

                alignment = negative ? -alignment : alignment;
            }

            int formatStart = 0;
            int formatLength = 0;
            if (i < format.Length && format[i] == ':')
            {
                i++;
                formatStart = i;
                while (i < format.Length && format[i] != '}')
                {
                    if (format[i] == '{')
                    {
                        return false;
                    }

                    i++;
                }

                formatLength = i - formatStart;
            }

            if (i >= format.Length || format[i] != '}')
            {
                return false;
            }

            // WHY: Runtimes disagree on "}}" right after a format item; let string.Format decide that case.
            if (i + 1 < format.Length && format[i + 1] == '}')
            {
                return false;
            }

            i++;
            _segments.Add(new Segment
            {
                ArgumentIndex = index,
                Alignment = alignment,
                FormatStart = formatStart,
                FormatLength = formatLength
            });
            return true;
        }

        private struct Segment
        {
            public int ArgumentIndex;
            public int LiteralStart;
            public int LiteralLength;
            public int Alignment;
            public int FormatStart;
            public int FormatLength;
        }
    }
}
