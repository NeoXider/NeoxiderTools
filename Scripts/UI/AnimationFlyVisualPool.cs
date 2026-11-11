using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.UI
{
    /// <summary>
    ///     Ownership and reuse of the visuals spawned by <see cref="AnimationFly" />, without any knowledge
    ///     of tweens, coordinate spaces or requests. Every visual the fly creates is owned until it is
    ///     destroyed or handed over to the caller (KeepAlive); pooled visuals stay owned while they wait.
    /// </summary>
    internal sealed class AnimationFlyVisualPool
    {
        private static readonly Predicate<GameObject> IsDestroyed = visual => visual == null;

        private readonly Dictionary<(Object Visual, AnimationFlySpawnSpace Space), Stack<GameObject>> _pool = new();
        private readonly HashSet<GameObject> _owned = new();

        /// <summary>Number of visuals currently owned (in flight or waiting in the pool).</summary>
        internal int OwnedCount => _owned.Count;

        /// <summary>Starts owning <paramref name="visual" />.</summary>
        internal void Track(GameObject visual)
        {
            if (visual != null)
            {
                _owned.Add(visual);
            }
        }

        /// <summary>Stops owning <paramref name="visual" /> without destroying it (KeepAlive hand-over).</summary>
        internal void Release(GameObject visual)
        {
            _owned.Remove(visual);
        }

        /// <summary>True while <paramref name="visual" /> is owned by this pool.</summary>
        internal bool Owns(GameObject visual)
        {
            return visual != null && _owned.Contains(visual);
        }

        /// <summary>Number of idle visuals stored for one visual key and spawn space.</summary>
        internal int GetPooledCount(Object visualKey, AnimationFlySpawnSpace space)
        {
            return visualKey != null && _pool.TryGetValue((visualKey, space), out Stack<GameObject> stack)
                ? stack.Count
                : 0;
        }

        /// <summary>
        ///     Pops a live pooled visual for the key and re-parents it under <paramref name="parent" />.
        ///     Destroyed entries are skipped and forgotten.
        /// </summary>
        internal bool TryTake(Object visualKey, AnimationFlySpawnSpace space, Transform parent,
            out GameObject visual)
        {
            visual = null;
            if (visualKey == null || !_pool.TryGetValue((visualKey, space), out Stack<GameObject> stack))
            {
                return false;
            }

            while (stack.Count > 0)
            {
                GameObject candidate = stack.Pop();
                if (candidate == null)
                {
                    _owned.Remove(candidate);
                    continue;
                }

                candidate.transform.SetParent(parent, false);
                visual = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Stores an already deactivated visual for reuse. Returns false when the key is missing or the
        ///     per-key limit is reached; the visual is then no longer owned and the caller destroys it.
        /// </summary>
        internal bool TryReturn(Object visualKey, AnimationFlySpawnSpace space, GameObject visual, int maxPerKey)
        {
            if (visual == null)
            {
                return false;
            }

            if (visualKey == null || maxPerKey <= 0)
            {
                _owned.Remove(visual);
                return false;
            }

            if (!_pool.TryGetValue((visualKey, space), out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                _pool.Add((visualKey, space), stack);
            }

            if (stack.Count >= maxPerKey)
            {
                _owned.Remove(visual);
                return false;
            }

            stack.Push(visual);
            return true;
        }

        /// <summary>
        ///     Forgets owned visuals that were destroyed from outside (for example with their parent panel).
        ///     Returns how many references were dropped.
        /// </summary>
        internal int PruneDestroyed()
        {
            return _owned.Count == 0 ? 0 : _owned.RemoveWhere(IsDestroyed);
        }

        /// <summary>
        ///     Runs <paramref name="beforeDestroy" /> on every live owned visual, destroys it and clears the
        ///     pool. Uses <see cref="Object.DestroyImmediate(Object)" /> outside Play Mode.
        /// </summary>
        internal void DestroyAll(Action<GameObject> beforeDestroy)
        {
            foreach (GameObject visual in _owned)
            {
                if (visual == null)
                {
                    continue;
                }

                beforeDestroy?.Invoke(visual);
                if (Application.isPlaying)
                {
                    Object.Destroy(visual);
                }
                else
                {
                    Object.DestroyImmediate(visual);
                }
            }

            _owned.Clear();
            _pool.Clear();
        }
    }
}
