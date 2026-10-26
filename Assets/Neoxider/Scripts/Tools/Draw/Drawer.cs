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

        private GameObject _autoPoolPrefab;
        private Vector3[] _cachedPositionsArray;

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
            // CreateLineRenderer can also be used without BeginLine/EndLine.
            foreach (LineRenderer lr in new List<LineRenderer>(_lineResources.Keys))
            {
                Delete(lr);
            }

            // This source is unique to this Drawer. Assigned/shared prefab pools are never retired here.
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

            if (Input.GetMouseButtonDown(0) && TryScreenToWorld(Input.mousePosition, out Vector3 start))
            {
                BeginLine(start);
            }

            if (Input.GetMouseButton(0))
            {
                if (TryScreenToWorld(Input.mousePosition, out Vector3 worldPos))
                {
                    AppendPoint(worldPos);
                    Folow(worldPos);
                }

                if (drawRelease > 0 && Timer >= drawRelease)
                {
                    EndLine();
                }
            }

            if (Input.GetMouseButtonUp(0))
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
            _currentLR.SetPosition(_currentLR.positionCount - 1, pos);
            if (_liveCol != null)
            {
                UpdateColliderPoints(_liveCol, new List<Vector3>(GetCurrentWorldPositions()));
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
            if (CanAccessLine(_currentLR))
            {
                _currentLR.positionCount = 0;
                if (_liveCol != null)
                {
                    _liveCol.enabled = false;
                }
            }

            // Both metrics and geometry must describe the reset before either callback runs.
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
                            // The one cleanup slot may already belong to another lifecycle participant.
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

            // All drawing APIs accept world positions, including template and pooled renderers.
            lr.useWorldSpace = true;
            lr.positionCount = 0;
            if (lr.TryGetComponent(out EdgeCollider2D previousCollider))
            {
                previousCollider.enabled = false;
            }

            // A caller may have returned this instance directly through PoolManager.
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

            List<Vector3> processed = LimitPoints(Smooth(rawPoints, smoothing, fixedZ), maxPoints);
            UpdateLinePositions(processed);

            if (useFolow)
            {
                _currentLR.SetPosition(_currentLR.positionCount - 1, pos);
            }

            if (addCollider && !colliderAfterCreation && _liveCol)
            {
                UpdateColliderPoints(_liveCol, processed);
            }

            // Publish only after geometry and both metrics are committed. A listener may end/reset the stroke.
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

        public static void UpdateColliderPoints(EdgeCollider2D col, List<Vector3> src)
        {
            col.enabled = src.Count >= 2;
            if (!col.enabled)
            {
                return;
            }

            Vector2[] pts2D = new Vector2[src.Count];
            for (int i = 0; i < src.Count; i++)
            {
                pts2D[i] = col.transform.InverseTransformPoint(src[i]);
            }

            col.points = pts2D;
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

            if (max == 2)
            {
                return new List<Vector3> { pts[0], pts[^1] };
            }

            List<Vector3> result = new(max);
            float step = (pts.Count - 1f) / (max - 1);

            for (int i = 0; i < max; i++)
            {
                int idx = Mathf.RoundToInt(i * step);
                result.Add(pts[idx]);
            }

            return result;
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

            // Listeners can begin another stroke or delete this line safely.
            OnLineCreated?.Invoke(line);
        }

        public EdgeCollider2D CreateCollider()
        {
            if (!CanAccessLine(_currentLR))
            {
                return null;
            }

            _liveCol = _currentLR.GetOrAdd<EdgeCollider2D>();
            UpdateColliderPoints(_liveCol, new List<Vector3>(GetCurrentWorldPositions()));
            _liveCol.edgeRadius = Mathf.Max(0f,
                lineWidth * (useWidthCurve ? widthCurve.Evaluate(1f) : 1f) * 0.5f);
            return _liveCol;
        }

        private Vector3[] GetCurrentWorldPositions()
        {
            if (!CanAccessLine(_currentLR))
            {
                return Array.Empty<Vector3>();
            }

            Vector3[] positions = new Vector3[_currentLR.positionCount];
            for (int i = 0; i < _currentLR.positionCount; i++)
            {
                positions[i] = _currentLR.GetPosition(i);
            }

            return positions;
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

            // Unknown pooled instances can belong to a newer rental after our cleanup removed its record.
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
                    // Pool cleanup cannot invoke user callbacks inside its transitioning rental.
                    Distance.SetValueWithoutNotify(0f);
                    PointCount.SetValueWithoutNotify(0);
                }
            }

            // Release hooks run with IsInPool=true, before reuse/eviction. Check the captured generation,
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

            // Reset listeners can return/re-rent this same renderer. Never act on their replacement.
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
            Vector3[] input = points.ToArray();
            if (fixedZ.HasValue)
            {
                for (int i = 0; i < input.Length; i++)
                {
                    input[i].z = fixedZ.Value;
                }
            }

            if (passes <= 0 || points.Count < 3)
            {
                return new List<Vector3>(input);
            }

            Vector3[] output = null;

            for (int p = 0; p < passes; p++)
            {
                int newSize = 1 + (input.Length - 1) * 2 + 1;
                if (output == null || output.Length != newSize)
                {
                    output = new Vector3[newSize];
                }

                output[0] = input[0];
                int idx = 1;

                for (int i = 0; i < input.Length - 1; i++)
                {
                    Vector3 a = input[i];
                    Vector3 b = input[i + 1];

                    output[idx++] = Vector3.Lerp(a, b, 0.25f);
                    output[idx++] = Vector3.Lerp(a, b, 0.75f);
                }

                output[idx] = input[^1];

                (input, output) = (output, input);
            }

            return new List<Vector3>(input);
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

            if (_cachedPositionsArray == null || _cachedPositionsArray.Length != positions.Count)
            {
                _cachedPositionsArray = new Vector3[positions.Count];
            }

            for (int i = 0; i < positions.Count; i++)
            {
                _cachedPositionsArray[i] = positions[i];
            }

            _currentLR.positionCount = positions.Count;
            _currentLR.SetPositions(_cachedPositionsArray);
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
