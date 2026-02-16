using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Controls cursor visibility and lock state with optional runtime toggle key.
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + nameof(CursorLockController))]
    public class CursorLockController : MonoBehaviour
    {
        [Header("Start State")] [SerializeField]
        private bool _lockOnStart = true;

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