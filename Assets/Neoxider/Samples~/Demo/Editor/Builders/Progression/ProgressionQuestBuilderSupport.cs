using System;
using System.Collections.Generic;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Helpers shared by the Progression and Quest demo builders: serialized-property access that fails with the
    ///     scene, owner and field in the message, string-list writes, optional fields, multi-line status rows and
    ///     definition validation reporting.
    /// </summary>
    internal static class ProgressionQuestBuilderSupport
    {
        /// <summary>Required property of a serialized object; throws naming scene, owner and path when missing.</summary>
        public static SerializedProperty Require(SerializedObject serialized, string path, string scene)
        {
            SerializedProperty property = serialized.FindProperty(path);
            if (property == null)
            {
                throw new InvalidOperationException(scene + ": " + serialized.targetObject.GetType().Name + " '" +
                                                    serialized.targetObject.name + "' has no serialized field '" + path +
                                                    "' (the module API changed; update the demo builder).");
            }

            return property;
        }

        /// <summary>Required relative property of an array element or nested struct.</summary>
        public static SerializedProperty Relative(SerializedProperty owner, string path, string scene)
        {
            SerializedProperty property = owner.FindPropertyRelative(path);
            if (property == null)
            {
                throw new InvalidOperationException(scene + ": '" + owner.propertyPath + "' has no field '" + path +
                                                    "' (the module API changed; update the demo builder).");
            }

            return property;
        }

        /// <summary>Replaces a serialized string list with <paramref name="values" />.</summary>
        public static void SetStrings(SerializedProperty list, IReadOnlyList<string> values)
        {
            int count = values?.Count ?? 0;
            list.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                list.GetArrayElementAtIndex(i).stringValue = values[i] ?? string.Empty;
            }
        }

        /// <summary>
        ///     Sets an optional serialized field (present only in some build configurations); returns false and
        ///     leaves a note when the field does not exist.
        /// </summary>
        public static bool TrySet(DemoSceneBuildContext ctx, Object target, string path, object value)
        {
            if (new SerializedObject(target).FindProperty(path) == null)
            {
                ctx.Note("Optional field '" + path + "' not present on " + target.GetType().Name + "; skipped.");
                return false;
            }

            ctx.Set(target, path, value);
            return true;
        }

        /// <summary>Wrapping status text row named "Status · <paramref name="name" />" for runtime-written text.</summary>
        public static TMP_Text StatusText(DemoPanel panel, string name, string initial, float fontSize, float height,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            RectTransform row = panel.Row("Status · " + name, height);
            TMP_Text text = DemoSceneKit.Text(panel.Context, row, "Text", initial, fontSize, DemoPalette.Text, alignment);
            DemoSceneKit.Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        /// <summary>Turns ValidateDefinition() issues of a demo asset into scene warnings.</summary>
        public static void ReportIssues(DemoSceneBuildContext ctx, string scene, Object asset, IReadOnlyList<string> issues)
        {
            if (issues == null)
            {
                return;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                ctx.Warn(scene + ": " + asset.name + ": " + issues[i]);
            }
        }
    }
}
