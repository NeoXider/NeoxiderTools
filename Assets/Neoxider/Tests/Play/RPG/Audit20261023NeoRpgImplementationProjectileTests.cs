using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Resources;
using Neo.Rpg;
using Neo.Rpg.Components;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play.RPG
{
    public sealed class Audit20261023NeoRpgImplementationProjectileTests
    {
        private readonly List<Object> _objects = new();
        private static readonly Vector3 Origin = new(13000f, -17000f, 0f);

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null) { Object.Destroy(_objects[i]); }
            }
            _objects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Dense3D_PublicFlightFindsNearTargetsPastIgnoredAndDuplicateColliders()
        {
            return ExerciseDense(false);
        }

        [UnityTest]
        public IEnumerator Dense2D_PublicFlightFindsNearTargetsPastIgnoredAndDuplicateColliders()
        {
            return ExerciseDense(true);
        }

        [UnityTest]
        public IEnumerator MixedDimensions_ThreeDimensionalPassKeepsPrecedence()
        {
            RpgCharacter source = Character("source", Origin);
            RpgAttackController controller = source.gameObject.AddComponent<RpgAttackController>();
            controller.EnableBuiltInInput = false;
            RpgCharacter near2D = Character("near2D", Origin + Vector3.right);
            Hurtbox(near2D.gameObject, true, 0f);
            RpgCharacter far3D = Character("far3D", Origin + Vector3.right * 3f);
            Hurtbox(far3D.gameObject, false, 0f);
            RpgAttackDefinition attack = Attack(true, true, 1);
            RpgProjectile projectile = Projectile();
            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            projectile.Initialize(controller, attack, source, Vector3.right);
            for (int frame = 0; frame < 30 && projectile != null; frame++) { yield return null; }
            Assert.That(far3D.CurrentHp, Is.EqualTo(90f));
            Assert.That(near2D.CurrentHp, Is.EqualTo(100f));
        }

        private IEnumerator ExerciseDense(bool twoDimensional)
        {
            RpgCharacter source = Character("source", Origin);
            RpgAttackController controller = source.gameObject.AddComponent<RpgAttackController>();
            controller.EnableBuiltInInput = false;
            // Raw capacity is one. The first colliders have no valid delivery, then one receiver has three hurtboxes.
            GameObject ignored = Track(new GameObject("no-receiver"));
            ignored.transform.position = Origin + Vector3.right * 0.3f;
            Hurtbox(ignored, twoDimensional, 0f);
            Hurtbox(source.gameObject, twoDimensional, 0.6f);
            Hurtbox(source.gameObject, twoDimensional, 0.9f);
            RpgCharacter near = Character("near", Origin + Vector3.right * 1.5f);
            Hurtbox(near.gameObject, twoDimensional, 0f);
            Hurtbox(near.gameObject, twoDimensional, 0.2f);
            Hurtbox(near.gameObject, twoDimensional, 0.4f);
            RpgCharacter next = Character("next", Origin + Vector3.right * 2.5f);
            Hurtbox(next.gameObject, twoDimensional, 0f);
            RpgCharacter far = Character("far", Origin + Vector3.right * 3.5f);
            Hurtbox(far.gameObject, twoDimensional, 0f);
            RpgAttackDefinition attack = Attack(!twoDimensional, twoDimensional, 2);
            RpgProjectile projectile = Projectile();
            int delivered = 0;
            RpgGameObjectEvent onHit = (RpgGameObjectEvent)Get(projectile, "_onHit");
            List<GameObject> targets = new();
            onHit.AddListener((GameObject target) => { delivered++; targets.Add(target.transform.parent.gameObject); });
            Physics.SyncTransforms();
            Physics2D.SyncTransforms();

            // Exercise the public initialization and actual Update/physics path, never a private hit helper.
            projectile.Initialize(controller, attack, source, Vector3.right);
            for (int frame = 0; frame < 30 && projectile != null; frame++) { yield return null; }

            Assert.That(delivered, Is.EqualTo(2));
            Assert.That(targets, Is.EqualTo(new[] { near.gameObject, next.gameObject }));
            Assert.That(near.CurrentHp, Is.EqualTo(90f));
            Assert.That(next.CurrentHp, Is.EqualTo(90f));
            Assert.That(far.CurrentHp, Is.EqualTo(100f));
            Assert.That(source.CurrentHp, Is.EqualTo(100f));
            Assert.That(projectile == null, Is.True, "ProjectileMaxHits limits actual delivery");
        }

        private RpgCharacter Character(string name, Vector3 position)
        {
            RpgCharacterTemplate template = Track(ScriptableObject.CreateInstance<RpgCharacterTemplate>());
            template.resources = new[] { new RpgResourceDefinition { id = new RpgStatId(RpgResourceId.Hp), startMax = 100f } };
            GameObject actor = Track(new GameObject("AuditRpgProjectile-" + name));
            actor.transform.position = position;
            RpgCharacter character = actor.AddComponent<RpgCharacter>();
            character.ApplyTemplate(template);
            return character;
        }

        private RpgAttackDefinition Attack(bool use3D, bool use2D, int maxHits)
        {
            RpgAttackDefinition attack = Track(ScriptableObject.CreateInstance<RpgAttackDefinition>());
            Set(attack, "_use3D", use3D);
            Set(attack, "_use2D", use2D);
            Set(attack, "_power", 10f);
            Set(attack, "_radius", 0.05f);
            Set(attack, "_projectileSpeed", 20000f);
            Set(attack, "_projectileLifetime", 10f);
            Set(attack, "_projectileMaxHits", maxHits);
            return attack;
        }

        private RpgProjectile Projectile()
        {
            GameObject instance = Track(new GameObject("AuditRpgProjectile-flight"));
            instance.transform.position = Origin;
            RpgProjectile projectile = instance.AddComponent<RpgProjectile>();
            Set(projectile, "_maxHitsPerCast", 1);
            return projectile;
        }

        private void Hurtbox(GameObject receiver, bool twoDimensional, float offset)
        {
            GameObject child = new("hurtbox");
            child.transform.SetParent(receiver.transform, false);
            child.transform.localPosition = Vector3.right * offset;
            if (twoDimensional) { child.AddComponent<CircleCollider2D>().radius = 0.05f; }
            else { child.AddComponent<SphereCollider>().radius = 0.05f; }
        }

        private T Track<T>(T instance) where T : Object { _objects.Add(instance); return instance; }
        private static object Get(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
