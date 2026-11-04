using System;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Drawer demo (Scenes/Tools/DrawExample.unity) around its hand-authored gameplay: the "Draw"
    ///     Drawer, the ball Spawner and the PoolManager stay as they were. The builder saves the controls card, the
    ///     info card and the persistent wiring: Drawer.PointCount / Distance reactive events feed value labels,
    ///     Undo / Clear call the Drawer and Pause / Resume call the Spawner directly (NoCode), and a
    ///     <see cref="DemoPointerGate" /> keeps clicks on the UI from starting strokes.
    /// </summary>
    internal static class DrawExampleBuilder
    {
        internal const string ScenePath = "Scenes/Tools/DrawExample.unity";
        internal const string DrawerName = "Draw";
        internal const string UndoLabel = "Undo last";
        internal const string ClearLabel = "Clear all";
        internal const string PauseLabel = "Pause balls";
        internal const string ResumeLabel = "Resume balls";

        private const string SceneName = "DrawExample.unity";
        private const float ValueSplit = 0.6f;
        private static readonly Color Accent = new(0.95f, 0.45f, 0.88f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Drawer drawer = Step("find the hand-authored Drawer", () => WorldDemoBuild.RequireNamed<Drawer>(ctx, DrawerName));
            Spawner spawner = Step("find the ball Spawner", () => ctx.Find<Spawner>());
            ModuleDemoSceneInfo info = Step("ensure the info service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            DrawerDemoController controller = Step("ensure the demo controller",
                () => ctx.FindOrCreate<DrawerDemoController>("Services", "Demo Controller"));

            DemoFrame frame = Step("author the frame", () => WorldDemoBuild.Frame(ctx, "Neo.Tools · Drawer", Accent));
            Step("author the controls card and wiring",
                () => BuildControls(ctx, frame.Content, drawer, spawner, controller));
            Step("author the info card", () => frame.InfoCard(info, InfoContent(spawner != null)));
            Step("gate the Drawer against UI presses", () => frame.GateWorldInput(drawer));
            if (spawner == null)
            {
                ctx.Warn(SceneName + ": no hand-authored Spawner; the Pause / Resume balls row was skipped.");
            }
        }

        private static void BuildControls(DemoSceneBuildContext ctx, DemoPanel card, Drawer drawer, Spawner spawner,
            DrawerDemoController controller)
        {
            card.Section("Draw · Drawer");
            WorldDemoBuild.Hint(card, "Hold the left button and drag in the scene. Lines get a live EdgeCollider2D: " +
                                      "catch the falling balls.");

            card.Section("Current stroke · reactive values");
            TMP_Text points = card.CompactValue("Points · PointCount", "0", ValueSplit);
            DemoValueReadout pointsReadout = ctx.Ensure<DemoValueReadout>(points.gameObject);
            pointsReadout.Configure(points, "0");
            TMP_Text length = card.CompactValue("Length · Distance", "0.0 u", ValueSplit);
            DemoValueReadout lengthReadout = ctx.Ensure<DemoValueReadout>(length.gameObject);
            lengthReadout.Configure(length, "0.0", string.Empty, " u");
            TMP_Text lines = card.CompactValue("Lines alive · lines.Count", "0", ValueSplit);

            card.Section("Tune · next stroke");
            DemoSliderRow smoothing = WorldDemoBuild.CompactSlider(card, "Smoothing passes (Chaikin)", 0f, 5f,
                Mathf.Clamp(drawer.smoothing, 0, 5), true, "0");
            DemoWiring.AddDynamic(smoothing.Slider.onValueChanged, controller.SetSmoothing);
            DemoSliderRow width = WorldDemoBuild.CompactSlider(card, "Line width", 0.1f, 1.2f,
                Mathf.Clamp(drawer.lineWidth, 0.1f, 1.2f), false, "0.00");
            DemoWiring.AddDynamic(width.Slider.onValueChanged, controller.SetLineWidth);
            DemoSliderRow lifetime = WorldDemoBuild.CompactSlider(card, "Auto-delete after, s (0 = keep)", 0f, 10f,
                Mathf.Clamp(Mathf.Round(drawer.deleteAfterRelease), 0f, 10f), true, "0");
            DemoWiring.AddDynamic(lifetime.Slider.onValueChanged, controller.SetDeleteAfter);
            Toggle collider = WorldDemoBuild.CompactToggle(card, "Live collider · addCollider", drawer.addCollider);
            DemoWiring.AddDynamic(collider.onValueChanged, controller.SetLiveCollider);

            card.Section("Actions · call Drawer and Spawner directly");
            Button[] edit = card.ButtonRow(UndoLabel, ClearLabel);
            Button[] balls = spawner != null ? card.ButtonRow(PauseLabel, ResumeLabel) : null;
            DemoLogFeed log = card.LogRow(5, "drag in the scene to draw");

            DemoWiring.AddVoid(edit[0].onClick, drawer.DeleteLast);
            DemoWiring.AddString(edit[0].onClick, log.Log, "Drawer.DeleteLast()");
            DemoWiring.AddVoid(edit[1].onClick, drawer.DeleteAll);
            DemoWiring.AddString(edit[1].onClick, log.Log, "Drawer.DeleteAll()");
            if (balls != null)
            {
                DemoWiring.AddVoid(balls[0].onClick, spawner.StopSpawn);
                DemoWiring.AddString(balls[0].onClick, log.Log, "Spawner.StopSpawn()");
                DemoWiring.AddVoid(balls[1].onClick, spawner.StartSpawn);
                DemoWiring.AddString(balls[1].onClick, log.Log, "Spawner.StartSpawn()");
            }

            WireDrawerEvents(ctx, drawer, controller, pointsReadout, lengthReadout, log);

            ctx.Set(controller, "_drawer", drawer);
            ctx.Set(controller, "_smoothingSlider", smoothing.Slider);
            ctx.Set(controller, "_widthSlider", width.Slider);
            ctx.Set(controller, "_lifetimeSlider", lifetime.Slider);
            ctx.Set(controller, "_colliderToggle", collider);
            ctx.Set(controller, "_linesValue", lines);
            ctx.Set(controller, "_log", log);
        }

        private static void WireDrawerEvents(DemoSceneBuildContext ctx, Drawer drawer, DrawerDemoController controller,
            DemoValueReadout points, DemoValueReadout length, DemoLogFeed log)
        {
            ctx.OwnListeners(drawer, drawer.PointCount.OnChanged);
            ctx.OwnListeners(drawer, drawer.Distance.OnChanged);
            ctx.OwnListeners(drawer, drawer.OnLineCreated);
            ctx.OwnListeners(drawer, drawer.OnLineStarted);

            // WHY: NoCode path - the reactive properties drive the labels without any controller code.
            DemoWiring.AddDynamic(drawer.PointCount.OnChanged, points.SetInt);
            DemoWiring.AddDynamic(drawer.Distance.OnChanged, length.SetFloat);
            DemoWiring.AddDynamic(drawer.OnLineCreated, controller.ReportLine);
            DemoWiring.AddString(drawer.OnLineStarted, log.Log, "Drawer.OnLineStarted: new stroke");
            ctx.MarkModified(drawer);
        }

        private static DemoInfoContent InfoContent(bool hasSpawner)
        {
            return WorldDemoBuild.Info(
                "Neo.Tools · Drawer",
                "Draw lines with the mouse. Each stroke is Chaikin-smoothed live, gets an EdgeCollider2D and deletes " +
                "itself after a few seconds" + (hasSpawner ? "; SimpleSpawner drops balls that bounce off it." : "."),
                "drawer.BeginLine(p), AppendPoint(p), EndLine(), DeleteLast(), DeleteAll(); reactive PointCount and " +
                "Distance; OnLineStarted(Vector3), OnLineCreated(LineRenderer).",
                "Select Draw: every setting is an Inspector field. PointCount.OnChanged and Distance.OnChanged feed " +
                "the value labels; Undo and Clear call Drawer directly.",
                "Drag in the scene: Points and Length count while drawing, release logs OnLineCreated. Smoothing, " +
                "width and collider apply to the next stroke.");
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(SceneName, step, action);
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(SceneName, step, action);
        }
    }
}
