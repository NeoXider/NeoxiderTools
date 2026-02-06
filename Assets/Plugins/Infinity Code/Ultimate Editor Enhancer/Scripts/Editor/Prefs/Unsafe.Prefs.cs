/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.IO;
using InfinityCode.UltimateEditorEnhancer.Interceptors;
using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer
{
    public static partial class Prefs
    {
        public static bool _changeNumberFieldValueByArrow = true;
        public static bool _expandLongTextFields = true;
        public static bool _hierarchyTypeFilter = true;
        public static bool _improveCurveEditor = true;
        public static bool _searchInEnumFields = true;
        public static bool _unsafeFeatures = true;
        public static bool longTextFieldsInVisualScripting = false;
        public static int searchInEnumFieldsMinValues = 10;
        public static bool _treeControllerCollapse = true;

        private static int hasUnsafeBlock = -1; // -1 - unknown, 0 - no block, 1 - has block

        public static bool changeNumberFieldValueByArrow
        {
            get => _changeNumberFieldValueByArrow && unsafeFeatures;
        }

        public static bool expandLongTextFields
        {
            get => _expandLongTextFields && unsafeFeatures;
        }

        public static bool hierarchyTypeFilter
        {
            get => _hierarchyTypeFilter && unsafeFeatures;
        }

        public static bool improveCurveEditor
        {
            get => _improveCurveEditor && unsafeFeatures;
        }

        public static bool searchInEnumFields
        {
            get => _searchInEnumFields && unsafeFeatures;
        }
        
        public static bool treeControllerCollapse
        {
            get => _treeControllerCollapse && unsafeFeatures;
        }

        public static bool unsafeFeatures
        {
            get
            {
                if (hasUnsafeBlock == -1)
                {
                    hasUnsafeBlock = File.Exists("UEENoUnsafe.txt") ? 1 : 0;
                }
                return _unsafeFeatures && hasUnsafeBlock == 0;
            }
        }

        public class UnsafeManager: StandalonePrefManager<UnsafeManager>, IStateablePref
        {
            public override IEnumerable<string> keywords
            {
                get
                {
                    return new[]
                    {
                        "Unsafe",
                        "Change Number Fields Value By Arrows",
                        "Hierarchy Type Filter",
                        "Improve Curve Editor",
                        "Search In Enum Fields",
                        "Tree Controller Collapse"
                    };
                }
            }

            private void DrawToggleField(string label, ref bool value, Action OnChange)
            {
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.ToggleLeft(label, value);
                if (EditorGUI.EndChangeCheck() && OnChange != null) OnChange();
            }

            public override void Draw()
            {
                EditorGUI.BeginChangeCheck();

                _unsafeFeatures = EditorGUILayout.ToggleLeft("Unsafe Features", _unsafeFeatures);

                if (EditorGUI.EndChangeCheck()) RefreshFeatures();

                EditorGUI.BeginDisabledGroup(!_unsafeFeatures);

                DrawToggleField("Change Number Fields Value By Arrows And Mouse Wheel", ref _changeNumberFieldValueByArrow, NumberFieldInterceptor.Refresh);
                
                _expandLongTextFields = EditorGUILayout.ToggleLeft("Expand Long Text Fields", _expandLongTextFields);
                EditorGUI.indentLevel++;
                longTextFieldsInVisualScripting = EditorGUILayout.ToggleLeft("Long Text Fields In Visual Scripting", longTextFieldsInVisualScripting);
                EditorGUI.indentLevel--;


                DrawToggleField("Hierarchy Type Filter", ref _hierarchyTypeFilter, HierarchyToolbarInterceptor.Refresh);
                _improveCurveEditor = EditorGUILayout.ToggleLeft("Improve Curve Editor", _improveCurveEditor);
                DrawToggleField("Search In Enum Fields", ref _searchInEnumFields, EnumPopupInterceptor.Refresh);

                if (_searchInEnumFields)
                {
                    EditorGUI.indentLevel++;
                    searchInEnumFieldsMinValues = EditorGUILayout.IntField("Min Values", searchInEnumFieldsMinValues);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.EndDisabledGroup();
                
                DrawToggleField("Tree Controller Collapse", ref _treeControllerCollapse, TreeViewControllerUserInputChangedExpandedState.Refresh);
            }

            public string GetMenuName()
            {
                return "Unsafe";
            }

            private static void RefreshFeatures()
            {
                EnumPopupInterceptor.Refresh();
                HierarchyToolbarInterceptor.Refresh();
                NumberFieldInterceptor.Refresh();
            }

            public void SetState(bool state)
            {
                _unsafeFeatures = state;
                RefreshFeatures();
            }
        }
    }
}