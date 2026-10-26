using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Tools;

namespace Neo.Tools.Tests
{
    public sealed class Audit20261023FixtureInventorySystemTestWalletState : InventoryItemStateBehaviour
    {
        [Serializable]
        private sealed class WalletState
        {
            public int coins;
        }

        public int Coins { get; set; }

        public override string CaptureInventoryState()
        {
            return JsonUtility.ToJson(new WalletState
            {
                coins = Coins
            });
        }

        public override void RestoreInventoryState(string json)
        {
            WalletState state = JsonUtility.FromJson<WalletState>(json);
            Coins = state != null ? state.coins : 0;
        }
    }
}
