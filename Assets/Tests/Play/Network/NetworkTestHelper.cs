#if MIRROR
using Mirror;
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
    }
}
#endif
