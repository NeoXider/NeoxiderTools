using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Tools;

namespace Neo.Tools.Tests
{
    public sealed class Audit20261023NeoToolsManagersEventHubProbe : EM
    {
        public void AttachForTest()
        {
            base.Awake();
        }

        public void ReleaseForTest()
        {
            base.OnDestroy();
        }
    }
}
