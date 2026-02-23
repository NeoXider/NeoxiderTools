using System;
using System.Linq;
using Neo.StateMachine;
using UnityEditor;
using UnityEngine;

namespace Neo.StateMachine.NoCode.Editor
{
    /// <summary>
    ///     Окно редактора для редактирования переходов State Machine.
    ///     Открывается из инспектора или программно.
    /// </summary>
    public class TransitionEditorWindow : EditorWindow
    {
        private StateMachineData data;
        private Vector2 scrollPosition;
        private StateTransition transition;

        private void OnGUI()
        {
            if (transition == null)
            {
                EditorGUILayout.HelpBox("No transition selected.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Основная информация о переходе
            EditorGUILayout.LabelField("Transition Information", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            transition.TransitionName = EditorGUILayout.TextField("Name", transition.TransitionName);
            transition.IsEnabled = EditorGUILayout.Toggle("Enabled", transition.IsEnabled);
            transition.Priority = EditorGUILayout.IntField("Priority", transition.Priority);

            EditorGUILayout.Space(10);

            // Информация о состояниях
            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("From State", transition.FromStateData, typeof(StateData), false);
            EditorGUILayout.ObjectField("To State", transition.ToStateData, typeof(StateData), false);
            EditorGUI.EndDisabledGroup();

            if (transition.FromStateData != null)
            {
                EditorGUILayout.LabelField("From State Name", transition.FromStateData.StateName);
            }

            if (transition.ToStateData != null)
            {
                EditorGUILayout.LabelField("To State Name", transition.ToStateData.StateName);
            }

            EditorGUILayout.Space(10);

            // Предикаты (условия)
            EditorGUILayout.LabelField("Conditions (Predicates)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (transition.Predicates == null || transition.Predicates.Count == 0)
            {
                EditorGUILayout.HelpBox("No conditions. This transition will always be available.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < transition.Predicates.Count; i++)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"Condition {i + 1}", EditorStyles.boldLabel);

                    StatePredicate predicate = transition.Predicates[i];
                    if (predicate != null)
                    {
                        EditorGUILayout.LabelField("Type", predicate.GetType().Name);
                        EditorGUILayout.LabelField("Name", predicate.PredicateName);
                        predicate.IsInverted = EditorGUILayout.Toggle("Inverted", predicate.IsInverted);

                        if (predicate is ConditionEntryPredicate && data != null)
                        {
                            int transIndex = data.Transitions.IndexOf(transition);
                            if (transIndex >= 0)
                            {
                                SerializedObject so = new SerializedObject(data);
                                SerializedProperty transitionsProp = so.FindProperty("transitions");
                                if (transitionsProp != null && transIndex < transitionsProp.arraySize)
                                {
                                    SerializedProperty transEl = transitionsProp.GetArrayElementAtIndex(transIndex);
                                    SerializedProperty predicatesProp = transEl.FindPropertyRelative("predicates");
                                    if (predicatesProp != null && i < predicatesProp.arraySize)
                                    {
                                        SerializedProperty predProp = predicatesProp.GetArrayElementAtIndex(i);
                                        SerializedProperty contextSlotProp = predProp.FindPropertyRelative("contextSlot");
                                        SerializedProperty entryProp = predProp.FindPropertyRelative("conditionEntry");
                                        so.Update();
                                        if (contextSlotProp != null)
                                            EditorGUILayout.PropertyField(contextSlotProp, new GUIContent("Context Slot", "Owner = object with StateMachine; Override1..5 = from Context Overrides on component (set in scene)."));
                                        if (entryProp != null)
                                            EditorGUILayout.PropertyField(entryProp, true);
                                        so.ApplyModifiedProperties();
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Null predicate", MessageType.Warning);
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(100)))
                    {
                        transition.RemovePredicate(predicate);
                        break;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }

            EditorGUILayout.Space(10);

            // Кнопка добавления предиката
            if (GUILayout.Button("Add Condition", GUILayout.Height(30)))
            {
                ShowAddPredicateMenu();
            }

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                if (data != null)
                {
                    EditorUtility.SetDirty(data);
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Close", GUILayout.Height(30)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        ///     Показать окно редактора перехода.
        /// </summary>
        /// <param name="transition">Переход для редактирования.</param>
        /// <param name="data">StateMachineData, содержащий переход.</param>
        public static void ShowWindow(StateTransition transition, StateMachineData data)
        {
            TransitionEditorWindow window = GetWindow<TransitionEditorWindow>("Edit Transition");
            window.transition = transition;
            window.data = data;
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void ShowAddPredicateMenu()
        {
            GenericMenu menu = new();
            Type[] predicateTypes = TypeCache.GetTypesDerivedFrom<StatePredicate>()
                .Where(t => !t.IsAbstract && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
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
            if (predicateType == null)
            {
                return;
            }

            if (Activator.CreateInstance(predicateType) is not StatePredicate predicate)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(predicate.PredicateName) || predicate.PredicateName == "Unnamed Predicate")
            {
                predicate.PredicateName = predicateType.Name;
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