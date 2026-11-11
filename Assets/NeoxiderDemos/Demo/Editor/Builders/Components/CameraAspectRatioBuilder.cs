using System;
using System.IO;
using System.Reflection;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the <see cref="CameraAspectRatioScaler" /> demo into CameraAspectRatio.unity. The hand-authored
    ///     'Main Camera' keeps its scaler, now configured for a 1080×1920 design with a stored reference size of 9.6
    ///     (the pre-fix scene had a crept 10.5 camera size and a 2.1 multiplier). The builder adds a world-space
    ///     design frame, a bars camera that clears the letterbox/pillarbox area, three device render textures, the
    ///     control card (scale mode, design preset, reference slider, output), a live readout with the log feed and
    ///     the info card. The hand-authored '9-Sliced' background stays untouched.
    /// </summary>
    internal static class CameraAspectRatioBuilder
    {
        public const string ScenePath = "Scenes/Tools/CameraAspectRatio.unity";

        private const string SceneName = "CameraAspectRatio";
        private const string TextureFolder = "Data/Tools/CameraAspectRatio";
        private const string BackgroundName = "9-Sliced";
        private const float ReferenceSize = 9.6f;
        private const float PanelWidth = 460f;

        private static readonly Color Accent = new(0.36f, 0.62f, 1f);
        private static readonly Color WorldBackground = new(0.12f, 0.16f, 0.23f, 1f);
        private static readonly Color BarsColor = new(0.025f, 0.03f, 0.045f, 1f);
        private static readonly Color FrameInk = new(0.07f, 0.08f, 0.13f, 1f);
        private static readonly Color PlateColor = new(1f, 1f, 1f, 0.82f);
        private static readonly Vector2 DesignResolution = new(1080f, 1920f);

        private static readonly string[] DeviceLabels = { "Phone 9:19.5", "Tablet 3:4", "Ultrawide 21:9" };
        private static readonly string[] DeviceFiles = { "CameraAspectPhone", "CameraAspectTablet", "CameraAspectUltrawide" };
        private static readonly Vector2Int[] DeviceSizes = { new(540, 1170), new(768, 1024), new(1260, 540) };

        private static readonly string[] CornerNames = { "Corner TL", "Corner TR", "Corner BL", "Corner BR" };
        private static readonly Color[] CornerColors = { DemoPalette.Danger, DemoPalette.Accent, DemoPalette.Good, DemoPalette.Cyan };
        private static readonly string[] EdgeNames = { "Edge Top", "Edge Bottom", "Edge Left", "Edge Right" };

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "services";
            try
            {
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                CameraAspectRatioDemoController controller =
                    ctx.FindOrCreate<CameraAspectRatioDemoController>("Services", "Demo Controller");

                step = "frame";
                // WHY: no backdrop, header or bottom log - the camera's world (and its bars) must stay visible in the
                // center; the log lives in the readout panel on the right instead.
                DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("CameraAspectRatioScaler", Accent)
                {
                    Backdrop = DemoBackdrop.None,
                    Header = false,
                    LogFeed = false,
                    Placement = DemoContentPlacement.Left,
                    ContentWidth = 560f,
                    ContentOffsetY = 20f
                });

                step = "camera and scaler";
                Camera camera = frame.Camera != null ? frame.Camera : DemoSceneKit.EnsureCamera(ctx, Accent);
                CameraAspectRatioScaler scaler = ConfigureScaler(ctx, camera);
                CheckBackground(ctx);

                step = "bars camera";
                BuildBarsCamera(ctx, camera);

                step = "device render textures";
                RenderTexture[] textures = DeviceTextures(ctx);

                step = "design frame";
                DesignFrameParts world = BuildDesignFrame(ctx);

                step = "control card";
                ControlParts controls = BuildControls(ctx, frame.Content, scaler, controller);

                step = "readout panel";
                ReadoutParts readout = BuildReadout(ctx, frame);

                step = "device preview";
                PreviewParts preview = BuildPreview(ctx, frame, textures);

                step = "panels toggle";
                BuildPanelsToggle(frame, info, readout.Panel);

                step = "info card";
                frame.InfoCard(info, InfoContent());

                step = "controller references";
                ctx.Set(controller, "_scaler", scaler);
                ctx.Set(controller, "_camera", camera);
                ctx.Set(controller, "_frameFill", world.Fill);
                ctx.Set(controller, "_frameEdges", world.Edges);
                ctx.Set(controller, "_frameCorners", world.Corners);
                ctx.Set(controller, "_frameCenter", world.Center);
                ctx.Set(controller, "_frameTitle", world.Title);
                ctx.Set(controller, "_frameCaption", world.Caption);
                ctx.Set(controller, "_frameTitlePlate", world.TitlePlate);
                ctx.Set(controller, "_frameCaptionPlate", world.CaptionPlate);
                ctx.Set(controller, "_deviceLabels", DeviceLabels);
                ctx.Set(controller, "_deviceTextures", textures);
                ctx.Set(controller, "_previewRoot", preview.Root);
                ctx.Set(controller, "_previewImage", preview.Image);
                ctx.Set(controller, "_previewFitter", preview.Fitter);
                ctx.Set(controller, "_previewCaption", preview.Caption);
                ctx.Set(controller, "_referenceSlider", controls.ReferenceSlider);
                ctx.Set(controller, "_modeHint", controls.ModeHint);
                ctx.Set(controller, "_outputValue", readout.Output);
                ctx.Set(controller, "_aspectValue", readout.Aspect);
                ctx.Set(controller, "_sizeValue", readout.Size);
                ctx.Set(controller, "_viewportValue", readout.Viewport);
                ctx.Set(controller, "_visibleValue", readout.Visible);
                ctx.Set(controller, "_frameValue", readout.Frame);
                ctx.Set(controller, "_log", readout.Log);

                step = "design frame layout";
                // WHY: the controller owns the frame math; running it here saves the frame at the authored reference.
                controller.LayoutDesignFrame();
                MarkFrameModified(ctx, world);
            }
            catch (Exception exception)
            {
                throw DemoSceneKit.StepFailed(SceneName, step, exception);
            }
        }

        private static CameraAspectRatioScaler ConfigureScaler(DemoSceneBuildContext ctx, Camera camera)
        {
            if (camera == null)
            {
                throw new InvalidOperationException("The scene has no camera for the scaler.");
            }

            CameraAspectRatioScaler scaler = ctx.Ensure<CameraAspectRatioScaler>(camera.gameObject);
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = WorldBackground;

            ctx.Set(scaler, "scaleMode", (int)CameraAspectRatioScaler.ScaleMode.FitBoth);
            ctx.Set(scaler, "useTargetResolution", true);
            ctx.Set(scaler, "targetResolution", DesignResolution);
            ctx.Set(scaler, "scaleMultiplier", 1f);
            ctx.Set(scaler, "useViewportRectInFitBoth", true);
            ctx.Set(scaler, "minSize", 1f);
            ctx.Set(scaler, "maxSize", 40f);
            ctx.Set(scaler, "updateInRuntime", true);
            ctx.Set(scaler, "updateInEditor", true);
            ctx.Set(scaler, "resetViewportRectOnDisable", true);

            // WHY: author the camera at the design composition first, then store it as the reference through the
            // C# API; OnValidate previews above may have rescaled the camera for the batch screen size.
            camera.orthographicSize = ReferenceSize;
            camera.fieldOfView = 60f;
            scaler.ReferenceOrthographicSize = ReferenceSize;
            scaler.ReferenceFieldOfView = 60f;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            ctx.MarkModified(camera);
            ctx.MarkModified(scaler);
            return scaler;
        }

        private static void CheckBackground(DemoSceneBuildContext ctx)
        {
            GameObject background = null;
            foreach (GameObject root in ctx.Scene.GetRootGameObjects())
            {
                if (root.name == BackgroundName && !DemoSceneBuildContext.IsGenerated(root))
                {
                    background = root;
                    break;
                }
            }

            SpriteRenderer renderer = background != null ? background.GetComponent<SpriteRenderer>() : null;
            if (renderer != null && renderer.sprite == null)
            {
                ctx.Warn("Hand-authored '" + BackgroundName + "' has no sprite (its art lives in the NeoxiderPages " +
                         "sample); the design frame still shows the composition.");
            }
        }

        private static void BuildBarsCamera(DemoSceneBuildContext ctx, Camera main)
        {
            GameObject root = ctx.Root("Bars Camera");
            root.tag = "Untagged";
            root.transform.SetPositionAndRotation(new Vector3(0f, 0f, -20f), Quaternion.identity);
            Camera bars = ctx.Ensure<Camera>(root);
            bars.clearFlags = CameraClearFlags.SolidColor;
            bars.backgroundColor = BarsColor;
            bars.cullingMask = 0;
            bars.orthographic = true;
            bars.orthographicSize = 1f;
            bars.depth = main.depth - 10f;
            bars.rect = new Rect(0f, 0f, 1f, 1f);
            bars.useOcclusionCulling = false;
            bars.allowHDR = false;
            bars.allowMSAA = false;
            bars.targetTexture = null;
        }

        private static RenderTexture[] DeviceTextures(DemoSceneBuildContext ctx)
        {
            RenderTexture[] textures = new RenderTexture[DeviceFiles.Length];
            try
            {
                for (int i = 0; i < DeviceFiles.Length; i++)
                {
                    textures[i] = DemoSceneAssets.EnsureRenderTexture(TextureFolder + "/" + DeviceFiles[i] + ".renderTexture",
                        DeviceSizes[i].x, DeviceSizes[i].y);
                }
            }
            catch (Exception exception)
            {
                ctx.Warn("Device render textures unavailable (" + exception.Message + "); the Phone / Tablet / " +
                         "Ultrawide outputs only log a hint until the next rebuild.");
            }

            return textures;
        }

        private static DesignFrameParts BuildDesignFrame(DemoSceneBuildContext ctx)
        {
            GameObject world = ctx.Root("World");
            world.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            world.transform.localScale = Vector3.one;

            GameObject frame = ctx.Child(world.transform, "Design Frame");
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localRotation = Quaternion.identity;
            frame.transform.localScale = Vector3.one;

            DesignFrameParts parts = new();
            parts.Fill = SlicedSprite(ctx, frame.transform, "Fill", DemoPalette.WithAlpha(Accent, 0.14f), 1);
            for (int i = 0; i < EdgeNames.Length; i++)
            {
                parts.Edges[i] = SlicedSprite(ctx, frame.transform, EdgeNames[i], Accent, 2);
            }

            for (int i = 0; i < CornerNames.Length; i++)
            {
                SpriteRenderer corner = DemoSceneKit.WorldSprite(ctx, frame.transform, CornerNames[i], DemoSceneAssets.Disc,
                    CornerColors[i], Vector3.zero, 1f, 3);
                DemoSceneKit.WorldSprite(ctx, corner.transform, "Ring", DemoSceneAssets.Ring, Color.white, Vector3.zero,
                    1.15f, 4);
                parts.Corners[i] = corner.transform;
            }

            parts.Center = DemoSceneKit.WorldSprite(ctx, frame.transform, "Center", DemoSceneAssets.Ring, Accent,
                Vector3.zero, 1f, 3).transform;
            parts.Title = WorldText(ctx, frame.transform, "Title", "DESIGN", FontStyles.Bold, out SpriteRenderer titlePlate);
            parts.TitlePlate = titlePlate;
            parts.Caption = WorldText(ctx, frame.transform, "Caption", "Reference size", FontStyles.Normal,
                out SpriteRenderer captionPlate);
            parts.CaptionPlate = captionPlate;
            return parts;
        }

        private static SpriteRenderer SlicedSprite(DemoSceneBuildContext ctx, Transform parent, string name, Color color,
            int sortingOrder)
        {
            SpriteRenderer renderer = DemoSceneKit.WorldSprite(ctx, parent, name, DemoSceneAssets.RoundedRect, color,
                Vector3.zero, 1f, sortingOrder);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = Vector2.one;
            return renderer;
        }

        private static TMP_Text WorldText(DemoSceneBuildContext ctx, Transform parent, string name, string text,
            FontStyles style, out SpriteRenderer plate)
        {
            RectTransform rect = ctx.UiChild(parent, name);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            TextMeshPro label = ctx.Ensure<TextMeshPro>(rect.gameObject);
            DemoSceneKit.StyleText(label, text, 4.8f, FrameInk, TextAlignmentOptions.Center, style);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = false;
            Renderer renderer = label.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 6;
            }

            plate = SlicedSprite(ctx, rect, "Plate", PlateColor, 5);
            return label;
        }

        private static void MarkFrameModified(DemoSceneBuildContext ctx, DesignFrameParts world)
        {
            ctx.MarkModified(world.Fill);
            foreach (SpriteRenderer edge in world.Edges)
            {
                ctx.MarkModified(edge);
            }

            foreach (Transform corner in world.Corners)
            {
                ctx.MarkModified(corner);
            }

            ctx.MarkModified(world.Center);
            ctx.MarkModified(world.Title);
            ctx.MarkModified(world.Title.rectTransform);
            ctx.MarkModified(world.Caption);
            ctx.MarkModified(world.Caption.rectTransform);
            ctx.MarkModified(world.TitlePlate);
            ctx.MarkModified(world.CaptionPlate);
        }

        private static ControlParts BuildControls(DemoSceneBuildContext ctx, DemoPanel card, CameraAspectRatioScaler scaler,
            CameraAspectRatioDemoController controller)
        {
            ControlParts parts = new();
            card.Section("Neoxider · Tools · Components");
            TMP_Text title = card.BigLabel("CameraAspectRatioScaler");
            DemoPanel.FitOneLine(title, 24f, 40f);
            card.Note("On 'Main Camera'. The blue frame is the design - watch which part of it stays visible.", 44f);

            card.Section("1 · Scale mode");
            Button[] modes = card.ButtonRow("Fit Width", "Fit Height", "Fit Both", "Manual");
            for (int i = 0; i < modes.Length; i++)
            {
                DemoWiring.AddInt(modes[i].onClick, controller.SetScaleMode, i);
            }

            parts.ModeHint = card.Note("Fit Both · the whole design frame stays visible; Camera.rect adds bars " +
                                       "(a render texture expands instead).", 44f);
            parts.ModeHint.color = DemoPalette.Text;
            Code(card, "scaler.Mode = ScaleMode.FitBoth; scaler.Refresh();", 24f);

            card.Section("2 · Design resolution");
            Button[] designs = card.ButtonRow("Portrait 9:16", "Landscape 16:9", "Square 1:1");
            DemoWiring.AddInt(designs[0].onClick, controller.SetDesignPreset, CameraAspectRatioDemoController.PortraitPreset);
            DemoWiring.AddInt(designs[1].onClick, controller.SetDesignPreset, CameraAspectRatioDemoController.LandscapePreset);
            DemoWiring.AddInt(designs[2].onClick, controller.SetDesignPreset, CameraAspectRatioDemoController.SquarePreset);

            DemoSliderRow reference = card.SliderRow("Reference size (half height, units)", 2f, 20f, ReferenceSize, false,
                "0.0");
            // WHY: NoCode path - the slider writes the scaler property directly; the controller only resizes the frame.
            DemoWiring.AddDynamic<float>(reference.Slider.onValueChanged, ReferenceSetter(scaler));
            DemoWiring.AddDynamic<float>(reference.Slider.onValueChanged, controller.OnReferenceSizeChanged);
            parts.ReferenceSlider = reference.Slider;
            Code(card, "scaler.TargetResolution = new Vector2(1080, 1920);\nscaler.ReferenceOrthographicSize = 9.6f;", 44f);

            card.Section("3 · Output");
            string[] outputLabels = new string[DeviceLabels.Length + 1];
            outputLabels[0] = "Screen";
            Array.Copy(DeviceLabels, 0, outputLabels, 1, DeviceLabels.Length);
            Button[] outputs = card.ButtonRow(outputLabels);
            for (int i = 0; i < outputs.Length; i++)
            {
                DemoWiring.AddInt(outputs[i].onClick, controller.SetOutput, i);
            }

            card.Note("Screen follows the Game view: try Free Aspect, 16:9, 9:16. Devices render the same camera " +
                      "into Camera.targetTexture.", 44f);
            Button reset = card.Button("Reset demo");
            DemoWiring.AddVoid(reset.onClick, controller.ResetDemo);
            return parts;
        }

        private static UnityAction<float> ReferenceSetter(CameraAspectRatioScaler scaler)
        {
            PropertyInfo property = typeof(CameraAspectRatioScaler).GetProperty(
                nameof(CameraAspectRatioScaler.ReferenceOrthographicSize), BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setter = property != null ? property.GetSetMethod() : null;
            if (setter == null)
            {
                throw new InvalidOperationException("CameraAspectRatioScaler.ReferenceOrthographicSize has no public setter.");
            }

            return (UnityAction<float>)Delegate.CreateDelegate(typeof(UnityAction<float>), scaler, setter);
        }

        private static void Code(DemoPanel card, string code, float height)
        {
            TMP_Text note = card.Note("C#  " + code, height);
            note.text = "<color=#" + ColorUtility.ToHtmlStringRGB(Accent) + ">C#</color>  " + code;
            note.fontSize = 15f;
        }

        private static ReadoutParts BuildReadout(DemoSceneBuildContext ctx, DemoFrame frame)
        {
            DemoPanel panel = frame.Panel("Readout", DemoPanelPlacement.BottomRight, PanelWidth);
            ReadoutParts parts = new() { Panel = panel };
            panel.Section("Live readout · Main Camera");
            parts.Output = CompactValue(panel, "Output");
            parts.Aspect = CompactValue(panel, "Aspect output / design");
            parts.Size = CompactValue(panel, "orthographicSize");
            parts.Viewport = CompactValue(panel, "Camera.rect");
            parts.Visible = CompactValue(panel, "Visible world");
            parts.Frame = CompactValue(panel, "Design frame");

            panel.Section("Log");
            RectTransform row = panel.Row("Log", 116f);
            TMP_Text text = DemoSceneKit.Text(ctx, row, "LogText", "› ready", 15f, DemoPalette.Muted,
                TextAlignmentOptions.TopLeft);
            DemoSceneKit.Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            DemoLogFeed feed = ctx.Ensure<DemoLogFeed>(row.gameObject);
            ctx.Set(feed, "_text", text);
            ctx.Set(feed, "_maxLines", 5);
            ctx.Set(feed, "_lineColor", DemoPalette.Muted);
            parts.Log = feed;
            return parts;
        }

        private static TMP_Text CompactValue(DemoPanel panel, string caption)
        {
            TMP_Text value = panel.ValueRow(caption, "-", true).Value;
            DemoPanel.FitOneLine(value, 11f, 18f);
            return value;
        }

        private static PreviewParts BuildPreview(DemoSceneBuildContext ctx, DemoFrame frame, RenderTexture[] textures)
        {
            RectTransform layer = frame.Layer("Device Preview");
            Image box = DemoSceneKit.Image(ctx, layer, "Device", DemoPalette.Panel, true, false);
            DemoSceneKit.Anchor(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 900f));

            TMP_Text caption = DemoSceneKit.Text(ctx, box.transform, "Caption", DeviceLabels[0], 18f, DemoPalette.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.SetAnchors(caption.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -48f),
                new Vector2(-16f, -12f));
            DemoPanel.FitOneLine(caption, 12f, 18f);

            RectTransform inset = ctx.UiChild(box.transform, "Inset");
            DemoSceneKit.SetAnchors(inset, Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -56f));

            RectTransform screen = ctx.UiChild(inset, "Screen");
            DemoSceneKit.Stretch(screen);
            RawImage image = ctx.Ensure<RawImage>(screen.gameObject);
            image.texture = textures.Length > 0 ? textures[0] : null;
            image.color = Color.white;
            image.raycastTarget = false;
            AspectRatioFitter fitter = ctx.Ensure<AspectRatioFitter>(screen.gameObject);
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = DeviceSizes[0].x / (float)DeviceSizes[0].y;

            // WHY: the preview only appears while the camera renders into a device texture (controller.SetOutput).
            layer.gameObject.SetActive(false);
            return new PreviewParts
            {
                Root = layer.gameObject,
                Image = image,
                Fitter = fitter,
                Caption = caption
            };
        }

        private static void BuildPanelsToggle(DemoFrame frame, ModuleDemoSceneInfo info, DemoPanel readout)
        {
            DemoPanel view = frame.Panel("View", DemoPanelPlacement.BottomLeft, 300f);
            Toggle toggle = view.Toggle("Show panels", true);
            // WHY: pure NoCode - hiding the cards uncovers the whole camera view on narrow Game views.
            DemoWiring.AddDynamic<bool>(toggle.onValueChanged, frame.Content.Rect.gameObject.SetActive);
            DemoWiring.AddDynamic<bool>(toggle.onValueChanged, readout.Rect.gameObject.SetActive);
            DemoWiring.AddDynamic<bool>(toggle.onValueChanged, info.SetOverlayVisible);
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Tools · CameraAspectRatioScaler",
                Purpose = "One component on the camera keeps a design composition readable on every aspect ratio: " +
                          "Fit Width, Fit Height, Fit Both (bars) or Manual.",
                RuntimeApi = "Mode, TargetResolution, ReferenceOrthographicSize / ReferenceFieldOfView, Refresh(), " +
                             "CaptureReferenceFromCamera().",
                SceneWorkflow = "Select 'Main Camera': Reference Size is stored on the scaler, so Edit Mode previews " +
                                "never creep the camera size.",
                Verification = "Play, press Phone / Ultrawide and switch modes: the readout says which part of the " +
                               "design frame is cropped.",
                Hints = new[]
                {
                    "Reference slider -> CameraAspectRatioScaler.ReferenceOrthographicSize (NoCode)",
                    "Mode / Design / Output -> CameraAspectRatioDemoController (C#)"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopRight,
                PanelWidth = PanelWidth,
                StartCollapsed = false
            };
        }

        private sealed class DesignFrameParts
        {
            public SpriteRenderer Fill { get; set; }
            public SpriteRenderer[] Edges { get; } = new SpriteRenderer[4];
            public Transform[] Corners { get; } = new Transform[4];
            public Transform Center { get; set; }
            public TMP_Text Title { get; set; }
            public TMP_Text Caption { get; set; }
            public SpriteRenderer TitlePlate { get; set; }
            public SpriteRenderer CaptionPlate { get; set; }
        }

        private sealed class ControlParts
        {
            public TMP_Text ModeHint { get; set; }
            public Slider ReferenceSlider { get; set; }
        }

        private sealed class ReadoutParts
        {
            public DemoPanel Panel { get; set; }
            public TMP_Text Output { get; set; }
            public TMP_Text Aspect { get; set; }
            public TMP_Text Size { get; set; }
            public TMP_Text Viewport { get; set; }
            public TMP_Text Visible { get; set; }
            public TMP_Text Frame { get; set; }
            public DemoLogFeed Log { get; set; }
        }

        private sealed class PreviewParts
        {
            public GameObject Root { get; set; }
            public RawImage Image { get; set; }
            public AspectRatioFitter Fitter { get; set; }
            public TMP_Text Caption { get; set; }
        }
    }
}
