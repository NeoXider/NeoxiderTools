#if MIRROR
using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Network;
using Mirror;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023FixtureNetworkRateLimitRateLimitProbe : NeoNetworkComponent
    {
        public bool Check()
        {
            return RateLimitCheck();
        }
    }
}
#endif
