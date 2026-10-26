using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Pause behaviour when this GameObject is active: optional time scale, GM pause/resume, and cursor show/hide.
    ///     Active pages share first/last ownership of time, GM pause and cursor state without scene wiring.
    ///     Each channel restores its original baseline only after its final requesting page closes.
    /// </summary>
    [NeoDoc("UI/PausePage.md")]
    [CreateFromMenu("Neoxider/Tools/Other/PausePage")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(PausePage))]
    public class PausePage : MonoBehaviour
    {
        /// <summary>How to restore the cursor after the pause page is closed.</summary>
        public enum AfterPauseCursorBehavior
        {
            /// <summary>Restore <see cref="Cursor.lockState"/> and <see cref="Cursor.visible"/> from before pause opened.</summary>
            RestorePrevious,

            /// <summary>Always hide and lock the cursor (typical FPS after closing pause).</summary>
            ForceLockedHidden
        }

        [Header("Time")]
        [Tooltip("When enabled, sets Time.timeScale on pause and restores it on resume.")]
        [SerializeField]
        private bool _useTimeScale = true;

        [Tooltip("Time scale while this pause page is active (0 = full pause).")] [SerializeField] [Min(0f)]
        private float _timeScaleOnPause;

        [Header("Game Manager")]
        [Tooltip("Shares a GM pause with other pages; resumes only the exact pause acquired by the first requester.")]
        [SerializeField]
        private bool _sendPause = true;

        [Header("Cursor")]
        [Tooltip("When enabled, shows and unlocks cursor while pause is active; restores or applies lock on disable.")]
        [SerializeField]
        private bool _controlCursor;

        [Tooltip(
            "Default: restore cursor as before pause. ForceLockedHidden: after pause always lock and hide (FPS).")]
        [SerializeField]
        private AfterPauseCursorBehavior _afterPauseCursor = AfterPauseCursorBehavior.RestorePrevious;

        private const int MaxSynchronizationPasses = 16;
        private static readonly List<PausePage> ActivePages = new List<PausePage>();
        private static bool _hasTimeBaseline;
        private static float _timeBaseline;
        private static bool _hasCursorBaseline;
        private static bool _cursorBaselineVisible;
        private static CursorLockMode _cursorBaselineLock;
        private static bool _forceLockOnFinalCursorRelease;
        private static bool _managerAttempted;
        private static GM _pausedManager;
        private static ulong _managerPauseVersion;
        private static bool _managerControlsTime;
        private static bool _synchronizing;
        private static bool _synchronizationRequested;
        private static int _session;
        private static int _lastUpdateFrame = -1;

        private bool _registered;
        private int _registeredSession;
        private bool _requestsTime;
        private float _requestedTimeScale;
        private bool _requestsManager;
        private bool _requestsCursor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedState()
        {
            ActivePages.Clear();
            _hasTimeBaseline = false;
            _hasCursorBaseline = false;
            _forceLockOnFinalCursorRelease = false;
            _managerAttempted = false;
            _pausedManager = null;
            _managerControlsTime = false;
            _synchronizing = false;
            _synchronizationRequested = false;
            _lastUpdateFrame = -1;
            unchecked
            {
                _session++;
            }
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            if (!_registered || _registeredSession != _session)
            {
                _registered = false;
                return;
            }

            _registered = false;
            ActivePages.Remove(this);
            Synchronize();
        }

        private void LateUpdate()
        {
            // Also repopulate active components when both domain and scene reload are disabled.
            if (!_registered || _registeredSession != _session)
            {
                Register();
            }

            if (_lastUpdateFrame != Time.frameCount)
            {
                _lastUpdateFrame = Time.frameCount;
                Synchronize();
            }
        }

        private void Register()
        {
            if (_registered && _registeredSession == _session)
            {
                return;
            }

            _registered = true;
            _registeredSession = _session;
            _requestsTime = _useTimeScale;
            _requestedTimeScale = float.IsNaN(_timeScaleOnPause) || float.IsInfinity(_timeScaleOnPause)
                ? 0f
                : Mathf.Max(0f, _timeScaleOnPause);
            _requestsManager = _sendPause;
            _requestsCursor = _controlCursor;
            if (_requestsTime)
            {
                CaptureTimeBaseline();
            }

            if (_requestsCursor)
            {
                if (!_hasCursorBaseline)
                {
                    _cursorBaselineLock = Cursor.lockState;
                    _cursorBaselineVisible = Cursor.visible;
                    _hasCursorBaseline = true;
                }

                _forceLockOnFinalCursorRelease |=
                    _afterPauseCursor == AfterPauseCursorBehavior.ForceLockedHidden;
            }

            ActivePages.Add(this);
            Synchronize();
        }

        private static void Synchronize()
        {
            _synchronizationRequested = true;
            if (_synchronizing)
            {
                return;
            }

            _synchronizing = true;
            Exception failure = null;
            try
            {
                for (int pass = 0; pass < MaxSynchronizationPasses; pass++)
                {
                    _synchronizationRequested = false;
                    ReadRequests(out bool time, out float scale, out bool manager, out bool cursor);
                    if (!manager)
                    {
                        ReleaseManager();
                    }
                    else if (!_managerAttempted)
                    {
                        AcquireManager();
                    }

                    // A GM observer may close the last page while Pause is still on the stack.
                    ReadRequests(out time, out scale, out manager, out cursor);
                    if (!manager)
                    {
                        ReleaseManager();
                    }

                    ReadRequests(out time, out scale, out manager, out cursor);
                    ApplyTime(time, scale);
                    ApplyCursor(cursor);
                    if (!_synchronizationRequested)
                    {
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                // Restore channels even if an external GM observer throws after committing its state.
                ReadRequests(out bool time, out float scale, out bool manager, out bool cursor);
                if (!manager)
                {
                    try
                    {
                        ReleaseManager();
                    }
                    catch (Exception exception)
                    {
                        failure ??= exception;
                    }
                }

                ReadRequests(out time, out scale, out manager, out cursor);
                try
                {
                    ApplyTime(time, scale);
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }

                try
                {
                    ApplyCursor(cursor);
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }

                _synchronizing = false;
            }

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        private static void ReadRequests(out bool time, out float scale, out bool manager, out bool cursor)
        {
            time = false;
            scale = float.PositiveInfinity;
            manager = false;
            cursor = false;
            for (int index = 0; index < ActivePages.Count; index++)
            {
                PausePage page = ActivePages[index];
                if (page == null || !page._registered || page._registeredSession != _session)
                {
                    continue;
                }

                if (page._requestsTime)
                {
                    time = true;
                    scale = Mathf.Min(scale, page._requestedTimeScale);
                }

                manager |= page._requestsManager;
                cursor |= page._requestsCursor;
            }
        }

        private static void CaptureTimeBaseline()
        {
            if (!_hasTimeBaseline)
            {
                _timeBaseline = Time.timeScale;
                _hasTimeBaseline = true;
            }
        }

        private static void AcquireManager()
        {
            _managerAttempted = true;
            GM manager = GM.I;
            if (manager == null || manager.State == GM.GameState.Pause)
            {
                return;
            }

            bool controlsTime = manager.useTimeScalePause;
            if (controlsTime)
            {
                CaptureTimeBaseline();
                // Other pages may already hold time at zero. GM must capture the true baseline.
                Time.timeScale = _timeBaseline;
            }
            else
            {
                ReadRequests(out bool time, out float scale, out bool managerRequested, out bool cursor);
                ApplyTime(time, scale);
            }

            ulong expectedVersion = unchecked(manager.TransitionVersion + 1UL);
            try
            {
                manager.Pause();
            }
            finally
            {
                if (manager != null && manager.State == GM.GameState.Pause &&
                    manager.TransitionVersion == expectedVersion)
                {
                    _pausedManager = manager;
                    _managerPauseVersion = expectedVersion;
                    _managerControlsTime = controlsTime;
                }
            }
        }

        private static bool OwnsManagerPause()
        {
            return _pausedManager != null && _pausedManager.State == GM.GameState.Pause &&
                   _pausedManager.TransitionVersion == _managerPauseVersion &&
                   GM.TryGetInstance(out GM current) && ReferenceEquals(current, _pausedManager);
        }

        private static void ReleaseManager()
        {
            GM manager = OwnsManagerPause() ? _pausedManager : null;
            _pausedManager = null;
            _managerControlsTime = false;
            _managerAttempted = false;
            if (manager != null)
            {
                // GM restores its capture before broadcasting Resume; page time is restored afterwards.
                manager.Resume();
            }
        }

        private static void ApplyTime(bool requested, float scale)
        {
            bool managerTime = _managerControlsTime && OwnsManagerPause();
            if (requested || managerTime)
            {
                CaptureTimeBaseline();
                Time.timeScale = managerTime ? 0f : scale;
            }
            else if (_hasTimeBaseline)
            {
                float baseline = _timeBaseline;
                _hasTimeBaseline = false;
                // A foreign/replacement GM may now own its own pause. Releasing page time cannot resume it.
                bool foreignManagerTime = GM.TryGetInstance(out GM current) &&
                                          current.State == GM.GameState.Pause && current.useTimeScalePause;
                Time.timeScale = foreignManagerTime ? 0f : baseline;
            }
        }

        private static void ApplyCursor(bool requested)
        {
            if (requested)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (_hasCursorBaseline)
            {
                CursorLockMode lockState = _forceLockOnFinalCursorRelease
                    ? CursorLockMode.Locked
                    : _cursorBaselineLock;
                bool visible = !_forceLockOnFinalCursorRelease && _cursorBaselineVisible;
                _hasCursorBaseline = false;
                _forceLockOnFinalCursorRelease = false;
                Cursor.lockState = lockState;
                Cursor.visible = visible;
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
