using System;
using System.Collections.Generic;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Follow demo (Scenes/Tools/Move/Follow.unity) around its hand-authored followers. The three
    ///     original Follow capsules and the Target stay; the builder renames the Target from "Circle (1)", gives it a
    ///     MouseMover2D (hold to drag), adds a Rail Follower (position limits) and a Turret (2D aim limit) to
    ///     "[Demo] World", and saves the controls, the live status panel, the info card and every persistent
    ///     listener. Speed, deadzone and Teleport call Follow directly (NoCode); the rest goes through
    ///     <see cref="FollowDemoController" />.
    /// </summary>
    internal static class FollowBuilder
    {
        internal const string ScenePath = "Scenes/Tools/Move/Follow.unity";
        internal const string TargetName = "Target";
        internal const string RailFollowerName = "Rail Follower";
        internal const string TurretName = "Turret";
        internal const string TeleportLabel = "Teleport";
        internal const string ResetLabel = "Reset layout";
        internal const string DropLabel = "Drop target";
        internal const string RestoreLabel = "Restore target";

        private const string SceneName = "Follow.unity";
        private const string LegacyTargetName = "Circle (1)";
        private const float LivePanelWidth = 380f;

        // WHY: world layout for the 1920x1080 frame at the scene's orthographic size 5: the free area between the
        // info card / live panel (left) and the controls card (right) is roughly x -4.9..4.3, y -5..4.
        private const float RailY = -2f;
        private const float RailMinX = -4.2f;
        private const float RailMaxX = 3f;
        private const float FollowerScale = 0.6f;
        private const float TurretLimit = 70f;
        private const float SectorLength = 0.9f;

        private static readonly Color Accent = DemoPalette.Good;
        private static readonly Color RailColor = DemoPalette.Cyan;
        private static readonly Color TurretColor = DemoPalette.Warm;
        private static readonly Vector3 TurretPosition = new(3.5f, -3.75f, 0f);

        private static readonly string[] SmoothOptions =
        {
            "Scene defaults",
            nameof(Follow.SmoothMode.None),
            nameof(Follow.SmoothMode.MoveTowards),
            nameof(Follow.SmoothMode.Lerp),
            nameof(Follow.SmoothMode.SmoothDamp),
            nameof(Follow.SmoothMode.Exponential)
        };

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            SceneRefs refs = Step("find the hand-authored Target and followers", () => FindScene(ctx));
            ModuleDemoSceneInfo info = Step("ensure the info service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            FollowDemoController controller = Step("ensure the demo controller",
                () => ctx.FindOrCreate<FollowDemoController>("Services", "Demo Controller"));
            MouseMover2D mover = Step("give the Target a MouseMover2D", () => ConfigureTarget(ctx, refs.Target));
            Step("author the Rail Follower and the Turret", () => BuildWorld(ctx, refs));

            DemoFrame frame = Step("author the frame", () => WorldDemoBuild.Frame(ctx, "Neo.Tools · Follow", Accent));
            Controls controls = Step("author the controls card", () => BuildControls(ctx, frame.Content, refs, controller));
            List<TMP_Text> status = Step("author the live status panel", () => BuildLivePanel(frame, refs));
            Step("author the info card", () => frame.InfoCard(info, InfoContent()));
            Step("wire follower events to the log", () => WireFollowerEvents(ctx, refs, controls.Log));
            Step("gate the Target mover against UI presses", () => frame.GateWorldInput(mover));
            Step("assign the controller references", () => AssignController(ctx, controller, refs, mover, controls, status));
        }

        private static SceneRefs FindScene(DemoSceneBuildContext ctx)
        {
            Transform target = WorldDemoBuild.FindNamed<Transform>(ctx, TargetName, LegacyTargetName);
            List<Follow> authored = ctx.FindAll<Follow>();
            if (authored.Count == 0)
            {
                throw new InvalidOperationException("the scene has no hand-authored Follow component. Restore " +
                                                    SceneName + " from version control instead of rebuilding it.");
            }

            if (target == null || target.GetComponent<Follow>() != null)
            {
                Transform authoredTarget = authored[0].GetTarget();
                target = authoredTarget != null && !DemoSceneBuildContext.IsGenerated(authoredTarget.gameObject)
                    ? authoredTarget
                    : throw new InvalidOperationException("no '" + TargetName + "' / '" + LegacyTargetName +
                                                          "' object and the followers have no target either.");
            }

            if (target.name != TargetName)
            {
                ctx.Note("Renamed '" + target.name + "' to '" + TargetName + "' (the object every follower chases).");
                target.gameObject.name = TargetName;
                ctx.MarkModified(target.gameObject);
            }

            // WHY: left-to-right order matches the live panel rows to what the user sees in the Game view.
            authored.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            foreach (Follow follower in authored)
            {
                if (follower.GetTarget() == null)
                {
                    ctx.Set(follower, "target", target);
                    ctx.Note("'" + follower.name + "' had no target; assigned '" + TargetName + "'.");
                }
            }

            SpriteRenderer look = authored[0].GetComponent<SpriteRenderer>();
            Sprite capsule = look != null && look.sprite != null ? look.sprite : DemoSceneAssets.Disc;
            return new SceneRefs(target, authored, capsule);
        }

        private static MouseMover2D ConfigureTarget(DemoSceneBuildContext ctx, Transform target)
        {
            MouseMover2D mover = ctx.Ensure<MouseMover2D>(target.gameObject);
            ctx.Set(mover, "mode", (int)MouseMover2D.MoveMode.MoveToPointHold);
            ctx.Set(mover, "axes", (int)MouseMover2D.AxisMask.XY);
            ctx.Set(mover, "speed", 9f);
            ctx.Set(mover, "mouseButton", 0);
            ctx.Set(mover, "arrivalThreshold", 0.05f);
            ctx.Set(mover, "offset", Vector2.zero);
            return mover;
        }

        private static void BuildWorld(DemoSceneBuildContext ctx, SceneRefs refs)
        {
            Transform world = ctx.Root("World").transform;
            float railCenter = (RailMinX + RailMaxX) * 0.5f;
            WorldDemoBuild.Bar(ctx, world, "Rail Track", DemoPalette.WithAlpha(RailColor, 0.55f),
                new Vector3(railCenter, RailY, 0.1f), new Vector2(RailMaxX - RailMinX + 0.8f, 0.08f), 0f, -1);
            DemoSceneKit.WorldLabel(ctx, world, "Rail Label", "Rail Follower · limitY", new Vector3(RailMinX + 1.2f,
                RailY - 0.5f, -0.2f), 2.2f, RailColor, new Vector2(3.2f, 0.4f));

            SpriteRenderer rail = DemoSceneKit.WorldSprite(ctx, world, RailFollowerName, refs.Capsule, RailColor,
                new Vector3(RailMinX + 0.4f, RailY, 0f), FollowerScale, 1);
            Follow railFollow = ctx.Ensure<Follow>(rail.gameObject);
            ConfigureFollower(ctx, railFollow, refs.Target, true, Follow.SmoothMode.Exponential, 3f, false);
            ctx.Set(railFollow, "limitX.enabled", true);
            ctx.Set(railFollow, "limitX.min", RailMinX);
            ctx.Set(railFollow, "limitX.max", RailMaxX);
            ctx.Set(railFollow, "limitY.enabled", true);
            ctx.Set(railFollow, "limitY.min", RailY);
            ctx.Set(railFollow, "limitY.max", RailY);

            DemoSceneKit.WorldSprite(ctx, world, "Turret Base", DemoSceneAssets.Ring,
                DemoPalette.WithAlpha(TurretColor, 0.7f), TurretPosition + new Vector3(0f, 0f, 0.1f), 0.9f, 0);
            for (int side = -1; side <= 1; side += 2)
            {
                float angle = side * TurretLimit;
                Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
                WorldDemoBuild.Bar(ctx, world, side < 0 ? "Turret Limit Right" : "Turret Limit Left",
                    DemoPalette.WithAlpha(TurretColor, 0.45f), TurretPosition + direction * (SectorLength * 0.5f) +
                                                               new Vector3(0f, 0f, 0.1f),
                    new Vector2(0.05f, SectorLength), angle, -1);
            }

            SpriteRenderer turret = DemoSceneKit.WorldSprite(ctx, world, TurretName, refs.Capsule, TurretColor,
                TurretPosition, FollowerScale, 1);
            Follow turretFollow = ctx.Ensure<Follow>(turret.gameObject);
            ConfigureFollower(ctx, turretFollow, refs.Target, false, Follow.SmoothMode.MoveTowards, 1f, true);
            ctx.Set(turretFollow, "rotationSmoothMode", (int)Follow.SmoothMode.MoveTowards);
            ctx.Set(turretFollow, "rotationSpeed", 220f);
            ctx.Set(turretFollow, "rotationLimitZ.enabled", true);
            ctx.Set(turretFollow, "rotationLimitZ.min", -TurretLimit);
            ctx.Set(turretFollow, "rotationLimitZ.max", TurretLimit);
            DemoSceneKit.WorldLabel(ctx, world, "Turret Label", TurretName + " · aim ±" + TurretLimit.ToString("0") + "°",
                TurretPosition + new Vector3(0f, -0.9f, -0.2f), 2.2f, TurretColor, new Vector2(2.6f, 0.4f));

            refs.Generated.Clear();
            refs.Generated.Add(railFollow);
            refs.Generated.Add(turretFollow);
        }

        private static void ConfigureFollower(DemoSceneBuildContext ctx, Follow follow, Transform target, bool position,
            Follow.SmoothMode mode, float speed, bool rotation)
        {
            ctx.Set(follow, "target", target);
            ctx.Set(follow, "findTargetByTag", false);
            ctx.Set(follow, "followMode", (int)Follow.FollowMode.TwoD);
            ctx.Set(follow, "followPosition", position);
            ctx.Set(follow, "positionSmoothMode", (int)mode);
            ctx.Set(follow, "positionSpeed", speed);
            ctx.Set(follow, "offset", Vector3.zero);
            ctx.Set(follow, "deadzone.enabled", false);
            ctx.Set(follow, "deadzone.radius", 1f);
            ctx.Set(follow, "distanceControl.activationDistance", 0f);
            ctx.Set(follow, "distanceControl.stoppingDistance", 0f);
            ctx.Set(follow, "limitX.enabled", false);
            ctx.Set(follow, "limitY.enabled", false);
            ctx.Set(follow, "limitZ.enabled", false);
            ctx.Set(follow, "followRotation", rotation);
            ctx.Set(follow, "rotationOffset2D", 0f);
            ctx.Set(follow, "rotationLimitZ.enabled", false);
            ctx.Set(follow, "showDebugGizmos", true);
        }

        private static Controls BuildControls(DemoSceneBuildContext ctx, DemoPanel card, SceneRefs refs,
            FollowDemoController controller)
        {
            List<Follow> positional = refs.PositionFollowers();

            card.Section("Target · MouseMover2D");
            WorldDemoBuild.Hint(card, "Hold the left mouse button in the scene: " + TargetName +
                                      " runs to the cursor and every follower reacts.");
            Toggle orbit = WorldDemoBuild.CompactToggle(card, "Idle orbit while the mouse is up", true);
            DemoWiring.AddDynamic(orbit.onValueChanged, controller.SetAutoOrbit);

            card.Section("Tune · calls Follow directly");
            TMP_Dropdown smoothing = card.Dropdown("Smoothing", SmoothOptions, FollowDemoController.SceneDefaultsOption);
            DemoWiring.AddDynamic(smoothing.onValueChanged, controller.SetPositionSmoothMode);

            float speed = positional.Count > 0 ? WorldDemoBuild.ReadFloat(positional[0], "positionSpeed", 1f) : 1f;
            DemoSliderRow speedRow = WorldDemoBuild.CompactSlider(card, "Position speed -> SetPositionSpeed", 0.5f, 12f,
                Mathf.Clamp(speed, 0.5f, 12f), false, "0.0");
            float radius = positional.Count > 0 ? WorldDemoBuild.ReadFloat(positional[0], "deadzone.radius", 1f) : 1f;
            bool deadzoneOn = positional.Count > 0 && WorldDemoBuild.ReadBool(positional[0], "deadzone.enabled", false);
            Toggle deadzone = WorldDemoBuild.CompactToggle(card, "Deadzone r " + radius.ToString("0.#") + " -> SetDeadzoneEnabled",
                deadzoneOn);
            foreach (Follow follower in positional)
            {
                DemoWiring.AddDynamic(speedRow.Slider.onValueChanged, follower.SetPositionSpeed);
                DemoWiring.AddDynamic(deadzone.onValueChanged, follower.SetDeadzoneEnabled);
            }

            card.Section("C# API");
            Button[] first = card.ButtonRow(TeleportLabel, ResetLabel);
            Button[] second = card.ButtonRow(DropLabel, RestoreLabel);
            DemoLogFeed log = card.LogRow(5, "followers ready");

            foreach (Follow follower in positional)
            {
                DemoWiring.AddVoid(first[0].onClick, follower.TeleportToTarget);
            }

            DemoWiring.AddString(first[0].onClick, log.Log, "Follow.TeleportToTarget(): position limits still apply");
            DemoWiring.AddVoid(first[1].onClick, controller.ResetLayout);
            DemoWiring.AddVoid(second[0].onClick, controller.DropTarget);
            DemoWiring.AddVoid(second[1].onClick, controller.RestoreTarget);
            return new Controls(orbit, log);
        }

        private static List<TMP_Text> BuildLivePanel(DemoFrame frame, SceneRefs refs)
        {
            DemoPanel live = frame.Panel("Live Followers", DemoPanelPlacement.BottomLeft, LivePanelWidth);
            // WHY: authored followers are listed left to right as they stand in the Game view.
            live.Section("Live · left to right · distance");
            List<TMP_Text> status = new();
            List<Follow> all = refs.All();
            for (int i = 0; i < all.Count; i++)
            {
                string caption = Caption(all[i]);
                SpriteRenderer look = all[i].GetComponent<SpriteRenderer>();
                string dot = WorldDemoBuild.Dot(look != null ? look.color : DemoPalette.Text);
                status.Add(live.CompactValue(caption, "-", 0.64f, displayCaption: dot + caption));
            }

            return status;
        }

        private static string Caption(Follow follower)
        {
            if (follower.name == RailFollowerName && DemoSceneBuildContext.IsGenerated(follower.gameObject))
            {
                return RailFollowerName + " · limitY";
            }

            if (follower.name == TurretName && DemoSceneBuildContext.IsGenerated(follower.gameObject))
            {
                return TurretName + " · aim ±" + TurretLimit.ToString("0") + "°";
            }

            bool position = WorldDemoBuild.ReadBool(follower, "followPosition", true);
            bool rotation = WorldDemoBuild.ReadBool(follower, "followRotation", true);
            int mode = WorldDemoBuild.ReadInt(follower, "positionSmoothMode", (int)Follow.SmoothMode.MoveTowards);
            string summary = position
                ? (Enum.IsDefined(typeof(Follow.SmoothMode), mode) ? ((Follow.SmoothMode)mode).ToString() : "custom") +
                  (rotation ? ", aims" : string.Empty)
                : rotation
                    ? "aims only"
                    : "idle";
            return follower.name + " · " + summary;
        }

        private static DemoInfoContent InfoContent()
        {
            return WorldDemoBuild.Info(
                "Neo.Tools · Follow",
                "Five Follow components chase one Target. The three green capsules are the original scene; " +
                RailFollowerName + " (position limits) and " + TurretName + " (2D aim limit) live in [Demo] World.",
                "follow.SetTarget(t), SetPositionSpeed(v), SetPositionSmoothMode(Follow.SmoothMode.Lerp), " +
                "SetDeadzoneEnabled(true), TeleportToTarget(); events onStartFollowing, onStopFollowing, onTargetLost.",
                "Select a follower: target, smoothing, deadzone, limits and events are Inspector fields. The speed " +
                "slider, deadzone toggle and Teleport button call Follow directly through persistent UnityEvents.",
                "Hold the mouse in the scene to drag " + TargetName + " (MouseMover2D). Drop target logs " +
                "onTargetLost; Teleport keeps " + RailFollowerName + " on its rail.");
        }

        private static void WireFollowerEvents(DemoSceneBuildContext ctx, SceneRefs refs, DemoLogFeed log)
        {
            foreach (Follow follower in refs.All())
            {
                bool authored = !DemoSceneBuildContext.IsGenerated(follower.gameObject);
                if (authored)
                {
                    ctx.OwnListeners(follower, follower.onTargetLost);
                    ctx.OwnListeners(follower, follower.onStartFollowing);
                    ctx.MarkModified(follower);
                }

                DemoWiring.AddString(follower.onTargetLost, log.Log, follower.name + ".onTargetLost");
                if (WorldDemoBuild.ReadBool(follower, "followPosition", true))
                {
                    DemoWiring.AddString(follower.onStartFollowing, log.Log, follower.name + ".onStartFollowing");
                }
            }
        }

        private static void AssignController(DemoSceneBuildContext ctx, FollowDemoController controller, SceneRefs refs,
            MouseMover2D mover, Controls controls, List<TMP_Text> status)
        {
            List<Follow> all = refs.All();
            List<int> modes = new();
            foreach (Follow follower in all)
            {
                modes.Add(WorldDemoBuild.ReadInt(follower, "positionSmoothMode", (int)Follow.SmoothMode.MoveTowards));
            }

            ctx.Set(controller, "_target", refs.Target);
            ctx.Set(controller, "_targetMover", mover);
            ctx.Set(controller, "_followers", all);
            ctx.Set(controller, "_defaultSmoothModes", modes);
            ctx.Set(controller, "_statusLabels", status);
            ctx.Set(controller, "_autoOrbit", true);
            ctx.Set(controller, "_autoOrbitToggle", controls.Orbit);
            ctx.Set(controller, "_log", controls.Log);
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(SceneName, step, action);
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(SceneName, step, action);
        }

        private sealed class SceneRefs
        {
            public SceneRefs(Transform target, List<Follow> authored, Sprite capsule)
            {
                Target = target;
                Authored = authored;
                Capsule = capsule;
            }

            public Transform Target { get; }
            public List<Follow> Authored { get; }
            public List<Follow> Generated { get; } = new();
            public Sprite Capsule { get; }

            public List<Follow> All()
            {
                List<Follow> all = new(Authored);
                all.AddRange(Generated);
                return all;
            }

            public List<Follow> PositionFollowers()
            {
                List<Follow> result = new();
                foreach (Follow follower in All())
                {
                    if (WorldDemoBuild.ReadBool(follower, "followPosition", true))
                    {
                        result.Add(follower);
                    }
                }

                return result;
            }
        }

        private sealed class Controls
        {
            public Controls(Toggle orbit, DemoLogFeed log)
            {
                Orbit = orbit;
                Log = log;
            }

            public Toggle Orbit { get; }
            public DemoLogFeed Log { get; }
        }
    }
}
