using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.Bonus
{
    /// <summary>How SlotElement draws row index in scene gizmo labels.</summary>
    public enum SlotGizmoRowLabelMode
    {
        /// <summary>Index among all reel elements sorted by Y (legacy).</summary>
        FullReelOrderedByY,

        /// <summary>Row within visible window: 0 = bottom … matches LinesData / SpinController matrix.</summary>
        VisibleWindowBottomUp
    }

    [NeoDoc("Bonus/Slot/SlotElement.md")]
    [CreateFromMenu("Neoxider/Bonus/SlotElement", "Prefabs/Bonus/Slot/SlotElement.prefab")]
    [AddComponentMenu("Neoxider/" + "Bonus/" + nameof(SlotElement))]
    public class SlotElement : MonoBehaviour
    {
        [Header("Refs")] [SerializeField] public Image image;

        [SerializeField] public SpriteRenderer spriteRenderer;
        [SerializeField] public TMP_Text textDescription;

        [Header("Debug Gizmo")] [Tooltip("Enable/disable gizmo label above element")]
        public bool gizmoEnabled = true;

        [Tooltip("Auto-detect [col,row] from hierarchy positions")]
        public bool gizmoAutoDetect = true;

        [Tooltip("Manual column index (when AutoDetect off)")]
        public int gizmoManualCol = -1;

        [Tooltip("Manual row index (when AutoDetect off)")]
        public int gizmoManualRow = -1;

        [Tooltip("World-space tweak after visual center (pivot vs graphic center).")]
        public Vector3 gizmoLabelOffset = new(0f, 0f, 0f);

        [Tooltip("Gizmo marker dot size")] public float gizmoIconSize = 0.15f;

        [Tooltip("Label text color")] public Color gizmoColor = new(1f, 1f, 0.2f, 1f); // brighter (near yellow)

        [Tooltip("Label font size")] public int gizmoFontSize = 22;

        [Tooltip(
            "Подъём текста над центром графики в пикселях Scene view (фиксированный при зуме). Уменьшите, если текст «висит» слишком высоко.")]
        [Min(0f)]
        public float gizmoLabelRaiseScreenPixels = 12f;

        [Tooltip(
            "При режиме Visible Window + Auto: не рисовать гизмо у символов вне видимого окна маски (крутящиеся сверху/снизу).")]
        public bool gizmoOnlyVisibleWindowSlots = true;

        [Tooltip("Draw black outline for readability")]
        public bool gizmoOutline = true;

        [Tooltip("Outline color")] public Color gizmoOutlineColor = new(0f, 0f, 0f, 1f);

        [Tooltip("Outline thickness in screen pixels (Scene view GUI).")]
        [Min(0)]
        public int gizmoOutlineScreenPixels = 1;

        [Tooltip("Row label mode for Auto Detect.")]
        public SlotGizmoRowLabelMode gizmoRowLabelMode = SlotGizmoRowLabelMode.VisibleWindowBottomUp;

        [Tooltip("Added to displayed column and row indices in the label.")]
        public int gizmoLabelIndexOffset;

        public int id { get; private set; }

        private void OnValidate()
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            image ??= GetComponent<Image>();
            if (textDescription == null)
            {
                textDescription = GetComponentInChildren<TMP_Text>();
            }
        }

        /// <summary>
        ///     Sets the element visuals from slot data.
        /// </summary>
        public void SetVisuals(SlotVisualData data)
        {
            if (data == null)
            {
                // Hide element when there is no data
                if (image)
                {
                    image.enabled = false;
                }

                if (spriteRenderer)
                {
                    spriteRenderer.enabled = false;
                }

                if (textDescription)
                {
                    textDescription.gameObject.SetActive(false);
                }

                return;
            }

            // Set ID
            id = data.id;

            // Set sprite
            if (image != null)
            {
                image.enabled = true;
                image.sprite = data.sprite;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = data.sprite;
            }

            // Set description
            if (textDescription != null)
            {
                bool hasDescription = !string.IsNullOrEmpty(data.description);
                textDescription.gameObject.SetActive(hasDescription);
                if (hasDescription)
                {
                    textDescription.text = data.description;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!gizmoEnabled)
            {
                return;
            }

            SpinController owner = GetComponentInParent<SpinController>();
            if (owner != null && !owner.DrawSlotElementGizmosInScene)
            {
                return;
            }

            if (!ShouldDrawGizmoForSlotPosition())
            {
                return;
            }

            Vector3 anchorWorld = GetVisualAnchorWorld();

            // Marker dot at visual center (matches label anchor)
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(anchorWorld, gizmoIconSize);

            // Label text
            (int col, int row) = gizmoAutoDetect ? AutoDetectColRow() : (gizmoManualCol, gizmoManualRow);
            string label =
                $"[{col + gizmoLabelIndexOffset},{row + gizmoLabelIndexOffset}] id:{id}";

            // Style
            GUIStyle style = new(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(10, gizmoFontSize)
            };
            style.normal.textColor = gizmoColor;

            SceneView sv = SceneView.currentDrawingSceneView;
            if (sv != null && sv.camera != null && sv.camera.WorldToScreenPoint(anchorWorld).z < 0f)
            {
                return;
            }

            Handles.BeginGUI();
            try
            {
                // WorldToGUIPoint matches active Scene view projection (stable across zoom when anchor is correct).
                Vector2 gui = HandleUtility.WorldToGUIPoint(anchorWorld);
                gui.y -= gizmoLabelRaiseScreenPixels;

                GUIContent content = new(label);
                Vector2 size = style.CalcSize(content);
                Rect rect = new Rect(
                    Mathf.Round(gui.x - size.x * 0.5f),
                    Mathf.Round(gui.y - size.y),
                    size.x,
                    size.y);

                if (gizmoOutline && gizmoOutlineScreenPixels > 0)
                {
                    GUIStyle outlineStyle = new(style);
                    outlineStyle.normal.textColor = gizmoOutlineColor;
                    int o = gizmoOutlineScreenPixels;
                    for (int dx = -o; dx <= o; dx++)
                    {
                        for (int dy = -o; dy <= o; dy++)
                        {
                            if (dx == 0 && dy == 0)
                            {
                                continue;
                            }

                            GUI.Label(
                                new Rect(rect.x + dx, rect.y + dy, rect.width, rect.height),
                                label,
                                outlineStyle);
                        }
                    }
                }

                GUI.Label(rect, label, style);
            }
            finally
            {
                Handles.EndGUI();
            }
        }

        private bool ShouldDrawGizmoForSlotPosition()
        {
            if (!gizmoOnlyVisibleWindowSlots || !gizmoAutoDetect ||
                gizmoRowLabelMode != SlotGizmoRowLabelMode.VisibleWindowBottomUp)
            {
                return true;
            }

            Row rowComp = GetComponentInParent<Row>();
            return rowComp != null && rowComp.TryGetWindowRowFromBottom(this, out _);
        }

        /// <summary>
        ///     Pivot UI часто не в центре спрайта — подпись «плыла» при зуме/смещалась. Центр по rect / bounds.
        /// </summary>
        private Vector3 GetVisualAnchorWorld()
        {
            RectTransform rt = transform as RectTransform;
            if (rt == null && image != null)
            {
                rt = image.rectTransform;
            }

            if (rt != null)
            {
                Vector3[] wc = new Vector3[4];
                rt.GetWorldCorners(wc);
                Vector3 center = (wc[0] + wc[1] + wc[2] + wc[3]) * 0.25f;
                return center + gizmoLabelOffset;
            }

            if (spriteRenderer != null)
            {
                return spriteRenderer.bounds.center + gizmoLabelOffset;
            }

            return transform.position + gizmoLabelOffset;
        }

        private (int col, int row) AutoDetectColRow()
        {
            Row rowComp = GetComponentInParent<Row>();
            int rowIndex = -1;
            int colIndex = -1;

            // Row index: visible window (preferred) or full reel order
            if (gizmoRowLabelMode == SlotGizmoRowLabelMode.VisibleWindowBottomUp &&
                rowComp.TryGetWindowRowFromBottom(this, out int windowRow))
            {
                rowIndex = windowRow;
            }
            else if (rowComp != null && rowComp.SlotElements != null && rowComp.SlotElements.Length > 0)
            {
                SlotElement[] sortedByY = rowComp.SlotElements
                    .OrderBy(se => se.transform.position.y)
                    .ToArray();

                for (int i = 0; i < sortedByY.Length; i++)
                {
                    if (sortedByY[i] == this)
                    {
                        rowIndex = i;
                        break;
                    }
                }
            }

            // Column index: left to right among all Row under the same parent
            if (rowComp != null && rowComp.transform.parent != null)
            {
                Row[] allRows = rowComp.transform.parent.GetComponentsInChildren<Row>(true);
                Row[] sortedByX = allRows.OrderBy(r => r.transform.position.x).ToArray();

                for (int i = 0; i < sortedByX.Length; i++)
                {
                    if (sortedByX[i] == rowComp)
                    {
                        colIndex = i;
                        break;
                    }
                }
            }

            return (colIndex, rowIndex);
        }
#endif
    }
}
