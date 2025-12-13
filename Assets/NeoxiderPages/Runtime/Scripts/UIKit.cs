using UnityEngine.Events;

namespace Neo.Pages
{
    /// <summary>
    /// Статическая шина событий для показа UI-страниц.
    /// </summary>
    public class UIKit
    {
        /// <summary>
        /// Идентификатор страницы (используется в <see cref="PM"/> и UI-компонентах).
        /// </summary>
        public enum Page
        {
            None,

            _CloseCurrentPage,
            _ChangeLastPage,

            Menu,

            Settings,
            Shop,
            Leader,
            Info,
            Levels,

            Game,
            Win,
            Lose,
            Pause,
            End,

            Main,
            Grade,
            Bonus,
            Inventory,
            Map,

            Privacy,

            Other
        }

        /// <summary>
        /// Событие запроса показа страницы.
        /// </summary>
        public static UnityEvent<Page> OnShowPage = new();

        /// <summary>
        /// Запрашивает показ страницы (генерирует событие <see cref="OnShowPage"/>).
        /// </summary>
        /// <param name="page">Тип страницы.</param>
        public static void ShowPage(Page page)
        {
            OnShowPage.Invoke(page);
        }
    }
}