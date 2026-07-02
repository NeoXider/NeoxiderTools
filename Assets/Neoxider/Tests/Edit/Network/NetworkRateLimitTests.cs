#if MIRROR
using Neo.Network;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the command rate limit used by NoCode network components
    ///     (NetworkEventDispatcher, relays, property sync).
    /// </summary>
    [TestFixture]
    public class NetworkRateLimitTests
    {
        private sealed class RateLimitProbe : NeoNetworkComponent
        {
            public bool Check()
            {
                return RateLimitCheck();
            }
        }

        [Test]
        public void SecondImmediateCommand_IsRateLimited()
        {
            var go = new GameObject("NetworkRateLimitTests");
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
