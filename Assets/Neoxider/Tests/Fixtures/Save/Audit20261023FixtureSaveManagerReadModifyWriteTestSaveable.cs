using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Save;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023FixtureSaveManagerReadModifyWriteTestSaveable : MonoBehaviour, ISaveableComponent, ISaveIdentityProvider
    {
        [SaveField("value")] public int Value;

        public string SaveIdentity => "current-saveable";

        public void OnDataLoaded()
        {
        }
    }
}
