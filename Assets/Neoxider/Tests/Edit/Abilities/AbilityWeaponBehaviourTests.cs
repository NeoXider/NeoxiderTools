using Neo.Abilities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Component features behind distinct weapon behaviours: an orbiting projectile, the auto-caster target pick,
    ///     the NoCode level-up action, the ability level binding and presentation cues.
    /// </summary>
    public sealed class AbilityWeaponBehaviourTests
    {
        [Test]
        public void OrbitProjectile_StaysOnItsRing_HitsThere_AndOutlivesItsHits()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour owner = rig.AddUnit("Owner", 1, new Vector3(3f, 1f, 0f));
                AbilityUnitBehaviour onRing = rig.AddUnit("OnRing", 2, new Vector3(5f, 1f, 0f));
                AbilityUnitBehaviour far = rig.AddUnit("Far", 2, new Vector3(9f, 1f, 0f));
                AbilityBlueprint blades = new AbilityBlueprint
                {
                    Id = "blades",
                    Targeting = TargetingMode.NoTarget,
                    Delivery = AbilityDeliveryType.Projectile,
                    ProjectileArchetypeId = "unbound_blades"
                };
                blades.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
                rig.System.RegisterAbility(blades);
                rig.System.GrantAbility(owner.UnitId, "blades");
                CastResult cast = rig.System.Cast(CastRequest.NoTarget(owner.UnitId, "blades"));
                Assert.That(cast.Success, Is.True);

                AbilityProjectileBehaviour projectile = rig.Own(new GameObject("Blades"))
                    .AddComponent<AbilityProjectileBehaviour>();
                projectile.Motion = AbilityProjectileBehaviour.ProjectileMotion.Orbit;
                projectile.Plane = AbilityZonePlane.ScreenXY;
                projectile.OrbitRadius = 2f;
                // WHY: a still ring keeps the hit position exact whatever Time.deltaTime is in Edit Mode.
                projectile.OrbitDegreesPerSecond = 0f;
                projectile.OnSpawned(new SpawnRequest("unbound_blades", owner.UnitId, owner.transform.position,
                    Vector3.zero, UnitId.None, "blades", 0f, cast.CastId), rig.Hub);

                AbilitySceneRig.CallPrivate(projectile, "Update");
                Assert.That(Vector3.Distance(projectile.transform.position, owner.transform.position),
                    Is.EqualTo(2f).Within(0.001f));
                Assert.That(onRing.Unit.Health, Is.EqualTo(90f));
                Assert.That(far.Unit.Health, Is.EqualTo(100f));
                Assert.That(projectile.IsFlying, Is.True, "An orbiting projectile is not consumed by a hit.");

                AbilitySceneRig.CallPrivate(projectile, "Update");
                Assert.That(onRing.Unit.Health, Is.EqualTo(90f),
                    "Without a rehit interval the same unit is hit once.");

                owner.transform.position = new Vector3(-4f, 0f, 0f);
                AbilitySceneRig.CallPrivate(projectile, "Update");
                Assert.That(Vector3.Distance(projectile.transform.position, owner.transform.position),
                    Is.EqualTo(2f).Within(0.001f), "The ring follows its owner.");
            }
        }

        [Test]
        public void AutoCaster_TargetPick_AimsAtTheFarthestTheNearestOrAnyValidUnit()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour player = rig.AddUnit("player", 1, Vector3.zero);
                player.gameObject.AddComponent<AbilityCasterBehaviour>();
                AbilityAutoCaster autoCaster = player.gameObject.AddComponent<AbilityAutoCaster>();
                AbilityUnitBehaviour near = rig.AddUnit("near", 2, new Vector3(2f, 0f, 0f));
                AbilityUnitBehaviour far = rig.AddUnit("far", 2, new Vector3(6f, 0f, 0f));
                AbilityBlueprint zap = new AbilityBlueprint { Id = "zap", Targeting = TargetingMode.Unit, Range = 10f };
                zap.ImpactEffects.Add(AbilityTestSupport.DamageNode(1f));
                rig.System.RegisterAbility(zap);
                rig.System.GrantAbility(player.UnitId, "zap");

                autoCaster.TargetPick = AbilityAutoCaster.AutoCastTargetPick.Farthest;
                autoCaster.CastReadyAbilities();
                Assert.That(far.Unit.Health, Is.EqualTo(99f));
                Assert.That(near.Unit.Health, Is.EqualTo(100f));

                autoCaster.TargetPick = AbilityAutoCaster.AutoCastTargetPick.Nearest;
                autoCaster.CastReadyAbilities();
                Assert.That(near.Unit.Health, Is.EqualTo(99f));
                Assert.That(far.Unit.Health, Is.EqualTo(99f));

                autoCaster.TargetPick = AbilityAutoCaster.AutoCastTargetPick.Random;
                for (int i = 0; i < 40; i++)
                {
                    autoCaster.CastReadyAbilities();
                }

                Assert.That(near.Unit.Health + far.Unit.Health, Is.EqualTo(158f), "Every pass casts once.");
                Assert.That(near.Unit.Health, Is.LessThan(99f), "Random reaches the near unit.");
                Assert.That(far.Unit.Health, Is.LessThan(99f), "Random reaches the far unit.");
            }
        }

        [Test]
        public void LevelUpAbility_GrantsFirst_ThenRaisesTheLevelUpToTheCap()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour player = rig.AddUnit("player", 1, Vector3.zero);
                player.gameObject.AddComponent<AbilityCasterBehaviour>();
                AbilityCooldownSource source = player.gameObject.AddComponent<AbilityCooldownSource>();
                source.AbilityId = "zap";
                rig.System.RegisterAbility(new AbilityBlueprint { Id = "zap", Targeting = TargetingMode.NoTarget });

                AbilityNoCodeAction action = rig.Own(new GameObject("bridge")).AddComponent<AbilityNoCodeAction>();
                SerializedObject serialized = new SerializedObject(action);
                serialized.FindProperty("_actionType").enumValueIndex = (int)AbilityNoCodeActionType.LevelUpAbility;
                serialized.FindProperty("_abilityId").stringValue = "zap";
                serialized.FindProperty("_unit").objectReferenceValue = player;
                serialized.FindProperty("_maxLevel").intValue = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                int failed = 0;
                action.OnFailed.AddListener(_ => failed++);

                Assert.That(source.Level, Is.Zero, "Not granted yet.");
                Assert.That(source.IsGranted, Is.False);
                Assert.That(source.NextLevel, Is.EqualTo(1), "The first pick gives level 1.");

                action.Execute();
                Assert.That(source.IsGranted, Is.True);
                Assert.That(source.Level, Is.EqualTo(1), "The first pick grants the ability.");
                Assert.That(source.NextLevel, Is.EqualTo(2));

                action.Execute();
                action.Execute();
                Assert.That(source.Level, Is.EqualTo(3));
                Assert.That(failed, Is.Zero);

                action.Execute();
                Assert.That(source.Level, Is.EqualTo(3), "The cap holds.");
                Assert.That(failed, Is.EqualTo(1));
            }
        }

        [Test]
        public void HomingProjectile_WhoseTargetIsGoneAtLaunch_FliesAlongTheLaunchDirectionIn2D()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour owner = rig.AddUnit("Owner", 1, Vector3.zero);
                AbilityProjectileBehaviour projectile = rig.Own(new GameObject("Bolt"))
                    .AddComponent<AbilityProjectileBehaviour>();
                projectile.Plane = AbilityZonePlane.ScreenXY;
                // WHY: the target another weapon killed in the same frame no longer resolves to a position.
                UnitId gone = new UnitId(987654u);
                projectile.OnSpawned(new SpawnRequest("bolt", owner.UnitId, Vector3.zero, Vector3.up * 4f, gone,
                    "bolt", 10f), rig.Hub);

                AbilitySceneRig.CallPrivate(projectile, "Update");

                Vector3 position = projectile.transform.position;
                Assert.That(projectile.IsFlying, Is.True);
                Assert.That(position.z, Is.EqualTo(0f).Within(0.0001f), "A 2D projectile never leaves its plane.");
                Assert.That(position.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(position.y, Is.GreaterThanOrEqualTo(0f));
                Assert.That(projectile.transform.rotation.eulerAngles.z, Is.EqualTo(90f).Within(0.01f),
                    "It faces and follows the launch direction instead of hanging in place.");
            }
        }

        [Test]
        public void SecondChainNode_SwitchedOnByChanceByLevel_AddsABoltAtTheNearestFoeFromThatLevel()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour caster = rig.AddUnit("Caster", 1, Vector3.zero);
                AbilityUnitBehaviour aimed = rig.AddUnit("Aimed", 2, new Vector3(8f, 0f, 0f));
                AbilityUnitBehaviour near = rig.AddUnit("Near", 2, new Vector3(1f, 0f, 0f));
                AbilityBlueprint lightning = new AbilityBlueprint
                {
                    Id = "lightning",
                    Targeting = TargetingMode.Unit,
                    TeamFilter = AbilityTeamFilter.Enemies,
                    Range = 20f
                };
                lightning.ImpactEffects.Add(new EffectNodeData
                {
                    OpId = AbilityEffectOps.Chain,
                    Target = EffectTargetSelector.Target,
                    TeamFilter = AbilityTeamFilter.Enemies,
                    Radius = 3f,
                    MaxTargets = 3,
                    Amount = 10f,
                    DamageType = AbilityDamageTypes.Pure
                });
                // WHY: the extra bolt of a leveled weapon is a second node whose chance is 0 below its level.
                lightning.ImpactEffects.Add(new EffectNodeData
                {
                    OpId = AbilityEffectOps.Chain,
                    Target = EffectTargetSelector.AreaAroundCaster,
                    TeamFilter = AbilityTeamFilter.Enemies,
                    Radius = 3f,
                    MaxTargets = 3,
                    Amount = 10f,
                    DamageType = AbilityDamageTypes.Pure,
                    ChanceByLevel = new[] { 0f, 0f, 1f }
                });
                rig.System.RegisterAbility(lightning);
                rig.System.GrantAbility(caster.UnitId, "lightning");

                Assert.That(rig.System.Cast(CastRequest.AtUnit(caster.UnitId, "lightning", aimed.UnitId)).Success,
                    Is.True);
                Assert.That(aimed.Unit.Health, Is.EqualTo(90f));
                Assert.That(near.Unit.Health, Is.EqualTo(100f), "Below level 3 there is one bolt.");

                rig.System.SetAbilityLevel(caster.UnitId, "lightning", 3);
                Assert.That(rig.System.Cast(CastRequest.AtUnit(caster.UnitId, "lightning", aimed.UnitId)).Success,
                    Is.True);
                Assert.That(aimed.Unit.Health, Is.EqualTo(80f));
                Assert.That(near.Unit.Health, Is.EqualTo(90f), "From level 3 a second bolt strikes the nearest foe.");
            }
        }

        [Test]
        public void CuePlayer_PlaysTheCastCueOnce_AndTheImpactCueForEveryDamagedUnit()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour player = rig.AddUnit("player", 1, Vector3.zero);
                rig.AddUnit("first", 2, new Vector3(1f, 0f, 0f));
                rig.AddUnit("second", 2, new Vector3(0f, 2f, 0f));
                rig.AddUnit("outside", 2, new Vector3(20f, 0f, 0f));

                AbilityCuePlayer cues = rig.Own(new GameObject("Cues")).AddComponent<AbilityCuePlayer>();
                cues.SetCue("cast_fx", null);
                cues.SetCue("hit_fx", null);
                int castPlays = 0;
                int hitPlays = 0;
                cues.Cues[0].OnPlayed.AddListener(() => castPlays++);
                cues.Cues[1].OnPlayed.AddListener(() => hitPlays++);
                cues.Subscribe();

                AbilityBlueprint burst = new AbilityBlueprint
                {
                    Id = "burst",
                    Targeting = TargetingMode.NoTarget,
                    CastCue = "cast_fx",
                    ImpactCue = "hit_fx"
                };
                burst.ImpactEffects.Add(new EffectNodeData
                {
                    OpId = AbilityEffectOps.Damage,
                    Target = EffectTargetSelector.AreaAroundCaster,
                    Radius = 5f,
                    Amount = 5f,
                    DamageType = AbilityDamageTypes.Pure
                });
                rig.System.RegisterAbility(burst);
                rig.System.GrantAbility(player.UnitId, "burst");

                Assert.That(rig.System.Cast(CastRequest.NoTarget(player.UnitId, "burst")).Success, Is.True);
                Assert.That(castPlays, Is.EqualTo(1));
                Assert.That(hitPlays, Is.EqualTo(2));
                Assert.That(cues.PlayAt("hit_fx", Vector3.one), Is.True);
                Assert.That(cues.PlayAt("unknown", Vector3.one), Is.False);
                Assert.That(hitPlays, Is.EqualTo(3));

                AbilitySceneRig.CallPrivate(cues, "OnDisable");
                rig.System.Cast(CastRequest.NoTarget(player.UnitId, "burst"));
                Assert.That(castPlays, Is.EqualTo(1), "A disabled player is unsubscribed.");
            }
        }
    }
}
