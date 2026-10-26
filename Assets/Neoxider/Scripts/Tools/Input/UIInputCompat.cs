using System;
using System.Collections.Generic;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Tools
{
    /// <summary>
    ///     Mouse-compatible UI picking above the input-independent UIUtils coordinate API.
    /// </summary>
    public static class UIInputCompat
    {
        /// <summary>Returns a new list of UI hits under the compatible mouse pointer.</summary>
        public static List<RaycastResult> GetUIElementsUnderCursor()
        {
            List<RaycastResult> results = new();
            GetUIElementsUnderCursor(results);
            return results;
        }

        /// <summary>
        ///     Clears and fills a caller-owned buffer. Missing mouse/EventSystem returns zero hits.
        ///     UIUtils still constructs PointerEventData for each query.
        /// </summary>
        public static int GetUIElementsUnderCursor(List<RaycastResult> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (!MouseInputCompat.TryGetPosition(out Vector3 position))
            {
                results.Clear();
                return 0;
            }

            return UIUtils.GetUIElementsAtPosition(position, results);
        }

        /// <summary>Checks whether the compatible mouse pointer hits any UI element.</summary>
        public static bool IsPointerOverUI()
        {
            return GetUIElementsUnderCursor().Count > 0;
        }
    }
}
