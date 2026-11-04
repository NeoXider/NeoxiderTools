#if MIRROR
using System.Collections;
using System.Reflection;
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
            GameObject go = new GameObject("NetworkRateLimitTests", typeof(NetworkIdentity));
            try
            {
                RateLimitProbe probe = go.AddComponent<RateLimitProbe>();

                Assert.IsFalse(probe.Check(), "first command must pass");
                Assert.IsTrue(probe.Check(), "immediate second command must be limited");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PerConnectionBudget_DropsExpiredPeers_AndKeepsOpenWindowsLimited()
        {
            GameObject go = new GameObject("NetworkRateLimitPruneTests", typeof(NetworkIdentity));
            try
            {
                RateLimitProbe probe = go.AddComponent<RateLimitProbe>();
                probe.Limit = 0f;
                for (int i = 0; i < 200; i++)
                {
                    Assert.IsFalse(probe.CheckFor(new NetworkConnectionToClient(1000 + i)));
                }

                Assert.That(PerConnectionEntries(probe),
                    Is.LessThanOrEqualTo(NeoNetworkComponent.PerConnectionPruneThreshold),
                    "historical connection ids must not accumulate once their window has passed");

                probe.Limit = 60f;
                NetworkConnectionToClient peer = new NetworkConnectionToClient(5000);
                Assert.IsFalse(probe.CheckFor(peer));
                Assert.IsTrue(probe.CheckFor(peer), "a peer inside its window stays limited");
                for (int i = 0; i < 100; i++)
                {
                    probe.CheckFor(new NetworkConnectionToClient(6000 + i));
                }

                Assert.IsTrue(probe.CheckFor(peer), "pruning must never forget a peer whose window is still open");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static int PerConnectionEntries(NeoNetworkComponent component)
        {
            FieldInfo field = typeof(NeoNetworkComponent).GetField("_lastCmdTimePerConnection",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(component) is IDictionary entries ? entries.Count : 0;
        }
    }
}
#endif
