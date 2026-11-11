using System;
using System.Collections.Generic;
using System.Globalization;
using Neo.Parallax;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Parallax demo into ParallaxDemo.unity: seven landscape bands under [Demo] World, each a
    ///     real <see cref="ParallaxLayer" /> bound to the scene camera with its own multiplier (from -1, locked to
    ///     the camera, to +0.4, racing past in front), auto-scrolling clouds and kit sprites; plus the control card
    ///     and info card. <see cref="ParallaxDemoController" /> only moves the camera.
    /// </summary>
    internal static class ParallaxDemoBuilder
    {
        private const string Scene = "ParallaxDemo.unity";

        private static readonly Color Accent = new(1f, 0.60f, 0.20f);

        private static readonly Band[] Bands =
        {
            new("Sun", BandSprite.Glow, new Color(1f, 0.86f, 0.45f, 1f), new Vector2(4.2f, 4f), new Vector2(2.4f, 2.4f),
                new Vector2(-1f, 0f), Vector2.zero, Vector2.zero, -60, false),
            new("Clouds", BandSprite.Glow, new Color(1f, 1f, 1f, 0.75f), new Vector2(0f, 3.3f), new Vector2(3.4f, 1.3f),
                new Vector2(-0.85f, 0f), new Vector2(-0.35f, 0f), new Vector2(1.2f, 0f), -50, true),
            new("Far mountains", BandSprite.Disc, new Color(0.36f, 0.42f, 0.62f), new Vector2(0f, 0.6f),
                new Vector2(4.6f, 3.6f), new Vector2(-0.75f, 0f), Vector2.zero, Vector2.zero, -40, true),
            new("Mid hills", BandSprite.Disc, new Color(0.27f, 0.50f, 0.60f), new Vector2(0f, -1.4f),
                new Vector2(3.4f, 2.6f), new Vector2(-0.5f, 0f), Vector2.zero, Vector2.zero, -30, true),
            new("Near hills", BandSprite.Disc, new Color(0.30f, 0.60f, 0.45f), new Vector2(0f, -2.8f),
                new Vector2(2.6f, 2f), new Vector2(-0.25f, 0f), Vector2.zero, Vector2.zero, -20, true),
            new("Ground", BandSprite.Disc, new Color(0.47f, 0.39f, 0.28f), new Vector2(0f, -4.3f),
                new Vector2(2f, 1.4f), Vector2.zero, Vector2.zero, Vector2.zero, -10, true),
            new("Front bushes", BandSprite.Disc, new Color(0.16f, 0.34f, 0.22f), new Vector2(0f, -4f),
                new Vector2(1.3f, 1f), new Vector2(0.4f, 0f), Vector2.zero, new Vector2(1.6f, 0f), 0, true)
        };

        [DemoSceneBuilder("Scenes/Parallax/ParallaxDemo.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "services";
            try
            {
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                ParallaxDemoController controller = ctx.FindOrCreate<ParallaxDemoController>("Services", "Demo Controller");

                step = "frame";
                // WHY: no backdrop - the scrolling landscape is the background and the camera clear color is the sky.
                DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Neo.Parallax", Accent)
                {
                    Backdrop = DemoBackdrop.None,
                    Placement = DemoContentPlacement.Right,
                    ContentWidth = 440f,
                    LogLines = 4
                });
                Camera camera = frame.Camera;
                if (camera == null)
                {
                    throw new InvalidOperationException("the scene has no camera for the parallax layers.");
                }

                if (!camera.orthographic)
                {
                    ctx.Warn("Camera '" + camera.name + "' is perspective; ParallaxLayer then uses fixed tile counts.");
                }

                step = "layers";
                Transform world = ctx.Root("World").transform;
                List<ParallaxLayer> layers = new();
                foreach (Band band in Bands)
                {
                    layers.Add(BuildBand(ctx, world, band, camera));
                }

                step = "camera card";
                DemoPanel card = frame.Content;
                card.Section("Camera · ParallaxDemoController");
                TMP_Text cameraX = card.ValueLabel("Camera X", "0.0");
                DemoSliderRow speed = card.SliderRow("Pan speed", 0f, 6f, 1.5f, false, "0.0");
                DemoWiring.AddDynamic(speed.Slider.onValueChanged, controller.SetPanSpeed);
                Toggle autoPan = card.Toggle("Auto pan", true);
                DemoWiring.AddDynamic(autoPan.onValueChanged, controller.SetAutoPan);
                Button[] direction = card.ButtonRow("Reverse", "Reset camera");
                DemoWiring.AddVoid(direction[0].onClick, controller.Reverse);
                DemoWiring.AddVoid(direction[1].onClick, controller.ResetCamera);
                Button[] jumps = card.ButtonRow("Jump -25", "Jump +25");
                DemoWiring.AddFloat(jumps[0].onClick, controller.Jump, -25f);
                DemoWiring.AddFloat(jumps[1].onClick, controller.Jump, 25f);

                step = "layer notes";
                card.Section("Layers · parallaxMultiplier.x");
                card.Note(Describe(Bands[0]) + "  locked to the camera");
                card.Note(Describe(Bands[1]) + "  + scrollSpeed " + Number(Bands[1].ScrollSpeed.x) + " drift");
                card.Note(Describe(Bands[2]) + " · " + Describe(Bands[3]) + " · " + Describe(Bands[4]));
                card.Note(Describe(Bands[5]) + "  stays in the world");
                card.Note(Describe(Bands[6]) + "  races past in front");

                step = "info card";
                frame.InfoCard(info, new DemoInfoContent
                {
                    Title = "Neo.Parallax · ParallaxLayer",
                    Purpose = "Seven ParallaxLayer bands tile and recycle around one camera. The multiplier is how " +
                              "much a band resists the camera: -1 follows it (sky), 0 stays in the world, positive " +
                              "values race past in front.",
                    RuntimeApi = "parallaxLayer.SetTargetCamera(camera); TargetCamera. Multiplier, scrollSpeed, " +
                                 "tiling, spacing and sprite variants are Inspector settings.",
                    SceneWorkflow = "Select a band under [Demo] World / Band · */Layer to see its ParallaxLayer " +
                                    "(Target Camera, Parallax Multiplier, Scroll Speed). Edit-mode preview shows the tiles.",
                    Verification = "Enter Play Mode: the camera pans, deeper bands move slower, clouds drift even with " +
                                   "Auto pan off, and jumps of 25 units leave no gaps.",
                    Hints = new[]
                    {
                        "Pan speed / Auto pan / Reverse / Jump -> ParallaxDemoController moves the camera",
                        "Every band reads the camera delta in LateUpdate and recycles its tiles",
                        "Clouds: scrollSpeed adds motion of their own"
                    }
                });

                step = "controller references";
                ctx.Set(controller, "_camera", camera);
                ctx.Set(controller, "_layers", layers);
                ctx.Set(controller, "_panSpeed", 1.5f);
                ctx.Set(controller, "_autoPan", true);
                ctx.Set(controller, "_cameraX", cameraX);
                ctx.Set(controller, "_speedSlider", speed.Slider);
                ctx.Set(controller, "_autoPanToggle", autoPan);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw DemoSceneKit.StepFailed(Scene, step, exception);
            }
        }

        private static ParallaxLayer BuildBand(DemoSceneBuildContext ctx, Transform world, Band band, Camera camera)
        {
            Sprite sprite = band.Sprite == BandSprite.Glow ? DemoSceneAssets.Glow : DemoSceneAssets.Disc;
            if (sprite == null)
            {
                throw new InvalidOperationException("kit sprite for band '" + band.Name + "' is missing.");
            }

            // WHY: the band parent carries position and size; the ParallaxLayer child stays at scale 1 so its tiles
            // are sized and spaced once by the parent's lossy scale.
            GameObject holder = ctx.Child(world, "Band · " + band.Name);
            holder.transform.localPosition = new Vector3(band.Position.x, band.Position.y, 0f);
            holder.transform.localRotation = Quaternion.identity;
            holder.transform.localScale = new Vector3(band.Size.x, band.Size.y, 1f);

            GameObject layerObject = ctx.Child(holder.transform, "Layer");
            layerObject.transform.localPosition = Vector3.zero;
            layerObject.transform.localRotation = Quaternion.identity;
            layerObject.transform.localScale = Vector3.one;
            SpriteRenderer renderer = ctx.Ensure<SpriteRenderer>(layerObject);
            renderer.sprite = sprite;
            renderer.color = band.Color;
            renderer.sortingOrder = band.Order;
            renderer.drawMode = SpriteDrawMode.Simple;
            ctx.MarkModified(renderer);

            ParallaxLayer layer = ctx.Ensure<ParallaxLayer>(layerObject);
            ctx.Set(layer, "targetCamera", camera);
            ctx.Set(layer, "useMainCameraFallback", false);
            ctx.Set(layer, "parallaxMultiplier", band.Multiplier);
            ctx.Set(layer, "scrollSpeed", band.ScrollSpeed);
            ctx.Set(layer, "tileSpacing", band.Spacing);
            ctx.Set(layer, "tileHorizontally", band.Tile);
            ctx.Set(layer, "tileVertically", false);
            ctx.Set(layer, "paddingTiles", new Vector2Int(1, 1));
            ctx.Set(layer, "templateRenderer", renderer);
            ctx.Set(layer, "generateInEditor", true);
            ctx.Set(layer, "fitToMaxSpriteSize", true);
            return layer;
        }

        private static string Describe(Band band)
        {
            return band.Name + " " + Number(band.Multiplier.x);
        }

        private static string Number(float value)
        {
            return value.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
        }

        private enum BandSprite
        {
            Disc,
            Glow
        }

        private sealed class Band
        {
            public Band(string name, BandSprite sprite, Color color, Vector2 position, Vector2 size, Vector2 multiplier,
                Vector2 scrollSpeed, Vector2 spacing, int order, bool tile)
            {
                Name = name;
                Sprite = sprite;
                Color = color;
                Position = position;
                Size = size;
                Multiplier = multiplier;
                ScrollSpeed = scrollSpeed;
                Spacing = spacing;
                Order = order;
                Tile = tile;
            }

            public string Name { get; }
            public BandSprite Sprite { get; }
            public Color Color { get; }
            public Vector2 Position { get; }
            public Vector2 Size { get; }
            public Vector2 Multiplier { get; }
            public Vector2 ScrollSpeed { get; }
            public Vector2 Spacing { get; }
            public int Order { get; }
            public bool Tile { get; }
        }
    }
}
