using System;
using System.Collections.Generic;
using Neo.Editor;
using UnityEditor;
using UnityEngine;

namespace Neo.StateMachine.NoCode.Editor
{
    [CustomEditor(typeof(StateData))]
    public class StateDataEditor : CustomEditorBase
    {
        private const int CachedCountTexts = 64;
        private const string OnEnterLabel = "On Enter Actions";
        private const string OnUpdateLabel = "On Update Actions";
        private const string OnExitLabel = "On Exit Actions";

        private static readonly GUIContent StateNameContent = new("State Name");
        private static readonly GUIContent NoActionTypesContent = new("No action types available");

        // WHY: GUILayout.Width/Height and EditorGUILayout.Space build a GUILayoutOption plus its params array on
        // every call, and each action row asks for them on every IMGUI event.
        private static readonly GUILayoutOption[] AddButtonOptions = { GUILayout.Height(22f), GUILayout.Width(140f) };
        private static readonly GUILayoutOption[] RemoveButtonOptions = { GUILayout.Width(70f) };
        private static readonly GUILayoutOption[] SpaceSmallOption = { GUILayout.Height(4f) };
        private static readonly GUILayoutOption[] SpaceLargeOption = { GUILayout.Height(6f) };

        private static readonly string[] CountTexts = new string[CachedCountTexts];
        private static readonly Dictionary<string, string> ShortTypeNames = new(StringComparer.Ordinal);

        // WHY: TypeCache does not change without a domain reload, which also clears these statics, so the type scan
        // and the menu contents are built once per reload instead of on every OnEnable of every inspector.
        private static Type[] s_actionTypes;
        private static GUIContent[] s_actionTypeContents;

        private Type[] actionTypes;

        private NeoxiderEditorGUI.Badge[] _summaryBadges;
        private int _badgeEnter = -1;
        private int _badgeUpdate = -1;
        private int _badgeExit = -1;
        private int _badgeTypes = -1;

        protected override bool UseCustomNeoxiderInspectorGUI => true;

        private void OnEnable()
        {
            actionTypes = GetActionTypes();
        }

        /// <summary>
        ///     Concrete <see cref="StateAction" /> types that the Add menu offers (serializable, non-abstract,
        ///     non-generic, with a public parameterless constructor), sorted by name. Built once per domain reload.
        /// </summary>
        internal static Type[] GetActionTypes()
        {
            if (s_actionTypes != null)
            {
                return s_actionTypes;
            }

            List<Type> types = new();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<StateAction>())
            {
                if (IsCreatableActionType(type))
                {
                    types.Add(type);
                }
            }

            types.Sort(CompareByName);
            s_actionTypes = types.ToArray();
            s_actionTypeContents = new GUIContent[s_actionTypes.Length];
            for (int i = 0; i < s_actionTypes.Length; i++)
            {
                s_actionTypeContents[i] = new GUIContent(s_actionTypes[i].Name);
            }

            return s_actionTypes;
        }

        private static int CompareByName(Type left, Type right)
        {
            // WHY: the same culture order the menu had with OrderBy(t => t.Name); the full name keeps two types of
            // one name from different namespaces in a stable order.
            int byName = string.Compare(left.Name, right.Name, StringComparison.CurrentCulture);
            return byName != 0 ? byName : string.CompareOrdinal(left.FullName, right.FullName);
        }

        private static bool IsCreatableActionType(Type type)
        {
            return type != null && typeof(StateAction).IsAssignableFrom(type) &&
                   type.IsDefined(typeof(SerializableAttribute), false) && !type.IsAbstract &&
                   !type.IsGenericType && !type.ContainsGenericParameters &&
                   type.GetConstructor(Type.EmptyTypes) != null;
        }

        protected override void ProcessAttributeAssignments()
        {
        }

        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty stateNameProp = serializedObject.FindProperty("stateName");
            SerializedProperty onEnterProp = serializedObject.FindProperty("onEnterActions");
            SerializedProperty onUpdateProp = serializedObject.FindProperty("onUpdateActions");
            SerializedProperty onExitProp = serializedObject.FindProperty("onExitActions");

            DrawSummary(stateNameProp, onEnterProp, onUpdateProp, onExitProp);

            NeoxiderEditorGUI.BeginSection("Identity", "State name and base asset configuration.");
            EditorGUILayout.PropertyField(stateNameProp, StateNameContent);
            NeoxiderEditorGUI.EndSection();

            Space(SpaceLargeOption);
            DrawActionList(OnEnterLabel, "Add " + OnEnterLabel, onEnterProp, "Run once when entering the state.");
            Space(SpaceLargeOption);
            DrawActionList(OnUpdateLabel, "Add " + OnUpdateLabel, onUpdateProp,
                "Run every frame while the state is active.");
            Space(SpaceLargeOption);
            DrawActionList(OnExitLabel, "Add " + OnExitLabel, onExitProp, "Run when leaving the state.");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSummary(SerializedProperty stateNameProp, SerializedProperty onEnterProp,
            SerializedProperty onUpdateProp, SerializedProperty onExitProp)
        {
            string stateName = stateNameProp.stringValue;
            bool unnamed = string.IsNullOrWhiteSpace(stateName);
            string title = unnamed ? target.name : stateName;

            NeoxiderEditorGUI.DrawSummaryCard(title,
                "State asset with enter, update, and exit action groups. Action types are cached and not recomputed every repaint.",
                GetSummaryBadges(onEnterProp.arraySize, onUpdateProp.arraySize, onExitProp.arraySize,
                    actionTypes?.Length ?? 0));

            if (unnamed)
            {
                EditorGUILayout.HelpBox(
                    "State name is empty. Set a meaningful name so transitions and debugging stay readable.",
                    MessageType.Warning);
            }

            Space(SpaceSmallOption);
        }

        /// <summary>
        ///     Summary badges for the given counts. The array and its texts are rebuilt only when a count changes,
        ///     so an unchanged inspector repaints without allocating.
        /// </summary>
        internal NeoxiderEditorGUI.Badge[] GetSummaryBadges(int enterCount, int updateCount, int exitCount,
            int actionTypeCount)
        {
            if (_summaryBadges != null && enterCount == _badgeEnter && updateCount == _badgeUpdate &&
                exitCount == _badgeExit && actionTypeCount == _badgeTypes)
            {
                return _summaryBadges;
            }

            _badgeEnter = enterCount;
            _badgeUpdate = updateCount;
            _badgeExit = exitCount;
            _badgeTypes = actionTypeCount;
            _summaryBadges = new NeoxiderEditorGUI.Badge[]
            {
                new NeoxiderEditorGUI.Badge("Enter " + enterCount, new Color(0.18f, 0.62f, 0.32f, 1f)),
                new NeoxiderEditorGUI.Badge("Update " + updateCount, new Color(0.20f, 0.50f, 0.78f, 1f)),
                new NeoxiderEditorGUI.Badge("Exit " + exitCount, new Color(0.78f, 0.46f, 0.18f, 1f)),
                new NeoxiderEditorGUI.Badge("Action Types " + actionTypeCount, new Color(0.42f, 0.34f, 0.82f, 1f))
            };
            return _summaryBadges;
        }

        internal static string GetCountText(int count)
        {
            if (count < 0 || count >= CachedCountTexts)
            {
                return "Count: " + count;
            }

            return CountTexts[count] ??= "Count: " + count;
        }

        private static void Space(GUILayoutOption[] heightOption)
        {
            GUILayoutUtility.GetRect(0f, 0f, heightOption);
        }

        private void DrawActionList(string label, string addLabel, SerializedProperty listProperty, string subtitle)
        {
            if (listProperty == null)
            {
                EditorGUILayout.HelpBox("Property for '" + label + "' not found.", MessageType.Error);
                return;
            }

            NeoxiderEditorGUI.BeginSection(label, subtitle);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(GetCountText(listProperty.arraySize), EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(addLabel, AddButtonOptions))
                {
                    ShowAddActionMenu(listProperty);
                }
            }

            if (listProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("List is empty. Leave it that way if this lifecycle stage is unused.",
                    MessageType.Info);
            }

            int removeIndex = -1;
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(GetManagedReferenceTypeName(element), EditorStyles.miniBoldLabel);
                if (GUILayout.Button("Remove", RemoveButtonOptions))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(element, GUIContent.none, true);
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                listProperty.DeleteArrayElementAtIndex(removeIndex);
            }

            NeoxiderEditorGUI.EndSection();
        }

        private void ShowAddActionMenu(SerializedProperty listProperty)
        {
            GenericMenu menu = new();
            if (actionTypes == null || actionTypes.Length == 0)
            {
                menu.AddDisabledItem(NoActionTypesContent);
                menu.ShowAsContext();
                return;
            }

            string propertyPath = listProperty.propertyPath;
            for (int i = 0; i < actionTypes.Length; i++)
            {
                Type actionType = actionTypes[i];
                menu.AddItem(GetActionTypeContent(actionType, i), false, () => AddAction(propertyPath, actionType));
            }

            menu.ShowAsContext();
        }

        private static GUIContent GetActionTypeContent(Type actionType, int index)
        {
            // WHY: actionTypes normally is the shared static list, whose contents are cached; a list set from
            // elsewhere (tests) still gets correct labels.
            if (s_actionTypeContents != null && index < s_actionTypeContents.Length &&
                ReferenceEquals(s_actionTypes[index], actionType))
            {
                return s_actionTypeContents[index];
            }

            return new GUIContent(actionType.Name);
        }

        private void AddAction(string propertyPath, Type actionType)
        {
            if (this == null || target == null || !IsCreatableActionType(actionType))
            {
                return;
            }

            serializedObject.Update();
            SerializedProperty listProperty = serializedObject.FindProperty(propertyPath);
            if (listProperty == null || !listProperty.isArray)
            {
                return;
            }

            StateAction action;
            try
            {
                action = (StateAction)Activator.CreateInstance(actionType);
            }
            catch (Exception exception)
            {
                // WHY: answers an explicit Add Action click, so it must not be silenced by the runtime error gate.
                NeoDiagnostics.LogError(
                    "[StateDataEditor] Cannot create " + actionType.Name + ": " +
                    exception.GetBaseException().Message, target, true);
                return;
            }

            int index = listProperty.arraySize;
            listProperty.InsertArrayElementAtIndex(index);
            SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
            element.managedReferenceValue = action;
            serializedObject.ApplyModifiedProperties();
        }

        private static string GetManagedReferenceTypeName(SerializedProperty property)
        {
            string fullTypename = property?.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(fullTypename))
            {
                return "Action";
            }

            // WHY: one row per action asks on every IMGUI event; the split and substring run once per type name.
            if (!ShortTypeNames.TryGetValue(fullTypename, out string shortName))
            {
                shortName = ToShortTypeName(fullTypename);
                ShortTypeNames[fullTypename] = shortName;
            }

            return shortName;
        }

        /// <summary>
        ///     Short class name of a <c>managedReferenceFullTypename</c> ("Assembly Namespace.Type"), or "Action" when
        ///     the text does not have that shape.
        /// </summary>
        internal static string ToShortTypeName(string managedReferenceFullTypename)
        {
            if (string.IsNullOrEmpty(managedReferenceFullTypename))
            {
                return "Action";
            }

            string[] split = managedReferenceFullTypename.Split(' ');
            if (split.Length != 2)
            {
                return "Action";
            }

            string fullTypeName = split[1];
            int lastDot = fullTypeName.LastIndexOf('.');
            return lastDot >= 0 ? fullTypeName[(lastDot + 1)..] : fullTypeName;
        }
    }
}
