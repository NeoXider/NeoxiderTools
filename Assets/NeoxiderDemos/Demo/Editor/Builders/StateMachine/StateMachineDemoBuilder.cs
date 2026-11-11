using System.Collections.Generic;
using Neo.Condition;
using Neo.Samples;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ConditionValueType = Neo.Condition.ValueType;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.StateMachine demo into StateMachineDemo.unity. Left card: the C# path (Agent dot driven by a
    ///     StateMachine of IState classes inside <see cref="StateMachineDemoController" />) and the NoCode path (the
    ///     "Guard" object, a <see cref="StateMachineBehaviourBase" /> playing the saved Guard StateMachineData). The
    ///     guard's states, transitions and actions are data assets in Data/StateMachine: Sleep -> Alert fires when a
    ///     ConditionEntryPredicate reads the Noise toggle as on (context slot Override1), Alert -> Sleep when it is off
    ///     and the guard spent 1.5 s in Alert; each state shows its look through SetContextGameObjectActiveAction.
    ///     Buttons call the behaviour directly (ChangeState(StateData), GoToInitialState) through persistent listeners.
    /// </summary>
    internal static class StateMachineDemoBuilder
    {
        internal const string ScenePath = "Scenes/StateMachine/StateMachineDemo.unity";
        internal const string SleepAssetPath = "Data/StateMachine/Guard Sleep.asset";
        internal const string AlertAssetPath = "Data/StateMachine/Guard Alert.asset";
        internal const string MachineAssetPath = "Data/StateMachine/Guard StateMachine.asset";
        internal const string ProvokeLabel = "Provoke";
        internal const string CalmLabel = "Calm";
        internal const string ForceAlertLabel = "Force Alert";
        internal const string InitialLabel = "Back to initial";
        internal const string GuardName = "Guard";
        internal const string NoiseLabel = "Noise · transition reads Toggle.isOn";
        internal const float CalmDownSeconds = 1.5f;

        private const string SceneFile = "StateMachineDemo.unity";
        private const float LaneHalfWidth = 3f;
        private const float AgentScale = 0.85f;

        private static readonly Color Accent = new(0.20f, 0.80f, 0.75f);
        private static readonly Color AgentColor = new(0.24f, 0.83f, 0.79f);
        private static readonly Color PreyColor = new(0.98f, 0.86f, 0.30f);
        private static readonly Color SleepColor = new(0.42f, 0.55f, 0.95f);
        private static readonly Color AlertColor = new(0.98f, 0.35f, 0.55f);
        private static readonly Color GuardAccent = new(0.70f, 0.62f, 1f);

        // WHY: with the orthographic 5.4 camera one world unit is 100 reference pixels; the lane and the guard sit
        // right of the 560 px controls card, left of the top-right info card and above the bottom log feed.
        private static readonly Vector3 LaneCenter = new(1.4f, 0.9f, 0f);
        private static readonly Vector3 GuardPosition = new(1.4f, -2.1f, 0f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            ModuleDemoSceneInfo info = Step("ensure the info service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            StateMachineDemoController controller = Step("ensure the demo controller",
                () => ctx.FindOrCreate<StateMachineDemoController>("Services", "Demo Controller"));
            Step("upgrade the EventSystem input module", () => DemoSceneKit.UpgradeLegacyInputModules(ctx));
            Step("set up the 2D camera", () => ConfigureCamera(ctx));
            World world = Step("author the Agent and the Guard", () => BuildWorld(ctx));

            DemoFrame frame = Step("author the frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.StateMachine", Accent)
                {
                    Backdrop = DemoBackdrop.None,
                    Placement = DemoContentPlacement.Left,
                    ContentWidth = 560f,
                    ContentOffsetY = 0f,
                    LogLines = 5
                }));
            CodeUi code = Step("author the C# section", () => BuildCodeSection(frame.Content, controller));
            NoCodeUi noCode = Step("author the NoCode section", () => BuildNoCodeSection(frame.Content, controller));
            GuardAssets assets = Step("author the Guard StateMachineData assets", BuildAssets);
            StateMachineBehaviourBase guard = Step("configure the Guard StateMachineBehaviourBase",
                () => ConfigureGuard(ctx, world, noCode, assets, controller));
            Step("wire the Guard buttons", () => WireGuardButtons(noCode, guard, assets, frame.Log));
            Step("author the info card", () => frame.InfoCard(info, InfoContent()));
            Step("assign the controller references", () =>
            {
                ctx.Set(controller, "_agent", world.Agent.transform);
                ctx.Set(controller, "_agentRenderer", world.Agent);
                ctx.Set(controller, "_prey", world.Prey.transform);
                ctx.Set(controller, "_preyRenderer", world.Prey);
                ctx.Set(controller, "_laneHalfWidth", LaneHalfWidth);
                ctx.Set(controller, "_stateLabel", code.State);
                ctx.Set(controller, "_previousLabel", code.Previous);
                ctx.Set(controller, "_timeLabel", code.Time);
                ctx.Set(controller, "_guard", guard);
                ctx.Set(controller, "_guardStateLabel", noCode.GuardState);
                ctx.Set(controller, "_log", frame.Log);
            });
        }

        private static void ConfigureCamera(DemoSceneBuildContext ctx)
        {
            Camera camera = ctx.Find<Camera>();
            if (camera == null)
            {
                return;
            }

            // WHY: the hand-authored camera was perspective; the 2D lane layout assumes the kit's 5.4 orthographic view.
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DemoPalette.CameraBackground(Accent);
            camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            ctx.MarkModified(camera);
            ctx.MarkModified(camera.transform);
        }

        private static World BuildWorld(DemoSceneBuildContext ctx)
        {
            Transform root = ctx.Root("World").transform;

            DemoSceneKit.WorldLabel(ctx, root, "Agent Label", "C# · StateMachine<IState> · Agent",
                LaneCenter + new Vector3(0f, 1.45f, 0f), 3.2f, Accent, new Vector2(8f, 0.6f));
            LogicDemoBuildUtility.WorldBar(ctx, root, "Patrol Lane", DemoPalette.WithAlpha(Accent, 0.35f),
                LaneCenter + new Vector3(0f, -0.62f, 0.1f), new Vector2(LaneHalfWidth * 2f + 1f, 0.07f), -1);
            SpriteRenderer agent = DemoSceneKit.WorldSprite(ctx, root, "Agent", DemoSceneAssets.Disc, AgentColor,
                LaneCenter, AgentScale, 2);
            SpriteRenderer prey = DemoSceneKit.WorldSprite(ctx, root, "Prey", DemoSceneAssets.Ring, PreyColor,
                LaneCenter + new Vector3(2.2f, 0f, 0f), 0.65f, 1);
            // WHY: the prey appears only when Provoke places it; the controller enables the renderer.
            prey.enabled = false;

            DemoSceneKit.WorldLabel(ctx, root, "Guard Label", "NoCode · StateMachineBehaviourBase · Guard",
                GuardPosition + new Vector3(0f, 1.15f, 0f), 3.2f, GuardAccent, new Vector2(8f, 0.6f));
            GameObject guard = ctx.Child(root, GuardName);
            guard.transform.localPosition = GuardPosition;
            guard.transform.localRotation = Quaternion.identity;
            guard.transform.localScale = Vector3.one;

            SpriteRenderer sleep = DemoSceneKit.WorldSprite(ctx, guard.transform, "Sleep Look", DemoSceneAssets.Disc,
                SleepColor, Vector3.zero, 0.8f, 2);
            DemoSceneKit.WorldLabel(ctx, sleep.transform, "Zzz", "z z z", new Vector3(0.9f, 0.7f, -0.1f), 3.6f,
                SleepColor, new Vector2(2f, 0.6f), 3);
            SpriteRenderer alert = DemoSceneKit.WorldSprite(ctx, guard.transform, "Alert Look", DemoSceneAssets.Disc,
                AlertColor, Vector3.zero, 0.95f, 2);
            DemoSceneKit.WorldLabel(ctx, alert.transform, "Mark", "!", new Vector3(0f, 0.95f, -0.1f), 7f,
                AlertColor, new Vector2(1f, 0.8f), 3);
            // WHY: Sleep is the initial state; the Alert look is shown by the Alert state's OnEnter action.
            alert.gameObject.SetActive(false);
            DemoSceneKit.WorldLabel(ctx, root, "Guard Data Label", "Guard StateMachine.asset · Sleep / Alert",
                GuardPosition + new Vector3(0f, -0.95f, 0f), 2.4f, DemoPalette.Muted, new Vector2(8f, 0.5f));

            return new World(agent, prey, guard, sleep.gameObject, alert.gameObject);
        }

        private static CodeUi BuildCodeSection(DemoPanel card, StateMachineDemoController controller)
        {
            card.Section("C# · StateMachine<IState>");
            card.Note("Agent: Idle -> Patrol -> Chase -> Attack (C# classes)");
            TMP_Text state = card.BigLabel("IDLE");
            TMP_Text previous = card.ValueLabel("Previous state", "-");
            TMP_Text time = card.ValueLabel("Time in state", "0.0 s");
            Button[] buttons = card.ButtonRow(ProvokeLabel, CalmLabel);
            DemoWiring.AddVoid(buttons[0].onClick, controller.Provoke);
            DemoWiring.AddVoid(buttons[1].onClick, controller.Calm);
            card.Note("Provoke -> ChangeState(chase) · Calm -> ChangeState(idle)");
            return new CodeUi(state, previous, time);
        }

        private static NoCodeUi BuildNoCodeSection(DemoPanel card, StateMachineDemoController controller)
        {
            card.Spacer(6f);
            card.Section("NoCode · StateMachineBehaviourBase");
            card.Note("'" + GuardName + "' plays Guard StateMachine.asset (Sleep / Alert)");
            TMP_Text guardState = card.ValueLabel("Guard state", "Sleep");
            Toggle noise = card.Toggle(NoiseLabel, false);
            DemoWiring.AddDynamic(noise.onValueChanged, controller.LogNoise);
            Button[] buttons = card.ButtonRow(ForceAlertLabel, InitialLabel);
            card.Note("ChangeState(StateData) · GoToInitialState() · no code");
            return new NoCodeUi(guardState, noise, buttons[0], buttons[1]);
        }

        private static GuardAssets BuildAssets()
        {
            StateData sleep = DemoSceneAssets.EnsureAsset<StateData>(SleepAssetPath,
                state => ConfigureState(state, "Sleep", ConditionContextSlot.Override2));
            StateData alert = DemoSceneAssets.EnsureAsset<StateData>(AlertAssetPath,
                state => ConfigureState(state, "Alert", ConditionContextSlot.Override3));
            StateMachineData machine = DemoSceneAssets.EnsureAsset<StateMachineData>(MachineAssetPath,
                data => ConfigureMachine(data, sleep, alert));
            return new GuardAssets(sleep, alert, machine);
        }

        private static void ConfigureState(StateData state, string stateName, ConditionContextSlot look)
        {
            state.StateName = stateName;
            SetLookAction(state.OnEnterActions, look, true);
            SetLookAction(state.OnExitActions, look, false);
            if (state.OnUpdateActions.Count > 0)
            {
                state.OnUpdateActions.Clear();
            }
        }

        private static void SetLookAction(List<StateAction> actions, ConditionContextSlot slot, bool active)
        {
            // WHY: reuse the existing managed-reference instance so a rebuild does not churn the asset file.
            if (actions.Count == 1 && actions[0] is SetContextGameObjectActiveAction existing)
            {
                existing.ContextSlot = slot;
                existing.SetActive = active;
                return;
            }

            actions.Clear();
            actions.Add(new SetContextGameObjectActiveAction { ContextSlot = slot, SetActive = active });
        }

        private static void ConfigureMachine(StateMachineData data, StateData sleep, StateData alert)
        {
            data.States = new[] { sleep, alert };
            data.InitialState = sleep;
            List<StateTransition> transitions = data.Transitions;
            StateTransition toAlert = EnsureTransition(transitions, 0, "Sleep -> Alert (noise)", sleep, alert);
            NoisePredicate(toAlert.Predicates, 0, true);
            TrimPredicates(toAlert.Predicates, 1);

            StateTransition toSleep = EnsureTransition(transitions, 1, "Alert -> Sleep (quiet, 1.5 s in Alert)", alert,
                sleep);
            NoisePredicate(toSleep.Predicates, 0, false);
            StateDurationPredicate duration = EnsurePredicate<StateDurationPredicate>(toSleep.Predicates, 1);
            duration.PredicateName = "At least 1.5 s in Alert";
            duration.IsInverted = false;
            duration.RequiredDuration = CalmDownSeconds;
            duration.Comparison = ComparisonType.GreaterThanOrEqual;
            TrimPredicates(toSleep.Predicates, 2);

            while (transitions.Count > 2)
            {
                transitions.RemoveAt(transitions.Count - 1);
            }
        }

        private static StateTransition EnsureTransition(List<StateTransition> transitions, int index, string transitionName,
            StateData from, StateData to)
        {
            while (transitions.Count <= index)
            {
                transitions.Add(new StateTransition());
            }

            StateTransition transition = transitions[index] ?? new StateTransition();
            transitions[index] = transition;
            transition.TransitionName = transitionName;
            transition.FromStateData = from;
            transition.ToStateData = to;
            transition.Priority = 0;
            transition.IsEnabled = true;
            return transition;
        }

        private static void NoisePredicate(List<StatePredicate> predicates, int index, bool noiseOn)
        {
            ConditionEntryPredicate predicate = EnsurePredicate<ConditionEntryPredicate>(predicates, index);
            predicate.PredicateName = noiseOn ? "Noise toggle is on" : "Noise toggle is off";
            predicate.IsInverted = false;
            predicate.ContextSlot = ConditionContextSlot.Override1;
            ConditionEntry entry = predicate.ConditionEntry ?? new ConditionEntry();
            entry.Source = SourceMode.Component;
            entry.UseSceneSearch = false;
            entry.SearchObjectName = string.Empty;
            // WHY: no Source Object - the entry reads the context object of slot Override1 (the Noise toggle).
            entry.SourceObject = null;
            entry.ComponentOccurrence = 0;
            entry.ComponentTypeName = typeof(Toggle).FullName;
            entry.PropertyName = nameof(Toggle.isOn);
            entry.IsMethodWithArgument = false;
            entry.CurrentValueType = ConditionValueType.Bool;
            entry.Compare = CompareOp.Equal;
            entry.ThresholdSource = ThresholdSource.Constant;
            entry.ThresholdBool = noiseOn;
            entry.Invert = false;
            predicate.ConditionEntry = entry;
        }

        private static T EnsurePredicate<T>(List<StatePredicate> predicates, int index) where T : StatePredicate, new()
        {
            while (predicates.Count <= index)
            {
                predicates.Add(new T());
            }

            if (predicates[index] is T existing)
            {
                return existing;
            }

            T created = new();
            predicates[index] = created;
            return created;
        }

        private static void TrimPredicates(List<StatePredicate> predicates, int count)
        {
            while (predicates.Count > count)
            {
                predicates.RemoveAt(predicates.Count - 1);
            }
        }

        private static StateMachineBehaviourBase ConfigureGuard(DemoSceneBuildContext ctx, World world, NoCodeUi noCode,
            GuardAssets assets, StateMachineDemoController controller)
        {
            StateMachineBehaviourBase guard = ctx.Ensure<StateMachineBehaviourBase>(world.Guard);
            ctx.Set(guard, "enableDebugLog", false);
            ctx.Set(guard, "exitCurrentStateOnDisable", true);
            ctx.Set(guard, "reloadDataOnEnable", true);
            ctx.Set(guard, "showStateInInspector", true);
            ctx.Set(guard, "autoEvaluateTransitions", true);
            ctx.Set(guard, "stateMachineData", assets.Machine);
            // WHY: StateData assets cannot hold scene objects; slots Override1..3 resolve to these entries.
            ctx.Set(guard, "contextOverrides",
                new List<GameObject> { noCode.Noise.gameObject, world.SleepLook, world.AlertLook });
            DemoWiring.AddDynamic<string, string>(guard.OnStateChanged, controller.OnGuardStateChanged);
            return guard;
        }

        private static void WireGuardButtons(NoCodeUi noCode, StateMachineBehaviourBase guard, GuardAssets assets,
            DemoLogFeed log)
        {
            DemoWiring.AddObject(noCode.ForceAlert.onClick, guard.ChangeState, assets.Alert);
            DemoWiring.AddString(noCode.ForceAlert.onClick, log.Log, "Guard.ChangeState(Guard Alert asset)");
            DemoWiring.AddVoid(noCode.Initial.onClick, guard.GoToInitialState);
            DemoWiring.AddString(noCode.Initial.onClick, log.Log, "Guard.GoToInitialState()");
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.StateMachine",
                Purpose = "Two ways to run states. Agent: a C# StateMachine<IState> with four state classes. " +
                          "Guard: a StateMachineBehaviourBase that plays a StateMachineData asset authored in the Inspector.",
                RuntimeApi = "new StateMachine<IState>(); ChangeState(state); Update(); OnStateChanged. " +
                             "Behaviour: ChangeState(stateData), ChangeState(\"Alert\"), GoToInitialState().",
                SceneWorkflow = "Select '" + GuardName + "': State Machine Data = Guard StateMachine.asset; Context " +
                                "Overrides = Noise toggle, Sleep Look, Alert Look (slots Override1..3).",
                Verification = "Play: the Agent goes Idle -> Patrol; Provoke sends it after the yellow ring. Noise on " +
                               "turns the Guard pink (Alert).",
                Hints = new[]
                {
                    "Provoke / Calm -> StateMachineDemoController -> machine.ChangeState (C#)",
                    "Force Alert -> Guard.ChangeState(StateData) (NoCode)",
                    "Guard.OnStateChanged -> controller log"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopRight,
                PanelWidth = 440f,
                StartCollapsed = false
            };
        }

        private static void Step(string step, System.Action action)
        {
            DemoSceneKit.Step(SceneFile, step, action);
        }

        private static T Step<T>(string step, System.Func<T> action)
        {
            return DemoSceneKit.Step(SceneFile, step, action);
        }

        private sealed class World
        {
            public World(SpriteRenderer agent, SpriteRenderer prey, GameObject guard, GameObject sleepLook,
                GameObject alertLook)
            {
                Agent = agent;
                Prey = prey;
                Guard = guard;
                SleepLook = sleepLook;
                AlertLook = alertLook;
            }

            public SpriteRenderer Agent { get; }
            public SpriteRenderer Prey { get; }
            public GameObject Guard { get; }
            public GameObject SleepLook { get; }
            public GameObject AlertLook { get; }
        }

        private sealed class CodeUi
        {
            public CodeUi(TMP_Text state, TMP_Text previous, TMP_Text time)
            {
                State = state;
                Previous = previous;
                Time = time;
            }

            public TMP_Text State { get; }
            public TMP_Text Previous { get; }
            public TMP_Text Time { get; }
        }

        private sealed class NoCodeUi
        {
            public NoCodeUi(TMP_Text guardState, Toggle noise, Button forceAlert, Button initial)
            {
                GuardState = guardState;
                Noise = noise;
                ForceAlert = forceAlert;
                Initial = initial;
            }

            public TMP_Text GuardState { get; }
            public Toggle Noise { get; }
            public Button ForceAlert { get; }
            public Button Initial { get; }
        }

        private sealed class GuardAssets
        {
            public GuardAssets(StateData sleep, StateData alert, StateMachineData machine)
            {
                Sleep = sleep;
                Alert = alert;
                Machine = machine;
            }

            public StateData Sleep { get; }
            public StateData Alert { get; }
            public StateMachineData Machine { get; }
        }
    }
}
