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
            set => _motionSpeed = value;
        }

        [UxmlAttribute]
        public float MotionPhase
        {
            get => _motionPhase;
            set => _motionPhase = value;
        }

        public UIMeshRigGeometry BuildGeometry(Vector2 surfaceSize, float timeSeconds)
        {
            if (_sprite == null || surfaceSize.x <= 0f || surfaceSize.y <= 0f)
            {
                return new UIMeshRigGeometry(new Vector3[0], new int[0], new Vector2[0]);
            }

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
                _pointStates);
        }

        private void GenerateMesh(MeshGenerationContext context)
        {
            Vector2 surfaceSize = contentRect.size;
            UIMeshRigGeometry geometry = BuildGeometry(surfaceSize, Time.realtimeSinceStartup);
            if (geometry.Vertices.Length == 0)
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
            Vertex[] vertices = new Vertex[geometry.Vertices.Length];
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

            ushort[] indices = new ushort[geometry.Indices.Length];
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
            for (int index = 0; index < pointCount; index++)
            {
                UIMeshRigPointLayout layout = UIMeshRigLayoutPresets.GetPoint(_layoutPreset, index);
                UIMeshRigProceduralPose pose = UIMeshRigProceduralPose.Identity;
                if (_motionEnabled && layout.MotionPreset != UIMeshRigMotionPreset.Custom)
                {
                    UIMeshRigMotionProfile profile = UIMeshRigMotionPresets.Create(layout.MotionPreset);
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
                    UIMeshRigFalloffPresets.Create(UIMeshRigFalloffPreset.Smooth),
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

            if (_motionEnabled && panel != null)
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
