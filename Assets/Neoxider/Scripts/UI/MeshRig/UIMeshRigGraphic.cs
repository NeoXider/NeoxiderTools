using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Neo.UI
{
    /// <summary>
    /// Animator-friendly deformable uGUI sprite. Child <see cref="UIMeshRigPoint"/> transforms act as bones.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Neoxider/UI/UI Mesh Rig Graphic")]
    public sealed class UIMeshRigGraphic : MaskableGraphic
    {
        [SerializeField] private Sprite _sprite;
        [Range(2, 40)] [SerializeField] private int _columns = 16;
        [Range(2, 40)] [SerializeField] private int _rows = 20;
        [SerializeField] private bool _preserveAspect = true;
        [SerializeField] private bool _deformationEnabled = true;
        [SerializeField] private UIMeshRigAuthoringMode _authoringMode = UIMeshRigAuthoringMode.Setup;
        [SerializeField] private UIMeshRigSceneTool _sceneTool = UIMeshRigSceneTool.Move;

        private readonly List<UIMeshRigPoint> _points = new List<UIMeshRigPoint>();
        private bool _pointCacheDirty = true;
        private bool _bindingCacheDirty = true;
        private float[,] _cachedWeights;
        private int _cachedVertexCount;

        public override Texture mainTexture => _sprite != null ? _sprite.texture : s_WhiteTexture;
        public Sprite Sprite => _sprite;
        public int Columns => _columns;
        public int Rows => _rows;
        public bool PreserveAspect => _preserveAspect;
        public bool DeformationEnabled => _deformationEnabled;
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

        public void SetSource(Sprite sprite, Color tint, Material sourceMaterial = null)
        {
            _sprite = sprite;
            color = tint;
            material = sourceMaterial;
            SetAllDirty();
        }

        public void SetPreserveAspect(bool preserveAspect)
        {
            if (_preserveAspect == preserveAspect)
            {
                return;
            }

            _preserveAspect = preserveAspect;
            SetVerticesDirty();
        }

        public void SetDeformationEnabled(bool enabled)
        {
            if (_deformationEnabled == enabled)
            {
                return;
            }

            _deformationEnabled = enabled;
            SetVerticesDirty();
        }

        public override void SetNativeSize()
        {
            if (_sprite == null)
            {
                return;
            }

            float pixelsPerUnit = _sprite.pixelsPerUnit;
            if (pixelsPerUnit <= 0f)
            {
                pixelsPerUnit = 100f;
            }

            rectTransform.anchorMax = rectTransform.anchorMin;
            rectTransform.sizeDelta = _sprite.rect.size / pixelsPerUnit * 100f;
            SetAllDirty();
        }

        public void SetGridResolution(int columns, int rows)
        {
            _columns = Mathf.Clamp(columns, 2, 40);
            _rows = Mathf.Clamp(rows, 2, 40);
            NotifyBindingChanged();
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

            SetVerticesDirty();
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

            SetVerticesDirty();
        }

        public void ResetPose()
        {
            EnsurePointCache();
            for (int index = 0; index < _points.Count; index++)
            {
                _points[index].ResetPose(this);
            }

            SetVerticesDirty();
        }

        public Vector2 CalculateDeformedLocalPoint(Vector2 normalizedPosition)
        {
            Vector2 baseLocal = NormalizedToLocal(normalizedPosition);
            if (!_deformationEnabled || (!Application.isPlaying && _authoringMode != UIMeshRigAuthoringMode.Pose))
            {
                return baseLocal;
            }

            EnsurePointCache();
            float totalWeight = 0f;
            Vector2 weightedPosition = Vector2.zero;
            for (int index = 0; index < _points.Count; index++)
            {
                UIMeshRigPoint point = _points[index];
                float weight = point.CalculateWeight(normalizedPosition);
                if (weight <= 0f)
                {
                    continue;
                }

                weightedPosition += point.TransformLocalPoint(this, baseLocal) * weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0.00001f)
            {
                return baseLocal;
            }

            Vector2 average = weightedPosition / totalWeight;
            return Vector2.LerpUnclamped(baseLocal, average, Mathf.Clamp01(totalWeight));
        }

        public Vector2 WorldToNormalized(Vector3 worldPosition)
        {
            return LocalToNormalized(WorldToLocal(worldPosition));
        }

        public Vector3 NormalizedToWorld(Vector2 normalizedPosition)
        {
            return transform.TransformPoint(NormalizedToLocal(normalizedPosition));
        }

        public Vector2 WorldToLocal(Vector3 worldPosition)
        {
            return rectTransform.InverseTransformPoint(worldPosition);
        }

        public Vector2 NormalizedToLocal(Vector2 normalizedPosition)
        {
            Rect drawingRect = GetDrawingRect();
            return new Vector2(
                Mathf.Lerp(drawingRect.xMin, drawingRect.xMax, normalizedPosition.x),
                Mathf.Lerp(drawingRect.yMin, drawingRect.yMax, normalizedPosition.y));
        }

        public Vector2 LocalToNormalized(Vector2 localPosition)
        {
            Rect drawingRect = GetDrawingRect();
            return new Vector2(
                Mathf.InverseLerp(drawingRect.xMin, drawingRect.xMax, localPosition.x),
                Mathf.InverseLerp(drawingRect.yMin, drawingRect.yMax, localPosition.y));
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
            _bindingCacheDirty = true;
            SetVerticesDirty();
        }

        public void NotifyPoseChanged()
        {
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _pointCacheDirty = true;
            _bindingCacheDirty = true;
            Canvas.willRenderCanvases -= HandleWillRenderCanvases;
            Canvas.willRenderCanvases += HandleWillRenderCanvases;
            SetAllDirty();
        }

        protected override void OnDisable()
        {
            Canvas.willRenderCanvases -= HandleWillRenderCanvases;
            base.OnDisable();
        }

        private void OnTransformChildrenChanged()
        {
            _pointCacheDirty = true;
            _bindingCacheDirty = true;
            SetVerticesDirty();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _columns = Mathf.Clamp(_columns, 2, 40);
            _rows = Mathf.Clamp(_rows, 2, 40);
            _pointCacheDirty = true;
            _bindingCacheDirty = true;
            SetAllDirty();
        }

        private void LateUpdate()
        {
            SynchronizePointTransforms();
        }

        private void HandleWillRenderCanvases()
        {
            SynchronizePointTransforms();
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

                if (!Application.isPlaying && _authoringMode == UIMeshRigAuthoringMode.Setup)
                {
                    point.CaptureRestPose(this);
                    _bindingCacheDirty = true;
                }

                point.transform.hasChanged = false;
                changed = true;
            }

            if (changed)
            {
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            if (_sprite == null)
            {
                vertexHelper.Clear();
                return;
            }

            Rect drawingRect = GetDrawingRect();
            Vector4 uv = DataUtility.GetOuterUV(_sprite);
            int columns = Mathf.Clamp(_columns, 2, 40);
            int rows = Mathf.Clamp(_rows, 2, 40);
            vertexHelper.Clear();
            EnsureBindingCache(columns, rows);

            for (int y = 0; y <= rows; y++)
            {
                float v = y / (float)rows;
                for (int x = 0; x <= columns; x++)
                {
                    float u = x / (float)columns;
                    Vector2 normalized = new Vector2(u, v);
                    int vertexIndex = y * (columns + 1) + x;
                    Vector2 position = CalculateCachedDeformedLocalPoint(normalized, vertexIndex);
                    UIVertex vertex = UIVertex.simpleVert;
                    vertex.position = position;
                    vertex.color = color;
                    vertex.uv0 = new Vector2(
                        Mathf.Lerp(uv.x, uv.z, u),
                        Mathf.Lerp(uv.y, uv.w, v));
                    vertexHelper.AddVert(vertex);
                }
            }

            int stride = columns + 1;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int bottomLeft = y * stride + x;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + stride;
                    int topRight = topLeft + 1;
                    vertexHelper.AddTriangle(bottomLeft, topLeft, topRight);
                    vertexHelper.AddTriangle(bottomLeft, topRight, bottomRight);
                }
            }
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
                UIMeshRigPoint candidate = candidates[index];
                UIMeshRigGraphic nearestOwner = candidate.GetComponentInParent<UIMeshRigGraphic>();
                if (nearestOwner == this)
                {
                    _points.Add(candidate);
                }
            }

            _pointCacheDirty = false;
            _bindingCacheDirty = true;
        }

        private void EnsureBindingCache(int columns, int rows)
        {
            EnsurePointCache();
            int vertexCount = (columns + 1) * (rows + 1);
            if (!_bindingCacheDirty && _cachedWeights != null &&
                _cachedVertexCount == vertexCount && _cachedWeights.GetLength(1) == _points.Count)
            {
                return;
            }

            _cachedWeights = new float[vertexCount, _points.Count];
            for (int y = 0; y <= rows; y++)
            {
                float v = y / (float)rows;
                for (int x = 0; x <= columns; x++)
                {
                    float u = x / (float)columns;
                    int vertexIndex = y * (columns + 1) + x;
                    Vector2 normalized = new Vector2(u, v);
                    for (int pointIndex = 0; pointIndex < _points.Count; pointIndex++)
                    {
                        _cachedWeights[vertexIndex, pointIndex] = _points[pointIndex].CalculateWeight(normalized);
                    }
                }
            }

            _cachedVertexCount = vertexCount;
            _bindingCacheDirty = false;
        }

        private Vector2 CalculateCachedDeformedLocalPoint(Vector2 normalizedPosition, int vertexIndex)
        {
            Vector2 baseLocal = NormalizedToLocal(normalizedPosition);
            if (!_deformationEnabled || (!Application.isPlaying && _authoringMode != UIMeshRigAuthoringMode.Pose))
            {
                return baseLocal;
            }

            float totalWeight = 0f;
            Vector2 weightedPosition = Vector2.zero;
            for (int pointIndex = 0; pointIndex < _points.Count; pointIndex++)
            {
                float weight = _cachedWeights[vertexIndex, pointIndex];
                if (weight <= 0f || !_points[pointIndex].isActiveAndEnabled)
                {
                    continue;
                }

                weightedPosition += _points[pointIndex].TransformLocalPoint(this, baseLocal) * weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0.00001f)
            {
                return baseLocal;
            }

            Vector2 average = weightedPosition / totalWeight;
            return Vector2.LerpUnclamped(baseLocal, average, Mathf.Clamp01(totalWeight));
        }

        private Rect GetDrawingRect()
        {
            Rect rect = GetPixelAdjustedRect();
            if (!_preserveAspect || _sprite == null || _sprite.rect.height <= 0f || rect.height <= 0f)
            {
                return rect;
            }

            float spriteAspect = _sprite.rect.width / _sprite.rect.height;
            float rectAspect = rect.width / rect.height;
            if (spriteAspect > rectAspect)
            {
                float height = rect.width / spriteAspect;
                float offset = (rect.height - height) * rectTransform.pivot.y;
                rect.y += offset;
                rect.height = height;
            }
            else
            {
                float width = rect.height * spriteAspect;
                float offset = (rect.width - width) * rectTransform.pivot.x;
                rect.x += offset;
                rect.width = width;
            }

            return rect;
        }
    }
}
