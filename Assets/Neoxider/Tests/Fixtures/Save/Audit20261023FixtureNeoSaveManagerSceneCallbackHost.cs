using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Save;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023FixtureNeoSaveManagerSceneCallbackHost : SaveManager
    {
        protected override bool SetInstanceOnAwakeEnabled => false;
        protected override bool DontDestroyOnLoadEnabled => false;
    }
}
