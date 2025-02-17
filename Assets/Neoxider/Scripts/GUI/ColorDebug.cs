
namespace Neo
{
    public enum ColorHTML
    {
        aqua,
        black,
        blue,
        brown,
        cyan,
        darkblue,
        fuchsia,
        green,
        grey,
        lightblue,
        lime,
        magenta,
        maroon,
        navy,
        olive,
        orange,
        purple,
        red,
        silver,
        teal,
        white,
        yellow
    }

    public static class ColorDebug
    {
        public static string GetColorString(string text, ColorHTML type = ColorHTML.cyan, bool bold = false)
        {
            string boldS = "";
            string boldE = "";

            if(bold)
            {
                boldS = "<b>";
                boldE = "</b>";
            }

            return "<color=" + type.ToString() + ">"
                +boldS + text + boldE + "</color>";
        }

        public static string AddColor(this string text, ColorHTML type = ColorHTML.cyan, bool bold = false)
        {
            return GetColorString(text, type, bold);
        }
    }
}
