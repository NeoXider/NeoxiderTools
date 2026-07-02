using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
#if MIRROR
using Mirror;
#endif

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the 9.7.0 Spawner deny zones: positions inside a configured deny
    ///     collider are rejected, everything else (and a zone-less spawner) passes.
    /// </summary>
    [TestFixture]
    public class SpawnerDenyZoneTests
    {
        private GameObject _spawnerGo;
        private Spawner _spawner;
        private GameObject _denyGo;

        [SetUp]
        public void SetUp()
        {
#if MIRROR
            // Spawner is a NetworkBehaviour with Mirror installed; OnValidate demands an identity.
            _spawnerGo = new GameObject("SpawnerDenyZoneTests", typeof(NetworkIdentity));
#else
            _spawnerGo = new GameObject("SpawnerDenyZoneTests");
#endif
            _spawner = _spawnerGo.AddComponent<Spawner>();

            _denyGo = new GameObject("DenyZone");
            _denyGo.transform.position = Vector3.zero;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_spawnerGo);
            Object.DestroyImmediate(_denyGo);
        }

        private void SetDenyAreas3D(params Collider[] areas)
        {
            FieldInfo field = typeof(Spawner).GetField("_denyAreas",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Spawner._denyAreas field expected");
            field.SetValue(_spawner, areas);
        }

        private void SetDenyAreas2D(params Collider2D[] areas)
        {
            FieldInfo field = typeof(Spawner).GetField("_denyAreas2D",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Spawner._denyAreas2D field expected");
            field.SetValue(_spawner, areas);
        }

        [Test]
        public void NoDenyZones_EveryPositionIsAllowed()
        {
            Assert.IsTrue(_spawner.IsPositionAllowed(Vector3.zero));
            Assert.IsTrue(_spawner.IsPositionAllowed(new Vector3(100f, -5f, 3f)));
        }

        [Test]
        public void PositionInsideDenyCollider_IsRejected()
        {
            var box = _denyGo.AddComponent<BoxCollider>();
            box.size = new Vector3(2f, 2f, 2f);
            Physics.SyncTransforms();

            SetDenyAreas3D(box);

            Assert.IsFalse(_spawner.IsPositionAllowed(Vector3.zero), "center of the deny box must be rejected");
            Assert.IsTrue(_spawner.IsPositionAllowed(new Vector3(10f, 0f, 0f)), "far outside the box must pass");
        }

        [Test]
        public void PositionInsideDenyCollider2D_IsRejected()
        {
            var box = _denyGo.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2f, 2f);
            Physics2D.SyncTransforms();

            SetDenyAreas2D(box);

            Assert.IsFalse(_spawner.IsPositionAllowed(Vector3.zero), "center of the 2D deny box must be rejected");
            Assert.IsTrue(_spawner.IsPositionAllowed(new Vector3(10f, 0f, 0f)), "far outside the box must pass");
        }

        [Test]
        public void NullEntriesInDenyAreas_AreIgnored()
        {
            SetDenyAreas3D(new Collider[] { null });
            SetDenyAreas2D(new Collider2D[] { null });

            Assert.IsTrue(_spawner.IsPositionAllowed(Vector3.zero));
        }
    }
}
