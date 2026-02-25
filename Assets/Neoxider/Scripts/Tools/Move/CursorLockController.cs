using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Controls cursor visibility and lock state. Supports optional apply on Enable/Disable
    ///     and runtime toggle key. For New Input System, call SetCursorLocked or ToggleCursorState from your action's callback.
    /// </summary>
    [NeoDoc("Tools/Move/CursorLockController.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/CursorLockController")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(CursorLockController))]
    public class CursorLockController : MonoBehaviour
    {
        public enum CursorStateMode
        {
            LockAndHide,
            OnlyHide,
            OnlyLock
        }

        [Header("Mode")]
        [Tooltip("LockAndHide = lock + hide; OnlyHide = visibility only; OnlyLock = lock only.")]
        [SerializeField] private CursorStateMode _mode = CursorStateMode.LockAndHide;

        [Header("Start State")]
        [SerializeField] private bool _lockOnStart = true;

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
            if (_allowToggle && KeyInputCompat.GetKeyDown(_toggleKey))
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
        ///     Applies cursor state according to Mode: lock and/or visibility.
        /// </summary>
        /// <param name="locked">True = apply "locked" state (lock and/or hide by mode), false = apply "unlocked" state.</param>
        public void SetCursorLocked(bool locked)
        {
            switch (_mode)
            {
                case CursorStateMode.LockAndHide:
                    Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
                    Cursor.visible = !locked;
                    break;
                case CursorStateMode.OnlyHide:
                    Cursor.visible = !locked;
                    break;
                case CursorStateMode.OnlyLock:
                    Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
                    break;
            }

            if (locked)
                _onCursorLocked?.Invoke();
            else
                _onCursorUnlocked?.Invoke();
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