using System;
using System.Collections.Generic;
using Neo.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Keeps world-input components (Drawer, MouseMover2D, MouseEffect...) from reacting to the demo UI. While the
    ///     mouse button is up, the listed behaviours are disabled whenever the pointer is over canvas UI and enabled
    ///     again when it leaves, so a press on a button, slider or card never reaches them, not even in a
    ///     FixedUpdate that runs before this frame's Update. The state never changes while the button is held or on
    ///     the release frame, so a stroke or drag that started in the scene keeps working across the UI and always
    ///     sees its own release. Runs before default-order scripts.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/Demo Pointer Gate")]
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class DemoPointerGate : MonoBehaviour
    {
        [Tooltip("World-input behaviours that are disabled while the pointer is over canvas UI.")]
        [SerializeField] private Behaviour[] _gated = Array.Empty<Behaviour>();

        [Tooltip("Canvas raycasters that count as UI. Empty = every raycaster of the current EventSystem, keeping " +
                 "only GraphicRaycaster hits (slower: physics raycasters run too).")]
        [SerializeField] private GraphicRaycaster[] _uiRaycasters = Array.Empty<GraphicRaycaster>();

        private readonly List<RaycastResult> _hits = new();
        private bool[] _restore = Array.Empty<bool>();
        private PointerEventData _pointer;
        private EventSystem _pointerOwner;
        private bool _blocked;

        /// <summary>True while the gated behaviours are disabled because the pointer is on the UI.</summary>
        public bool IsBlocking => _blocked;

        public int GatedCount => _gated.Length;

        private void Update()
        {
            bool pressed = MouseInputCompat.TryGetButtonDown(0, out bool down) && down;
            bool held = MouseInputCompat.TryGetButton(0, out bool isHeld) && isHeld;
            bool released = MouseInputCompat.TryGetButtonUp(0, out bool up) && up;
            if (!pressed && (held || released))
            {
                return;
            }

            SetBlocked(IsPointerOverUi());
        }

        private void OnDisable()
        {
            SetBlocked(false);
        }

        /// <summary>Replaces the gated behaviours (C# setup; scenes assign them in the Inspector).</summary>
        public void SetGated(params Behaviour[] behaviours)
        {
            SetBlocked(false);
            _gated = behaviours ?? Array.Empty<Behaviour>();
        }

        private void SetBlocked(bool blocked)
        {
            if (blocked == _blocked)
            {
                return;
            }

            _blocked = blocked;
            if (blocked)
            {
                if (_restore.Length != _gated.Length)
                {
                    _restore = new bool[_gated.Length];
                }

                for (int i = 0; i < _gated.Length; i++)
                {
                    Behaviour behaviour = _gated[i];
                    _restore[i] = behaviour != null && behaviour.enabled;
                    if (_restore[i])
                    {
                        behaviour.enabled = false;
                    }
                }

                return;
            }

            int count = Mathf.Min(_restore.Length, _gated.Length);
            for (int i = 0; i < count; i++)
            {
                if (_restore[i] && _gated[i] != null)
                {
                    _gated[i].enabled = true;
                }

                _restore[i] = false;
            }
        }

        /// <summary>
        ///     True when a GraphicRaycaster (canvas UI) is under the pointer. Unlike
        ///     EventSystem.IsPointerOverGameObject, colliders hit by a Physics2DRaycaster on the camera do not count.
        /// </summary>
        private bool IsPointerOverUi()
        {
            EventSystem current = EventSystem.current;
            if (current == null || !MouseInputCompat.TryGetPosition(out Vector3 position))
            {
                return false;
            }

            if (_pointer == null || _pointerOwner != current)
            {
                _pointer = new PointerEventData(current);
                _pointerOwner = current;
            }

            _pointer.Reset();
            _pointer.position = position;
            _hits.Clear();
            bool any = false;
            foreach (GraphicRaycaster raycaster in _uiRaycasters)
            {
                if (raycaster != null && raycaster.isActiveAndEnabled)
                {
                    any = true;
                    raycaster.Raycast(_pointer, _hits);
                }
            }

            if (!any)
            {
                current.RaycastAll(_pointer, _hits);
            }

            bool overUi = false;
            for (int i = 0; i < _hits.Count && !overUi; i++)
            {
                overUi = _hits[i].module is GraphicRaycaster;
            }

            _hits.Clear();
            return overUi;
        }
    }
}
