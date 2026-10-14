using System;
using System.Collections.Generic;
using System.Globalization;

namespace HtmlToUitk.Core.Css
{
    public static class ColorParser
    {
        private const string NamedTable =
            "aliceblue:f0f8ff,antiquewhite:faebd7,aqua:00ffff,aquamarine:7fffd4,azure:f0ffff,beige:f5f5dc,bisque:ffe4c4,black:000000,blanchedalmond:ffebcd,blue:0000ff," +
            "blueviolet:8a2be2,brown:a52a2a,burlywood:deb887,cadetblue:5f9ea0,chartreuse:7fff00,chocolate:d2691e,coral:ff7f50,cornflowerblue:6495ed,cornsilk:fff8dc,crimson:dc143c," +
            "cyan:00ffff,darkblue:00008b,darkcyan:008b8b,darkgoldenrod:b8860b,darkgray:a9a9a9,darkgreen:006400,darkgrey:a9a9a9,darkkhaki:bdb76b,darkmagenta:8b008b,darkolivegreen:556b2f," +
            "darkorange:ff8c00,darkorchid:9932cc,darkred:8b0000,darksalmon:e9967a,darkseagreen:8fbc8f,darkslateblue:483d8b,darkslategray:2f4f4f,darkslategrey:2f4f4f,darkturquoise:00ced1,darkviolet:9400d3," +
            "deeppink:ff1493,deepskyblue:00bfff,dimgray:696969,dimgrey:696969,dodgerblue:1e90ff,firebrick:b22222,floralwhite:fffaf0,forestgreen:228b22,fuchsia:ff00ff,gainsboro:dcdcdc," +
            "ghostwhite:f8f8ff,gold:ffd700,goldenrod:daa520,gray:808080,green:008000,greenyellow:adff2f,grey:808080,honeydew:f0fff0,hotpink:ff69b4,indianred:cd5c5c," +
            "indigo:4b0082,ivory:fffff0,khaki:f0e68c,lavender:e6e6fa,lavenderblush:fff0f5,lawngreen:7cfc00,lemonchiffon:fffacd,lightblue:add8e6,lightcoral:f08080,lightcyan:e0ffff," +
            "lightgoldenrodyellow:fafad2,lightgray:d3d3d3,lightgreen:90ee90,lightgrey:d3d3d3,lightpink:ffb6c1,lightsalmon:ffa07a,lightseagreen:20b2aa,lightskyblue:87cefa,lightslategray:778899,lightslategrey:778899," +
            "lightsteelblue:b0c4de,lightyellow:ffffe0,lime:00ff00,limegreen:32cd32,linen:faf0e6,magenta:ff00ff,maroon:800000,mediumaquamarine:66cdaa,mediumblue:0000cd,mediumorchid:ba55d3," +
            "mediumpurple:9370db,mediumseagreen:3cb371,mediumslateblue:7b68ee,mediumspringgreen:00fa9a,mediumturquoise:48d1cc,mediumvioletred:c71585,midnightblue:191970,mintcream:f5fffa,mistyrose:ffe4e1,moccasin:ffe4b5," +
            "navajowhite:ffdead,navy:000080,oldlace:fdf5e6,olive:808000,olivedrab:6b8e23,orange:ffa500,orangered:ff4500,orchid:da70d6,palegoldenrod:eee8aa,palegreen:98fb98," +
            "paleturquoise:afeeee,palevioletred:db7093,papayawhip:ffefd5,peachpuff:ffdab9,peru:cd853f,pink:ffc0cb,plum:dda0dd,powderblue:b0e0e6,purple:800080,rebeccapurple:663399," +
            "red:ff0000,rosybrown:bc8f8f,royalblue:4169e1,saddlebrown:8b4513,salmon:fa8072,sandybrown:f4a460,seagreen:2e8b57,seashell:fff5ee,sienna:a0522d,silver:c0c0c0," +
            "skyblue:87ceeb,slateblue:6a5acd,slategray:708090,slategrey:708090,snow:fffafa,springgreen:00ff7f,steelblue:4682b4,tan:d2b48c,teal:008080,thistle:d8bfd8," +
            "tomato:ff6347,turquoise:40e0d0,violet:ee82ee,wheat:f5deb3,white:ffffff,whitesmoke:f5f5f5,yellow:ffff00,yellowgreen:9acd32";

        private static readonly Dictionary<string, int> Named = BuildNamed();

        private static Dictionary<string, int> BuildNamed()
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string pair in NamedTable.Split(','))
            {
                int colon = pair.IndexOf(':');
                map[pair.Substring(0, colon)] = int.Parse(pair.Substring(colon + 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            return map;
        }

        public static bool IsNamedColor(string name)
        {
            return name != null && (name == "transparent" || Named.ContainsKey(name.ToLowerInvariant()));
        }

        public static IReadOnlyCollection<string> NamedColors => Named.Keys;

        public static bool TryParseHex(string text, out CssColor color)
        {
            color = null;
            if (text == null || text.Length < 4 || text[0] != '#') return false;
            string digits = text.Substring(1);
            foreach (char c in digits) if (!CssChars.IsHex(c)) return false;
            int r, g, b;
            double a = 1;
            switch (digits.Length)
            {
                case 3:
                case 4:
                    r = Nibble(digits[0]) * 17;
                    g = Nibble(digits[1]) * 17;
                    b = Nibble(digits[2]) * 17;
                    if (digits.Length == 4) a = Nibble(digits[3]) * 17 / 255.0;
                    break;
                case 6:
                case 8:
                    r = Byte(digits, 0);
                    g = Byte(digits, 2);
                    b = Byte(digits, 4);
                    if (digits.Length == 8) a = Byte(digits, 6) / 255.0;
                    break;
                default:
                    return false;
            }
            color = new CssColor(r, g, b, a, text);
            return true;
        }

        private static int Nibble(char c) => int.Parse(c.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        private static int Byte(string digits, int index) => int.Parse(digits.Substring(index, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        public static bool TryParse(CssValue value, out CssColor color)
        {
            color = null;
            if (value == null) return false;
            var direct = value as CssColor;
            if (direct != null)
            {
                color = direct;
                return true;
            }
            var ident = value as CssIdent;
            if (ident != null)
            {
                string name = ident.Name.ToLowerInvariant();
                if (name == "transparent")
                {
                    color = new CssColor(0, 0, 0, 0, ident.Name);
                    return true;
                }
                int rgb;
                if (Named.TryGetValue(name, out rgb))
                {
                    color = new CssColor((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF, 1, ident.Name);
                    return true;
                }
                return false;
            }
            var raw = value as CssRaw;
            if (raw != null) return TryParseHex(raw.Text.Trim(), out color);
            return false;
        }

        public static bool TryFromFunction(CssFunction function, string authored, out CssColor color)
        {
            color = null;
            string name = function.Name.ToLowerInvariant();
            if (name != "rgb" && name != "rgba") return false;
            var items = new List<CssValue>();
            if (function.Args.Count == 1 && function.Args[0] is CssList list && list.Separator == ' ')
            {
                foreach (CssValue item in list.Items)
                {
                    var op = item as CssIdent;
                    if (op != null && op.Name == "/") continue;
                    items.Add(item);
                }
            }
            else if (function.Args.Count >= 3 && function.Args.Count <= 4)
            {
                foreach (CssValue item in function.Args) items.Add(item);
            }
            else return false;
            if (items.Count < 3 || items.Count > 4) return false;
            var channels = new int[3];
            for (int i = 0; i < 3; i++)
            {
                double v;
                if (items[i] is CssNumber number) v = number.Value;
                else if (items[i] is CssPercent percent) v = percent.Value * 2.55;
                else return false;
                channels[i] = ClampByte(v);
            }
            double alpha = 1;
            if (items.Count == 4)
            {
                if (items[3] is CssNumber an) alpha = an.Value;
                else if (items[3] is CssPercent ap) alpha = ap.Value / 100.0;
                else return false;
                if (alpha < 0) alpha = 0;
                if (alpha > 1) alpha = 1;
            }
            color = new CssColor(channels[0], channels[1], channels[2], alpha, authored);
            return true;
        }

        private static int ClampByte(double value)
        {
            int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            if (rounded < 0) return 0;
            if (rounded > 255) return 255;
            return rounded;
        }
    }
}
