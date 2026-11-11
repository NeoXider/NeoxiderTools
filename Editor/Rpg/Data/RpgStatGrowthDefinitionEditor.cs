using Neo.Rpg;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Rpg
{
    [CustomEditor(typeof(RpgStatGrowthDefinition))]
    [CanEditMultipleObjects]
    public sealed class RpgStatGrowthDefinitionEditor : UnityEditor.Editor
    {
        private const string PreviewCountKey = "Neoxider.RpgStatGrowthDefinition.PreviewLevelCount";
        private const int PreviewCountDefault = 10;
        private const int PreviewCountMin = 1;
        private const int PreviewCountMax = 100;

        private SerializedProperty _maxHp;
        private SerializedProperty _hpRegen;
        private SerializedProperty _damagePercent;
        private SerializedProperty _defensePercent;
        private SerializedProperty _xpReward;

        // WHY: EditorPrefs is registry-backed on Windows and was read on every inspector event; the count is
        // read once per domain and written only when the slider moves.
        private static int? s_previewCount;

        // WHY: five curves were evaluated and formatted per row on every repaint; rows are kept until the asset
        // changes or the row count changes. Each row holds Hp, Regen, Dmg, Def and XP text.
        private string[] _previewCells = System.Array.Empty<string>();
        private int _previewRows = -1;
        private int _previewDirtyCount = -1;
        private RpgStatGrowthDefinition _previewSource;

        private const int PreviewColumns = 5;

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
            _maxHp = serializedObject.FindProperty("MaxHp");
            _hpRegen = serializedObject.FindProperty("HpRegen");
            _damagePercent = serializedObject.FindProperty("DamagePercent");
            _defensePercent = serializedObject.FindProperty("DefensePercent");
            _xpReward = serializedObject.FindProperty("XpReward");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Primary Resource Growth", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_maxHp);
            EditorGUILayout.PropertyField(_hpRegen);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Combat Modifiers Growth", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_damagePercent);
            EditorGUILayout.PropertyField(_defensePercent);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Progression Growth", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_xpReward);

            serializedObject.ApplyModifiedProperties();

            DrawLevelsPreview();
        }

        private void DrawLevelsPreview()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                return;
            }

            RpgStatGrowthDefinition definition = (RpgStatGrowthDefinition)target;
            int previewCount = PreviewCount;

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Level Preview", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Levels in preview");
            int newCount = EditorGUILayout.IntSlider(previewCount, PreviewCountMin, PreviewCountMax);
            if (newCount != previewCount)
            {
                PreviewCount = newCount;
            }

            EditorGUILayout.EndHorizontal();
            RefreshPreview(definition, previewCount);

            EditorGUILayout.Space(4f);

            Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            float colWidth = headerRect.width / 6f; // WHY: Reduced from 5 to 6 for XP column

            EditorGUI.LabelField(new Rect(headerRect.x, headerRect.y, colWidth * 0.5f, headerRect.height), "Lvl",
                EditorStyles.boldLabel);
            EditorGUI.LabelField(
                new Rect(headerRect.x + colWidth * 0.5f, headerRect.y, colWidth * 1.0f, headerRect.height), "Hp",
                EditorStyles.boldLabel);
            EditorGUI.LabelField(
                new Rect(headerRect.x + colWidth * 1.5f, headerRect.y, colWidth * 1.0f, headerRect.height), "Reg",
                EditorStyles.boldLabel);
            EditorGUI.LabelField(
                new Rect(headerRect.x + colWidth * 2.5f, headerRect.y, colWidth * 1.0f, headerRect.height), "Dmg",
                EditorStyles.boldLabel);
            EditorGUI.LabelField(
                new Rect(headerRect.x + colWidth * 3.5f, headerRect.y, colWidth * 1.0f, headerRect.height), "Def",
                EditorStyles.boldLabel);
            EditorGUI.LabelField(
                new Rect(headerRect.x + colWidth * 4.5f, headerRect.y, colWidth * 1.5f, headerRect.height), "XP Rew",
                EditorStyles.boldLabel);

            for (int level = 1; level <= previewCount; level++)
            {
                Rect lineRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                int cell = (level - 1) * PreviewColumns;

                EditorGUI.LabelField(new Rect(lineRect.x, lineRect.y, colWidth * 0.5f, lineRect.height),
                    LevelLabel(level));
                EditorGUI.LabelField(
                    new Rect(lineRect.x + colWidth * 0.5f, lineRect.y, colWidth * 1.0f, lineRect.height),
                    _previewCells[cell]);
                EditorGUI.LabelField(
                    new Rect(lineRect.x + colWidth * 1.5f, lineRect.y, colWidth * 1.0f, lineRect.height),
                    _previewCells[cell + 1]);
                EditorGUI.LabelField(
                    new Rect(lineRect.x + colWidth * 2.5f, lineRect.y, colWidth * 1.0f, lineRect.height),
                    _previewCells[cell + 2]);
                EditorGUI.LabelField(
                    new Rect(lineRect.x + colWidth * 3.5f, lineRect.y, colWidth * 1.0f, lineRect.height),
                    _previewCells[cell + 3]);
                EditorGUI.LabelField(
                    new Rect(lineRect.x + colWidth * 4.5f, lineRect.y, colWidth * 1.5f, lineRect.height),
                    _previewCells[cell + 4]);
            }
        }

        private void RefreshPreview(RpgStatGrowthDefinition definition, int previewCount)
        {
            int dirtyCount = EditorUtility.GetDirtyCount(definition);
            if (ReferenceEquals(definition, _previewSource) && dirtyCount == _previewDirtyCount &&
                _previewRows == previewCount)
            {
                return;
            }

            if (_previewCells.Length != previewCount * PreviewColumns)
            {
                _previewCells = new string[previewCount * PreviewColumns];
            }

            for (int level = 1; level <= previewCount; level++)
            {
                int cell = (level - 1) * PreviewColumns;
                _previewCells[cell] = definition.MaxHp.Evaluate(level).ToString("F0");
                _previewCells[cell + 1] = definition.HpRegen.Evaluate(level).ToString("F1");
                _previewCells[cell + 2] = definition.DamagePercent.Evaluate(level).ToString("F0") + "%";
                _previewCells[cell + 3] = definition.DefensePercent.Evaluate(level).ToString("F0") + "%";
                _previewCells[cell + 4] = definition.XpReward.Evaluate(level).ToString("F0");
            }

            _previewSource = definition;
            _previewDirtyCount = dirtyCount;
            _previewRows = previewCount;
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
