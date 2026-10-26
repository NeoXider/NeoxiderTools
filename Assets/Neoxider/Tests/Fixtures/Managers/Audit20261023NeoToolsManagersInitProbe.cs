using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Tools;

namespace Neo.Tools.Tests
{
    public sealed class Audit20261023NeoToolsManagersInitProbe : MonoBehaviour, IInit
    {
        public Action OnInit { get; set; }
        public int InitPriority => 1;

        public void Init()
        {
            OnInit?.Invoke();
        }
    }
}
