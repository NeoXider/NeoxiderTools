using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>Asset type that no test ever instantiates, so a Resources lookup for it can never succeed.</summary>
    public sealed class EditorLibraryPassUnloadedAsset : ScriptableObject
    {
    }
}
