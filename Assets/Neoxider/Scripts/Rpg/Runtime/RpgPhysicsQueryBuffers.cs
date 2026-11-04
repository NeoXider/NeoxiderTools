using System.Collections.Generic;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Reusable, growable physics query buffers for RPG attacks and targeting. Queries never truncate: a full 3D
    ///     buffer doubles and repeats the query up to <see cref="MaxPooledHits" />, then falls back to the allocating
    ///     <c>*All</c> variant; 2D queries use the list overloads that grow on their own.
    /// </summary>
    /// <remarks>
    ///     One instance per owner. The buffers are overwritten by the next query, so callers that can re-enter
    ///     (hit callbacks starting another query on the same owner) must rent a separate instance.
    /// </remarks>
    internal sealed class RpgPhysicsQueryBuffers
    {
        internal const int MaxPooledHits = 4096;

        private readonly List<RaycastHit2D> _hits2D = new();
        private readonly List<Collider2D> _colliders2D = new();
        private RaycastHit[] _hits3D;
        private Collider[] _colliders3D;

        internal RpgPhysicsQueryBuffers(int initialCapacity = 16)
        {
            int capacity = Mathf.Clamp(initialCapacity, 1, MaxPooledHits);
            _hits3D = new RaycastHit[capacity];
            _colliders3D = new Collider[capacity];
        }

        internal RaycastHit[] Hits3D => _hits3D;
        internal Collider[] Colliders3D => _colliders3D;
        internal List<RaycastHit2D> Hits2D => _hits2D;
        internal List<Collider2D> Colliders2D => _colliders2D;

        /// <summary>Complete sphere cast into <see cref="Hits3D" />, sorted nearest first.</summary>
        internal int SphereCast(Vector3 origin, float radius, Vector3 direction, float distance, int layerMask)
        {
            int count;
            while (true)
            {
                count = Physics.SphereCastNonAlloc(origin, radius, direction, _hits3D, distance, layerMask);
                if (count < _hits3D.Length)
                {
                    break;
                }

                if (_hits3D.Length >= MaxPooledHits)
                {
                    _hits3D = Physics.SphereCastAll(origin, radius, direction, distance, layerMask);
                    count = _hits3D.Length;
                    break;
                }

                _hits3D = new RaycastHit[Mathf.Min(MaxPooledHits, _hits3D.Length * 2)];
            }

            RpgHitSort.Sort(_hits3D, count, RpgHitDistanceComparer.Instance);
            return count;
        }

        /// <summary>Complete sphere overlap into <see cref="Colliders3D" /> (unordered).</summary>
        internal int OverlapSphere(Vector3 center, float radius, int layerMask)
        {
            while (true)
            {
                int count = Physics.OverlapSphereNonAlloc(center, radius, _colliders3D, layerMask);
                if (count < _colliders3D.Length)
                {
                    return count;
                }

                if (_colliders3D.Length >= MaxPooledHits)
                {
                    _colliders3D = Physics.OverlapSphere(center, radius, layerMask);
                    return _colliders3D.Length;
                }

                _colliders3D = new Collider[Mathf.Min(MaxPooledHits, _colliders3D.Length * 2)];
            }
        }

        /// <summary>Circle cast into <see cref="Hits2D" /> with the legacy layer/trigger semantics.</summary>
        internal int CircleCast2D(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask)
        {
            return Physics2D.CircleCast(origin, radius, direction, CreateFilter(layerMask), _hits2D, distance);
        }

        /// <summary>Ray cast into <see cref="Hits2D" /> with the legacy layer/trigger semantics.</summary>
        internal int Raycast2D(Vector2 origin, Vector2 direction, float distance, int layerMask)
        {
            return Physics2D.Raycast(origin, direction, CreateFilter(layerMask), _hits2D, distance);
        }

        /// <summary>Circle overlap into <see cref="Colliders2D" /> with the legacy layer/trigger semantics.</summary>
        internal int OverlapCircle2D(Vector2 center, float radius, int layerMask)
        {
            return Physics2D.OverlapCircle(center, radius, CreateFilter(layerMask), _colliders2D);
        }

        /// <summary>Drops references held by the last query so pooled buffers never keep destroyed colliders alive.</summary>
        internal void Release()
        {
            System.Array.Clear(_hits3D, 0, _hits3D.Length);
            System.Array.Clear(_colliders3D, 0, _colliders3D.Length);
            _hits2D.Clear();
            _colliders2D.Clear();
        }

        private static ContactFilter2D CreateFilter(int layerMask)
        {
            // WHY: mirrors the legacy *All overloads: layer mask plus the global trigger setting, no depth filter.
            ContactFilter2D filter = new();
            filter.SetLayerMask(layerMask);
            filter.useTriggers = Physics2D.queriesHitTriggers;
            return filter;
        }
    }

    /// <summary>
    ///     In-place sort of query results that never allocates. <see cref="System.Array.Sort{T}(T[], int, int, IComparer{T})" />
    ///     and <see cref="List{T}.Sort(IComparer{T})" /> wrap the comparer in a new delegate on every call, which cost
    ///     one GC allocation per attack. The comparers below are total orders, so the result equals theirs.
    /// </summary>
    internal static class RpgHitSort
    {
        private const int InsertionSortLimit = 16;

        /// <summary>Sorts the first <paramref name="count" /> items (arrays and lists alike).</summary>
        internal static void Sort<T>(IList<T> items, int count, IComparer<T> comparer)
        {
            if (count < 2)
            {
                return;
            }

            if (count <= InsertionSortLimit)
            {
                InsertionSort(items, count, comparer);
            }
            else
            {
                HeapSort(items, count, comparer);
            }
        }

        private static void InsertionSort<T>(IList<T> items, int count, IComparer<T> comparer)
        {
            for (int i = 1; i < count; i++)
            {
                T key = items[i];
                int j = i - 1;
                while (j >= 0 && comparer.Compare(items[j], key) > 0)
                {
                    items[j + 1] = items[j];
                    j--;
                }

                items[j + 1] = key;
            }
        }

        // WHY: O(n log n) without recursion or scratch memory for the rare crowded query.
        private static void HeapSort<T>(IList<T> items, int count, IComparer<T> comparer)
        {
            for (int root = count / 2 - 1; root >= 0; root--)
            {
                SiftDown(items, root, count, comparer);
            }

            for (int end = count - 1; end > 0; end--)
            {
                T top = items[0];
                items[0] = items[end];
                items[end] = top;
                SiftDown(items, 0, end, comparer);
            }
        }

        private static void SiftDown<T>(IList<T> items, int root, int count, IComparer<T> comparer)
        {
            while (true)
            {
                int child = root * 2 + 1;
                if (child >= count)
                {
                    return;
                }

                if (child + 1 < count && comparer.Compare(items[child], items[child + 1]) < 0)
                {
                    child++;
                }

                if (comparer.Compare(items[root], items[child]) >= 0)
                {
                    return;
                }

                T swap = items[root];
                items[root] = items[child];
                items[child] = swap;
                root = child;
            }
        }
    }

    /// <summary>Nearest-first ordering for 3D hits with an allocation-free identity tie-break.</summary>
    internal sealed class RpgHitDistanceComparer : IComparer<RaycastHit>
    {
        internal static readonly RpgHitDistanceComparer Instance = new();

        public int Compare(RaycastHit left, RaycastHit right)
        {
            int distance = left.distance.CompareTo(right.distance);
            return distance != 0 ? distance : RpgHitIdentity.Compare(left.collider, right.collider);
        }
    }

    /// <summary>Nearest-first ordering for 2D hits with an allocation-free identity tie-break.</summary>
    internal sealed class RpgHit2DDistanceComparer : IComparer<RaycastHit2D>
    {
        internal static readonly RpgHit2DDistanceComparer Instance = new();

        public int Compare(RaycastHit2D left, RaycastHit2D right)
        {
            int distance = left.distance.CompareTo(right.distance);
            return distance != 0 ? distance : RpgHitIdentity.Compare(left.collider, right.collider);
        }
    }

    internal static class RpgHitIdentity
    {
        // WHY: Object.GetHashCode is the engine identity and stays valid on Unity 6.5, where GetInstanceID is
        // obsolete-as-error; formatting StableId strings inside a sort comparer allocated on every distance tie.
        internal static int Compare(Object left, Object right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            return left.GetHashCode().CompareTo(right.GetHashCode());
        }
    }
}
