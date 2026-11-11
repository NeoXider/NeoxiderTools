using Neo.Save;
using Neo.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Shop</b> demo scene (Shop.unity). The storefront cards, the wallet label,
    ///     the control card, the log feed and the info card are saved in the scene by its DemoSceneKit builder; this
    ///     component builds nothing. It is the target of persistent UnityEvents: the TryBuy / sale / reset buttons call
    ///     its public methods, and the <see cref="Neo.Shop.Shop" /> events (OnPurchasedId, OnPurchaseFailedId, OnSelectId,
    ///     OnShopChanged) are wired to it in the Inspector. Wallet buttons call <see cref="Money" /> directly (NoCode).
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Shop Demo")]
    public sealed class ShopDemoController : MonoBehaviour
    {
        [Header("Module")]
        [SerializeField] private Neo.Shop.Shop _shop;
        [SerializeField] private Money _money;

        [Tooltip("Catalog item whose price the sale buttons override at runtime (SetRuntimePrice).")]
        [SerializeField] private ShopItemData _saleItem;

        [Tooltip("Runtime price multiplier applied by StartSale.")]
        [SerializeField] [Range(0.05f, 1f)] private float _saleFactor = 0.5f;

        [Tooltip("The wallet is topped up to this balance on start so the first purchase can succeed.")]
        [SerializeField] [Min(0f)] private float _startBalance = 200f;

        [Tooltip("SaveProvider key of the Shop profile (Shop > Save Key); cleared by ResetDemo.")]
        [SerializeField] private string _shopSaveKey = "Shop";

        [Header("Live values")]
        [SerializeField] private TMP_Text _statusValue;
        [SerializeField] private TMP_Text _equippedValue;
        [SerializeField] private TMP_Text _ownedValue;
        [SerializeField] private TMP_Text _salePriceValue;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned (the scene builder ran).</summary>
        public bool IsSceneBuilt => _shop != null && _money != null && _log != null && _statusValue != null &&
                                    _equippedValue != null && _ownedValue != null;

        public Neo.Shop.Shop TargetShop => _shop;
        public Money Wallet => _money;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            TopUpWallet();
            RefreshLabels();
            Log("Shop ready - click a price button or a TryBuy button");
        }

        /// <summary>Button target: runs the typed purchase API and shows the returned status.</summary>
        public void TryBuy(ShopItemData item)
        {
            if (_shop == null || item == null)
            {
                WarnUnbuilt();
                return;
            }

            ShopPurchaseStatus status = _shop.TryBuy(item);
            if (_statusValue != null)
            {
                _statusValue.text = Colored(status.ToString(), StatusColor(status));
            }

            Log("shop.TryBuy(" + item.nameItem + ") -> ShopPurchaseStatus." + status);
            RefreshLabels();
        }

        /// <summary>Button target: persists a discounted runtime price for the sale item.</summary>
        public void StartSale()
        {
            if (_shop == null || _saleItem == null)
            {
                WarnUnbuilt();
                return;
            }

            float price = Mathf.Max(1f, Mathf.Round(_saleItem.price * _saleFactor));
            _shop.SetRuntimePrice(_saleItem, price);
            Log("shop.SetRuntimePrice(" + _saleItem.nameItem + ", " + price.ToString("0") + ") - saved override");
            RefreshLabels();
        }

        /// <summary>Button target: removes the runtime price override of the sale item.</summary>
        public void EndSale()
        {
            if (_shop == null || _saleItem == null)
            {
                WarnUnbuilt();
                return;
            }

            _shop.ClearRuntimePrice(_saleItem);
            Log("shop.ClearRuntimePrice(" + _saleItem.nameItem + ") - back to ShopItemData.price");
            RefreshLabels();
        }

        /// <summary>
        ///     Button target: forgets every purchase and the balance (SaveProvider keys of Shop and Money), then
        ///     reloads the scene so <see cref="Neo.Shop.Shop" /> loads an empty profile.
        /// </summary>
        public void ResetDemo()
        {
            if (!string.IsNullOrEmpty(_shopSaveKey))
            {
                SaveProvider.DeleteKey(_shopSaveKey);
            }

            if (_money != null)
            {
                _money.ClearSavedMoneyAndReset();
            }

            SaveProvider.Save();
            Log("SaveProvider.DeleteKey(\"" + _shopSaveKey + "\") + money.ClearSavedMoneyAndReset()");
            ReloadScene();
        }

        /// <summary>Listener of <see cref="Neo.Shop.Shop.OnPurchasedId" />.</summary>
        public void OnItemPurchased(string itemId)
        {
            Log("Shop.OnPurchasedId(\"" + itemId + "\")");
            RefreshLabels();
        }

        /// <summary>Listener of <see cref="Neo.Shop.Shop.OnPurchaseFailedId" />.</summary>
        public void OnPurchaseFailed(string itemId)
        {
            Log("Shop.OnPurchaseFailedId(\"" + itemId + "\") - not enough coins");
        }

        /// <summary>Listener of <see cref="Neo.Shop.Shop.OnSelectId" />.</summary>
        public void OnItemSelected(string itemId)
        {
            Log("Shop.OnSelectId(\"" + itemId + "\") - equipped");
            RefreshLabels();
        }

        /// <summary>Listener of <see cref="Neo.Shop.Shop.OnShopChanged" />: mirrors ownership, equipment and the sale price.</summary>
        public void RefreshLabels()
        {
            if (_shop == null)
            {
                return;
            }

            if (_equippedValue != null)
            {
                _equippedValue.text = string.IsNullOrEmpty(_shop.EquippedId) ? "-" : _shop.EquippedId;
            }

            if (_ownedValue != null)
            {
                _ownedValue.text = CountOwned() + " / " + CountItems();
            }

            if (_salePriceValue != null && _saleItem != null)
            {
                float price = _shop.GetPrice(_saleItem);
                _salePriceValue.text = Mathf.Approximately(price, _saleItem.price)
                    ? price.ToString("0")
                    : Colored(price.ToString("0") + " (sale)", DemoPalette.Good);
            }
        }

        private void TopUpWallet()
        {
            if (_money == null || _startBalance <= 0f)
            {
                return;
            }

            // WHY: Money loads its saved balance in its own Start; reloading here first keeps a larger saved
            // balance instead of overwriting it with the top-up when this Start runs earlier.
            _money.ReloadBalanceFromSave();
            if (_money.money < _startBalance)
            {
                float added = _startBalance - _money.money;
                _money.Add(added);
                Log("money.Add(" + added.ToString("0") + ") - demo top-up to " + _startBalance.ToString("0"));
            }
        }

        private int CountOwned()
        {
            ShopItemData[] items = _shop.ShopItemDatas;
            int owned = 0;
            if (items == null)
            {
                return owned;
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && _shop.IsOwned(items[i]))
                {
                    owned++;
                }
            }

            return owned;
        }

        private int CountItems()
        {
            ShopItemData[] items = _shop.ShopItemDatas;
            return items != null ? items.Length : 0;
        }

        private void ReloadScene()
        {
            Scene scene = gameObject.scene;
            if (Application.CanStreamedLevelBeLoaded(scene.path))
            {
                SceneManager.LoadScene(scene.path);
                return;
            }

#if UNITY_EDITOR
            // WHY: sample scenes are usually not in the build list; the editor can still reload them in Play Mode.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Log("Restart the scene to start with an empty shop profile");
#endif
        }

        private static Color StatusColor(ShopPurchaseStatus status)
        {
            switch (status)
            {
                case ShopPurchaseStatus.Purchased:
                case ShopPurchaseStatus.Selected:
                    return DemoPalette.Good;
                case ShopPurchaseStatus.AlreadyOwned:
                    return DemoPalette.Cyan;
                case ShopPurchaseStatus.InsufficientFunds:
                case ShopPurchaseStatus.CommitFailed:
                    return DemoPalette.Danger;
                default:
                    return DemoPalette.Warm;
            }
        }

        private static string Colored(string text, Color color)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[ShopDemoController] The Shop demo scene is not authored. Run Neoxider/Demo/Rebuild Active Demo Scene.",
                this);
        }
    }
}
