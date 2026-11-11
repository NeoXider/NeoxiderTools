using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.StateMachine.NoCode.Editor
{
    /// <summary>
    ///     Custom inspector for StateMachineData.
    ///     Visual layout for states and transitions.
    /// </summary>
    [CustomEditor(typeof(StateMachineData))]
    [CanEditMultipleObjects]
    public class StateMachineDataEditor : CustomEditorBase
    {
        private const int CachedRowTexts = 64;

        private static readonly GUIContent InitialStateContent = new("Initial State (StateData)");
        private static readonly GUIContent NameContent = new("Name");
        private static readonly GUIContent FromStateContent = new("From State");
        private static readonly GUIContent ToStateContent = new("To State");
        private static readonly GUIContent PriorityContent = new("Priority");
        private static readonly GUIContent IsEnabledContent = new("Is Enabled");

        // WHY: GUILayout.Width/Height and EditorGUILayout.Space build a GUILayoutOption plus its params array on
        // every call; the transition rows ask for the same few options on every IMGUI event.
        private static readonly GUILayoutOption[] ButtonHeightOption = { GUILayout.Height(24f) };
        private static readonly GUILayoutOption[] PredicateLabelWidthOption = { GUILayout.Width(90f) };
        private static readonly GUILayoutOption[] RemoveButtonWidthOption = { GUILayout.Width(24f) };
        private static readonly GUILayoutOption[] MoveButtonWidthOption = { GUILayout.Width(48f) };
        private static readonly GUILayoutOption[] SpaceSmallOption = { GUILayout.Height(4f) };
        private static readonly GUILayoutOption[] SpaceLargeOption = { GUILayout.Height(6f) };

        private static readonly string[] TransitionTitles = new string[CachedRowTexts];
        private static readonly string[] PredicateCountTexts = new string[CachedRowTexts];

        private StateMachineData data;

        private NeoxiderEditorGUI.Badge[] _summaryBadges;
        private int _badgeStateCount = -1;
        private int _badgeTransitionCount = -1;
        private bool _badgeInitialSet;
        private bool _badgeLegacyName;

        private string _legacyNameWarningSource;
        private string _legacyNameWarning;

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        private void OnEnable()
        {
            data = target as StateMachineData;
        }

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            bool isValid = ValidateSelectedConfigurations(true);
            DrawSummary(isValid);

            DrawStatesSection();

            Space(SpaceLargeOption);

            DrawInitialStateSection();

            Space(SpaceLargeOption);

            DrawTransitionsSection();

            Space(SpaceLargeOption);

            NeoxiderEditorGUI.BeginSection("Validation", "Run a manual validation pass and show the result.");
            if (GUILayout.Button("Validate Configuration", ButtonHeightOption))
            {
                serializedObject.ApplyModifiedProperties();
                bool validationResult = ValidateSelectedConfigurations(false);
                if (validationResult)
                {
                    EditorUtility.DisplayDialog("Validation", "State Machine configuration is valid!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation",
                        "State Machine configuration has errors. Check the console.", "OK");
                }
            }

            NeoxiderEditorGUI.EndSection();

            serializedObject.ApplyModifiedProperties();
        }

        private bool ValidateSelectedConfigurations(bool silent)
        {
            bool isValid = true;
            foreach (Object selectedTarget in targets)
            {
                if (selectedTarget is StateMachineData selectedData)
                {
                    isValid &= selectedData.Validate(silent);
                }
            }

            return isValid;
        }

        private void DrawSummary(bool isValid)
        {
            SerializedProperty statesProp = serializedObject.FindProperty("states");
            SerializedProperty transitionsProp = serializedObject.FindProperty("transitions");
            SerializedProperty initialStateProp = serializedObject.FindProperty("initialState");
            SerializedProperty legacyInitialStateNameProp = serializedObject.FindProperty("initialStateName");

            string title = data != null ? data.name : "State Machine";
            string subtitle = isValid
                ? "Configuration looks valid. Edit states, initial state, and transitions below."
                : "There are issues in this configuration. The inspector surfaces them before play mode.";

            bool initialSet = initialStateProp.objectReferenceValue != null;
            bool legacyName = !initialSet && !string.IsNullOrEmpty(legacyInitialStateNameProp.stringValue);
            NeoxiderEditorGUI.DrawSummaryCard(title, subtitle,
                GetSummaryBadges(statesProp.arraySize, transitionsProp.arraySize, initialSet, legacyName));

            if (!isValid)
            {
                EditorGUILayout.HelpBox(
                    "State Machine configuration has errors or missing links. Check states, initial state, and transitions.",
                    MessageType.Warning);
            }

            if (statesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("States array is empty. The state machine cannot start without states.",
                    MessageType.Info);
            }

            Space(SpaceSmallOption);
        }

        /// <summary>
        ///     Summary badges for the given counts and flags. The array and its texts are rebuilt only when one of
        ///     them changes, so an unchanged inspector repaints without allocating.
        /// </summary>
        internal NeoxiderEditorGUI.Badge[] GetSummaryBadges(int stateCount, int transitionCount, bool initialSet,
            bool legacyName)
        {
            if (_summaryBadges != null && stateCount == _badgeStateCount &&
                transitionCount == _badgeTransitionCount && initialSet == _badgeInitialSet &&
                legacyName == _badgeLegacyName)
            {
                return _summaryBadges;
            }

            _badgeStateCount = stateCount;
            _badgeTransitionCount = transitionCount;
            _badgeInitialSet = initialSet;
            _badgeLegacyName = legacyName;
            _summaryBadges = new NeoxiderEditorGUI.Badge[legacyName ? 4 : 3];
            _summaryBadges[0] = new NeoxiderEditorGUI.Badge("States " + stateCount,
                new Color(0.20f, 0.50f, 0.78f, 1f));
            _summaryBadges[1] = new NeoxiderEditorGUI.Badge("Transitions " + transitionCount,
                new Color(0.42f, 0.34f, 0.82f, 1f));
            _summaryBadges[2] = new NeoxiderEditorGUI.Badge(initialSet ? "Initial Set" : "Initial Missing",
                initialSet ? new Color(0.18f, 0.62f, 0.32f, 1f) : new Color(0.78f, 0.46f, 0.18f, 1f));
            if (legacyName)
            {
                _summaryBadges[3] =
                    new NeoxiderEditorGUI.Badge("Legacy Initial Name", new Color(0.65f, 0.56f, 0.18f, 1f));
            }

            return _summaryBadges;
        }

        internal static string GetTransitionTitle(int index)
        {
            if (index < 0 || index >= CachedRowTexts)
            {
                return "Transition " + (index + 1);
            }

            return TransitionTitles[index] ??= "Transition " + (index + 1);
        }

        internal static string GetPredicateCountText(int count)
        {
            if (count < 0 || count >= CachedRowTexts)
            {
                return "Predicates: " + count;
            }

            return PredicateCountTexts[count] ??= "Predicates: " + count;
        }

        private static void Space(GUILayoutOption[] heightOption)
        {
            GUILayoutUtility.GetRect(0f, 0f, heightOption);
        }

        private void DrawStatesSection()
        {
            SerializedProperty statesProp = serializedObject.FindProperty("states");
            NeoxiderEditorGUI.BeginSection("States", "All states available to this machine.");
            EditorGUILayout.PropertyField(statesProp, true);
            NeoxiderEditorGUI.EndSection();
        }

        private void DrawInitialStateSection()
        {
            SerializedProperty initialStateProp = serializedObject.FindProperty("initialState");
            SerializedProperty initialStateNameProp = serializedObject.FindProperty("initialStateName");

            NeoxiderEditorGUI.BeginSection("Initial State",
                "Prefer assigning a StateData reference; keep the legacy name field only for backward compatibility.");
            EditorGUILayout.PropertyField(initialStateProp, InitialStateContent);

            if (initialStateProp.objectReferenceValue == null &&
                !string.IsNullOrEmpty(initialStateNameProp.stringValue))
            {
                EditorGUILayout.HelpBox(GetLegacyNameWarning(initialStateNameProp.stringValue), MessageType.Warning);
            }

            NeoxiderEditorGUI.EndSection();
        }

        private string GetLegacyNameWarning(string legacyName)
        {
            if (_legacyNameWarning == null || !string.Equals(_legacyNameWarningSource, legacyName))
            {
                _legacyNameWarningSource = legacyName;
                _legacyNameWarning = "Using legacy initial state name: " + legacyName +
                                     ". Please assign a StateData object instead.";
            }

            return _legacyNameWarning;
        }

        private void DrawTransitionsSection()
        {
            SerializedProperty transitionsProp = serializedObject.FindProperty("transitions");
            NeoxiderEditorGUI.BeginSection("Transitions",
                "Transitions between states. Use clear names and valid references to state assets.");
            if (transitionsProp == null)
            {
                EditorGUILayout.HelpBox("Transitions property is missing.", MessageType.Error);
                NeoxiderEditorGUI.EndSection();
                return;
            }

            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("Select one StateMachineData asset to edit its conditions.", MessageType.Info);
            }

            int removeIndex = -1;
            int moveUpIndex = -1;
            int moveDownIndex = -1;

            for (int i = 0; i < transitionsProp.arraySize; i++)
            {
                SerializedProperty transitionProp = transitionsProp.GetArrayElementAtIndex(i);
                SerializedProperty fromProp = transitionProp.FindPropertyRelative("fromStateData");
                SerializedProperty toProp = transitionProp.FindPropertyRelative("toStateData");
                SerializedProperty priorityProp = transitionProp.FindPropertyRelative("priority");
                SerializedProperty enabledProp = transitionProp.FindPropertyRelative("isEnabled");
                SerializedProperty nameProp = transitionProp.FindPropertyRelative("transitionName");
                SerializedProperty predicatesProp = transitionProp.FindPropertyRelative("predicates");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                string transitionName = nameProp?.stringValue;
                string transitionTitle = !string.IsNullOrWhiteSpace(transitionName)
                    ? transitionName
                    : GetTransitionTitle(i);
                EditorGUILayout.LabelField(transitionTitle, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(GetPredicateCountText(predicatesProp?.arraySize ?? 0),
                    EditorStyles.miniBoldLabel, PredicateLabelWidthOption);
                if (GUILayout.Button("X", RemoveButtonWidthOption))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();

                if (nameProp != null)
                {
                    EditorGUILayout.PropertyField(nameProp, NameContent);
                }

                if (fromProp != null)
                {
                    EditorGUILayout.PropertyField(fromProp, FromStateContent);
                }

                if (toProp != null)
                {
                    EditorGUILayout.PropertyField(toProp, ToStateContent);
                }

                if (priorityProp != null)
                {
                    EditorGUILayout.PropertyField(priorityProp, PriorityContent);
                }

                if (enabledProp != null)
                {
                    EditorGUILayout.PropertyField(enabledProp, IsEnabledContent);
                }

                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(serializedObject.isEditingMultipleObjects))
                {
                    if (GUILayout.Button("Edit Conditions"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        if (i < data.Transitions.Count && data.Transitions[i] != null)
                        {
                            TransitionEditorWindow.ShowWindow(data.Transitions[i], data);
                        }

                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button("Up", MoveButtonWidthOption))
                {
                    moveUpIndex = i;
                }

                if (GUILayout.Button("Down", MoveButtonWidthOption))
                {
                    moveDownIndex = i;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                Space(SpaceSmallOption);
            }

            if (removeIndex >= 0)
            {
                transitionsProp.DeleteArrayElementAtIndex(removeIndex);
            }
            else if (moveUpIndex > 0)
            {
                transitionsProp.MoveArrayElement(moveUpIndex, moveUpIndex - 1);
            }
            else if (moveDownIndex >= 0 && moveDownIndex < transitionsProp.arraySize - 1)
            {
                transitionsProp.MoveArrayElement(moveDownIndex, moveDownIndex + 1);
            }

            if (GUILayout.Button("Add Transition", ButtonHeightOption))
            {
                int index = transitionsProp.arraySize;
                transitionsProp.InsertArrayElementAtIndex(index);
                SerializedProperty newTransition = transitionsProp.GetArrayElementAtIndex(index);
                SerializedProperty fromProp = newTransition.FindPropertyRelative("fromStateData");
                SerializedProperty toProp = newTransition.FindPropertyRelative("toStateData");
                SerializedProperty enabledProp = newTransition.FindPropertyRelative("isEnabled");
                SerializedProperty nameProp = newTransition.FindPropertyRelative("transitionName");
                SerializedProperty priorityProp = newTransition.FindPropertyRelative("priority");
                SerializedProperty predicatesProp = newTransition.FindPropertyRelative("predicates");
                if (fromProp != null)
                {
                    fromProp.objectReferenceValue = null;
                }

                if (toProp != null)
                {
                    toProp.objectReferenceValue = null;
                }

                if (enabledProp != null)
                {
                    enabledProp.boolValue = true;
                }

                if (priorityProp != null)
                {
                    priorityProp.intValue = 0;
                }

                if (nameProp != null)
                {
                    nameProp.stringValue = "New Transition";
                }

                if (predicatesProp != null)
                {
                    predicatesProp.ClearArray();
                }
            }

            NeoxiderEditorGUI.EndSection();
        }
    }
}
