using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Shop
{
    /// <summary>
    ///     Bundle of <see cref="ShopItemData"/> sold for a single price. Purchasing the bundle marks every
    ///     contained item as owned (and the bundle itself when <see cref="isSinglePurchase"/> is true).
    /// </summary>
    [CreateAssetMenu(fileName = "Shop Bundle Data", menuName = "Neoxider/Shop/Shop Bundle Data", order = 33)]
    public class ShopBundleData : ScriptableObject
    {
        [Tooltip("Stable identifier used for save state. Auto-filled from name on validate.")]
        [SerializeField]
        private string _id = "";

        [Tooltip("Bundle display name shown in shop.")] [SerializeField]
        private string _nameBundle;

        [Tooltip("Bundle description.")] [TextArea(2, 4)] [SerializeField]
        private string _description;

        [Tooltip("Bundle preview/banner image.")] [SerializeField]
        private Sprite _sprite;

        [Tooltip("Bundle icon.")] [SerializeField]
        private Sprite _icon;

        [Tooltip("Price charged once for the whole bundle.")] [SerializeField] [Min(0)]
        private int _bundlePrice = 100;

        [Tooltip("If true, bundle can be bought only once.")] [SerializeField]
        private bool _isSinglePurchase = true;

        [Tooltip("Items granted to the player when the bundle is purchased.")] [SerializeField]
        private ShopItemData[] _items;

        [Tooltip("Optional Money.SaveKey used for this bundle currency. Empty = use Shop default currency.")]
        [SerializeField]
        private string _currencyOverrideSaveKey = "";

        /// <summary>Stable identifier; auto-filled from <see cref="nameBundle"/> on validate.</summary>
        public string Id => _id;

        /// <summary>Display name.</summary>
        public string nameBundle => _nameBundle;

        /// <summary>Description text.</summary>
        public string description => _description;

        /// <summary>Preview/banner image.</summary>
        public Sprite sprite => _sprite;

        /// <summary>Icon.</summary>
        public Sprite icon => _icon;

        /// <summary>Bundle price.</summary>
        public int price => _bundlePrice;

        /// <summary>Whether the bundle can be purchased only once.</summary>
        public bool isSinglePurchase => _isSinglePurchase;

        /// <summary>Items granted on purchase. May contain nulls if assigned and later removed.</summary>
        public IReadOnlyList<ShopItemData> Items => _items ?? Array.Empty<ShopItemData>();

        /// <summary>Optional per-bundle Money.SaveKey. Empty means use Shop's default.</summary>
        public string CurrencyOverrideSaveKey => _currencyOverrideSaveKey;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(_nameBundle))
            {
                _id = _nameBundle.Replace(" ", "_");
            }
        }
    }
}
