using UnityEngine.Events;

namespace Neo.Pages
{
    /// <summary>
    ///     Static event bus for showing UI pages.
    /// </summary>
    public class UIKit
    {
        /// <summary>
        ///     Event requesting a page show by <see cref="PageId" />.
        /// </summary>
        public static UnityEvent<PageId> OnShowPage = new();

        /// <summary>
        ///     Event requesting a page show by <see cref="PageId" /> asset name.
        /// </summary>
        public static UnityEvent<string> OnShowPageByName = new();

        /// <summary>
        ///     Requests showing a page (raises <see cref="OnShowPage" />).
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
        ///     Requests showing a page by PageId asset name (e.g. "PageEnd").
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
