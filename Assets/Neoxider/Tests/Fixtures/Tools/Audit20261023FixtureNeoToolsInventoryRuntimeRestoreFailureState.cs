using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Tools;

namespace Neo.Tests.Edit
{
    public sealed class Audit20261023FixtureNeoToolsInventoryRuntimeRestoreFailureState : InventoryItemStateBehaviour
    {
        public override string CaptureInventoryState() => "{}";

        public override void RestoreInventoryState(string json)
        {
            throw new InvalidOperationException("restore failed");
        }
    }
}
