using Neo.Editor;
using Neo.Rpg.Components;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Rpg
{
    // WHY: RpgCharacter keeps a custom editor because it curates which fields show (advanced networking
    // internals stay hidden) and adds a live runtime summary. It still inherits CustomEditorBase and renders
    // its sections through the shared DrawNeoSection / DrawPropertyFieldNoHeader helpers, so it matches the
    // rest of the package (green foldout bars) and a field's [Header] never doubles the section title.
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

            DrawNeoSection("Template", 2, () =>
            {
                DrawPropertyFieldNoHeader(_template);
                DrawPropertyFieldNoHeader(_applyTemplateOnAwake);
            });

            DrawNeoSection("Resources", 1, () => DrawPropertyFieldNoHeader(_resources));
            DrawNeoSection("Stats", 1, () => DrawPropertyFieldNoHeader(_stats));

            DrawNeoSection("Buffs And Statuses", 3, () =>
            {
                DrawPropertyFieldNoHeader(_knownBuffs);
                DrawPropertyFieldNoHeader(_inlineBuffs);
                DrawPropertyFieldNoHeader(_knownStatuses);
            });

            DrawNeoSection("Progression", 2, () =>
            {
                DrawPropertyFieldNoHeader(_progression);
                DrawPropertyFieldNoHeader(_levelProvider);
            });

            DrawNeoSection("Persistence", 3, () =>
            {
                DrawPropertyFieldNoHeader(_saveKey);
                DrawPropertyFieldNoHeader(_loadOnAwake);
                DrawPropertyFieldNoHeader(_autoSave);
            });

            DrawNeoSection("Network", 2, () =>
            {
                DrawPropertyFieldNoHeader(_isNetworked);
                DrawPropertyFieldNoHeader(_authorityMode);
                EditorGUILayout.HelpBox(
                    "When isNetworked is enabled, the public API routes client-only mutations to the server and syncs a full resource/stat/effect snapshot back to clients.",
                    MessageType.Info);
            });

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

        private void DrawEventsSection()
        {
            var events = new System.Collections.Generic.List<SerializedProperty>();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name.StartsWith("_on"))
                {
                    events.Add(iterator.Copy());
                }
            }

            if (events.Count == 0)
            {
                return;
            }

            DrawNeoSection("Events", events.Count, () =>
            {
                for (int i = 0; i < events.Count; i++)
                {
                    DrawPropertyFieldNoHeader(events[i]);
                }
            });
        }
    }
}
