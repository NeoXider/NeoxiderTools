using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using PartialCandidate = global::Neo.Editor.Tests.Audit20261023FixtureNeoEditorCollectionPartialCandidate;
using SecondCandidate = global::Neo.Editor.Tests.Audit20261023FixtureNeoEditorCollectionSecondCandidate;
using CompleteCandidate = global::Neo.Editor.Tests.Audit20261023FixtureNeoEditorCollectionCompleteCandidate;
using ComponentFindProbe = global::Neo.Editor.Tests.Audit20261023FixtureNeoEditorCollectionComponentFindProbe;
using GameObjectFindProbe = global::Neo.Editor.Tests.Audit20261023FixtureNeoEditorCollectionGameObjectFindProbe;
using CollectionProbe = global::Neo.Editor.Tests.Audit20261023FixtureNeoEditorCollectionCollectionProbe;
using IFirstRequirement = global::Neo.Editor.Tests.Audit20261023FixtureEditorFirstRequirement;
using ISecondRequirement = global::Neo.Editor.Tests.Audit20261023FixtureEditorSecondRequirement;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoEditorCollectionTests
    {
        private const string ResourcePath = global::Neo.Editor.Tests.Audit20261023FixtureContracts.ResourcePath;
        private NeoxiderSettings.NeoxiderData _previousSettings;
        private GameObject _root;













        [SetUp]
        public void SetUp()
        {
            _previousSettings = NeoxiderSettings.Current;
            NeoxiderSettings.Current = new NeoxiderSettings.NeoxiderData { enableAttributeSearch = true };
            _root = new GameObject("Audit20261023NeoEditorCollections");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            NeoxiderSettings.Current = _previousSettings;
        }

        [Test]
        public void ComponentLookup_PopulatesTypedArraysAndListsWithActualComponents()
        {
            BoxCollider local = _root.AddComponent<BoxCollider>();
            GameObject child = new("Child");
            child.transform.SetParent(_root.transform);
            BoxCollider nested = child.AddComponent<BoxCollider>();
            CollectionProbe probe = _root.AddComponent<CollectionProbe>();

            ComponentDrawer.ProcessComponentAttributes(probe);

            Assert.That(probe.Local, Is.EqualTo(new[] { local }));
            Assert.That(probe.Children, Is.EquivalentTo(new[] { local, nested }));
            Assert.That(probe.SceneArray, Has.Member(local));
            Assert.That(probe.SceneArray, Has.Member(nested));
            Assert.That(probe.SceneList, Has.Member(local));
            Assert.That(probe.SceneList, Has.Member(nested));
        }

        [Test]
        public void ComponentLookup_PreservesExistingCollections()
        {
            BoxCollider collider = _root.AddComponent<BoxCollider>();
            CollectionProbe probe = _root.AddComponent<CollectionProbe>();
            BoxCollider[] assigned = { collider };
            List<BoxCollider> list = new() { collider };
            probe.Local = assigned;
            probe.Children = list;
            probe.SceneArray = assigned;
            probe.SceneList = list;

            ComponentDrawer.ProcessComponentAttributes(probe);

            Assert.That(probe.Local, Is.SameAs(assigned));
            Assert.That(probe.Children, Is.SameAs(list));
            Assert.That(probe.SceneArray, Is.SameAs(assigned));
            Assert.That(probe.SceneList, Is.SameAs(list));
        }

        [Test]
        public void SceneLookup_ChoosesTheFirstCandidateSatisfyingEveryInterface()
        {
            PartialCandidate partial = _root.AddComponent<PartialCandidate>();
            CompleteCandidate complete = _root.AddComponent<CompleteCandidate>();
            ComponentFindProbe probe = _root.AddComponent<ComponentFindProbe>();
            FieldInfo field = typeof(ComponentFindProbe).GetField(nameof(ComponentFindProbe.Candidate));

            Assert.That(ComponentDrawer.FindCompatibleSceneCandidate(field, new Object[] { partial, complete }),
                Is.SameAs(complete), "A candidate satisfying only one stacked requirement must be skipped.");
            ComponentDrawer.ProcessComponentAttributes(probe);
            Assert.That(probe.Candidate, Is.SameAs(complete));

            probe.Candidate = partial;
            ComponentDrawer.ProcessComponentAttributes(probe);
            Assert.That(probe.Candidate, Is.SameAs(partial), "Automatic lookup preserves an explicit reference.");
        }

        [Test]
        public void SceneLookup_ValidatesGameObjectsThroughAllOfTheirComponents()
        {
            _root.AddComponent<PartialCandidate>();
            GameObject compatible = new("Compatible");
            compatible.transform.SetParent(_root.transform);
            compatible.AddComponent<PartialCandidate>();
            compatible.AddComponent<SecondCandidate>();
            GameObjectFindProbe probe = _root.AddComponent<GameObjectFindProbe>();
            FieldInfo field = typeof(GameObjectFindProbe).GetField(nameof(GameObjectFindProbe.Candidate));

            Assert.That(ComponentDrawer.FindCompatibleSceneCandidate(field, new Object[] { _root, compatible }),
                Is.SameAs(compatible));
            ComponentDrawer.ProcessComponentAttributes(probe);
            Assert.That(probe.Candidate, Is.SameAs(compatible));
        }

        [Test]
        public void SceneLookup_NoCompatibleCandidateDoesNotDirtyOrRepeatTheDiagnostic()
        {
            _root.AddComponent<PartialCandidate>();
            ComponentFindProbe probe = _root.AddComponent<ComponentFindProbe>();
            EditorUtility.ClearDirty(probe);
            LogAssert.Expect(LogType.Warning,
                "No compatible MonoBehaviour found in scene for field 'Candidate'. All RequireInterface constraints must be satisfied.");

            ComponentDrawer.ProcessComponentAttributes(probe);
            ComponentDrawer.ProcessComponentAttributes(probe);

            Assert.That(probe.Candidate, Is.Null);
            Assert.That(EditorUtility.IsDirty(probe), Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ResourceLookup_PopulatesTypedListAndArrayAndKeepsExistingAssignments()
        {
            string folderName = "Audit20261023NeoEditorResources_" + Guid.NewGuid().ToString("N");
            string folder = "Assets/" + folderName;
            AssetDatabase.CreateFolder("Assets", folderName);
            AssetDatabase.CreateFolder(folder, "Resources");
            AssetDatabase.CreateFolder(folder + "/Resources", ResourcePath);
            try
            {
                Texture2D first = new(2, 2);
                Texture2D second = new(2, 2);
                AssetDatabase.CreateAsset(first, folder + "/Resources/" + ResourcePath + "/First.asset");
                AssetDatabase.CreateAsset(second, folder + "/Resources/" + ResourcePath + "/Second.asset");
                CollectionProbe probe = _root.AddComponent<CollectionProbe>();

                ResourceDrawer.ProcessResourceAttributes(probe);

                Assert.That(probe.Images, Is.EquivalentTo(new[] { first, second }));
                Assert.That(probe.ImageArray, Is.EquivalentTo(new[] { first, second }));
                List<Texture2D> assigned = probe.Images;
                ResourceDrawer.ProcessResourceAttributes(probe);
                Assert.That(probe.Images, Is.SameAs(assigned));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
