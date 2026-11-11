using System;
using System.Collections.Generic;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>Parts of a resource row authored by <see cref="RpgDemoBuilderUtility.ResourceRow" />.</summary>
    internal readonly struct RpgResourceRow
    {
        public RpgResourceRow(RpgResourceBinding binding, TMP_Text current, TMP_Text max, Image fill)
        {
            Binding = binding;
            Current = current;
            Max = max;
            Fill = fill;
        }

        public RpgResourceBinding Binding { get; }
        public TMP_Text Current { get; }
        public TMP_Text Max { get; }
        public Image Fill { get; }
    }

    /// <summary>
    ///     Shared authoring helpers of the Neo.Rpg demo builders: NoCode resource/stat rows bound with
    ///     <see cref="RpgResourceBinding" /> / <see cref="RpgStatBinding" />, <see cref="RpgNoCodeAction" /> buttons,
    ///     bootstrap-leftover clean-up through ctx.Retire and the dark world look. Step reporting, readouts, setters
    ///     and listener ownership come from the kit.
    /// </summary>
    internal static class RpgDemoBuilderUtility
    {
        /// <summary>Coral accent shared by the RPG demos.</summary>
        internal static readonly Color Accent = new(0.96f, 0.45f, 0.38f);

        internal const string MaterialFolder = "Materials/";

        /// <summary>
        ///     "Resource · caption" row: caption, live "current / max" readouts and a filled bar, all driven by an
        ///     <see cref="RpgResourceBinding" /> on the row through persistent listeners (the NoCode display path).
        /// </summary>
        internal static RpgResourceRow ResourceRow(DemoSceneBuildContext ctx, DemoPanel panel, string caption,
            RpgCharacter character, RpgStatId resource, Color color, float current, float max)
        {
            RectTransform row = panel.Row("Resource · " + caption, 46f);
            TMP_Text label = DemoSceneKit.Text(ctx, row, "Caption", caption, 18f, DemoPalette.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            DemoSceneKit.Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(0f, 24f));

            TMP_Text currentText = DemoSceneKit.Text(ctx, row, "Current", Format(current), 18f, color,
                TextAlignmentOptions.Right, FontStyles.Bold);
            DemoSceneKit.Anchor(currentText.rectTransform, new Vector2(0.55f, 1f), new Vector2(0.8f, 1f),
                new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 24f));
            TMP_Text maxText = DemoSceneKit.Text(ctx, row, "Max", " / " + Format(max), 18f, DemoPalette.Muted,
                TextAlignmentOptions.Left);
            DemoSceneKit.Anchor(maxText.rectTransform, new Vector2(0.8f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(0f, 24f));

            Image track = DemoSceneKit.Image(ctx, row, "Track", DemoPalette.Track);
            DemoSceneKit.Anchor(track.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 2f), new Vector2(0f, 14f));
            Image fill = DemoSceneKit.Image(ctx, track.transform, "Fill", color);
            DemoSceneKit.Stretch(fill.rectTransform, 2f);
            // WHY: a Filled image lets the binding drive Image.fillAmount directly from a persistent listener.
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            RpgResourceBinding binding = ctx.Ensure<RpgResourceBinding>(row.gameObject);
            ctx.Set(binding, "_character", character);
            SetStatId(ctx, binding, "_resourceId", resource);

            DemoValueReadout currentReadout = DemoSceneKit.Readout(ctx, currentText, "0");
            DemoValueReadout maxReadout = DemoSceneKit.Readout(ctx, maxText, "0", " / ");
            DemoWiring.AddDynamic(binding.OnCurrent, currentReadout.SetFloat);
            DemoWiring.AddDynamic(binding.OnMax, maxReadout.SetFloat);
            DemoWiring.AddSetter(binding.OnPercent, fill, nameof(Image.fillAmount));
            return new RpgResourceRow(binding, currentText, maxText, fill);
        }

        /// <summary>Value row whose label follows a stat through an <see cref="RpgStatBinding" /> on the row.</summary>
        internal static RpgStatBinding StatRow(DemoSceneBuildContext ctx, DemoPanel panel, string caption,
            RpgCharacter character, RpgStatId stat, float initial)
        {
            TMP_Text value = panel.ValueLabel(caption, Format(initial));
            RpgStatBinding binding = ctx.Ensure<RpgStatBinding>(value.transform.parent.gameObject);
            ctx.Set(binding, "_character", character);
            SetStatId(ctx, binding, "_statId", stat);
            DemoWiring.AddDynamic(binding.OnValue, DemoSceneKit.Readout(ctx, value, "0.#").SetFloat);
            return binding;
        }

        /// <summary>
        ///     Turns an authored button into a designer-style NoCode button: an <see cref="RpgNoCodeAction" /> on the
        ///     button object, Button.onClick → Execute, OnResultMessage → result readout and log feed.
        /// </summary>
        internal static RpgNoCodeAction NoCodeButton(DemoSceneBuildContext ctx, Button button, RpgNoCodeActionType type,
            RpgCharacter character, float amount, DemoValueReadout result, DemoLogFeed log,
            RpgAttackController attackController = null)
        {
            RpgNoCodeAction action = ctx.Ensure<RpgNoCodeAction>(button.gameObject);
            ctx.Set(action, "_actionType", (int)type);
            ctx.Set(action, "_character", character);
            ctx.Set(action, "_amount", amount);
            ctx.Set(action, "_attackController", attackController);
            DemoWiring.AddVoid(button.onClick, action.Execute);
            if (result != null)
            {
                DemoWiring.AddDynamic(action.OnResultMessage, result.SetText);
            }

            if (log != null)
            {
                DemoWiring.AddDynamic(action.OnResultMessage, log.Log);
            }

            return action;
        }

        /// <summary>Writes an <see cref="RpgStatId" /> field (preset + custom string) through SerializedObject.</summary>
        internal static void SetStatId(DemoSceneBuildContext ctx, Object target, string field, RpgStatId id)
        {
            ctx.Set(target, field + ".preset", (int)id.preset);
            ctx.Set(target, field + ".customId", id.preset == RpgStatPreset.Custom ? id.customId ?? string.Empty : string.Empty);
        }

        /// <summary>Paints a renderer with a demo material asset (Materials/&lt;file&gt;.mat); missing assets are reported.</summary>
        internal static void Paint(DemoSceneBuildContext ctx, Component target, string materialFile)
        {
            Renderer renderer = target != null ? target.GetComponent<Renderer>() : null;
            if (renderer == null)
            {
                return;
            }

            string path = DemoSampleRoots.Demo + "/" + MaterialFolder + materialFile;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                ctx.Warn("Demo material missing: " + path + "; '" + target.name + "' keeps its material.");
                return;
            }

            if (renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
            }

            ctx.MarkModified(renderer);
        }

        /// <summary>Dark solid clear color tinted by the accent so the floating cards and the header stay readable.</summary>
        internal static void DarkBackground(DemoSceneBuildContext ctx, Camera camera, Color accent)
        {
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DemoPalette.CameraBackground(accent);
            ctx.MarkModified(camera);
        }

        /// <summary>
        ///     Deletes bootstrap leftovers: top-level hand-authored objects named <paramref name="objectName" /> beyond
        ///     the one kept (the first matching <paramref name="isCanonical" />, otherwise the first by sibling order).
        /// </summary>
        /// <returns>The kept object, or null when the scene has none.</returns>
        internal static GameObject KeepSingleRoot(DemoSceneBuildContext ctx, string objectName,
            Predicate<GameObject> isCanonical)
        {
            List<GameObject> named = NamedRoots(ctx, objectName);
            if (named.Count == 0)
            {
                return null;
            }

            GameObject kept = named.Find(isCanonical) ?? named[0];
            RemoveAllBut(ctx, named, kept);
            return kept;
        }

        /// <summary>Top-level hand-authored objects with the given name, in sibling order.</summary>
        internal static List<GameObject> NamedRoots(DemoSceneBuildContext ctx, string objectName)
        {
            List<GameObject> named = new();
            foreach (GameObject root in ctx.Scene.GetRootGameObjects())
            {
                if (root.name == objectName && !DemoSceneBuildContext.IsGenerated(root))
                {
                    named.Add(root);
                }
            }

            return named;
        }

        /// <summary>Deletes every object of <paramref name="candidates" /> except <paramref name="kept" />.</summary>
        internal static void RemoveAllBut(DemoSceneBuildContext ctx, List<GameObject> candidates, GameObject kept)
        {
            foreach (GameObject candidate in candidates)
            {
                if (candidate != null && candidate != kept)
                {
                    ctx.Retire(candidate, "bootstrap leftover at " + candidate.transform.position);
                }
            }
        }

        internal static string Format(float value)
        {
            return value.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
