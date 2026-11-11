using System;
using System.Collections.Generic;
using Neo.NoCode;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>One numeric source of a <see cref="NoCodeFormattedText" /> authored by a builder.</summary>
    internal readonly struct LogicBindingSource
    {
        public LogicBindingSource(GameObject source, string componentTypeName, string memberName)
        {
            Source = source;
            ComponentTypeName = componentTypeName;
            MemberName = memberName;
        }

        public GameObject Source { get; }
        public string ComponentTypeName { get; }
        public string MemberName { get; }
    }

    /// <summary>
    ///     Helpers shared by the StateMachine, Condition and NoCode demo builders on top of the kit: hand-authored
    ///     lookups, filled bars driven by <see cref="SetProgress" />, NoCode binding setup through serialized fields and
    ///     world bars. Step reporting, retiring, listener ownership, the input-module upgrade, two-argument listeners
    ///     and world labels live in the kit (DemoSceneKit.Step, ctx.Retire, ctx.OwnListeners,
    ///     DemoSceneKit.UpgradeLegacyInputModules, DemoWiring.AddDynamic, DemoSceneKit.WorldLabel).
    /// </summary>
    internal static class LogicDemoBuildUtility
    {
        /// <summary>Hand-authored component of type <typeparamref name="T" /> on an object named <paramref name="objectName" />.</summary>
        public static T FindHandAuthored<T>(DemoSceneBuildContext ctx, string objectName) where T : Component
        {
            foreach (T candidate in ctx.FindAll<T>())
            {
                if (candidate.gameObject.name == objectName)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        ///     Like <see cref="FindHandAuthored{T}" /> but throws a message that tells what is missing; used for the
        ///     module objects a scene is built around (the builder never re-creates hand-authored gameplay).
        /// </summary>
        public static T RequireHandAuthored<T>(DemoSceneBuildContext ctx, string objectName, string sceneFile)
            where T : Component
        {
            T found = FindHandAuthored<T>(ctx, objectName);
            if (found != null)
            {
                return found;
            }

            List<T> all = ctx.FindAll<T>();
            string present = all.Count == 0 ? "none" : DemoSceneBuildContext.PathOf(all[0].transform);
            throw new InvalidOperationException("expected a hand-authored " + typeof(T).Name + " on '" + objectName +
                                                "' (the scene has " + present + "). Restore " + sceneFile +
                                                " from version control instead of rebuilding it.");
        }

        /// <summary>True when a Button under <paramref name="root" /> has a persistent call targeting a <typeparamref name="T" />.</summary>
        public static bool HasButtonCalling<T>(GameObject root) where T : Object
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (button.onClick.GetPersistentTarget(i) is T)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     "Bar · caption" row: caption, a value label on the right and a track with a Filled fill image (the
        ///     target of <see cref="SetProgress" />). Returns the fill image; <paramref name="value" /> is the label.
        /// </summary>
        public static Image FilledBar(DemoSceneBuildContext ctx, DemoPanel panel, string caption, Color color, float fill,
            out TMP_Text value)
        {
            RectTransform row = panel.Row("Bar · " + caption, 48f);
            TMP_Text label = DemoSceneKit.Text(ctx, row, "Caption", caption, 16f, DemoPalette.Muted);
            DemoSceneKit.Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(0.62f, 1f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(0f, 24f));
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            value = DemoSceneKit.Text(ctx, row, "Value", "-", 18f, DemoPalette.Text, TextAlignmentOptions.Right,
                FontStyles.Bold);
            DemoSceneKit.Anchor(value.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(0f, 24f));
            value.textWrappingMode = TextWrappingModes.NoWrap;

            Image track = DemoSceneKit.Image(ctx, row, "Track", DemoPalette.Track);
            DemoSceneKit.Anchor(track.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 2f), new Vector2(0f, 16f));
            Image fillImage = DemoSceneKit.Image(ctx, track.transform, "Fill", color);
            DemoSceneKit.Stretch(fillImage.rectTransform, 2f);
            // WHY: SetProgress writes Image.fillAmount, which only shows on a Filled image (with a sprite).
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = Mathf.Clamp01(fill);
            return fillImage;
        }

        /// <summary>Points a single-source NoCode binding at <paramref name="source" />.<paramref name="memberName" />.</summary>
        public static void Bind(DemoSceneBuildContext ctx, NoCodeFloatBindingBehaviour binding, GameObject source,
            string componentTypeName, string memberName, NoCodeFloatUpdateMode mode, float pollIntervalSeconds = 0.16f)
        {
            ctx.Set(binding, "_binding._useSceneSearch", false);
            ctx.Set(binding, "_binding._searchObjectName", string.Empty);
            ctx.Set(binding, "_binding._sourceRoot", source);
            ctx.Set(binding, "_binding._componentTypeName", componentTypeName);
            ctx.Set(binding, "_binding._componentOccurrence", 0);
            ctx.Set(binding, "_binding._memberName", memberName);
            ctx.Set(binding, "_updateMode", (int)mode);
            ctx.Set(binding, "_pollIntervalSeconds", pollIntervalSeconds);
        }

        /// <summary>
        ///     Configures a <see cref="NoCodeFormattedText" /> through its serialized fields (no runtime refresh in
        ///     Edit Mode): sources {0}, {1}... in order, the format, the update mode and the TMP target.
        /// </summary>
        public static NoCodeFormattedText FormattedText(DemoSceneBuildContext ctx, TMP_Text label, string format,
            NoCodeFloatUpdateMode mode, float pollIntervalSeconds, params LogicBindingSource[] sources)
        {
            NoCodeFormattedText formatted = ctx.Ensure<NoCodeFormattedText>(label.gameObject);
            SerializedObject serialized = new(formatted);
            SerializedProperty values = Require(serialized, "_values");
            values.arraySize = sources.Length;
            for (int i = 0; i < sources.Length; i++)
            {
                SerializedProperty element = values.GetArrayElementAtIndex(i);
                RequireRelative(element, "_useSceneSearch").boolValue = false;
                RequireRelative(element, "_searchObjectName").stringValue = string.Empty;
                RequireRelative(element, "_sourceRoot").objectReferenceValue = sources[i].Source;
                RequireRelative(element, "_componentTypeName").stringValue = sources[i].ComponentTypeName;
                RequireRelative(element, "_componentOccurrence").intValue = 0;
                RequireRelative(element, "_memberName").stringValue = sources[i].MemberName;
            }

            Require(serialized, "_format").stringValue = format;
            Require(serialized, "_updateMode").intValue = (int)mode;
            Require(serialized, "_pollIntervalSeconds").floatValue = pollIntervalSeconds;
            Require(serialized, "_setText").objectReferenceValue = null;
            Require(serialized, "_tmpText").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ctx.MarkModified(formatted);
            return formatted;
        }

        /// <summary>World bar: the kit disc sprite stretched to <paramref name="size" /> world units (soft ends).</summary>
        public static SpriteRenderer WorldBar(DemoSceneBuildContext ctx, Transform parent, string name, Color color,
            Vector3 localPosition, Vector2 size, int sortingOrder)
        {
            SpriteRenderer bar = DemoSceneKit.WorldSprite(ctx, parent, name, DemoSceneAssets.Disc, color, localPosition,
                1f, sortingOrder);
            bar.drawMode = SpriteDrawMode.Simple;
            bar.transform.localScale = new Vector3(size.x, size.y, 1f);
            return bar;
        }

        /// <summary>"#RRGGBB" of a color for rich text.</summary>
        public static string Hex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        private static SerializedProperty Require(SerializedObject serialized, string path)
        {
            SerializedProperty property = serialized.FindProperty(path);
            if (property == null)
            {
                throw new InvalidOperationException(serialized.targetObject.GetType().Name + " has no serialized field '" +
                                                    path + "' (did the component's fields change?).");
            }

            return property;
        }

        private static SerializedProperty RequireRelative(SerializedProperty parent, string path)
        {
            SerializedProperty property = parent.FindPropertyRelative(path);
            if (property == null)
            {
                throw new InvalidOperationException("'" + parent.propertyPath + "' has no serialized field '" + path +
                                                    "' (did ComponentFloatBinding change?).");
            }

            return property;
        }
    }
}
