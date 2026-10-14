using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HtmlToUitk.Core.Html
{
    public static class HtmlEntities
    {
        private const string Latin1 = "nbsp iexcl cent pound curren yen brvbar sect uml copy ordf laquo not shy reg macr deg plusmn sup2 sup3 acute micro para middot cedil sup1 ordm raquo frac14 frac12 frac34 iquest Agrave Aacute Acirc Atilde Auml Aring AElig Ccedil Egrave Eacute Ecirc Euml Igrave Iacute Icirc Iuml ETH Ntilde Ograve Oacute Ocirc Otilde Ouml times Oslash Ugrave Uacute Ucirc Uuml Yacute THORN szlig agrave aacute acirc atilde auml aring aelig ccedil egrave eacute ecirc euml igrave iacute icirc iuml eth ntilde ograve oacute ocirc otilde ouml divide oslash ugrave uacute ucirc uuml yacute thorn yuml";

        private const string Others =
            "quot=34 amp=38 apos=39 lt=60 gt=62 OElig=338 oelig=339 Scaron=352 scaron=353 Yuml=376 fnof=402 circ=710 tilde=732 " +
            "Alpha=913 Beta=914 Gamma=915 Delta=916 Epsilon=917 Zeta=918 Eta=919 Theta=920 Iota=921 Kappa=922 Lambda=923 Mu=924 Nu=925 Xi=926 Omicron=927 Pi=928 Rho=929 Sigma=931 Tau=932 Upsilon=933 Phi=934 Chi=935 Psi=936 Omega=937 " +
            "alpha=945 beta=946 gamma=947 delta=948 epsilon=949 zeta=950 eta=951 theta=952 iota=953 kappa=954 lambda=955 mu=956 nu=957 xi=958 omicron=959 pi=960 rho=961 sigmaf=962 sigma=963 tau=964 upsilon=965 phi=966 chi=967 psi=968 omega=969 thetasym=977 upsih=978 piv=982 " +
            "ensp=8194 emsp=8195 thinsp=8201 zwnj=8204 zwj=8205 lrm=8206 rlm=8207 ndash=8211 mdash=8212 lsquo=8216 rsquo=8217 sbquo=8218 ldquo=8220 rdquo=8221 bdquo=8222 dagger=8224 Dagger=8225 bull=8226 hellip=8230 permil=8240 prime=8242 Prime=8243 lsaquo=8249 rsaquo=8250 oline=8254 frasl=8260 euro=8364 " +
            "image=8465 weierp=8472 real=8476 trade=8482 alefsym=8501 larr=8592 uarr=8593 rarr=8594 darr=8595 harr=8596 crarr=8629 lArr=8656 uArr=8657 rArr=8658 dArr=8659 hArr=8660 " +
            "forall=8704 part=8706 exist=8707 empty=8709 nabla=8711 isin=8712 notin=8713 ni=8715 prod=8719 sum=8721 minus=8722 lowast=8727 radic=8730 prop=8733 infin=8734 ang=8736 and=8743 or=8744 cap=8745 cup=8746 int=8747 there4=8756 sim=8764 cong=8773 asymp=8776 ne=8800 equiv=8801 le=8804 ge=8805 sub=8834 sup=8835 nsub=8836 sube=8838 supe=8839 oplus=8853 otimes=8855 perp=8869 sdot=8901 " +
            "lceil=8968 rceil=8969 lfloor=8970 rfloor=8971 lang=10216 rang=10217 loz=9674 spades=9824 clubs=9827 hearts=9829 diams=9830 check=10003 cross=10007 star=9734 starf=9733 phone=9742 nbsp=160 " +
            "AMP=38 LT=60 GT=62 QUOT=34 COPY=169 REG=174 Tab=9 NewLine=10 excl=33 num=35 dollar=36 percnt=37 lpar=40 rpar=41 ast=42 plus=43 comma=44 period=46 sol=47 colon=58 semi=59 equals=61 quest=63 commat=64 lsqb=91 bsol=92 rsqb=93 Hat=94 lowbar=95 grave=96 lcub=123 verbar=124 vert=124 rcub=125 hyphen=8208 dash=8208 horbar=8213 bigstar=9733 checkmark=10003 hearts=9829";

        private static readonly Dictionary<string, string> Named = BuildNamed();
        private static readonly HashSet<string> Legacy = BuildLegacy();
        private static readonly int[] Windows1252 =
        {
            0x20AC, 0x81, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021, 0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x8D, 0x017D, 0x8F,
            0x90, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014, 0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x9D, 0x017E, 0x0178
        };

        private static Dictionary<string, string> BuildNamed()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] latin = Latin1.Split(' ');
            for (int i = 0; i < latin.Length; i++) map[latin[i]] = char.ConvertFromUtf32(160 + i);
            foreach (string pair in Others.Split(' '))
            {
                int eq = pair.IndexOf('=');
                map[pair.Substring(0, eq)] = char.ConvertFromUtf32(int.Parse(pair.Substring(eq + 1), CultureInfo.InvariantCulture));
            }
            return map;
        }

        private static HashSet<string> BuildLegacy()
        {
            var set = new HashSet<string>(Latin1.Split(' '), StringComparer.Ordinal) { "amp", "lt", "gt", "quot", "AMP", "LT", "GT", "QUOT", "COPY", "REG" };
            return set;
        }

        public static bool IsKnown(string name) => name != null && Named.ContainsKey(name);

        public static string Decode(string text) => Decode(text, false);

        public static string Decode(string text, bool inAttribute)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('&') < 0) return text ?? "";
            var sb = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c != '&')
                {
                    sb.Append(c);
                    i++;
                    continue;
                }
                int consumed = TryDecode(text, i, inAttribute, sb);
                if (consumed == 0)
                {
                    sb.Append('&');
                    i++;
                }
                else i += consumed;
            }
            return sb.ToString();
        }

        private static int TryDecode(string text, int start, bool inAttribute, StringBuilder sb)
        {
            int i = start + 1;
            if (i >= text.Length) return 0;
            if (text[i] == '#')
            {
                i++;
                bool hex = i < text.Length && (text[i] == 'x' || text[i] == 'X');
                if (hex) i++;
                int digitsStart = i;
                long value = 0;
                while (i < text.Length && (hex ? CssHex(text[i]) : (text[i] >= '0' && text[i] <= '9')))
                {
                    if (value <= 0x10FFFF) value = value * (hex ? 16 : 10) + int.Parse(text[i].ToString(), hex ? NumberStyles.HexNumber : NumberStyles.Integer, CultureInfo.InvariantCulture);
                    i++;
                }
                if (i == digitsStart) return 0;
                if (i < text.Length && text[i] == ';') i++;
                sb.Append(FromCodePoint(value));
                return i - start;
            }
            int nameStart = i;
            while (i < text.Length && ((text[i] >= 'a' && text[i] <= 'z') || (text[i] >= 'A' && text[i] <= 'Z') || (text[i] >= '0' && text[i] <= '9'))) i++;
            if (i == nameStart) return 0;
            string name = text.Substring(nameStart, i - nameStart);
            if (i < text.Length && text[i] == ';')
            {
                string value;
                if (Named.TryGetValue(name, out value))
                {
                    sb.Append(value);
                    return i + 1 - start;
                }
            }
            for (int length = name.Length; length >= 2; length--)
            {
                string prefix = name.Substring(0, length);
                if (!Legacy.Contains(prefix)) continue;
                int after = nameStart + length;
                if (inAttribute && after < text.Length && (text[after] == '=' || (text[after] >= 'a' && text[after] <= 'z') || (text[after] >= 'A' && text[after] <= 'Z') || (text[after] >= '0' && text[after] <= '9'))) return 0;
                sb.Append(Named[prefix]);
                return after - start;
            }
            return 0;
        }

        private static bool CssHex(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

        private static string FromCodePoint(long value)
        {
            if (value == 0 || value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF)) return ((char)0xFFFD).ToString();
            if (value >= 0x80 && value <= 0x9F) value = Windows1252[value - 0x80];
            return char.ConvertFromUtf32((int)value);
        }
    }
}
