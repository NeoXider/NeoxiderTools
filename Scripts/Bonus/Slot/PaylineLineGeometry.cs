using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     Shared world-position math for payline cells (editor gizmos + runtime LineRenderer).
    /// </summary>
    internal static class PaylineLineGeometry
    {
        public static Vector3 GetCellWorld(Row row, int rowFromBottom)
        {
            if (row == null)
            {
                return Vector3.zero;
            }

            // WHY: one window classification per cell instead of one per reel element (each used to allocate).
            SlotElement visible = row.GetVisibleElement(rowFromBottom);
            return visible != null ? visible.transform.position : GetApproxCellWorld(row, rowFromBottom);
        }

        private static Vector3 GetApproxCellWorld(Row row, int rowFromBottom)
        {
            float step = Mathf.Abs(row.spaceY) > 1e-6f ? Mathf.Abs(row.spaceY) : 1f;
            float localY = row.offsetY + rowFromBottom * step;

            Transform t = row.transform;
            if (t is RectTransform rt)
            {
                return rt.TransformPoint(new Vector3(rt.rect.center.x, localY, 0f));
            }

            return t.TransformPoint(new Vector3(0f, localY, 0f));
        }
    }
}
