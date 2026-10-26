using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Save;

namespace Neo.Editor.Tests.Edit
{
    public abstract class Audit20261023FixtureNeoSaveManagerBaseSaveable : MonoBehaviour, ISaveableComponent, ISaveIdentityProvider
    {
        [SaveField("base")] private int _baseValue;
        public int BaseValue { get => _baseValue; set => _baseValue = value; }
        public string SaveIdentity => "inherited";

        public void OnDataLoaded()
        {
        }
    }
}
