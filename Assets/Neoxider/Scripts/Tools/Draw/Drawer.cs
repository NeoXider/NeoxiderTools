using System;
using System.Collections.Generic;
using Neo.Extensions;
using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Tools
{
    /// <summary>
    ///       Universal line-drawing component.
    ///     - Draws a <see cref="LineRenderer" /> with the mouse / first touch,
    ///     - Chaikin-smooths the polyline *live* as you draw,
    ///     - Optional 2-D collider & timed self-destruct,
    ///     - Can clone most settings from a "template" <see cref="LineRenderer" />.
    /// </summary>
    [NeoDoc("Tools/Draw/Drawer.md")]
    [CreateFromMenu("Neoxider/Tools/Other/Drawer")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Drawer))]
    public sealed class Drawer : MonoBehaviour
    {
        [Header("Input")] [Tooltip("If OFF, component ignores all input completely.")]
        public bool isActive = true;

        [SerializeField] [Tooltip("Input camera. If unset, uses the main camera, then the first scene camera.")]
        private Camera cam;

        [Header("Template (Optional)")]
        [Tooltip("Clone this template for non-pooled lines. Drawer visual fields override the template.")]
        public bool useTemplateSettings;

        public LineRenderer templateRenderer;

        [Header("Line Visual")]
        [Tooltip("If set, this material is used. Otherwise Sprite, else Texture2D, else default.")]
        public Material lineMaterial;

        [Tooltip("If material is not set, this sprite (sprite texture) is used.")]
        public Sprite lineSprite;

        [Tooltip("If material and sprite are not set, this texture is used.")]
        public Texture2D lineTexture;

        [FormerlySerializedAs("colour")] public Gradient color = new()
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        };

        [Header("Width")] [Min(0f)] public float lineWidth = 0.1f;
        public bool useWidthCurve;
        public AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 1);

        [Header("Renderer Extras")] public LineTextureMode textureMode = LineTextureMode.Stretch;
        public Vector2 textureScale = Vector2.one;
        public LineAlignment alignment = LineAlignment.View;
        [Range(0, 90)] public int cornerVerts = 10;
        [Range(0, 90)] public int capVerts = 90;
        public bool loop;
        public string sortingLayerName = "Default";
        public int sortingOrder;

        [Header("Algorithms")] public bool useFolow = true;

        [Tooltip("Chaikin smoothing passes each frame.")] [Range(0, 10)]
        public int smoothing = 2;

        public float fixedZ;

        [Min(0)] public int maxPoints = 1000;

        [Min(0)] public float fixedLength;

        [Tooltip("Minimum distance (world units) between raw points.")] [Min(0f)]
        public float minPointDistance = 1f;

        [Header("Lifecycle")]
        [Tooltip("Minimum raw point count required to keep a finished line.")]
        [Min(2)]
        public int minCountCreate = 2;

        [Min(0)] public float minDistanceCreate = 0.2f;

        [Min(0)] public float maxDistanceCreate;

        [Min(0)] public float deleteAfterRelease;

        [Tooltip("Maximum drawing time in seconds while input is held; 0 = unlimited.")] [Min(0)]
        public float drawRelease;

        [Header("Physics")] public bool addCollider;

        public bool colliderAfterCreation;

        [Header("Pooling")] [Tooltip("Use PoolManager for reusing LineRenderer objects")]
        public bool usePooling;

        [Tooltip("LineRenderer prefab for pooling. If not set, created automatically")]
        public LineRenderer poolPrefab;

        public LineCreatedEvent OnLineCreated = new();

        [Tooltip("Reactive total drawn distance; subscribe via Distance.OnChanged")]
        public ReactivePropertyFloat Distance = new();

        [Tooltip("Reactive point count; subscribe via PointCount.OnChanged")]
        public ReactivePropertyInt PointCount = new();

        public UnityEvent<float> OnTimerChanged = new();

        public UnityEvent<float> OnRemainingPercent = new();

        [Tooltip("Event: line drawing start (start point)")]
        public UnityEvent<Vector3> OnLineStarted = new();

        public List<LineRenderer> lines = new();

        // WHY: stroke buffers are grow-only so a warm stroke rebuilds geometry without managed allocations;
        // anything above this element count is released when the stroke ends or is reset, deleted or
        // returned to its pool, so one huge stroke does not pin its peak buffers.
        private const int RetainedBufferLimit = 16384;

        private static readonly List<Vector2> SharedColliderPoints = new();

        private readonly List<Vector3> _processedPoints = new();
        private readonly List<Vector2> _colliderPoints = new();
        private GameObject _autoPoolPrefab;
        private Vector3[] _cachedPositionsArray;
        private Vector3[] _rendererPositions;
        private bool _liveColliderDirty;
        private Matrix4x4 _liveColliderWorldToLocal;

        private LineRenderer _currentLR;
        private EdgeCollider2D _liveCol;
        private readonly Dictionary<LineRenderer, LineResources> _lineResources = new();
        private readonly Dictionary<LineRenderer, CoroutineExtensions.CoroutineHandle> _pendingDeletes = new();
        private bool _isDestroying;
        private int _strokeVersion;
        private float _timer;

        /// <summary>Camera used to project screen input onto the fixed-Z drawing plane.</summary>
        public Camera InputCamera
        {
            get => cam;
            set => cam = value;
        }

        public float DistanceValue => Distance.CurrentValue;

        /// <summary>Point count (for NeoCondition and reflection).</summary>
        public int PointCountValue => PointCount.CurrentValue;

        public int CountPoints => rawPoints.Count;

        public float Timer
        {
            get => _timer;
            set
            {
                int version = _strokeVersion;
                LineRenderer line = _currentLR;
                _timer = value;
                OnTimerChanged?.Invoke(value);

                if (_strokeVersion != version || _currentLR != line || (line != null && !CanAccessLine(line)))
                {
                    return;
                }

                if (drawRelease > 0)
                {
                    float progress = Mathf.Clamp01(1 - value / drawRelease);
                    OnRemainingPercent?.Invoke(progress);
                }
            }
        }

        public List<Vector3> rawPoints { get; } = new();

        private void Awake()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam == null)
            {
                cam = FindFirstObjectByType<Camera>();
                if (cam == null)
                {
                    NeoDiagnostics.LogError(
                        $"[Drawer] Camera not found on {gameObject.name}. Component will be disabled.",
                        this);
                    enabled = false;
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            _isDestroying = true;
            DeleteAll();
            // WHY: CreateLineRenderer can also be used without BeginLine/EndLine, so lines outside `lines` own resources too.
            foreach (LineRenderer lr in new List<LineRenderer>(_lineResources.Keys))
            {
                Delete(lr);
            }

            // WHY: this source is unique to this Drawer. Assigned/shared prefab pools are never retired here.
            SpawnUtility.RetirePool(_autoPoolPrefab);
            _autoPoolPrefab.SafeDestroy();
        }

        private void Update()
        {
            if (!isActive)
            {
                return;
            }

            if (CanAccessLine(_currentLR))
            {
                Timer += Time.deltaTime;
            }

            // WHY: MouseInputCompat keeps the component working when only the Input System package is active,
            // where every legacy Input call throws.
            bool hasPointer = MouseInputCompat.TryGetPosition(out Vector3 pointer);
            if (MouseInputCompat.TryGetButtonDown(0, out bool pressed) && pressed && hasPointer
                && TryScreenToWorld(pointer, out Vector3 start))
            {
                BeginLine(start);
            }

            if (MouseInputCompat.TryGetButton(0, out bool held) && held)
            {
                if (hasPointer && TryScreenToWorld(pointer, out Vector3 worldPos))
                {
                    AppendPoint(worldPos);
                    Folow(worldPos);
                }

                if (drawRelease > 0 && Timer >= drawRelease)
                {
                    EndLine();
                }
            }

            if (MouseInputCompat.TryGetButtonUp(0, out bool released) && released)
            {
                EndLine();
            }
        }

        private void Folow(Vector3 pos)
        {
            if (!useFolow || !CanAccessLine(_currentLR) || _currentLR.positionCount == 0)
            {
                return;
            }

            pos.z = fixedZ;
            int last = _currentLR.positionCount - 1;
            if (_currentLR.GetPosition(last) != pos)
            {
                _currentLR.SetPosition(last, pos);
                _liveColliderDirty = true;
            }

            if (_liveCol != null && (_liveColliderDirty
                                     || _liveCol.transform.worldToLocalMatrix != _liveColliderWorldToLocal))
            {
                SyncColliderWithCurrentLine(_liveCol);
            }
        }

        /// <summary>
        ///     Initializes a new line by creating the LineRenderer and setting up initial conditions.
        /// </summary>
        /// <param name="position">The starting world position of the line.</param>
        public void BeginLine(Vector3 position)
        {
            if (_isDestroying)
            {
                return;
            }

            EndLine();
            if (_currentLR != null)
            {
                Delete(_currentLR);
                if (_currentLR != null)
                {
                    return;
                }
            }

            _currentLR = CreateLineRenderer();
            _liveCol = null;
            LineRenderer line = _currentLR;
            int version = ResetPointsCore();
            if (_strokeVersion != version || _currentLR != line || !CanAccessLine(line))
            {
                return;
            }

            position.z = fixedZ;
            AppendPoint(position);
            if (_strokeVersion != version || _currentLR != line || !CanAccessLine(line))
            {
                return;
            }

            if (addCollider && !colliderAfterCreation)
            {
                CreateCollider();
            }

            Timer = 0;
            if (_strokeVersion != version || _currentLR != line || !CanAccessLine(line))
            {
                return;
            }

            OnLineStarted?.Invoke(position);
        }

        /// <summary>
        ///     Clears any existing points and resets the LineRenderer's position count.
        /// </summary>
        public void ResetPoints()
        {
            ResetPointsCore();
        }

        private int ResetPointsCore()
        {
            int version = ++_strokeVersion;
            float previousDistance = Distance.CurrentValue;
            int previousCount = PointCount.CurrentValue;
            rawPoints.Clear();
            ReleaseOversizedBuffers();
            if (CanAccessLine(_currentLR))
            {
                _currentLR.positionCount = 0;
                if (_liveCol != null)
                {
                    _liveCol.enabled = false;
                }
            }

            // WHY: both metrics and geometry must describe the reset before either callback runs.
            Distance.SetValueWithoutNotify(0f);
            PointCount.SetValueWithoutNotify(0);
            if (previousDistance != 0f)
            {
                Distance.ForceNotify();
            }

            if (_strokeVersion != version)
            {
                return version;
            }

            if (previousCount != 0)
            {
                PointCount.ForceNotify();
            }

            return version;
        }

        /// <summary>
        ///     Creates and configures a LineRenderer. The source of settings depends on the
        ///     flag <see cref="useTemplateSettings" />.
        /// </summary>
        public LineRenderer CreateLineRenderer()
        {
            if (_isDestroying)
            {
                return null;
            }

            LineRenderer lr = null;

            if (usePooling && PoolManager.I != null)
            {
                GameObject prefab = GetPoolPrefab();
                if (prefab != null)
                {
                    GameObject pooled = PoolManager.Get(prefab, Vector3.zero, Quaternion.identity, transform);
                    if (pooled != null)
                    {
                        lr = pooled.GetComponent<LineRenderer>();
                        if (lr == null || !TryTrackLine(lr))
                        {
                            // WHY: the one cleanup slot may already belong to another lifecycle participant.
                            // Return the untouched rental and use an ordinary renderer instead.
                            PoolManager.Release(pooled);
                            lr = null;
                        }
                    }
                }
            }

            if (lr == null && useTemplateSettings && templateRenderer != null)
            {
                lr = Instantiate(templateRenderer, transform);
                lr.name = $"Line {Time.frameCount}";
                lr.transform.localPosition = Vector3.zero;
                lr.transform.localRotation = Quaternion.identity;
                lr.transform.localScale = Vector3.one;
            }
            else if (lr == null)
            {
                GameObject go = new($"Line {Time.frameCount}");
                go.transform.SetParent(transform);
                lr = go.AddComponent<LineRenderer>();
            }

            if (!_lineResources.TryGetValue(lr, out LineResources resources))
            {
                if (!TryTrackLine(lr))
                {
                    lr.gameObject.SafeDestroy();
                    return null;
                }

                resources = _lineResources[lr];
            }

            // WHY: all drawing APIs accept world positions, including template and pooled renderers.
            lr.useWorldSpace = true;
            lr.positionCount = 0;
            if (lr.TryGetComponent(out EdgeCollider2D previousCollider))
            {
                previousCollider.enabled = false;
            }

            // WHY: a caller may have returned this instance directly through PoolManager.
            lines.Remove(lr);
            if (_pendingDeletes.TryGetValue(lr, out CoroutineExtensions.CoroutineHandle previousDelete))
            {
                _pendingDeletes.Remove(lr);
                previousDelete.Stop();
            }

            Material originalMaterial = lr.sharedMaterial;
            Material sourceMaterial = lineMaterial != null ? lineMaterial : originalMaterial;
            Material runtimeMat = null;
            if (lineMaterial != null)
            {
                runtimeMat = new Material(lineMaterial);
                if (lineSprite != null)
                {
                    runtimeMat.mainTexture = lineSprite.texture;
                }
                else if (lineTexture != null)
                {
                    runtimeMat.mainTexture = lineTexture;
                }

                lr.sharedMaterial = runtimeMat;
            }
            else if (lineSprite != null)
            {
                runtimeMat = sourceMaterial != null
                    ? new Material(sourceMaterial)
                    : new Material(Shader.Find("Sprites/Default"));
                runtimeMat.mainTexture = lineSprite.texture;
                lr.sharedMaterial = runtimeMat;
            }
            else if (lineTexture != null)
            {
                runtimeMat = sourceMaterial != null
                    ? new Material(sourceMaterial)
                    : new Material(Shader.Find("Sprites/Default"));
                runtimeMat.mainTexture = lineTexture;
                lr.sharedMaterial = runtimeMat;
            }
            else if (sourceMaterial == null)
            {
                runtimeMat = new Material(Shader.Find("Sprites/Default"));
                lr.sharedMaterial = runtimeMat;
            }

            resources.OwnedMaterial = runtimeMat;
            resources.OriginalMaterial = originalMaterial;

            ApplyFallbackSettings(lr);
            return lr;
        }

        /// <summary>
        ///     Gets or creates the prefab used for pooling.
        /// </summary>
        private GameObject GetPoolPrefab()
        {
            if (poolPrefab != null)
            {
                return poolPrefab.gameObject;
            }

            if (_autoPoolPrefab != null)
            {
                return _autoPoolPrefab;
            }

            _autoPoolPrefab = new GameObject("LineRenderer_PoolPrefab");
            _autoPoolPrefab.SetActive(false);
            LineRenderer lr = _autoPoolPrefab.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            ApplyFallbackSettings(lr);
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(_autoPoolPrefab);
            }

            return _autoPoolPrefab;
        }

        /// <summary>
        ///     Applies settings defined in the inspector to a LineRenderer when no template is used.
        /// </summary>
        public void ApplyFallbackSettings(LineRenderer lr)
        {
            if (!CanAccessLine(lr))
            {
                return;
            }

            lr.colorGradient = color;
            lr.widthCurve = useWidthCurve ? widthCurve : AnimationCurve.Linear(0, 1, 1, 1);
            lr.widthMultiplier = lineWidth;

            lr.textureMode = textureMode;
            lr.textureScale = textureScale;
            lr.alignment = alignment;
            lr.numCornerVertices = cornerVerts;
            lr.numCapVertices = capVerts;
            lr.loop = loop;
            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        /// <summary>
        ///     Adds a new point to the line, applying smoothing if configured.
        /// </summary>
        /// <param name="worldPos">The world position of the point to add.</param>
        public void AppendPoint(Vector3 worldPos)
        {
            if (!CanAccessLine(_currentLR))
            {
                return;
            }

            worldPos.z = fixedZ;
            Vector3 pos = worldPos;

            if (CountPoints == 0)
            {
                rawPoints.Add(pos);
                UpdateLinePositions(rawPoints);
                PointCount.Value = rawPoints.Count;
                return;
            }

            Vector3 last = rawPoints[^1];

            if (fixedLength > 0)
            {
                Vector3 dir = worldPos - last;
                if (dir.sqrMagnitude < 1e-6f)
                {
                    return;
                }

                float sqrDistance = dir.sqrMagnitude;
                float sqrFixedLength = fixedLength * fixedLength;

                if (sqrDistance >= sqrFixedLength)
                {
                    pos = last + dir.normalized * fixedLength;
                }
                else
                {
                    return;
                }
            }
            else
            {
                float sqrDistance = (worldPos - last).sqrMagnitude;
                float sqrMinDistance = minPointDistance * minPointDistance;
                if (sqrDistance < sqrMinDistance)
                {
                    return;
                }
            }

            rawPoints.Add(pos);
            int previousCount = PointCount.CurrentValue;
            float previousDistance = Distance.CurrentValue;
            PointCount.SetValueWithoutNotify(rawPoints.Count);
            Distance.SetValueWithoutNotify(previousDistance + Vector3.Distance(last, pos));
            LineRenderer line = _currentLR;
            LineResources resources = _lineResources[line];
            int version = _strokeVersion;

            Smooth(rawPoints, smoothing, fixedZ, _processedPoints);
            LimitPoints(_processedPoints, maxPoints, _processedPoints);
            UpdateLinePositions(_processedPoints);

            if (useFolow)
            {
                _currentLR.SetPosition(_currentLR.positionCount - 1, pos);
            }

            if (addCollider && !colliderAfterCreation && _liveCol)
            {
                // WHY: the renderer buffer already holds the processed stroke, including the followed endpoint.
                WriteColliderPoints(_liveCol, _cachedPositionsArray, _processedPoints.Count, _colliderPoints);
                _liveColliderDirty = false;
                _liveColliderWorldToLocal = _liveCol.transform.worldToLocalMatrix;
            }

            // WHY: publish only after geometry and both metrics are committed; a listener may end/reset the stroke.
            if (PointCount.CurrentValue != previousCount)
            {
                PointCount.ForceNotify();
            }

            if (_strokeVersion != version || !CanAccessLine(line, resources))
            {
                return;
            }

            if (Distance.CurrentValue != previousDistance)
            {
                Distance.ForceNotify();
            }

            if (_currentLR != line || _strokeVersion != version || !CanAccessLine(line, resources))
            {
                return;
            }

            if (maxDistanceCreate > 0 && Distance.CurrentValue >= maxDistanceCreate)
            {
                EndLine();
            }
        }

        /// <summary>
        ///     Writes world-space <paramref name="src" /> points into <paramref name="col" /> (collider-local space).
        ///     Fewer than two points disable the collider. Does not allocate once its shared buffer is warm.
        /// </summary>
        public static void UpdateColliderPoints(EdgeCollider2D col, List<Vector3> src)
        {
            col.enabled = src.Count >= 2;
            if (!col.enabled)
            {
                return;
            }

            Matrix4x4 worldToLocal = col.transform.worldToLocalMatrix;
            SharedColliderPoints.Clear();
            for (int i = 0; i < src.Count; i++)
            {
                SharedColliderPoints.Add(worldToLocal.MultiplyPoint3x4(src[i]));
            }

            col.SetPoints(SharedColliderPoints);
            SharedColliderPoints.Clear();
            if (SharedColliderPoints.Capacity > RetainedBufferLimit)
            {
                SharedColliderPoints.Capacity = 0;
            }
        }

        /// <summary>
        ///     Evenly selects <= Max points from the list.
        ///     0 or 1 -> returns the source list (without restriction).
        /// </summary>
        public static List<Vector3> LimitPoints(List<Vector3> pts, int max)
        {
            if (max <= 1 || pts.Count <= max)
            {
                return pts;
            }

            List<Vector3> result = new(max);
            LimitPoints(pts, max, result);
            return result;
        }

        /// <summary>
        ///     Non-allocating form of <see cref="LimitPoints(List{Vector3}, int)" />: writes the evenly sampled points
        ///     into <paramref name="results" />. <paramref name="results" /> may be the same list as
        ///     <paramref name="points" />; the selection is then done in place.
        /// </summary>
        /// <param name="points">Source points.</param>
        /// <param name="max">Maximum point count; 0 or 1 keeps every point.</param>
        /// <param name="results">Destination list; cleared first unless it is <paramref name="points" />.</param>
        public static void LimitPoints(List<Vector3> points, int max, List<Vector3> results)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (!ReferenceEquals(points, results))
            {
                results.Clear();
                results.AddRange(points);
            }

            int count = results.Count;
            if (max <= 1 || count <= max)
            {
                return;
            }

            if (max == 2)
            {
                results[1] = results[count - 1];
                results.RemoveRange(2, count - 2);
                return;
            }

            // WHY: in place is safe because every sampled index is >= its destination index (step > 1),
            // so a source slot is always read before any write can reach it.
            float step = (count - 1f) / (max - 1);
            for (int i = 0; i < max; i++)
            {
                results[i] = results[Mathf.RoundToInt(i * step)];
            }

            results.RemoveRange(max, count - max);
        }

        /// <summary>
        ///     Finalizes and processes the line, including optional collider generation.
        /// </summary>
        public void EndLine()
        {
            if (!CanAccessLine(_currentLR))
            {
                return;
            }

            ReleaseOversizedBuffers();
            if (rawPoints.Count < minCountCreate ||
                (minDistanceCreate > 0 && Distance.CurrentValue < minDistanceCreate))
            {
                // WHY: Delete releases pooled instances instead of destroying them; destroying one would
                // leave the pool handing back a dead object on the next stroke.
                Delete(_currentLR);
                return;
            }

            // WHY: ensure collider gets the smoothed version
            if (addCollider && colliderAfterCreation)
            {
                CreateCollider();
            }

            LineRenderer line = _currentLR;
            _currentLR = null;
            _liveCol = null;
            rawPoints.Clear();
            ReleaseOversizedBuffers();
            lines.Add(line);

            if (deleteAfterRelease > 0f)
            {
                LineResources resources = _lineResources[line];
                _pendingDeletes[line] = this.Delay(deleteAfterRelease, () =>
                {
                    if (_lineResources.TryGetValue(line, out LineResources current)
                        && ReferenceEquals(current, resources))
                    {
                        _pendingDeletes.Remove(line);
                    }

                    DeleteLine(line, resources);
                });
            }

            // WHY: raised last, so listeners can begin another stroke or delete this line safely.
            OnLineCreated?.Invoke(line);
        }

        public EdgeCollider2D CreateCollider()
        {
            if (!CanAccessLine(_currentLR))
            {
                return null;
            }

            _liveCol = _currentLR.GetOrAdd<EdgeCollider2D>();
            SyncColliderWithCurrentLine(_liveCol);
            _liveCol.edgeRadius = Mathf.Max(0f,
                lineWidth * (useWidthCurve ? widthCurve.Evaluate(1f) : 1f) * 0.5f);
            return _liveCol;
        }

        private void SyncColliderWithCurrentLine(EdgeCollider2D col)
        {
            int count = _currentLR.positionCount;
            EnsureCapacity(ref _rendererPositions, count);
            if (count > 0)
            {
                _currentLR.GetPositions(_rendererPositions);
            }

            WriteColliderPoints(col, _rendererPositions, count, _colliderPoints);
            _liveColliderDirty = false;
            _liveColliderWorldToLocal = col.transform.worldToLocalMatrix;
        }

        private static void WriteColliderPoints(EdgeCollider2D col, Vector3[] source, int count,
            List<Vector2> buffer)
        {
            col.enabled = count >= 2;
            if (!col.enabled)
            {
                return;
            }

            Matrix4x4 worldToLocal = col.transform.worldToLocalMatrix;
            buffer.Clear();
            for (int i = 0; i < count; i++)
            {
                buffer.Add(worldToLocal.MultiplyPoint3x4(source[i]));
            }

            col.SetPoints(buffer);
        }

        private static void EnsureCapacity(ref Vector3[] buffer, int count)
        {
            if (buffer != null && buffer.Length >= count)
            {
                return;
            }

            int size = buffer == null ? 16 : buffer.Length;
            while (size < count)
            {
                size *= 2;
            }

            buffer = new Vector3[size];
        }

        private void ReleaseOversizedBuffers()
        {
            if (rawPoints.Count == 0 && rawPoints.Capacity > RetainedBufferLimit)
            {
                rawPoints.Capacity = 0;
            }

            if (_processedPoints.Capacity > RetainedBufferLimit)
            {
                _processedPoints.Clear();
                _processedPoints.Capacity = 0;
            }

            if (_colliderPoints.Capacity > RetainedBufferLimit)
            {
                _colliderPoints.Clear();
                _colliderPoints.Capacity = 0;
            }

            if (_cachedPositionsArray != null && _cachedPositionsArray.Length > RetainedBufferLimit)
            {
                _cachedPositionsArray = null;
            }

            if (_rendererPositions != null && _rendererPositions.Length > RetainedBufferLimit)
            {
                _rendererPositions = null;
            }
        }

        private bool TryTrackLine(LineRenderer lr)
        {
            LineResources resources = new(lr.GetComponent<PooledObjectInfo>());
            _lineResources[lr] = resources;
            if (resources.Pool != null && !resources.Info.TryRegisterReleaseCleanup(resources.Pool,
                    resources.Generation, instance => ReleaseLineResources(lr, resources)))
            {
                _lineResources.Remove(lr);
                return false;
            }

            return true;
        }

        private bool CanAccessLine(LineRenderer lr)
        {
            if (ReferenceEquals(lr, null))
            {
                return false;
            }

            _lineResources.TryGetValue(lr, out LineResources resources);
            return CanAccessLine(lr, resources);
        }

        private bool CanAccessLine(LineRenderer lr, LineResources resources)
        {
            if (lr == null)
            {
                return false;
            }

            if (resources != null)
            {
                return !resources.Disposed && _lineResources.TryGetValue(lr, out LineResources current)
                       && ReferenceEquals(current, resources)
                       && (resources.Pool == null || (resources.MatchesGeneration && !resources.Info.IsInPool));
            }

            // WHY: unknown pooled instances can belong to a newer rental after our cleanup removed its record.
            return !lr.TryGetComponent(out PooledObjectInfo info) || info.OwnerPool == null;
        }

        private void CancelPendingDelete(LineRenderer lr)
        {
            if (_pendingDeletes.TryGetValue(lr, out CoroutineExtensions.CoroutineHandle handle))
            {
                _pendingDeletes.Remove(lr);
                handle.Stop();
            }
        }

        private void ReleaseLineResources(LineRenderer lr, LineResources resources)
        {
            if (resources.Disposed)
            {
                return;
            }

            resources.Disposed = true;
            if (_lineResources.TryGetValue(lr, out LineResources current) && ReferenceEquals(current, resources))
            {
                _lineResources.Remove(lr);
                lines.Remove(lr);
                CancelPendingDelete(lr);
                if (_currentLR == lr)
                {
                    _currentLR = null;
                    _liveCol = null;
                    _strokeVersion++;
                    rawPoints.Clear();
                    ReleaseOversizedBuffers();
                    // WHY: pool cleanup cannot invoke user callbacks inside its transitioning rental.
                    Distance.SetValueWithoutNotify(0f);
                    PointCount.SetValueWithoutNotify(0);
                }
            }

            // WHY: release hooks run with IsInPool=true, before reuse/eviction. Check the captured generation,
            // not active-rental status, and restore only the exact clone we installed.
            if (lr != null && (resources.Pool == null || resources.MatchesGeneration)
                           && resources.OwnedMaterial != null && lr.sharedMaterial == resources.OwnedMaterial)
            {
                lr.sharedMaterial = resources.OriginalMaterial;
            }

            resources.OwnedMaterial.SafeDestroy();
        }

        /// <summary>
        ///     Destroys the provided LineRenderer and removes it from the list of lines.
        /// </summary>
        public void Delete(LineRenderer lr)
        {
            if (ReferenceEquals(lr, null))
            {
                return;
            }

            _lineResources.TryGetValue(lr, out LineResources resources);
            DeleteLine(lr, resources);
        }

        private void DeleteLine(LineRenderer lr, LineResources resources)
        {
            if (_lineResources.TryGetValue(lr, out LineResources current) && !ReferenceEquals(current, resources))
            {
                return;
            }

            lines.Remove(lr);
            CancelPendingDelete(lr);
            if (_currentLR == lr)
            {
                _currentLR = null;
                _liveCol = null;
                ResetPoints();
            }

            if (lr == null)
            {
                if (resources != null)
                {
                    ReleaseLineResources(lr, resources);
                }

                return;
            }

            // WHY: reset listeners can return/re-rent this same renderer; never act on their replacement.
            if (!CanAccessLine(lr, resources))
            {
                return;
            }

            if (resources != null && resources.Pool != null)
            {
                resources.Pool.ReturnObject(lr.gameObject);
            }
            else
            {
                if (resources != null)
                {
                    ReleaseLineResources(lr, resources);
                }

                lr.gameObject.SafeDestroy();
            }
        }

        /// <summary>
        ///     Deletes the first line in the list, if any exist.
        /// </summary>
        public void DeleteFirst()
        {
            if (lines.Count == 0)
            {
                return;
            }

            Delete(lines[0]);
        }

        /// <summary>
        ///     Deletes the last line in the list, if any exist.
        /// </summary>
        public void DeleteLast()
        {
            if (lines.Count == 0)
            {
                return;
            }

            Delete(lines[^1]);
        }

        /// <summary>
        ///     Destroys all lines and clears the list of LineRenderers.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void DeleteAll()
        {
            List<(LineRenderer Line, LineResources Resources)> snapshot = new();
            foreach (LineRenderer lr in new List<LineRenderer>(lines))
            {
                if (!ReferenceEquals(lr, null))
                {
                    _lineResources.TryGetValue(lr, out LineResources resources);
                    snapshot.Add((lr, resources));
                }
                else
                {
                    lines.Remove(lr);
                }
            }

            if (!ReferenceEquals(_currentLR, null))
            {
                _lineResources.TryGetValue(_currentLR, out LineResources resources);
                snapshot.Add((_currentLR, resources));
            }

            foreach ((LineRenderer Line, LineResources Resources) entry in snapshot)
            {
                DeleteLine(entry.Line, entry.Resources);
            }

            if (_currentLR == null)
            {
                ResetPoints();
            }
        }

        /// <summary>
        ///     Converts a screen position to its intersection with the fixed-Z drawing plane.
        /// </summary>
        /// <param name="screen">The screen space coordinates.</param>
        /// <returns>The converted world space position.</returns>
        public Vector3 ScreenToWorld(Vector3 screen)
        {
            if (!TryScreenToWorld(screen, out Vector3 world))
            {
                throw new InvalidOperationException("Drawer needs an input camera facing the fixed-Z drawing plane.");
            }

            return world;
        }

        /// <summary>Returns false when no input camera or forward ray/plane intersection is available.</summary>
        public bool TryScreenToWorld(Vector3 screen, out Vector3 world)
        {
            world = default;
            if (cam == null)
            {
                return false;
            }

            Ray ray = cam.ScreenPointToRay(screen);
            Plane plane = new(Vector3.forward, new Vector3(0f, 0f, fixedZ));
            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            world = ray.GetPoint(distance);
            world.z = fixedZ;
            return true;
        }

        /// <summary>
        ///     Applies the Chaikin smoothing algorithm to a list of points over multiple passes.
        /// </summary>
        public static List<Vector3> Smooth(List<Vector3> points, int passes, float? fixedZ = 0)
        {
            List<Vector3> result = new(points.Count);
            Smooth(points, passes, fixedZ, result);
            return result;
        }

        /// <summary>
        ///     Non-allocating form of <see cref="Smooth(List{Vector3}, int, float?)" />: writes the smoothed copy of
        ///     <paramref name="points" /> into <paramref name="results" /> (cleared first). Each pass doubles the point
        ///     count; fewer than three points are copied unsmoothed. <paramref name="points" /> is never modified unless
        ///     it is also passed as <paramref name="results" />.
        /// </summary>
        /// <param name="points">Source polyline.</param>
        /// <param name="passes">Chaikin passes; 0 or less only copies (and flattens Z when requested).</param>
        /// <param name="fixedZ">When set, every point is placed on this Z before smoothing.</param>
        /// <param name="results">Destination list, reused across calls to avoid allocations.</param>
        public static void Smooth(List<Vector3> points, int passes, float? fixedZ, List<Vector3> results)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (!ReferenceEquals(points, results))
            {
                results.Clear();
                results.AddRange(points);
            }

            if (fixedZ.HasValue)
            {
                float z = fixedZ.Value;
                for (int i = 0; i < results.Count; i++)
                {
                    Vector3 point = results[i];
                    point.z = z;
                    results[i] = point;
                }
            }

            if (passes <= 0 || results.Count < 3)
            {
                return;
            }

            for (int pass = 0; pass < passes; pass++)
            {
                ChaikinPassInPlace(results);
            }
        }

        private static void ChaikinPassInPlace(List<Vector3> points)
        {
            int count = points.Count;
            int expandedCount = count * 2;
            Vector3 last = points[count - 1];
            for (int i = count; i < expandedCount; i++)
            {
                points.Add(last);
            }

            // WHY: walking backwards writes pair i into slots 2i+1 and 2i+2, which are always above the
            // inputs i and i+1 still to be read, so one buffer serves as both source and destination.
            for (int i = count - 2; i >= 0; i--)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                points[2 * i + 1] = Vector3.Lerp(a, b, 0.25f);
                points[2 * i + 2] = Vector3.Lerp(a, b, 0.75f);
            }
        }

        public static float GetDistance(Vector3[] points)
        {
            float distance = 0;

            for (int i = 1; i < points.Length; i++)
            {
                distance += Vector3.Distance(points[i - 1], points[i]);
            }

            return distance;
        }

        public static float GetDistance(List<Vector3> points)
        {
            float distance = 0;

            for (int i = 1; i < points.Count; i++)
            {
                distance += Vector3.Distance(points[i - 1], points[i]);
            }

            return distance;
        }

        /// <summary>
        ///     Updates LineRenderer positions using cached array to avoid allocations.
        /// </summary>
        private void UpdateLinePositions(List<Vector3> positions)
        {
            if (!CanAccessLine(_currentLR))
            {
                return;
            }

            int count = positions.Count;
            EnsureCapacity(ref _cachedPositionsArray, count);
            for (int i = 0; i < count; i++)
            {
                _cachedPositionsArray[i] = positions[i];
            }

            // WHY: SetPositions ignores entries at or beyond positionCount, so the grow-only buffer can be larger.
            _currentLR.positionCount = count;
            _currentLR.SetPositions(_cachedPositionsArray);
            _liveColliderDirty = true;
        }

        private sealed class LineResources
        {
            public readonly PooledObjectInfo Info;
            public readonly NeoObjectPool Pool;
            public readonly int Generation;
            public Material OwnedMaterial;
            public Material OriginalMaterial;
            public bool Disposed;

            public LineResources(PooledObjectInfo info)
            {
                Info = info;
                Pool = info != null ? info.OwnerPool : null;
                Generation = info != null ? info.SpawnGeneration : 0;
            }

            public bool MatchesGeneration => Info != null && Info.OwnerPool == Pool
                                                         && Info.SpawnGeneration == Generation;
        }

        [Serializable]
        public class LineCreatedEvent : UnityEvent<LineRenderer>
        {
        }
    }
}
