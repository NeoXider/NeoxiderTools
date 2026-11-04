using System;
using Neo.Samples;
using Neo.Shop;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Shared layout of the Neo.Bonus demo scenes: the gameplay prefab (wheel or slot machine) stays on its own
    ///     screen-space canvas in the middle of the screen, the DemoSceneKit frame draws header, log and the info
    ///     card around it (no backdrop, so the gameplay stays visible) and the content card sits on the right.
    ///     Both canvases scale with the same 1920x1080 reference, so the layout holds at any resolution.
    /// </summary>
    internal static class BonusDemoStage
    {
        /// <summary>Prefabs/Bonus/WheelFortune.prefab (GUIDs survive the UPM install, paths do not).</summary>
        public const string WheelPrefabGuid = "fe17a43c9caff1443bfcad1dc0d3475e";

        /// <summary>Prefabs/Bonus/Slot/SlotUI.prefab.</summary>
        public const string SlotPrefabGuid = "0b236f3e79dd0e04692c9b4a5daf8a74";

        /// <summary>Width of the right content card; the header and log feed of the frame follow it.</summary>
        public const float CardWidth = 440f;

        public static readonly Vector2 ReferenceResolution = new(1920f, 1080f);

        /// <summary>Frame options of every Bonus demo: no backdrop, content card on the right.</summary>
        public static DemoFrameOptions FrameOptions(string title, Color accent)
        {
            return new DemoFrameOptions(title, accent)
            {
                Backdrop = DemoBackdrop.None,
                Placement = DemoContentPlacement.Right,
                ContentWidth = CardWidth,
                ContentOffsetY = 0f,
                LogLines = 5
            };
        }

        /// <summary>
        ///     The scene's hand-authored <typeparamref name="T" /> when it has one; otherwise an instance of the package
        ///     prefab on a generated "[Demo] Stage" canvas (empty scenes, tests).
        /// </summary>
        public static T FindOrInstantiate<T>(DemoSceneBuildContext ctx, string prefabGuid, string objectName, string scene)
            where T : Component
        {
            T authored = ctx.Find<T>();
            if (authored != null)
            {
                return authored;
            }

            GameObject prefab = LoadPrefab(prefabGuid, objectName, scene);
            GameObject stage = ctx.Root("Stage", true);
            DemoSceneKit.OverlayCanvas(ctx, stage, 0, ReferenceResolution);
            GameObject instance = ctx.Prefab(stage.transform, objectName, prefab);
            T component = instance.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(scene + ": the package prefab '" + AssetDatabase.GetAssetPath(prefab) +
                                                    "' has no " + typeof(T).Name + " on its root.");
            }

            ctx.Note(scene + ": no hand-authored " + typeof(T).Name + "; instantiated the package prefab under [Demo] Stage.");
            return component;
        }

        /// <summary>
        ///     Root canvas of the gameplay object, switched to ScaleWithScreenSize 1920x1080 (match 0.5) like the frame
        ///     canvas so the authored layout lines up with the cards at every resolution.
        /// </summary>
        public static Canvas ConfigureStageCanvas(DemoSceneBuildContext ctx, Component gameplay, string scene)
        {
            Canvas[] canvases = gameplay.GetComponentsInParent<Canvas>(true);
            if (canvases.Length == 0)
            {
                throw new InvalidOperationException(scene + ": '" + DemoSceneBuildContext.PathOf(gameplay.transform) +
                                                    "' is not under a Canvas; the demo needs it on a screen-space canvas.");
            }

            Canvas canvas = canvases[canvases.Length - 1];
            CanvasScaler scaler = ctx.Ensure<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            ctx.MarkModified(scaler);
            return canvas;
        }

        /// <summary>
        ///     The demo gradient behind the gameplay, as the first child of its canvas: a generated
        ///     "[Demo] Backdrop" root under a hand-authored canvas, or a "Backdrop" child of a generated stage.
        /// </summary>
        public static void Backdrop(DemoSceneBuildContext ctx, Canvas canvas, Color accent)
        {
            RectTransform host = DemoSceneBuildContext.IsGenerated(canvas.gameObject)
                ? ctx.UiChild(canvas.transform, "Backdrop")
                : (RectTransform)ctx.RootUnder(canvas.transform, "[Demo] Backdrop", true).transform;
            host.SetAsFirstSibling();
            DemoSceneKit.StyleBackdrop(ctx, host, accent);
        }

        /// <summary>
        ///     Session-only <see cref="Money" /> wallet under [Demo] Services (a hand-authored wallet wins). It never
        ///     touches SaveProvider, so the demo starts from the same balance on every Play.
        /// </summary>
        public static Money Wallet(DemoSceneBuildContext ctx, string saveKey)
        {
            Money wallet = ctx.FindOrCreate<Money>("Services", "Wallet (Money)");
            ctx.Set(wallet, "isNetworked", false);
            ctx.Set(wallet, "_persistMoney", false);
            ctx.Set(wallet, "_moneySave", saveKey);
            return wallet;
        }

        /// <summary>Places a child RectTransform of an authored prefab instance (records a prefab override).</summary>
        public static void Place(DemoSceneBuildContext ctx, Transform root, string childName, Vector2 position, float scale,
            string scene)
        {
            RectTransform rect = root.Find(childName) as RectTransform;
            if (rect == null)
            {
                ctx.Warn(scene + ": '" + childName + "' not found under '" + DemoSceneBuildContext.PathOf(root) +
                         "'; its layout was left as authored.");
                return;
            }

            rect.anchoredPosition = position;
            rect.localScale = new Vector3(scale, scale, 1f);
            ctx.MarkModified(rect);
        }

        /// <summary>Required serialized property of an asset or component, with a message naming scene and owner.</summary>
        public static SerializedProperty Require(SerializedObject serialized, string path, string scene)
        {
            SerializedProperty property = serialized.FindProperty(path);
            if (property == null)
            {
                throw new InvalidOperationException(scene + ": " + serialized.targetObject.GetType().Name + " '" +
                                                    serialized.targetObject.name + "' has no serialized field '" + path +
                                                    "' (the Bonus data layout changed; update the builder).");
            }

            return property;
        }

        private static GameObject LoadPrefab(string guid, string label, string scene)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException(scene + ": the scene has no " + label + " and the package prefab (guid " +
                                                    guid + ") was not found; reimport com.neoxider.tools.");
            }

            return prefab;
        }
    }
}
