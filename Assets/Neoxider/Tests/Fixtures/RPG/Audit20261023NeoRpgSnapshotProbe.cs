using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Runtime;

namespace Neo.Tests.Edit.RPG
{
    public sealed class Audit20261023NeoRpgSnapshotProbe : MonoBehaviour, IRpgCharacterNetworkAdapter
    {
        public readonly List<RpgCharacterProfileData> Snapshots = new();
        public Action<RpgCharacterProfileData> Received;
        private bool _capturing;
        public bool SuppressLocalSimulation => false;
        public bool TryRoute(RpgCharacterNetworkCommand command) => false;
        public void NotifyStateChanged()
        {
            if (_capturing) { throw new InvalidOperationException("Recursive initial snapshot"); }
            _capturing = true;
            try
            {
                RpgCharacterProfileData snapshot = GetComponent<RpgCharacter>().CaptureProfile();
                Snapshots.Add(snapshot);
                Received?.Invoke(snapshot);
            }
            finally { _capturing = false; }
        }
    }
}
