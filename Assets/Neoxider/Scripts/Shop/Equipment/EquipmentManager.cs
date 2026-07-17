using System;
using System.Collections.Generic;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Neo.Shop
{
    /// <summary>
    ///     Multi-category equipment (dress-up / skins): one equipped item per category, applied to a
    ///     visual slot (<see cref="SpriteRenderer"/> or uGUI <see cref="Image"/>) and persisted via
    ///     <see cref="SaveProvider"/>.
    ///     <para>
    ///         Complements <see cref="Shop"/>'s single <c>EquippedId</c> flow: use the Shop for
    ///         ownership/purchases and this manager for wearing several items at once
    ///         (hair + dress + shoes + accessory). Wire a shop purchase/equip event to
    ///         <see cref="EquipById(string)"/> for a buy-then-wear flow.
    ///     </para>
    /// </summary>
    [NeoDoc("Shop/Equipment/EquipmentManager.md")]
    [CreateFromMenu("Neoxider/Shop/EquipmentManager")]
    [AddComponentMenu("Neoxider/Shop/" + nameof(EquipmentManager))]
    public sealed class EquipmentManager : MonoBehaviour
    {
        [Serializable]
        public sealed class CategorySlot
        {
            [Tooltip("Category id matching EquipItemDefinition.CategoryId.")]
            [FormerlySerializedAs("categoryId")]
            public string CategoryId = "";

            [Tooltip("World-space visual for the equipped sprite (character layer).")]
            [FormerlySerializedAs("spriteTarget")]
            public SpriteRenderer SpriteTarget;

            [Tooltip("uGUI visual for the equipped sprite (UI character).")]
            [FormerlySerializedAs("imageTarget")]
            public Image ImageTarget;

            [Tooltip("Call SetNativeSize() on the Image after assigning the sprite.")]
            [FormerlySerializedAs("setNativeSize")]
            public bool ApplyNativeSize = true;

            [Tooltip("Item id equipped when nothing is saved. Empty = category starts empty.")]
            [FormerlySerializedAs("defaultItemId")]
            public string DefaultItemId = "";
        }

        [Header("Catalog")]
        [Tooltip("All equippable items. Ids must be unique.")]
        [SerializeField]
        private EquipItemDefinition[] _items = Array.Empty<EquipItemDefinition>();

        [Header("Slots")]
        [SerializeField]
        private CategorySlot[] _slots = Array.Empty<CategorySlot>();

        [Header("Persistence")]
        [Tooltip("When on, equipped ids load on Start and persist on every change via SaveProvider.")]
        [SerializeField]
        private bool _persist = true;

        [Tooltip("SaveProvider key prefix; the category id is appended.")]
        [SerializeField]
        private string _saveKeyPrefix = "Equip_";

        [Header("Events")]
        [Tooltip("categoryId, itemId (empty itemId = category was unequipped).")]
        public UnityEvent<string, string> OnEquipChanged = new();

        private readonly Dictionary<string, string> _equipped = new(StringComparer.Ordinal);

        /// <summary>Currently equipped item id for the category; empty when nothing is equipped.</summary>
        public string GetEquippedId(string categoryId)
        {
            return _equipped.TryGetValue(categoryId ?? "", out string itemId) ? itemId : "";
        }

        /// <summary>Whether the item is currently equipped in its category.</summary>
        public bool IsEquipped(string itemId)
        {
            EquipItemDefinition item = FindItem(itemId);
            return item != null && string.Equals(GetEquippedId(item.CategoryId), item.Id, StringComparison.Ordinal);
        }

        private void Start()
        {
            RestoreOrDefaults();
        }

        /// <summary>
        ///     Equip an item by id (NoCode entry point — wire shop purchase/cell clicks here).
        ///     Unknown ids are ignored with a gated warning.
        /// </summary>
        public void EquipById(string itemId)
        {
            EquipItemDefinition item = FindItem(itemId);
            if (item == null)
            {
                NeoDiagnostics.LogWarning($"[EquipmentManager] Unknown equip item id '{itemId}'.", this);
                return;
            }

            Equip(item);
        }

        /// <summary>
        ///     Equip a catalog item. A configured <see cref="CategorySlot"/> for the category is
        ///     optional: when present its sprite target is updated, but the equipped state is always
        ///     tracked, persisted, and broadcast — so equipment works for pure state/save/query use
        ///     (e.g. driven by <c>ShopVariantsPanel</c>) without wiring a visual target.
        /// </summary>
        public void Equip(EquipItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            CategorySlot slot = FindSlot(item.CategoryId);
            if (slot != null)
            {
                ApplySprite(slot, item.Sprite);
            }

            _equipped[item.CategoryId] = item.Id;
            PersistCategory(item.CategoryId, item.Id);
            OnEquipChanged?.Invoke(item.CategoryId, item.Id);
        }

        /// <summary>
        ///     Clear a category: hides the visual when a <see cref="CategorySlot"/> is configured and
        ///     always clears the tracked/persisted equipped state. No-op when the category was already
        ///     empty (so it does not spam <see cref="OnEquipChanged"/>).
        /// </summary>
        public void Unequip(string categoryId)
        {
            categoryId ??= "";

            CategorySlot slot = FindSlot(categoryId);
            if (slot != null)
            {
                ApplySprite(slot, null);
            }
            else if (string.IsNullOrEmpty(GetEquippedId(categoryId)))
            {
                return; // WHY: no slot and nothing tracked — nothing to do
            }

            _equipped[categoryId] = "";
            PersistCategory(categoryId, "");
            OnEquipChanged?.Invoke(categoryId, "");
        }

        /// <summary>Toggle: equips the item, or unequips its category when it is already worn.</summary>
        public void ToggleById(string itemId)
        {
            EquipItemDefinition item = FindItem(itemId);
            if (item == null)
            {
                return;
            }

            if (IsEquipped(itemId))
            {
                Unequip(item.CategoryId);
            }
            else
            {
                Equip(item);
            }
        }

        private void RestoreOrDefaults()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                CategorySlot slot = _slots[i];
                if (slot == null || string.IsNullOrEmpty(slot.CategoryId))
                {
                    continue;
                }

                string savedId = _persist
                    ? SaveProvider.GetString(_saveKeyPrefix + slot.CategoryId, slot.DefaultItemId)
                    : slot.DefaultItemId;

                if (string.IsNullOrEmpty(savedId))
                {
                    ApplySprite(slot, null);
                    _equipped[slot.CategoryId] = "";
                    continue;
                }

                EquipItemDefinition item = FindItem(savedId);
                if (item != null)
                {
                    ApplySprite(slot, item.Sprite);
                    _equipped[slot.CategoryId] = item.Id;
                }
                else
                {
                    ApplySprite(slot, null);
                    _equipped[slot.CategoryId] = "";
                }
            }

            RestoreSlotlessCategories();
        }

        // WHY: Categories that have no visual CategorySlot can still be equipped (state-only). Restore
        // their persisted equipped id from the item catalog so saved state survives a reload without a slot.
        private void RestoreSlotlessCategories()
        {
            if (!_persist || _items == null)
            {
                return;
            }

            for (int i = 0; i < _items.Length; i++)
            {
                EquipItemDefinition candidate = _items[i];
                if (candidate == null || string.IsNullOrEmpty(candidate.CategoryId) ||
                    _equipped.ContainsKey(candidate.CategoryId) || FindSlot(candidate.CategoryId) != null)
                {
                    continue;
                }

                string savedId = SaveProvider.GetString(_saveKeyPrefix + candidate.CategoryId, "");
                if (!string.IsNullOrEmpty(savedId) && FindItem(savedId) != null)
                {
                    _equipped[candidate.CategoryId] = savedId;
                }
            }
        }

        private void PersistCategory(string categoryId, string itemId)
        {
            if (_persist)
            {
                SaveProvider.SetString(_saveKeyPrefix + categoryId, itemId ?? "");
            }
        }

        private static void ApplySprite(CategorySlot slot, Sprite sprite)
        {
            if (slot.SpriteTarget != null)
            {
                slot.SpriteTarget.sprite = sprite;
                slot.SpriteTarget.enabled = sprite != null;
            }

            if (slot.ImageTarget != null)
            {
                slot.ImageTarget.sprite = sprite;
                slot.ImageTarget.enabled = sprite != null;
                if (sprite != null && slot.ApplyNativeSize)
                {
                    slot.ImageTarget.SetNativeSize();
                }
            }
        }

        private EquipItemDefinition FindItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            for (int i = 0; i < _items.Length; i++)
            {
                EquipItemDefinition item = _items[i];
                if (item != null && string.Equals(item.Id, itemId, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private CategorySlot FindSlot(string categoryId)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                CategorySlot slot = _slots[i];
                if (slot != null && string.Equals(slot.CategoryId, categoryId, StringComparison.Ordinal))
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
