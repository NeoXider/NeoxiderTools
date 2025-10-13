using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.Bonus
{
    public class SlotElement : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Image _image;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TMP_Text _textDescription;

        [Header("Debug Gizmo")]
        [Tooltip("Включить/выключить гизмо-лейбл над элементом")]
        public bool gizmoEnabled = true;

        [Tooltip("Автоматически определять [col,row] по позициям в иерархии")]
        public bool gizmoAutoDetect = true;

        [Tooltip("Ручной индекс колонки (если AutoDetect выключен)")]
        public int gizmoManualCol = -1;

        [Tooltip("Ручной индекс строки (если AutoDetect выключен)")]
        public int gizmoManualRow = -1;

        [Tooltip("Смещение лейбла в мировых координатах")]
        public Vector3 gizmoLabelOffset = new Vector3(0f, 0.25f, 0f);

        [Tooltip("Размер маркера-точки гизмо")]
        public float gizmoIconSize = 0.15f;

        [Tooltip("Цвет текста лейбла")]
        public Color gizmoColor = new Color(1f, 1f, 0.2f, 1f); // ярче (почти жёлтый)

        [Tooltip("Размер шрифта лейбла")]
        public int gizmoFontSize = 16; // больше по умолчанию

        [Tooltip("Рисовать чёрную обводку для читаемости")]
        public bool gizmoOutline = true;

        [Tooltip("Цвет обводки")]
        public Color gizmoOutlineColor = new Color(0f, 0f, 0f, 1f);

        [Tooltip("Толщина обводки в юнитах сцены")]
        public float gizmoOutlineOffset = 0.012f;

        public int id { get; private set; }

        private void OnValidate()
        {
            _spriteRenderer ??= GetComponent<SpriteRenderer>();
            _image ??= GetComponent<Image>();
            if (_textDescription == null) _textDescription = GetComponentInChildren<TMP_Text>();
        }

        /// <summary>
        ///     Устанавливает визуальное представление элемента на основе данных.
        /// </summary>
        public void SetVisuals(SlotVisualData data)
        {
            if (data == null)
            {
                // Скрываем элемент, если нет данных
                if (_image) _image.enabled = false;
                if (_spriteRenderer) _spriteRenderer.enabled = false;
                if (_textDescription) _textDescription.gameObject.SetActive(false);
                return;
            }

            // Устанавливаем ID
            id = data.id;

            // Устанавливаем спрайт
            if (_image != null)
            {
                _image.enabled = true;
                _image.sprite = data.sprite;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = true;
                _spriteRenderer.sprite = data.sprite;
            }

            // Устанавливаем описание
            if (_textDescription != null)
            {
                var hasDescription = !string.IsNullOrEmpty(data.description);
                _textDescription.gameObject.SetActive(hasDescription);
                if (hasDescription) _textDescription.text = data.description;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!gizmoEnabled) return;

            // точка-метка на позиции
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoIconSize);

            // текст метки
            var (col, row) = gizmoAutoDetect ? AutoDetectColRow() : (gizmoManualCol, gizmoManualRow);
            string label = $"[{col},{row}] id:{id}";

            // стиль
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(10, gizmoFontSize)
            };
            style.normal.textColor = gizmoColor;

            Vector3 pos = transform.position + gizmoLabelOffset;

            // обводка (четыре смещения по диагоналям)
            if (gizmoOutline)
            {
                GUIStyle outline = new GUIStyle(style);
                outline.normal.textColor = gizmoOutlineColor;

                Vector3 o = Vector3.one.normalized * gizmoOutlineOffset;
                Handles.Label(pos + new Vector3( o.x,  o.y, 0f), label, outline);
                Handles.Label(pos + new Vector3( o.x, -o.y, 0f), label, outline);
                Handles.Label(pos + new Vector3(-o.x,  o.y, 0f), label, outline);
                Handles.Label(pos + new Vector3(-o.x, -o.y, 0f), label, outline);
            }

            // основной текст
            Handles.Label(pos, label, style);
        }

        private (int col, int row) AutoDetectColRow()
        {
            var rowComp = GetComponentInParent<Row>();
            int rowIndex = -1;
            int colIndex = -1;

            // индекс строки внутри Row: сверху-вниз
            if (rowComp != null && rowComp.SlotElements != null && rowComp.SlotElements.Length > 0)
            {
                var sortedByY = rowComp.SlotElements
                    .OrderByDescending(se => GetLocalY(se.transform))
                    .ToArray();

                for (int i = 0; i < sortedByY.Length; i++)
                {
                    if (sortedByY[i] == this)
                    {
                        rowIndex = i; // 0 = верх, 1 = центр, 2 = низ
                        break;
                    }
                }
            }

            // индекс колонки: слева-направо среди всех Row общего родителя
            if (rowComp != null && rowComp.transform.parent != null)
            {
                var allRows = rowComp.transform.parent.GetComponentsInChildren<Row>(true);
                var sortedByX = allRows.OrderBy(r => GetLocalX(r.transform)).ToArray();

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

        private static float GetLocalX(Transform t)
        {
            if (t is RectTransform rt) return rt.anchoredPosition.x;
            return t.localPosition.x;
        }

        private static float GetLocalY(Transform t)
        {
            if (t is RectTransform rt) return rt.anchoredPosition.y;
            return t.localPosition.y;
        }
#endif
    }
}
