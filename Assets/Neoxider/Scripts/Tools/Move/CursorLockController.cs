using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Controls cursor visibility and lock state. Supports optional apply on Enable/Disable
    ///     and runtime toggle key. Does not conflict with PlayerController3DPhysics / PlayerController2DPhysics:
    ///     they manage cursor independently; use one source of truth per context (e.g. CursorLockController for menu,
    ///     player controller for gameplay).
    /// </summary>
    [NeoDoc("Tools/Move/CursorLockController.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/CursorLockController")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(CursorLockController))]
    public class CursorLockController : MonoBehaviour
    {
        [Header("Start State")] [SerializeField]
        private bool _lockOnStart = true;

        [Header("Lifecycle (optional)")]
        [Tooltip("Apply cursor state when this component is enabled (e.g. when returning to gameplay).")]
        [SerializeField]
        private bool _applyOnEnable;

        [SerializeField] private bool _lockOnEnable = true;

        [Tooltip("Apply cursor state when this component is disabled (e.g. when opening menu/pause).")] [SerializeField]
        private bool _applyOnDisable;

        [SerializeField] private bool _lockOnDisable;

        [Header("Toggle")] [SerializeField] private bool _allowToggle = true;

        [SerializeField] private KeyCode _toggleKey = KeyCode.Escape;

        [Header("Events")] [SerializeField] private UnityEvent _onCursorLocked = new();

        [SerializeField] private UnityEvent _onCursorUnlocked = new();

        /// <summary>
        ///     Gets whether cursor is currently locked.
        /// </summary>
        public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        private void Start()
        {
            SetCursorLocked(_lockOnStart);
        }

        private void Update()
        {
            if (_allowToggle && Input.GetKeyDown(_toggleKey))
            {
                ToggleCursorState();
            }
        }

        private void OnEnable()
        {
            if (_applyOnEnable)
            {
                SetCursorLocked(_lockOnEnable);
            }
        }

        private void OnDisable()
        {
            if (_applyOnDisable)
            {
                SetCursorLocked(_lockOnDisable);
            }
        }

        /// <summary>
        ///     Locks or unlocks cursor and updates visibility.
        /// </summary>
        /// <param name="locked">True to lock and hide cursor, false to unlock and show it.</param>
        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;

            if (locked)
            {
                _onCursorLocked?.Invoke();
            }
            else
            {
                _onCursorUnlocked?.Invoke();
            }
        }

        /// <summary>
        ///     Toggles cursor lock state.
        /// </summary>
        public void ToggleCursorState()
        {
            SetCursorLocked(!IsLocked);
        }

        /// <summary>
        ///     Shows and unlocks cursor.
        /// </summary>
        public void ShowCursor()
        {
            SetCursorLocked(false);
        }

        /// <summary>
        ///     Hides and locks cursor.
        /// </summary>
        public void HideCursor()
        {
            SetCursorLocked(true);
        }
    }
}