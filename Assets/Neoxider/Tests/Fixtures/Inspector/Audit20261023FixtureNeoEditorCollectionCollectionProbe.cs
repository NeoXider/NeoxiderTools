using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023FixtureNeoEditorCollectionCollectionProbe : MonoBehaviour
    {
        [global::GetComponents] public BoxCollider[] Local = Array.Empty<BoxCollider>();
        [global::GetComponents(true)] public List<BoxCollider> Children = new();
        [global::FindAllInScene] public BoxCollider[] SceneArray;
        [global::FindAllInScene] public List<BoxCollider> SceneList = new();
        [global::LoadAllFromResources(global::Neo.Editor.Tests.Audit20261023FixtureContracts.ResourcePath)] public List<Texture2D> Images = new();
        [global::LoadAllFromResources(global::Neo.Editor.Tests.Audit20261023FixtureContracts.ResourcePath)] public Texture2D[] ImageArray;
    }
}
