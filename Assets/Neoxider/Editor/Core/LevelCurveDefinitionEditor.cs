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

        // WHY: EditorPrefs is registry-backed on Windows and the preview count was read on every inspector
        // event; it is read once per domain and written only when the slider moves.
        private static int? s_previewCount;

        // WHY: a Custom table is walked per level, so the preview cost grows with entries x levels. The
        // computed rows are kept until the asset changes or the row count changes.
        private string[] _previewXp = System.Array.Empty<string>();
        private int _previewDirtyCount = -1;
        private LevelCurveDefinition _previewSource;

        private static int PreviewCount
        {
            get
            {
                s_previewCount ??= Mathf.Clamp(EditorPrefs.GetInt(PreviewCountKey, PreviewCountDefault),
                    PreviewCountMin, PreviewCountMax);
                return s_previewCount.Value;
            }
            set
            {
                int clamped = Mathf.Clamp(value, PreviewCountMin, PreviewCountMax);
                if (s_previewCount == clamped)
                {
                    return;
                }

                s_previewCount = clamped;
                EditorPrefs.SetInt(PreviewCountKey, clamped);
            }
        }

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

            LevelCurveMode mode = (LevelCurveMode)_mode.enumValueIndex;

            switch (mode)
            {
                case LevelCurveMode.Formula:
                    EditorGUILayout.LabelField("Formula", EditorStyles.boldLabel);
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
                    EditorGUILayout.LabelField("Curve (X = level, Y = cumulative XP)", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_animationCurve, true);
                    break;
                case LevelCurveMode.Custom:
                    EditorGUILayout.LabelField("Manual table (level → required XP)", EditorStyles.boldLabel);
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

            LevelCurveDefinition definition = (LevelCurveDefinition)target;
            int previewCount = PreviewCount;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Level preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Levels in preview");
            int newCount = EditorGUILayout.IntSlider(previewCount, PreviewCountMin, PreviewCountMax);
            if (newCount != previewCount)
            {
                PreviewCount = newCount;
            }

            EditorGUILayout.EndHorizontal();
            RefreshPreview(definition, previewCount);

            Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(new Rect(headerRect.x, headerRect.y, 60f, headerRect.height), "Level");
            EditorGUI.LabelField(new Rect(headerRect.x + 70f, headerRect.y, headerRect.width - 70f, headerRect.height),
                "Cumulative XP");

            for (int level = 1; level <= previewCount; level++)
            {
                Rect lineRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(new Rect(lineRect.x, lineRect.y, 60f, lineRect.height), LevelLabel(level));
                EditorGUI.LabelField(new Rect(lineRect.x + 70f, lineRect.y, lineRect.width - 70f, lineRect.height),
                    _previewXp[level - 1]);
            }
        }

        private void RefreshPreview(LevelCurveDefinition definition, int previewCount)
        {
            int dirtyCount = EditorUtility.GetDirtyCount(definition);
            if (ReferenceEquals(definition, _previewSource) && dirtyCount == _previewDirtyCount &&
                _previewXp.Length == previewCount)
            {
                return;
            }

            if (_previewXp.Length != previewCount)
            {
                _previewXp = new string[previewCount];
            }

            for (int level = 1; level <= previewCount; level++)
            {
                _previewXp[level - 1] = definition.GetRequiredXpForLevel(level).ToString();
            }

            _previewSource = definition;
            _previewDirtyCount = dirtyCount;
        }

        private static readonly string[] LevelLabels = BuildLevelLabels();

        private static string[] BuildLevelLabels()
        {
            string[] labels = new string[PreviewCountMax];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = (i + 1).ToString();
            }

            return labels;
        }

        private static string LevelLabel(int level)
        {
            return level >= 1 && level <= LevelLabels.Length ? LevelLabels[level - 1] : level.ToString();
        }
    }
}
