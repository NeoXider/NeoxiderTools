using UnityEngine;

namespace Neo.Pages
{
    /// <summary>
    /// Идентификатор страницы как ассет (ScriptableObject).
    /// Используется для расширяемого выбора страниц без правки enum.
    /// </summary>
    [CreateAssetMenu(menuName = "Neo/Pages/Page Id", fileName = "Page")]
    public sealed class PageId : ScriptableObject
    {
        /// <summary>
        /// Стабильный ключ страницы.
        /// По умолчанию генерируется из имени ассета.
        /// Рекомендуемый формат: <c>PageMenu</c>, <c>PageShop</c>, <c>PageSettings</c>.
        /// </summary>
        public string Id => name;

        /// <summary>
        /// Отображаемое имя (без префикса <c>Page</c>, если он есть).
        /// </summary>
        public string DisplayName => name.StartsWith("Page") && name.Length > 4 ? name.Substring(4) : name;
    }
}