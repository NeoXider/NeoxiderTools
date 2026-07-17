using Neo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Shop
{
    /// <summary>
    ///     Reactive purchase-button state for one <see cref="ShopItem"/> slot. While enabled it
    ///     subscribes to the balance of the same currency the <see cref="Shop"/> purchase would spend
    ///     from (per-item Currency Override Save Key included, via
    ///     <see cref="Shop.ResolveCurrencyMoney"/>) and to shop refreshes, and immediately drives:
    ///     <list type="bullet">
    ///         <item><see cref="ButtonPrice"/> state — Buy / Select / Selected / Unaffordable;</item>
    ///         <item>the buy <see cref="Button"/>'s <c>interactable</c> flag for unaffordable items.</item>
    ///     </list>
    ///     Affordability itself comes from <see cref="Shop.CanAfford(string)"/> — no currency logic is
    ///     duplicated here. Rebinding the slot to another item (list refresh, category switch) is picked
    ///     up on the next shop refresh, including a currency re-subscription.
    /// </summary>
    [NeoDoc("Shop/ShopPurchaseButtonView.md")]
    [CreateFromMenu("Neoxider/Shop/ShopPurchaseButtonView")]
    [AddComponentMenu("Neoxider/Shop/" + nameof(ShopPurchaseButtonView))]
    public sealed class ShopPurchaseButtonView : MonoBehaviour
    {
        [Tooltip("Shop queried for price/ownership/affordability; auto-resolved from parents or the scene.")]
        [SerializeField]
        private Shop _shop;

        [Tooltip("Item slot this view reacts to; auto-resolved from this object or parents.")] [SerializeField]
        private ShopItem _item;

        [Tooltip("Price button visual driven by the state; auto-resolved from the item or children.")]
        [SerializeField]
        private ButtonPrice _buttonPrice;

        [Tooltip("Buy button whose interactable flag mirrors affordability; defaults to ShopItem.buttonBuy.")]
        [SerializeField]
        private Button _button;

        [Tooltip("Disable the buy button while the item is unaffordable.")] [SerializeField]
        private bool _disableButtonWhenUnaffordable = true;

        private Money _subscribedMoney;
        private string _subscribedItemId = "";

        /// <summary>Last state pushed to the visuals.</summary>
        public ButtonPrice.ButtonType CurrentState { get; private set; } = ButtonPrice.ButtonType.Buy;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_shop != null)
            {
                _shop.OnShopChanged.AddListener(Refresh);
                _shop.OnSelectId.AddListener(OnShopSelectionChanged);
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_shop != null)
            {
                _shop.OnShopChanged.RemoveListener(Refresh);
                _shop.OnSelectId.RemoveListener(OnShopSelectionChanged);
            }

            UnsubscribeBalance();
        }

        /// <summary>Re-evaluates ownership, selection, and affordability and applies the visuals.</summary>
        public void Refresh()
        {
            if (_shop == null || _item == null)
            {
                return;
            }

            string itemId = _item.BoundItemId;
            if (string.IsNullOrEmpty(itemId) || _item.BoundItemData == null)
            {
                UnsubscribeBalance();
                return; // bundle slots and empty slots are not this view's concern
            }

            SubscribeBalance(itemId);

            bool owned = _shop.IsOwned(itemId);
            bool equipped = string.Equals(_shop.EquippedId, itemId, System.StringComparison.Ordinal);
            int price = owned ? 0 : Mathf.RoundToInt(_shop.GetPrice(itemId));
            bool unaffordable = !owned && price > 0 && !_shop.CanAfford(itemId);

            ButtonPrice.ButtonType state;
            if (equipped)
            {
                state = ButtonPrice.ButtonType.Selected;
            }
            else if (owned || price <= 0)
            {
                state = ButtonPrice.ButtonType.Select;
            }
            else if (unaffordable)
            {
                state = ButtonPrice.ButtonType.Unaffordable;
            }
            else
            {
                state = ButtonPrice.ButtonType.Buy;
            }

            CurrentState = state;

            if (_buttonPrice != null)
            {
                _buttonPrice.SetVisual(price, state);
            }

            if (_button != null && _disableButtonWhenUnaffordable)
            {
                _button.interactable = !unaffordable;
            }
        }

        private void OnShopSelectionChanged(string _)
        {
            Refresh();
        }

        private void OnBalanceChanged(float _)
        {
            Refresh();
        }

        private void SubscribeBalance(string itemId)
        {
            if (_subscribedMoney != null && string.Equals(_subscribedItemId, itemId, System.StringComparison.Ordinal))
            {
                return;
            }

            UnsubscribeBalance();

            Money money = _shop.ResolveCurrencyMoney(itemId);
            if (money == null)
            {
                return; // custom IMoneySpend wallet: no balance stream, shop stays optimistic
            }

            money.CurrentMoney.AddListener(OnBalanceChanged);
            _subscribedMoney = money;
            _subscribedItemId = itemId;
        }

        private void UnsubscribeBalance()
        {
            if (_subscribedMoney != null)
            {
                _subscribedMoney.CurrentMoney.RemoveListener(OnBalanceChanged);
                _subscribedMoney = null;
            }

            _subscribedItemId = "";
        }

        private void ResolveReferences()
        {
            if (_item == null)
            {
                _item = GetComponentInParent<ShopItem>();
            }

            if (_shop == null)
            {
                _shop = GetComponentInParent<Shop>();
                if (_shop == null)
                {
                    _shop = FindFirstObjectByType<Shop>();
                }
            }

            if (_buttonPrice == null)
            {
                _buttonPrice = _item != null
                    ? _item.GetComponentInChildren<ButtonPrice>(true)
                    : GetComponentInChildren<ButtonPrice>(true);
            }

            if (_button == null && _item != null)
            {
                _button = _item.buttonBuy;
            }

            if (_button == null)
            {
                _button = GetComponentInChildren<Button>(true);
            }
        }
    }
}
