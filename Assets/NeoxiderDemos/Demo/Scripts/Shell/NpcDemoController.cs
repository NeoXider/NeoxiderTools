using System;
using Neo.NPC;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.NPC</b> demo scene (Ai/AiMoveExample.unity). The four NPCs, the patrol
    ///     route, the sentry zone, the world labels, the control card and the info card are saved in the scene by its
    ///     DemoSceneKit builder; this component builds nothing. Walk / Run / Stop / Resume call
    ///     <see cref="NpcNavigation" /> directly (NoCode); the mode buttons call <see cref="SetGuardMode" /> because a
    ///     persistent UnityEvent cannot pass an enum. The NPC events (onModeChanged, onSpeedChanged,
    ///     onMovementStarted / Stopped, onStartFollowing / onStopFollowing, onPatrolPointReached) are wired to the
    ///     public listener methods below in the Inspector.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/NPC Demo")]
    public sealed class NpcDemoController : MonoBehaviour
    {
        [Header("NPCs")]
        [Tooltip("Blue NPC in FollowTarget mode (chases the orbiting target).")]
        [SerializeField] private NpcNavigation _chaser;

        [Tooltip("White NPC whose mode the card switches (Follow / Patrol / Combined).")]
        [SerializeField] private NpcNavigation _guard;

        [Tooltip("Green NPC walking the patrol route.")]
        [SerializeField] private NpcNavigation _patroller;

        [Tooltip("Orange NPC in Combined mode (patrol zone + aggro).")]
        [SerializeField] private NpcNavigation _sentry;

        [Header("Live values")]
        [SerializeField] private TMP_Text _chaserValue;
        [SerializeField] private TMP_Text _guardValue;
        [SerializeField] private TMP_Text _sentryValue;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private bool _chaserMoving;
        private float _chaserSpeed;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned (the scene builder ran).</summary>
        public bool IsSceneBuilt => _chaser != null && _guard != null && _patroller != null && _sentry != null &&
                                    _chaserValue != null && _guardValue != null && _sentryValue != null && _log != null;

        public NpcNavigation Chaser => _chaser;
        public NpcNavigation Guard => _guard;
        public NpcNavigation Patroller => _patroller;
        public NpcNavigation Sentry => _sentry;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            if (_chaser != null && _chaser.TryGetComponent(out NavMeshAgent agent))
            {
                _chaserSpeed = agent.speed;
            }

            RefreshChaser();
            if (_guard != null)
            {
                SetLabel(_guardValue, _guard.Mode.ToString());
            }

            if (_sentry != null)
            {
                SetLabel(_sentryValue, _sentry.IsChasing ? "CHASING" : "patrolling");
            }

            Log("4 NPCs live - one NpcNavigation per NavMeshAgent");
        }

        /// <summary>Button target: switches the guard to <see cref="NpcNavigation.NavigationMode" /> by its value.</summary>
        public void SetGuardMode(int mode)
        {
            if (_guard == null)
            {
                WarnUnbuilt();
                return;
            }

            if (!Enum.IsDefined(typeof(NpcNavigation.NavigationMode), mode))
            {
                Log("Ignored unknown navigation mode " + mode);
                return;
            }

            NpcNavigation.NavigationMode value = (NpcNavigation.NavigationMode)mode;
            if (_guard.Mode == value)
            {
                Log("guard is already in " + value + " mode");
                return;
            }

            _guard.SetMode(value);
            Log("guard.SetMode(NavigationMode." + value + ")");
        }

        /// <summary>Listener of the guard's onModeChanged.</summary>
        public void OnGuardModeChanged(NpcNavigation.NavigationMode mode)
        {
            SetLabel(_guardValue, mode.ToString());
        }

        /// <summary>Listener of the chaser's onMovementStarted.</summary>
        public void OnChaserStarted()
        {
            _chaserMoving = true;
            RefreshChaser();
        }

        /// <summary>Listener of the chaser's onMovementStopped.</summary>
        public void OnChaserStopped()
        {
            _chaserMoving = false;
            RefreshChaser();
        }

        /// <summary>Listener of the chaser's onSpeedChanged (Walk / Run).</summary>
        public void OnChaserSpeedChanged(float speed)
        {
            _chaserSpeed = speed;
            RefreshChaser();
            Log("chaser onSpeedChanged -> " + speed.ToString("0.0") + " m/s");
        }

        /// <summary>Listener of the sentry's onStartFollowing.</summary>
        public void OnSentryChaseStarted()
        {
            SetLabel(_sentryValue, "<color=#" + ColorUtility.ToHtmlStringRGB(DemoPalette.Danger) + ">CHASING</color>");
            Log("sentry onStartFollowing - target inside aggro range");
        }

        /// <summary>Listener of the sentry's onStopFollowing.</summary>
        public void OnSentryChaseStopped()
        {
            SetLabel(_sentryValue, "patrolling");
            Log("sentry onStopFollowing - back to its zone");
        }

        /// <summary>Listener of the patroller's onPatrolPointReached.</summary>
        public void OnPatrolPointReached(int index)
        {
            Log("patroller onPatrolPointReached(" + index + ")");
        }

        private void RefreshChaser()
        {
            string state = _chaser != null && !_chaser.IsActive ? "stopped" : _chaserMoving ? "moving" : "waiting";
            SetLabel(_chaserValue, state + " · " + _chaserSpeed.ToString("0.0") + " m/s");
        }

        private static void SetLabel(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[NpcDemoController] The NPC demo scene is not authored. Run Neoxider/Demo/Rebuild Active Demo Scene.",
                this);
        }
    }
}
