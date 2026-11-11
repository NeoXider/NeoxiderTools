using UnityEditor;
using UnityEngine;

namespace Neo.Pages.Editor
{
    /// <summary>
    ///     IMGUI pieces shared by the Pages inspectors: the segmented mode switch and the PageId dropdown. Contents
    ///     and layout options are cached and layout groups use Begin/End instead of scope objects, so drawing them
    ///     allocates nothing per repaint.
    /// </summary>
    internal static class PagesEditorGUI
    {
        internal const string SourceLabelAll = "Source: all project folders";

        internal const string NoPageIdsMessage =
            "There are no PageId assets in the project.\nCreate one with Create > Neoxider > Pages > Page Id, or run " +
            "Neoxider > Samples > Generate Default PageIds.";

        internal static readonly GUIContent DropdownMode = new("Dropdown", "Pick from the PageId assets of the project");
        internal static readonly GUIContent AssetMode = new("Asset", "Manually pick a specific PageId asset");
        internal static readonly GUIContent ButtonsMode = new("Buttons", "Quick buttons for every PageId asset of the project");

        internal static readonly GUIContent[] DropdownOrAsset = { DropdownMode, AssetMode };
        internal static readonly GUIContent[] ButtonsDropdownOrAsset = { ButtonsMode, DropdownMode, AssetMode };

        private static readonly GUILayoutOption[] ModeButtonOptions = { GUILayout.Height(22f) };

        /// <summary>Row of mini buttons; returns the selected index.</summary>
        internal static int SegmentedMode(int value, GUIContent[] modes)
        {
            int selected = value;
            Color previousBackground = GUI.backgroundColor;
            Color previousContent = GUI.contentColor;
            Color selectedBackground = EditorGUIUtility.isProSkin
                ? new Color(0.25f, 0.55f, 0.95f, 1f)
                : new Color(0.20f, 0.45f, 0.90f, 1f);

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < modes.Length; i++)
            {
                GUIStyle style = i == 0
                    ? EditorStyles.miniButtonLeft
                    : i == modes.Length - 1
                        ? EditorStyles.miniButtonRight
                        : EditorStyles.miniButtonMid;
                GUI.backgroundColor = selected == i ? selectedBackground : Color.white;
                GUI.contentColor = Color.white;
                if (GUILayout.Button(modes[i], style, ModeButtonOptions))
                {
                    selected = i;
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = previousBackground;
            GUI.contentColor = previousContent;
            return selected;
        }

        /// <summary>
        ///     Layout PageId dropdown over every PageId asset of the project. Writes the property only when the user
        ///     picks another entry, so multi-object editing keeps differing values.
        /// </summary>
        /// <returns>False when the project has no PageId assets (a help box is shown instead).</returns>
        internal static bool PageIdPopup(SerializedProperty property, GUIContent label)
        {
            PageId[] ids = PageIdEditorCache.GetIds();
            if (ids.Length == 0)
            {
                EditorGUILayout.HelpBox(NoPageIdsMessage, MessageType.Warning);
                return false;
            }

            if (!TryGetPopupIndex(property, out int current))
            {
                EditorGUILayout.PropertyField(property, label);
                return true;
            }

            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup(label, current, PageIdEditorCache.GetLabelContents());
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                Assign(property, ids, next);
            }

            return true;
        }

        /// <summary>Rect PageId dropdown (reorderable list elements); draws nothing when there are no PageId assets.</summary>
        internal static void PageIdPopup(Rect rect, SerializedProperty property)
        {
            PageId[] ids = PageIdEditorCache.GetIds();
            if (ids.Length == 0)
            {
                return;
            }

            if (!TryGetPopupIndex(property, out int current))
            {
                EditorGUI.PropertyField(rect, property, GUIContent.none);
                return;
            }

            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int next = EditorGUI.Popup(rect, current, PageIdEditorCache.GetLabelContents());
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                Assign(property, ids, next);
            }
        }

        private static bool TryGetPopupIndex(SerializedProperty property, out int index)
        {
            PageId current = property.objectReferenceValue as PageId;
            if (current == null)
            {
                index = 0;
                return true;
            }

            // WHY: a PageId outside the cached list (another folder root, just created) must not read as <None>:
            // the dropdown would then clear the reference on the next pick.
            int found = PageIdEditorCache.IndexOf(current);
            index = found + 1;
            return found >= 0;
        }

        private static void Assign(SerializedProperty property, PageId[] ids, int popupIndex)
        {
            if (popupIndex <= 0)
            {
                property.objectReferenceValue = null;
            }
            else if (popupIndex <= ids.Length)
            {
                property.objectReferenceValue = ids[popupIndex - 1];
            }
        }
    }
}
