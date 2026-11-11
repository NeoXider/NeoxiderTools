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
    [NeoDoc("UI/UIMeshRig.md")]
    public sealed class UIMeshRigGraphic : MaskableGraphic, ILayoutElement, IUIMeshRigOwner
    {
        [Header("Source")]
        [Tooltip("Sprite drawn by the rig. Without it the graphic renders nothing.")]
        [SerializeField] private Sprite _sprite;

        [Tooltip("Fits the sprite inside the RectTransform without stretching it.")]
        [SerializeField] private bool _preserveAspect = true;

        [Header("Deformation Mesh")]
        [Tooltip("Horizontal grid resolution. Below 4 the silhouette cannot bend smoothly.")]
        [Range(2, 40)] [SerializeField] private int _columns = 16;

        [Tooltip("Vertical grid resolution. Below 4 the silhouette cannot bend smoothly.")]
        [Range(2, 40)] [SerializeField] private int _rows = 20;

        [Tooltip("Turns the whole rig off without deleting its points.")]
        [SerializeField] private bool _deformationEnabled = true;

        [Header("Interaction")]
        [Tooltip(
            "Rect accepts the whole RectTransform, Deformed Mesh rejects taps outside the warped mesh, " +
            "Sprite Alpha additionally samples the texture (needs Read/Write Enabled).")]
        [SerializeField] private UIMeshRigRaycastMode _raycastMode = UIMeshRigRaycastMode.DeformedMesh;

        [Tooltip("Minimum sprite alpha that still counts as a hit. Used only by the Sprite Alpha mode.")]
        [Range(0f, 1f)] [SerializeField] private float _alphaHitTestMinimumThreshold = 0.1f;

        [Tooltip(
            "Grows the built-in Raycast Padding every frame so a rig deformed outside its RectTransform " +
            "stays clickable. Your own Raycast Padding value is remembered and used as the baseline.")]
        [SerializeField] private bool _autoExpandRaycastToDeformedMesh = true;

        [Header("Authoring")]
        [Tooltip("Setup edits bind centres and influence ellipses. Pose / Animate deforms the mesh.")]
        [SerializeField] private UIMeshRigAuthoringMode _authoringMode = UIMeshRigAuthoringMode.Setup;

        [Tooltip("Scene-view transform tool used while posing the selected point.")]
        [SerializeField] private UIMeshRigSceneTool _sceneTool = UIMeshRigSceneTool.Move;

        // WHY: the auto-expansion writes into the inherited, serialized m_RaycastPadding, so the value the
        // designer typed has to live somewhere else or the next frame overwrites it. Both fields used to be
        // visible at once ("Raycast Padding" twice, one of them meaningless); the authored copy is hidden and
        // re-captured whenever the visible field stops matching what we last wrote ourselves.
        [HideInInspector] [SerializeField] private Vector4 _configuredRaycastPadding;
        [HideInInspector] [SerializeField] private Vector4 _autoAppliedRaycastPadding;
        [HideInInspector] [SerializeField] private bool _raycastPaddingInitialized;

        private readonly List<UIMeshRigPoint> _points = new List<UIMeshRigPoint>();
        private readonly List<UIMeshRigPointState> _pointStates = new List<UIMeshRigPointState>();
        private bool _pointCacheDirty = true;
        private UIMeshRigGeometry _meshGeometry;
        private Vector2[] _raycastVertexPositions;
        private int _raycastColumns;
        private int _raycastRows;

        // WHY: a readable texture can still refuse alpha sampling (a tightly packed atlas sprite has no
        // textureRect). The refusal is remembered per sprite, so assigning another sprite in the Inspector,
        // which never goes through SetSource, re-enables the alpha test instead of leaving it off for good.
        private Sprite _alphaHitTestUnavailableSprite;

        // WHY: direct child points ride RectTransform anchors, which follow the full rect, while their binds
        // live in the aspect-fitted drawing rect. The rects the anchors were last made consistent with are
        // kept so a resize, sprite swap or Preserve Aspect toggle can move each anchor by exactly what keeps
        // the point's rest offset from its bind.
        private Rect _anchorSyncRect;
        private Rect _anchorSyncDrawingRect;
        private bool _anchorSyncValid;

        public override Texture mainTexture => _sprite != null ? _sprite.texture : s_WhiteTexture;
        public Sprite Sprite => _sprite;
        public int Columns => _columns;
        public int Rows => _rows;
        public bool PreserveAspect => _preserveAspect;
        public bool DeformationEnabled => _deformationEnabled;
        public UIMeshRigRaycastMode RaycastMode => _raycastMode;
        public UIMeshRigAuthoringMode AuthoringMode => _authoringMode;
        public UIMeshRigSceneTool SceneTool => _sceneTool;
        Transform IUIMeshRigOwner.RigTransform => transform;
        float IUIMeshRigOwner.MotionUnitScale => 1f;
        IReadOnlyList<UIMeshRigPoint> IUIMeshRigOwner.RigPoints => Points;
        public IReadOnlyList<UIMeshRigPoint> Points
        {
            get
            {
                EnsurePointCache();
                return _points;
            }
        }

        public float minWidth => 0f;
        public float maxWidth => float.PositiveInfinity;
        public float preferredWidth => GetPreferredSize(true);
        public float flexibleWidth => -1f;
        public float minHeight => 0f;
        public float maxHeight => float.PositiveInfinity;
        public float preferredHeight => GetPreferredSize(false);
        public float flexibleHeight => -1f;
        public int layoutPriority => 0;

        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }

        public void SetSource(Sprite sprite, Color tint, Material sourceMaterial = null)
        {
            _sprite = sprite;
            color = tint;
            material = sourceMaterial;
            SynchronizeDirectChildAnchors();
            SetAllDirty();
        }

        public void SetPreserveAspect(bool preserveAspect)
        {
            if (_preserveAspect == preserveAspect)
            {
                return;
            }

            _preserveAspect = preserveAspect;
            SynchronizeDirectChildAnchors();
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

        public void SetRaycastMode(UIMeshRigRaycastMode mode, float alphaThreshold = 0.1f)
        {
            _raycastMode = mode;
            _alphaHitTestMinimumThreshold = Mathf.Clamp01(alphaThreshold);
        }

        public void SetInteractionRaycastPadding(Vector4 padding)
        {
            _configuredRaycastPadding = padding;
            _autoAppliedRaycastPadding = padding;
            _raycastPaddingInitialized = true;
            raycastPadding = padding;
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
            CollectPointStates();
            return UIMeshRigGeometryBuilder.DeformPoint(
                normalizedPosition,
                GetCoordinateSpace(),
                IsDeformationActive(),
                _pointStates);
        }

        /// <summary>Builds the current deformed grid into new arrays the caller owns.</summary>
        public UIMeshRigGeometry BuildGeometry()
        {
            if (_sprite == null)
            {
                return new UIMeshRigGeometry(new Vector3[0], new int[0], new Vector2[0]);
            }

            return BuildSpriteGeometry(null);
        }

        private UIMeshRigGeometry BuildSpriteGeometry(UIMeshRigGeometry reuse)
        {
            CollectPointStates();
            Vector4 outerUv = DataUtility.GetOuterUV(_sprite);
            Rect uvRect = Rect.MinMaxRect(outerUv.x, outerUv.y, outerUv.z, outerUv.w);
            return UIMeshRigGeometryBuilder.Build(
                _columns,
                _rows,
                GetCoordinateSpace(),
                uvRect,
                UIMeshRigGeometryBuilder.GetSpriteContentBounds(_sprite),
                IsDeformationActive(),
                _pointStates,
                reuse);
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
            return GetCoordinateSpace().NormalizedToPosition(normalizedPosition);
        }

        public Vector2 LocalToNormalized(Vector2 localPosition)
        {
            return GetCoordinateSpace().PositionToNormalized(localPosition);
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
            SetVerticesDirty();
        }

        public void NotifyPoseChanged()
        {
            SetVerticesDirty();
        }

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.Raycast(screenPoint, eventCamera))
            {
                return false;
            }

            if (_sprite == null)
            {
                return false;
            }

            if (_raycastMode == UIMeshRigRaycastMode.Rect)
            {
                return true;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out localPoint))
            {
                return false;
            }

            Vector2 normalized;
            if (!TryGetNormalizedAtDeformedPoint(localPoint, out normalized))
            {
                return false;
            }

            if (_raycastMode != UIMeshRigRaycastMode.SpriteAlpha || _sprite == null)
            {
                return true;
            }

            // WHY: Match uGUI Image behavior: a non-readable texture remains clickable instead of silently breaking input.
            Texture2D texture = _sprite.texture;
            if (texture == null || !texture.isReadable || ReferenceEquals(_alphaHitTestUnavailableSprite, _sprite))
            {
                return true;
            }

            try
            {
                Rect textureRect = _sprite.textureRect;
                float textureU = (textureRect.x + normalized.x * textureRect.width) / texture.width;
                float textureV = (textureRect.y + normalized.y * textureRect.height) / texture.height;
                return texture.GetPixelBilinear(textureU, textureV).a >= _alphaHitTestMinimumThreshold;
            }
            catch (UnityException)
            {
                _alphaHitTestUnavailableSprite = _sprite;
                return true;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _pointCacheDirty = true;
            Canvas.willRenderCanvases -= HandleWillRenderCanvases;
            Canvas.willRenderCanvases += HandleWillRenderCanvases;
            SetAllDirty();
            EnsureRaycastPaddingInitialized();
            UIMeshRigOwnerResolver.NotifyEnclosingOwner(transform);
            SynchronizeDirectChildAnchors();
        }

        protected override void OnDisable()
        {
            Canvas.willRenderCanvases -= HandleWillRenderCanvases;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UIMeshRigOwnerResolver.NotifyEnclosingOwner(transform);
        }

        private void OnTransformChildrenChanged()
        {
            _pointCacheDirty = true;
            SetVerticesDirty();
        }

#if UNITY_EDITOR
        // WHY: Graphic.OnValidate is declared only under UNITY_EDITOR. Without this guard a player build
        // fails with CS0115 (no suitable method to override) although the editor compiles and tests pass.
        protected override void OnValidate()
        {
            base.OnValidate();
            _columns = Mathf.Clamp(_columns, 2, 40);
            _rows = Mathf.Clamp(_rows, 2, 40);
            EnsureRaycastPaddingInitialized();
            _pointCacheDirty = true;
            SetAllDirty();
        }
#endif

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            // WHY: every mode, Edit and Play. Only Play Mode Setup used to react, and it snapped each point
            // back onto its bind, which also threw away the Animator or code pose for that frame. Moving the
            // anchors instead keeps every offset relative to the bind.
            if (isActiveAndEnabled)
            {
                SynchronizeDirectChildAnchors();
            }
        }

        private void LateUpdate()
        {
            SynchronizePointTransforms();
        }

        private void HandleWillRenderCanvases()
        {
            SynchronizePointTransforms();
        }

        /// <summary>
        /// Anchor that puts a direct child point exactly on <paramref name="normalizedPosition"/> with a zero
        /// anchoredPosition. Bind centres are normalized in the aspect-fitted drawing rect, anchors in the
        /// full RectTransform; both agree only while Preserve Aspect leaves no letterbox.
        /// </summary>
        internal Vector2 GetBindAnchor(Vector2 normalizedPosition)
        {
            SynchronizeDirectChildAnchors();
            Rect fullRect = rectTransform.rect;
            Rect drawingRect = GetAnchorDrawingRect();
            if (drawingRect == fullRect)
            {
                return normalizedPosition;
            }

            return LocalToAnchor(
                fullRect,
                drawingRect.min + Vector2.Scale(drawingRect.size, normalizedPosition),
                normalizedPosition);
        }

        /// <summary>
        /// Moves the anchors of direct child points after the full or drawing rect changed (resize, sprite
        /// swap, Preserve Aspect) so each point keeps its rest offset from its bind. anchoredPosition is left
        /// alone: Animator keys and code poses stay relative to the bind.
        /// </summary>
        internal void SynchronizeDirectChildAnchors()
        {
            Rect fullRect = rectTransform.rect;
            Rect drawingRect = GetAnchorDrawingRect();
            if (_anchorSyncValid && fullRect == _anchorSyncRect && drawingRect == _anchorSyncDrawingRect)
            {
                return;
            }

            bool hadPreviousRects = _anchorSyncValid;
            Rect previousRect = _anchorSyncRect;
            Rect previousDrawingRect = _anchorSyncDrawingRect;
            _anchorSyncRect = fullRect;
            _anchorSyncDrawingRect = drawingRect;
            _anchorSyncValid = true;
            if (!hadPreviousRects)
            {
                return;
            }

            EnsurePointCache();
            bool moved = false;
            for (int index = 0; index < _points.Count; index++)
            {
                UIMeshRigPoint point = _points[index];
                RectTransform pointRect = point.transform as RectTransform;
                if (pointRect == null || pointRect.parent != transform || pointRect.anchorMin != pointRect.anchorMax)
                {
                    continue;
                }

                Vector2 anchor = pointRect.anchorMin;
                Vector2 center = point.RestCenterNormalized;
                Vector2 restOffset =
                    previousDrawingRect.min + Vector2.Scale(previousDrawingRect.size, center) -
                    (previousRect.min + Vector2.Scale(previousRect.size, anchor));
                Vector2 bind = drawingRect.min + Vector2.Scale(drawingRect.size, center);
                Vector2 nextAnchor = LocalToAnchor(fullRect, bind - restOffset, anchor);
                if ((nextAnchor - anchor).sqrMagnitude <= 0.000000000001f)
                {
                    continue;
                }

                // WHY: the anchor move is a consequence of the rect change, not an authoring edit. Leaving
                // hasChanged raised would make Setup re-capture the point and rewrite its rest data.
                bool wasChanged = pointRect.hasChanged;
                pointRect.anchorMin = nextAnchor;
                pointRect.anchorMax = nextAnchor;
                if (!wasChanged)
                {
                    pointRect.hasChanged = false;
                }

                moved = true;
            }

            if (moved)
            {
                SetVerticesDirty();
            }
        }

        private static Vector2 LocalToAnchor(Rect fullRect, Vector2 localPosition, Vector2 fallback)
        {
            return new Vector2(
                Mathf.Abs(fullRect.width) > 0.000001f ? (localPosition.x - fullRect.xMin) / fullRect.width : fallback.x,
                Mathf.Abs(fullRect.height) > 0.000001f ? (localPosition.y - fullRect.yMin) / fullRect.height : fallback.y);
        }

        private void SynchronizePointTransforms()
        {
            SynchronizeDirectChildAnchors();
            EnsurePointCache();
            bool changed = false;
            bool rebindAllowed = UIMeshRigAuthoring.IsRebindAllowed(_authoringMode);
            for (int index = 0; index < _points.Count; index++)
            {
                UIMeshRigPoint point = _points[index];
                if (!point.transform.hasChanged)
                {
                    continue;
                }

                // WHY: rebinding writes serialized authoring data (bind centre, anchors, rest TRS) from a
                // LateUpdate with no Undo entry, and the Edit Mode preview forces a player-loop update every
                // editor tick. Any stray hasChanged — an undo, a prefab revert, a redo — was therefore
                // rewritten into the asset behind the user and left the scene permanently dirty. A previewed
                // point is never rebound; stop the preview to edit the bind. An Animation window or Timeline
                // preview counts as a preview too, see UIMeshRigAuthoring.IsRebindAllowed.
                if (rebindAllowed && !point.HasProceduralPose)
                {
                    point.CaptureRestPose(this);
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
                ClearRaycastMeshCache();
                return;
            }

            int columns = Mathf.Clamp(_columns, 2, 40);
            int rows = Mathf.Clamp(_rows, 2, 40);
            // WHY: an animated rig repopulates every frame; reusing one geometry keeps that path free of
            // the three per-rebuild array allocations.
            _meshGeometry = BuildSpriteGeometry(_meshGeometry);
            UIMeshRigGeometry geometry = _meshGeometry;
            int vertexCount = geometry.Vertices.Length;
            if (_raycastVertexPositions == null || _raycastVertexPositions.Length != vertexCount)
            {
                _raycastVertexPositions = new Vector2[vertexCount];
            }
            _raycastColumns = columns;
            _raycastRows = rows;
            vertexHelper.Clear();

            for (int vertexIndex = 0; vertexIndex < geometry.Vertices.Length; vertexIndex++)
            {
                Vector3 position = geometry.Vertices[vertexIndex];
                _raycastVertexPositions[vertexIndex] = position;
                UIVertex vertex = UIVertex.simpleVert;
                vertex.position = position;
                vertex.color = color;
                vertex.uv0 = geometry.UV[vertexIndex];
                vertexHelper.AddVert(vertex);
            }

            UpdateEffectiveRaycastPadding(GetDrawingRect());

            for (int index = 0; index < geometry.Indices.Length; index += 3)
            {
                vertexHelper.AddTriangle(
                    geometry.Indices[index],
                    geometry.Indices[index + 1],
                    geometry.Indices[index + 2]);
            }
        }

        private void EnsurePointCache()
        {
            if (!_pointCacheDirty)
            {
                return;
            }

            UIMeshRigOwnerResolver.CollectOwnedPoints(this, this, _points);
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

        private bool IsDeformationActive()
        {
            if (!_deformationEnabled)
            {
                return false;
            }

            if (Application.isPlaying || _authoringMode == UIMeshRigAuthoringMode.Pose ||
                UIMeshRigAuthoring.IsAnimationPreviewActive())
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

        private UIMeshRigCoordinateSpace GetCoordinateSpace()
        {
            Rect drawingRect = GetDrawingRect();
            return new UIMeshRigCoordinateSpace(drawingRect.min, drawingRect.size);
        }

        private Rect GetDrawingRect()
        {
            return FitToSprite(GetPixelAdjustedRect());
        }

        // WHY: anchor bookkeeping uses the unsnapped rect. A pixel-perfect canvas snaps the drawing rect
        // differently at every canvas scale, so resizing the Game view would otherwise rewrite every point
        // anchor in the scene for a sub-pixel difference.
        private Rect GetAnchorDrawingRect()
        {
            return FitToSprite(rectTransform.rect);
        }

        private Rect FitToSprite(Rect rect)
        {
            if (!_preserveAspect || _sprite == null || _sprite.rect.height <= 0f || rect.height <= 0f)
            {
                return rect;
            }

            float spriteAspect = _sprite.rect.width / _sprite.rect.height;
            return UIMeshRigGeometryBuilder.GetAspectFittedRect(rect, spriteAspect, true, rectTransform.pivot);
        }

        private bool TryGetNormalizedAtDeformedPoint(Vector2 localPoint, out Vector2 normalized)
        {
            int columns = Mathf.Clamp(_columns, 2, 40);
            int rows = Mathf.Clamp(_rows, 2, 40);
            bool hasCache = _raycastVertexPositions != null &&
                            _raycastColumns == columns && _raycastRows == rows &&
                            _raycastVertexPositions.Length == (columns + 1) * (rows + 1);
            // WHY: the grid parameters below span the sprite's texture region, which a trimmed atlas sprite
            // covers only partly; deforming them as rig coordinates needs the same content bounds the mesh uses.
            Rect bounds = hasCache ? UIMeshRigGeometryBuilder.UnitRect : UIMeshRigGeometryBuilder.GetSpriteContentBounds(_sprite);
            for (int y = 0; y < rows; y++)
            {
                float v0 = y / (float)rows;
                float v1 = (y + 1) / (float)rows;
                for (int x = 0; x < columns; x++)
                {
                    float u0 = x / (float)columns;
                    float u1 = (x + 1) / (float)columns;
                    Vector2 uv00 = new Vector2(u0, v0);
                    Vector2 uv10 = new Vector2(u1, v0);
                    Vector2 uv01 = new Vector2(u0, v1);
                    Vector2 uv11 = new Vector2(u1, v1);
                    Vector3 barycentric;
                    int stride = columns + 1;
                    int i00 = y * stride + x;
                    int i10 = i00 + 1;
                    int i01 = i00 + stride;
                    int i11 = i01 + 1;
                    Vector2 p00 = hasCache ? _raycastVertexPositions[i00] : CalculateDeformedLocalPoint(GridToNormalized(uv00, bounds));
                    Vector2 p10 = hasCache ? _raycastVertexPositions[i10] : CalculateDeformedLocalPoint(GridToNormalized(uv10, bounds));
                    Vector2 p01 = hasCache ? _raycastVertexPositions[i01] : CalculateDeformedLocalPoint(GridToNormalized(uv01, bounds));
                    Vector2 p11 = hasCache ? _raycastVertexPositions[i11] : CalculateDeformedLocalPoint(GridToNormalized(uv11, bounds));
                    if (TryPointInTriangle(localPoint, p00, p01, p11,
                            out barycentric))
                    {
                        normalized = uv00 * barycentric.x + uv01 * barycentric.y + uv11 * barycentric.z;
                        return true;
                    }

                    if (TryPointInTriangle(localPoint, p00, p11, p10,
                            out barycentric))
                    {
                        normalized = uv00 * barycentric.x + uv11 * barycentric.y + uv10 * barycentric.z;
                        return true;
                    }
                }
            }

            normalized = default;
            return false;
        }

        private static Vector2 GridToNormalized(Vector2 gridPosition, Rect bounds)
        {
            return new Vector2(
                Mathf.LerpUnclamped(bounds.xMin, bounds.xMax, gridPosition.x),
                Mathf.LerpUnclamped(bounds.yMin, bounds.yMax, gridPosition.y));
        }

        private static bool TryPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c, out Vector3 barycentric)
        {
            Vector2 v0 = b - a;
            Vector2 v1 = c - a;
            Vector2 v2 = point - a;
            float denominator = v0.x * v1.y - v1.x * v0.y;
            if (Mathf.Abs(denominator) <= 0.000001f)
            {
                barycentric = default;
                return false;
            }

            float y = (v2.x * v1.y - v1.x * v2.y) / denominator;
            float z = (v0.x * v2.y - v2.x * v0.y) / denominator;
            float x = 1f - y - z;
            barycentric = new Vector3(x, y, z);
            return x >= -0.0001f && y >= -0.0001f && z >= -0.0001f;
        }

        private void EnsureRaycastPaddingInitialized()
        {
            if (!_raycastPaddingInitialized)
            {
                _configuredRaycastPadding = raycastPadding;
                _autoAppliedRaycastPadding = raycastPadding;
                _raycastPaddingInitialized = true;
                return;
            }

            // WHY: the visible Raycast Padding field is ours to overwrite, so an edit is only recognisable as
            // "the user typed this" while it still differs from the value we wrote last.
            if (raycastPadding != _autoAppliedRaycastPadding)
            {
                _configuredRaycastPadding = raycastPadding;
                _autoAppliedRaycastPadding = raycastPadding;
            }
        }

        private void ClearRaycastMeshCache()
        {
            _raycastVertexPositions = null;
            _raycastColumns = 0;
            _raycastRows = 0;
            EnsureRaycastPaddingInitialized();
            ApplyEffectiveRaycastPadding(_configuredRaycastPadding);
        }

        private void ApplyEffectiveRaycastPadding(Vector4 padding)
        {
            _autoAppliedRaycastPadding = padding;
            raycastPadding = padding;
        }

        private void UpdateEffectiveRaycastPadding(Rect sourceRect)
        {
            EnsureRaycastPaddingInitialized();
            if (!_autoExpandRaycastToDeformedMesh || _raycastVertexPositions == null || _raycastVertexPositions.Length == 0)
            {
                ApplyEffectiveRaycastPadding(_configuredRaycastPadding);
                return;
            }

            Vector2 min = _raycastVertexPositions[0];
            Vector2 max = min;
            for (int index = 1; index < _raycastVertexPositions.Length; index++)
            {
                min = Vector2.Min(min, _raycastVertexPositions[index]);
                max = Vector2.Max(max, _raycastVertexPositions[index]);
            }

            ApplyEffectiveRaycastPadding(new Vector4(
                _configuredRaycastPadding.x - Mathf.Max(0f, sourceRect.xMin - min.x),
                _configuredRaycastPadding.y - Mathf.Max(0f, sourceRect.yMin - min.y),
                _configuredRaycastPadding.z - Mathf.Max(0f, max.x - sourceRect.xMax),
                _configuredRaycastPadding.w - Mathf.Max(0f, max.y - sourceRect.yMax)));
        }

        private float GetPreferredSize(bool horizontal)
        {
            if (_sprite == null)
            {
                return 0f;
            }

            float pixelsPerUnit = _sprite.pixelsPerUnit > 0f ? _sprite.pixelsPerUnit : 100f;
            return (horizontal ? _sprite.rect.width : _sprite.rect.height) / pixelsPerUnit * 100f;
        }
    }
}
