using System;
using Neo.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace UI
    {
        [NeoDoc("Shop/ButtonPrice.md")]
        [CreateFromMenu("Neoxider/UI/ButtonPrice", "Prefabs/UI/ButtonPrice.prefab")]
        [AddComponentMenu("Neoxider/" + "UI/" + nameof(ButtonPrice))]
        public class ButtonPrice : MonoBehaviour
        {
            public enum ButtonType
            {
                Buy,
                Select,
                Selected,

                /// <summary>Priced item the player cannot pay for right now (see ShopPurchaseButtonView).</summary>
                Unaffordable
            }

            [Header("References")] [SerializeField]
            private TMP_Text[] _textPrice;

            [SerializeField] private TMP_Text[] _textButton;
            [SerializeField] private Visual _visual;

            [Header("Settings")] [SerializeField] [Min(0)]
            private int _price;

            [SerializeField] private bool _textPrice_0;
            [SerializeField] private bool _textButtonAndPrice;
            [SerializeField] private ButtonType _type = ButtonType.Buy;
            [SerializeField] private string _textBuy = "Buy";
            [SerializeField] private string _textSelect = "Select";
            [SerializeField] private string _textSelected = "Selected";

            [Tooltip("Button label in the Unaffordable state (kept equal to Buy by default so old prefabs look unchanged).")]
            [SerializeField]
            private string _textUnaffordable = "Buy";
            [SerializeField] private string _customSeparator = ".";

            [SerializeField] private bool _editorView = true;

            [SerializeField] private UnityEvent OnBuy;
            [SerializeField] private UnityEvent OnSelect;
            [SerializeField] private UnityEvent OnSelected;
            [SerializeField] private UnityEvent OnUnaffordable;

            /// <summary>Current visual state.</summary>
            public ButtonType CurrentType => _type;

            private void OnValidate()
            {
                if (_editorView)
                {
                    SetVisual(_price, _type);
                }
                else
                {
                    SetAutoVisual(_price, _type);
                }
            }


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
                {
                    type = ButtonType.Select;
                }

                if (type == ButtonType.Unaffordable && price <= 0)
                {
                    type = ButtonType.Select; // WHY: free items are always affordable
                }

                if (price > 0 && type != ButtonType.Buy && type != ButtonType.Unaffordable)
                {
                    type = ButtonType.Buy;
                }

                return type;
            }

            public void SetVisual(ButtonType type)
            {
                SetVisualId((int)type);
            }

            public void TrySetVisualId(int id)
            {
                if (!Enum.IsDefined(typeof(ButtonType), id))
                {
                    return;
                }

                ButtonType type = CheckAutoType(_price, (ButtonType)id);

                SetVisual(_price, type);
            }


            public void SetVisualId(int id)
            {
                if (!Enum.IsDefined(typeof(ButtonType), id))
                {
                    return;
                }

                _type = (ButtonType)id;

                if (_visual != null)
                {
                    bool hasUnaffordableVisual = _visual.unaffordable != null && _visual.unaffordable.Length > 0;
                    bool showBuyVisual = _type == ButtonType.Buy ||
                                         (_type == ButtonType.Unaffordable && !hasUnaffordableVisual);
                    _visual.buy.SetActiveAll(showBuyVisual);
                    _visual.select.SetActiveAll(_type == ButtonType.Select);
                    _visual.selected.SetActiveAll(_type == ButtonType.Selected);
                    if (hasUnaffordableVisual)
                    {
                        _visual.unaffordable.SetActiveAll(_type == ButtonType.Unaffordable);
                    }
                }

                if (_price == 0 && !_textPrice_0)
                {
                    if (!_textButtonAndPrice)
                    {
                        SetPriceText("");
                    }
                }
                else
                {
                    SetPriceText(_price.FormatWithSeparator(_customSeparator));
                }

                if (_textButton != null)
                {
                    switch (_type)
                    {
                        case ButtonType.Buy:
                            if (!_textButtonAndPrice)
                            {
                                SetButtonText(_textBuy);
                            }

                            break;
                        case ButtonType.Select:
                            SetButtonText(_textSelect);
                            break;
                        case ButtonType.Selected:
                            SetButtonText(_textSelected);
                            break;
                        case ButtonType.Unaffordable:
                            if (!_textButtonAndPrice)
                            {
                                SetButtonText(_textUnaffordable);
                            }

                            break;
                        default:
                            SetButtonText("");
                            break;
                    }
                }

                if (id == 0)
                {
                    OnBuy?.Invoke();
                }
                else if (id == 1)
                {
                    OnSelect?.Invoke();
                }
                else if (id == 2)
                {
                    OnSelected?.Invoke();
                }
                else if (id == 3)
                {
                    OnUnaffordable?.Invoke();
                }
            }

            private void SetButtonText(string textBuy)
            {
                if (_textButton == null)
                {
                    return;
                }

                foreach (TMP_Text item in _textButton)
                {
                    if (item != null)
                    {
                        item.text = textBuy;
                    }
                }
            }

            private void SetPriceText(string textPrice)
            {
                if (_textPrice == null)
                {
                    return;
                }

                foreach (TMP_Text item in _textPrice)
                {
                    if (item != null)
                    {
                        item.text = textPrice;
                    }
                }
            }

            [Serializable]
            public class Visual
            {
                public GameObject[] buy;
                public GameObject[] select;
                public GameObject[] selected;

                [Tooltip("Optional. When empty, the Unaffordable state keeps showing the Buy visuals.")]
                public GameObject[] unaffordable;
            }
        }
    }
}
