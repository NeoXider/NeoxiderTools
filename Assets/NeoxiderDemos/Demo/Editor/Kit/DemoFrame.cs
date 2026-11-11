using System;
using System.Collections.Generic;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Demo.Editor
{
    /// <summary>How the full-screen backdrop behind the demo frame is drawn.</summary>
    public enum DemoBackdrop
    {
        /// <summary>Opaque dark gradient tinted by the accent (UI-only demos).</summary>
        Gradient = 0,

        /// <summary>Same gradient at reduced alpha, the world stays faintly visible.</summary>
        Dim = 1,

        /// <summary>No backdrop: the camera's world is fully visible behind the cards.</summary>
        None = 2
    }

    /// <summary>Where the main content card sits.</summary>
    public enum DemoContentPlacement
    {
        Center = 0,
        Left = 1,
        Right = 2
    }

    /// <summary>Anchor of an extra floating panel created with <see cref="DemoFrame.Panel" />.</summary>
    public enum DemoPanelPlacement
    {
        TopLeft = 0,
        TopRight = 1,
        BottomLeft = 2,
        BottomRight = 3,
        Left = 4,
        Right = 5,
        Center = 6
    }

    /// <summary>EventSystem input module the kit authors.</summary>
    public enum DemoInputModule
    {
        /// <summary>
        ///     InputSystemUIInputModule when the Input System package is present (works with "Input System" and
        ///     "Both" handling, the Unity 6 default for new projects), StandaloneInputModule otherwise.
        /// </summary>
        Auto = 0,
        InputSystem = 1,
        Legacy = 2
    }

    /// <summary>Options of <see cref="DemoSceneKit.Frame" />. Defaults reproduce the NeoDemoShell look.</summary>
    public sealed class DemoFrameOptions
    {
        public DemoFrameOptions(string title, Color accent)
        {
            Title = title;
            Accent = accent;
        }

        public string Title { get; set; }
        public Color Accent { get; set; }
        public string Overline { get; set; } = "NEOXIDER · MODULE DEMO";
        public DemoBackdrop Backdrop { get; set; } = DemoBackdrop.Gradient;

        /// <summary>Backdrop alpha for <see cref="DemoBackdrop.Dim" />.</summary>
        public float DimAlpha { get; set; } = 0.55f;

        public DemoContentPlacement Placement { get; set; } = DemoContentPlacement.Center;
        public float ContentWidth { get; set; } = 1000f;

        /// <summary>Vertical offset of the content card from its anchor.</summary>
        public float ContentOffsetY { get; set; } = 8f;

        /// <summary>
        ///     Author the main content card (<see cref="DemoFrame.Content" />). Off for HUD-only game demos that need
        ///     camera, EventSystem, canvas, header, log and info card but lay out their own panels.
        /// </summary>
        public bool ContentCard { get; set; } = true;

        public bool Header { get; set; } = true;

        /// <summary>Header width; null follows <see cref="ContentWidth" /> + 40 (set it for a narrow side card).</summary>
        public float? HeaderWidth { get; set; }

        public bool LogFeed { get; set; } = true;

        /// <summary>Bottom log feed width; null follows <see cref="ContentWidth" /> + 40.</summary>
        public float? LogWidth { get; set; }

        public int LogLines { get; set; } = 5;
        public int SortingOrder { get; set; } = 25000;
        public Vector2 ReferenceResolution { get; set; } = new(1920f, 1080f);

        /// <summary>Author an EventSystem when the scene has no hand-authored one.</summary>
        public bool EventSystem { get; set; } = true;

        public DemoInputModule InputModule { get; set; } = DemoInputModule.Auto;

        /// <summary>
        ///     Replace a legacy StandaloneInputModule on a hand-authored EventSystem with the resolved module
        ///     (<see cref="DemoSceneKit.UpgradeLegacyInputModule" />); a note in the summary records it.
        ///     StandaloneInputModule throws in Input-System-only projects, the Unity 6 default.
        /// </summary>
        public bool UpgradeLegacyInputModule { get; set; } = true;

        /// <summary>Author an orthographic camera when the scene has no hand-authored camera.</summary>
        public bool Camera { get; set; } = true;

        /// <summary>Generated root key of the frame canvas ("[Demo] Frame").</summary>
        public string RootKey { get; set; } = "Frame";
    }

    /// <summary>Texts pushed into a <see cref="ModuleDemoSceneInfo" /> before its card is authored.</summary>
    public sealed class DemoInfoContent
    {
        public string Title { get; set; }
        public string Purpose { get; set; }
        public string RuntimeApi { get; set; }
        public string SceneWorkflow { get; set; }
        public string Verification { get; set; }
        public string[] Hints { get; set; }
        public ModuleDemoSceneInfo.Corner? Corner { get; set; }

        /// <summary>Collapsed state on Play; the kit also authors the card in this state.</summary>
        public bool? StartCollapsed { get; set; }

        public float? PanelWidth { get; set; }

        /// <summary>Show the authored card on Play (forces a hand-authored info whose overlay was switched off).</summary>
        public bool? ShowRuntimeOverlay { get; set; }
    }

    /// <summary>Handle to an authored demo frame returned by <see cref="DemoSceneKit.Frame" />.</summary>
    public sealed class DemoFrame
    {
        internal DemoFrame(DemoSceneBuildContext context, DemoFrameOptions options)
        {
            Context = context;
            Options = options;
            Accent = options.Accent;
        }

        public DemoSceneBuildContext Context { get; }
        public DemoFrameOptions Options { get; }
        public Color Accent { get; }

        /// <summary>"[Demo] Frame" root that holds the Canvas.</summary>
        public GameObject Root { get; internal set; }

        public Canvas Canvas { get; internal set; }
        public RectTransform CanvasRect { get; internal set; }

        /// <summary>Backdrop image (null with <see cref="DemoBackdrop.None" />).</summary>
        public Image Backdrop { get; internal set; }

        public TMP_Text Title { get; internal set; }
        public TMP_Text Overline { get; internal set; }

        /// <summary>The content card; add rows to it (null with <see cref="DemoFrameOptions.ContentCard" /> off).</summary>
        public DemoPanel Content { get; internal set; }

        /// <summary>Bottom log feed (null when disabled in the options).</summary>
        public DemoLogFeed Log { get; internal set; }

        /// <summary>Scene camera (hand-authored or generated).</summary>
        public Camera Camera { get; internal set; }

        public EventSystem EventSystem { get; internal set; }

        /// <summary>Extra floating card with a vertical layout, anchored at <paramref name="placement" />.</summary>
        public DemoPanel Panel(string name, DemoPanelPlacement placement, float width = 420f, Vector2 offset = default)
        {
            return DemoSceneKit.FloatingPanel(this, name, placement, width, offset);
        }

        /// <summary>Full-screen empty layer on the frame canvas for custom UI (placed in call order).</summary>
        public RectTransform Layer(string name)
        {
            RectTransform layer = Context.UiChild(CanvasRect, name);
            DemoSceneKit.Stretch(layer);
            return layer;
        }

        /// <summary>Authors the module info card on this frame and binds it to <paramref name="info" />.</summary>
        public ModuleDemoSceneInfo InfoCard(ModuleDemoSceneInfo info, DemoInfoContent content = null)
        {
            return DemoSceneKit.InfoCard(Context, CanvasRect, info, content, Accent);
        }

        /// <summary>
        ///     <see cref="DemoPointerGate" /> on "[Demo] Services/Pointer Gate" (a hand-authored gate wins) that keeps
        ///     the world-input <paramref name="gated" /> behaviours (drawers, mouse movers, spawners) from reacting to
        ///     presses on this frame's UI; only this frame's GraphicRaycaster is checked. Call it once per scene.
        /// </summary>
        public DemoPointerGate GateWorldInput(params Behaviour[] gated)
        {
            List<Behaviour> valid = new();
            foreach (Behaviour behaviour in gated ?? Array.Empty<Behaviour>())
            {
                if (behaviour != null)
                {
                    valid.Add(behaviour);
                }
            }

            List<GraphicRaycaster> raycasters = new();
            GraphicRaycaster frameRaycaster = Root != null ? Root.GetComponent<GraphicRaycaster>() : null;
            if (frameRaycaster != null)
            {
                raycasters.Add(frameRaycaster);
            }

            DemoPointerGate gate = Context.FindOrCreate<DemoPointerGate>("Services", "Pointer Gate");
            Context.Set(gate, "_gated", valid);
            Context.Set(gate, "_uiRaycasters", raycasters);
            return gate;
        }
    }
}
