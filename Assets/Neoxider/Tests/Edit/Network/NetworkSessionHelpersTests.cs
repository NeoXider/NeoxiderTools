#if MIRROR
using Mirror;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Scene-object activation for every peer, and the singleton lookup that must not cache a miss for the whole
    ///     session. Both run without a live Mirror session.
    /// </summary>
    [TestFixture]
    public sealed class NetworkSessionHelpersTests
    {
        private sealed class RetrySingleton : NetworkSingleton<RetrySingleton>
        {
        }

        private readonly System.Collections.Generic.List<GameObject> _created = new();

        [SetUp]
        public void SetUp()
        {
            RetrySingleton.DestroyInstance();
            RetrySingleton.ForgetFailedSearch();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
            RetrySingleton.DestroyInstance();
            RetrySingleton.ForgetFailedSearch();
        }

        private GameObject NewSceneObject(string name, ulong sceneId, bool active)
        {
            GameObject go = new GameObject(name, typeof(NetworkIdentity));
            go.GetComponent<NetworkIdentity>().sceneId = sceneId;
            go.SetActive(active);
            _created.Add(go);
            return go;
        }

        // ---- NetworkSingleton.I ------------------------------------------------------------------------

        [Test]
        public void Singleton_MissIsTrustedForTheFrame_ButNotForever()
        {
            Assert.That(RetrySingleton.I, Is.Null);

            GameObject go = new GameObject("RetrySingleton", typeof(NetworkIdentity));
            _created.Add(go);
            go.AddComponent<RetrySingleton>();

            Assert.That(RetrySingleton.I, Is.Null, "within the same frame the earlier miss is still trusted (no per-call scene search)");

            NetworkSingletonSearchState.Epoch++; // what a scene load or unload does
            Assert.That(RetrySingleton.I, Is.Not.Null, "after a scene change the lookup searches again");
            Assert.That(RetrySingleton.HasInstance, Is.True);
        }

        [Test]
        public void Singleton_ForgetFailedSearch_ForcesAnImmediateRetry()
        {
            Assert.That(RetrySingleton.I, Is.Null);

            GameObject go = new GameObject("RetrySingleton", typeof(NetworkIdentity));
            _created.Add(go);
            go.AddComponent<RetrySingleton>();

            RetrySingleton.ForgetFailedSearch();
            Assert.That(RetrySingleton.I, Is.Not.Null);
        }

        [Test]
        public void Singleton_ADestroyedInstance_IsReportedAsARealNull()
        {
            GameObject go = new GameObject("RetrySingleton", typeof(NetworkIdentity));
            _created.Add(go);
            go.AddComponent<RetrySingleton>();
            Assert.That(RetrySingleton.I, Is.Not.Null);

            Object.DestroyImmediate(go);
            NetworkSingletonSearchState.Epoch++;

            RetrySingleton result = RetrySingleton.I;
            Assert.That(ReferenceEquals(result, null), Is.True,
                "a destroyed instance must not leak out as a fake null: `I?.Member` would reach the dead object");
        }

        [Test]
        public void Singleton_AnInactiveInstanceIsFound()
        {
            GameObject go = new GameObject("RetrySingleton", typeof(NetworkIdentity));
            _created.Add(go);
            go.AddComponent<RetrySingleton>();
            go.SetActive(false);

            Assert.That(RetrySingleton.I, Is.Not.Null, "scene network objects start disabled until the session wakes them");
        }

        // ---- scene object activation ---------------------------------------------------------------------

        [Test]
        public void ActivateNetworkedSceneObjects_WakesDisabledSceneIdentities()
        {
            GameObject door = NewSceneObject("Door", 701, false);
            GameObject coordinator = NewSceneObject("Coordinator", 702, false);

            int activated = NeoMirrorSceneReactivator.ActivateNetworkedSceneObjects();

            Assert.That(door.activeSelf, Is.True);
            Assert.That(coordinator.activeSelf, Is.True);
            Assert.That(activated, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ActivateNetworkedSceneObjects_LeavesRuntimeObjectsAndActiveOnesAlone()
        {
            GameObject runtime = NewSceneObject("RuntimeCopy", 0, false);
            GameObject alreadyOn = NewSceneObject("AlreadyOn", 703, true);

            NeoMirrorSceneReactivator.ActivateNetworkedSceneObjects();

            Assert.That(runtime.activeSelf, Is.False, "sceneId 0 means it is not a scene object");
            Assert.That(alreadyOn.activeSelf, Is.True);
        }

        [Test]
        public void ActivateNetworkedSceneObjects_HonoursTheSkipFilter()
        {
            GameObject template = NewSceneObject("PlayerTemplate", 704, false);
            GameObject other = NewSceneObject("Other", 705, false);

            NeoMirrorSceneReactivator.ActivateNetworkedSceneObjects(candidate => candidate == template);

            Assert.That(template.activeSelf, Is.False, "the scene player template must stay off");
            Assert.That(other.activeSelf, Is.True);
        }

        [Test]
        public void ActivateNetworkedSceneObjects_IgnoresHiddenEditorObjects()
        {
            GameObject hidden = NewSceneObject("Hidden", 706, false);
            hidden.hideFlags = HideFlags.HideAndDontSave;

            NeoMirrorSceneReactivator.ActivateNetworkedSceneObjects();

            Assert.That(hidden.activeSelf, Is.False);
            hidden.hideFlags = HideFlags.None;
        }

        [Test]
        public void ActivateNetworkedSceneObjects_UnloadedSceneIsANoOp()
        {
            Assert.That(NeoMirrorSceneReactivator.ActivateNetworkedSceneObjects(default(UnityEngine.SceneManagement.Scene)), Is.EqualTo(0));
        }
    }
}
#endif
