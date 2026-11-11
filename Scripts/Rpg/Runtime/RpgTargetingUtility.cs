using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Neo.Rpg
{
    internal static class RpgTargetingUtility
    {
        private sealed class Scratch
        {
            internal readonly RpgPhysicsQueryBuffers Buffers = new();
            internal readonly List<GameObject> Candidates = new();
            internal readonly List<IRpgCombatReceiver> Receivers = new();
            internal readonly HashSet<IRpgCombatReceiver> Seen = new();

            internal void Clear()
            {
                Buffers.Release();
                Candidates.Clear();
                Receivers.Clear();
                Seen.Clear();
            }
        }

        // WHY: shared main-thread scratch keeps selection allocation-free; a re-entrant selection (receiver getters
        // calling back into targeting) rents a private instance instead of corrupting the outer pass.
        private static Scratch _shared;
        private static bool _sharedInUse;

        internal static GameObject SelectTarget(Transform sourceTransform, RpgTargetQuery query,
            Func<GameObject, IRpgCombatReceiver> resolveReceiver, IRpgCombatReceiver sourceReceiver = null)
        {
            if (sourceTransform == null || query == null || resolveReceiver == null)
            {
                return null;
            }

            bool pooled = !_sharedInUse;
            Scratch scratch = pooled ? _shared ??= new Scratch() : new Scratch();
            if (pooled)
            {
                _sharedInUse = true;
            }

            try
            {
                return SelectTarget(scratch, sourceTransform, query, resolveReceiver, sourceReceiver);
            }
            finally
            {
                scratch.Clear();
                if (pooled)
                {
                    _sharedInUse = false;
                }
            }
        }

        private static GameObject SelectTarget(Scratch scratch, Transform sourceTransform, RpgTargetQuery query,
            Func<GameObject, IRpgCombatReceiver> resolveReceiver, IRpgCombatReceiver sourceReceiver)
        {
            sourceReceiver ??= resolveReceiver(sourceTransform.gameObject);
            Vector3 position = sourceTransform.position;
            float range = query.Range;

            if (query.Use3D)
            {
                int count = scratch.Buffers.OverlapSphere(position, range, query.TargetLayers);
                Collider[] colliders = scratch.Buffers.Colliders3D;
                for (int i = 0; i < count; i++)
                {
                    if (colliders[i] != null)
                    {
                        AddCandidate(scratch, colliders[i].gameObject, sourceReceiver, query, resolveReceiver);
                    }
                }
            }

            if (query.Use2D)
            {
                int count = scratch.Buffers.OverlapCircle2D(position, range, query.TargetLayers);
                List<Collider2D> colliders2D = scratch.Buffers.Colliders2D;
                for (int i = 0; i < count && i < colliders2D.Count; i++)
                {
                    if (colliders2D[i] != null)
                    {
                        AddCandidate(scratch, colliders2D[i].gameObject, sourceReceiver, query, resolveReceiver);
                    }
                }
            }

            List<GameObject> candidates = scratch.Candidates;
            if (candidates.Count == 0)
            {
                return null;
            }

            if (query.SelectionMode == RpgTargetSelectionMode.Random)
            {
                return candidates[Random.Range(0, candidates.Count)];
            }

            List<IRpgCombatReceiver> receivers = scratch.Receivers;
            GameObject best = candidates[0];
            float bestScore = Score(candidates[0], receivers[0], position, query.SelectionMode);
            for (int i = 1; i < candidates.Count; i++)
            {
                float score = Score(candidates[i], receivers[i], position, query.SelectionMode);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidates[i];
                }
            }

            return best;
        }

        private static void AddCandidate(Scratch scratch, GameObject candidate, IRpgCombatReceiver sourceReceiver,
            RpgTargetQuery query, Func<GameObject, IRpgCombatReceiver> resolveReceiver)
        {
            if (candidate == null)
            {
                return;
            }

            IRpgCombatReceiver receiver = resolveReceiver(candidate);
            if (receiver == null || query.IgnoreSelf && ReferenceEquals(receiver, sourceReceiver))
            {
                return;
            }

            if (!query.IncludeDeadTargets && receiver.IsDead)
            {
                return;
            }

            if (query.RequireCanPerformActions && !receiver.CanPerformActions)
            {
                return;
            }

            // WHY: one candidate per receiver; extra colliders of the same actor (hurtboxes, 2D+3D) are ignored.
            if (scratch.Seen.Add(receiver))
            {
                scratch.Candidates.Add(candidate);
                scratch.Receivers.Add(receiver);
            }
        }

        private static float Score(GameObject candidate, IRpgCombatReceiver receiver, Vector3 sourcePosition,
            RpgTargetSelectionMode selectionMode)
        {
            float distance = Vector3.Distance(sourcePosition, candidate.transform.position);
            return selectionMode switch
            {
                RpgTargetSelectionMode.Nearest => -distance,
                RpgTargetSelectionMode.Farthest => distance,
                RpgTargetSelectionMode.LowestCurrentHp => -receiver.CurrentHp,
                RpgTargetSelectionMode.HighestCurrentHp => receiver.CurrentHp,
                RpgTargetSelectionMode.LowestHpPercent => -(receiver.MaxHp > 0f
                    ? receiver.CurrentHp / receiver.MaxHp
                    : 0f),
                RpgTargetSelectionMode.HighestLevel => receiver.Level,
                _ => -distance
            };
        }
    }
}
