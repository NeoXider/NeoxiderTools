using System.Collections;
using System.Reflection;
using Neo.Rpg;
using Neo.Rpg.Components;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tests.Play.RPG
{
    /// <summary>
    ///     End-to-end PlayMode coverage for the RPG combat loop:
    ///     - Player melee swing damages an NPC via <see cref="RpgAttackController"/> (Direct delivery).
    ///     - Melee NPC drains player HP via <see cref="RpgContactDamage"/>.
    ///     - Ranged NPC damages player via <see cref="RpgAttackController"/> (Projectile delivery).
    ///     Spawned objects build their own components in code so the suite is self-contained and does
    ///     not depend on demo prefabs/scenes.
    /// </summary>
    [TestFixture]
    public class RpgCombatPlayModeTests
    {
        private const float PlayerStartHp = 100f;
        private const float NpcStartHp = 80f;

        private GameObject _player;
        private GameObject _npc;
        private GameObject _projectileTemplate;
        private RpgAttackDefinition _attackDefinition;
        private RpgAttackDefinition _projectileDefinition;
        private RpgAttackPreset _projectilePreset;

        [TearDown]
        public void TearDown()
        {
            if (_player != null)
            {
                Object.DestroyImmediate(_player);
            }

            if (_npc != null)
            {
                Object.DestroyImmediate(_npc);
            }

            // WHY: Clean up any spawned projectile clones (active or inactive) before the template
            // so we don't accidentally kill the template earlier than intended.
            foreach (RpgProjectile leftover in Object.FindObjectsByType<RpgProjectile>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (_projectileTemplate != null && leftover.gameObject == _projectileTemplate)
                {
                    continue;
                }

                Object.DestroyImmediate(leftover.gameObject);
            }

            if (_projectileTemplate != null)
            {
                Object.DestroyImmediate(_projectileTemplate);
            }

            if (_attackDefinition != null)
            {
                Object.DestroyImmediate(_attackDefinition);
            }

            if (_projectileDefinition != null)
            {
                Object.DestroyImmediate(_projectileDefinition);
            }

            if (_projectilePreset != null)
            {
                Object.DestroyImmediate(_projectilePreset);
            }

            _player = null;
            _npc = null;
            _projectileTemplate = null;
            _attackDefinition = null;
            _projectileDefinition = null;
            _projectilePreset = null;
        }

        [UnityTest]
        public IEnumerator PlayerDirectAttack_DealsDamageToNpc()
        {
            _player = BuildCharacter("Player", new Vector3(0f, 0f, 0f), PlayerStartHp, false);
            _npc = BuildCharacter("MeleeDummy", new Vector3(0f, 0f, 2f), NpcStartHp, true);

            RpgAttackController controller = _player.AddComponent<RpgAttackController>();
            controller.EnableBuiltInInput = false;
            SetPrivate(controller, "_characterSource", _player.GetComponent<RpgCharacter>());

            _attackDefinition = CreateDirectAttack(35f, 5f, 0.6f);
            SetPrivate(controller, "_attacks", new[] { _attackDefinition });

            yield return null;

            float beforeHp = _npc.GetComponent<RpgCharacter>().HpValue;
            bool used = controller.UsePrimaryAttack();
            yield return null;

            Assert.That(used, Is.True, "UsePrimaryAttack should have succeeded.");
            float afterHp = _npc.GetComponent<RpgCharacter>().HpValue;
            Assert.That(afterHp, Is.LessThan(beforeHp),
                $"NPC HP should drop after a melee swing (before={beforeHp}, after={afterHp}).");
            Assert.That(beforeHp - afterHp, Is.EqualTo(35f).Within(0.001f),
                "Direct attack should deal its full power (no resistances configured).");
        }

        [UnityTest]
        public IEnumerator MeleeNpc_ContactDamage_DrainsPlayerHp()
        {
            _player = BuildCharacter("Player", new Vector3(0f, 0f, 0f), PlayerStartHp, true);
            _npc = BuildCharacter("MeleeNpc", new Vector3(0f, 0f, 1.2f), NpcStartHp, true);

            RpgContactDamage contact = _npc.AddComponent<RpgContactDamage>();
            SetPrivate(contact, "damage", 7);
            SetPrivate(contact, "damageRange", 1.8f);
            SetPrivate(contact, "cooldown", 0.1f);
            contact.SetTargetReceiver(_player.GetComponent<RpgCharacter>());

            float beforeHp = _player.GetComponent<RpgCharacter>().HpValue;

            float waited = 0f;
            while (waited < 0.5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            float afterHp = _player.GetComponent<RpgCharacter>().HpValue;
            Assert.That(afterHp, Is.LessThan(beforeHp),
                $"Melee NPC contact damage should drain player HP (before={beforeHp}, after={afterHp}).");
        }

        [UnityTest]
        public IEnumerator RangedNpc_ProjectileAttack_DealsDamageToPlayer()
        {
            _player = BuildCharacter("Player", new Vector3(0f, 0f, 3.5f), PlayerStartHp, true);
            _npc = BuildCharacter("RangedNpc", new Vector3(0f, 0f, 0f), NpcStartHp, false);

            RpgAttackController controller = _npc.AddComponent<RpgAttackController>();
            controller.EnableBuiltInInput = false;
            SetPrivate(controller, "_characterSource", _npc.GetComponent<RpgCharacter>());

            // WHY: The template must stay inactive so its own Update() never destroys it (RpgProjectile
            // self-destructs when _owner is null). Instantiate copies inactive state to the clone,
            // so we activate clones manually after they spawn.
            _projectileTemplate = new GameObject("ProjectileTemplate");
            _projectileTemplate.SetActive(false);
            RpgProjectile projectile = _projectileTemplate.AddComponent<RpgProjectile>();

            _projectileDefinition = CreateProjectileAttack(22f, projectile);
            _projectilePreset = ScriptableObject.CreateInstance<RpgAttackPreset>();
            _projectilePreset.name = "TestRangedPreset";
            SetPrivate(_projectilePreset, "_attackDefinition", _projectileDefinition);
            SetPrivate(_projectilePreset, "_requireTarget", true);
            SetPrivate(_projectilePreset, "_aimAtTarget", true);
            SetPrivate(_projectilePreset, "_useSelectorComponentWhenAvailable", false);

            SetPrivate(controller, "_presets", new[] { _projectilePreset });

            yield return null;

            float beforeHp = _player.GetComponent<RpgCharacter>().HpValue;
            bool used = controller.TryUsePreset(_projectilePreset, _player, out string failReason);
            Assert.That(used, Is.True, $"Projectile preset should fire: {failReason}");

            ActivateSpawnedProjectiles();

            float waited = 0f;
            while (waited < 2.0f)
            {
                waited += Time.deltaTime;
                if (_player.GetComponent<RpgCharacter>().HpValue < beforeHp)
                {
                    break;
                }

                yield return null;
            }

            float afterHp = _player.GetComponent<RpgCharacter>().HpValue;
            Assert.That(afterHp, Is.LessThan(beforeHp),
                $"Ranged projectile should drain player HP (before={beforeHp}, after={afterHp}).");
            Assert.That(beforeHp - afterHp, Is.EqualTo(22f).Within(0.001f),
                "Projectile should deal its full power (no resistances configured).");
        }

        private void ActivateSpawnedProjectiles()
        {
            foreach (RpgProjectile p in Object.FindObjectsByType<RpgProjectile>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.gameObject == _projectileTemplate)
                {
                    continue;
                }

                if (!p.gameObject.activeSelf)
                {
                    p.gameObject.SetActive(true);
                }
            }
        }

        private static GameObject BuildCharacter(string name, Vector3 position, float startHp, bool addCollider)
        {
            GameObject go = new(name);
            go.transform.position = position;

#if MIRROR
            go.AddComponent<NetworkIdentity>();
#endif

            RpgCharacter character = go.AddComponent<RpgCharacter>();
            RpgCharacterTemplate template = ScriptableObject.CreateInstance<RpgCharacterTemplate>();
            template.name = $"{name}Template";
            template.resources = new[]
            {
                new RpgResourceDefinition
                {
                    id = new RpgStatId(RpgStatPreset.Hp),
                    startCurrent = startHp,
                    startMax = startHp,
                    restoreOnAwake = true,
                    restoreToFull = false
                }
            };
            character.ApplyTemplate(template);

            if (addCollider)
            {
                SphereCollider collider = go.AddComponent<SphereCollider>();
                collider.radius = 0.5f;
            }

            return go;
        }

        private static RpgAttackDefinition CreateDirectAttack(float power, float range, float radius)
        {
            RpgAttackDefinition definition = ScriptableObject.CreateInstance<RpgAttackDefinition>();
            definition.name = "TestDirectAttack";
            SetPrivate(definition, "_id", "TestDirectAttack");
            SetPrivate(definition, "_deliveryType", RpgAttackDeliveryType.Direct);
            SetPrivate(definition, "_hitMode", RpgHitMode.Damage);
            SetPrivate(definition, "_power", power);
            SetPrivate(definition, "_range", range);
            SetPrivate(definition, "_radius", radius);
            SetPrivate(definition, "_cooldown", 0f);
            SetPrivate(definition, "_castDelay", 0f);
            SetPrivate(definition, "_use2D", false);
            SetPrivate(definition, "_use3D", true);
            SetPrivate(definition, "_targetLayers", (LayerMask)(-1));
            SetPrivate(definition, "_maxTargets", 1);
            return definition;
        }

        private static RpgAttackDefinition CreateProjectileAttack(float power, RpgProjectile projectilePrefab)
        {
            RpgAttackDefinition definition = ScriptableObject.CreateInstance<RpgAttackDefinition>();
            definition.name = "TestProjectileAttack";
            SetPrivate(definition, "_id", "TestProjectileAttack");
            SetPrivate(definition, "_deliveryType", RpgAttackDeliveryType.Projectile);
            SetPrivate(definition, "_hitMode", RpgHitMode.Damage);
            SetPrivate(definition, "_power", power);
            SetPrivate(definition, "_range", 10f);
            SetPrivate(definition, "_radius", 0.5f);
            SetPrivate(definition, "_cooldown", 0f);
            SetPrivate(definition, "_castDelay", 0f);
            SetPrivate(definition, "_use2D", false);
            SetPrivate(definition, "_use3D", true);
            SetPrivate(definition, "_targetLayers", (LayerMask)(-1));
            SetPrivate(definition, "_maxTargets", 1);
            SetPrivate(definition, "_projectilePrefab", projectilePrefab);
            SetPrivate(definition, "_projectileSpeed", 12f);
            SetPrivate(definition, "_projectileLifetime", 3f);
            SetPrivate(definition, "_projectileMaxHits", 1);
            return definition;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(field, Is.Not.Null,
                $"Field '{fieldName}' not found on type '{target.GetType().FullName}'.");
            field.SetValue(target, value);
        }
    }
}
