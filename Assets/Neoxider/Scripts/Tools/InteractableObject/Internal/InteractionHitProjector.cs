using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Which colliders belong to one interaction target and which source hierarchies its rays ignore.
    ///     Every question is answered live: nothing about the hierarchy is remembered between calls.
    /// </summary>
    internal readonly struct InteractionTargetScope
    {
        private readonly Transform _target;
        private readonly Collider _explicit3D;
        private readonly Collider2D _explicit2D;
        private readonly bool _ignoreSourceHierarchies;
        private readonly Transform _distanceSource;
        private readonly Transform _viewSource;

        internal InteractionTargetScope(Transform target, Collider explicit3D, Collider2D explicit2D,
            bool ignoreSourceHierarchies, Transform distanceSource, Transform viewSource)
        {
            _target = target;
            _explicit3D = explicit3D;
            _explicit2D = explicit2D;
            _ignoreSourceHierarchies = ignoreSourceHierarchies;
            _distanceSource = distanceSource;
            _viewSource = viewSource;
        }

        internal Transform Target => _target;
        internal Collider Explicit3D => _explicit3D;
        internal Collider2D Explicit2D => _explicit2D;

        /// <summary>
        ///     True for the explicit target collider, or for any collider in the target hierarchy when no
        ///     explicit collider of that dimension is assigned.
        /// </summary>
        internal bool Contains(Component hitCollider)
        {
            if (hitCollider == null)
            {
                return false;
            }

            if (hitCollider is Collider hitCollider3D)
            {
                if (_explicit3D != null)
                {
                    return hitCollider3D == _explicit3D;
                }

                Transform t = hitCollider3D.transform;
                return t == _target || t.IsChildOf(_target);
            }

            if (hitCollider is Collider2D hitCollider2D)
            {
                if (_explicit2D != null)
                {
                    return hitCollider2D == _explicit2D;
                }

                Transform t2 = hitCollider2D.transform;
                return t2 == _target || t2.IsChildOf(_target);
            }

            return false;
        }

        /// <summary>
        ///     True when source hierarchies are ignored and the collider belongs to the distance or view source rig
        ///     (player capsule, camera rig). Callers ask only about colliders for which <see cref="Contains" /> is
        ///     false, because a target collider is never ignored.
        /// </summary>
        internal bool IsInIgnoredSourceRig(Component nonTargetCollider)
        {
            if (!_ignoreSourceHierarchies || nonTargetCollider == null)
            {
                return false;
            }

            Transform hitTransform = nonTargetCollider.transform;
            return IsSameHierarchy(hitTransform, _distanceSource) || IsSameHierarchy(hitTransform, _viewSource);
        }

        private bool IsSameHierarchy(Transform a, Transform b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            // WHY: A shared level container must not make every target and obstacle part of the player rig.
            return a == b || a.IsChildOf(b) || b.IsChildOf(a) ||
                   (a.root == b.root && a.root != _target.root);
        }
    }

    /// <summary>
    ///     Projects one borrowed raw hit set onto one interaction target. The result in <see cref="Hits" />
    ///     feeds <see cref="InteractionQueryMath" />; buffers are reused, so a warmed projection allocates nothing.
    /// </summary>
    /// <remarks>
    ///     Two exact strategies produce the same selection. The complete projection inspects all H raw hits
    ///     and marks targets, blockers and ignored sources. The indexed projection builds one collider index per
    ///     shared raw entry and looks up this target's colliders; under the clear-path rule it then walks the
    ///     shared distance order only through the hits in front of the nearest target and appends the first
    ///     blocker found. N consumers of one dense ray cost about H + N instead of N x H. The complete projection
    ///     remains for foreign-trigger blocking (range line of sight), for callers that need the nearest
    ///     non-target hit after a miss, for raw sets smaller than <see cref="IndexedMinimumHits" />, where a
    ///     handful of direct checks is cheaper than the index and the live hierarchy walk, and for targets whose
    ///     hierarchy held more colliders than the raw set at their last enumeration (see
    ///     <see cref="UseTargetSizeHint" />).
    /// </remarks>
    internal sealed class InteractionHitProjector
    {
        /// <summary>Raw hit count from which camera and look selection switch to the shared collider index.</summary>
        internal const int DefaultIndexedMinimumHits = 8;

        /// <summary>
        ///     Complete projections chosen by the target-size hint before the hierarchy is enumerated again, so a
        ///     target that lost colliders returns to the index.
        /// </summary>
        internal const int TargetSizeHintLifetime = 64;

        private InteractionRayHit[] _hits = new InteractionRayHit[16];
        private List<Collider> _colliders3D;
        private List<Collider2D> _colliders2D;
        private int[] _selected = Array.Empty<int>();
        private int _targetColliders3D;
        private int _targetColliders2D;
        private int _hintedCompletes3D;
        private int _hintedCompletes2D;

        /// <summary>Projected candidates of the last call; only the returned count is meaningful.</summary>
        internal InteractionRayHit[] Hits => _hits;

        /// <summary>Raw hit count that enables the indexed strategy. Values below one are treated as one.</summary>
        internal int IndexedMinimumHits { get; set; } = DefaultIndexedMinimumHits;

        /// <summary>
        ///     When true, a target whose hierarchy held more colliders than the raw hit count at its last enumeration
        ///     uses the complete projection: H hierarchy checks then cost less than enumerating and looking up every
        ///     target collider. The hint only selects a strategy; both strategies return the same selection.
        /// </summary>
        internal bool UseTargetSizeHint { get; set; } = true;

        /// <summary>
        ///     Projects 3D hits. Triggers were already decided by the native query and are not re-filtered.
        /// </summary>
        /// <param name="foreignTriggersBlock">Foreign trigger volumes count as blockers (range line of sight).</param>
        /// <param name="requireClearPath">The caller will reject a target behind a blocker.</param>
        /// <param name="keepNonTargetsOnMiss">The caller reads the nearest non-target hit when no target is hit.</param>
        internal int Project3D(InteractionPhysicsQueries.Hits3D hits, in InteractionTargetScope scope,
            bool foreignTriggersBlock, bool requireClearPath, bool keepNonTargetsOnMiss)
        {
            using ProfilerMarker.AutoScope classificationScope = InteractionPhysicsQueries.ProfileClassification(false);
            if (!hits.IsComplete)
            {
                return 0;
            }

            InteractionPhysicsQueries.Entry entry = hits.GetCompleteEntry();
            if (UsesIndex(entry.Count, foreignTriggersBlock) &&
                !PrefersCompleteForLargeTarget(scope.Explicit3D != null, entry.Count, ref _targetColliders3D,
                    ref _hintedCompletes3D))
            {
                int candidateCount = ProjectIndexedTargets3D(entry, scope);
                if (TryCompleteIndexed(entry, scope, false, requireClearPath, keepNonTargetsOnMiss, ref candidateCount))
                {
                    return candidateCount;
                }
            }

            return ProjectComplete3D(entry, scope, foreignTriggersBlock);
        }

        /// <summary>
        ///     Projects 2D hits. Physics2D queries take no trigger option, so excluded triggers are dropped here.
        /// </summary>
        /// <param name="includeTriggers">Keep trigger colliders that the global 2D policy returned.</param>
        internal int Project2D(InteractionPhysicsQueries.Hits2D hits, in InteractionTargetScope scope,
            bool foreignTriggersBlock, bool includeTriggers, bool requireClearPath, bool keepNonTargetsOnMiss)
        {
            using ProfilerMarker.AutoScope classificationScope = InteractionPhysicsQueries.ProfileClassification(true);
            if (!hits.IsComplete)
            {
                return 0;
            }

            InteractionPhysicsQueries.Entry entry = hits.GetCompleteEntry();
            if (UsesIndex(entry.Count, foreignTriggersBlock) &&
                !PrefersCompleteForLargeTarget(scope.Explicit2D != null, entry.Count, ref _targetColliders2D,
                    ref _hintedCompletes2D))
            {
                int candidateCount = ProjectIndexedTargets2D(entry, scope, includeTriggers);
                if (TryCompleteIndexed(entry, scope, true, requireClearPath, keepNonTargetsOnMiss, ref candidateCount))
                {
                    return candidateCount;
                }
            }

            return ProjectComplete2D(entry, scope, foreignTriggersBlock, includeTriggers);
        }

        private bool UsesIndex(int rawCount, bool foreignTriggersBlock)
        {
            return !foreignTriggersBlock && rawCount >= Math.Max(1, IndexedMinimumHits);
        }

        private bool PrefersCompleteForLargeTarget(bool explicitTarget, int rawCount, ref int targetColliders,
            ref int hintedCompletes)
        {
            // WHY: An indexed projection enumerates all C target colliders, the complete one checks H raw hits, so
            // the index pays off for this target only while C <= H. The count comes from the last enumeration; a
            // stale value can only change the cost, never the selection.
            if (explicitTarget || !UseTargetSizeHint || targetColliders <= rawCount)
            {
                return false;
            }

            if (++hintedCompletes < TargetSizeHintLifetime)
            {
                return true;
            }

            hintedCompletes = 0;
            targetColliders = 0;
            return false;
        }

        /// <summary>
        ///     Finishes an indexed projection whose target candidates are already in <see cref="Hits" />. Returns
        ///     false when the caller needs the complete projection instead (a miss that must keep non-target hits).
        /// </summary>
        private bool TryCompleteIndexed(InteractionPhysicsQueries.Entry entry, in InteractionTargetScope scope,
            bool twoDimensions, bool requireClearPath, bool keepNonTargetsOnMiss, ref int candidateCount)
        {
            bool selected = InteractionQueryMath.TrySelectTarget(_hits, candidateCount, false,
                out InteractionRayHit nearestTarget);
            if (selected && requireClearPath &&
                AppendBlockerInFront(entry, scope, twoDimensions, nearestTarget.Distance, ref candidateCount))
            {
                selected = false;
            }

            return selected || !keepNonTargetsOnMiss;
        }

        /// <summary>
        ///     Appends the nearest blocker strictly in front of the nearest target, if any. With it the candidates
        ///     give the same clear-path verdict and target hit as the complete projection.
        /// </summary>
        private bool AppendBlockerInFront(InteractionPhysicsQueries.Entry entry, in InteractionTargetScope scope,
            bool twoDimensions, float targetDistance, ref int candidateCount)
        {
            entry.EnsureDistanceOrder(twoDimensions);
            int inspected = 0;
            bool blocked = false;
            for (int k = 0; k < entry.DistanceOrderCount; k++)
            {
                int index = entry.DistanceOrder[k];
                float distance = twoDimensions ? entry.Hits2D[index].distance : entry.Hits3D[index].distance;
                if (!(distance < targetDistance))
                {
                    break;
                }

                inspected++;
                Vector3 point;
                Component hitCollider;
                bool isTrigger = false;
                if (twoDimensions)
                {
                    RaycastHit2D hit = entry.Hits2D[index];
                    point = hit.point;
                    Collider2D collider2D = hit.collider;
                    hitCollider = collider2D;
                    if (collider2D != null)
                    {
                        isTrigger = collider2D.isTrigger;
                    }
                }
                else
                {
                    RaycastHit hit = entry.Hits3D[index];
                    point = hit.point;
                    Collider collider3D = hit.collider;
                    hitCollider = collider3D;
                    if (collider3D != null)
                    {
                        isTrigger = collider3D.isTrigger;
                    }
                }

                // WHY: Every valid hit in front of the nearest target is a non-target for this scope. The complete
                // projection lets such a hit block unless it is gone, in the ignored source rig or a trigger; only
                // range line of sight lets foreign triggers block, and it never takes this path.
                if (hitCollider == null || isTrigger || scope.IsInIgnoredSourceRig(hitCollider))
                {
                    continue;
                }

                EnsureCapacity(candidateCount + 1);
                _hits[candidateCount++] = new InteractionRayHit(distance, point, false, true);
                blocked = true;
                break;
            }

            InteractionPhysicsQueries.CountInspectedHits(inspected);
            return blocked;
        }

        private int ProjectComplete3D(InteractionPhysicsQueries.Entry entry, in InteractionTargetScope scope,
            bool foreignTriggersBlock)
        {
            int rawCount = entry.Count;
            RaycastHit[] raw = entry.Hits3D;
            EnsureCapacity(rawCount);
            int candidateCount = 0;
            for (int i = 0; i < rawCount; i++)
            {
                RaycastHit physicsHit = raw[i];
                Collider hitCollider = physicsHit.collider;
                if (hitCollider == null)
                {
                    continue;
                }

                bool isTarget = scope.Contains(hitCollider);
                if (!isTarget && scope.IsInIgnoredSourceRig(hitCollider))
                {
                    continue;
                }

                bool blocksInteraction = !isTarget && (foreignTriggersBlock || !hitCollider.isTrigger);
                _hits[candidateCount++] = new InteractionRayHit(physicsHit.distance, physicsHit.point,
                    isTarget, blocksInteraction);
            }

            InteractionPhysicsQueries.CountInspectedHits(rawCount);
            return candidateCount;
        }

        private int ProjectComplete2D(InteractionPhysicsQueries.Entry entry, in InteractionTargetScope scope,
            bool foreignTriggersBlock, bool includeTriggers)
        {
            int rawCount = entry.Count;
            RaycastHit2D[] raw = entry.Hits2D;
            EnsureCapacity(rawCount);
            int candidateCount = 0;
            for (int i = 0; i < rawCount; i++)
            {
                RaycastHit2D physicsHit = raw[i];
                Collider2D hitCollider = physicsHit.collider;
                if (hitCollider == null || (!includeTriggers && hitCollider.isTrigger))
                {
                    continue;
                }

                bool isTarget = scope.Contains(hitCollider);
                if (!isTarget && scope.IsInIgnoredSourceRig(hitCollider))
                {
                    continue;
                }

                bool blocksInteraction = !isTarget && (foreignTriggersBlock || !hitCollider.isTrigger);
                _hits[candidateCount++] = new InteractionRayHit(physicsHit.distance, physicsHit.point,
                    isTarget, blocksInteraction);
            }

            InteractionPhysicsQueries.CountInspectedHits(rawCount);
            return candidateCount;
        }

        private int ProjectIndexedTargets3D(InteractionPhysicsQueries.Entry entry, in InteractionTargetScope scope)
        {
            // WHY: No callback or yield runs inside this method, so the entry cannot expire while it is read and
            // the live hierarchy enumerated here equals the one the complete projection would test hit by hit.
            entry.EnsureColliderIndex3D();
            int selectedCount = 0;
            Collider explicitCollider = scope.Explicit3D;
            if (explicitCollider != null)
            {
                CollectHitIndices(entry, entry.FirstHitIndex(explicitCollider), ref selectedCount);
            }
            else
            {
                _colliders3D ??= new List<Collider>();
                try
                {
                    scope.Target.GetComponentsInChildren(true, _colliders3D);
                    _targetColliders3D = _colliders3D.Count;
                    _hintedCompletes3D = 0;
                    for (int i = 0; i < _colliders3D.Count; i++)
                    {
                        Collider collider = _colliders3D[i];
                        if (collider != null)
                        {
                            CollectHitIndices(entry, entry.FirstHitIndex(collider), ref selectedCount);
                        }
                    }
                }
                finally
                {
                    _colliders3D.Clear();
                }
            }

            SortSelected(selectedCount);
            EnsureCapacity(selectedCount);
            RaycastHit[] raw = entry.Hits3D;
            for (int i = 0; i < selectedCount; i++)
            {
                RaycastHit hit = raw[_selected[i]];
                // WHY: Legacy 3D never re-reads a cached hit's live trigger flag; the native query decided it.
                _hits[i] = new InteractionRayHit(hit.distance, hit.point, true, false);
            }

            InteractionPhysicsQueries.CountInspectedHits(selectedCount);
            return selectedCount;
        }

        private int ProjectIndexedTargets2D(InteractionPhysicsQueries.Entry entry, in InteractionTargetScope scope,
            bool includeTriggers)
        {
            entry.EnsureColliderIndex2D();
            int selectedCount = 0;
            Collider2D explicitCollider = scope.Explicit2D;
            if (explicitCollider != null)
            {
                CollectHitIndices(entry, entry.FirstHitIndex(explicitCollider), ref selectedCount);
            }
            else
            {
                _colliders2D ??= new List<Collider2D>();
                try
                {
                    scope.Target.GetComponentsInChildren(true, _colliders2D);
                    _targetColliders2D = _colliders2D.Count;
                    _hintedCompletes2D = 0;
                    for (int i = 0; i < _colliders2D.Count; i++)
                    {
                        Collider2D collider = _colliders2D[i];
                        if (collider != null)
                        {
                            CollectHitIndices(entry, entry.FirstHitIndex(collider), ref selectedCount);
                        }
                    }
                }
                finally
                {
                    _colliders2D.Clear();
                }
            }

            SortSelected(selectedCount);
            EnsureCapacity(selectedCount);
            RaycastHit2D[] raw = entry.Hits2D;
            int candidateCount = 0;
            for (int i = 0; i < selectedCount; i++)
            {
                RaycastHit2D hit = raw[_selected[i]];
                if (includeTriggers || !hit.collider.isTrigger)
                {
                    _hits[candidateCount++] = new InteractionRayHit(hit.distance, hit.point, true, false);
                }
            }

            InteractionPhysicsQueries.CountInspectedHits(selectedCount);
            return candidateCount;
        }

        private void CollectHitIndices(InteractionPhysicsQueries.Entry entry, int index, ref int count)
        {
            for (; index >= 0; index = entry.NextHitIndices[index])
            {
                if (count == _selected.Length)
                {
                    Array.Resize(ref _selected, Math.Max(16, _selected.Length * 2));
                }

                _selected[count++] = index;
            }
        }

        private void SortSelected(int selectedCount)
        {
            // WHY: Raw order decides equal-distance ties exactly like the complete projection.
            if (selectedCount > 1)
            {
                Array.Sort(_selected, 0, selectedCount);
            }
        }

        private void EnsureCapacity(int count)
        {
            if (_hits.Length < count)
            {
                Array.Resize(ref _hits, count);
            }
        }
    }
}
