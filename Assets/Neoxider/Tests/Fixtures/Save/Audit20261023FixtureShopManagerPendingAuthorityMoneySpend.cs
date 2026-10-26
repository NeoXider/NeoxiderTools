using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Save;
using Neo.Shop;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023FixtureShopManagerPendingAuthorityMoneySpend : MonoBehaviour, IMoneySpendAuthority
    {
        public int TrySpendCalls { get; private set; }

        public bool Spend(float count)
        {
            return TrySpend(count).IsConfirmed;
        }

        public MoneySpendResult TrySpend(float count)
        {
            TrySpendCalls++;
            return new MoneySpendResult(MoneySpendStatus.RequestedServerAuthority, count, 100f, 100f);
        }

        public bool CanConfirmSpendNow(float count)
        {
            return false;
        }
    }
}
