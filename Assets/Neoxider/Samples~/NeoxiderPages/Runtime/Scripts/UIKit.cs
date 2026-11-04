using UnityEngine;
using UnityEngine.Events;

namespace Neo.Pages
{
    /// <summary>
    ///     Static event bus for showing UI pages. The scene's <see cref="PM" /> listens to it while it exists.
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

        // WHY: with Enter Play Mode Options (no domain reload) listeners of the previous play session would still be
        // attached and call into destroyed objects.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            OnShowPage ??= new UnityEvent<PageId>();
            OnShowPageByName ??= new UnityEvent<string>();
            OnShowPage.RemoveAllListeners();
            OnShowPageByName.RemoveAllListeners();
        }

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
