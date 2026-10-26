#if MIRROR
using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Network;
using Mirror;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023FixtureNetworkContextActionIngressCooldownRelay : NetworkContextActionRelay
    {
        protected override float NetworkRateLimit => 1f;
    }
}
#endif
