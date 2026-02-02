using UnityEngine.Events;

namespace Neo.Pages
{
    /// <summary>
    /// Статическая шина событий для показа UI-страниц.
    /// </summary>
    public class UIKit
    {
        /// <summary>
        /// Событие запроса показа страницы по <see cref="PageId"/>.
        /// </summary>
        public static UnityEvent<PageId> OnShowPage = new();

        /// <summary>
        /// Событие запроса показа страницы по имени <see cref="PageId"/>.
        /// </summary>
        public static UnityEvent<string> OnShowPageByName = new();

        /// <summary>
        /// Запрашивает показ страницы (генерирует событие <see cref="OnShowPage"/>).
        /// </summary>
        public static void ShowPage(PageId pageId)
        {
            if (pageId == null)
            {
                return;
            }

            OnShowPage.Invoke(pageId);
        }

        /// <summary>
        /// Запрашивает показ страницы по имени PageId (например, "PageEnd").
        /// </summary>
        public static void ShowPage(string pageIdName)
        {
            if (string.IsNullOrWhiteSpace(pageIdName))
            {
                return;
            }

            OnShowPageByName.Invoke(pageIdName);
        }
    }
}