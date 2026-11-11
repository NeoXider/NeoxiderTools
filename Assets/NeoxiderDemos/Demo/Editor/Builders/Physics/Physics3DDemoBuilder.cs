using System;
using System.Globalization;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Tools/Physics demo (Scenes/Tools/Physics/Physics3DDemo.unity) from an empty scene: a lit arena
    ///     with three stations on plain Rigidbodies - a <see cref="MagneticField" /> that pulses between pull and push
    ///     inside a ring of steel balls, an <see cref="ImpulseZone" /> trigger field over a bouncy plate that keeps three
    ///     balls bouncing, and a manual <see cref="ExplosiveForce" /> mine in front of a crate pyramid. The controls card
    ///     calls the components' public methods through persistent listeners and their UnityEvents write to the inline
    ///     log; there is no demo script. Materials are demo assets next to the scene, created once and configured on
    ///     every pass.
    /// </summary>
    internal static class Physics3DDemoBuilder
    {
        internal const string ScenePath = "Scenes/Tools/Physics/Physics3DDemo.unity";
        internal const string FieldToggleLabel = "Field enabled (MagneticField.enabled)";
        internal const string KickLabel = "Kick the balls";
        internal const string DetonateLabel = "Detonate";
        internal const string MegaBlastLabel = "Mega blast";
        internal const float MegaBlastForce = 18f;
        internal const int SteelBallCount = 8;
        internal const int CrateCount = 6;
        internal const int PadBallCount = 3;

        private const string Scene = "Physics3DDemo";
        private const string WorldRootKey = "World";
        private const string MaterialFolder = "Scenes/Tools/Physics/";
        private const float CameraPitch = 46f;
        private const float CameraFieldOfView = 42f;
        private const float WallHeight = 0.5f;
        private const float WallThickness = 0.2f;
        private const float ArenaHalfWidth = 9.75f;
        private const float ArenaFront = -3.3f;
        private const float ArenaBack = 3.9f;
        private const float DividerX = 3.25f;

        private const float MagnetStrength = 6f;
        private const float MagnetRadius = 3.6f;
        private const float MagnetHeight = 1.6f;
        private const float PullSeconds = 2.2f;
        private const float PushSeconds = 0.45f;
        private const float SteelRingRadius = 1.8f;

        // WHY: ImpulseZone pushes only on OnTriggerEnter, so a ball that comes to rest inside its trigger never gets
        // another impulse. The field therefore floats above a fully elastic plate with a gap a resting ball cannot
        // reach: every impulse leaves the ball able to climb back into the field, and the boost on the way up and the
        // brake on the way down keep the bounce bounded. The plate and the field were tuned with a 1D fixed-step model
        // (0.02 s, bounce threshold 2 m/s); Kick can still stall a ball caught in the gap, and the next Kick revives it.
        private const float PadImpulse = 6f;
        private const float PadImpulseMin = 2f;
        private const float PadImpulseMax = 12f;
        private const float PadCooldown = 0.4f;
        private const float PlateTop = 0.23f;
        private const float FieldBottom = 1.2f;
        private const float FieldHeight = 0.3f;
        private const float FieldSize = 2.3f;

        private const float MineForce = 10f;
        private const float MineRadius = 4.2f;

        private static readonly Color Accent = new(0.36f, 0.80f, 1f);
        private static readonly Vector3 CameraPosition = new(3f, 17.5f, -13f);

        private static readonly Vector3 MagnetStation = new(-6.5f, 0f, 0.3f);
        private static readonly Vector3 PadStation = new(0f, 0f, 0.3f);
        private static readonly Vector3 MineStation = new(6.5f, 0f, 0.3f);
        private static readonly Vector3 TileScale = new(6.3f, 0.01f, 7f);
        private static readonly Vector3 MineOffset = new(0f, -0.35f, -1.7f);

        /// <summary>
        ///     Pad balls: local start position above the field (staggered so they bounce out of step) and the Demo
        ///     material they use (Materials/*.mat).
        /// </summary>
        private static readonly PadBall[] PadBalls =
        {
            new("Ball · Red", new Vector3(-0.75f, 2.4f, 0f), "Materials/red.mat"),
            new("Ball · Yellow", new Vector3(0f, 3.0f, 0f), "Materials/yellow.mat"),
            new("Ball · Blue", new Vector3(0.75f, 3.6f, 0f), "Materials/blue.mat")
        };

        /// <summary>Corners of the field frame, local to the pad station.</summary>
        private static readonly Vector2[] FieldPostCorners =
        {
            new(-1.1f, -1.1f), new(1.1f, -1.1f), new(-1.1f, 1.1f), new(1.1f, 1.1f)
        };

        /// <summary>Crate pyramid (3-2-1) in front of the mine, local to the mine station.</summary>
        private static readonly Vector3[] CratePositions =
        {
            new(-0.85f, 0.4f, 0.7f), new(0f, 0.4f, 0.7f), new(0.85f, 0.4f, 0.7f),
            new(-0.425f, 1.2f, 0.7f), new(0.425f, 1.2f, 0.7f),
            new(0f, 2.0f, 0.7f)
        };

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            ModuleDemoSceneInfo info = Step("ensure the info service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            DemoFrame frame = Step("author the frame",
                () => WorldDemoBuild.Frame(ctx, "Neo.Tools · Physics 3D", Accent));
            Step("aim the perspective camera at the arena", () => StyleCamera(ctx, frame.Camera));
            Palette palette = Step("ensure the materials and the bounce physics material in " + MaterialFolder,
                EnsurePalette);

            Transform world = ctx.Root(WorldRootKey).transform;
            Step("light the arena", () => BuildLighting(ctx, world));
            Step("author the arena", () => BuildArena(ctx, world, palette));
            MagneticField magnet = Step("author the magnet station", () => BuildMagnetStation(ctx, world, palette));
            PadParts pad = Step("author the jump pad station", () => BuildPadStation(ctx, world, palette));
            ExplosiveForce mine = Step("author the mine station", () => BuildMineStation(ctx, world, palette));
            Step("author the controls card and wiring", () => BuildControls(frame.Content, magnet, pad, mine));
            Step("author the info card", () => frame.InfoCard(info, InfoContent()));
        }

        private static void StyleCamera(DemoSceneBuildContext ctx, Camera camera)
        {
            if (camera == null)
            {
                throw new InvalidOperationException("the frame found no camera.");
            }

            // WHY: the arena layout is measured for this view: the stations sit between the info card (top-left)
            // and the controls card (right), with the station titles along the bottom edge.
            camera.orthographic = false;
            camera.fieldOfView = CameraFieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 150f;
            camera.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraPitch, 0f, 0f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DemoPalette.CameraBackground(Accent);
            ctx.MarkModified(camera);
            ctx.MarkModified(camera.transform);
        }

        private static Palette EnsurePalette()
        {
            return new Palette(
                LitMaterial("Physics3DGround.mat", new Color(0.13f, 0.15f, 0.21f), 0.15f, 0f, Color.black),
                LitMaterial("Physics3DArena.mat", new Color(0.19f, 0.22f, 0.30f), 0.25f, 0f, Color.black),
                LitMaterial("Physics3DWall.mat", new Color(0.33f, 0.37f, 0.48f), 0.3f, 0f, Color.black),
                LitMaterial("Physics3DPad.mat", new Color(0.30f, 0.95f, 0.55f), 0.6f, 0f, new Color(0.10f, 0.45f, 0.22f)),
                FieldMaterial("Physics3DField.mat", new Color(0.30f, 0.95f, 0.55f, 0.24f), new Color(0.05f, 0.30f, 0.14f)),
                LitMaterial("Physics3DMagnet.mat", new Color(0.50f, 0.56f, 1f), 0.8f, 0.3f, new Color(0.22f, 0.24f, 0.65f)),
                LitMaterial("Physics3DSteel.mat", new Color(0.82f, 0.85f, 0.90f), 0.75f, 0.85f, Color.black),
                LitMaterial("Physics3DCrate.mat", new Color(0.95f, 0.62f, 0.25f), 0.2f, 0f, Color.black),
                DemoSceneAssets.LoadDemoAsset<Material>("Materials/red.mat"),
                DemoSceneAssets.LoadDemoAsset<Material>("Materials/green.mat"),
                DemoSceneAssets.LoadDemoAsset<Material>("Materials/blue.mat"),
                DemoSceneAssets.EnsureAsset<PhysicsMaterial>(MaterialFolder + "Physics3DBounce.physicMaterial",
                    () => new PhysicsMaterial("Physics3DBounce"), ConfigureBounce));
        }

        private static Material LitMaterial(string file, Color color, float smoothness, float metallic, Color emission)
        {
            return DemoSceneAssets.EnsureAsset<Material>(MaterialFolder + file, CreateLitMaterial,
                material => ConfigureLitMaterial(material, color, smoothness, metallic, emission));
        }

        private static Material FieldMaterial(string file, Color color, Color emission)
        {
            return DemoSceneAssets.EnsureAsset<Material>(MaterialFolder + file, CreateLitMaterial, material =>
            {
                ConfigureLitMaterial(material, color, 0.9f, 0f, emission);
                ConfigureTransparent(material);
            });
        }

        /// <summary>Alpha-blended surface for the Standard shader (Fade mode) or a URP-style Lit shader.</summary>
        private static void ConfigureTransparent(Material material)
        {
            SetFloatIfPresent(material, "_Mode", 2f);
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            if (material.HasProperty("_Surface"))
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void ConfigureBounce(PhysicsMaterial bounce)
        {
            // WHY: Maximum outranks every other combine mode, so the plate stays fully elastic against the balls'
            // default material; no friction keeps the bounce straight up.
            bounce.bounciness = 1f;
            bounce.bounceCombine = PhysicsMaterialCombine.Maximum;
            bounce.dynamicFriction = 0f;
            bounce.staticFriction = 0f;
            bounce.frictionCombine = PhysicsMaterialCombine.Minimum;
        }

        private static Material CreateLitMaterial()
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            Material template = pipeline != null ? pipeline.defaultMaterial : null;
            Shader shader = template != null ? template.shader : Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("no lit shader: neither the render pipeline's default material nor " +
                                                    "the built-in Standard shader is available.");
            }

            return new Material(shader);
        }

        private static void ConfigureLitMaterial(Material material, Color color, float smoothness, float metallic,
            Color emission)
        {
            material.color = color;
            SetFloatIfPresent(material, "_Glossiness", smoothness);
            SetFloatIfPresent(material, "_Smoothness", smoothness);
            SetFloatIfPresent(material, "_Metallic", metallic);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emission);
            }

            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void BuildLighting(DemoSceneBuildContext ctx, Transform world)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.46f, 0.50f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.27f, 0.29f, 0.36f);
            RenderSettings.ambientGroundColor = new Color(0.11f, 0.11f, 0.14f);
            RenderSettings.fog = false;

            if (ctx.Find<Light>() != null)
            {
                return;
            }

            GameObject sun = ctx.Child(world, "Sun");
            sun.transform.SetPositionAndRotation(new Vector3(0f, 10f, -6f), Quaternion.Euler(52f, -35f, 0f));
            Light light = ctx.Ensure<Light>(sun);
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
        }

        private static void BuildArena(DemoSceneBuildContext ctx, Transform world, Palette palette)
        {
            // WHY: the ground reaches far past the arena, so crates thrown over the low walls land instead of falling
            // forever, and it fills the whole view of the tilted camera.
            Shape(ctx, world, "Ground", PrimitiveType.Cube, new Vector3(0f, -0.25f, 8f), new Vector3(60f, 0.5f, 40f),
                palette.Ground, true);

            float depth = ArenaBack - ArenaFront;
            float centerZ = (ArenaBack + ArenaFront) * 0.5f;
            float width = ArenaHalfWidth * 2f;
            Shape(ctx, world, "Wall · Back", PrimitiveType.Cube, new Vector3(0f, WallHeight * 0.5f, ArenaBack),
                new Vector3(width, WallHeight, WallThickness), palette.Wall, true);
            Shape(ctx, world, "Wall · Front", PrimitiveType.Cube, new Vector3(0f, WallHeight * 0.5f, ArenaFront),
                new Vector3(width, WallHeight, WallThickness), palette.Wall, true);
            Shape(ctx, world, "Wall · Left", PrimitiveType.Cube, new Vector3(-ArenaHalfWidth, WallHeight * 0.5f, centerZ),
                new Vector3(WallThickness, WallHeight, depth), palette.Wall, true);
            Shape(ctx, world, "Wall · Right", PrimitiveType.Cube, new Vector3(ArenaHalfWidth, WallHeight * 0.5f, centerZ),
                new Vector3(WallThickness, WallHeight, depth), palette.Wall, true);
            Shape(ctx, world, "Divider · Left", PrimitiveType.Cube, new Vector3(-DividerX, WallHeight * 0.5f, centerZ),
                new Vector3(WallThickness, WallHeight, depth), palette.Wall, true);
            Shape(ctx, world, "Divider · Right", PrimitiveType.Cube, new Vector3(DividerX, WallHeight * 0.5f, centerZ),
                new Vector3(WallThickness, WallHeight, depth), palette.Wall, true);
        }

        private static Transform Station(DemoSceneBuildContext ctx, Transform world, string name, Vector3 position,
            Palette palette, Material stripe, string title, string subtitle)
        {
            Transform station = ctx.Child(world, name).transform;
            station.SetLocalPositionAndRotation(position, Quaternion.identity);
            station.localScale = Vector3.one;
            Shape(ctx, station, "Tile", PrimitiveType.Cube, new Vector3(0f, 0.005f, 0f), TileScale, palette.Arena, false);
            Shape(ctx, station, "Stripe", PrimitiveType.Cube, new Vector3(0f, 0.02f, -3.2f), new Vector3(5.6f, 0.02f, 0.16f),
                stripe, false);

            Quaternion facing = Quaternion.Euler(CameraPitch, 0f, 0f);
            TextMeshPro heading = DemoSceneKit.WorldLabel(ctx, station, "Title", title, new Vector3(0f, 1.15f, -4.3f), 4.2f,
                DemoPalette.Text, new Vector2(6.2f, 0.7f));
            heading.transform.localRotation = facing;
            heading.characterSpacing = 4f;
            TextMeshPro caption = DemoSceneKit.WorldLabel(ctx, station, "Subtitle", subtitle, new Vector3(0f, 0.62f, -4.3f),
                2.4f, DemoPalette.Muted, new Vector2(6.2f, 0.45f));
            caption.transform.localRotation = facing;
            return station;
        }

        private static MagneticField BuildMagnetStation(DemoSceneBuildContext ctx, Transform world, Palette palette)
        {
            Transform station = Station(ctx, world, "Station · MagneticField", MagnetStation, palette, palette.Blue,
                "MAGNETIC FIELD", "pulls " + Format(PullSeconds, "0.0") + " s, pushes " + Format(PushSeconds, "0.00") +
                                  " s · Toggle phases");

            // WHY: the magnet keeps its static SphereCollider so attracted balls cling to it; the field skips its own
            // collider and static colliders without a Rigidbody get no force.
            GameObject magnetObject = Shape(ctx, station, "Magnet", PrimitiveType.Sphere, new Vector3(0f, MagnetHeight, 0f),
                Vector3.one, palette.Magnet, true);
            MagneticField magnet = ctx.Ensure<MagneticField>(magnetObject);
            ctx.Set(magnet, "mode", MagneticField.FieldMode.Attract);
            ctx.Set(magnet, "fieldStrength", MagnetStrength);
            ctx.Set(magnet, "radius", MagnetRadius);
            ctx.Set(magnet, "falloffType", MagneticField.FalloffType.Constant);
            ctx.Set(magnet, "affectedLayers", -1);
            ctx.Set(magnet, "_toggle", true);
            ctx.Set(magnet, "attractDuration", PullSeconds);
            ctx.Set(magnet, "repelDuration", PushSeconds);
            ctx.Set(magnet, "startWithAttract", true);
            ctx.Set(magnet, "addRigidbodyIfNeeded", false);
            ctx.Set(magnet, "useFixedUpdate", true);
            ctx.Set(magnet, "updateInterval", 0f);
            magnet.enabled = true;

            for (int i = 0; i < SteelBallCount; i++)
            {
                float angle = i * Mathf.PI * 2f / SteelBallCount;
                Vector3 position = new(Mathf.Cos(angle) * SteelRingRadius, 0.2f, Mathf.Sin(angle) * SteelRingRadius);
                GameObject ball = Shape(ctx, station, "Steel Ball " + (i + 1), PrimitiveType.Sphere, position,
                    Vector3.one * 0.4f, palette.Steel, true);
                Body(ctx, ball, 0.4f, 0.6f, 0.5f, CollisionDetectionMode.ContinuousDynamic);
            }

            return magnet;
        }

        private static PadParts BuildPadStation(DemoSceneBuildContext ctx, Transform world, Palette palette)
        {
            Transform station = Station(ctx, world, "Station · ImpulseZone", PadStation, palette, palette.Green,
                "IMPULSE ZONE", "trigger field · OnTriggerEnter › impulse up");

            GameObject padBase = Shape(ctx, station, "Pad Base", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f),
                new Vector3(2.6f, 0.2f, 2.6f), palette.Wall, true);
            Bouncy(ctx, padBase, palette.Bounce);
            const float plateThickness = PlateTop - 0.2f;
            GameObject plate = Shape(ctx, station, "Bounce Plate", PrimitiveType.Cube,
                new Vector3(0f, PlateTop - plateThickness * 0.5f, 0f), new Vector3(FieldSize, plateThickness, FieldSize),
                palette.Pad, true);
            Bouncy(ctx, plate, palette.Bounce);

            float postHeight = FieldBottom + FieldHeight - PlateTop;
            for (int i = 0; i < FieldPostCorners.Length; i++)
            {
                Vector2 corner = FieldPostCorners[i];
                Shape(ctx, station, "Field Post " + (i + 1), PrimitiveType.Cylinder,
                    new Vector3(corner.x, PlateTop + postHeight * 0.5f, corner.y),
                    new Vector3(0.08f, postHeight * 0.5f, 0.08f), palette.Pad, false);
            }

            GameObject padObject = Shape(ctx, station, "Impulse Field", PrimitiveType.Cube,
                new Vector3(0f, FieldBottom + FieldHeight * 0.5f, 0f), new Vector3(FieldSize, FieldHeight, FieldSize),
                palette.Field, true);
            MeshRenderer fieldRenderer = padObject.GetComponent<MeshRenderer>();
            fieldRenderer.shadowCastingMode = ShadowCastingMode.Off;
            fieldRenderer.receiveShadows = false;
            ctx.MarkModified(fieldRenderer);
            BoxCollider trigger = padObject.GetComponent<BoxCollider>();
            // WHY: ImpulseZone.Awake marks it too; the saved scene already shows the trigger the Inspector will have.
            trigger.isTrigger = true;
            trigger.sharedMaterial = null;
            ctx.MarkModified(trigger);
            ImpulseZone pad = ctx.Ensure<ImpulseZone>(padObject);
            ctx.Set(pad, "impulseForce", PadImpulse);
            ctx.Set(pad, "direction", ImpulseZone.ImpulseDirection.Custom);
            ctx.Set(pad, "customDirection", Vector3.up);
            ctx.Set(pad, "affectedLayers", -1);
            ctx.Set(pad, "requiredTag", string.Empty);
            ctx.Set(pad, "addRigidbodyIfNeeded", false);
            ctx.Set(pad, "oneTimeOnly", false);
            ctx.Set(pad, "cooldown", PadCooldown);

            Rigidbody[] balls = new Rigidbody[PadBalls.Length];
            for (int i = 0; i < PadBalls.Length; i++)
            {
                PadBall spec = PadBalls[i];
                GameObject ball = Shape(ctx, station, spec.Name, PrimitiveType.Sphere, spec.Position, Vector3.one * 0.6f,
                    DemoSceneAssets.LoadDemoAsset<Material>(spec.MaterialPath), true);
                // WHY: no linear damping, so the elastic plate returns a ball to the height it fell from.
                balls[i] = Body(ctx, ball, 1f, 0f, 0.05f, CollisionDetectionMode.ContinuousDynamic);
            }

            return new PadParts(pad, balls);
        }

        private static void Bouncy(DemoSceneBuildContext ctx, GameObject host, PhysicsMaterial bounce)
        {
            Collider surface = host.GetComponent<Collider>();
            if (surface == null)
            {
                throw new InvalidOperationException("'" + host.name + "' has no collider to make bouncy.");
            }

            surface.isTrigger = false;
            surface.sharedMaterial = bounce;
            ctx.MarkModified(surface);
        }

        private static ExplosiveForce BuildMineStation(DemoSceneBuildContext ctx, Transform world, Palette palette)
        {
            Transform station = Station(ctx, world, "Station · ExplosiveForce", MineStation, palette, palette.Red,
                "EXPLOSIVE FORCE", "Manual · radius " + Format(MineRadius, "0.0") + " · quadratic falloff");

            // WHY: the blast origin sits under the plate, so the impulse lifts the crates as well as pushing them back.
            GameObject mineObject = ctx.Child(station, "Mine");
            mineObject.transform.SetLocalPositionAndRotation(MineOffset, Quaternion.identity);
            mineObject.transform.localScale = Vector3.one;
            Shape(ctx, mineObject.transform, "Plate", PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f),
                new Vector3(1f, 0.05f, 1f), palette.Red, false);
            ExplosiveForce mine = ctx.Ensure<ExplosiveForce>(mineObject);
            ctx.Set(mine, "activationMode", ExplosiveForce.ActivationMode.Manual);
            ctx.Set(mine, "delay", 0f);
            ctx.Set(mine, "force", MineForce);
            ctx.Set(mine, "forceRandomness", 0f);
            ctx.Set(mine, "forceMode", ExplosiveForce.ForceMode.AddExplosionForce);
            ctx.Set(mine, "falloffType", ExplosiveForce.FalloffType.Quadratic);
            ctx.Set(mine, "radius", MineRadius);
            ctx.Set(mine, "affectedLayers", -1);
            ctx.Set(mine, "addRigidbodyIfNeeded", false);
            ctx.Set(mine, "destroyAfterExplosion", false);
            ctx.Set(mine, "destroyDelay", 0f);

            for (int i = 0; i < CratePositions.Length; i++)
            {
                GameObject crate = Shape(ctx, station, "Crate " + (i + 1), PrimitiveType.Cube, CratePositions[i],
                    Vector3.one * 0.8f, palette.Crate, true);
                Body(ctx, crate, 1f, 0.05f, 0.05f, CollisionDetectionMode.Discrete);
            }

            return mine;
        }

        private static void BuildControls(DemoPanel card, MagneticField magnet, PadParts pad, ExplosiveForce mine)
        {
            card.Section("1 · MagneticField · pulsing magnet");
            Toggle field = WorldDemoBuild.CompactToggle(card, FieldToggleLabel, true);
            DemoSliderRow strength = WorldDemoBuild.CompactSlider(card, "Strength · SetStrength(float)", 0f, 12f,
                MagnetStrength, false, "0.0");
            TMP_Text phaseLabel = card.CompactValue("Phase · OnModeChanged(bool)", "pull", 0.62f);
            DemoValueReadout phase = DemoSceneKit.Readout(card.Context, phaseLabel, "0", string.Empty, string.Empty,
                "pull", "push");

            card.Section("2 · ImpulseZone · bounce field");
            DemoSliderRow impulse = WorldDemoBuild.CompactSlider(card, "Impulse · SetImpulseForce(float)", PadImpulseMin,
                PadImpulseMax, PadImpulse, false, "0.0");
            Button kick = card.Button(KickLabel);

            card.Section("3 · ExplosiveForce · mine");
            Button[] blast = card.ButtonRow(DetonateLabel, MegaBlastLabel);
            WorldDemoBuild.Hint(card, "Detonate › Explode() (Force " + Format(MineForce, "0") + "), Mega blast › Explode(" +
                                      Format(MegaBlastForce, "0") + "). Restart Play to restack.");
            DemoLogFeed log = card.LogRow(5, "ready");

            DemoWiring.AddSetter(field.onValueChanged, magnet, "enabled");
            DemoWiring.AddDynamic(strength.Slider.onValueChanged, magnet.SetStrength);
            DemoWiring.AddDynamic(magnet.OnModeChanged, phase.SetBool);

            DemoWiring.AddDynamic(impulse.Slider.onValueChanged, pad.Zone.SetImpulseForce);
            foreach (Rigidbody ball in pad.Balls)
            {
                DemoWiring.AddObject(kick.onClick, pad.Zone.ApplyImpulseToObject, ball.gameObject);
            }

            DemoWiring.AddString(kick.onClick, log.Log, "ImpulseZone.ApplyImpulseToObject(ball) × " + pad.Balls.Length);
            DemoWiring.AddString(pad.Zone.OnImpulseApplied, log.Log, "ImpulseZone.OnImpulseApplied · impulse up");

            DemoWiring.AddVoid(blast[0].onClick, mine.Explode);
            DemoWiring.AddFloat(blast[1].onClick, mine.Explode, MegaBlastForce);
            DemoWiring.AddString(mine.OnExplode, log.Log, "ExplosiveForce.OnExplode · boom");
            DemoWiring.AddString(mine.OnObjectAffected, log.Log, "ExplosiveForce.OnObjectAffected · body pushed");
        }

        private static DemoInfoContent InfoContent()
        {
            return WorldDemoBuild.Info("Neo.Tools · Physics 3D",
                "Three ready 3D physics emitters on plain Rigidbodies, set up in the Inspector and driven by persistent " +
                "listeners: MagneticField pulls and pushes the steel balls, ImpulseZone keeps three balls bouncing " +
                "through a trigger field, ExplosiveForce throws the crate pyramid.",
                "magnet.SetStrength / SetMode / enabled / OnModeChanged; pad.SetImpulseForce / " +
                "ApplyImpulseToObject(go) / OnImpulseApplied; mine.Explode() / Explode(force) / OnExplode / " +
                "OnObjectAffected.",
                "[Demo] World: Magnet (MagneticField, Toggle " + Format(PullSeconds, "0.0") + " s pull / " +
                Format(PushSeconds, "0.00") + " s push), Impulse Field (trigger BoxCollider + ImpulseZone, Custom up, " +
                "over an elastic Bounce Plate), Mine (ExplosiveForce, Manual). The card's controls call these " +
                "components directly.",
                "Play: the magnet pulses on its own; the field speeds the balls up on the way up and slows them on " +
                "the way down, so they keep bouncing (Kick revives a ball that stopped). Detonate and Mega blast " +
                "throw the crates; restart Play to restack them.",
                "Strength slider › MagneticField.SetStrength (dynamic float)",
                "Kick the balls › ImpulseZone.ApplyImpulseToObject(ball)",
                "Mega blast › ExplosiveForce.Explode(" + Format(MegaBlastForce, "0") + ")");
        }

        private static GameObject Shape(DemoSceneBuildContext ctx, Transform parent, string name, PrimitiveType type,
            Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
        {
            GameObject shape = ctx.Primitive(parent, name, type);
            shape.transform.SetLocalPositionAndRotation(localPosition, Quaternion.identity);
            shape.transform.localScale = localScale;
            MeshRenderer renderer = shape.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                ctx.MarkModified(renderer);
            }

            if (!keepCollider)
            {
                ShopNpcBuildSupport.RemoveCollider(shape);
            }

            return shape;
        }

        private static Rigidbody Body(DemoSceneBuildContext ctx, GameObject host, float mass, float linearDamping,
            float angularDamping, CollisionDetectionMode detection)
        {
            Rigidbody body = ctx.Ensure<Rigidbody>(host);
            body.isKinematic = false;
            body.useGravity = true;
            body.mass = mass;
            body.linearDamping = linearDamping;
            body.angularDamping = angularDamping;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = detection;
            return body;
        }

        private static string Format(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(Scene, step, action);
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(Scene, step, action);
        }

        private readonly struct PadBall
        {
            public PadBall(string name, Vector3 position, string materialPath)
            {
                Name = name;
                Position = position;
                MaterialPath = materialPath;
            }

            public string Name { get; }
            public Vector3 Position { get; }
            public string MaterialPath { get; }
        }

        private readonly struct PadParts
        {
            public PadParts(ImpulseZone zone, Rigidbody[] balls)
            {
                Zone = zone;
                Balls = balls;
            }

            public ImpulseZone Zone { get; }
            public Rigidbody[] Balls { get; }
        }

        private readonly struct Palette
        {
            public Palette(Material ground, Material arena, Material wall, Material pad, Material field, Material magnet,
                Material steel, Material crate, Material red, Material green, Material blue, PhysicsMaterial bounce)
            {
                Ground = ground;
                Arena = arena;
                Wall = wall;
                Pad = pad;
                Field = field;
                Magnet = magnet;
                Steel = steel;
                Crate = crate;
                Red = red;
                Green = green;
                Blue = blue;
                Bounce = bounce;
            }

            public Material Ground { get; }
            public Material Arena { get; }
            public Material Wall { get; }
            public Material Pad { get; }
            public Material Field { get; }
            public Material Magnet { get; }
            public Material Steel { get; }
            public Material Crate { get; }
            public Material Red { get; }
            public Material Green { get; }
            public Material Blue { get; }

            /// <summary>Fully elastic, frictionless physics material of the bounce plate.</summary>
            public PhysicsMaterial Bounce { get; }
        }
    }
}
