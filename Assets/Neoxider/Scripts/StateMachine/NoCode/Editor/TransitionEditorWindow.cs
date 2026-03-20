using System;
using System.Linq;
using Neo.Editor;
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

            DrawSummary();

            NeoxiderEditorGUI.BeginSection("Transition Information",
                "Базовые свойства перехода и его место в приоритетной очереди.");
            transition.TransitionName = EditorGUILayout.TextField("Name", transition.TransitionName);
            transition.IsEnabled = EditorGUILayout.Toggle("Enabled", transition.IsEnabled);
            transition.Priority = EditorGUILayout.IntField("Priority", transition.Priority);
            NeoxiderEditorGUI.DrawCaption(
                "Совет: короткие читаемые имена переходов сильно упрощают отладку и чтение graph-like конфигурации.");
            NeoxiderEditorGUI.EndSection();

            EditorGUILayout.Space(6f);

            NeoxiderEditorGUI.BeginSection("States",
                "Связанные состояния меняются в основном из инспектора StateMachineData.");

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
                "Условия определяют, может ли переход выполниться. Чем чище этот список, тем проще читать поведение state machine.");

            if (transition.Predicates == null || transition.Predicates.Count == 0)
            {
                EditorGUILayout.HelpBox("No conditions. This transition will always be available.", MessageType.Info);
            }
            else
            {
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
                        predicate.IsInverted = EditorGUILayout.Toggle("Inverted", predicate.IsInverted);
                        NeoxiderEditorGUI.DrawCaption(
                            predicate.IsInverted
                                ? "Этот предикат инвертирован: результат будет трактоваться наоборот."
                                : "Предикат участвует в вычислении перехода как обычное условие.");

                        if (predicate is ConditionEntryPredicate && data != null)
                        {
                            int transIndex = data.Transitions.IndexOf(transition);
                            if (transIndex >= 0)
                            {
                                SerializedObject so = new(data);
                                SerializedProperty transitionsProp = so.FindProperty("transitions");
                                if (transitionsProp != null && transIndex < transitionsProp.arraySize)
                                {
                                    SerializedProperty transEl = transitionsProp.GetArrayElementAtIndex(transIndex);
                                    SerializedProperty predicatesProp = transEl.FindPropertyRelative("predicates");
                                    if (predicatesProp != null && i < predicatesProp.arraySize)
                                    {
                                        SerializedProperty predProp = predicatesProp.GetArrayElementAtIndex(i);
                                        SerializedProperty contextSlotProp =
                                            predProp.FindPropertyRelative("contextSlot");
                                        SerializedProperty entryProp = predProp.FindPropertyRelative("conditionEntry");
                                        so.Update();
                                        if (contextSlotProp != null)
                                        {
                                            EditorGUILayout.PropertyField(contextSlotProp,
                                                new GUIContent("Context Slot",
                                                    "Owner = object with StateMachine; Override1..5 = from Context Overrides on component (set in scene)."));
                                        }

                                        if (entryProp != null)
                                        {
                                            EditorGUILayout.PropertyField(entryProp, true);
                                        }

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

            if (GUILayout.Button("Add Condition", GUILayout.Height(30)))
            {
                ShowAddPredicateMenu();
            }

            NeoxiderEditorGUI.EndSection();

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

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Height(30)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
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
