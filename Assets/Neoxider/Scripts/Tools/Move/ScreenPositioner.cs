using Neo.Extensions;
using UnityEngine;

namespace Neo.Tools
{
    public class ScreenPositioner : MonoBehaviour
    {
        [Header("Position Settings")] public bool _useScreenPosition;
        public Vector2 _positionScreen = Vector2.zero;

        [Space] public Vector2 _offsetScreen = Vector2.zero;
        public Vector2 _offset = Vector2.zero;

        [SerializeField] private bool _useDepth;
        [SerializeField] private float _depth = 10f;

        [Header("References")] [SerializeField]
        private Camera _targetCamera;

        [Space] public ScreenEdge _screenEdge = ScreenEdge.BottomLeft;

        private void Start()
        {
            InitializeComponents();
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
                _targetCamera = Camera.main;
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
            var z = transform.position.z;

            if (_useScreenPosition)
                transform.position = _targetCamera.GetWorldPositionAtScreenEdge(
                    ScreenEdge.BottomLeft,
                    _positionScreen,
                    _depth
                );
            else
                transform.position = _targetCamera.GetWorldPositionAtScreenEdge(
                    _screenEdge,
                    _offsetScreen,
                    _depth
                );

            if (!_useDepth)
                transform.SetPosition(z: z);

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