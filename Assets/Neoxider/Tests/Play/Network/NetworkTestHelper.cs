#if MIRROR
using Mirror;
using Neo.Network;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Shared helper for network play-mode tests.
    ///     Mirror's assetId setter is internal, so tests must use field reflection.
    /// </summary>
    internal static class NetworkTestHelper
    {
        private static readonly System.Reflection.FieldInfo AssetIdField =
            typeof(NetworkIdentity).GetField("_assetId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        /// <summary>
        ///     Sets the assetId on a NetworkIdentity via private field reflection.
        ///     Mirror's assetId property has an internal setter inaccessible from Neo test assemblies.
        /// </summary>
        public static void SetAssetId(NetworkIdentity identity, uint id)
        {
            AssetIdField.SetValue(identity, id);
        }

        public static TestNetworkManager CreateTestNetworkManager(string name, out GameObject managerObject)
        {
            managerObject = new GameObject(name);
            managerObject.SetActive(false);

            Transport transport = managerObject.AddComponent<DummyTransport>();
            TestNetworkManager manager = managerObject.AddComponent<TestNetworkManager>();
            manager.transport = transport;
            Transport.active = transport;

            managerObject.SetActive(true);
            return manager;
        }

        public static DummyTransport UseDummyTransport(NetworkManager manager)
        {
            if (!manager.TryGetComponent(out DummyTransport transport))
            {
                transport = manager.gameObject.AddComponent<DummyTransport>();
            }

            manager.transport = transport;
            Transport.active = transport;
            return transport;
        }
    }
}
#endif
