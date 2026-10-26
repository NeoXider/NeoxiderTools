using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Save;

namespace Neo.Save.Tests
{
    public sealed class Audit20261023FixtureSaveIdentityUtilityCustomIdentitySaveable : MonoBehaviour, ISaveableComponent, ISaveIdentityProvider
    {
        public void OnDataLoaded()
        {
        }

        public string SaveIdentity => "custom-save-id";
    }
}
