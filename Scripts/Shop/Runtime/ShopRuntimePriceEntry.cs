using System;
using UnityEngine;

namespace Neo.Shop
{
    /// <summary>
    ///     Runtime price override for a single <see cref="ShopItemData"/> identified by <see cref="Id"/>.
    ///     Stored inside <see cref="ShopProfileData.PriceOverrides"/>; absent when the live price equals
    ///     <see cref="ShopItemData.price"/>.
    /// </summary>
    [Serializable]
    public struct ShopRuntimePriceEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private float _price;

        public ShopRuntimePriceEntry(string id, float price)
        {
            _id = id;
            _price = price;
        }

        /// <summary>Stable item identifier (<see cref="ShopItemData.Id"/>).</summary>
        public string Id
        {
            get => _id;
            set => _id = value;
        }

        /// <summary>Current runtime price.</summary>
        public float Price
        {
            get => _price;
            set => _price = value;
        }
    }
}
