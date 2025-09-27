using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Bonus
{
    public class SlotElement : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TMP_Text _textDescription;

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
    }
}