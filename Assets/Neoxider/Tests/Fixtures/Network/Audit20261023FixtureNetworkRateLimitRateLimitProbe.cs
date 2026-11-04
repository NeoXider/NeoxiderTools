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
        /// <summary>Command spacing used by both checks; the base default is 0.05 s.</summary>
        public float Limit = 0.05f;

        protected override float NetworkRateLimit => Limit;

        public bool Check()
        {
            return RateLimitCheck();
        }

        public bool CheckFor(NetworkConnectionToClient sender)
        {
            return RateLimitCheck(sender);
        }
    }
}
#endif
