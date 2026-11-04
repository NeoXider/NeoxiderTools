using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Inspector-processing probe with no injection attributes and no editor hooks, standing in for the plain
    ///     third-party MonoBehaviours every inspector also draws.
    /// </summary>
    public sealed class EditorLibraryPassPlainComponent : MonoBehaviour
    {
        public int Plain;
        public float Ratio;
        public Vector3 Position;
        public string Label = string.Empty;
        public BoxCollider Unattributed;
    }
}
