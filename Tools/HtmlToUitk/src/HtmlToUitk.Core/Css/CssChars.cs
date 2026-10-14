using System;
using System.Text;

namespace HtmlToUitk.Core.Css
{
    public static class CssChars
    {
        public static bool IsWhitespace(char c) => c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\f';

        public static bool IsNameStart(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c >= 0x80;

        public static bool IsNameChar(char c) => IsNameStart(c) || (c >= '0' && c <= '9') || c == '-';

        public static bool IsDigit(char c) => c >= '0' && c <= '9';

        public static bool IsHex(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

        public static bool StartsIdent(string text, int pos)
        {
            if (pos >= text.Length) return false;
            char c = text[pos];
            if (IsNameStart(c)) return true;
            if (c == '\\') return pos + 1 < text.Length;
            if (c == '-')
            {
                if (pos + 1 >= text.Length) return false;
                char d = text[pos + 1];
                return IsNameStart(d) || d == '-' || (d == '\\' && pos + 2 < text.Length);
            }
            return false;
        }

        public static string ReadIdent(string text, ref int pos)
        {
            var sb = new StringBuilder();
            while (pos < text.Length)
            {
                char c = text[pos];
                if (IsNameChar(c))
                {
                    sb.Append(c);
                    pos++;
                }
                else if (c == '\\' && pos + 1 < text.Length)
                {
                    pos++;
                    ReadEscape(text, ref pos, sb);
                }
                else break;
            }
            return sb.ToString();
        }

        public static void ReadEscape(string text, ref int pos, StringBuilder sb)
        {
            char c = text[pos];
            if (IsHex(c))
            {
                int start = pos;
                int value = 0;
                while (pos < text.Length && pos - start < 6 && IsHex(text[pos]))
                {
                    value = (value << 4) | System.Convert.ToInt32(text[pos].ToString(), 16);
                    pos++;
                }
                if (pos < text.Length && IsWhitespace(text[pos]))
                {
                    if (text[pos] == '\r' && pos + 1 < text.Length && text[pos + 1] == '\n') pos++;
                    pos++;
                }
                if (value == 0 || value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF)) sb.Append((char)0xFFFD);
                else sb.Append(char.ConvertFromUtf32(value));
            }
            else
            {
                sb.Append(c);
                pos++;
            }
        }

        public static string CollapseWhitespace(string text)
        {
            var sb = new StringBuilder(text.Length);
            bool pending = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (IsWhitespace(c))
                {
                    pending = sb.Length > 0;
                    continue;
                }
                if (pending)
                {
                    sb.Append(' ');
                    pending = false;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static string Escape(string name)
        {
            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (IsNameChar(c)) sb.Append(c);
                else sb.Append('\\').Append(c);
            }
            return sb.ToString();
        }
    }
}
