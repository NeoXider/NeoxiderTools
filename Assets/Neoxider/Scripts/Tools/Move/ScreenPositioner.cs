using System;
using System.Collections;
using System.Collections.Generic;
using Neo;
using UnityEngine;

namespace Neo.Tools
{
    public class ScreenPositioner : MonoBehaviour
    {
        [Header("Position Settings")]
        [SerializeField] private ScreenEdge _screenEdge = ScreenEdge.TopLeft;
        [SerializeField] private Vector2 _offset = Vector2.zero;
        [SerializeField] private bool _useDepth;
        [SerializeField] private float _depth = 10f;
    
        [Header("Rotation Settings")]
        [SerializeField] private float _angle = 0;

        [Header("References")]
        [SerializeField] private Camera _targetCamera;

        private void Start()
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
            ApplyRotation();
        }

        private void ApplyScreenPosition()
        {
            float z = transform.position.z;
            transform.position = _targetCamera.GetWorldPositionAtScreenEdge(
                _screenEdge,
                _offset,
                _depth
            );
            
            if(!_useDepth)
                transform.SetPosition(z: z);
        }
        
        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(0, 0, _angle);
        }

        private void OnValidate()
        {
            InitializeComponents();
            UpdatePositionAndRotation();
        }

        public void Configure(ScreenEdge edge, Vector2 offset, float depth)
        {
            _screenEdge = edge;
            _offset = offset;
            _depth = depth;
            UpdatePositionAndRotation();
        }
    }
}