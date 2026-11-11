using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UIElements;

namespace Neo.UI
{
    /// <summary>
    /// UI Toolkit output adapter for UI Mesh Rig. The element is available in UI Builder under
    /// Library / Custom Controls / Neoxider / UI Mesh Rig.
    /// </summary>
    [UxmlElement(libraryPath = "Neoxider/UI Mesh Rig")]
    public partial class UIMeshRigElement : VisualElement
    {
        private readonly List<UIMeshRigPointState> _pointStates = new List<UIMeshRigPointState>();
        private UIMeshRigGeometry _meshGeometry;
        private Vertex[] _vertexBuffer;
        private ushort[] _indexBuffer;
        private Sprite _sprite;
        private int _columns = 16;
        private int _rows = 20;
        private bool _preserveAspect = true;
        private bool _deformationEnabled = true;
        private Color _tint = Color.white;
        private UIMeshRigLayoutPreset _layoutPreset = UIMeshRigLayoutPreset.SimpleBounce;
        private bool _motionEnabled = true;
        private float _motionSpeed = 1f;
        private float _motionPhase;
        private IVisualElementScheduledItem _motionSchedule;

        public UIMeshRigElement()
        {
            style.width = 300f;
            style.height = 300f;
            style.flexShrink = 0f;
            generateVisualContent += GenerateMesh;
            RegisterCallback<AttachToPanelEvent>(HandleAttach);
            RegisterCallback<DetachFromPanelEvent>(HandleDetach);
        }

        [UxmlAttribute]
        public Sprite Sprite
        {
            get => _sprite;
            set
            {
                if (_sprite == value)
                {
                    return;
                }

                _sprite = value;
                UpdateMotionSchedule();
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public int Columns
        {
            get => _columns;
            set
            {
                _columns = Mathf.Clamp(value, 2, 40);
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public int Rows
        {
            get => _rows;
            set
            {
                _rows = Mathf.Clamp(value, 2, 40);
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public bool PreserveAspect
        {
            get => _preserveAspect;
            set
            {
                _preserveAspect = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public bool DeformationEnabled
        {
            get => _deformationEnabled;
            set
            {
                _deformationEnabled = value;
                UpdateMotionSchedule();
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public Color Tint
        {
            get => _tint;
            set
            {
                _tint = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public UIMeshRigLayoutPreset LayoutPreset
        {
            get => _layoutPreset;
            set
            {
                _layoutPreset = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public bool MotionEnabled
        {
            get => _motionEnabled;
            set
            {
                _motionEnabled = value;
                UpdateMotionSchedule();
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public float MotionSpeed
        {
            get => _motionSpeed;
            set
            {
                _motionSpeed = value;
                UpdateMotionSchedule();
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public float MotionPhase
        {
            get => _motionPhase;
            set
            {
                _motionPhase = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        ///     True while the drawn pose changes over time, so the element repaints every 16 ms on a panel:
        ///     motion and deformation on, a sprite set and a finite, non-zero <see cref="MotionSpeed" />.
        /// </summary>
        internal bool NeedsTimedRepaint =>
            _motionEnabled && _deformationEnabled && _sprite != null && _motionSpeed != 0f &&
            !float.IsNaN(_motionSpeed) && !float.IsInfinity(_motionSpeed);

        /// <summary>
        ///     Builds the rig for a surface of <paramref name="surfaceSize" /> panel pixels at
        ///     <paramref name="timeSeconds" /> of motion time, into new arrays the caller owns.
        /// </summary>
        public UIMeshRigGeometry BuildGeometry(Vector2 surfaceSize, float timeSeconds)
        {
            if (!CanBuild(surfaceSize))
            {
                return new UIMeshRigGeometry(new Vector3[0], new int[0], new Vector2[0]);
            }

            return BuildSpriteGeometry(surfaceSize, timeSeconds, null);
        }

        /// <summary>
        ///     Repaint path: builds into the element's own reused geometry. Returns null when there is nothing
        ///     to draw. The result is overwritten by the next call.
        /// </summary>
        internal UIMeshRigGeometry BuildGeometryReusingBuffers(Vector2 surfaceSize, float timeSeconds)
        {
            if (!CanBuild(surfaceSize))
            {
                return null;
            }

            _meshGeometry = BuildSpriteGeometry(surfaceSize, timeSeconds, _meshGeometry);
            return _meshGeometry;
        }

        private bool CanBuild(Vector2 surfaceSize)
        {
            return _sprite != null && surfaceSize.x > 0f && surfaceSize.y > 0f;
        }

        private UIMeshRigGeometry BuildSpriteGeometry(Vector2 surfaceSize, float timeSeconds, UIMeshRigGeometry reuse)
        {
            UIMeshRigCoordinateSpace space = GetCoordinateSpace(surfaceSize);
            CollectPointStates(space, timeSeconds);
            Vector4 outerUv = DataUtility.GetOuterUV(_sprite);
            Rect uvRect = Rect.MinMaxRect(outerUv.x, outerUv.y, outerUv.z, outerUv.w);
            return UIMeshRigGeometryBuilder.Build(
                _columns,
                _rows,
                space,
                uvRect,
                _deformationEnabled,
                _pointStates,
                reuse);
        }

        // WHY: an animating rig repaints every 16 ms. Geometry, vertex and index buffers are reused,
        // so a steady animation does not allocate per frame.
        private void GenerateMesh(MeshGenerationContext context)
        {
            UIMeshRigGeometry geometry = BuildGeometryReusingBuffers(contentRect.size, Time.realtimeSinceStartup);
            if (geometry == null)
            {
                return;
            }

            MeshWriteData meshWriteData = context.Allocate(
                geometry.Vertices.Length,
                geometry.Indices.Length,
                _sprite.texture);

#pragma warning disable CS0618
            Rect uvRegion = meshWriteData.uvRegion;
#pragma warning restore CS0618
            if (_vertexBuffer == null || _vertexBuffer.Length != geometry.Vertices.Length)
            {
                _vertexBuffer = new Vertex[geometry.Vertices.Length];
            }

            Vertex[] vertices = _vertexBuffer;
            for (int index = 0; index < vertices.Length; index++)
            {
                Vector2 sourceUv = geometry.UV[index];
                Vertex vertex = new Vertex
                {
                    position = new Vector3(
                        geometry.Vertices[index].x,
                        geometry.Vertices[index].y,
                        Vertex.nearZ),
                    tint = _tint,
                    uv = new Vector2(
                        uvRegion.xMin + sourceUv.x * uvRegion.width,
                        uvRegion.yMin + sourceUv.y * uvRegion.height)
                };
                vertices[index] = vertex;
            }

            if (_indexBuffer == null || _indexBuffer.Length != geometry.Indices.Length)
            {
                _indexBuffer = new ushort[geometry.Indices.Length];
            }

            ushort[] indices = _indexBuffer;
            for (int index = 0; index < indices.Length; index++)
            {
                indices[index] = (ushort)geometry.Indices[index];
            }

            meshWriteData.SetAllVertices(vertices);
            meshWriteData.SetAllIndices(indices);
        }

        private UIMeshRigCoordinateSpace GetCoordinateSpace(Vector2 surfaceSize)
        {
            Rect availableRect = new Rect(Vector2.zero, surfaceSize);
            float spriteAspect = _sprite != null && _sprite.rect.height > 0f
                ? _sprite.rect.width / _sprite.rect.height
                : 0f;
            Rect drawingRect = UIMeshRigGeometryBuilder.GetAspectFittedRect(
                availableRect,
                spriteAspect,
                _preserveAspect,
                new Vector2(0.5f, 0.5f));
            return new UIMeshRigCoordinateSpace(
                new Vector2(drawingRect.xMin, drawingRect.yMax),
                new Vector2(drawingRect.width, -drawingRect.height));
        }

        private void CollectPointStates(UIMeshRigCoordinateSpace space, float timeSeconds)
        {
            _pointStates.Clear();
            int pointCount = UIMeshRigLayoutPresets.GetPointCount(_layoutPreset);
            AnimationCurve falloff = UIMeshRigFalloffPresets.GetShared(UIMeshRigFalloffPreset.Smooth);
            for (int index = 0; index < pointCount; index++)
            {
                UIMeshRigPointLayout layout = UIMeshRigLayoutPresets.GetPoint(_layoutPreset, index);
                UIMeshRigProceduralPose pose = UIMeshRigProceduralPose.Identity;
                if (_motionEnabled && layout.MotionPreset != UIMeshRigMotionPreset.Custom)
                {
                    // WHY: shared read-only profile; Create() built about seven AnimationCurves per point on
                    // every repaint.
                    UIMeshRigMotionProfile profile = UIMeshRigMotionPresets.GetShared(layout.MotionPreset);
                    pose = UIMeshRigMotionEvaluator.Evaluate(
                        profile,
                        timeSeconds,
                        _motionSpeed,
                        layout.Phase + _motionPhase,
                        layout.CenterNormalized,
                        layout.Seed);
                }

                Vector2 restCenter = space.NormalizedToPosition(layout.CenterNormalized);
                Vector2 currentCenter = restCenter + new Vector2(pose.Position.x, -pose.Position.y);
                _pointStates.Add(new UIMeshRigPointState(
                    true,
                    layout.CenterNormalized,
                    layout.InnerRadiusNormalized,
                    layout.OuterRadiusNormalized,
                    layout.Strength,
                    falloff,
                    currentCenter,
                    -pose.RotationDegrees,
                    pose.Scale));
            }
        }

        private void HandleAttach(AttachToPanelEvent attachEvent)
        {
            UpdateMotionSchedule();
        }

        private void HandleDetach(DetachFromPanelEvent detachEvent)
        {
            if (_motionSchedule != null)
            {
                _motionSchedule.Pause();
            }
        }

        private void UpdateMotionSchedule()
        {
            if (_motionSchedule == null)
            {
                _motionSchedule = schedule.Execute(MarkDirtyRepaint).Every(16);
            }

            // WHY: with speed 0, no sprite or deformation off every 16 ms repaint redrew identical geometry; the
            // schedule now runs only while the pose depends on time, and the setters repaint once on a change.
            if (panel != null && NeedsTimedRepaint)
            {
                _motionSchedule.Resume();
            }
            else
            {
                _motionSchedule.Pause();
            }
        }
    }
}
