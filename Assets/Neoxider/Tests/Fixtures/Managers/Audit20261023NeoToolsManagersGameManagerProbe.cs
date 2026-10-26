using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Tools;

namespace Neo.Tools.Tests
{
    public sealed class Audit20261023NeoToolsManagersGameManagerProbe : GM
    {
        public bool SkipResume { get; set; }
        public bool SkipStop { get; set; }

        public override void Resume()
        {
            if (!SkipResume)
            {
                base.Resume();
            }
        }

        public override void StopGame()
        {
            if (!SkipStop)
            {
                base.StopGame();
            }
        }

        public void ReleaseForTest()
        {
            base.OnDestroy();
        }
    }
}
