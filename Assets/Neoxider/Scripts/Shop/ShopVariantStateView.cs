using Neo.Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Shop
{
    /// <summary>
    ///     Ready-made <see cref="IShopVariantView"/>: toggles authored GameObject groups per
    ///     <see cref="ShopVariantState"/> (lock icons, check marks, dim overlays, frames) and raises a
    ///     UnityEvent per state for custom prefab reactions. Put it on (or under) a
    ///     <see cref="ShopItem"/> rendered by a <see cref="ShopVariantsPanel"/>; visuals stay fully
    ///     prefab-driven — the component never resizes or repositions anything.
    /// </summary>
    [NeoDoc("Shop/ShopVariantsPanel.md")]
    [CreateFromMenu("Neoxider/Shop/ShopVariantStateView")]
    [AddComponentMenu("Neoxider/Shop/" + nameof(ShopVariantStateView))]
    public sealed class ShopVariantStateView : MonoBehaviour, IShopVariantView
    {
        [Tooltip("Active only while the variant is not owned (e.g. lock icon, price badge, dim overlay).")]
        [SerializeField]
        private GameObject[] _unownedVisuals;

        [Tooltip("Active only while the variant is owned but not equipped.")] [SerializeField]
        private GameObject[] _ownedVisuals;

        [Tooltip("Active only while the variant is equipped (e.g. check mark, frame).")] [SerializeField]
        private GameObject[] _equippedVisuals;

        [Space] public UnityEvent<ShopItemData> OnUnowned = new();
        public UnityEvent<ShopItemData> OnOwned = new();
        public UnityEvent<ShopItemData> OnEquipped = new();

        /// <summary>Last state applied by the owning panel.</summary>
        public ShopVariantState CurrentState { get; private set; } = ShopVariantState.Unowned;

        public void ApplyVariantState(ShopVariantState state, ShopItemData data)
        {
            CurrentState = state;

            _unownedVisuals.SetActiveAll(state == ShopVariantState.Unowned);
            _ownedVisuals.SetActiveAll(state == ShopVariantState.Owned);
            _equippedVisuals.SetActiveAll(state == ShopVariantState.Equipped);

            switch (state)
            {
                case ShopVariantState.Unowned:
                    OnUnowned?.Invoke(data);
                    break;
                case ShopVariantState.Owned:
                    OnOwned?.Invoke(data);
                    break;
                case ShopVariantState.Equipped:
                    OnEquipped?.Invoke(data);
                    break;
            }
        }
    }
}
