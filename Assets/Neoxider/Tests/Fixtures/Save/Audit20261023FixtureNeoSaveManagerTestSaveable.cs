using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Save;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023FixtureNeoSaveManagerTestSaveable : MonoBehaviour, ISaveableComponent, ISaveIdentityProvider
    {
        [SaveField("value")] public int Value;
        [SaveField("other")] public int OtherValue;
        public string Identity;
        public int LoadedCount;
        public string SaveIdentity => Identity;

        public void OnDataLoaded()
        {
            LoadedCount++;
        }
    }
}
