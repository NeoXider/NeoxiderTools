using System;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public class Audit20261023NeoNetworkContractsTests
    {
        [Serializable]
        public sealed class AuthoritySettings
        {
            public NetworkAuthorityMode authorityMode;
        }

        [Serializable]
        public sealed class PersistedAuthoritySettings
        {
            public int authorityMode;
        }

        // These fixtures represent previously saved settings, independently of enum declaration order.
        [TestCase("{\"authorityMode\":0}", NetworkAuthorityMode.None)]
        [TestCase("{\"authorityMode\":1}", NetworkAuthorityMode.OwnerOnly)]
        [TestCase("{\"authorityMode\":2}", NetworkAuthorityMode.ServerOnly)]
        public void ExistingSettings_RestoreOriginalAuthoritySelection(string json, NetworkAuthorityMode expected)
        {
            AuthoritySettings settings = JsonUtility.FromJson<AuthoritySettings>(json);

            Assert.That(settings.authorityMode, Is.EqualTo(expected));
        }

        [TestCase(NetworkAuthorityMode.None, 0)]
        [TestCase(NetworkAuthorityMode.OwnerOnly, 1)]
        [TestCase(NetworkAuthorityMode.ServerOnly, 2)]
        public void SavingSettings_PreservesCompatibleAuthorityInteger(NetworkAuthorityMode mode, int expected)
        {
            AuthoritySettings settings = new AuthoritySettings { authorityMode = mode };
            string json = JsonUtility.ToJson(settings);
            PersistedAuthoritySettings persisted = JsonUtility.FromJson<PersistedAuthoritySettings>(json);

            Assert.That(persisted.authorityMode, Is.EqualTo(expected));
        }

        [Test]
        public void SettingsWithoutAuthorityField_SelectDefaultPolicy()
        {
            AuthoritySettings settings = JsonUtility.FromJson<AuthoritySettings>("{}");

            Assert.That(settings.authorityMode, Is.EqualTo(NetworkAuthorityMode.None));
        }
    }
}
