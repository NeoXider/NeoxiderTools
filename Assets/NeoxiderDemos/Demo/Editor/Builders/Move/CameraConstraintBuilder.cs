using System;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the CameraConstraint demo (Scenes/Tools/CameraConstraint.unity) around its hand-authored camera:
    ///     Main Camera keeps CameraConstraint (bounds = the "9-Sliced" BoxCollider2D) and MouseMover2D (drag). The
    ///     builder zooms the camera in so the tall level can actually scroll, marks the bounds corners in
    ///     "[Demo] World", and saves the controls card, the info card and the wiring: the edge padding slider calls
    ///     <see cref="CameraConstraint.SetEdgePadding" /> directly (NoCode), the rest goes through
    ///     <see cref="CameraConstraintDemoController" />, and a <see cref="DemoPointerGate" /> keeps UI drags from
    ///     moving the camera.
    /// </summary>
    internal static class CameraConstraintBuilder
    {
        internal const string ScenePath = "Scenes/Tools/CameraConstraint.unity";
        internal const string JumpUpLabel = "Jump up";
        internal const string CenterLabel = "Center";
        internal const string JumpDownLabel = "Jump down";

        /// <summary>
        ///     Starting zoom: narrower than the bounds (25.8 x 55.9 units) at 16:9, so both axes have room and the
        ///     clamp is visible. The scene used to start at 20, where X could not move at all.
        /// </summary>
        internal const float StartZoom = 6f;

        private const string SceneName = "CameraConstraint.unity";
        private const float ValueSplit = 0.36f;
        private const float MinZoom = 3f;
        private const float MaxZoom = 22f;
        private const float TickStep = 8f;
        private static readonly Color Accent = DemoPalette.Warm;

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Camera camera = Step("find Main Camera", () => WorldDemoBuild.RequireNamed<Camera>(ctx, "Main Camera"));
            CameraConstraint constraint = Step("find its CameraConstraint", () => RequireConstraint(camera));
            MouseMover2D mover = camera.GetComponent<MouseMover2D>();
            if (mover == null)
            {
                ctx.Warn(SceneName + ": Main Camera has no MouseMover2D; dragging the camera is unavailable.");
            }

            Rect bounds = Step("read the bounds source", () => ReadBounds(constraint));
            Step("use the kit input module on the hand-authored EventSystem",
                () => DemoSceneKit.UpgradeLegacyInputModule(ctx, ctx.Find<EventSystem>()));
            Step("zoom the camera in", () => SetStartZoom(ctx, camera));
            Step("mark the bounds in [Demo] World", () => BuildWorld(ctx, bounds));

            ModuleDemoSceneInfo info = Step("ensure the info service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            CameraConstraintDemoController controller = Step("ensure the demo controller",
                () => ctx.FindOrCreate<CameraConstraintDemoController>("Services", "Demo Controller"));
            DemoFrame frame = Step("author the frame",
                () => WorldDemoBuild.Frame(ctx, "Neo.Tools · CameraConstraint", Accent));
            Step("author the controls card and wiring",
                () => BuildControls(ctx, frame.Content, camera, constraint, mover, controller));
            Step("author the info card", () => frame.InfoCard(info, InfoContent(constraint)));
            if (mover != null)
            {
                Step("gate the camera mover against UI presses", () => frame.GateWorldInput(mover));
            }
        }

        private static CameraConstraint RequireConstraint(Camera camera)
        {
            CameraConstraint constraint = camera.GetComponent<CameraConstraint>();
            if (constraint == null)
            {
                throw new InvalidOperationException("'" + camera.name + "' has no CameraConstraint. Restore " + SceneName +
                                                    " from version control instead of rebuilding it.");
            }

            return constraint;
        }

        /// <summary>World rectangle of the constraint's bounds source, from serialized data (no physics sync needed).</summary>
        private static Rect ReadBounds(CameraConstraint constraint)
        {
            CameraConstraint.BoundsType type = (CameraConstraint.BoundsType)WorldDemoBuild.ReadInt(constraint, "boundsType",
                (int)CameraConstraint.BoundsType.SpriteRenderer);
            switch (type)
            {
                case CameraConstraint.BoundsType.BoxCollider2D:
                {
                    BoxCollider2D box = WorldDemoBuild.ReadReference<BoxCollider2D>(constraint, "boxCollider2D");
                    if (box != null)
                    {
                        Transform host = box.transform;
                        Vector3 center = host.TransformPoint(box.offset);
                        Vector3 scale = host.lossyScale;
                        Vector2 size = new(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y));
                        return new Rect((Vector2)center - size * 0.5f, size);
                    }

                    break;
                }
                case CameraConstraint.BoundsType.SpriteRenderer:
                {
                    SpriteRenderer sprite = WorldDemoBuild.ReadReference<SpriteRenderer>(constraint, "spriteRenderer");
                    if (sprite != null)
                    {
                        Bounds rendered = sprite.bounds;
                        return new Rect(rendered.min, rendered.size);
                    }

                    break;
                }
                case CameraConstraint.BoundsType.BoxCollider:
                {
                    BoxCollider box = WorldDemoBuild.ReadReference<BoxCollider>(constraint, "boxCollider");
                    if (box != null)
                    {
                        Transform host = box.transform;
                        Vector3 center = host.TransformPoint(box.center);
                        Vector3 scale = host.lossyScale;
                        Vector2 size = new(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y));
                        return new Rect((Vector2)center - size * 0.5f, size);
                    }

                    break;
                }
            }

            throw new InvalidOperationException("CameraConstraint on '" + constraint.name + "' has no " + type +
                                                " bounds source assigned.");
        }

        private static void SetStartZoom(DemoSceneBuildContext ctx, Camera camera)
        {
            if (!camera.orthographic)
            {
                ctx.Note("Main Camera is perspective; the zoom slider changes orthographicSize only.");
                return;
            }

            if (!Mathf.Approximately(camera.orthographicSize, StartZoom))
            {
                ctx.Note("Main Camera orthographicSize " + camera.orthographicSize.ToString("0.#") + " -> " + StartZoom +
                         " so both axes can scroll inside the bounds.");
                camera.orthographicSize = StartZoom;
                ctx.MarkModified(camera);
            }
        }

        private static void BuildWorld(DemoSceneBuildContext ctx, Rect bounds)
        {
            Transform world = ctx.Root("World").transform;
            Color marker = DemoPalette.WithAlpha(Accent, 0.9f);
            Vector2[] corners = { bounds.min, new(bounds.xMax, bounds.yMin), bounds.max, new(bounds.xMin, bounds.yMax) };
            string[] names = { "Corner Bottom Left", "Corner Bottom Right", "Corner Top Right", "Corner Top Left" };
            for (int i = 0; i < corners.Length; i++)
            {
                DemoSceneKit.WorldSprite(ctx, world, names[i], DemoSceneAssets.Ring, marker,
                    new Vector3(corners[i].x, corners[i].y, -0.1f), 1.6f, 4);
            }

            DemoSceneKit.WorldLabel(ctx, world, "Top Edge Label", "TOP EDGE · bounds yMax " + bounds.yMax.ToString("0.#"),
                new Vector3(bounds.center.x, bounds.yMax - 2.2f, -0.2f), 4f, marker, new Vector2(bounds.width, 1f));
            DemoSceneKit.WorldLabel(ctx, world, "Bottom Edge Label",
                "BOTTOM EDGE · bounds yMin " + bounds.yMin.ToString("0.#"),
                new Vector3(bounds.center.x, bounds.yMin + 1.6f, -0.2f), 4f, marker, new Vector2(bounds.width, 1f));

            // WHY: height ticks give the vertical drag a visible reference even where the background art is plain;
            // they sit left of centre, clear of the info card and the controls card at the start zoom.
            float tickX = Mathf.Max(bounds.xMin + 1.1f, bounds.center.x - 5.2f);
            float first = Mathf.Ceil((bounds.yMin + TickStep * 0.5f) / TickStep) * TickStep;
            for (float y = first; y <= bounds.yMax - TickStep * 0.5f; y += TickStep)
            {
                string tag = "y " + y.ToString("0");
                WorldDemoBuild.Bar(ctx, world, "Tick " + tag, DemoPalette.WithAlpha(DemoPalette.Text, 0.55f),
                    new Vector3(tickX, y, -0.1f), new Vector2(1.6f, 0.12f), 0f, 4);
                DemoSceneKit.WorldLabel(ctx, world, "Tick Label " + tag, tag,
                    new Vector3(tickX + 1.9f, y, -0.2f), 3.2f, DemoPalette.Text, new Vector2(2.4f, 0.8f));
            }
        }

        private static void BuildControls(DemoSceneBuildContext ctx, DemoPanel card, Camera camera,
            CameraConstraint constraint, MouseMover2D mover, CameraConstraintDemoController controller)
        {
            card.Section("Drag · MouseMover2D on Main Camera");
            WorldDemoBuild.Hint(card, mover != null
                ? "Hold the left mouse button and drag in the scene to move Main Camera."
                : "Main Camera has no MouseMover2D: use Jump up / Jump down to move it.");

            card.Section("Live · GetConstraintBounds, IsAtEdge");
            TMP_Text position = card.CompactValue("Camera position", "-", ValueSplit);
            TMP_Text range = card.CompactValue("Allowed range", "-", ValueSplit);
            TMP_Text edge = card.CompactValue("IsAtEdge", "-", ValueSplit);

            card.Section("Tune");
            DemoSliderRow zoom = WorldDemoBuild.CompactSlider(card, "Zoom · Camera.orthographicSize", MinZoom, MaxZoom,
                Mathf.Clamp(camera.orthographicSize, MinZoom, MaxZoom), false, "0.0");
            DemoWiring.AddDynamic(zoom.Slider.onValueChanged, controller.SetZoom);
            float padding = WorldDemoBuild.ReadFloat(constraint, "edgePadding", 0f);
            DemoSliderRow paddingRow = WorldDemoBuild.CompactSlider(card, "Edge padding -> SetEdgePadding", 0f, 6f,
                Mathf.Clamp(padding, 0f, 6f), false, "0.0");
            // WHY: NoCode path - the slider calls the module component directly.
            DemoWiring.AddDynamic(paddingRow.Slider.onValueChanged, constraint.SetEdgePadding);
            Toggle clampX = WorldDemoBuild.CompactToggle(card, "Clamp X · SetAxisConstraint",
                WorldDemoBuild.ReadBool(constraint, "constraintX", true));
            DemoWiring.AddDynamic(clampX.onValueChanged, controller.SetClampX);
            Toggle clampY = WorldDemoBuild.CompactToggle(card, "Clamp Y · SetAxisConstraint",
                WorldDemoBuild.ReadBool(constraint, "constraintY", true));
            DemoWiring.AddDynamic(clampY.onValueChanged, controller.SetClampY);

            Button[] jump = card.ButtonRow(JumpUpLabel, CenterLabel, JumpDownLabel);
            DemoWiring.AddInt(jump[0].onClick, controller.JumpVertical, 1);
            DemoWiring.AddInt(jump[1].onClick, controller.JumpVertical, 0);
            DemoWiring.AddInt(jump[2].onClick, controller.JumpVertical, -1);
            DemoLogFeed log = card.LogRow(4, "drag in the scene");

            ctx.Set(controller, "_camera", camera);
            ctx.Set(controller, "_constraint", constraint);
            ctx.Set(controller, "_zoomSlider", zoom.Slider);
            ctx.Set(controller, "_clampXToggle", clampX);
            ctx.Set(controller, "_clampYToggle", clampY);
            ctx.Set(controller, "_positionValue", position);
            ctx.Set(controller, "_rangeValue", range);
            ctx.Set(controller, "_edgeValue", edge);
            ctx.Set(controller, "_log", log);
        }

        private static DemoInfoContent InfoContent(CameraConstraint constraint)
        {
            BoxCollider2D box = WorldDemoBuild.ReadReference<BoxCollider2D>(constraint, "boxCollider2D");
            string source = box != null ? "'" + box.name + "' BoxCollider2D" : "its bounds source";
            return WorldDemoBuild.Info(
                "Neo.Tools · CameraConstraint",
                "Main Camera is dragged by MouseMover2D; CameraConstraint clamps it every LateUpdate so the view never " +
                "leaves " + source + ", for the current zoom and aspect.",
                "constraint.SetBoundsSource(CameraConstraint.BoundsType.BoxCollider2D, box), SetEdgePadding(f), " +
                "SetAxisConstraint(x, y, z), UpdateBounds(), GetConstraintBounds(out min, out max), IsAtEdge(...).",
                "Select Main Camera: CameraConstraint (bounds type, source, padding, axes) and MouseMover2D (DeltaRaw) " +
                "are Inspector fields. The padding slider calls SetEdgePadding directly.",
                "Drag up and down; Jump up / down throw the camera far away and it snaps back to the edge. Zoom out " +
                "past the bounds width and X is pinned to the centre.");
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
