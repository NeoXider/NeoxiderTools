using System;
using Neo.Samples;
using Neo.UI;
using UnityEditor;
using UnityEngine;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Completes the hand-authored UI Mesh Rig demo (UIMeshRigDemo.unity). The three uGUI workflow cards and the
    ///     uGUI / UI Toolkit / world adapters stay exactly as authored; the builder adds the fourth output adapter
    ///     (<see cref="UIMeshRigSpriteRenderer" /> with the Character layout) next to the world flag, the module info
    ///     card (collapsed, so it never covers the workflow cards) and an Input System UI module for the scene's
    ///     EventSystem.
    /// </summary>
    internal static class UIMeshRigDemoBuilder
    {
        private const string Scene = "UI Mesh Rig/UIMeshRigDemo.unity";
        private const string LogoPath = "UI Mesh Rig/UIMeshRigDemoLogo.png";
        private const string SpriteRigName = "Sprite Renderer - Character";

        // WHY: the bottom row sits at world y -2.78 (the flag cloth); x 6.45 continues its spacing to the right.
        private static readonly Vector3 SpriteRigPosition = new(6.45f, -2.78f, 0f);
        private const float SpriteRigSize = 2.2f;

        private static readonly Color Accent = new(0.35f, 0.95f, 1f);

        [DemoSceneBuilder(Scene)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Sprite logo = LoadSprite(LogoPath);
            ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
            DemoSceneKit.UpgradeLegacyInputModules(ctx);

            UIMeshRigSpriteRenderer spriteRig = SpriteRendererRig(ctx, logo);

            DemoSceneKit.InfoCardCanvas(ctx, info, new DemoInfoContent
            {
                Title = "Neo.UI · UI Mesh Rig",
                Purpose = "Bends a sprite through a grid mesh driven by Rig Points. One geometry core, four " +
                          "outputs: uGUI, UI Toolkit, world MeshRenderer and SpriteRenderer.",
                RuntimeApi = "UIMeshRigLayoutBuilder.Apply(rig, UIMeshRigLayoutPreset.Character); " +
                             "UIMeshRigPointMotion.ApplyPreset / Play; rig.SetSource(sprite, color).",
                SceneWorkflow = "Select 'Interactive Rig Graphic' or its 'Rig Point' child; the Scene overlay " +
                                "switches Setup / Pose, drag the point to bend the logo.",
                Verification = "Top row: static pose, procedural motion, Animator (moves in Play Mode only). " +
                               "Bottom row: uGUI, UI Toolkit, world mesh, SpriteRenderer.",
                Hints = new[]
                {
                    "Click a top-row logo: Button.onClick -> UIMeshRigDemoClickPulse.Play",
                    "[Demo] World/" + SpriteRigName + ": UIMeshRigSpriteRenderer deforms a runtime sprite clone",
                    "Steady rigs rebuild into reused buffers: no GC allocations per frame"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                StartCollapsed = true,
                PanelWidth = 460f,
                ShowRuntimeOverlay = true
            }, "Info Card", Accent);

            RestoreSourceSprite(ctx, spriteRig);
        }

        private static UIMeshRigSpriteRenderer SpriteRendererRig(DemoSceneBuildContext ctx, Sprite logo)
        {
            GameObject world = ctx.Root("World");
            GameObject host = ctx.Child(world.transform, SpriteRigName);
            UIMeshRigSpriteRenderer rig = ctx.Ensure<UIMeshRigSpriteRenderer>(host);
            if (rig.Sprite != logo)
            {
                rig.SetSource(logo, Color.white);
            }

            if (rig.Columns != 16 || rig.Rows != 20)
            {
                rig.SetGridResolution(16, 20);
            }

            Vector2 native = rig.NativeSize;
            float longest = Mathf.Max(native.x, native.y);
            host.transform.localPosition = SpriteRigPosition;
            host.transform.localRotation = Quaternion.identity;
            host.transform.localScale = Vector3.one * (longest > 0f ? SpriteRigSize / longest : 1f);

            // WHY: points are authored once; later passes keep them so hand-tuned bind poses survive rebuilds.
            if (rig.Points.Count == 0)
            {
                UIMeshRigLayoutBuilder.Apply(rig, UIMeshRigLayoutPreset.Character, true, true);
                ctx.Note("Authored the Character layout on '" + DemoSceneBuildContext.PathOf(host.transform) + "'.");
            }

            ctx.Keep(host);
            ctx.MarkModified(rig);
            ctx.MarkModified(host.transform);
            return rig;
        }

        /// <summary>
        ///     <see cref="UIMeshRigSpriteRenderer" /> is [ExecuteAlways] and assigns a HideAndDontSave runtime clone to
        ///     the SpriteRenderer when the scene opens; OnEnable re-clones on load.
        /// </summary>
        /// <remarks>
        ///     WHY: ctx.AllowRuntimeAsset would let the clone pass validation, but the saved reference would be empty;
        ///     pointing the renderer back at the source asset keeps the logo visible even without the adapter.
        /// </remarks>
        private static void RestoreSourceSprite(DemoSceneBuildContext ctx, UIMeshRigSpriteRenderer rig)
        {
            SpriteRenderer renderer = rig.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null || EditorUtility.IsPersistent(renderer.sprite))
            {
                return;
            }

            renderer.sprite = rig.Sprite;
            ctx.MarkModified(renderer);
        }

        private static Sprite LoadSprite(string relativePath)
        {
            string path = DemoSampleRoots.Demo + "/" + relativePath;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(Scene + " (load logo sprite): no Sprite at '" + path +
                                                    "'. Restore the PNG or set its Texture Type to Sprite.");
            }

            return sprite;
        }
    }
}
