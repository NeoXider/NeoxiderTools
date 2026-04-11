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
            if (serializedObject.isEditingMultipleObjects) return;

            var definition = (RpgStatGrowthDefinition)target;
            int previewCount = EditorPrefs.GetInt(PreviewCountKey, PreviewCountDefault);
            previewCount = Mathf.Clamp(previewCount, PreviewCountMin, PreviewCountMax);

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Level Preview", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Levels in preview");
            int newCount = EditorGUILayout.IntSlider(previewCount, PreviewCountMin, PreviewCountMax);
            if (newCount != previewCount)
            {
                EditorPrefs.SetInt(PreviewCountKey, newCount);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);

            // Draw header
            Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            float colWidth = headerRect.width / 6f; // Reduced from 5 to 6 for XP column
            
            EditorGUI.LabelField(new Rect(headerRect.x, headerRect.y, colWidth * 0.5f, headerRect.height), "Lvl", EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(headerRect.x + colWidth * 0.5f, headerRect.y, colWidth * 1.0f, headerRect.height), "Hp", EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(headerRect.x + colWidth * 1.5f, headerRect.y, colWidth * 1.0f, headerRect.height), "Reg", EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(headerRect.x + colWidth * 2.5f, headerRect.y, colWidth * 1.0f, headerRect.height), "Dmg", EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(headerRect.x + colWidth * 3.5f, headerRect.y, colWidth * 1.0f, headerRect.height), "Def", EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(headerRect.x + colWidth * 4.5f, headerRect.y, colWidth * 1.5f, headerRect.height), "XP Rew", EditorStyles.boldLabel);

            // Draw table rows
            for (int level = 1; level <= previewCount; level++)
            {
                Rect lineRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                
                float hp = definition.MaxHp.Evaluate(level);
                float regen = definition.HpRegen.Evaluate(level);
                float dmg = definition.DamagePercent.Evaluate(level);
                float def = definition.DefensePercent.Evaluate(level);
                float xp = definition.XpReward.Evaluate(level);

                EditorGUI.LabelField(new Rect(lineRect.x, lineRect.y, colWidth * 0.5f, lineRect.height), level.ToString());
                EditorGUI.LabelField(new Rect(lineRect.x + colWidth * 0.5f, lineRect.y, colWidth * 1.0f, lineRect.height), hp.ToString("F0"));
                EditorGUI.LabelField(new Rect(lineRect.x + colWidth * 1.5f, lineRect.y, colWidth * 1.0f, lineRect.height), regen.ToString("F1"));
                EditorGUI.LabelField(new Rect(lineRect.x + colWidth * 2.5f, lineRect.y, colWidth * 1.0f, lineRect.height), dmg.ToString("F0") + "%");
                EditorGUI.LabelField(new Rect(lineRect.x + colWidth * 3.5f, lineRect.y, colWidth * 1.0f, lineRect.height), def.ToString("F0") + "%");
                EditorGUI.LabelField(new Rect(lineRect.x + colWidth * 4.5f, lineRect.y, colWidth * 1.5f, lineRect.height), xp.ToString("F0"));
            }
        }
    }
}
