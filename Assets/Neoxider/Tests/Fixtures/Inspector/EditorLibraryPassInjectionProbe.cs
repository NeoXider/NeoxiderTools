using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Inspector-processing probe: one resolvable [GetComponent], one [LoadFromResources] that can never resolve,
    ///     one [GetComponents] on a non-component element type and one [GetComponents] whose host never has a
    ///     matching component, next to plain value fields.
    /// </summary>
    public sealed class EditorLibraryPassInjectionProbe : MonoBehaviour
    {
        public const string MissingResourcePath = "NeoEditorLibraryPass/DoesNotExist";

        [global::GetComponent] public BoxCollider Local;

        [global::LoadFromResources(MissingResourcePath)] public EditorLibraryPassUnloadedAsset Missing;

        [global::GetComponents] public int[] NotComponents = new int[0];

        [global::GetComponents] public SphereCollider[] NoMatches = new SphereCollider[0];

        public int Plain;
        public float Ratio;
        public Vector3 Position;
    }
}
