using UnityEngine;
using UnityEditor;
using Neo.StateMachine;
using Neo.StateMachine.NoCode;

namespace Neo.StateMachine.NoCode.Editor
{
    /// <summary>
    ///     Окно редактора для редактирования переходов State Machine.
    ///     Открывается из инспектора или программно.
    /// </summary>
    public class TransitionEditorWindow : EditorWindow
    {
        private StateTransition transition;
        private StateMachineData data;
        private Vector2 scrollPosition;

        /// <summary>
        ///     Показать окно редактора перехода.
        /// </summary>
        /// <param name="transition">Переход для редактирования.</param>
        /// <param name="data">StateMachineData, содержащий переход.</param>
        public static void ShowWindow(StateTransition transition, StateMachineData data)
        {
            var window = GetWindow<TransitionEditorWindow>("Edit Transition");
            window.transition = transition;
            window.data = data;
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

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
                    
                    var predicate = transition.Predicates[i];
                    if (predicate != null)
                    {
                        EditorGUILayout.LabelField("Type", predicate.GetType().Name);
                        EditorGUILayout.LabelField("Name", predicate.PredicateName);
                        predicate.IsInverted = EditorGUILayout.Toggle("Inverted", predicate.IsInverted);
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

        private void ShowAddPredicateMenu()
        {
            GenericMenu menu = new GenericMenu();

            // Добавляем типы предикатов, которые можно создать
            // TODO: Использовать рефлексию для автоматического поиска всех типов StatePredicate
            menu.AddItem(new GUIContent("Float Comparison"), false, () =>
            {
                // TODO: Создать FloatComparisonPredicate
                Debug.LogWarning("[TransitionEditorWindow] FloatComparisonPredicate creation not yet implemented.");
            });

            menu.AddItem(new GUIContent("Bool Comparison"), false, () =>
            {
                // TODO: Создать BoolComparisonPredicate
                Debug.LogWarning("[TransitionEditorWindow] BoolComparisonPredicate creation not yet implemented.");
            });

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Custom Predicate..."), false, () =>
            {
                Debug.LogWarning("[TransitionEditorWindow] Custom predicate creation not yet implemented.");
            });

            menu.ShowAsContext();
        }
    }
}

