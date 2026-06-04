using TMPro;
using UnityEngine;

namespace Neo.Demo.GridSystem
{
    [AddComponentMenu("Neoxider/Demo/GridSystem/DiceDieView")]
    public sealed class DiceDieView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private TextMeshPro _valueLabel;

        public void Initialize(int value, Sprite sprite, int sortingOrder)
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }

            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            _renderer.sprite = sprite;
            _renderer.sortingOrder = sortingOrder;

            if (value >= 10)
            {
                EnsureValueLabel();
                _valueLabel.text = value.ToString();
                _valueLabel.gameObject.SetActive(true);
                Renderer labelRenderer = _valueLabel.GetComponent<Renderer>();
                if (labelRenderer != null)
                {
                    labelRenderer.sortingOrder = sortingOrder + 1;
                }
            }
            else if (_valueLabel != null)
            {
                _valueLabel.gameObject.SetActive(false);
            }
        }

        private void EnsureValueLabel()
        {
            if (_valueLabel != null)
            {
                return;
            }

            Transform existing = transform.Find("Value");
            if (existing != null)
            {
                _valueLabel = existing.GetComponent<TextMeshPro>();
            }

            if (_valueLabel == null)
            {
                GameObject label = new("Value");
                label.transform.SetParent(transform, false);
                label.transform.localPosition = new Vector3(0f, 0.02f, -0.01f);
                _valueLabel = label.AddComponent<TextMeshPro>();
            }

            _valueLabel.fontSize = 4.5f;
            _valueLabel.alignment = TextAlignmentOptions.Center;
            _valueLabel.color = Color.white;
        }
    }
}
