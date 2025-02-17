using UnityEngine;
using UnityEngine.UI;

namespace Neo.Bonus
{
    public class SlotElement : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _sprite;

        public Sprite Sprite
        {
            get => _sprite; set
            {
                _sprite = value;

                SetSprite(value);
            }
        }

        private void SetSprite(Sprite value)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.sprite = value;

            if (_image != null)
                _image.sprite = value;
        }

        private void OnValidate()
        {
            _spriteRenderer ??= GetComponent<SpriteRenderer>();
        }
    }
}
