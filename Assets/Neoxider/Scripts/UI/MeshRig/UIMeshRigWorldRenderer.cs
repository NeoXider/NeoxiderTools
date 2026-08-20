using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;

namespace Neo.UI
{
    /// <summary>
    /// World-space UI Mesh Rig output for scenes without a Canvas. Geometry is written to a regular
    /// MeshFilter/MeshRenderer pair and uses the same points, falloff and motion core as uGUI.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("Neoxider/UI/UI Mesh Rig World Renderer")]
    [NeoDoc("UI/UIMeshRig.md")]
    public sealed class UIMeshRigWorldRenderer : MonoBehaviour, IUIMeshRigOwner
    {
        [Header("Source")]
        [Tooltip("Sprite drawn by the rig. Without it the renderer produces an empty mesh.")]
        [SerializeField] private Sprite _sprite;

        [Tooltip("Tint pushed to the material through a MaterialPropertyBlock.")]
        [SerializeField] private Color _color = Color.white;

        [Tooltip("Optional material override. Empty falls back to a runtime Sprites/Default instance.")]
        [SerializeField] private Material _material;

        [Header("Geometry")]
        [Tooltip("Horizontal grid resolution. Below 4 the silhouette cannot bend smoothly.")]
        [Range(2, 40)] [SerializeField] private int _columns = 16;

        [Tooltip("Vertical grid resolution. Below 4 the silhouette cannot bend smoothly.")]
        [Range(2, 40)] [SerializeField] private int _rows = 20;

        [Tooltip("Fits the sprite inside Size without stretching it.")]
        [SerializeField] private bool _preserveAspect = true;

        [Tooltip("Quad size in world units before deformation.")]
        [SerializeField] private Vector2 _size = new Vector2(3f, 3f);

        [Tooltip("Origin of the quad inside Size. 0.5, 0.5 centres it on the Transform.")]
        [SerializeField] private Vector2 _pivot = new Vector2(0.5f, 0.5f);

        [Tooltip("Scales pixel-authored motion presets into world units.")]
        [Min(0.01f)] [SerializeField] private float _pixelsPerUnit = 100f;

        [Tooltip("Turns the whole rig off without deleting its points.")]
        [SerializeField] private bool _deformationEnabled = true;

        [Header("Authoring")]
        [Tooltip("Setup edits bind centres and influence ellipses. Pose / Animate deforms the mesh.")]
        [SerializeField] private UIMeshRigAuthoringMode _authoringMode = UIMeshRigAuthoringMode.Setup;

        [Tooltip("Scene-view transform tool used while posing the selected point.")]
        [SerializeField] private UIMeshRigSceneTool _sceneTool = UIMeshRigSceneTool.Move;

        private readonly List<UIMeshRigPoint> _points = new List<UIMeshRigPoint>();
        private readonly List<UIMeshRigPointState> _pointStates = new List<UIMeshRigPointState>();
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        private Material _runtimeMaterial;
        private MaterialPropertyBlock _propertyBlock;
        private bool _pointCacheDirty = true;
        private bool _geometryDirty = true;

        public Sprite Sprite => _sprite;
        public int Columns => _columns;
        public int Rows => _rows;
        public bool PreserveAspect => _preserveAspect;
        public bool DeformationEnabled => _deformationEnabled;
        public Vector2 Size => _size;
        public Color Color => _color;
        public UIMeshRigAuthoringMode AuthoringMode => _authoringMode;
        public UIMeshRigSceneTool SceneTool => _sceneTool;
        public IReadOnlyList<UIMeshRigPoint> Points
        {
            get
            {
                EnsurePointCache();
                return _points;
            }
        }

        Transform IUIMeshRigOwner.RigTransform => transform;
        float IUIMeshRigOwner.MotionUnitScale => 1f / Mathf.Max(0.01f, _pixelsPerUnit);
        IReadOnlyList<UIMeshRigPoint> IUIMeshRigOwner.RigPoints => Points;

        public void SetSource(Sprite sprite, Color tint, Material sourceMaterial = null)
        {
            _sprite = sprite;
            _color = tint;
            _material = sourceMaterial;
            MarkGeometryDirty();
        }

        public void SetGridResolution(int columns, int rows)
        {
            _columns = Mathf.Clamp(columns, 2, 40);
            _rows = Mathf.Clamp(rows, 2, 40);
            MarkGeometryDirty();
        }

        public void SetPreserveAspect(bool preserveAspect)
        {
            _preserveAspect = preserveAspect;
            MarkGeometryDirty();
        }

        public void SetDeformationEnabled(bool enabled)
        {
            _deformationEnabled = enabled;
            MarkGeometryDirty();
        }

        public void SetSize(Vector2 size)
        {
            _size = new Vector2(Mathf.Max(0.001f, size.x), Mathf.Max(0.001f, size.y));
            MarkGeometryDirty();
        }

        public void SetNativeSize()
        {
            if (_sprite == null)
            {
                return;
            }

            float units = Mathf.Max(0.01f, _pixelsPerUnit);
            SetSize(_sprite.rect.size / units);
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
            CacheRendererComponents();
            _pointCacheDirty = true;
            MarkGeometryDirty();
            RebuildIfNeeded();
        }

        private void OnDisable()
        {
            ReleaseRuntimeResources();
        }

        private void OnValidate()
        {
            _columns = Mathf.Clamp(_columns, 2, 40);
            _rows = Mathf.Clamp(_rows, 2, 40);
            _size = new Vector2(Mathf.Max(0.001f, _size.x), Mathf.Max(0.001f, _size.y));
            _pivot = new Vector2(Mathf.Clamp01(_pivot.x), Mathf.Clamp01(_pivot.y));
            _pixelsPerUnit = Mathf.Max(0.01f, _pixelsPerUnit);
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

            CacheRendererComponents();
            EnsureMesh();
            UIMeshRigGeometry geometry = BuildGeometry();
            _mesh.Clear();
            if (geometry.Vertices.Length > 0)
            {
                _mesh.vertices = geometry.Vertices;
                _mesh.triangles = geometry.Indices;
                _mesh.uv = geometry.UV;
                _mesh.RecalculateBounds();
            }

            ApplyRendererSettings();
            _geometryDirty = false;
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
            Rect availableRect = new Rect(
                -_size.x * _pivot.x,
                -_size.y * _pivot.y,
                _size.x,
                _size.y);
            float spriteAspect = _sprite != null && _sprite.rect.height > 0f
                ? _sprite.rect.width / _sprite.rect.height
                : 0f;
            Rect drawingRect = UIMeshRigGeometryBuilder.GetAspectFittedRect(
                availableRect,
                spriteAspect,
                _preserveAspect,
                _pivot);
            return new UIMeshRigCoordinateSpace(drawingRect.min, drawingRect.size);
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

        private void CacheRendererComponents()
        {
            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
            }

            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
            }
        }

        private void EnsureMesh()
        {
            if (_mesh != null)
            {
                return;
            }

            _mesh = new Mesh
            {
                name = "UI Mesh Rig World Mesh",
                hideFlags = HideFlags.DontSave
            };
            _mesh.MarkDynamic();
            _meshFilter.sharedMesh = _mesh;
        }

        private void ApplyRendererSettings()
        {
            Material targetMaterial = _material != null ? _material : GetRuntimeMaterial();
            _meshRenderer.sharedMaterial = targetMaterial;
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_Color", _color);
            _propertyBlock.SetTexture("_MainTex", _sprite != null ? _sprite.texture : Texture2D.whiteTexture);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        private Material GetRuntimeMaterial()
        {
            if (_runtimeMaterial != null)
            {
                return _runtimeMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            _runtimeMaterial = new Material(shader)
            {
                name = "UI Mesh Rig World Runtime Material",
                hideFlags = HideFlags.HideAndDontSave
            };
            return _runtimeMaterial;
        }

        private void MarkGeometryDirty()
        {
            _geometryDirty = true;
        }

        private void ReleaseRuntimeResources()
        {
            if (_meshFilter != null && _meshFilter.sharedMesh == _mesh)
            {
                _meshFilter.sharedMesh = null;
            }

            DestroyRuntimeObject(_mesh);
            DestroyRuntimeObject(_runtimeMaterial);
            _mesh = null;
            _runtimeMaterial = null;
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
