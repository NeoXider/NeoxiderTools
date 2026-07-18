using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Abilities;
using Neo.Core.Resources;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Shared helpers for the Neo.Abilities EditMode tests: unit factories that wire resource pools
    ///     exactly like the production <c>UnitTemplate</c> (MaxDecrease/Increase = -1), an in-memory world
    ///     adapter (positions + radius queries + spawn capture) and an event recorder. Pure C#; no scene.
    /// </summary>
    internal static class AbilityTestSupport
    {
        /// <summary>Default health used by combat helpers; large enough not to die by accident.</summary>
        public const float DefaultHealth = 1000f;

        public static AbilityUnit CreateUnit(AbilitySystem system, int team = 1, float health = DefaultHealth,
            float healthCurrent = -1f, float mana = 0f, string name = null)
        {
            AbilityUnit unit = system.CreateUnit(new TeamId(team), name);
            if (health > 0f)
            {
                AddPool(unit, AbilityResourceIds.Health, health, healthCurrent);
            }

            if (mana > 0f)
            {
                AddPool(unit, AbilityResourceIds.Mana, mana);
            }

            return unit;
        }

        /// <summary>Adds a pool configured like production: no per-step decrease/increase clamp.</summary>
        public static void AddPool(AbilityUnit unit, string resourceId, float max, float current = -1f)
        {
            unit.Resources.AddPool(resourceId, new ResourcePoolEntry
            {
                Current = current < 0f ? max : current,
                Max = max,
                MaxDecreaseAmount = -1f,
                MaxIncreaseAmount = -1f
            });
        }

        public static ModifierBlueprint Modifier(string id, float duration = 0f,
            ModifierStackPolicy policy = ModifierStackPolicy.Refresh)
        {
            return new ModifierBlueprint
            {
                Id = id,
                Duration = duration,
                StackPolicy = policy
            };
        }

        public static ModifierBlueprint WithProperty(this ModifierBlueprint bp, string propertyId,
            PropertyOp op, float value, float perStack = 0f)
        {
            bp.Properties.Add(new PropertyContribution(propertyId, op, value, perStack));
            return bp;
        }

        public static ModifierBlueprint WithState(this ModifierBlueprint bp, string state)
        {
            bp.States.Add(state);
            return bp;
        }

        public static EffectNodeData DamageNode(float amount, string damageType = AbilityDamageTypes.Pure,
            EffectTargetSelector target = EffectTargetSelector.Target)
        {
            return new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage,
                Target = target,
                Amount = amount,
                DamageType = damageType
            };
        }

        public static EffectNodeData HealNode(float amount, EffectTargetSelector target = EffectTargetSelector.Target)
        {
            return new EffectNodeData { OpId = AbilityEffectOps.Heal, Target = target, Amount = amount };
        }

        public static List<EffectNodeData> Nodes(params EffectNodeData[] nodes)
        {
            return new List<EffectNodeData>(nodes);
        }

        public static EffectContext Context(AbilitySystem system, UnitId caster, uint seed = 1,
            params UnitId[] targets)
        {
            var ctx = new EffectContext(system, caster, "test_ability", new XorShiftRandom(seed));
            for (int i = 0; i < targets.Length; i++)
            {
                ctx.PrimaryTargets.Add(targets[i]);
            }

            return ctx;
        }

        public static List<ModifierInstance> ActiveModifiers(AbilitySystem system, UnitId owner)
        {
            var list = new List<ModifierInstance>();
            system.Modifiers.GetModifiers(owner, list);
            return list;
        }
    }

    /// <summary>In-memory world seam: unit positions, radius queries, captured spawn requests.</summary>
    internal sealed class FakeWorldAdapter : IAbilityWorldAdapter
    {
        public readonly Dictionary<UnitId, Vector3> Positions = new Dictionary<UnitId, Vector3>();
        public readonly List<SpawnRequest> Spawns = new List<SpawnRequest>();

        public void SetPosition(UnitId unit, Vector3 position)
        {
            Positions[unit] = position;
        }

        public bool TryGetPosition(UnitId unit, out Vector3 position)
        {
            return Positions.TryGetValue(unit, out position);
        }

        public void QueryUnitsInRadius(Vector3 point, float radius, List<UnitId> results)
        {
            foreach (KeyValuePair<UnitId, Vector3> pair in Positions)
            {
                if ((pair.Value - point).magnitude <= radius)
                {
                    results.Add(pair.Key);
                }
            }
        }

        public bool TryMoveUnit(UnitId unit, Vector3 newPosition)
        {
            if (!Positions.ContainsKey(unit))
            {
                return false;
            }

            Positions[unit] = newPosition;
            return true;
        }

        public void RequestSpawn(SpawnRequest request)
        {
            Spawns.Add(request);
        }
    }

    /// <summary>
    ///     Scene-side rig for behaviour tests: owns a hub and unit GameObjects, drives the EditMode
    ///     lifecycle manually (OnEnable/OnDisable via reflection — EditMode has no lifecycle), and
    ///     tears everything down deterministically.
    /// </summary>
    internal sealed class AbilitySceneRig : IDisposable
    {
        private static readonly FieldInfo InstanceField = typeof(AbilitySystemBehaviour).GetField(
            "_instance", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly List<GameObject> _owned = new List<GameObject>();
        private readonly AbilitySystemBehaviour _previousInstance;

        public AbilitySceneRig()
        {
            Hub = Own(new GameObject("AbilityHubUnderTest")).AddComponent<AbilitySystemBehaviour>();
            // WHY: EditMode never runs Awake, so the singleton is unset and units would resolve the hub
            // via FindFirstObjectByType — which can pick a hub from the currently open user scene.
            _previousInstance = AbilitySystemBehaviour.InstanceOrNull;
            InstanceField?.SetValue(null, Hub);
        }

        public AbilitySystemBehaviour Hub { get; }

        public AbilitySystem System => Hub.System;

        public GameObject Own(GameObject go)
        {
            _owned.Add(go);
            return go;
        }

        public AbilityUnitBehaviour AddUnit(string name, int team, Vector3 position, float health = 100f)
        {
            GameObject go = Own(new GameObject(name));
            go.transform.position = position;
            var behaviour = go.AddComponent<AbilityUnitBehaviour>();
            behaviour.SetTeamOverride(team);
            CallPrivate(behaviour, "OnEnable");
            if (behaviour.Unit != null && health > 0f)
            {
                AbilityTestSupport.AddPool(behaviour.Unit, AbilityResourceIds.Health, health);
            }

            return behaviour;
        }

        public static void CallPrivate(Component target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            method?.Invoke(target, null);
        }

        public void Dispose()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                GameObject go = _owned[i];
                if (go == null)
                {
                    continue;
                }

                var unitBehaviour = go.GetComponent<AbilityUnitBehaviour>();
                if (unitBehaviour != null && unitBehaviour.Unit != null)
                {
                    CallPrivate(unitBehaviour, "OnDisable");
                }

                UnityEngine.Object.DestroyImmediate(go);
            }

            _owned.Clear();
            // WHY: Restore whatever singleton existed before the rig so user-scene hubs keep working.
            InstanceField?.SetValue(null, _previousInstance);
        }
    }

    /// <summary>Records every published event so tests can assert counts, order and payloads.</summary>
    internal sealed class EventLog
    {
        public readonly List<AbilityEventArgs> Events = new List<AbilityEventArgs>();

        public EventLog(AbilitySystem system)
        {
            system.Events.SubscribeAny(Events.Add);
        }

        public int Count(string eventId)
        {
            int n = 0;
            for (int i = 0; i < Events.Count; i++)
            {
                if (Events[i].EventId == eventId)
                {
                    n++;
                }
            }

            return n;
        }

        public bool Any(string eventId)
        {
            return Count(eventId) > 0;
        }

        public bool TryGetLast(string eventId, out AbilityEventArgs args)
        {
            for (int i = Events.Count - 1; i >= 0; i--)
            {
                if (Events[i].EventId == eventId)
                {
                    args = Events[i];
                    return true;
                }
            }

            args = default;
            return false;
        }
    }
}
