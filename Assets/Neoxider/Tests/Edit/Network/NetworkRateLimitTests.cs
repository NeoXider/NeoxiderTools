#if MIRROR
using Mirror;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;
using RateLimitProbe = global::Neo.Editor.Tests.Audit20261023FixtureNetworkRateLimitRateLimitProbe;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the command rate limit used by NoCode network components
    ///     (NetworkEventDispatcher, relays, property sync).
    /// </summary>
    [TestFixture]
    public class NetworkRateLimitTests
    {


        [Test]
        public void SecondImmediateCommand_IsRateLimited()
        {
            // WHY: NetworkBehaviour.OnValidate logs an error when the host object has no NetworkIdentity.
            var go = new GameObject("NetworkRateLimitTests", typeof(NetworkIdentity));
            try
            {
                var probe = go.AddComponent<RateLimitProbe>();

                Assert.IsFalse(probe.Check(), "first command must pass");
                Assert.IsTrue(probe.Check(), "immediate second command must be limited");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
