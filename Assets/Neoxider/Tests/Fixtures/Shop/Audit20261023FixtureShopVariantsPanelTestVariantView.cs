using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Shop;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023FixtureShopVariantsPanelTestVariantView : MonoBehaviour, IShopVariantView
    {
        public readonly List<ShopVariantState> States = new();
        public ShopVariantState Current => States.Count > 0 ? States[^1] : ShopVariantState.Unowned;

        public void ApplyVariantState(ShopVariantState state, ShopItemData data)
        {
            States.Add(state);
        }
    }
}
