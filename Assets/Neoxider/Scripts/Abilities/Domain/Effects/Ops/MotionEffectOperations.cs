using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Shared helpers for the motion family (knockback / pull / teleport). Displacement goes through
    ///     the world adapter's <see cref="IAbilityWorldAdapter.TryMoveUnit" /> seam, so the pure domain
    ///     never touches transforms and headless callers (NullWorldAdapter) safely no-op.
    /// </summary>
    internal static class MotionSupport
    {
        /// <summary>Squared distance below which a push/pull has no usable direction (units coincide).</summary>
        public const float MinSeparationSqr = 1e-6f;

        /// <summary>A unit can be displaced when it is alive and neither Unmovable nor Invulnerable.</summary>
        public static bool CanMove(AbilityUnit unit)
        {
            return unit != null && unit.IsAlive &&
                   !unit.HasState(AbilityStates.Unmovable) &&
                   !unit.HasState(AbilityStates.Invulnerable);
        }

        /// <summary>Moves a unit to <paramref name="destination" /> when it is movable. Returns true on success.</summary>
        public static bool TryMove(EffectContext context, UnitId unitId, Vector3 destination)
        {
            AbilityUnit unit = context.System.GetUnit(unitId);
            return CanMove(unit) && context.System.World.TryMoveUnit(unitId, destination);
        }
    }

    /// <summary>
    ///     Built-in "knockback" op: pushes each target away from the caster (or from the target point
    ///     when the cast has one) by the resolved <see cref="EffectNodeData.Amount" /> distance.
    /// </summary>
    public sealed class KnockbackEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.Knockback;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            if (targets.Count == 0)
            {
                return;
            }

            IAbilityWorldAdapter world = context.System.World;
            Vector3 origin;
            if (context.HasTargetPoint)
            {
                origin = context.TargetPoint;
            }
            else if (!world.TryGetPosition(context.Caster, out origin))
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                UnitId t = targets[i];
                if (!world.TryGetPosition(t, out Vector3 pos) || !MotionSupport.CanMove(context.System.GetUnit(t)))
                {
                    continue;
                }

                float distance = LeveledValueResolver.ResolveAmount(node, context, t);
                if (distance <= 0f)
                {
                    continue;
                }

                Vector3 direction = pos - origin;
                if (direction.sqrMagnitude < MotionSupport.MinSeparationSqr)
                {
                    continue; // WHY: target sits on the origin — no push direction.
                }

                world.TryMoveUnit(t, pos + direction.normalized * distance);
            }
        }
    }

    /// <summary>
    ///     Built-in "pull" op: drags each target toward the caster by the resolved
    ///     <see cref="EffectNodeData.Amount" /> distance, never overshooting past the caster.
    /// </summary>
    public sealed class PullEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.Pull;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            if (targets.Count == 0)
            {
                return;
            }

            IAbilityWorldAdapter world = context.System.World;
            if (!world.TryGetPosition(context.Caster, out Vector3 anchor))
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                UnitId t = targets[i];
                if (t == context.Caster || !world.TryGetPosition(t, out Vector3 pos) ||
                    !MotionSupport.CanMove(context.System.GetUnit(t)))
                {
                    continue;
                }

                float distance = LeveledValueResolver.ResolveAmount(node, context, t);
                if (distance <= 0f)
                {
                    continue;
                }

                Vector3 delta = anchor - pos;
                float separation = delta.magnitude;
                if (separation < Mathf.Sqrt(MotionSupport.MinSeparationSqr))
                {
                    continue; // WHY: already at the caster.
                }

                float move = Mathf.Min(distance, separation); // WHY: never overshoot past the caster.
                world.TryMoveUnit(t, pos + delta / separation * move);
            }
        }
    }

    /// <summary>
    ///     Built-in "teleport" op. Behaviour depends on the node's selector:
    ///     <list type="bullet">
    ///         <item>Caster selector ⇒ blink the caster to the target point (or the first target's position).</item>
    ///         <item>Target selector ⇒ pull each target to the caster's position.</item>
    ///     </list>
    ///     With <c>CustomParam = "swap"</c> the caster and the first primary target trade positions.
    /// </summary>
    public sealed class TeleportEffectOperation : IEffectOperation
    {
        public string Id => AbilityEffectOps.Teleport;

        public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
        {
            IAbilityWorldAdapter world = context.System.World;

            if (string.Equals(node.CustomParam, "swap", StringComparison.OrdinalIgnoreCase))
            {
                UnitId other = context.PrimaryTargets.Count > 0
                    ? context.PrimaryTargets[0]
                    : targets.Count > 0 ? targets[0] : UnitId.None;
                if (!other.IsValid || other == context.Caster)
                {
                    return;
                }

                if (world.TryGetPosition(context.Caster, out Vector3 casterPos) &&
                    world.TryGetPosition(other, out Vector3 otherPos))
                {
                    MotionSupport.TryMove(context, context.Caster, otherPos);
                    MotionSupport.TryMove(context, other, casterPos);
                }

                return;
            }

            if (node.Target == EffectTargetSelector.Caster)
            {
                Vector3 destination;
                if (context.HasTargetPoint)
                {
                    destination = context.TargetPoint;
                }
                else if (context.PrimaryTargets.Count == 0 ||
                         !world.TryGetPosition(context.PrimaryTargets[0], out destination))
                {
                    return;
                }

                MotionSupport.TryMove(context, context.Caster, destination);
                return;
            }

            if (!world.TryGetPosition(context.Caster, out Vector3 anchor))
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != context.Caster)
                {
                    MotionSupport.TryMove(context, targets[i], anchor);
                }
            }
        }
    }
}
