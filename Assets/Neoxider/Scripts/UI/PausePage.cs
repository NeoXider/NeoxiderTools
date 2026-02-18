using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Pause behaviour when this GameObject is active: optional time scale, GM pause/resume, and cursor show/hide.
    ///     On disable, previous time scale and cursor state are restored so it works with CursorLockController
    ///     and PlayerController3DPhysics / PlayerController2DPhysics without conflict.
    /// </summary>
    [NeoDoc("UI/PausePage.md")]
    [CreateFromMenu("Neoxider/Tools/Other/PausePage")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(PausePage))]
    public class PausePage : MonoBehaviour
    {
        [Header("Time")]
        [Tooltip("When enabled, sets Time.timeScale on pause and restores it on resume.")]
        [SerializeField]
        private bool _useTimeScale = true;

        [Tooltip("Time scale while this pause page is active (0 = full pause).")] [SerializeField] [Min(0f)]
        private float _timeScaleOnPause;

        [Header("Game Manager")]
        [Tooltip("When enabled, calls GM.I.Pause() on enable and GM.I.Resume() on disable.")]
        [SerializeField]
        private bool _sendPause = true;

        [Header("Cursor")]
        [Tooltip("When enabled, shows and unlocks cursor while pause is active; restores previous state on disable.")]
        [SerializeField]
        private bool _controlCursor;

        private bool _savedCursorVisible;
        private CursorLockMode _savedLockState;

        private float _savedTimeScale;

        private void OnEnable()
        {
            _savedTimeScale = Time.timeScale;

            if (_useTimeScale)
            {
                Time.timeScale = _timeScaleOnPause;
            }

            if (_sendPause)
            {
                GM.I?.Pause();
            }

            if (_controlCursor)
            {
                _savedLockState = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnDisable()
        {
            if (_useTimeScale)
            {
                Time.timeScale = _savedTimeScale;
            }

            if (_sendPause)
            {
                GM.I?.Resume();
            }

            if (_controlCursor)
            {
                Cursor.lockState = _savedLockState;
                Cursor.visible = _savedCursorVisible;
            }
        }

        private void OnValidate()
        {
            if (TryGetComponent(out Animator animator))
            {
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }
    }
}