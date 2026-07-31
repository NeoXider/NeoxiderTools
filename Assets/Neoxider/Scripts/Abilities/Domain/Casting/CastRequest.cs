using System;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     A serializable cast intent: caster, ability, target, deterministic seed.
    ///     Safe to send over the network — contains only ids and value types.
    /// </summary>
    [Serializable]
    public struct CastRequest
    {
        public UnitId Caster;
        public string AbilityId;
        public UnitId TargetUnit;
        public Vector3 TargetPoint;
        public Vector3 Direction;
        public uint Seed;

        public static CastRequest NoTarget(UnitId caster, string abilityId, uint seed = 0)
        {
            return new CastRequest { Caster = caster, AbilityId = abilityId, Seed = seed };
        }

        public static CastRequest AtUnit(UnitId caster, string abilityId, UnitId target, uint seed = 0)
        {
            return new CastRequest { Caster = caster, AbilityId = abilityId, TargetUnit = target, Seed = seed };
        }

        public static CastRequest AtPoint(UnitId caster, string abilityId, Vector3 point, uint seed = 0)
        {
            return new CastRequest { Caster = caster, AbilityId = abilityId, TargetPoint = point, Seed = seed };
        }

        public static CastRequest Towards(UnitId caster, string abilityId, Vector3 direction, uint seed = 0)
        {
            return new CastRequest { Caster = caster, AbilityId = abilityId, Direction = direction, Seed = seed };
        }
    }
}
