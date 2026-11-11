using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     The outside-facing spawn and event API of the ability hub: archetype roles for plain prefabs, the spawned
    ///     object callback, the event listener component and the summon component.
    /// </summary>
    public sealed class AbilitySpawnApiTests
    {
        [Test]
        public void AdoptSpawned_PlainObjectWithProjectileRole_BecomesAProjectile_AndIsHandedToListeners()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour owner = rig.AddUnit("Owner", 1, Vector3.zero);
                AbilityUnitBehaviour enemy = rig.AddUnit("Enemy", 2, new Vector3(5f, 0f, 0f));
                AbilityBlueprint bolt = new AbilityBlueprint
                {
                    Id = "bolt",
                    Targeting = TargetingMode.Unit,
                    Delivery = AbilityDeliveryType.Projectile,
                    ProjectileArchetypeId = "orb"
                };
                bolt.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
                rig.System.RegisterAbility(bolt);
                rig.System.GrantAbility(owner.UnitId, "bolt");
                // WHY: the archetype is mapped after the cast, so the cast itself spawns nothing in Edit Mode.
                CastResult cast = rig.System.Cast(CastRequest.AtUnit(owner.UnitId, "bolt", enemy.UnitId));
                Assert.That(cast.Success, Is.True);

                GameObject prefab = rig.Own(new GameObject("Orb Prefab"));
                rig.Hub.AddArchetype(new SpawnArchetypeEntry
                {
                    Id = "orb",
                    Prefab = prefab,
                    Role = SpawnArchetypeRole.Projectile,
                    Projectile = new AbilityProjectileSettings
                    {
                        MaxHits = 3, HitRadius = 0.7f, Plane = AbilityZonePlane.ScreenXY
                    }
                });

                int raised = 0;
                int inspectorRaised = 0;
                GameObject seen = null;
                string seenAbility = null;
                rig.Hub.EntitySpawned += (request, spawned) =>
                {
                    raised++;
                    seen = spawned;
                    seenAbility = request.AbilityId;
                };
                rig.Hub.OnEntitySpawned.AddListener(_ => inspectorRaised++);

                GameObject instance = rig.Own(new GameObject("Orb"));
                SpawnRequest spawn = new SpawnRequest("orb", owner.UnitId, Vector3.zero, Vector3.right, enemy.UnitId,
                    "bolt", 0f, cast.CastId);
                rig.Hub.AdoptSpawned(spawn, instance);

                AbilityProjectileBehaviour projectile = instance.GetComponent<AbilityProjectileBehaviour>();
                Assert.That(projectile, Is.Not.Null, "The hub adds the projectile component to a plain object.");
                Assert.That(projectile.IsFlying, Is.True);
                Assert.That(projectile.CastId, Is.EqualTo(cast.CastId));
                Assert.That(projectile.Plane, Is.EqualTo(AbilityZonePlane.ScreenXY));
                Assert.That(projectile.HitRadius, Is.EqualTo(0.7f));
                Assert.That(raised, Is.EqualTo(1));
                Assert.That(inspectorRaised, Is.EqualTo(1));
                Assert.That(seen, Is.SameAs(instance));
                Assert.That(seenAbility, Is.EqualTo("bolt"));

                rig.Hub.AdoptSpawned(spawn, instance);
                Assert.That(instance.GetComponents<AbilityProjectileBehaviour>().Length, Is.EqualTo(1),
                    "A reused instance keeps its component.");
            }
        }

        [Test]
        public void AdoptSpawned_ZoneViewRole_AddsTheView_AndAsIsAddsNothing()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour owner = rig.AddUnit("Owner", 1, Vector3.zero);
                GameObject prefab = rig.Own(new GameObject("Visual Prefab"));
                rig.Hub.AddArchetype(new SpawnArchetypeEntry
                {
                    Id = "pool", Prefab = prefab, Role = SpawnArchetypeRole.ZoneView,
                    Zone = new AbilityZoneViewSettings { Plane = AbilityZonePlane.ScreenXY, Offset = Vector3.zero }
                });
                rig.Hub.AddArchetype("effect", prefab);

                GameObject zone = rig.Own(new GameObject("Zone"));
                rig.Hub.AdoptSpawned(new SpawnRequest("pool", owner.UnitId, new Vector3(2f, 3f, 0f), Vector3.zero,
                    UnitId.None, "fire", 2f), zone);
                Assert.That(zone.GetComponent<AbilityZoneView>(), Is.Not.Null);
                Assert.That(zone.transform.position, Is.EqualTo(new Vector3(2f, 3f, 0f)));
                Assert.That(zone.transform.localScale.x, Is.EqualTo(4f).Within(0.001f), "radius 2 over mesh radius 0.5");

                GameObject effect = rig.Own(new GameObject("Effect"));
                rig.Hub.AdoptSpawned(new SpawnRequest("effect", owner.UnitId, Vector3.zero, Vector3.zero, UnitId.None,
                    "fire", 0f), effect);
                Assert.That(effect.GetComponents<MonoBehaviour>().Length, Is.Zero, "As Is leaves the object alone.");
            }
        }

        [Test]
        public void EventListener_FiltersByAbilityAndUnit_AndRelaysAmountAndObjects()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour player = rig.AddUnit("player", 1, Vector3.zero);
                AbilityUnitBehaviour enemy = rig.AddUnit("enemy", 2, new Vector3(1f, 0f, 0f));
                AbilityUnitBehaviour bystander = rig.AddUnit("bystander", 2, new Vector3(2f, 0f, 0f));
                AbilityBlueprint strike = new AbilityBlueprint { Id = "strike", Targeting = TargetingMode.Unit };
                strike.ImpactEffects.Add(AbilityTestSupport.DamageNode(12f));
                AbilityBlueprint poke = new AbilityBlueprint { Id = "poke", Targeting = TargetingMode.Unit };
                poke.ImpactEffects.Add(AbilityTestSupport.DamageNode(3f));
                rig.System.RegisterAbility(strike);
                rig.System.RegisterAbility(poke);
                rig.System.GrantAbility(player.UnitId, "strike");
                rig.System.GrantAbility(player.UnitId, "poke");

                AbilityEventListener listener = rig.Own(new GameObject("Listener")).AddComponent<AbilityEventListener>();
                listener.EventId = AbilityEvents.TakeDamage;
                listener.AbilityId = "strike";
                listener.Unit = enemy;
                int events = 0;
                int codeEvents = 0;
                float amount = 0f;
                GameObject unitObject = null;
                GameObject otherObject = null;
                string ability = null;
                listener.OnEvent.AddListener(() => events++);
                listener.OnAmount.AddListener(value => amount = value);
                listener.OnUnitObject.AddListener(value => unitObject = value);
                listener.OnOtherObject.AddListener(value => otherObject = value);
                listener.OnAbilityId.AddListener(value => ability = value);
                listener.Raised += _ => codeEvents++;
                listener.Subscribe();

                rig.System.Cast(CastRequest.AtUnit(player.UnitId, "poke", enemy.UnitId));
                rig.System.Cast(CastRequest.AtUnit(player.UnitId, "strike", bystander.UnitId));
                Assert.That(events, Is.Zero, "Another ability and another unit are filtered out.");

                rig.System.Cast(CastRequest.AtUnit(player.UnitId, "strike", enemy.UnitId));
                Assert.That(events, Is.EqualTo(1));
                Assert.That(codeEvents, Is.EqualTo(1));
                Assert.That(amount, Is.EqualTo(12f));
                Assert.That(unitObject, Is.SameAs(enemy.gameObject));
                Assert.That(otherObject, Is.SameAs(player.gameObject));
                Assert.That(ability, Is.EqualTo("strike"));

                AbilitySceneRig.CallPrivate(listener, "OnDisable");
                rig.System.Cast(CastRequest.AtUnit(player.UnitId, "strike", enemy.UnitId));
                Assert.That(events, Is.EqualTo(1), "A disabled listener is unsubscribed.");
            }
        }

        [Test]
        public void Summon_TakesTheOwnersTeamAndLevel_HandsOverTheOwner_AndKeepsTheCap()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour owner = rig.AddUnit("owner", 1, Vector3.zero);
                owner.SetLevel(4);
                AbilityUnitBehaviour firstUnit = rig.AddUnit("wolf A", 2, new Vector3(1f, 0f, 0f));
                AbilityUnitBehaviour secondUnit = rig.AddUnit("wolf B", 2, new Vector3(2f, 0f, 0f));
                AbilitySummonBehaviour first = firstUnit.gameObject.AddComponent<AbilitySummonBehaviour>();
                AbilitySummonBehaviour second = secondUnit.gameObject.AddComponent<AbilitySummonBehaviour>();
                first.MaxPerOwner = 1;
                second.MaxPerOwner = 1;
                Transform handedOwner = null;
                int dismissed = 0;
                first.OnOwnerAssigned.AddListener(value => handedOwner = value);
                first.OnDismissed.AddListener(() => dismissed++);
                SpawnRequest request = new SpawnRequest("wolf", owner.UnitId, Vector3.zero, Vector3.zero, UnitId.None,
                    "summon_wolf", 0f);

                first.OnSpawned(request, rig.Hub);
                Assert.That(firstUnit.Unit.Team, Is.EqualTo(owner.Unit.Team), "The summon fights on the owner's side.");
                Assert.That(firstUnit.Unit.Level, Is.EqualTo(4));
                Assert.That(first.Owner, Is.SameAs(owner));
                Assert.That(first.SourceAbilityId, Is.EqualTo("summon_wolf"));
                Assert.That(handedOwner, Is.SameAs(owner.transform));
                Assert.That(first.IsSummoned, Is.True);

                second.OnSpawned(request, rig.Hub);
                Assert.That(dismissed, Is.EqualTo(1), "A new summon over the cap dismisses the oldest.");
                Assert.That(second.IsSummoned, Is.True);
            }
        }
    }
}
