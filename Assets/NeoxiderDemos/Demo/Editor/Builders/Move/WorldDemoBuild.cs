using System;
using System.Collections.Generic;
using System.Text;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Shared steps of the Tools world demos (Follow, DrawExample, MouseEffectorExample, CameraConstraint). The
    ///     scene world stays fully visible: no backdrop, the controls sit in a right-hand card with an inline log
    ///     (<see cref="DemoPanel.LogRow" />), the module info card in the top-left corner. Hand-authored gameplay
    ///     objects are found by name and kept; listeners the builders add to hand-authored events are re-owned with
    ///     ctx.OwnListeners on every pass, so rebuilds neither duplicate them nor leave calls to deleted generated
    ///     objects behind.
    /// </summary>
    internal static class WorldDemoBuild
    {
        public const float InfoCardWidth = 400f;
        public const float ControlsWidth = 450f;

        private const string Bullet = "•";

        /// <summary>
        ///     First hand-authored <typeparamref name="T" /> whose object has one of <paramref name="names" /> (in
        ///     priority order); when nothing matches and the scene has exactly one <typeparamref name="T" />, that one
        ///     (the object was renamed). Null when neither applies.
        /// </summary>
        public static T FindNamed<T>(DemoSceneBuildContext ctx, params string[] names) where T : Component
        {
            List<T> all = ctx.FindAll<T>();
            foreach (string name in names)
            {
                foreach (T candidate in all)
                {
                    if (candidate.name == name)
                    {
                        return candidate;
                    }
                }
            }

            if (all.Count == 1)
            {
                ctx.Note("Using the only " + typeof(T).Name + " '" + DemoSceneBuildContext.PathOf(all[0].transform) +
                         "' (expected '" + string.Join("' or '", names) + "').");
                return all[0];
            }

            return null;
        }

        /// <summary><see cref="FindNamed{T}" /> that throws a message listing what the scene has instead.</summary>
        public static T RequireNamed<T>(DemoSceneBuildContext ctx, params string[] names) where T : Component
        {
            T found = FindNamed<T>(ctx, names);
            if (found != null)
            {
                return found;
            }

            List<T> all = ctx.FindAll<T>();
            StringBuilder present = new();
            foreach (T candidate in all)
            {
                present.Append(present.Length == 0 ? string.Empty : ", ").Append(DemoSceneBuildContext.PathOf(candidate.transform));
            }

            throw new InvalidOperationException("expected a hand-authored " + typeof(T).Name + " named '" +
                                                string.Join("' or '", names) + "', the scene has " +
                                                (all.Count == 0 ? "none" : present.ToString()) +
                                                ". Restore the scene from version control instead of rebuilding it.");
        }

        /// <summary>Frame for a world demo: no backdrop, controls card on the right, no bottom log feed.</summary>
        public static DemoFrame Frame(DemoSceneBuildContext ctx, string title, Color accent)
        {
            return DemoSceneKit.Frame(ctx, new DemoFrameOptions(title, accent)
            {
                Backdrop = DemoBackdrop.None,
                Placement = DemoContentPlacement.Right,
                ContentWidth = ControlsWidth,
                ContentOffsetY = 0f,
                LogFeed = false
            });
        }

        /// <summary>Info card defaults of the world demos (top-left, compact, expanded).</summary>
        public static DemoInfoContent Info(string title, string purpose, string runtimeApi, string sceneWorkflow,
            string verification, params string[] hints)
        {
            return new DemoInfoContent
            {
                Title = title,
                Purpose = purpose,
                RuntimeApi = runtimeApi,
                SceneWorkflow = sceneWorkflow,
                Verification = verification,
                Hints = hints ?? Array.Empty<string>(),
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = InfoCardWidth,
                StartCollapsed = false
            };
        }

        /// <summary><see cref="DemoPanel.Toggle" /> with a smaller single-line caption that may name an API call.</summary>
        public static Toggle CompactToggle(DemoPanel panel, string label, bool isOn)
        {
            Toggle toggle = panel.Toggle(label, isOn);
            TMP_Text caption = RowCaption(toggle.transform, label);
            caption.fontSize = 16f;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.overflowMode = TextOverflowModes.Ellipsis;
            DemoSceneKit.Anchor(caption.rectTransform, new Vector2(0f, 0f), new Vector2(0.88f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, Vector2.zero);
            return toggle;
        }

        /// <summary><see cref="DemoPanel.SliderRow" /> with a smaller single-line caption that may name an API call.</summary>
        public static DemoSliderRow CompactSlider(DemoPanel panel, string label, float min, float max, float value,
            bool wholeNumbers, string format)
        {
            DemoSliderRow row = panel.SliderRow(label, min, max, value, wholeNumbers, format);
            row.Caption.fontSize = 15f;
            row.Caption.textWrappingMode = TextWrappingModes.NoWrap;
            row.Caption.overflowMode = TextOverflowModes.Ellipsis;
            return row;
        }

        /// <summary>Two-line muted hint row (a <see cref="DemoPanel.Note" /> tall enough to wrap).</summary>
        public static TMP_Text Hint(DemoPanel panel, string text)
        {
            return panel.Note(text, 42f);
        }

        /// <summary>World sprite stretched to <paramref name="size" /> (world units) and turned by <paramref name="angle" />.</summary>
        public static SpriteRenderer Bar(DemoSceneBuildContext ctx, Transform parent, string name, Color color,
            Vector3 localPosition, Vector2 size, float angle, int sortingOrder)
        {
            SpriteRenderer bar = DemoSceneKit.WorldSprite(ctx, parent, name, DemoSceneAssets.Disc, color, localPosition, 1f,
                sortingOrder);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            bar.transform.localScale = new Vector3(size.x, size.y, 1f);
            return bar;
        }

        /// <summary>Rich-text colored bullet for captions that name a scene object by its tint.</summary>
        public static string Dot(Color color)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + Bullet + "</color> ";
        }

        private static TMP_Text RowCaption(Transform rowChild, string label)
        {
            Transform row = rowChild.parent;
            Transform node = row != null ? row.Find("Caption") : null;
            TMP_Text caption = node != null ? node.GetComponent<TMP_Text>() : null;
            if (caption == null)
            {
                throw new InvalidOperationException("Kit row '" + label +
                                                    "' has no 'Caption' label (did the DemoPanel row layout change?).");
            }

            return caption;
        }

        public static bool ReadBool(Object owner, string path, bool fallback)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(path);
            return property != null && property.propertyType == SerializedPropertyType.Boolean
                ? property.boolValue
                : fallback;
        }

        public static int ReadInt(Object owner, string path, int fallback)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(path);
            if (property == null)
            {
                return fallback;
            }

            return property.propertyType switch
            {
                SerializedPropertyType.Integer => property.intValue,
                SerializedPropertyType.Enum => property.intValue,
                _ => fallback
            };
        }

        public static float ReadFloat(Object owner, string path, float fallback)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(path);
            return property != null && property.propertyType == SerializedPropertyType.Float
                ? property.floatValue
                : fallback;
        }

        public static T ReadReference<T>(Object owner, string path) where T : Object
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(path);
            return property != null && property.propertyType == SerializedPropertyType.ObjectReference
                ? property.objectReferenceValue as T
                : null;
        }
    }
}
