using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Sprites;
using UnityEngine.U2D;

namespace Neo.UI
{
    /// <summary>
    /// UI Mesh Rig output for a plain <see cref="SpriteRenderer"/> — sorting layers, 2D lights, sprite
    /// masks and SRP batching keep working, the artwork just deforms.
    /// <para>
    /// <b>The imported Sprite asset is never touched.</b> The component builds a runtime clone with
    /// <see cref="Sprite.Create(Texture2D, Rect, Vector2, float, uint, SpriteMeshType)"/>, rewrites the
    /// clone's geometry, and assigns the clone to the renderer. The source asset keeps its import-time
    /// geometry, so stopping Play Mode (or deleting this component) cannot leave a mutated sprite behind.
    /// </para>
    /// <para>
    /// <b>Why not <c>Sprite.OverrideGeometry</c>.</b> It is public and needs no 2D Animation package, but
    /// it is a no-op on sprites that are not backed by the import pipeline: calling it on a runtime clone
    /// leaves vertex count and positions untouched (measured on 6000.3.14f1). The only way to make it bite
    /// is to call it on the imported asset — exactly the shared-state mutation this adapter must avoid.
    /// The public <see cref="SpriteDataAccessExtensions"/> API used here writes positions, UVs and indices
    /// on any Sprite instance, ships in <c>UnityEngine.CoreModule</c>, and needs no extra package.
    /// </para>
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Neoxider/UI/UI Mesh Rig Sprite Renderer")]
    [NeoDoc("UI/UIMeshRig.md")]
    public sealed class UIMeshRigSpriteRenderer : MonoBehaviour, IUIMeshRigOwner
    {
        [Header("Source")]
        [Tooltip("Sprite asset to deform. It is cloned at runtime and never modified on disk.")]
        [SerializeField] private Sprite _sprite;

        [Tooltip("Tint written to the SpriteRenderer. Matches SpriteRenderer.color.")]
        [SerializeField] private Color _color = Color.white;

        [Header("Deformation Mesh")]
        [Tooltip("Horizontal grid resolution. Below 4 the silhouette cannot bend smoothly.")]
        [Range(2, 40)] [SerializeField] private int _columns = 16;

        [Tooltip("Vertical grid resolution. Below 4 the silhouette cannot bend smoothly.")]
        [Range(2, 40)] [SerializeField] private int _rows = 20;

        [Tooltip("Turns the whole rig off without deleting its points.")]
        [SerializeField] private bool _deformationEnabled = true;

        [Tooltip(
            "Extra culling margin as a fraction of the sprite size. Sprite bounds come from the source " +
            "rect and do not grow with a deformed mesh, so without headroom a strongly warped sprite can " +
            "be culled early at the screen edge.")]
        [Range(0f, 2f)] [SerializeField] private float _boundsHeadroom = 0.25f;

        [Header("Authoring")]
        [Tooltip("Setup edits bind centres and influence ellipses. Pose / Animate deforms the mesh.")]
        [SerializeField] private UIMeshRigAuthoringMode _authoringMode = UIMeshRigAuthoringMode.Setup;

        [Tooltip("Scene-view transform tool used while posing the selected point.")]
        [SerializeField] private UIMeshRigSceneTool _sceneTool = UIMeshRigSceneTool.Move;

        private readonly List<UIMeshRigPoint> _points = new List<UIMeshRigPoint>();
        private readonly List<UIMeshRigPointState> _pointStates = new List<UIMeshRigPointState>();
        private SpriteRenderer _spriteRenderer;
        private Sprite _deformedSprite;
        private Sprite _clonedFrom;
        private float _clonedHeadroom = -1f;
        private int _writtenVertexCount;
        private bool _pointCacheDirty = true;
        private bool _geometryDirty = true;

        public Sprite Sprite => _sprite;
        public int Columns => _columns;
        public int Rows => _rows;
        public bool DeformationEnabled => _deformationEnabled;
        public Color Color => _color;
        public UIMeshRigAuthoringMode AuthoringMode => _authoringMode;
        public UIMeshRigSceneTool SceneTool => _sceneTool;

        /// <summary>The runtime clone currently assigned to the renderer, or null before the first build.</summary>
        public Sprite DeformedSprite => _deformedSprite;

        public IReadOnlyList<UIMeshRigPoint> Points
        {
            get
            {
                EnsurePointCache();
                return _points;
            }
        }

        /// <summary>Sprite size in world units, ignoring any deformation.</summary>
        public Vector2 NativeSize
        {
            get
            {
                if (_sprite == null)
                {
                    return Vector2.zero;
                }

                float pixelsPerUnit = GetPixelsPerUnit();
                return _sprite.rect.size / pixelsPerUnit;
            }
        }

        Transform IUIMeshRigOwner.RigTransform => transform;
        float IUIMeshRigOwner.MotionUnitScale => 1f / GetPixelsPerUnit();
        IReadOnlyList<UIMeshRigPoint> IUIMeshRigOwner.RigPoints => Points;

        public void SetSource(Sprite sprite, Color tint)
        {
            _sprite = sprite;
            _color = tint;
            ReleaseClone();
            MarkGeometryDirty();
        }

        public void SetGridResolution(int columns, int rows)
        {
            _columns = Mathf.Clamp(columns, 2, 40);
            _rows = Mathf.Clamp(rows, 2, 40);
            MarkGeometryDirty();
        }

        public void SetDeformationEnabled(bool enabled)
        {
            _deformationEnabled = enabled;
            MarkGeometryDirty();
        }

        public void SetAuthoringMode(UIMeshRigAuthoringMode mode)
        {
            if (_authoringMode == mode)
            {
                return;
            }

            _authoringMode = mode;
            if (mode == UIMeshRigAuthoringMode.Setup)
            {
                ResetPose();
            }

            MarkGeometryDirty();
        }

        public void SetSceneTool(UIMeshRigSceneTool tool)
        {
            _sceneTool = tool;
        }

        public void CaptureRestPose()
        {
            EnsurePointCache();
            for (int index = 0; index < _points.Count; index++)
            {
                _points[index].CaptureRestPose(this);
            }

            MarkGeometryDirty();
        }

        public void ResetPose()
        {
            EnsurePointCache();
            for (int index = 0; index < _points.Count; index++)
            {
                _points[index].ResetPose(this);
            }

            MarkGeometryDirty();
        }

        /// <summary>
        /// Rebuilds the clone geometry immediately instead of waiting for the next LateUpdate. Useful from
        /// editor tooling and tests, which never get a player loop tick.
        /// </summary>
        public void Rebuild()
        {
            MarkGeometryDirty();
            RebuildIfNeeded();
        }

        public UIMeshRigGeometry BuildGeometry()
        {
            if (_sprite == null)
            {
                return new UIMeshRigGeometry(new Vector3[0], new int[0], new Vector2[0]);
            }

            CollectPointStates();
            Vector4 outerUv = DataUtility.GetOuterUV(_sprite);
            Rect uvRect = Rect.MinMaxRect(outerUv.x, outerUv.y, outerUv.z, outerUv.w);
            return UIMeshRigGeometryBuilder.Build(
                _columns,
                _rows,
                GetCoordinateSpace(),
                uvRect,
                IsDeformationActive(),
                _pointStates);
        }

        public Vector2 WorldToNormalized(Vector3 worldPosition)
        {
            return GetCoordinateSpace().PositionToNormalized(WorldToLocal(worldPosition));
        }

        public Vector3 NormalizedToWorld(Vector2 normalizedPosition)
        {
            return transform.TransformPoint(NormalizedToLocal(normalizedPosition));
        }

        public Vector2 WorldToLocal(Vector3 worldPosition)
        {
            return transform.InverseTransformPoint(worldPosition);
        }

        public Vector2 NormalizedToLocal(Vector2 normalizedPosition)
        {
            return GetCoordinateSpace().NormalizedToPosition(normalizedPosition);
        }

        public float GetRelativeRotationDegrees(Transform point)
        {
            Quaternion relative = Quaternion.Inverse(transform.rotation) * point.rotation;
            return relative.eulerAngles.z;
        }

        public void NotifyPointChanged()
        {
            NotifyBindingChanged();
        }

        public void NotifyBindingChanged()
        {
            _pointCacheDirty = true;
            MarkGeometryDirty();
        }

        public void NotifyPoseChanged()
        {
            MarkGeometryDirty();
        }

        private void OnEnable()
        {
            CacheRenderer();
            _pointCacheDirty = true;
            MarkGeometryDirty();
            RebuildIfNeeded();
        }

        private void OnDisable()
        {
            ReleaseClone();
        }

        private void OnValidate()
        {
            _columns = Mathf.Clamp(_columns, 2, 40);
            _rows = Mathf.Clamp(_rows, 2, 40);
            _boundsHeadroom = Mathf.Clamp(_boundsHeadroom, 0f, 2f);
            _pointCacheDirty = true;
            MarkGeometryDirty();
        }

        private void OnTransformChildrenChanged()
        {
            _pointCacheDirty = true;
            MarkGeometryDirty();
        }

        private void LateUpdate()
        {
            SynchronizePointTransforms();
            RebuildIfNeeded();
        }

        private void SynchronizePointTransforms()
        {
            EnsurePointCache();
            bool changed = false;
            for (int index = 0; index < _points.Count; index++)
            {
                UIMeshRigPoint point = _points[index];
                if (!point.transform.hasChanged)
                {
                    continue;
                }

                // WHY: see UIMeshRigGraphic.SynchronizePointTransforms — rebinding writes serialized
                // authoring data with no Undo entry, and the Edit Mode preview ticks this every frame.
                // A previewed point is never rebound; stop the preview to edit the bind.
                if (!Application.isPlaying && _authoringMode == UIMeshRigAuthoringMode.Setup &&
                    !point.HasProceduralPose)
                {
                    point.CaptureRestPose(this);
                }

                point.transform.hasChanged = false;
                changed = true;
            }

            if (changed)
            {
                MarkGeometryDirty();
            }
        }

        private void RebuildIfNeeded()
        {
            if (!_geometryDirty)
            {
                return;
            }

            CacheRenderer();
            _geometryDirty = false;

            if (_sprite == null)
            {
                ReleaseClone();
                return;
            }

            EnsureClone();
            if (_deformedSprite == null)
            {
                return;
            }

            UIMeshRigGeometry geometry = BuildGeometry();
            if (geometry.Vertices.Length == 0 || geometry.Vertices.Length > 65534)
            {
                return;
            }

            WriteGeometry(_deformedSprite, geometry);
            _spriteRenderer.color = _color;
        }

        // WHY: SpriteDataAccessExtensions writes straight into the live Sprite and the renderer picks the
        // new geometry up without re-assigning SpriteRenderer.sprite (verified by rendering the same
        // renderer before/after a geometry rewrite), so the per-frame path stays allocation-light.
        private void WriteGeometry(Sprite target, UIMeshRigGeometry geometry)
        {
            int vertexCount = geometry.Vertices.Length;
            NativeArray<Vector3> positions = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
            NativeArray<Vector2> uv = new NativeArray<Vector2>(vertexCount, Allocator.Temp);
            NativeArray<ushort> indices = new NativeArray<ushort>(geometry.Indices.Length, Allocator.Temp);
            try
            {
                for (int index = 0; index < vertexCount; index++)
                {
                    positions[index] = geometry.Vertices[index];
                    uv[index] = geometry.UV[index];
                }

                for (int index = 0; index < geometry.Indices.Length; index++)
                {
                    indices[index] = (ushort)geometry.Indices[index];
                }

                if (_writtenVertexCount != vertexCount)
                {
                    target.SetVertexCount(vertexCount);
                    _writtenVertexCount = vertexCount;
                }

                target.SetVertexAttribute(VertexAttribute.Position, positions);
                target.SetVertexAttribute(VertexAttribute.TexCoord0, uv);
                target.SetIndices(indices);
            }
            finally
            {
                positions.Dispose();
                uv.Dispose();
                indices.Dispose();
            }
        }

        private void EnsureClone()
        {
            if (_deformedSprite != null &&
                ReferenceEquals(_clonedFrom, _sprite) &&
                Mathf.Approximately(_clonedHeadroom, _boundsHeadroom))
            {
                if (_spriteRenderer != null && _spriteRenderer.sprite != _deformedSprite)
                {
                    _spriteRenderer.sprite = _deformedSprite;
                }

                return;
            }

            ReleaseClone();
            Texture2D texture = _sprite.texture;
            if (texture == null)
            {
                return;
            }

            Vector2 rectSize = _sprite.rect.size;
            Vector2 pivotNormalized = rectSize.sqrMagnitude > 0f
                ? new Vector2(_sprite.pivot.x / rectSize.x, _sprite.pivot.y / rectSize.y)
                : new Vector2(0.5f, 0.5f);

            // WHY: sprite bounds are derived from rect / pixelsPerUnit and never grow with the geometry we
            // write. Cloning at a proportionally smaller PPU inflates only the bounds, which is what culling
            // reads, while every vertex we author stays in true world units.
            float clonePixelsPerUnit = GetPixelsPerUnit() / (1f + Mathf.Max(0f, _boundsHeadroom));
            _deformedSprite = Sprite.Create(
                texture,
                _sprite.textureRect,
                pivotNormalized,
                clonePixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            if (_deformedSprite == null)
            {
                return;
            }

            _deformedSprite.name = _sprite.name + " (UI Mesh Rig)";
            _deformedSprite.hideFlags = HideFlags.HideAndDontSave;
            _clonedFrom = _sprite;
            _clonedHeadroom = _boundsHeadroom;
            _writtenVertexCount = 0;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = _deformedSprite;
            }
        }

        private void ReleaseClone()
        {
            if (_spriteRenderer != null && _spriteRenderer.sprite == _deformedSprite)
            {
                _spriteRenderer.sprite = _sprite;
            }

            if (_deformedSprite != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_deformedSprite);
                }
                else
                {
                    DestroyImmediate(_deformedSprite);
                }
            }

            _deformedSprite = null;
            _clonedFrom = null;
            _clonedHeadroom = -1f;
            _writtenVertexCount = 0;
        }

        private void EnsurePointCache()
        {
            if (!_pointCacheDirty)
            {
                return;
            }

            _points.Clear();
            List<UIMeshRigPoint> candidates = new List<UIMeshRigPoint>();
            GetComponentsInChildren(true, candidates);
            for (int index = 0; index < candidates.Count; index++)
            {
                if (UIMeshRigOwnerResolver.OwnsPoint(this, candidates[index]))
                {
                    _points.Add(candidates[index]);
                }
            }

            _pointCacheDirty = false;
        }

        private void CollectPointStates()
        {
            EnsurePointCache();
            _pointStates.Clear();
            for (int index = 0; index < _points.Count; index++)
            {
                _pointStates.Add(_points[index].CreateState(this));
            }
        }

        private UIMeshRigCoordinateSpace GetCoordinateSpace()
        {
            if (_sprite == null)
            {
                return new UIMeshRigCoordinateSpace(Vector2.zero, Vector2.one);
            }

            Vector2 size = NativeSize;
            Vector2 rectSize = _sprite.rect.size;
            Vector2 pivotNormalized = rectSize.sqrMagnitude > 0f
                ? new Vector2(_sprite.pivot.x / rectSize.x, _sprite.pivot.y / rectSize.y)
                : new Vector2(0.5f, 0.5f);
            return new UIMeshRigCoordinateSpace(-Vector2.Scale(size, pivotNormalized), size);
        }

        private bool IsDeformationActive()
        {
            if (!_deformationEnabled)
            {
                return false;
            }

            if (Application.isPlaying || _authoringMode == UIMeshRigAuthoringMode.Pose)
            {
                return true;
            }

            EnsurePointCache();
            for (int index = 0; index < _points.Count; index++)
            {
                if (_points[index].HasProceduralPose)
                {
                    return true;
                }
            }

            return false;
        }

        private float GetPixelsPerUnit()
        {
            if (_sprite == null || _sprite.pixelsPerUnit <= 0f)
            {
                return 100f;
            }

            return _sprite.pixelsPerUnit;
        }

        private void CacheRenderer()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void MarkGeometryDirty()
        {
            _geometryDirty = true;
        }
    }
}
