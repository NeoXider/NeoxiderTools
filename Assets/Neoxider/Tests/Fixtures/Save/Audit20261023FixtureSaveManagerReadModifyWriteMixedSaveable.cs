using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Save;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023FixtureSaveManagerReadModifyWriteMixedSaveable : MonoBehaviour, ISaveableComponent, ISaveIdentityProvider
    {
        [SaveField("auto")] public int AutoValue;

        [SaveField("manual", autoSaveOnQuit: false)]
        public int ManualValue;

        public string SaveIdentity => "mixed-saveable";

        public void OnDataLoaded()
        {
        }
    }
}
