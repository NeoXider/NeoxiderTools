using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Shop
{
    /// <summary>
    ///     Serializable Shop profile persisted as a single JSON blob under <see cref="Shop"/>'s save key.
    ///     Replaces the legacy index-based keys (<c>Shop0..ShopN</c> / <c>ShopEquipped</c>) — stable across
    ///     reorders of <see cref="ShopItemData"/> assets.
    /// </summary>
    [Serializable]
    public sealed class ShopProfileData
    {
        [SerializeField] private int _version = 1;
        [SerializeField] private List<string> _ownedItemIds = new();
        [SerializeField] private List<string> _ownedBundleIds = new();
        [SerializeField] private List<ShopRuntimePriceEntry> _priceOverrides = new();
        [SerializeField] private string _equippedId = "";

        /// <summary>Profile schema version. Bumped when the structure changes incompatibly.</summary>
        public int Version
        {
            get => _version;
            set => _version = value;
        }

        /// <summary>Stable item IDs the player owns (single-purchase items live here).</summary>
        public List<string> OwnedItemIds => _ownedItemIds;

        /// <summary>Stable bundle IDs the player has purchased.</summary>
        public List<string> OwnedBundleIds => _ownedBundleIds;

        /// <summary>
        ///     Runtime price overrides keyed by item ID. Items with no entry use
        ///     <see cref="ShopItemData.price"/> directly.
        /// </summary>
        public List<ShopRuntimePriceEntry> PriceOverrides => _priceOverrides;

        /// <summary>Currently equipped item ID. Empty when nothing is equipped.</summary>
        public string EquippedId
        {
            get => _equippedId;
            set => _equippedId = value ?? string.Empty;
        }

        /// <summary>Drops empty/duplicate IDs and clamps the version to a sane minimum.</summary>
        public void Sanitize()
        {
            if (_version < 1)
            {
                _version = 1;
            }

            Dedupe(_ownedItemIds);
            Dedupe(_ownedBundleIds);
            DedupePriceOverrides(_priceOverrides);
            if (_equippedId == null)
            {
                _equippedId = string.Empty;
            }
        }

        /// <summary>Deep copy via JSON round-trip — keeps lists independent.</summary>
        public ShopProfileData Clone()
        {
            string json = JsonUtility.ToJson(this);
            return JsonUtility.FromJson<ShopProfileData>(json);
        }

        /// <summary>True when the item ID is currently in <see cref="OwnedItemIds"/>.</summary>
        public bool IsItemOwned(string id)
        {
            return !string.IsNullOrEmpty(id) && _ownedItemIds.Contains(id);
        }

        /// <summary>True when the bundle ID is currently in <see cref="OwnedBundleIds"/>.</summary>
        public bool IsBundleOwned(string id)
        {
            return !string.IsNullOrEmpty(id) && _ownedBundleIds.Contains(id);
        }

        /// <summary>Adds the item ID to <see cref="OwnedItemIds"/> if missing. Returns true when added.</summary>
        public bool TryAddOwnedItem(string id)
        {
            if (string.IsNullOrEmpty(id) || _ownedItemIds.Contains(id))
            {
                return false;
            }

            _ownedItemIds.Add(id);
            return true;
        }

        /// <summary>Adds the bundle ID to <see cref="OwnedBundleIds"/> if missing. Returns true when added.</summary>
        public bool TryAddOwnedBundle(string id)
        {
            if (string.IsNullOrEmpty(id) || _ownedBundleIds.Contains(id))
            {
                return false;
            }

            _ownedBundleIds.Add(id);
            return true;
        }

        /// <summary>
        ///     Returns the runtime price for <paramref name="id"/>. Falls back to
        ///     <paramref name="defaultPrice"/> when no override is stored.
        /// </summary>
        public float GetPriceOrDefault(string id, float defaultPrice)
        {
            if (string.IsNullOrEmpty(id))
            {
                return defaultPrice;
            }

            for (int i = 0; i < _priceOverrides.Count; i++)
            {
                if (_priceOverrides[i].Id == id)
                {
                    return _priceOverrides[i].Price;
                }
            }

            return defaultPrice;
        }

        /// <summary>Sets or updates the runtime price override for <paramref name="id"/>.</summary>
        public void SetPriceOverride(string id, float price)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            for (int i = 0; i < _priceOverrides.Count; i++)
            {
                if (_priceOverrides[i].Id == id)
                {
                    ShopRuntimePriceEntry entry = _priceOverrides[i];
                    entry.Price = price;
                    _priceOverrides[i] = entry;
                    return;
                }
            }

            _priceOverrides.Add(new ShopRuntimePriceEntry(id, price));
        }

        /// <summary>Removes the runtime price override for <paramref name="id"/> if present.</summary>
        public bool ClearPriceOverride(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            for (int i = _priceOverrides.Count - 1; i >= 0; i--)
            {
                if (_priceOverrides[i].Id == id)
                {
                    _priceOverrides.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private static void Dedupe(List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return;
            }

            HashSet<string> seen = new();
            for (int i = ids.Count - 1; i >= 0; i--)
            {
                string id = ids[i];
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                {
                    ids.RemoveAt(i);
                }
            }
        }

        private static void DedupePriceOverrides(List<ShopRuntimePriceEntry> overrides)
        {
            if (overrides == null || overrides.Count == 0)
            {
                return;
            }

            HashSet<string> seen = new();
            for (int i = overrides.Count - 1; i >= 0; i--)
            {
                string id = overrides[i].Id;
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                {
                    overrides.RemoveAt(i);
                }
            }
        }
    }
}
