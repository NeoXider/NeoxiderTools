using Neo.Editor;
using Neo.Rpg.Components;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Rpg
{
    [CustomEditor(typeof(RpgCharacter), true)]
    [CanEditMultipleObjects]
    public sealed class RpgCharacterEditor : CustomEditorBase
    {
        private SerializedProperty _template;
        private SerializedProperty _applyTemplateOnAwake;
        private SerializedProperty _resources;
        private SerializedProperty _stats;
        private SerializedProperty _knownBuffs;
        private SerializedProperty _inlineBuffs;
        private SerializedProperty _knownStatuses;
        private SerializedProperty _progression;
        private SerializedProperty _levelProvider;
        private SerializedProperty _saveKey;
        private SerializedProperty _loadOnAwake;
        private SerializedProperty _autoSave;
        private SerializedProperty _authorityMode;
        private SerializedProperty _isNetworked;

        private bool _showTemplate = true;
        private bool _showResources = true;
        private bool _showStats = true;
        private bool _showEffects = true;
        private bool _showProgression = true;
        private bool _showPersistence = true;
        private bool _showNetwork = true;
        private bool _showEvents;

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
            if (target is not MonoBehaviour targetObject)
            {
                return;
            }

            ComponentDrawer.ProcessComponentAttributes(targetObject);
            ResourceDrawer.ProcessResourceAttributes(targetObject);
        }

        private void OnEnable()
        {
            _template = serializedObject.FindProperty("_template");
            _applyTemplateOnAwake = serializedObject.FindProperty("_applyTemplateOnAwake");
            _resources = serializedObject.FindProperty("_resources");
            _stats = serializedObject.FindProperty("_stats");
            _knownBuffs = serializedObject.FindProperty("_knownBuffs");
            _inlineBuffs = serializedObject.FindProperty("_inlineBuffs");
            _knownStatuses = serializedObject.FindProperty("_knownStatuses");
            _progression = serializedObject.FindProperty("_progression");
            _levelProvider = serializedObject.FindProperty("_levelProvider");
            _saveKey = serializedObject.FindProperty("_saveKey");
            _loadOnAwake = serializedObject.FindProperty("_loadOnAwake");
            _autoSave = serializedObject.FindProperty("_autoSave");
            _authorityMode = serializedObject.FindProperty("_authorityMode");
            _isNetworked = serializedObject.FindProperty("isNetworked");
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            DrawRuntimeSummary();
            DrawTemplateSection();
            DrawResourcesSection();
            DrawStatsSection();
            DrawEffectsSection();
            DrawProgressionSection();
            DrawPersistenceSection();
            DrawNetworkSection();
            DrawEventsSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuntimeSummary()
        {
            if (targets.Length != 1 || target is not RpgCharacter character)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("HP", $"{character.HpValue:0.##} / {character.MaxHpValue:0.##}");
                EditorGUILayout.LabelField("Level", character.LevelValue.ToString());
                EditorGUILayout.LabelField("Dead", character.IsDead ? "Yes" : "No");
                EditorGUILayout.LabelField("Resources / Stats",
                    $"{character.Resources.Count} / {character.Stats.Count}");
            }
        }

        private void DrawTemplateSection()
        {
            _showTemplate = EditorGUILayout.Foldout(_showTemplate, "Template", true);
            if (!_showTemplate)
            {
                return;
            }

            DrawPropertyFieldNoHeader(_template);
            DrawPropertyFieldNoHeader(_applyTemplateOnAwake);
            EditorGUILayout.Space(4f);
        }

        private void DrawResourcesSection()
        {
            _showResources = EditorGUILayout.Foldout(_showResources, "Resources", true);
            if (!_showResources)
            {
                return;
            }

            DrawPropertyFieldNoHeader(_resources);
            EditorGUILayout.Space(4f);
        }

        private void DrawStatsSection()
        {
            _showStats = EditorGUILayout.Foldout(_showStats, "Stats", true);
            if (!_showStats)
            {
                return;
            }

            DrawPropertyFieldNoHeader(_stats);
            EditorGUILayout.Space(4f);
        }

        private void DrawEffectsSection()
        {
            _showEffects = EditorGUILayout.Foldout(_showEffects, "Buffs And Statuses", true);
            if (!_showEffects)
            {
                return;
            }

            DrawPropertyFieldNoHeader(_knownBuffs);
            DrawPropertyFieldNoHeader(_inlineBuffs);
            DrawPropertyFieldNoHeader(_knownStatuses);
            EditorGUILayout.Space(4f);
        }

        private void DrawProgressionSection()
        {
            _showProgression = EditorGUILayout.Foldout(_showProgression, "Progression", true);
            if (!_showProgression)
            {
                return;
            }

            DrawPropertyFieldNoHeader(_progression);
            DrawPropertyFieldNoHeader(_levelProvider);
            EditorGUILayout.Space(4f);
        }

        private void DrawPersistenceSection()
        {
            _showPersistence = EditorGUILayout.Foldout(_showPersistence, "Persistence", true);
            if (!_showPersistence)
            {
                return;
            }

            DrawPropertyFieldNoHeader(_saveKey);
            DrawPropertyFieldNoHeader(_loadOnAwake);
            DrawPropertyFieldNoHeader(_autoSave);
            EditorGUILayout.Space(4f);
        }

        private void DrawNetworkSection()
        {
            _showNetwork = EditorGUILayout.Foldout(_showNetwork, "Network", true);
            if (!_showNetwork)
            {
                return;
            }

            DrawPropertyFieldNoHeader(_isNetworked);
            DrawPropertyFieldNoHeader(_authorityMode);
            EditorGUILayout.HelpBox(
                "When isNetworked is enabled, the public API routes client-only mutations to the server and syncs a full resource/stat/effect snapshot back to clients.",
                MessageType.Info);
            EditorGUILayout.Space(4f);
        }

        private void DrawEventsSection()
        {
            _showEvents = EditorGUILayout.Foldout(_showEvents, "Events", true);
            if (!_showEvents)
            {
                return;
            }

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name.StartsWith("_on"))
                {
                    DrawPropertyFieldNoHeader(iterator);
                }
            }
        }
    }
}
