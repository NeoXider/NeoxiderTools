using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Localization
{
    public interface ILocalizer
    {
        string Resolve(string key);
    }

    public sealed class LocalizationKey
    {
        public LocalizationKey(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }

    public static class UitkLocalization
    {
        private static readonly Regex KeyPattern = new Regex("^[a-z0-9][A-Za-z0-9_-]*(\\.[A-Za-z0-9_-]+)+$", RegexOptions.CultureInvariant);

        public static bool LooksLikeKey(string text)
        {
            return !string.IsNullOrEmpty(text) && KeyPattern.IsMatch(text);
        }

        public static int Apply(VisualElement root, ILocalizer localizer)
        {
            if (root == null || localizer == null)
                return 0;
            int count = 0;
            root.Query<TextElement>().ForEach(element =>
            {
                LocalizationKey marker = element.userData as LocalizationKey;
                string key = marker != null ? marker.Key : element.text;
                if (marker != null ? string.IsNullOrEmpty(key) : !LooksLikeKey(key))
                    return;
                string resolved = localizer.Resolve(key);
                if (resolved == null)
                    return;
                element.userData = new LocalizationKey(key);
                element.text = resolved;
                count++;
            });
            return count;
        }
    }
}
