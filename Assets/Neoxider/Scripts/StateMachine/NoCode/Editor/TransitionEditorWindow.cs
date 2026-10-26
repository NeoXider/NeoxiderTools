using System;
using System.Linq;
using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.StateMachine.NoCode.Editor
{
    /// <summary>
    ///     Editor window for editing a State Machine transition.
    ///     Opened from the inspector or via code.
    /// </summary>
    public class TransitionEditorWindow : EditorWindow
    {
        private StateMachineData data;
        private bool requiresOwner;
        private int transitionIndex = -1;
        private Vector2 scrollPosition;
        private StateTransition transition;

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            // Inline transitions have no persistent identity. Undo may replace or reorder them;
            // require a fresh selection if the original object no longer belongs to the asset.
            if (!TryGetCurrentTransition())
            {
                transition = null;
                transitionIndex = -1;
            }

            Repaint();
        }

        private void RestoreTransition()
        {
            if (data != null)
            {
                transition = data.Transitions != null && transitionIndex >= 0 &&
                             transitionIndex < data.Transitions.Count
                    ? data.Transitions[transitionIndex]
                    : null;
            }
        }

        private bool TryGetCurrentTransition()
        {
            if (this == null || transition == null)
            {
                return false;
            }

            if (data == null)
            {
                return !requiresOwner;
            }

            int index = data.Transitions?.IndexOf(transition) ?? -1;
            if (index < 0)
            {
                return false;
            }

            transitionIndex = index;
            return true;
        }

        private void OnGUI()
        {
            if (!TryGetCurrentTransition())
            {
                EditorGUILayout.HelpBox("No transition selected, or the transition was removed from its asset.",
                    MessageType.Warning);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawSummary();

            NeoxiderEditorGUI.BeginSection("Transition Information",
                "Core transition properties and its place in the priority ordering.");
            EditorGUI.BeginChangeCheck();
            string transitionName = EditorGUILayout.TextField("Name", transition.TransitionName);
            bool isEnabled = EditorGUILayout.Toggle("Enabled", transition.IsEnabled);
            int priority = EditorGUILayout.IntField("Priority", transition.Priority);
            if (EditorGUI.EndChangeCheck())
            {
                SetTransitionDetails(transitionName, isEnabled, priority);
            }

            NeoxiderEditorGUI.DrawCaption(
                "Tip: short, readable transition names make debugging and graph-style configs much easier.");
            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(6f);

            NeoxiderEditorGUI.BeginSection("States",
                "Linked states are usually edited from the StateMachineData inspector.");

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("From State", transition.FromStateData, typeof(StateData), false);
            EditorGUILayout.ObjectField("To State", transition.ToStateData, typeof(StateData), false);
            EditorGUI.EndDisabledGroup();

            if (transition.FromStateData != null)
            {
                NeoxiderEditorGUI.DrawKeyValueRow("From State Name", transition.FromStateData.StateName,
                    new Color(0.38f, 0.72f, 1f, 1f));
            }

            if (transition.ToStateData != null)
            {
                NeoxiderEditorGUI.DrawKeyValueRow("To State Name", transition.ToStateData.StateName,
                    new Color(0.42f, 0.86f, 0.58f, 1f));
            }

            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(6f);

            NeoxiderEditorGUI.BeginSection("Conditions (Predicates)",
                "Conditions decide whether the transition may fire. A lean list is easier to read than a crowded one.");

            if (transition.Predicates == null || transition.Predicates.Count == 0)
            {
                EditorGUILayout.HelpBox("No conditions. This transition will always be available.", MessageType.Info);
            }
            else
            {
                int removeIndex = -1;
                for (int i = 0; i < transition.Predicates.Count; i++)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"{i + 1}. {GetPredicateTitle(transition.Predicates[i])}",
                        EditorStyles.boldLabel);

                    StatePredicate predicate = transition.Predicates[i];
                    if (predicate != null)
                    {
                        NeoxiderEditorGUI.DrawKeyValueRow("Type", predicate.GetType().Name,
                            new Color(0.70f, 0.62f, 1f, 1f));
                        NeoxiderEditorGUI.DrawKeyValueRow("Name", predicate.PredicateName);
                        DrawPredicateFields(i, predicate);
                        NeoxiderEditorGUI.DrawCaption(
                            predicate.IsInverted
                                ? "This predicate is inverted: its result is flipped for the transition."
                                : "This predicate contributes as a normal condition to the transition.");
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Null predicate", MessageType.Warning);
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(100)))
                    {
                        removeIndex = i;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }

                if (removeIndex >= 0)
                {
                    RemovePredicateAt(removeIndex);
                }
            }

            if (GUILayout.Button("Add Condition", GUILayout.Height(30)))
            {
                ShowAddPredicateMenu();
            }

            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Height(30)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        private SerializedProperty GetPredicateProperty(SerializedObject serializedData, int predicateIndex)
        {
            if (!TryGetCurrentTransition() || data == null || serializedData.targetObject != data)
            {
                return null;
            }

            SerializedProperty transitionsProperty = serializedData.FindProperty("transitions");
            if (transitionsProperty == null || transitionIndex >= transitionsProperty.arraySize)
            {
                return null;
            }

            SerializedProperty predicatesProperty = transitionsProperty.GetArrayElementAtIndex(transitionIndex)
                .FindPropertyRelative("predicates");
            return predicatesProperty != null && predicateIndex >= 0 && predicateIndex < predicatesProperty.arraySize
                ? predicatesProperty.GetArrayElementAtIndex(predicateIndex)
                : null;
        }

        private void DrawPredicateFields(int predicateIndex, StatePredicate predicate)
        {
            if (data == null)
            {
                predicate.IsInverted = EditorGUILayout.Toggle("Inverted", predicate.IsInverted);
                return;
            }

            using (SerializedObject serializedData = new(data))
            {
                serializedData.Update();
                SerializedProperty predicateProperty = GetPredicateProperty(serializedData, predicateIndex);
                if (predicateProperty != null)
                {
                    EditorGUILayout.PropertyField(predicateProperty, new GUIContent("Settings"), true);
                    serializedData.ApplyModifiedProperties();
                    RestoreTransition();
                }
            }
        }

        private void SetTransitionDetails(string transitionName, bool isEnabled, int priority)
        {
            if (!TryGetCurrentTransition())
            {
                return;
            }

            if (data != null)
            {
                Undo.RecordObject(data, "Edit State Machine Transition");
            }

            transition.TransitionName = transitionName;
            transition.IsEnabled = isEnabled;
            transition.Priority = priority;
            if (data != null)
            {
                EditorUtility.SetDirty(data);
            }
        }

        private void RemovePredicateAt(int predicateIndex)
        {
            if (!TryGetCurrentTransition() || transition.Predicates == null ||
                predicateIndex < 0 || predicateIndex >= transition.Predicates.Count)
            {
                return;
            }

            if (data != null)
            {
                Undo.RegisterCompleteObjectUndo(data, "Remove Transition Condition");
            }

            transition.Predicates.RemoveAt(predicateIndex);
            if (data != null)
            {
                EditorUtility.SetDirty(data);
            }
        }

        private void DrawSummary()
        {
            string title = string.IsNullOrWhiteSpace(transition.TransitionName)
                ? "Unnamed Transition"
                : transition.TransitionName;
            string from = transition.FromStateData != null ? transition.FromStateData.StateName : "None";
            string to = transition.ToStateData != null ? transition.ToStateData.StateName : "None";

            NeoxiderEditorGUI.DrawSummaryCard(title,
                $"Route: <b>{from}</b> → <b>{to}</b>",
                new NeoxiderEditorGUI.Badge(transition.IsEnabled ? "Enabled" : "Disabled",
                    transition.IsEnabled ? new Color(0.18f, 0.62f, 0.32f, 1f) : new Color(0.46f, 0.46f, 0.50f, 1f)),
                new NeoxiderEditorGUI.Badge($"Priority {transition.Priority}", new Color(0.42f, 0.34f, 0.82f, 1f)),
                new NeoxiderEditorGUI.Badge($"Predicates {transition.Predicates?.Count ?? 0}",
                    new Color(0.20f, 0.50f, 0.78f, 1f)));

            EditorGUILayout.Space(4f);
        }

        private static string GetPredicateTitle(StatePredicate predicate)
        {
            if (predicate == null)
            {
                return "Null Predicate";
            }

            if (!string.IsNullOrWhiteSpace(predicate.PredicateName))
            {
                return predicate.PredicateName;
            }

            return predicate.GetType().Name;
        }

        /// <summary>
        ///     Opens the transition editor window.
        /// </summary>
        /// <param name="transition">Transition to edit.</param>
        /// <param name="data">Owning StateMachineData asset.</param>
        public static void ShowWindow(StateTransition transition, StateMachineData data)
        {
            TransitionEditorWindow window = GetWindow<TransitionEditorWindow>("Edit Transition");
            window.SetTransition(transition, data);
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void SetTransition(StateTransition selectedTransition, StateMachineData owner)
        {
            transition = selectedTransition;
            data = owner;
            requiresOwner = owner is not null;
            transitionIndex = data != null ? data.Transitions?.IndexOf(transition) ?? -1 : -1;
        }

        private void ShowAddPredicateMenu()
        {
            GenericMenu menu = new();
            Type[] predicateTypes = TypeCache.GetTypesDerivedFrom<StatePredicate>()
                .Where(t => t.IsDefined(typeof(SerializableAttribute), false) && !t.IsAbstract &&
                            !t.IsGenericType && !t.ContainsGenericParameters &&
                            t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(GetPredicateOrder)
                .ThenBy(t => t.Name)
                .ToArray();

            if (predicateTypes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No predicate types available"));
            }
            else
            {
                foreach (Type type in predicateTypes)
                {
                    string menuPath = GetPredicateMenuPath(type);
                    menu.AddItem(new GUIContent(menuPath), false, () => AddPredicate(type));
                }
            }

            menu.ShowAsContext();
        }

        private void AddPredicate(Type predicateType)
        {
            if (!TryGetCurrentTransition() || predicateType == null ||
                !typeof(StatePredicate).IsAssignableFrom(predicateType) ||
                !predicateType.IsDefined(typeof(SerializableAttribute), false) ||
                predicateType.IsAbstract || predicateType.IsGenericType || predicateType.ContainsGenericParameters ||
                predicateType.GetConstructor(Type.EmptyTypes) == null)
            {
                return;
            }

            StatePredicate predicate;
            try
            {
                predicate = (StatePredicate)Activator.CreateInstance(predicateType);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[TransitionEditorWindow] Cannot create {predicateType.Name}: {exception.GetBaseException().Message}",
                    data);
                return;
            }

            if (string.IsNullOrWhiteSpace(predicate.PredicateName) || predicate.PredicateName == "Unnamed Predicate")
            {
                predicate.PredicateName = predicateType.Name;
            }

            if (data != null)
            {
                Undo.RegisterCompleteObjectUndo(data, "Add Transition Condition");
            }

            transition.AddPredicate(predicate);

            if (data != null)
            {
                EditorUtility.SetDirty(data);
            }
        }

        private static int GetPredicateOrder(Type type)
        {
            if (type == typeof(ConditionEntryPredicate))
            {
                return 0;
            }

            return 1;
        }

        private static string GetPredicateMenuPath(Type type)
        {
            if (type == typeof(ConditionEntryPredicate))
            {
                return "Neoxider/Condition Entry";
            }

            return $"Built-in/{type.Name}";
        }
    }
}
