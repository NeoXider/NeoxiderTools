using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Neo.Rpg
{
    internal static class RpgTargetingUtility
    {
        internal static GameObject SelectTarget(Transform sourceTransform, RpgTargetQuery query,
            Func<GameObject, IRpgCombatReceiver> resolveReceiver)
        {
            if (sourceTransform == null || query == null || resolveReceiver == null)
            {
                return null;
            }

            var candidates = new List<GameObject>();
            Vector3 position = sourceTransform.position;
            float range = query.Range;

            if (query.Use3D)
            {
                Collider[] colliders = Physics.OverlapSphere(position, range, query.TargetLayers);
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                    {
                        AddCandidate(candidates, colliders[i].gameObject, sourceTransform, query, resolveReceiver);
                    }
                }
            }

            if (query.Use2D)
            {
                Collider2D[] colliders2D = Physics2D.OverlapCircleAll(position, range, query.TargetLayers);
                for (int i = 0; i < colliders2D.Length; i++)
                {
                    if (colliders2D[i] != null)
                    {
                        AddCandidate(candidates, colliders2D[i].gameObject, sourceTransform, query, resolveReceiver);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            if (query.SelectionMode == RpgTargetSelectionMode.Random)
            {
                return candidates[Random.Range(0, candidates.Count)];
            }

            GameObject best = candidates[0];
            float bestScore = Score(candidates[0], sourceTransform.position, query.SelectionMode, resolveReceiver);
            for (int i = 1; i < candidates.Count; i++)
            {
                float score = Score(candidates[i], sourceTransform.position, query.SelectionMode, resolveReceiver);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidates[i];
                }
            }

            return best;
        }

        private static void AddCandidate(List<GameObject> candidates,
            GameObject candidate,
            Transform sourceTransform,
            RpgTargetQuery query,
            Func<GameObject, IRpgCombatReceiver> resolveReceiver)
        {
            if (candidate == null || candidates.Contains(candidate))
            {
                return;
            }

            if (query.IgnoreSelf && candidate == sourceTransform.gameObject)
            {
                return;
            }

            IRpgCombatReceiver receiver = resolveReceiver(candidate);
            if (receiver == null)
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

            candidates.Add(candidate);
        }

        private static float Score(GameObject candidate,
            Vector3 sourcePosition,
            RpgTargetSelectionMode selectionMode,
            Func<GameObject, IRpgCombatReceiver> resolveReceiver)
        {
            IRpgCombatReceiver receiver = resolveReceiver(candidate);
            if (receiver == null)
            {
                return float.MinValue;
            }

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
