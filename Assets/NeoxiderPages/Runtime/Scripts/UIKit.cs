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
    }
}