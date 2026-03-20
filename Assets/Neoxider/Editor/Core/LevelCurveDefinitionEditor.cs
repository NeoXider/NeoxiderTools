using Neo.Core.Level;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Core
{
    [CustomEditor(typeof(LevelCurveDefinition))]
    [CanEditMultipleObjects]
    public sealed class LevelCurveDefinitionEditor : UnityEditor.Editor
    {
        private const string PreviewCountKey = "Neoxider.LevelCurveDefinition.PreviewLevelCount";
        private const int PreviewCountDefault = 10;
        private const int PreviewCountMin = 1;
        private const int PreviewCountMax = 100;
        private SerializedProperty _animationCurve;
        private SerializedProperty _constantOffset;
        private SerializedProperty _customEntries;
        private SerializedProperty _expBase;
        private SerializedProperty _expFactor;
        private SerializedProperty _formulaType;

        private SerializedProperty _mode;
        private SerializedProperty _powerBase;
        private SerializedProperty _powerExponent;
        private SerializedProperty _quadraticBase;
        private SerializedProperty _xpPerLevel;

        private void OnEnable()
        {
            _mode = serializedObject.FindProperty("_mode");
            _formulaType = serializedObject.FindProperty("_formulaType");
            _xpPerLevel = serializedObject.FindProperty("_xpPerLevel");
            _constantOffset = serializedObject.FindProperty("_constantOffset");
            _quadraticBase = serializedObject.FindProperty("_quadraticBase");
            _expBase = serializedObject.FindProperty("_expBase");
            _expFactor = serializedObject.FindProperty("_expFactor");
            _powerBase = serializedObject.FindProperty("_powerBase");
            _powerExponent = serializedObject.FindProperty("_powerExponent");
            _animationCurve = serializedObject.FindProperty("_animationCurve");
            _customEntries = serializedObject.FindProperty("_customEntries");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_mode);
            EditorGUILayout.Space(4f);

            var mode = (LevelCurveMode)_mode.enumValueIndex;

            switch (mode)
            {
                case LevelCurveMode.Formula:
                    EditorGUILayout.LabelField("Формула", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_formulaType);
                    int formulaTypeIndex = _formulaType.enumValueIndex;
                    bool isLinearOrOffset = formulaTypeIndex == (int)LevelFormulaType.Linear ||
                                            formulaTypeIndex == (int)LevelFormulaType.LinearWithOffset;
                    if (isLinearOrOffset)
                    {
                        EditorGUILayout.PropertyField(_xpPerLevel);
                    }

                    if (formulaTypeIndex == (int)LevelFormulaType.LinearWithOffset)
                    {
                        EditorGUILayout.PropertyField(_constantOffset);
                    }

                    if (formulaTypeIndex == (int)LevelFormulaType.Quadratic)
                    {
                        EditorGUILayout.PropertyField(_quadraticBase);
                    }

                    if (formulaTypeIndex == (int)LevelFormulaType.Exponential)
                    {
                        EditorGUILayout.PropertyField(_expBase);
                        EditorGUILayout.PropertyField(_expFactor);
                    }

                    if (formulaTypeIndex == (int)LevelFormulaType.Power ||
                        formulaTypeIndex == (int)LevelFormulaType.PolynomialSingle)
                    {
                        EditorGUILayout.PropertyField(_powerBase);
                        EditorGUILayout.PropertyField(_powerExponent);
                    }

                    break;
                case LevelCurveMode.Curve:
                    EditorGUILayout.LabelField("Кривая (X = уровень, Y = кумулятивный XP)", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_animationCurve, true);
                    break;
                case LevelCurveMode.Custom:
                    EditorGUILayout.LabelField("Ручная таблица (уровень → требуемый XP)", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_customEntries, true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();

            DrawLevelsPreview(mode);
        }

        private void DrawLevelsPreview(LevelCurveMode mode)
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                return;
            }

            var definition = (LevelCurveDefinition)target;
            int previewCount = EditorPrefs.GetInt(PreviewCountKey, PreviewCountDefault);
            previewCount = Mathf.Clamp(previewCount, PreviewCountMin, PreviewCountMax);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Превью уровней", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Уровней в превью");
            int newCount = EditorGUILayout.IntSlider(previewCount, PreviewCountMin, PreviewCountMax);
            if (newCount != previewCount)
            {
                EditorPrefs.SetInt(PreviewCountKey, newCount);
            }

            EditorGUILayout.EndHorizontal();

            Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(new Rect(headerRect.x, headerRect.y, 60f, headerRect.height), "Уровень");
            EditorGUI.LabelField(new Rect(headerRect.x + 70f, headerRect.y, headerRect.width - 70f, headerRect.height),
                "Кумулятивный XP");

            for (int level = 1; level <= previewCount; level++)
            {
                Rect lineRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                int requiredXp = definition.GetRequiredXpForLevel(level);
                EditorGUI.LabelField(new Rect(lineRect.x, lineRect.y, 60f, lineRect.height), level.ToString());
                EditorGUI.LabelField(new Rect(lineRect.x + 70f, lineRect.y, lineRect.width - 70f, lineRect.height),
                    requiredXp.ToString());
            }
        }
    }
}
