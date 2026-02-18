using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.Bonus
{
    [AddComponentMenu("Neo/" + "Bonus/" + nameof(SlotElement))]
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

        [Tooltip("Label offset in world coordinates")]
        public Vector3 gizmoLabelOffset = new(0f, 0.25f, 0f);

        [Tooltip("Gizmo marker dot size")]
        public float gizmoIconSize = 0.15f;

        [Tooltip("Label text color")] public Color gizmoColor = new(1f, 1f, 0.2f, 1f); // ярче (почти жёлтый)

        [Tooltip("Label font size")] public int gizmoFontSize = 16; // больше по умолчанию

        [Tooltip("Draw black outline for readability")]
        public bool gizmoOutline = true;

        [Tooltip("Outline color")] public Color gizmoOutlineColor = new(0f, 0f, 0f, 1f);

        [Tooltip("Outline thickness in scene units")]
        public float gizmoOutlineOffset = 0.022f;

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
        ///     Устанавливает визуальное представление элемента на основе данных.
        /// </summary>
        public void SetVisuals(SlotVisualData data)
        {
            if (data == null)
            {
                // Скрываем элемент, если нет данных
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

            // Устанавливаем ID
            id = data.id;

            // Устанавливаем спрайт
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

            // Устанавливаем описание
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

            // точка-метка на позиции
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoIconSize);

            // текст метки
            (int col, int row) = gizmoAutoDetect ? AutoDetectColRow() : (gizmoManualCol, gizmoManualRow);
            string label = $"[{col},{row}] id:{id}";

            // стиль
            GUIStyle style = new(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(10, gizmoFontSize)
            };
            style.normal.textColor = gizmoColor;

            Vector3 pos = transform.position + gizmoLabelOffset;

            // обводка (четыре смещения по диагоналям)
            if (gizmoOutline)
            {
                GUIStyle outline = new(style);
                outline.normal.textColor = gizmoOutlineColor;

                Vector3 o = Vector3.one.normalized * gizmoOutlineOffset;
                Handles.Label(pos + new Vector3(o.x, o.y, 0f), label, outline);
                Handles.Label(pos + new Vector3(o.x, -o.y, 0f), label, outline);
                Handles.Label(pos + new Vector3(-o.x, o.y, 0f), label, outline);
                Handles.Label(pos + new Vector3(-o.x, -o.y, 0f), label, outline);
            }

            // основной текст
            Handles.Label(pos, label, style);
        }

        private (int col, int row) AutoDetectColRow()
        {
            Row rowComp = GetComponentInParent<Row>();
            int rowIndex = -1;
            int colIndex = -1;

            // индекс строки внутри Row: снизу-вверх
            if (rowComp != null && rowComp.SlotElements != null && rowComp.SlotElements.Length > 0)
            {
                SlotElement[] sortedByY = rowComp.SlotElements
                    .OrderBy(se => se.transform.position.y)
                    .ToArray();

                for (int i = 0; i < sortedByY.Length; i++)
                {
                    if (sortedByY[i] == this)
                    {
                        rowIndex = i; // 0 = низ, 1 = центр, 2 = верх
                        break;
                    }
                }
            }

            // индекс колонки: слева-направо среди всех Row общего родителя
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