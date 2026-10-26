using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Profiling;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Main-thread raw interaction queries. Shared results are borrowed until the frame or epoch changes.
    ///     Target classification, hierarchy exclusions, range and event delivery remain with each consumer.
    /// </summary>
    public static class InteractionPhysicsQueries
    {
        public enum QueryMode
        {
            /// <summary>Start a new epoch before querying. The default for external synchronous callers.</summary>
            Fresh,
            /// <summary>Reuse an identical query within the current stable interaction epoch.</summary>
            Shared
        }

        /// <summary>Diagnostics only; purpose is never part of the physical cache key.</summary>
        public enum QueryPurpose
        {
            External,
            Camera,
            KeyboardLook,
            RangeLineOfSight
        }

        public readonly struct Hits3D
        {
            private readonly Entry _entry;
            private readonly ulong _epoch;

            internal Hits3D(Entry entry)
            {
                _entry = entry;
                _epoch = entry.Generation;
            }

            public bool IsValid => _entry != null && _epoch == Epoch && _entry.Generation == _epoch;
            public bool IsComplete => IsValid && _entry.Complete;
            public int Count => IsValid ? _entry.Count : 0;

            public RaycastHit this[int index]
            {
                get
                {
                    if (!IsValid)
                    {
                        throw new InvalidOperationException("The borrowed interaction hits have expired.");
                    }

                    if (index < 0 || index >= _entry.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    return _entry.Hits3D[index];
                }
            }
        }

        public readonly struct Hits2D
        {
            private readonly Entry _entry;
            private readonly ulong _epoch;

            internal Hits2D(Entry entry)
            {
                _entry = entry;
                _epoch = entry.Generation;
            }

            public bool IsValid => _entry != null && _epoch == Epoch && _entry.Generation == _epoch;
            public bool IsComplete => IsValid && _entry.Complete;
            public int Count => IsValid ? _entry.Count : 0;

            public RaycastHit2D this[int index]
            {
                get
                {
                    if (!IsValid)
                    {
                        throw new InvalidOperationException("The borrowed interaction hits have expired.");
                    }

                    if (index < 0 || index >= _entry.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    return _entry.Hits2D[index];
                }
            }
        }

        /// <summary>Every native attempt is counted, including complete overflow fallbacks.</summary>
        public readonly struct Statistics
        {
            internal Statistics(long raycasts3D, long intersections2D, long raycasts2D, long filtered2D,
                long cacheHits, long fallbacks, long invalidations, long freshQueries, long callbackBoundaries,
                long frameAdvances, long callbackQueries, long cameraQueries, long lookQueries, long rangeQueries,
                long classificationPasses, long classifiedHits)
            {
                Raycasts3D = raycasts3D;
                RayIntersections2D = intersections2D;
                Raycasts2D = raycasts2D;
                FilteredRaycasts2D = filtered2D;
                CacheHits = cacheHits;
                OverflowFallbacks = fallbacks;
                Invalidations = invalidations;
                FreshQueries = freshQueries;
                CallbackBoundaries = callbackBoundaries;
                FrameAdvances = frameAdvances;
                NativeCallsInsideCallbacks = callbackQueries;
                CameraNativeCalls = cameraQueries;
                LookNativeCalls = lookQueries;
                RangeNativeCalls = rangeQueries;
                ClassificationPasses = classificationPasses;
                ClassifiedHits = classifiedHits;
            }

            public long Raycasts3D { get; }
            public long RayIntersections2D { get; }
            public long Raycasts2D { get; }
            public long FilteredRaycasts2D { get; }
            public long NativeCalls => Raycasts3D + RayIntersections2D + Raycasts2D + FilteredRaycasts2D;
            public long CacheHits { get; }
            public long OverflowFallbacks { get; }
            public long Invalidations { get; }
            public long FreshQueries { get; }
            public long CallbackBoundaries { get; }
            public long FrameAdvances { get; }
            public long NativeCallsInsideCallbacks { get; }
            public long CameraNativeCalls { get; }
            public long LookNativeCalls { get; }
            public long RangeNativeCalls { get; }
            /// <summary>Target-local projection passes, independent of the number of native queries.</summary>
            public long ClassificationPasses { get; }
            /// <summary>Raw hit entries inspected by target-local projection, including excluded entries.</summary>
            public long ClassifiedHits { get; }
            public long ExternalNativeCalls => NativeCalls - CameraNativeCalls - LookNativeCalls - RangeNativeCalls;
        }

        internal sealed class Entry
        {
            internal RaycastHit[] Hits3D = Array.Empty<RaycastHit>();
            internal RaycastHit2D[] Hits2D = Array.Empty<RaycastHit2D>();
            internal List<RaycastHit2D> Overflow2D;
            internal int Count;
            internal bool Complete;
            internal ulong Generation;

            internal void Release()
            {
                Array.Clear(Hits3D, 0, Math.Min(Count, Hits3D.Length));
                Array.Clear(Hits2D, 0, Math.Min(Count, Hits2D.Length));
                Overflow2D?.Clear();
                Count = 0;
                Complete = false;
                Generation = 0;
            }
        }

        internal readonly struct CallbackScope : IDisposable
        {
            public void Dispose()
            {
                try
                {
                    _callbackDepth--;
                    AdvanceEpoch();
                }
                finally
                {
                    CallbackMarker.End();
                }
            }
        }

        private enum Kind
        {
            Ray3D,
            Intersection2D,
            Ray2D,
            FilteredRay2D
        }

        private readonly struct Key : IEquatable<Key>
        {
            internal Key(Camera camera, Kind kind, Ray ray, float distance, int mask,
                QueryTriggerInteraction triggers, ContactFilter2D filter, Vector2 planarOrigin = default,
                Vector2 planarDirection = default)
            {
                Camera = camera;
                CameraHash = ReferenceEquals(camera, null) ? 0 : RuntimeHelpers.GetHashCode(camera);
                PixelRect = camera != null ? camera.pixelRect : default;
                TargetTexture = camera != null ? camera.targetTexture : null;
                Display = camera != null ? camera.targetDisplay : 0;
                QueryKind = kind;
                Ray = ray;
                PlanarOrigin = planarOrigin;
                PlanarDirection = planarDirection;
                Distance = distance;
                Mask = mask;
                Triggers = triggers == QueryTriggerInteraction.UseGlobal
                    ? Physics.queriesHitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore
                    : triggers;
                Filter = filter;
                World3D = Physics.defaultPhysicsScene;
                World2D = Physics2D.defaultPhysicsScene;
                Backfaces = kind == Kind.Ray3D && Physics.queriesHitBackfaces;
                GlobalTriggers2D = (kind == Kind.Intersection2D || kind == Kind.Ray2D) &&
                                   Physics2D.queriesHitTriggers;
                StartInside2D = kind != Kind.Ray3D && Physics2D.queriesStartInColliders;
            }

            internal Camera Camera { get; }
            private int CameraHash { get; }
            private Rect PixelRect { get; }
            private RenderTexture TargetTexture { get; }
            private int Display { get; }
            internal Kind QueryKind { get; }
            internal Ray Ray { get; }
            internal Vector2 PlanarOrigin { get; }
            internal Vector2 PlanarDirection { get; }
            internal float Distance { get; }
            internal int Mask { get; }
            internal QueryTriggerInteraction Triggers { get; }
            internal ContactFilter2D Filter { get; }
            private PhysicsScene World3D { get; }
            private PhysicsScene2D World2D { get; }
            private bool Backfaces { get; }
            private bool GlobalTriggers2D { get; }
            private bool StartInside2D { get; }

            public bool Equals(Key other)
            {
                // Vector3.operator == is approximate and must not identify physical queries.
                return ReferenceEquals(Camera, other.Camera) && PixelRect.Equals(other.PixelRect) &&
                       ReferenceEquals(TargetTexture, other.TargetTexture) && Display == other.Display &&
                       QueryKind == other.QueryKind &&
                       Ray.origin.Equals(other.Ray.origin) && Ray.direction.Equals(other.Ray.direction) &&
                       PlanarOrigin.Equals(other.PlanarOrigin) && PlanarDirection.Equals(other.PlanarDirection) &&
                       Distance.Equals(other.Distance) && Mask == other.Mask && Triggers == other.Triggers &&
                       World3D.Equals(other.World3D) && World2D.Equals(other.World2D) &&
                       Backfaces == other.Backfaces && GlobalTriggers2D == other.GlobalTriggers2D &&
                       StartInside2D == other.StartInside2D && FilterEquals(Filter, other.Filter);
            }

            public override bool Equals(object obj) => obj is Key other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = CameraHash;
                    hash = hash * 397 ^ PixelRect.GetHashCode();
                    hash = hash * 397 ^ (ReferenceEquals(TargetTexture, null)
                        ? 0 : RuntimeHelpers.GetHashCode(TargetTexture));
                    hash = hash * 397 ^ Display;
                    hash = hash * 397 ^ (int)QueryKind;
                    hash = hash * 397 ^ Ray.origin.GetHashCode();
                    hash = hash * 397 ^ Ray.direction.GetHashCode();
                    hash = hash * 397 ^ PlanarOrigin.GetHashCode();
                    hash = hash * 397 ^ PlanarDirection.GetHashCode();
                    hash = hash * 397 ^ Distance.GetHashCode();
                    hash = hash * 397 ^ Mask;
                    hash = hash * 397 ^ (int)Triggers;
                    hash = hash * 397 ^ World3D.GetHashCode();
                    hash = hash * 397 ^ World2D.GetHashCode();
                    hash = hash * 397 ^ Backfaces.GetHashCode();
                    hash = hash * 397 ^ GlobalTriggers2D.GetHashCode();
                    hash = hash * 397 ^ StartInside2D.GetHashCode();
                    hash = hash * 397 ^ Filter.layerMask.value;
                    hash = hash * 397 ^ Filter.useLayerMask.GetHashCode();
                    hash = hash * 397 ^ Filter.useTriggers.GetHashCode();
                    hash = hash * 397 ^ Filter.useDepth.GetHashCode();
                    hash = hash * 397 ^ Filter.useOutsideDepth.GetHashCode();
                    hash = hash * 397 ^ Filter.minDepth.GetHashCode();
                    hash = hash * 397 ^ Filter.maxDepth.GetHashCode();
                    hash = hash * 397 ^ Filter.useNormalAngle.GetHashCode();
                    hash = hash * 397 ^ Filter.useOutsideNormalAngle.GetHashCode();
                    hash = hash * 397 ^ Filter.minNormalAngle.GetHashCode();
                    return hash * 397 ^ Filter.maxNormalAngle.GetHashCode();
                }
            }
        }

        private static readonly Dictionary<Key, Entry> Active = new();
        private static readonly List<Entry> Buffers = new();
        private static readonly ProfilerMarker Native3DMarker = new("Neo.Interaction.Native.Raycast3D");
        private static readonly ProfilerMarker NativeIntersection2DMarker = new("Neo.Interaction.Native.Intersection2D");
        private static readonly ProfilerMarker NativeRay2DMarker = new("Neo.Interaction.Native.Raycast2D");
        private static readonly ProfilerMarker NativeFiltered2DMarker = new("Neo.Interaction.Native.FilteredRaycast2D");
        private static readonly ProfilerMarker CallbackMarker = new("Neo.Interaction.Callbacks");
        private static readonly ProfilerMarker Classify3DMarker = new("Neo.Interaction.Classify3D");
        private static readonly ProfilerMarker Classify2DMarker = new("Neo.Interaction.Classify2D");
        private static int _used;
        private static int _frame = -1;
        private static ulong _epoch = 1;
        private static int _callbackDepth;
        private static long _raycasts3D;
        private static long _intersections2D;
        private static long _raycasts2D;
        private static long _filtered2D;
        private static long _cacheHits;
        private static long _fallbacks;
        private static long _invalidations;
        private static long _freshQueries;
        private static long _callbackBoundaries;
        private static long _frameAdvances;
        private static long _callbackQueries;
        private static long _cameraQueries;
        private static long _lookQueries;
        private static long _rangeQueries;
        private static long _classificationPasses;
        private static long _classifiedHits;

        public static ulong Epoch
        {
            get
            {
                EnsureFrame();
                return _epoch;
            }
        }

        public static Statistics Counters => new(_raycasts3D, _intersections2D, _raycasts2D, _filtered2D,
            _cacheHits, _fallbacks, _invalidations, _freshQueries, _callbackBoundaries, _frameAdvances,
            _callbackQueries, _cameraQueries, _lookQueries, _rangeQueries, _classificationPasses, _classifiedHits);

        /// <summary>
        ///     End the borrowed-hit lifetime after manual simulation, SyncTransforms or an application world change.
        ///     Arbitrary external mutations between components' Update calls are not detected automatically.
        /// </summary>
        public static void Invalidate()
        {
            EnsureFrame();
            AdvanceEpoch();
        }

        /// <summary>End the epoch and release retained buffer capacity, for example at a scene/memory boundary.</summary>
        public static void ReleaseBuffers()
        {
            Invalidate();
            Buffers.Clear();
        }

        /// <summary>Reset diagnostics only; this does not invalidate or execute a query.</summary>
        public static void ResetCounters()
        {
            _raycasts3D = 0;
            _intersections2D = 0;
            _raycasts2D = 0;
            _filtered2D = 0;
            _cacheHits = 0;
            _fallbacks = 0;
            _invalidations = 0;
            _freshQueries = 0;
            _callbackBoundaries = 0;
            _frameAdvances = 0;
            _callbackQueries = 0;
            _cameraQueries = 0;
            _lookQueries = 0;
            _rangeQueries = 0;
            _classificationPasses = 0;
            _classifiedHits = 0;
        }

        public static Hits3D Raycast3D(Camera camera, Ray ray, float distance, int layerMask,
            QueryTriggerInteraction triggers, QueryMode mode = QueryMode.Fresh,
            QueryPurpose purpose = QueryPurpose.External)
        {
            if (!IsValidRay(ray, distance))
            {
                return default;
            }

            return new Hits3D(Get(new Key(camera, Kind.Ray3D, ray, distance, layerMask, triggers, default), mode, purpose));
        }

        public static Hits2D GetRayIntersection2D(Camera camera, Ray ray, float distance, int layerMask,
            QueryMode mode = QueryMode.Fresh, QueryPurpose purpose = QueryPurpose.External)
        {
            if (!IsValidRay(ray, distance))
            {
                return default;
            }

            return new Hits2D(Get(new Key(camera, Kind.Intersection2D, ray, distance, layerMask,
                QueryTriggerInteraction.Ignore, default), mode, purpose));
        }

        public static Hits2D Raycast2D(Camera camera, Vector2 origin, Vector2 direction, float distance,
            int layerMask, QueryMode mode = QueryMode.Fresh, QueryPurpose purpose = QueryPurpose.External)
        {
            if (!IsValidPlanarRay(origin, direction, distance))
            {
                return default;
            }

            return new Hits2D(Get(new Key(camera, Kind.Ray2D, default, distance, layerMask,
                QueryTriggerInteraction.Ignore, default, origin, direction), mode, purpose));
        }

        public static Hits2D Raycast2D(Camera camera, Vector2 origin, Vector2 direction, float distance,
            ContactFilter2D filter, QueryMode mode = QueryMode.Fresh, QueryPurpose purpose = QueryPurpose.External)
        {
            if (!IsValidPlanarRay(origin, direction, distance))
            {
                return default;
            }

            return new Hits2D(Get(new Key(camera, Kind.FilteredRay2D, default, distance, 0,
                QueryTriggerInteraction.Ignore, filter, origin, direction), mode, purpose));
        }

        internal static CallbackScope BeginCallbacks()
        {
            EnsureFrame();
            AdvanceEpoch();
            _callbackBoundaries++;
            _callbackDepth++;
            CallbackMarker.Begin();
            return new CallbackScope();
        }

        internal static ProfilerMarker.AutoScope ProfileClassification(int hitCount, bool twoDimensions)
        {
            _classificationPasses++;
            _classifiedHits += hitCount;
            return twoDimensions ? Classify2DMarker.Auto() : Classify3DMarker.Auto();
        }

        private static Entry Get(Key key, QueryMode mode, QueryPurpose purpose)
        {
            EnsureFrame();
            if (mode != QueryMode.Shared || _callbackDepth > 0)
            {
                _freshQueries++;
                AdvanceEpoch();
            }

            if (Active.TryGetValue(key, out Entry cached))
            {
                _cacheHits++;
                return cached;
            }

            if (_used == Buffers.Count)
            {
                Buffers.Add(new Entry());
            }

            Entry entry = Buffers[_used++];
            entry.Generation = _epoch;
            Active.Add(key, entry);
            Execute(key, entry, purpose);
            entry.Complete = true;
            return entry;
        }

        private static void Execute(Key key, Entry entry, QueryPurpose purpose)
        {
            if (key.QueryKind == Kind.Ray3D)
            {
                if (entry.Hits3D.Length == 0)
                {
                    entry.Hits3D = new RaycastHit[16];
                }

                int count;
                using (Native3DMarker.Auto())
                {
                    CountNative(key.QueryKind, purpose);
                    count = Physics.RaycastNonAlloc(key.Ray, entry.Hits3D, key.Distance, key.Mask, key.Triggers);
                }
                if (count == entry.Hits3D.Length)
                {
                    _fallbacks++;
                    RaycastHit[] complete;
                    using (Native3DMarker.Auto())
                    {
                        CountNative(key.QueryKind, purpose);
                        complete = Physics.RaycastAll(key.Ray, key.Distance, key.Mask, key.Triggers);
                    }
                    Grow(ref entry.Hits3D, complete.Length);
                    Array.Copy(complete, entry.Hits3D, complete.Length);
                    count = complete.Length;
                }

                entry.Count = count;
                return;
            }

            if (entry.Hits2D.Length == 0)
            {
                entry.Hits2D = new RaycastHit2D[16];
            }

            // Preserve the caller's original planar arguments, including non-unit/tiny/zero directions.
            Vector2 origin = key.PlanarOrigin;
            Vector2 direction = key.PlanarDirection;
            int count2D;
            using (GetNative2DMarker(key.QueryKind).Auto())
            {
                CountNative(key.QueryKind, purpose);
                if (key.QueryKind == Kind.Intersection2D)
                {
                    count2D = Physics2D.GetRayIntersectionNonAlloc(key.Ray, entry.Hits2D, key.Distance, key.Mask);
                }
                else if (key.QueryKind == Kind.Ray2D)
                {
                    count2D = Physics2D.RaycastNonAlloc(origin, direction, entry.Hits2D, key.Distance, key.Mask);
                }
                else
                {
                    count2D = Physics2D.Raycast(origin, direction, key.Filter, entry.Hits2D, key.Distance);
                }
            }

            if (count2D == entry.Hits2D.Length)
            {
                _fallbacks++;
                if (key.QueryKind == Kind.FilteredRay2D)
                {
                    entry.Overflow2D ??= new List<RaycastHit2D>();
                    entry.Overflow2D.Clear();
                    using (NativeFiltered2DMarker.Auto())
                    {
                        CountNative(key.QueryKind, purpose);
                        count2D = Physics2D.Raycast(origin, direction, key.Filter, entry.Overflow2D, key.Distance);
                    }
                    Grow(ref entry.Hits2D, count2D);
                    entry.Overflow2D.CopyTo(entry.Hits2D);
                    entry.Overflow2D.Clear();
                }
                else
                {
                    RaycastHit2D[] complete;
                    using (GetNative2DMarker(key.QueryKind).Auto())
                    {
                        CountNative(key.QueryKind, purpose);
                        complete = key.QueryKind == Kind.Intersection2D
                            ? Physics2D.GetRayIntersectionAll(key.Ray, key.Distance, key.Mask)
                            : Physics2D.RaycastAll(origin, direction, key.Distance, key.Mask);
                    }
                    Grow(ref entry.Hits2D, complete.Length);
                    Array.Copy(complete, entry.Hits2D, complete.Length);
                    count2D = complete.Length;
                }
            }

            entry.Count = count2D;
        }

        private static ProfilerMarker GetNative2DMarker(Kind kind)
        {
            return kind == Kind.Intersection2D ? NativeIntersection2DMarker :
                kind == Kind.Ray2D ? NativeRay2DMarker : NativeFiltered2DMarker;
        }

        private static void Grow<T>(ref T[] buffer, int count)
        {
            // Leave spare capacity so an unchanged full result does not overflow again after warming.
            int capacity = count < int.MaxValue ? count + 1 : count;
            if (buffer.Length < capacity)
            {
                Array.Resize(ref buffer, capacity);
            }
        }

        private static void CountNative(Kind kind, QueryPurpose purpose)
        {
            switch (kind)
            {
                case Kind.Ray3D: _raycasts3D++; break;
                case Kind.Intersection2D: _intersections2D++; break;
                case Kind.Ray2D: _raycasts2D++; break;
                case Kind.FilteredRay2D: _filtered2D++; break;
            }

            switch (purpose)
            {
                case QueryPurpose.Camera: _cameraQueries++; break;
                case QueryPurpose.KeyboardLook: _lookQueries++; break;
                case QueryPurpose.RangeLineOfSight: _rangeQueries++; break;
            }

            if (_callbackDepth > 0)
            {
                _callbackQueries++;
            }
        }

        private static bool FilterEquals(ContactFilter2D left, ContactFilter2D right)
        {
            return left.useTriggers == right.useTriggers && left.useLayerMask == right.useLayerMask &&
                   left.layerMask.value == right.layerMask.value && left.useDepth == right.useDepth &&
                   left.useOutsideDepth == right.useOutsideDepth && left.minDepth.Equals(right.minDepth) &&
                   left.maxDepth.Equals(right.maxDepth) && left.useNormalAngle == right.useNormalAngle &&
                   left.useOutsideNormalAngle == right.useOutsideNormalAngle &&
                   left.minNormalAngle.Equals(right.minNormalAngle) && left.maxNormalAngle.Equals(right.maxNormalAngle);
        }

        private static bool IsValidRay(Ray ray, float distance)
        {
            return !float.IsNaN(distance) && distance >= 0f &&
                   float.IsFinite(ray.origin.x) && float.IsFinite(ray.origin.y) && float.IsFinite(ray.origin.z) &&
                   float.IsFinite(ray.direction.x) && float.IsFinite(ray.direction.y) && float.IsFinite(ray.direction.z);
        }

        private static bool IsValidPlanarRay(Vector2 origin, Vector2 direction, float distance)
        {
            return !float.IsNaN(distance) && distance >= 0f &&
                   float.IsFinite(origin.x) && float.IsFinite(origin.y) &&
                   float.IsFinite(direction.x) && float.IsFinite(direction.y);
        }

        private static void EnsureFrame()
        {
            int frame = Time.frameCount;
            if (frame != _frame)
            {
                _frame = frame;
                _frameAdvances++;
                AdvanceEpoch();
            }
        }

        private static void AdvanceEpoch()
        {
            for (int i = 0; i < _used; i++)
            {
                Buffers[i].Release();
            }

            Active.Clear();
            _used = 0;
            _invalidations++;
            unchecked { _epoch++; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            // Old borrowed structs can survive with domain reload disabled. Invalidate their entries too.
            for (int i = 0; i < Buffers.Count; i++)
            {
                Buffers[i].Release();
            }

            Active.Clear();
            Buffers.Clear();
            _used = 0;
            _frame = -1;
            _epoch = 1;
            _callbackDepth = 0;
            ResetCounters();
        }
    }
}
