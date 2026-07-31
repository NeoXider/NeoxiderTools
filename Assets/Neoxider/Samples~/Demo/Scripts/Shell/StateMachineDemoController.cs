using Neo.Samples.Survivor;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Bright, self-contained demo for <b>Neo.StateMachine</b> — the pure C# code path (no NoCode assets).
    ///     A single colored dot is a tiny agent driven by a real <see cref="StateMachine{TState}" /> of
    ///     <see cref="IState" /> classes: Idle → Patrol → Chase → Attack. Each state changes the dot's color and
    ///     movement inside its own <c>OnEnter</c>/<c>OnUpdate</c>, and the machine ticks via <c>Update()</c> every
    ///     frame. "Provoke" calls <c>ChangeState(Chase)</c>, "Calm" calls <c>ChangeState(Idle)</c>, and Idle
    ///     auto-advances to Patrol on a timer. Every transition is logged through the machine's
    ///     <see cref="StateMachine{TState}.OnStateChanged" /> event.
    ///     A second, lower dot showcases the <b>NoCode</b> layer: a runtime-built
    ///     <see cref="StateMachineData" /> of two <see cref="StateData" /> assets (Sleep / Alert) whose
    ///     <c>OnEnter</c> recolor actions run through <see cref="InvokeUnityEventAction" />, driven by a
    ///     scene <see cref="StateMachineBehaviourBase" />. Its predicate-gated transitions fire when the
    ///     "Wake" / "Sleep" buttons flip a <see cref="BoolPredicate" />, and "Force Alert" calls
    ///     <see cref="StateMachineBehaviourBase.ChangeState(string)" /> by name. Robust in an empty scene.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/State Machine Demo")]
    public sealed class StateMachineDemoController : MonoBehaviour
    {
        private static readonly Color TealIdle = new(0.24f, 0.83f, 0.79f);
        private static readonly Color GreenPatrol = new(0.35f, 0.82f, 0.52f);
        private static readonly Color OrangeChase = new(1f, 0.62f, 0.22f);
        private static readonly Color RedAttack = new(1f, 0.32f, 0.38f);
        private static readonly Color PreyColor = new(0.98f, 0.86f, 0.30f);

        private const float HomeY = 2.7f;
        private const float BandHalfWidth = 4.2f;
        private const float IdleToPatrolDelay = 2.5f;
        private const float AttackDuration = 1.1f;
        private const float CatchDistance = 0.6f;

        // The real pure-C# machine and its four state instances (constructed with a back-reference to
        // this controller so the states can drive the shared dot; passed to ChangeState by instance).
        private StateMachine<IState> _machine;
        private IdleState _idle;
        private PatrolState _patrol;
        private ChaseState _chase;
        private AttackState _attack;

        private NeoDemoShell.Context _shell;
        private TMP_Text _stateBig;
        private TMP_Text _prevValue;
        private TMP_Text _timeValue;

        private Transform _dot;
        private SpriteRenderer _dotRenderer;
        private Transform _prey;
        private SpriteRenderer _preyRenderer;
        private float _stateEnterTime;

        private const float NoCodeY = -1.6f;
        private static readonly Color SleepColor = new(0.42f, 0.55f, 0.95f);
        private static readonly Color AlertColor = new(0.98f, 0.35f, 0.55f);

        // NoCode layer: a scene behaviour driving two ScriptableObject StateData assets by name + predicate.
        private StateMachineBehaviourBase _noCodeBehaviour;
        private SpriteRenderer _noCodeDotRenderer;
        private BoolPredicate _wakeGate;
        private BoolPredicate _sleepGate;
        private TMP_Text _noCodeStateValue;

        private void Start()
        {
            _shell = NeoDemoShell.Build("Neo.StateMachine", new Color(0.20f, 0.80f, 0.75f));
            FadeBackdrop();

            NeoDemoShell.ShowInfoCardOnce(
                "Neo.StateMachine (code path)",
                "A dot is a tiny agent. States are C# classes; the machine ticks them and raises events.",
                "Provoke → ChangeState(Chase); the dot hunts the prey and Attacks when close",
                "Calm → ChangeState(Idle); Idle auto-advances to Patrol after a timer",
                "Every OnStateChanged transition is logged");

            BuildAgent();
            BuildMachine();

            _stateBig = _shell.AddBigLabel("IDLE");
            _prevValue = _shell.AddValueLabel("Previous state");
            _timeValue = _shell.AddValueLabel("Time in state");
            _shell.AddButtonRow(
                ("Provoke", Provoke),
                ("Calm", Calm));

            BuildNoCodeAgent();

            _machine.ChangeState(_idle);
            _shell.Log("StateMachine<IState> started in Idle");
        }

        private void BuildNoCodeAgent()
        {
            var dotGo = new GameObject("NoCode Dot");
            dotGo.transform.SetParent(transform, false);
            dotGo.transform.position = new Vector3(0f, NoCodeY, 0f);
            dotGo.transform.localScale = Vector3.one * 0.7f;
            _noCodeDotRenderer = dotGo.AddComponent<SpriteRenderer>();
            _noCodeDotRenderer.sprite = SurvivorArt.Disc;
            _noCodeDotRenderer.color = SleepColor;
            _noCodeDotRenderer.sortingOrder = 2;

            // Two ScriptableObject states; each recolors the NoCode dot from an OnEnter action (no gameplay code).
            StateData sleep = BuildState("Sleep", SleepColor);
            StateData alert = BuildState("Alert", AlertColor);

            // Predicate-gated transitions authored the NoCode way; the buttons flip the BoolPredicate values.
            _wakeGate = new BoolPredicate { PredicateName = "Awake?", Value = false };
            _sleepGate = new BoolPredicate { PredicateName = "Asleep?", Value = true };

            var toAlert = new StateTransition
            {
                TransitionName = "Sleep -> Alert",
                FromStateData = sleep,
                ToStateData = alert
            };
            toAlert.AddPredicate(_wakeGate);

            var toSleep = new StateTransition
            {
                TransitionName = "Alert -> Sleep",
                FromStateData = alert,
                ToStateData = sleep
            };
            toSleep.AddPredicate(_sleepGate);

            var data = ScriptableObject.CreateInstance<StateMachineData>();
            data.name = "DemoNoCodeSM";
            data.States = new[] { sleep, alert };
            data.InitialState = sleep;
            data.Transitions.Add(toAlert);
            data.Transitions.Add(toSleep);

            var behaviourGo = new GameObject("NoCode StateMachine");
            behaviourGo.transform.SetParent(transform, false);
            _noCodeBehaviour = behaviourGo.AddComponent<StateMachineBehaviourBase>();
            SetPrivate(_noCodeBehaviour, "stateMachineData", data);

            // WHY: subscribe on the machine (Awake already created it) so the listener survives the Start-time load.
            _noCodeBehaviour.StateMachine.OnStateChanged.AddListener(HandleNoCodeChanged);

            _noCodeStateValue = _shell.AddValueLabel("NoCode state");
            _shell.AddButtonRow(
                ("NC Wake", NoCodeWake),
                ("NC Sleep", NoCodeSleep),
                ("Force Alert", NoCodeForceAlert));
        }

        private StateData BuildState(string name, Color tint)
        {
            var state = ScriptableObject.CreateInstance<StateData>();
            state.name = name;
            state.StateName = name;
            var recolor = new InvokeUnityEventAction();
            recolor.UnityEvent.AddListener(() =>
            {
                if (_noCodeDotRenderer != null)
                {
                    _noCodeDotRenderer.color = tint;
                }
            });
            state.OnEnterActions.Add(recolor);
            return state;
        }

        private void NoCodeWake()
        {
            // Flip predicate values; the behaviour's per-frame auto-evaluation fires the gated transition.
            _wakeGate.Value = true;
            _sleepGate.Value = false;
        }

        private void NoCodeSleep()
        {
            _wakeGate.Value = false;
            _sleepGate.Value = true;
        }

        private void NoCodeForceAlert()
        {
            // Direct name-based change through the NoCode behaviour, bypassing predicate evaluation.
            // Align the gates first so per-frame auto-evaluation does not revert the jump.
            _wakeGate.Value = true;
            _sleepGate.Value = false;
            _noCodeBehaviour.ChangeState("Alert");
        }

        private void HandleNoCodeChanged(IState previous, IState current)
        {
            string from = previous is StateData p ? p.StateName : "—";
            string to = current is StateData c ? c.StateName : "—";
            _shell.Log($"NoCode: {from} → {to}");
            if (_noCodeStateValue != null)
            {
                _noCodeStateValue.text = to;
            }
        }

        private static void SetPrivate(object target, string field, object value)
        {
            System.Reflection.FieldInfo info = target.GetType().GetField(field,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            info?.SetValue(target, value);
        }

        private void Update()
        {
            // Drive the active state's per-frame logic (movement, timers, self-transitions).
            _machine?.Update();

            if (_timeValue != null && _machine?.CurrentState != null)
            {
                _timeValue.text = (Time.time - _stateEnterTime).ToString("0.0") + " s";
            }
        }

        private void OnDestroy()
        {
            if (_machine != null)
            {
                _machine.OnStateChanged.RemoveListener(HandleStateChanged);
            }

            if (_noCodeBehaviour != null)
            {
                _noCodeBehaviour.StateMachine.OnStateChanged.RemoveListener(HandleNoCodeChanged);
            }
        }

        private void BuildMachine()
        {
            _machine = new StateMachine<IState>();
            _idle = new IdleState(this);
            _patrol = new PatrolState(this);
            _chase = new ChaseState(this);
            _attack = new AttackState(this);

            // Log every real transition straight from the machine's event.
            _machine.OnStateChanged.AddListener(HandleStateChanged);
        }

        private void BuildAgent()
        {
            var dotGo = new GameObject("Agent Dot");
            dotGo.transform.SetParent(transform, false);
            dotGo.transform.position = new Vector3(0f, HomeY, 0f);
            dotGo.transform.localScale = Vector3.one * 0.85f;
            _dotRenderer = dotGo.AddComponent<SpriteRenderer>();
            _dotRenderer.sprite = SurvivorArt.Disc;
            _dotRenderer.color = TealIdle;
            _dotRenderer.sortingOrder = 2;
            _dot = dotGo.transform;

            var preyGo = new GameObject("Prey");
            preyGo.transform.SetParent(transform, false);
            preyGo.transform.position = new Vector3(3f, HomeY, 0f);
            preyGo.transform.localScale = Vector3.one * 0.65f;
            _preyRenderer = preyGo.AddComponent<SpriteRenderer>();
            _preyRenderer.sprite = SurvivorArt.Ring;
            _preyRenderer.color = PreyColor;
            _preyRenderer.sortingOrder = 1;
            _preyRenderer.enabled = false;
            _prey = preyGo.transform;
        }

        private void Provoke()
        {
            RelocatePrey();
            _preyRenderer.enabled = true;
            _machine.ChangeState(_chase);
        }

        private void Calm()
        {
            _preyRenderer.enabled = false;
            _machine.ChangeState(_idle);
        }

        private void HandleStateChanged(IState previous, IState current)
        {
            _stateEnterTime = Time.time;
            string from = previous is AgentState p ? p.Name : "—";
            string to = current is AgentState c ? c.Name : "—";
            _shell.Log($"ChangeState: {from} → {to}");

            if (_stateBig != null)
            {
                _stateBig.text = to.ToUpperInvariant();
            }

            if (_prevValue != null)
            {
                _prevValue.text = from;
            }
        }

        private void RelocatePrey()
        {
            float x = Random.Range(-BandHalfWidth, BandHalfWidth);
            float y = Random.Range(HomeY - 0.7f, HomeY + 0.7f);
            _prey.position = new Vector3(x, y, 0f);
        }

        // Base class so the machine (StateMachine<IState>) can hold every state, while nested access lets each
        // state read/write the controller's dot without exposing public API.
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
                C._dotRenderer.color = Tint;
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

            protected void MoveDot(Vector3 target, float speed)
            {
                C._dot.position = Vector3.MoveTowards(C._dot.position, target, speed * Time.deltaTime);
            }
        }

        private sealed class IdleState : AgentState
        {
            public IdleState(StateMachineDemoController c) : base(c) { }
            public override string Name => "Idle";
            protected override Color Tint => TealIdle;

            public override void OnUpdate()
            {
                float bob = Mathf.Sin(Time.time * 2f) * 0.12f;
                MoveDot(new Vector3(0f, HomeY + bob, 0f), 3.5f);
                if (Time.time - C._stateEnterTime >= IdleToPatrolDelay)
                {
                    C._machine.ChangeState(C._patrol); // auto-advance on the idle timer
                }
            }
        }

        private sealed class PatrolState : AgentState
        {
            public PatrolState(StateMachineDemoController c) : base(c) { }
            public override string Name => "Patrol";
            protected override Color Tint => GreenPatrol;

            public override void OnUpdate()
            {
                float x = Mathf.PingPong(Time.time * 3.2f, BandHalfWidth * 2f) - BandHalfWidth;
                float bob = Mathf.Sin(Time.time * 3f) * 0.1f;
                C._dot.position = new Vector3(x, HomeY + bob, 0f);
            }
        }

        private sealed class ChaseState : AgentState
        {
            public ChaseState(StateMachineDemoController c) : base(c) { }
            public override string Name => "Chase";
            protected override Color Tint => OrangeChase;

            public override void OnUpdate()
            {
                Vector3 prey = C._prey.position;
                MoveDot(prey, 5.5f);
                if (Vector3.Distance(C._dot.position, prey) <= CatchDistance)
                {
                    C._machine.ChangeState(C._attack);
                }
            }
        }

        private sealed class AttackState : AgentState
        {
            public AttackState(StateMachineDemoController c) : base(c) { }
            public override string Name => "Attack";
            protected override Color Tint => RedAttack;

            public override void OnEnter()
            {
                base.OnEnter();
                C._dot.localScale = Vector3.one * 1.0f;
            }

            public override void OnUpdate()
            {
                // Quick jab pulses toward the prey, then it flees and we resume the chase.
                float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 0.35f;
                C._dot.localScale = Vector3.one * (0.85f * pulse);
                if (Time.time - C._stateEnterTime >= AttackDuration)
                {
                    C.RelocatePrey(); // prey escapes
                    C._machine.ChangeState(C._chase);
                }
            }

            public override void OnExit()
            {
                C._dot.localScale = Vector3.one * 0.85f;
            }
        }

        private void FadeBackdrop()
        {
            // The shell paints an opaque full-screen gradient on its overlay canvas; dim it so the world-space
            // agent sprites (rendered by the camera behind the overlay) stay visible under the control card.
            Transform backdrop = _shell.Canvas != null ? _shell.Canvas.transform.Find("Backdrop") : null;
            if (backdrop != null && backdrop.TryGetComponent(out UnityEngine.UI.Image img))
            {
                Color c = img.color;
                img.color = new Color(c.r, c.g, c.b, 0.18f);
            }
        }
    }
}
