using Neo.StateMachine;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.StateMachine</b> demo scene. The scene saves the world, the
    ///     UI, the NoCode guard and every persistent listener in StateMachineDemo.unity; this component builds nothing
    ///     at runtime and only references those scene objects.
    ///     <para>
    ///         Code path: a real <see cref="StateMachine{TState}" /> of <see cref="IState" /> classes drives the
    ///         "Agent" dot - Idle, Patrol, Chase, Attack. Each state recolors and moves the dot in its own
    ///         <c>OnEnter</c> / <c>OnUpdate</c>, the machine ticks from <see cref="Update" />, Provoke / Calm call
    ///         <c>ChangeState</c> and every transition is logged from <see cref="StateMachine{TState}.OnStateChanged" />.
    ///     </para>
    ///     <para>
    ///         NoCode path: the "Guard" object runs a <see cref="StateMachineBehaviourBase" /> with a saved
    ///         StateMachineData asset. Its transitions, state actions and the buttons that call it are authored in the
    ///         scene and the data; the behaviour's OnStateChanged event calls <see cref="OnGuardStateChanged" /> here only
    ///         to show and log the change.
    ///     </para>
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/State Machine Demo")]
    public sealed class StateMachineDemoController : MonoBehaviour
    {
        private static readonly Color TealIdle = new(0.24f, 0.83f, 0.79f);
        private static readonly Color GreenPatrol = new(0.35f, 0.82f, 0.52f);
        private static readonly Color OrangeChase = new(1f, 0.62f, 0.22f);
        private static readonly Color RedAttack = new(1f, 0.32f, 0.38f);

        private const float CatchDistance = 0.6f;
        private const float PreyVerticalRange = 0.7f;
        private const float AgentScale = 0.85f;

        [Header("Code path · Agent")]
        [Tooltip("Dot moved by the C# states. Its start position is the center of the patrol lane.")]
        [SerializeField] private Transform _agent;

        [SerializeField] private SpriteRenderer _agentRenderer;

        [Tooltip("Target that Provoke places on the lane; the agent chases it.")]
        [SerializeField] private Transform _prey;

        [SerializeField] private SpriteRenderer _preyRenderer;

        [Tooltip("Half width of the patrol lane around the agent's start position (world units).")]
        [SerializeField] [Min(0.5f)] private float _laneHalfWidth = 3f;

        [Tooltip("Seconds Idle waits before it changes to Patrol by itself.")]
        [SerializeField] [Min(0f)] private float _idleToPatrolDelay = 2.5f;

        [Tooltip("Seconds of Attack before the prey escapes and the chase resumes.")]
        [SerializeField] [Min(0.1f)] private float _attackDuration = 1.1f;

        [Header("Code path · UI")]
        [SerializeField] private TMP_Text _stateLabel;

        [SerializeField] private TMP_Text _previousLabel;
        [SerializeField] private TMP_Text _timeLabel;

        [Header("NoCode path · Guard")]
        [Tooltip("Scene StateMachineBehaviourBase that runs the saved Guard StateMachineData.")]
        [SerializeField] private StateMachineBehaviourBase _guard;

        [SerializeField] private TMP_Text _guardStateLabel;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private StateMachine<IState> _machine;
        private IdleState _idle;
        private PatrolState _patrol;
        private ChaseState _chase;
        private AttackState _attack;
        private Vector3 _home;
        private float _stateEnterTime;
        private int _shownTenths = -1;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _agent != null && _agentRenderer != null && _prey != null && _preyRenderer != null &&
                                    _stateLabel != null && _guard != null && _log != null;

        /// <summary>The code-path machine (created in Awake, started in Start when the scene is built).</summary>
        public StateMachine<IState> AgentMachine => _machine;

        /// <summary>Name of the agent's current state ("Idle", "Patrol", "Chase", "Attack"); empty when stopped.</summary>
        public string AgentStateName => _machine != null && _machine.CurrentState is AgentState state ? state.Name : string.Empty;

        /// <summary>NoCode guard driven by StateMachineData.</summary>
        public StateMachineBehaviourBase Guard => _guard;

        private void Awake()
        {
            _machine = new StateMachine<IState>();
            _idle = new IdleState(this);
            _patrol = new PatrolState(this);
            _chase = new ChaseState(this);
            _attack = new AttackState(this);
            _machine.OnStateChanged.AddListener(HandleStateChanged);
        }

        private void Start()
        {
            if (_agent == null || _agentRenderer == null)
            {
                WarnUnbuilt();
                return;
            }

            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            _home = _agent.position;
            if (_preyRenderer != null)
            {
                _preyRenderer.enabled = false;
            }

            _machine.ChangeState(_idle);
            if (_guard != null && _guardStateLabel != null && _guard.HasCurrentState)
            {
                _guardStateLabel.text = _guard.CurrentStateName;
            }

            Log("StateMachine<IState> started in Idle");
        }

        private void Update()
        {
            if (_machine == null || _machine.CurrentState == null)
            {
                return;
            }

            _machine.Update();
            RefreshTimeLabel();
        }

        private void OnDestroy()
        {
            if (_machine == null)
            {
                return;
            }

            _machine.OnStateChanged.RemoveListener(HandleStateChanged);
            _machine.Stop();
            _machine.ClearStateCache();
            _machine.ClearTransitionCache();
        }

        /// <summary>Button target: puts the prey on the lane and calls <c>machine.ChangeState(chase)</c>.</summary>
        public void Provoke()
        {
            if (!CanDrive())
            {
                return;
            }

            RelocatePrey();
            if (_preyRenderer != null)
            {
                _preyRenderer.enabled = true;
            }

            _machine.ChangeState(_chase);
        }

        /// <summary>Button target: hides the prey and calls <c>machine.ChangeState(idle)</c>.</summary>
        public void Calm()
        {
            if (!CanDrive())
            {
                return;
            }

            if (_preyRenderer != null)
            {
                _preyRenderer.enabled = false;
            }

            _machine.ChangeState(_idle);
        }

        /// <summary>
        ///     Persistent listener of the guard's <see cref="StateMachineBehaviourBase.OnStateChanged" />
        ///     (previous name, current name; empty means no state).
        /// </summary>
        public void OnGuardStateChanged(string previousState, string currentState)
        {
            string from = string.IsNullOrEmpty(previousState) ? "none" : previousState;
            string to = string.IsNullOrEmpty(currentState) ? "none" : currentState;
            if (_guardStateLabel != null)
            {
                _guardStateLabel.text = string.IsNullOrEmpty(currentState) ? "-" : currentState;
            }

            Log("Guard (NoCode): " + from + " -> " + to);
        }

        /// <summary>Noise toggle listener: explains what the guard's transition predicate will do.</summary>
        public void LogNoise(bool on)
        {
            Log(on
                ? "Noise on: predicate Toggle.isOn == true lets Sleep -> Alert fire"
                : "Noise off: Alert -> Sleep fires after 1.5 s of silence");
        }

        private bool CanDrive()
        {
            if (_machine != null && _agent != null && _agentRenderer != null && _prey != null)
            {
                return true;
            }

            WarnUnbuilt();
            return false;
        }

        private void HandleStateChanged(IState previous, IState current)
        {
            _stateEnterTime = Time.time;
            _shownTenths = -1;
            string from = previous is AgentState p ? p.Name : "none";
            string to = current is AgentState c ? c.Name : "none";
            Log("machine.ChangeState: " + from + " -> " + to);

            if (_stateLabel != null)
            {
                _stateLabel.text = to.ToUpperInvariant();
            }

            if (_previousLabel != null)
            {
                _previousLabel.text = from;
            }

            RefreshTimeLabel();
        }

        private void RefreshTimeLabel()
        {
            if (_timeLabel == null)
            {
                return;
            }

            // WHY: rewrite the label only when the shown tenth changes instead of formatting a string every frame.
            int tenths = Mathf.FloorToInt(Mathf.Max(0f, Time.time - _stateEnterTime) * 10f);
            if (tenths == _shownTenths)
            {
                return;
            }

            _shownTenths = tenths;
            _timeLabel.text = (tenths / 10f).ToString("0.0") + " s";
        }

        private void RelocatePrey()
        {
            if (_prey == null)
            {
                return;
            }

            float x = _home.x + Random.Range(-_laneHalfWidth, _laneHalfWidth);
            float y = _home.y + Random.Range(-PreyVerticalRange, PreyVerticalRange);
            _prey.position = new Vector3(x, y, _home.z);
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
                "[StateMachineDemoController] The scene is not authored. Re-import the scene from the Demo download.",
                this);
        }

        /// <summary>Base of the C# states: each one recolors and moves the shared agent dot.</summary>
        private abstract class AgentState : IState
        {
            protected readonly StateMachineDemoController C;

            protected AgentState(StateMachineDemoController controller)
            {
                C = controller;
            }

            public abstract string Name { get; }
            protected abstract Color Tint { get; }

            public virtual void OnEnter()
            {
                if (C._agentRenderer != null)
                {
                    C._agentRenderer.color = Tint;
                }
            }

            public virtual void OnUpdate()
            {
            }

            public virtual void OnExit()
            {
            }

            public void OnFixedUpdate()
            {
            }

            public void OnLateUpdate()
            {
            }

            protected float Elapsed => Time.time - C._stateEnterTime;

            protected void MoveAgent(Vector3 target, float speed)
            {
                C._agent.position = Vector3.MoveTowards(C._agent.position, target, speed * Time.deltaTime);
            }
        }

        private sealed class IdleState : AgentState
        {
            public IdleState(StateMachineDemoController c) : base(c)
            {
            }

            public override string Name => "Idle";
            protected override Color Tint => TealIdle;

            public override void OnUpdate()
            {
                float bob = Mathf.Sin(Time.time * 2f) * 0.12f;
                MoveAgent(C._home + new Vector3(0f, bob, 0f), 3.5f);
                if (Elapsed >= C._idleToPatrolDelay)
                {
                    C._machine.ChangeState(C._patrol);
                }
            }
        }

        private sealed class PatrolState : AgentState
        {
            public PatrolState(StateMachineDemoController c) : base(c)
            {
            }

            public override string Name => "Patrol";
            protected override Color Tint => GreenPatrol;

            public override void OnUpdate()
            {
                float x = Mathf.PingPong(Time.time * 3.2f, C._laneHalfWidth * 2f) - C._laneHalfWidth;
                float bob = Mathf.Sin(Time.time * 3f) * 0.1f;
                MoveAgent(C._home + new Vector3(x, bob, 0f), 6f);
            }
        }

        private sealed class ChaseState : AgentState
        {
            public ChaseState(StateMachineDemoController c) : base(c)
            {
            }

            public override string Name => "Chase";
            protected override Color Tint => OrangeChase;

            public override void OnUpdate()
            {
                Vector3 prey = C._prey.position;
                MoveAgent(prey, 5.5f);
                if (Vector3.Distance(C._agent.position, prey) <= CatchDistance)
                {
                    C._machine.ChangeState(C._attack);
                }
            }
        }

        private sealed class AttackState : AgentState
        {
            public AttackState(StateMachineDemoController c) : base(c)
            {
            }

            public override string Name => "Attack";
            protected override Color Tint => RedAttack;

            public override void OnUpdate()
            {
                float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 0.35f;
                C._agent.localScale = Vector3.one * (AgentScale * pulse);
                if (Elapsed >= C._attackDuration)
                {
                    C.RelocatePrey();
                    C._machine.ChangeState(C._chase);
                }
            }

            public override void OnExit()
            {
                if (C._agent != null)
                {
                    C._agent.localScale = Vector3.one * AgentScale;
                }
            }
        }
    }
}
