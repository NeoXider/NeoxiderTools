using System;
using Neo.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;


namespace Neo
{
    namespace UI
    {
        [AddComponentMenu("Neoxider/" + "Shop/" + nameof(ButtonPrice))]
        public class ButtonPrice : MonoBehaviour
        {
            public enum ButtonType
            {
                Buy,
                Select,
                Selected
            }

            [Serializable]
            public class Visual
            {
                public GameObject[] buy;
                public GameObject[] select;
                public GameObject[] selected;
            }

            [SerializeField] private TMP_Text[] _textPrice;
            [SerializeField] private TMP_Text[] _textButton;
            [SerializeField] [Min(0)] private int _price = 0;
            [Space] [SerializeField] private bool _textPrice_0 = false;
            [SerializeField] private bool _textButtonAndPrice = false;
            [SerializeField] private ButtonType _type = ButtonType.Buy;
            [SerializeField] private Visual _visual;
            [SerializeField] private string _textBuy = "Buy";
            [SerializeField] private string _textSelect = "Select";
            [SerializeField] private string _textSelected = "Selected";
            [SerializeField] private string _customSeparator = ".";

            [SerializeField] private bool _editorView = true;

            [SerializeField] private UnityEvent OnBuy;
            [SerializeField] private UnityEvent OnSelect;
            [SerializeField] private UnityEvent OnSelected;


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

                if (price > 0 && ButtonType.Buy != type) type = ButtonType.Buy;

                return type;
            }

            public void SetVisual(ButtonType type)
            {
                SetVisualId((int)type);
            }

            public void TrySetVisualId(int id)
            {
                var type = CheckAutoType(_price, (ButtonType)id);

                SetVisual(_price, type);
            }


            public void SetVisualId(int id)
            {
                _type = (ButtonType)id;

                if (_visual != null)
                {
                    _visual.buy.SetActiveAll(_type == ButtonType.Buy);
                    _visual.select.SetActiveAll(_type == ButtonType.Select);
                    _visual.selected.SetActiveAll(_type == ButtonType.Selected);
                }

                if (_price == 0 && !_textPrice_0)
                {
                    if (!_textButtonAndPrice)
                        SetPriceText("");
                }
                else
                {
                    SetPriceText(_price.FormatWithSeparator(_customSeparator));
                }

                if (_textButton != null)
                    switch (_type)
                    {
                        case ButtonType.Buy:
                            if (!_textButtonAndPrice)
                                SetButtonText(_textBuy);
                            break;
                        case ButtonType.Select:
                            SetButtonText(_textSelect);
                            break;
                        case ButtonType.Selected:
                            SetButtonText(_textSelected);
                            break;
                        default:
                            SetButtonText("");
                            break;
                    }

                if (id == 0)
                    OnBuy?.Invoke();
                else if (id == 1)
                    OnSelect?.Invoke();
                else if (id == 2)
                    OnSelected?.Invoke();
            }

            private void SetButtonText(string textBuy)
            {
                foreach (var item in _textButton) item.text = textBuy;
            }

            private void SetPriceText(string textPrice)
            {
                foreach (var item in _textPrice)
                    if (item != null)
                        item.text = textPrice;
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