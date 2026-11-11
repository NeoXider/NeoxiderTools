using System;
using Neo.Animations;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Animations demo into AnimatorsDemo.unity: three world targets under [Demo] World - a disc
    ///     scaled by a <see cref="Vector3Animator" /> (OnVectorChanged › Transform.localScale), a disc tinted by a
    ///     <see cref="ColorAnimator" /> (OnColorChanged › SpriteRenderer.color) and a square moved by a
    ///     <see cref="TransformAnimator" /> - plus a card bar filled by a <see cref="FloatAnimator" />
    ///     (Value.OnChanged › Image.fillAmount). The value bindings are persistent UnityEvents (NoCode); the card's
    ///     playback, shared type, speed and channel controls go through <see cref="AnimatorsDemoController" />.
    /// </summary>
    internal static class AnimatorsDemoBuilder
    {
        public const string ScenePath = "Scenes/Animations/AnimatorsDemo.unity";

        private const string Scene = "AnimatorsDemo.unity";
        private const float TargetY = 0.6f;
        private const float AnimationSpeed = 1.5f;

        private static readonly Color Accent = new(0.55f, 0.45f, 0.95f);
        private static readonly Color Warm = new(1f, 0.55f, 0.30f);
        private static readonly Color Cool = new(0.25f, 0.79f, 0.94f);
        private static readonly Vector3 ScaleStart = new(1.2f, 1.2f, 1f);
        private static readonly Vector3 ScaleEnd = new(2f, 2f, 1f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "services";
            try
            {
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                AnimatorsDemoController controller =
                    ctx.FindOrCreate<AnimatorsDemoController>("Services", "Demo Controller");

                step = "frame";
                // WHY: no backdrop - the animated world targets are the demo; the camera clear colour is the stage.
                DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Neo.Animations", Accent)
                {
                    Backdrop = DemoBackdrop.None,
                    Placement = DemoContentPlacement.Right,
                    ContentWidth = 460f,
                    ContentOffsetY = 0f,
                    LogLines = 4
                });
                DemoPanel card = frame.Content;

                step = "world targets";
                Transform world = ctx.Root("World").transform;
                DemoSceneKit.WorldSprite(ctx, world, "Stage Glow", DemoSceneAssets.Glow,
                    DemoPalette.WithAlpha(Accent, 0.16f), new Vector3(-0.4f, TargetY, 0f), 10f, -10);
                Vector3Animator vector = ScaleTarget(ctx, world);
                ColorAnimator color = ColorTarget(ctx, world, out SpriteRenderer colorDisc);
                TransformAnimator transformAnimator = TransformTarget(ctx, world);

                step = "playback rows";
                card.Section("Playback · all four animators");
                Button[] playback = card.ButtonRow("Play", "Pause", "Resume", "Stop");
                DemoWiring.AddVoid(playback[0].onClick, controller.PlayAll);
                DemoWiring.AddVoid(playback[1].onClick, controller.PauseAll);
                DemoWiring.AddVoid(playback[2].onClick, controller.ResumeAll);
                DemoWiring.AddVoid(playback[3].onClick, controller.StopAll);
                TMP_Text events = card.ValueLabel("FloatAnimator events", "waiting for Play Mode");
                DemoValueReadout eventsReadout = DemoSceneKit.Readout(ctx, events, "0");

                step = "shared settings";
                card.Section("Shared · Float, Color and Vector3");
                TMP_Dropdown type = card.Dropdown("AnimationType", Enum.GetNames(typeof(AnimationType)),
                    (int)AnimationType.Pulsing);
                DemoWiring.AddDynamic(type.onValueChanged, controller.SetAnimationType);
                DemoSliderRow speed = card.SliderRow("AnimationSpeed", 0f, 6f, AnimationSpeed, false, "0.0");
                DemoWiring.AddDynamic(speed.Slider.onValueChanged, controller.SetSpeed);

                step = "bindings";
                card.Section("Bindings · UnityEvents, no code");
                DemoBarRow bar = card.BarRow("FloatAnimator.Value › Image.fillAmount", Accent, 0.5f, true);
                DemoValueReadout valueReadout = card.ValueReadout("Value (0..1)", "0.00", "0.50");
                Image swatch = Swatch(ctx, card, "ColorAnimator › Image.color", colorDisc.color);
                card.Note("Vector3Animator › Transform.localScale of the left disc", 40f);
                FloatAnimator floatAnimator = FloatSource(ctx, bar);
                DemoWiring.AddSetter(floatAnimator.Value.OnChanged, bar.Fill, nameof(Image.fillAmount));
                DemoWiring.AddDynamic(floatAnimator.Value.OnChanged, valueReadout.SetFloat);
                DemoWiring.AddString(floatAnimator.OnAnimationStarted, eventsReadout.SetText, "OnAnimationStarted");
                DemoWiring.AddString(floatAnimator.OnAnimationPaused, eventsReadout.SetText, "OnAnimationPaused");
                DemoWiring.AddString(floatAnimator.OnAnimationStopped, eventsReadout.SetText, "OnAnimationStopped");
                DemoWiring.AddSetter(color.OnColorChanged, swatch, nameof(Image.color));

                step = "transform channels";
                card.Section("TransformAnimator · channels");
                Toggle rotate = card.Toggle("Rotate channel", true);
                Toggle bob = card.Toggle("Float (bob) channel", true);
                Toggle pulse = card.Toggle("Scale pulse channel", true);
                DemoWiring.AddDynamic(rotate.onValueChanged, controller.SetRotateChannel);
                DemoWiring.AddDynamic(bob.onValueChanged, controller.SetBobChannel);
                DemoWiring.AddDynamic(pulse.onValueChanged, controller.SetPulseChannel);
                Button shake = card.Button("Shake() impulse");
                DemoWiring.AddVoid(shake.onClick, transformAnimator.Shake);
                DemoWiring.AddString(shake.onClick, frame.Log.Log, "TransformAnimator.Shake() - one-shot impulse");

                step = "info card";
                frame.InfoCard(info, new DemoInfoContent
                {
                    Title = "Neo.Animations · value and transform animators",
                    Purpose = "One small component per animated value: a number (FloatAnimator), a colour " +
                              "(ColorAnimator), a vector (Vector3Animator) or a whole local pose (TransformAnimator). " +
                              "The value animators only publish values; you bind them to any target.",
                    RuntimeApi = "Play / Pause / Resume / Stop; AnimationType, AnimationSpeed; CurrentValue / " +
                                 "CurrentColor / CurrentVector; ManualUpdate + Tick(dt); TransformAnimator.Settings " +
                                 "channels and Shake(strength).",
                    SceneWorkflow = "Select a disc under [Demo] World: OnVectorChanged / OnColorChanged are wired to " +
                                    "Transform.localScale / SpriteRenderer.color in the Inspector. The card bar row " +
                                    "carries the FloatAnimator (Value.OnChanged › Image.fillAmount).",
                    Verification = "Enter Play Mode: the left disc pulses, the middle one shifts colour, the bar " +
                                   "fills, the square spins and bobs. Pause holds every value; Stop freezes the value " +
                                   "animators and returns the square to its base pose.",
                    Hints = new[]
                    {
                        "Playback, type, speed and channels › AnimatorsDemoController › the animator API",
                        "Type and speed are shared by the three value animators",
                        "Shake() impulse is wired straight to TransformAnimator.Shake (NoCode)"
                    },
                    Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                    StartCollapsed = false,
                    PanelWidth = 420f
                });

                step = "controller references";
                ctx.Set(controller, "_float", floatAnimator);
                ctx.Set(controller, "_color", color);
                ctx.Set(controller, "_vector", vector);
                ctx.Set(controller, "_transform", transformAnimator);
                ctx.Set(controller, "_typeDropdown", type);
                ctx.Set(controller, "_speedSlider", speed.Slider);
                ctx.Set(controller, "_rotateToggle", rotate);
                ctx.Set(controller, "_bobToggle", bob);
                ctx.Set(controller, "_pulseToggle", pulse);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw DemoSceneKit.StepFailed(Scene, step, exception);
            }
        }

        /// <summary>Opens the authored scene (rebuilding it first when the scene file is missing).</summary>
        [MenuItem("Neoxider/Demo/Open Animators Demo", false, 360)]
        private static void OpenFromMenu()
        {
            if (!DemoSampleRoots.TryResolveDemoRoot(out string root, out string error))
            {
                Debug.LogWarning(DemoSceneRunner.LogPrefix + error);
                return;
            }

            string path = root + "/" + ScenePath;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null &&
                !DemoSceneRunner.RebuildAll(new[] { path }).Succeeded)
            {
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
        }

        private static Vector3Animator ScaleTarget(DemoSceneBuildContext ctx, Transform world)
        {
            Transform holder = Holder(ctx, world, "Scale Target", -3.5f);
            SpriteRenderer disc = DemoSceneKit.WorldSprite(ctx, holder, "Disc", DemoSceneAssets.Disc, Accent,
                Vector3.zero, 1f, 1);
            // WHY: the start value is not emitted as a change event, so the saved pose is the start vector.
            disc.transform.localScale = ScaleStart;
            Vector3Animator vector = ctx.Ensure<Vector3Animator>(disc.gameObject);
            vector.animationType = AnimationType.Pulsing;
            vector.startVector = ScaleStart;
            vector.endVector = ScaleEnd;
            vector.animationSpeed = AnimationSpeed;
            vector.playOnStart = true;
            ctx.Set(vector, "_randomizeStartTime", false);
            ctx.Set(vector, "_useUnscaledTime", false);
            ctx.MarkModified(vector);
            DemoWiring.AddSetter(vector.OnVectorChanged, disc.transform, nameof(Transform.localScale));
            Captions(ctx, holder, "Vector3Animator", "OnVectorChanged\n› localScale");
            return vector;
        }

        private static ColorAnimator ColorTarget(DemoSceneBuildContext ctx, Transform world, out SpriteRenderer disc)
        {
            Transform holder = Holder(ctx, world, "Colour Target", -0.4f);
            disc = DemoSceneKit.WorldSprite(ctx, holder, "Disc", DemoSceneAssets.Disc, Accent, Vector3.zero, 1.7f, 1);
            ColorAnimator color = ctx.Ensure<ColorAnimator>(disc.gameObject);
            color.animationType = AnimationType.Pulsing;
            color.startColor = Accent;
            color.endColor = Warm;
            color.animationSpeed = AnimationSpeed;
            color.playOnStart = true;
            ctx.Set(color, "_randomizeStartTime", false);
            ctx.Set(color, "_useUnscaledTime", false);
            ctx.MarkModified(color);
            DemoWiring.AddSetter(color.OnColorChanged, disc, nameof(SpriteRenderer.color));
            Captions(ctx, holder, "ColorAnimator", "OnColorChanged\n› SpriteRenderer.color");
            return color;
        }

        private static TransformAnimator TransformTarget(DemoSceneBuildContext ctx, Transform world)
        {
            Transform holder = Holder(ctx, world, "Transform Target", 2.7f);
            SpriteRenderer square = DemoSceneKit.WorldSprite(ctx, holder, "Square", DemoSceneAssets.WhiteSquare, Cool,
                Vector3.zero, 1.3f, 1);
            TransformAnimator animator = ctx.Ensure<TransformAnimator>(square.gameObject);
            ctx.Set(animator, "_target", null);
            ctx.Set(animator, "_playOnEnable", true);
            ctx.Set(animator, "_randomizeStartTime", false);
            ctx.Set(animator, "_useUnscaledTime", false);
            ctx.Set(animator, "_settings.RotateEnabled", true);
            ctx.Set(animator, "_settings.RotationSpeed", new Vector3(0f, 0f, -60f));
            ctx.Set(animator, "_settings.FloatEnabled", true);
            ctx.Set(animator, "_settings.FloatDirection", Vector3.up);
            ctx.Set(animator, "_settings.FloatHeight", 0.35f);
            ctx.Set(animator, "_settings.FloatDuration", 2f);
            ctx.Set(animator, "_settings.ScaleEnabled", true);
            ctx.Set(animator, "_settings.ScaleAmplitude", 0.12f);
            ctx.Set(animator, "_settings.ScaleDuration", 1.2f);
            ctx.Set(animator, "_settings.ShakeEnabled", false);
            ctx.Set(animator, "_settings.ImpulseDuration", 0.45f);
            ctx.Set(animator, "_settings.ImpulsePositionStrength", 0.3f);
            ctx.Set(animator, "_settings.ImpulseRotationStrength", 20f);
            Captions(ctx, holder, "TransformAnimator", "rotate · bob · pulse\n· Shake()");
            return animator;
        }

        private static FloatAnimator FloatSource(DemoSceneBuildContext ctx, DemoBarRow bar)
        {
            // WHY: the animator sits on the bar row it drives, so selecting the row shows the binding.
            FloatAnimator animator = ctx.Ensure<FloatAnimator>(bar.Row.gameObject);
            animator.animationType = AnimationType.Pulsing;
            animator.minValue = 0f;
            animator.maxValue = 1f;
            animator.animationSpeed = AnimationSpeed;
            animator.playOnStart = true;
            ctx.Set(animator, "_randomizeStartTime", false);
            ctx.Set(animator, "_useUnscaledTime", false);
            ctx.MarkModified(animator);
            return animator;
        }

        private static Transform Holder(DemoSceneBuildContext ctx, Transform world, string name, float x)
        {
            Transform holder = ctx.Child(world, name).transform;
            holder.localPosition = new Vector3(x, TargetY, 0f);
            holder.localRotation = Quaternion.identity;
            holder.localScale = Vector3.one;
            return holder;
        }

        private static void Captions(DemoSceneBuildContext ctx, Transform holder, string title, string binding)
        {
            DemoSceneKit.WorldLabel(ctx, holder, "Caption", title, new Vector3(0f, -1.75f, -0.5f), 3f,
                DemoPalette.Text, new Vector2(3.2f, 0.5f));
            // WHY: the stations stand about 3 units apart; a one-line binding ran into its neighbour's caption.
            DemoSceneKit.WorldLabel(ctx, holder, "Binding", binding, new Vector3(0f, -2.45f, -0.5f), 1.8f,
                DemoPalette.Muted, new Vector2(3f, 0.9f));
        }

        private static Image Swatch(DemoSceneBuildContext ctx, DemoPanel card, string caption, Color initial)
        {
            RectTransform row = card.Row("Swatch · " + caption, 34f);
            TMP_Text label = DemoSceneKit.Text(ctx, row, "Caption", caption, 15f, DemoPalette.Muted);
            DemoSceneKit.Anchor(label.rectTransform, new Vector2(0f, 0f), new Vector2(0.7f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, Vector2.zero);
            Image swatch = DemoSceneKit.Image(ctx, row, "Swatch", initial, true, false);
            DemoSceneKit.Anchor(swatch.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(120f, 26f));
            return swatch;
        }
    }
}
