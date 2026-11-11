using System.Collections.Generic;
using System.Reflection;
using Neo.Abilities;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Contracts of the module additions a survivor game is assembled from: unique weighted draws with pick limits
    ///     (ChanceManager / ChanceSystemBehaviour), the 2D kinematic magnet with its reach event (MagneticField) and
    ///     2D projectile facing (AbilityZonePlaneFacing).
    /// </summary>
    public sealed class SurvivorModuleAdditionsTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (Object o in _created)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }

            _created.Clear();
        }

        private static ChanceManager Table(params float[] weights)
        {
            ChanceManager manager = new ChanceManager();
            manager.AutoNormalize = false;
            foreach (float w in weights)
            {
                manager.AddEntry(w);
            }

            return manager;
        }

        [Test]
        public void PickUnique_DrawsDifferentEntriesAndSkipsZeroWeight()
        {
            ChanceManager manager = Table(1f, 0f, 1f, 1f, 1f);
            float[] rolls = { 0.99f, 0.99f, 0.99f, 0.99f, 0.99f };
            int next = 0;
            manager.RandomProvider = () => rolls[next++ % rolls.Length];
            List<int> result = new List<int>();

            int drawn = manager.PickUnique(3, result);

            Assert.That(drawn, Is.EqualTo(3));
            Assert.That(result, Is.Unique);
            Assert.That(result, Has.No.Member(1), "a zero-weight entry is never drawn");
            Assert.That(manager.PickUnique(10, result), Is.EqualTo(4), "only four entries can be drawn");
        }

        [Test]
        public void PickUnique_HonoursMaxPicks()
        {
            ChanceManager manager = Table(1f, 1f);
            manager.GetEntry(0).MaxPicks = 1;
            List<int> result = new List<int>();

            manager.PickUnique(2, result, new[] { 1, 0 });

            Assert.That(result, Is.EquivalentTo(new[] { 1 }), "entry 0 already used its single pick");
            Assert.That(manager.HasPicksLeft(0, new[] { 0, 0 }), Is.True);
            Assert.That(manager.HasPicksLeft(0, new[] { 1, 0 }), Is.False);
            Assert.That(manager.HasPicksLeft(1, new[] { 0, 99 }), Is.True, "0 = unlimited");
        }

        [Test]
        public void GenerateUnique_HidesFirstThenShowsEachDrawnOptionAndConsumePickLimitsLaterDraws()
        {
            GameObject host = Track(new GameObject("Draft"));
            ChanceSystemBehaviour draft = host.AddComponent<ChanceSystemBehaviour>();
            for (int i = 0; i < 3; i++)
            {
                draft.AddChance(1f);
            }

            draft.Manager.GetEntry(2).MaxPicks = 1;
            List<string> log = new List<string>();
            draft.OnDrawStarted.AddListener(() => log.Add("hide"));
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                draft.GetOrAddEventForIndex(i).AddListener(() => log.Add("show" + index));
            }

            int drawn = draft.GenerateUnique(3);

            Assert.That(drawn, Is.EqualTo(3));
            Assert.That(log[0], Is.EqualTo("hide"));
            Assert.That(log, Has.Member("show0").And.Member("show1").And.Member("show2"));

            draft.ConsumePick(2);
            Assert.That(draft.HasPicksLeft(2), Is.False);
            Assert.That(draft.GenerateUnique(3), Is.EqualTo(2));
            Assert.That(draft.LastDraw, Has.No.Member(2));

            draft.ResetPicks();
            Assert.That(draft.HasPicksLeft(2), Is.True);
        }

        [Test]
        public void MagneticField_TwoDKinematic_ReportsReachedTriggersWithoutRigidbodies()
        {
            GameObject magnetGo = Track(new GameObject("Magnet"));
            MagneticField magnet = magnetGo.AddComponent<MagneticField>();
            magnet.Space = MagneticField.PhysicsSpace.TwoD;
            magnet.Motion = MagneticField.MoveMethod.Kinematic;
            magnet.ReachDistance = 0.5f;
            magnet.SetRadius(3f);

            GameObject near = Pickup("Near", new Vector3(0.3f, 0f, 0f));
            GameObject far = Pickup("Far", new Vector3(10f, 0f, 0f));
            List<GameObject> reached = new List<GameObject>();
            magnet.OnObjectReached.AddListener(reached.Add);
            Physics2D.SyncTransforms();

            InvokeUpdate(magnet);

            Assert.That(reached, Is.EquivalentTo(new[] { near }));
            Assert.That(magnet.ObjectsInFieldCount, Is.EqualTo(1), "the far pickup is outside the radius");
            Assert.That(near.GetComponent<Rigidbody2D>(), Is.Null, "kinematic motion adds no body");
            Assert.That(far.transform.position, Is.EqualTo(new Vector3(10f, 0f, 0f)));
        }

        [Test]
        public void ScreenXYFacing_TurnsAroundZ_GroundXZFacing_LooksAlongTheDirection()
        {
            Quaternion up = AbilityZonePlaneFacing.Facing(Vector3.up, AbilityZonePlane.ScreenXY);
            Assert.That((up * Vector3.right - Vector3.up).sqrMagnitude, Is.LessThan(1e-6f),
                "a sprite drawn pointing right faces up");
            Assert.That((up * Vector3.forward - Vector3.forward).sqrMagnitude, Is.LessThan(1e-6f),
                "the sprite stays flat to the camera");

            Quaternion ground = AbilityZonePlaneFacing.Facing(Vector3.right, AbilityZonePlane.GroundXZ);
            Assert.That((ground * Vector3.forward - Vector3.right).sqrMagnitude, Is.LessThan(1e-6f));
            Assert.That(AbilityZonePlaneFacing.Facing(Vector3.zero, AbilityZonePlane.ScreenXY),
                Is.EqualTo(Quaternion.identity));
        }

        private GameObject Pickup(string name, Vector3 position)
        {
            GameObject go = Track(new GameObject(name));
            go.transform.position = position;
            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.1f;
            return go;
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }

        private static void InvokeUpdate(MagneticField magnet)
        {
            MethodInfo update = typeof(MagneticField).GetMethod("UpdateField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);
            update.Invoke(magnet, null);
        }
    }
}
