using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Save;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023FixtureNeoSaveManagerDerivedSaveable : Audit20261023FixtureNeoSaveManagerBaseSaveable
    {
        [SaveField("derived")] public int DerivedValue;
    }
}
