using Neo.Rpg;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Rpg
{
    [CustomPropertyDrawer(typeof(RpgStatGrowthRule))]
    public class RpgStatGrowthRuleDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty mode = property.FindPropertyRelative("Mode");
            SerializedProperty formula = property.FindPropertyRelative("FormulaType");
            
            float height = EditorGUIUtility.singleLineHeight * 2 + 4f; // Label + Mode
            
            if (mode.enumValueIndex == (int)RpgStatGrowthMode.Formula)
            {
                height += EditorGUIUtility.singleLineHeight + 2f; // FormulaType
                height += EditorGUIUtility.singleLineHeight + 2f; // BaseValue
                
                int formulaVal = formula.enumValueIndex;
                if (formulaVal == (int)RpgStatFormulaType.Linear || formulaVal == (int)RpgStatFormulaType.Quadratic)
                {
                    height += EditorGUIUtility.singleLineHeight + 2f; // AddPerLevel
                }
                else if (formulaVal == (int)RpgStatFormulaType.Exponential || formulaVal == (int)RpgStatFormulaType.Power)
                {
                    height += EditorGUIUtility.singleLineHeight + 2f; // MultiplierPerLevel (used as base or exponent)
                }
                // Flat has no extra field
            }
            else if (mode.enumValueIndex == (int)RpgStatGrowthMode.Curve)
            {
                height += EditorGUIUtility.singleLineHeight + 2f; // Curve
            }
            
            return height + 4f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            Rect rect = new Rect(position.x, position.y + 2f, position.width, EditorGUIUtility.singleLineHeight);
            
            // Draw title
            EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
            rect.y += EditorGUIUtility.singleLineHeight + 2f;
            
            // Draw Mode
            SerializedProperty mode = property.FindPropertyRelative("Mode");
            EditorGUI.PropertyField(rect, mode);
            rect.y += EditorGUIUtility.singleLineHeight + 2f;
            
            EditorGUI.indentLevel++;
            
            if (mode.enumValueIndex == (int)RpgStatGrowthMode.Formula)
            {
                SerializedProperty formula = property.FindPropertyRelative("FormulaType");
                EditorGUI.PropertyField(rect, formula);
                rect.y += EditorGUIUtility.singleLineHeight + 2f;
                
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("BaseValue"));
                rect.y += EditorGUIUtility.singleLineHeight + 2f;
                
                int formulaVal = formula.enumValueIndex;
                if (formulaVal == (int)RpgStatFormulaType.Linear || formulaVal == (int)RpgStatFormulaType.Quadratic)
                {
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("AddPerLevel"), new GUIContent(formulaVal == (int)RpgStatFormulaType.Quadratic ? "Quadratic Base (AddPerLevel)" : "Add Per Level"));
                }
                else if (formulaVal == (int)RpgStatFormulaType.Exponential)
                {
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("MultiplierPerLevel"), new GUIContent("Multiplier Per Level"));
                }
                else if (formulaVal == (int)RpgStatFormulaType.Power)
                {
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("AddPerLevel"), new GUIContent("Power Base"));
                    rect.y += EditorGUIUtility.singleLineHeight + 2f;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("MultiplierPerLevel"), new GUIContent("Power Exponent"));
                }
            }
            else if (mode.enumValueIndex == (int)RpgStatGrowthMode.Curve)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("Curve"));
            }
            
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
}
