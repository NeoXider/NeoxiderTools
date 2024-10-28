using TMPro;
using UnityEditor;
using UnityEngine;


namespace Neoxider
{
    namespace UI
    {
        [AddComponentMenu("Neoxider/" + "UI/" + nameof(ButtonPrice))]
        public class ButtonPrice : MonoBehaviour
        {
            public enum ButtonType
            {
                Buy,
                Select,
                Selected,
            }

            [System.Serializable]
            public class Visual
            {
                public GameObject[] buy;
                public GameObject[] select;
                public GameObject[] selected;
            }

            [SerializeField] private TMP_Text _textPrice;
            [SerializeField, Min(0)] private int _price = 0;
            [Space]
            [SerializeField] private bool _textPrice_0 = false;
            [SerializeField] private ButtonType _type = ButtonType.Buy;
            [SerializeField] private GameObject[] _visuals;
            [SerializeField] private string _textSelect = "Select";
            [SerializeField] private string _textSelected = "Selected";
            [SerializeField] private string _customSeparator = ".";

            [SerializeField] private bool _editorView = true;


            public void SetAutoVisual(int price, ButtonType type = ButtonType.Buy)
            {
                type = CheckAutoType(price, type);

                SetVisual(price, type);
            }

            public void SetVisual(int price, ButtonType type = ButtonType.Buy)
            {
                _price = price;

                SetVisual(type);
            }

            public void SetPrice(int price)
            {
                SetAutoVisual(price);
            }

            private ButtonType CheckAutoType(int price, ButtonType type)
            {
                if (!_textPrice_0 && type == ButtonType.Buy && price == 0)
                    type = ButtonType.Select;

                if (price > 0 && ButtonType.Buy != type)
                {
                    type = ButtonType.Buy;
                }

                return type;
            }

            public void SetVisual(ButtonType type)
            {
                SetVisualId((int)type);
            }

            public void SetVisualId(int id)
            {
                _type = (ButtonType)id;

                if (_visuals != null)
                {
                    for (int i = 0; i < _visuals.Length; i++)
                    {
                        _visuals[i].SetActive(i == id);
                    }
                }

                if (_price == 0 && !_textPrice_0)
                {
                    _textPrice.text = _type == ButtonType.Selected ? _textSelected : _textSelect;
                }
                else
                {
                    _textPrice.text = _price.FormatWithSeparator(_customSeparator);
                }
            }

            private void OnValidate()
            {
                if (_editorView)
                    SetVisual(_price, _type);
                else
                    SetAutoVisual(_price, _type);
            }

        }
    }
}
