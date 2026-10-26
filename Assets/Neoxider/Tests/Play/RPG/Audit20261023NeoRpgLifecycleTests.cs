using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Components.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Tests.Play.RPG
{
    public sealed class Audit20261023NeoRpgLifecycleTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator DeactivatedCaster_CancelsPendingCastAndCanCastAfterReactivation()
        {
            RpgCharacter character = CreateCharacter();
            RpgAttackController controller = character.gameObject.AddComponent<RpgAttackController>();
            controller.EnableBuiltInInput = false;
            RpgAttackDefinition attack = CreateAttack();
            Configure(attack, "_castDelay", 0.05f);
            Configure(controller, "_attacks", new[] { attack });
            int resolutions = 0;
            RpgAttackEvent resolved = new();
            resolved.AddListener((string id) => resolutions++);
            Configure(controller, "_onAttackResolved", resolved);

            Assert.That(controller.TryUseAttack(attack.Id, out _), Is.True);
            Assert.That(controller.IsCasting, Is.True);
            character.gameObject.SetActive(false);
            Assert.That(controller.IsCasting, Is.False);
            yield return new WaitForSeconds(0.1f);
            character.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            Assert.That(resolutions, Is.Zero);
            Assert.That(character.ManaValue, Is.EqualTo(95f));

            Assert.That(controller.TryUseAttack(attack.Id, out _), Is.True);
            yield return new WaitForSeconds(0.1f);
            Assert.That(resolutions, Is.EqualTo(1));
            Assert.That(controller.IsCasting, Is.False);
        }

        [UnityTest]
        public IEnumerator AttackStartedListener_CannotReenterOrResolveAfterDeactivation()
        {
            RpgCharacter character = CreateCharacter();
            RpgAttackController controller = character.gameObject.AddComponent<RpgAttackController>();
            controller.EnableBuiltInInput = false;
            RpgAttackDefinition attack = CreateAttack();
            Configure(attack, "_castDelay", 0.05f);
            Configure(controller, "_attacks", new[] { attack });
            bool nestedAccepted = true;
            int resolutions = 0;
            RpgAttackEvent started = new();
            started.AddListener((string id) =>
            {
                nestedAccepted = controller.TryUseAttack(id, out _);
                character.gameObject.SetActive(false);
            });
            Configure(controller, "_onAttackStarted", started);
            RpgAttackEvent resolved = new();
            resolved.AddListener((string id) => resolutions++);
            Configure(controller, "_onAttackResolved", resolved);

            Assert.That(controller.TryUseAttack(attack.Id, out _), Is.True);
            Assert.That(nestedAccepted, Is.False);
            Assert.That(character.ManaValue, Is.EqualTo(95f));
            Assert.That(controller.IsCasting, Is.False);
            yield return new WaitForSeconds(0.1f);
            Assert.That(resolutions, Is.Zero);
        }

        [UnityTest]
        public IEnumerator EvadeExpiry_ReleasesItsLockAfterInvulnerabilitySettingChanges()
        {
            RpgCharacter character = CreateCharacter();
            character.LockInvulnerable();
            RpgEvadeController evade = character.gameObject.AddComponent<RpgEvadeController>();
            evade.EnableBuiltInInput = false;
            Configure(evade, "_evadeDuration", 0.04f);
            Assert.That(evade.TryStartEvade(), Is.True);
            Assert.That(character.CaptureProfile().InvulnerabilityLocks, Is.EqualTo(2));
            Configure(evade, "_grantInvulnerability", false);

            yield return new WaitForSeconds(0.1f);

            Assert.That(character.CaptureProfile().InvulnerabilityLocks, Is.EqualTo(1));
            Assert.That(evade.IsEvadingState.CurrentValue, Is.False);
            character.UnlockInvulnerable();
            Assert.That(character.IsInvulnerable, Is.False);
        }

        [UnityTest]
        public IEnumerator DisabledEvade_ReleasesOnlyItsLockAndCancelsDelayedCompletion()
        {
            RpgCharacter character = CreateCharacter();
            character.LockInvulnerable();
            RpgEvadeController evade = character.gameObject.AddComponent<RpgEvadeController>();
            evade.EnableBuiltInInput = false;
            Configure(evade, "_evadeDuration", 0.04f);
            Assert.That(evade.TryStartEvade(), Is.True);

            evade.enabled = false;
            Assert.That(character.CaptureProfile().InvulnerabilityLocks, Is.EqualTo(1));
            Assert.That(evade.IsEvading, Is.False);
            Assert.That(evade.IsEvadingState.CurrentValue, Is.False);
            Assert.That(evade.TryStartEvade(), Is.False);
            character.LockInvulnerable();
            yield return new WaitForSeconds(0.1f);
            Assert.That(character.CaptureProfile().InvulnerabilityLocks, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator EvadeStartedListener_MayDisableControllerWithoutLeakingLock()
        {
            RpgCharacter character = CreateCharacter();
            character.LockInvulnerable();
            RpgEvadeController evade = character.gameObject.AddComponent<RpgEvadeController>();
            evade.EnableBuiltInInput = false;
            UnityEvent started = new();
            started.AddListener(() => evade.enabled = false);
            Configure(evade, "_onEvadeStarted", started);

            Assert.That(evade.TryStartEvade(), Is.True);
            Assert.That(evade.IsEvading, Is.False);
            Assert.That(character.CaptureProfile().InvulnerabilityLocks, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ContactDamage_DoesNotDamageInactiveExplicitReceiver()
        {
            RpgCharacter target = CreateCharacter();
            GameObject source = Track(new GameObject("AuditRpgContact"));
            source.transform.position = target.transform.position;
            RpgContactDamage contact = source.AddComponent<RpgContactDamage>();
            Configure(contact, "targetTag", string.Empty);
            Configure(contact, "cooldown", 0.01f);
            Configure(contact, "targetFindInterval", 10f);
            contact.SetTargetReceiver(target);
            int lost = 0;
            contact.OnTargetLost.AddListener(() => lost++);
            yield return null;
            yield return null;
            float before = target.CurrentHp;
            Assert.That(before, Is.LessThan(100f));

            target.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.05f);
            Assert.That(target.CurrentHp, Is.EqualTo(before));
            Assert.That(contact.TargetInRange.CurrentValue, Is.False);
            Assert.That(contact.IsAttacking.CurrentValue, Is.False);
            Assert.That(lost, Is.EqualTo(1));

            contact.SetTargetReceiver(null);
            target.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.05f);
            Assert.That(target.CurrentHp, Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator AutoAttack_DoesNotSpendResourceWhileCachedTargetIsInactive()
        {
            RpgCharacter target = CreateCharacter();
            target.gameObject.tag = "Player";
            RpgCharacter source = CreateCharacter();
            RpgAttackController controller = source.gameObject.AddComponent<RpgAttackController>();
            controller.EnableBuiltInInput = false;
            RpgAttackDefinition attack = CreateAttack();
            Configure(controller, "_attacks", new[] { attack });
            RpgAutoAttackController autoAttack = source.gameObject.AddComponent<RpgAutoAttackController>();
            Configure(autoAttack, "attackInterval", 0.01f);
            Configure(autoAttack, "targetFindInterval", 10f);
            yield return new WaitForSeconds(0.05f);
            float manaBefore = source.ManaValue;
            Assert.That(manaBefore, Is.LessThan(100f));

            target.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.05f);
            Assert.That(source.ManaValue, Is.EqualTo(manaBefore));
        }

        [UnityTest]
        public IEnumerator Aura_UsesColliderCenterAndNegativeScaleAndDamagesEachActorOnce()
        {
            RpgCharacter target = CreateCharacter();
            GameObject weaponObject = Track(new GameObject("AuditRpgAura"));
            weaponObject.transform.position = target.transform.position + Vector3.right * 8f;
            weaponObject.transform.localScale = Vector3.one * -2f;
            SphereCollider area = weaponObject.AddComponent<SphereCollider>();
            area.center = Vector3.right * 4f;
            area.radius = 1f;
            AuraWeapon aura = weaponObject.AddComponent<AuraWeapon>();
            aura.hitLayers = -1;
            aura.targetTag = string.Empty;
            for (int i = 0; i < 2; i++)
            {
                GameObject child = Track(new GameObject("AuditRpgHurtbox"));
                child.transform.SetParent(target.transform, false);
                child.AddComponent<SphereCollider>();
            }
            Physics.SyncTransforms();

            aura.ApplyAuraDamage();
            Assert.That(target.CurrentHp, Is.EqualTo(90f));
            aura.ApplyAuraDamage();
            Assert.That(target.CurrentHp, Is.EqualTo(80f));
            yield return null;
        }

        private RpgCharacter CreateCharacter()
        {
            GameObject actor = Track(new GameObject("AuditRpgLifecycleActor"));
            actor.transform.position = new Vector3(20000f, 0f, 0f);
            RpgCharacter character = actor.AddComponent<RpgCharacter>();
            RpgCharacterTemplate template = Track(ScriptableObject.CreateInstance<RpgCharacterTemplate>());
            template.resources = new[]
            {
                new RpgResourceDefinition { id = new RpgStatId(RpgStatPreset.Hp) },
                new RpgResourceDefinition { id = new RpgStatId(RpgStatPreset.Mana) }
            };
            character.ApplyTemplate(template);
            return character;
        }

        private RpgAttackDefinition CreateAttack()
        {
            RpgAttackDefinition attack = Track(ScriptableObject.CreateInstance<RpgAttackDefinition>());
            JsonUtility.FromJsonOverwrite("{\"_id\":\"audit-cast\",\"_cooldown\":0," +
                "\"_costResourceId\":\"Mana\",\"_costAmount\":5,\"_use2D\":false,\"_use3D\":false}", attack);
            return attack;
        }

        private T Track<T>(T value) where T : Object
        {
            _objects.Add(value);
            return value;
        }

        private static void Configure(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
