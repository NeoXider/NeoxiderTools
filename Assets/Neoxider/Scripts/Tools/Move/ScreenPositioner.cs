using Neo.Extensions;
using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/Move/ScreenPositioner.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/ScreenPositioner")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ScreenPositioner))]
    public class ScreenPositioner : MonoBehaviour
    {
        [Header("Position Settings")]
        [SerializeField] private bool _useScreenPosition;
        [SerializeField] private Vector2 _positionScreen = Vector2.zero;
        [Tooltip("When Use Screen Position is off: edge of screen and pixel offset from it.")]
        [SerializeField] private Vector2 _offsetScreen = Vector2.zero;
        [SerializeField] private Vector2 _offset = Vector2.zero;
        [Tooltip("When on, position is updated every frame (e.g. for moving camera or resolution change).")]
        [SerializeField] private bool _updateEveryFrame;

        [SerializeField] private bool _useDepth;
        [SerializeField] private float _depth = 10f;

        [Header("References")]
        [SerializeField] private Camera _targetCamera;
        [Tooltip("Screen edge used as anchor. For Use Screen Position mode this is the edge from which Position Screen offset is applied.")]
        [SerializeField] private ScreenEdge _screenEdge = ScreenEdge.BottomLeft;

        private void Start()
        {
            InitializeComponents();
            UpdatePositionAndRotation();
        }

        private void LateUpdate()
        {
            if (_updateEveryFrame)
                UpdatePositionAndRotation();
        }

        private void OnValidate()
        {
            InitializeComponents();
            UpdatePositionAndRotation();
        }

        private void InitializeComponents()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
        }

        [Button("Update Position")]
        private void UpdatePositionAndRotation()
        {
            if (_targetCamera == null)
            {
                Debug.LogError("Camera reference is missing!");
                return;
            }

            ApplyScreenPosition();
        }

        private void ApplyScreenPosition()
        {
            if (_targetCamera == null)
            {
                Debug.LogError("[ScreenPositioner] Camera reference is missing. Cannot apply position.", this);
                return;
            }

            float z = transform.position.z;

            if (_useScreenPosition)
            {
                transform.position = _targetCamera.GetWorldPositionAtScreenEdge(
                    _screenEdge,
                    _positionScreen,
                    _depth
                );
            }
            else
            {
                transform.position = _targetCamera.GetWorldPositionAtScreenEdge(
                    _screenEdge,
                    _offsetScreen,
                    _depth
                );
            }

            if (!_useDepth)
            {
                transform.SetPosition(z: z);
            }

            transform.AddPosition(_offset);
        }

        public void Configure(ScreenEdge edge, Vector2 offset, float depth)
        {
            _screenEdge = edge;
            _offsetScreen = offset;
            _depth = depth;
            UpdatePositionAndRotation();
        }
    }
}