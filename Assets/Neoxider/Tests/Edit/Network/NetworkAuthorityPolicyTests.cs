using Neo.Network;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Decision table of the transport-free <see cref="NetworkAuthorityPolicy" />.
    /// </summary>
    [TestFixture]
    public class NetworkAuthorityPolicyTests
    {
        // WHY: each case is one decision-table row: mode, directServer, localHost, owner, expected.
        [TestCase(NetworkAuthorityMode.None, false, false, false, true)]
        [TestCase(NetworkAuthorityMode.None, true, false, false, true)]
        [TestCase(NetworkAuthorityMode.OwnerOnly, false, false, false, false)]
        [TestCase(NetworkAuthorityMode.OwnerOnly, false, false, true, true)]
        [TestCase(NetworkAuthorityMode.OwnerOnly, false, true, false, true)]
        [TestCase(NetworkAuthorityMode.OwnerOnly, true, false, false, true)]
        [TestCase(NetworkAuthorityMode.ServerOnly, false, false, false, false)]
        [TestCase(NetworkAuthorityMode.ServerOnly, false, false, true, false)]
        [TestCase(NetworkAuthorityMode.ServerOnly, false, true, false, true)]
        [TestCase(NetworkAuthorityMode.ServerOnly, true, false, false, true)]
        public void IsAuthorized_NamedPolicies_FollowTheDecisionTable(NetworkAuthorityMode mode, bool directServer,
            bool localHost, bool owner, bool expected)
        {
            Assert.That(NetworkAuthorityPolicy.IsKnown(mode), Is.True);
            Assert.That(NetworkAuthorityPolicy.IsAuthorized(mode, directServer, localHost, owner), Is.EqualTo(expected));
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(int.MaxValue)]
        public void IsAuthorized_UnnamedValue_IsRejectedForEveryCaller(int raw)
        {
            NetworkAuthorityMode mode = (NetworkAuthorityMode)raw;

            Assert.That(NetworkAuthorityPolicy.IsKnown(mode), Is.False);
            Assert.That(NetworkAuthorityPolicy.IsAuthorized(mode, true, true, true), Is.False,
                "An unknown policy must not fall back to an allowance, not even for the server.");
            Assert.That(NetworkAuthorityPolicy.IsAuthorized(mode, false, false, false), Is.False);
        }
    }
}
