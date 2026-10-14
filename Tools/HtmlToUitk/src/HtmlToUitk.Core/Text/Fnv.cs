using System.Text;

namespace HtmlToUitk.Core.Text
{
    public static class Fnv
    {
        public static uint Hash32(string text)
        {
            uint hash = 2166136261;
            byte[] bytes = Encoding.UTF8.GetBytes(text ?? "");
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= 16777619;
            }
            return hash;
        }

        public static string Hex8(string text)
        {
            return Hash32(text).ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
