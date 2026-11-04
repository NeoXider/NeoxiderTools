using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Neo.Condition;
using Neo.Editor.Binding;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using ValueType = Neo.Condition.ValueType;

namespace Neo.Editor.Condition
{
    [CustomEditor(typeof(NeoCondition))]
    [CanEditMultipleObjects]
    public sealed class NeoConditionEditor : CustomEditorBase
    {
        private static readonly HashSet<Type> SupportedTypes = new()
        {
            typeof(int), typeof(float), typeof(double),
            typeof(bool), typeof(string),
            typeof(long), typeof(short), typeof(byte)
        };

        private static readonly string[] CompareOpDisplayNames =
        {
            "==  Equal",
            "!=  Not Equal",
            ">   Greater",
            "<   Less",
            ">=  Greater Or Equal",
            "<=  Less Or Equal"
        };

        private static readonly string[] CompareOpShortNames =
        {
            "==", "!=", ">", "<", ">=", "<="
        };

        private static readonly GameObjectPropertyDef[] GameObjectProperties =
        {
            new() { Name = "activeSelf", Type = ValueType.Bool, DisplayName = "activeSelf  (bool) — object enabled" },
            new()
            {
                Name = "activeInHierarchy", Type = ValueType.Bool,
                DisplayName = "activeInHierarchy  (bool) — active in hierarchy"
            },
            new() { Name = "isStatic", Type = ValueType.Bool, DisplayName = "isStatic  (bool) — static" },
            new() { Name = "tag", Type = ValueType.String, DisplayName = "tag  (string) — object tag" },
            new() { Name = "name", Type = ValueType.String, DisplayName = "name  (string) — object name" },
            new() { Name = "layer", Type = ValueType.Int, DisplayName = "layer  (int) — object layer" }
        };

        private static readonly string[] SourceModeNames = { "Component", "GameObject" };

        private static readonly string[] BoolCompareOps = { "==  Equal", "!=  Not Equal" };
        private static readonly string[] BoolThresholdNames = { "true", "false" };

        private static readonly string[] ThresholdSourceNames = { "Constant (number/text)", "Other Object (variable)" };

        private static readonly Color SourceAccent = new(0.28f, 0.62f, 0.98f, 1f);
        private static readonly Color SceneSearchAccent = new(0.26f, 0.82f, 0.52f, 1f);
        private static readonly Color ReadAccent = new(0.26f, 0.84f, 0.86f, 1f);
        private static readonly Color CompareAccent = new(0.70f, 0.42f, 0.98f, 1f);
        private static readonly Color ThresholdAccent = new(1f, 0.64f, 0.22f, 1f);
        private static readonly Color OtherSourceAccent = new(0.96f, 0.46f, 0.70f, 1f);
        private static readonly Color ValueAccent = new(0.34f, 0.90f, 0.50f, 1f);

        private static readonly string[] GameObjectPropertyDisplayNames = BuildGameObjectPropertyDisplayNames();

        private static readonly GUIContent LogicModeContent = new("Logic Mode");
        private static readonly GUIContent CheckModeContent = new("Check Mode");
        private static readonly GUIContent IntervalContent = new("Interval (sec)");

        private static readonly GUIContent UseUnscaledTimeContent = new("Use Unscaled Time",
            "Count the interval in real time, so it keeps running while Time.timeScale is 0 (pause menus).");

        private static readonly GUIContent CheckOnStartContent = new("Check On Start");
        private static readonly GUIContent OnlyOnChangeContent = new("Only On Change");

        private static readonly GUIContent FindByNameContent = new("Find By Name",
            "Find a GameObject by name in the scene (GameObject.Find). Cached while the object exists.");

        private static readonly GUIContent ObjectNameContent =
            new("Object Name", "GameObject name to resolve via GameObject.Find().");

        private static readonly GUIContent WaitForObjectContent = new("Wait For Object",
            "Wait until the object appears in the scene (no warning). Useful for prefabs spawned later.");

        private static readonly GUIContent FindRetryIntervalContent = new("Find Retry Interval (sec)",
            "How often GameObject.Find runs again while the object is still missing. Non-blocking. 0 = retry every check. Default 1 s.");

        private static readonly GUIContent PrefabPreviewContent = new("Prefab Preview",
            "Drag a prefab from Project to configure components/properties before the instance exists.");

        private static readonly GUIContent SourceObjectContent = new("Source Object");

        private static readonly GUIContent CompareWithContent = new("Compare With",
            "Constant: compare to a number or text. Other Object: compare to another object's field/property. If Other Source Object is empty, the same object as on the left is used.");

        private static readonly GUIContent OtherFindByNameContent = new("Other: Find By Name");
        private static readonly GUIContent OtherObjectNameContent = new("Other Object Name");
        private static readonly GUIContent OtherWaitForObjectContent = new("Other: Wait For Object");

        private static readonly GUIContent OtherFindRetryIntervalContent = new("Other: Find Retry Interval (sec)",
            "How often Other GameObject.Find runs while that object is missing. 0 = every check. Default 1 s.");

        private static readonly GUIContent OtherSourceObjectContent = new("Other Source Object",
            "Empty = same object as on the left (compare two fields on one object).");

        private static readonly GUIContent ArgumentIntContent = new("Argument (int)");
        private static readonly GUIContent ArgumentFloatContent = new("Argument (float)");
        private static readonly GUIContent ArgumentStringContent = new("Argument (string)");

        // WHY: every popup below used to copy its option list into a fresh array on each IMGUI event. The
        // member and component lists are buffers reused across calls (IMGUI is single-threaded and these
        // helpers never nest); the arrays handed to the popups come from a cache that only allocates when
        // the option text actually changes.
        private static readonly List<string> ComponentNamesBuffer = new();
        private static readonly List<string> ComponentTypeNamesBuffer = new();
        private static readonly List<string> MemberNamesBuffer = new();
        private static readonly List<ValueType> MemberTypesBuffer = new();
        private static readonly List<string> MemberLabelsBuffer = new();
        private static readonly List<bool> MemberIsMethodBuffer = new();
        private static readonly List<ArgumentKind> MemberArgumentKindsBuffer = new();

        // WHY: the live "Current Value" rows ran GetMethods / GetProperty / GetField on every repaint in Play
        // Mode. Members of a type only change with a domain reload, so lookups are cached per type and name.
        private static readonly Dictionary<(Type, string), MemberInfo> ValueMemberCache = new();
        private static readonly Dictionary<(Type, string, ArgumentKind), MethodInfo> ValueMethodCache = new();
        private static readonly Dictionary<string, PropertyInfo> GameObjectPropertyCache = new(StringComparer.Ordinal);
        private static readonly object[] SingleArgumentBuffer = new object[1];

        private static string[] BuildGameObjectPropertyDisplayNames()
        {
            string[] names = new string[GameObjectProperties.Length];
            for (int i = 0; i < GameObjectProperties.Length; i++)
            {
                names[i] = GameObjectProperties[i].DisplayName;
            }

            return names;
        }

        /// <summary>
        ///     Enables custom UI inside the Neoxider-branded inspector style.
        /// </summary>
        protected override bool UseCustomNeoxiderInspectorGUI => true;

        protected override void ProcessAttributeAssignments()
        {
            if (target is MonoBehaviour mb)
            {
                ComponentDrawer.ProcessComponentAttributes(mb);
                ResourceDrawer.ProcessResourceAttributes(mb);
            }
        }

        /// <summary>
        ///     Draws a custom inspector inside the Neo frame (label, rainbow line, Actions).
        /// </summary>
        protected override void DrawCustomNeoxiderInspectorGUI()
        {
            serializedObject.Update();

            DrawConditionEditorHints();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_logicMode"), LogicModeContent);

            EditorGUILayout.Space(6);

            if (serializedObject.isEditingMultipleObjects)
            {
                // WHY: the condition rows resolve components, members and live values from the first selected
                // object only. Editing them for a mixed selection silently copied that object's choices onto
                // every other one, so the rows are left to single selection.
                EditorGUILayout.HelpBox(
                    "Select a single NeoCondition to edit its conditions. Shared settings below apply to every selected object.",
                    MessageType.Info);
            }
            else
            {
                DrawConditionsList();
            }

            EditorGUILayout.Space(6);

            SerializedProperty checkModeProp = serializedObject.FindProperty("_checkMode");
            EditorGUILayout.PropertyField(checkModeProp, CheckModeContent);

            if (checkModeProp.hasMultipleDifferentValues || checkModeProp.enumValueIndex == (int)CheckMode.Interval)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_checkInterval"), IntervalContent);
                SerializedProperty unscaledTimeProp = serializedObject.FindProperty("_useUnscaledTime");
                if (unscaledTimeProp != null)
                {
                    EditorGUILayout.PropertyField(unscaledTimeProp, UseUnscaledTimeContent);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_checkOnStart"), CheckOnStartContent);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_onlyOnChange"), OnlyOnChangeContent);

            EditorGUILayout.Space(6);

            DrawPlayModeInfo();

            DrawCollapsibleUnityEvents();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///     Warnings only — summary badges UI removed for a cleaner inspector.
        /// </summary>
        private void DrawConditionEditorHints()
        {
            SerializedProperty conditionsProp = serializedObject.FindProperty("_conditions");
            SerializedProperty checkModeProp = serializedObject.FindProperty("_checkMode");

            int sceneSearchCount = 0;
            for (int i = 0; i < conditionsProp.arraySize; i++)
            {
                SerializedProperty entryProp = conditionsProp.GetArrayElementAtIndex(i);
                if (entryProp.FindPropertyRelative("_useSceneSearch").boolValue)
                {
                    sceneSearchCount++;
                }
            }

            if (conditionsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "There are no conditions. Add at least one entry or the component will not evaluate anything.",
                    MessageType.Warning);
            }

            if (sceneSearchCount > 0 && checkModeProp.enumValueIndex == (int)CheckMode.EveryFrame)
            {
                EditorGUILayout.HelpBox(
                    "Scene Search together with Every Frame can be heavy. If you do not need instant per-frame response, prefer Interval or Manual.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawConditionsList()
        {
            SerializedProperty conditionsProp = serializedObject.FindProperty("_conditions");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Conditions ({conditionsProp.arraySize})", Styles.ListHeader);
            GUILayout.FlexibleSpace();

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
            if (GUILayout.Button("+  Add", EditorStyles.miniButton, GUILayout.Width(60), GUILayout.Height(18)))
            {
                conditionsProp.InsertArrayElementAtIndex(conditionsProp.arraySize);
                ResetConditionEntry(conditionsProp.GetArrayElementAtIndex(conditionsProp.arraySize - 1));
            }

            GUI.backgroundColor = oldBg;
            EditorGUILayout.EndHorizontal();

            if (conditionsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No conditions added. Click '+ Add' to create one.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(2);

            for (int i = 0; i < conditionsProp.arraySize; i++)
            {
                SerializedProperty entryProp = conditionsProp.GetArrayElementAtIndex(i);
                DrawConditionEntry(entryProp, i, conditionsProp);
                if (i < conditionsProp.arraySize - 1)
                {
                    EditorGUILayout.Space(2);
                }
            }
        }

        private void DrawConditionEntry(SerializedProperty entryProp, int index, SerializedProperty listProp)
        {
            SerializedProperty sourceModeProp = entryProp.FindPropertyRelative("_sourceMode");
            SerializedProperty useSceneSearchProp = entryProp.FindPropertyRelative("_useSceneSearch");
            SerializedProperty searchNameProp = entryProp.FindPropertyRelative("_searchObjectName");
            SerializedProperty waitForObjectProp = entryProp.FindPropertyRelative("_waitForObject");
            SerializedProperty findRetryIntervalProp = entryProp.FindPropertyRelative("_findRetryIntervalSeconds");
            SerializedProperty prefabPreviewProp = entryProp.FindPropertyRelative("_prefabPreview");
            SerializedProperty sourceObjProp = entryProp.FindPropertyRelative("_sourceObject");
            SerializedProperty compTypeProp = entryProp.FindPropertyRelative("_componentTypeName");
            SerializedProperty compIdxProp = entryProp.FindPropertyRelative("_componentIndex");
            SerializedProperty compOccurrenceProp = entryProp.FindPropertyRelative("_componentOccurrence");
            SerializedProperty propNameProp = entryProp.FindPropertyRelative("_propertyName");
            SerializedProperty valueTypeProp = entryProp.FindPropertyRelative("_valueType");
            SerializedProperty compareOpProp = entryProp.FindPropertyRelative("_compareOp");
            SerializedProperty invertProp = entryProp.FindPropertyRelative("_invert");
            SerializedProperty thresholdSourceProp = entryProp.FindPropertyRelative("_thresholdSource");
            SerializedProperty thresholdIntProp = entryProp.FindPropertyRelative("_thresholdInt");
            SerializedProperty thresholdFloatProp = entryProp.FindPropertyRelative("_thresholdFloat");
            SerializedProperty thresholdBoolProp = entryProp.FindPropertyRelative("_thresholdBool");
            SerializedProperty thresholdStringProp = entryProp.FindPropertyRelative("_thresholdString");
            SerializedProperty otherSourceModeProp = entryProp.FindPropertyRelative("_otherSourceMode");
            SerializedProperty otherUseSceneSearchProp = entryProp.FindPropertyRelative("_otherUseSceneSearch");
            SerializedProperty otherSearchNameProp = entryProp.FindPropertyRelative("_otherSearchObjectName");
            SerializedProperty otherWaitForObjectProp = entryProp.FindPropertyRelative("_otherWaitForObject");
            SerializedProperty otherFindRetryIntervalProp =
                entryProp.FindPropertyRelative("_otherFindRetryIntervalSeconds");
            SerializedProperty otherSourceObjProp = entryProp.FindPropertyRelative("_otherSourceObject");
            SerializedProperty otherCompTypeProp = entryProp.FindPropertyRelative("_otherComponentTypeName");
            SerializedProperty otherCompIdxProp = entryProp.FindPropertyRelative("_otherComponentIndex");
            SerializedProperty otherPropNameProp = entryProp.FindPropertyRelative("_otherPropertyName");
            SerializedProperty isMethodProp = entryProp.FindPropertyRelative("_isMethodWithArgument");
            SerializedProperty argKindProp = entryProp.FindPropertyRelative("_propertyArgumentKind");
            SerializedProperty argIntProp = entryProp.FindPropertyRelative("_propertyArgumentInt");
            SerializedProperty argFloatProp = entryProp.FindPropertyRelative("_propertyArgumentFloat");
            SerializedProperty argStringProp = entryProp.FindPropertyRelative("_propertyArgumentString");
            SerializedProperty otherIsMethodProp = entryProp.FindPropertyRelative("_otherIsMethodWithArgument");
            SerializedProperty otherArgKindProp = entryProp.FindPropertyRelative("_otherPropertyArgumentKind");
            SerializedProperty otherArgIntProp = entryProp.FindPropertyRelative("_otherPropertyArgumentInt");
            SerializedProperty otherArgFloatProp = entryProp.FindPropertyRelative("_otherPropertyArgumentFloat");
            SerializedProperty otherArgStringProp = entryProp.FindPropertyRelative("_otherPropertyArgumentString");

            NeoCondition condition = (NeoCondition)target;
            SerializedProperty logicProp = serializedObject.FindProperty("_logicMode");
            bool isGameObjectMode = sourceModeProp.enumValueIndex == (int)SourceMode.GameObject;
            bool isSceneSearch = useSceneSearchProp.boolValue;

            Color accentNormal;
            if (isSceneSearch)
            {
                accentNormal = new Color(0.4f, 0.9f, 0.5f, 0.6f);
            }
            else if (isGameObjectMode)
            {
                accentNormal = new Color(0.9f, 0.7f, 0.2f, 0.6f);
            }
            else
            {
                accentNormal = new Color(0.3f, 0.7f, 0.9f, 0.6f);
            }

            Rect boxRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (Event.current.type == EventType.Repaint)
            {
                Color accentColor = invertProp.boolValue
                    ? new Color(0.9f, 0.3f, 0.3f, 0.8f)
                    : accentNormal;
                EditorGUI.DrawRect(new Rect(boxRect.x, boxRect.y, 3f, boxRect.height), accentColor);
            }

            EditorGUILayout.BeginHorizontal();

            if (index > 0)
            {
                bool isAnd = logicProp.enumValueIndex == 0;
                string logicLabel = isAnd ? "AND" : "OR";
                GUILayout.Label(logicLabel, Styles.Logic(Styles.LogicColor(isAnd)), GUILayout.Width(32));
            }
            else
            {
                GUILayout.Label("#0", Styles.Index, GUILayout.Width(32));
            }

            string summary = BuildConditionSummary(sourceModeProp, useSceneSearchProp, searchNameProp,
                compTypeProp, propNameProp, compareOpProp, valueTypeProp,
                thresholdSourceProp, thresholdIntProp, thresholdFloatProp, thresholdBoolProp, thresholdStringProp,
                otherSourceModeProp, otherUseSceneSearchProp, otherSearchNameProp, otherCompTypeProp, otherPropNameProp,
                invertProp);
            GUILayout.Label(summary, Styles.Summary);

            GUILayout.FlexibleSpace();

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = invertProp.boolValue ? new Color(1f, 0.4f, 0.4f) : Color.gray;
            EditorGUI.BeginChangeCheck();
            bool inverted =
                GUILayout.Toggle(invertProp.boolValue, "NOT", EditorStyles.miniButton, GUILayout.Width(36));
            if (EditorGUI.EndChangeCheck())
            {
                invertProp.boolValue = inverted;
            }

            GUI.backgroundColor = oldBg;

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                listProp.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = oldBg;
                return;
            }

            GUI.backgroundColor = oldBg;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            GameObject targetObj = null;
            BeginAccentSection("Source Setup", isSceneSearch ? SceneSearchAccent : SourceAccent);

            EditorGUI.BeginChangeCheck();
            int newModeIdx = EditorGUILayout.Popup("Source", sourceModeProp.enumValueIndex, SourceModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                sourceModeProp.enumValueIndex = newModeIdx;
                compTypeProp.stringValue = "";
                compIdxProp.intValue = 0;
                compOccurrenceProp.intValue = 0;
                propNameProp.stringValue = "";
                if (isMethodProp != null)
                {
                    isMethodProp.boolValue = false;
                }
            }

            EditorGUI.BeginChangeCheck();
            bool useSceneSearch = EditorGUILayout.Toggle(FindByNameContent, useSceneSearchProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                useSceneSearchProp.boolValue = useSceneSearch;
                compTypeProp.stringValue = "";
                compIdxProp.intValue = 0;
                compOccurrenceProp.intValue = 0;
                propNameProp.stringValue = "";
                if (isMethodProp != null)
                {
                    isMethodProp.boolValue = false;
                }
            }

            if (isSceneSearch)
            {
                EditorGUILayout.PropertyField(searchNameProp, ObjectNameContent);

                EditorGUI.BeginChangeCheck();
                bool waitForObject = EditorGUILayout.Toggle(WaitForObjectContent, waitForObjectProp.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    waitForObjectProp.boolValue = waitForObject;
                }

                if (findRetryIntervalProp != null)
                {
                    EditorGUILayout.PropertyField(findRetryIntervalProp, FindRetryIntervalContent);
                }

                if (Application.isPlaying && condition != null)
                {
                    IReadOnlyList<ConditionEntry> entries = condition.Conditions;
                    if (index < entries.Count)
                    {
                        ConditionEntry runtimeEntry = entries[index];
                        GameObject found = runtimeEntry?.FoundByNameObject;
                        EditorGUI.BeginDisabledGroup(true);
                        DrawReadOnlyObjectField("Found Object", found);
                        EditorGUI.EndDisabledGroup();

                        if (found != null)
                        {
                            targetObj = found;
                        }
                    }
                }

                if (!Application.isPlaying && !string.IsNullOrEmpty(searchNameProp.stringValue))
                {
                    GameObject found = ComponentBindingInspectorShared.FindSceneObject(searchNameProp.stringValue);
                    if (found != null)
                    {
                        targetObj = found;
                        EditorGUI.BeginDisabledGroup(true);
                        DrawReadOnlyObjectField("Preview", found);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(prefabPreviewProp, PrefabPreviewContent);

                        GameObject prefab = prefabPreviewProp.objectReferenceValue as GameObject;
                        if (prefab != null)
                        {
                            targetObj = prefab;
                            EditorGUILayout.HelpBox(
                                waitForObjectProp.boolValue
                                    ? $"Object \"{searchNameProp.stringValue}\" is not in the scene yet — editing via prefab. Waiting for spawn (no warning)."
                                    : $"Object \"{searchNameProp.stringValue}\" is not in the scene yet — editing via prefab.",
                                MessageType.Info);
                        }
                        else
                        {
                            if (waitForObjectProp.boolValue)
                            {
                                EditorGUILayout.HelpBox(
                                    $"GameObject \"{searchNameProp.stringValue}\" not found — waiting for spawn (no warning).\nDrag a prefab into Prefab Preview to edit properties.",
                                    MessageType.Info);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(
                                    $"GameObject \"{searchNameProp.stringValue}\" not found in the scene.\nDrag a prefab into Prefab Preview to edit properties.",
                                    MessageType.Warning);
                            }
                        }
                    }
                }

                if (targetObj == null && condition != null && !Application.isPlaying)
                {
                    targetObj = null;
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(sourceObjProp, SourceObjectContent);
                if (EditorGUI.EndChangeCheck())
                {
                    compTypeProp.stringValue = "";
                    compIdxProp.intValue = 0;
                    compOccurrenceProp.intValue = 0;
                    propNameProp.stringValue = "";
                    if (isMethodProp != null)
                    {
                        isMethodProp.boolValue = false;
                    }
                }

                targetObj = (GameObject)sourceObjProp.objectReferenceValue;
                if (targetObj == null && condition != null)
                {
                    targetObj = condition.gameObject;
                }
            }

            EndAccentSection();

            if (targetObj != null)
            {
                BeginAccentSection(
                    isGameObjectMode ? "Read Value From GameObject" : "Read Value From Component",
                    ReadAccent);

                if (isGameObjectMode)
                {
                    DrawGameObjectPropertyDropdown(propNameProp, valueTypeProp);

                    if (Application.isPlaying && !string.IsNullOrEmpty(propNameProp.stringValue))
                    {
                        DrawCurrentValueGameObject(targetObj, propNameProp.stringValue);
                    }

                    if (!string.IsNullOrEmpty(propNameProp.stringValue))
                    {
                        DrawCompareAndThresholdOrOther(entryProp, condition, index, targetObj, compareOpProp,
                            valueTypeProp,
                            thresholdSourceProp, thresholdIntProp, thresholdFloatProp, thresholdBoolProp,
                            thresholdStringProp,
                            otherSourceModeProp, otherUseSceneSearchProp, otherSearchNameProp, otherWaitForObjectProp,
                            otherFindRetryIntervalProp,
                            otherSourceObjProp, otherCompTypeProp, otherCompIdxProp, otherPropNameProp);
                    }
                }
                else
                {
                    DrawComponentDropdown(targetObj, compTypeProp, compIdxProp, compOccurrenceProp, propNameProp,
                        isMethodProp);

                    string selectedCompType = compTypeProp.stringValue;
                    if (!string.IsNullOrEmpty(selectedCompType))
                    {
                        Component selectedComp =
                            ComponentBindingInspectorShared.FindComponentByTypeName(targetObj, selectedCompType,
                                compOccurrenceProp.intValue);
                        if (selectedComp != null)
                        {
                            DrawPropertyDropdown(selectedComp, propNameProp, valueTypeProp,
                                isMethodProp, argKindProp, argIntProp, argFloatProp, argStringProp);

                            if (Application.isPlaying && !string.IsNullOrEmpty(propNameProp.stringValue))
                            {
                                DrawCurrentValue(selectedComp, propNameProp, valueTypeProp,
                                    isMethodProp, argKindProp, argIntProp, argFloatProp, argStringProp);
                            }

                            if (!string.IsNullOrEmpty(propNameProp.stringValue))
                            {
                                DrawCompareAndThresholdOrOther(entryProp, condition, index, targetObj, compareOpProp,
                                    valueTypeProp,
                                    thresholdSourceProp, thresholdIntProp, thresholdFloatProp, thresholdBoolProp,
                                    thresholdStringProp,
                                    otherSourceModeProp, otherUseSceneSearchProp, otherSearchNameProp,
                                    otherWaitForObjectProp, otherFindRetryIntervalProp,
                                    otherSourceObjProp, otherCompTypeProp, otherCompIdxProp, otherPropNameProp);
                            }
                        }
                    }
                }

                EndAccentSection();
            }
            else if (!isSceneSearch)
            {
                EditorGUILayout.HelpBox("Source Object not assigned — will check on self.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCompareAndThresholdOrOther(
            SerializedProperty entryProp,
            NeoCondition condition,
            int index,
            GameObject leftTargetObj,
            SerializedProperty compareOpProp,
            SerializedProperty valueTypeProp,
            SerializedProperty thresholdSourceProp,
            SerializedProperty thresholdIntProp,
            SerializedProperty thresholdFloatProp,
            SerializedProperty thresholdBoolProp,
            SerializedProperty thresholdStringProp,
            SerializedProperty otherSourceModeProp,
            SerializedProperty otherUseSceneSearchProp,
            SerializedProperty otherSearchNameProp,
            SerializedProperty otherWaitForObjectProp,
            SerializedProperty otherFindRetryIntervalProp,
            SerializedProperty otherSourceObjProp,
            SerializedProperty otherCompTypeProp,
            SerializedProperty otherCompIdxProp,
            SerializedProperty otherPropNameProp)
        {
            BeginAccentSection("Compare", CompareAccent);
            SerializedProperty otherCompOccurrenceProp = entryProp.FindPropertyRelative("_otherComponentOccurrence");

            EditorGUILayout.PropertyField(thresholdSourceProp, CompareWithContent);

            bool isOtherObject = thresholdSourceProp.enumValueIndex == (int)ThresholdSource.OtherObject;

            if (isOtherObject)
            {
                SerializedProperty otherIsMethodProp = entryProp.FindPropertyRelative("_otherIsMethodWithArgument");
                SerializedProperty otherArgKindProp = entryProp.FindPropertyRelative("_otherPropertyArgumentKind");
                SerializedProperty otherArgIntProp = entryProp.FindPropertyRelative("_otherPropertyArgumentInt");
                SerializedProperty otherArgFloatProp = entryProp.FindPropertyRelative("_otherPropertyArgumentFloat");
                SerializedProperty otherArgStringProp = entryProp.FindPropertyRelative("_otherPropertyArgumentString");

                int currentOp = compareOpProp.enumValueIndex;
                int newOp = EditorGUILayout.Popup("Operator", currentOp, CompareOpDisplayNames);
                if (newOp != currentOp)
                {
                    compareOpProp.enumValueIndex = newOp;
                }

                EditorGUILayout.Space(4);
                BeginAccentSection("Right Side Source", OtherSourceAccent);

                int otherModeIdx =
                    EditorGUILayout.Popup("Other Source", otherSourceModeProp.enumValueIndex, SourceModeNames);
                if (otherModeIdx != otherSourceModeProp.enumValueIndex)
                {
                    otherSourceModeProp.enumValueIndex = otherModeIdx;
                    otherCompTypeProp.stringValue = "";
                    otherPropNameProp.stringValue = "";
                    if (otherIsMethodProp != null)
                    {
                        otherIsMethodProp.boolValue = false;
                    }
                }

                EditorGUI.BeginChangeCheck();
                bool otherUseSceneSearch =
                    EditorGUILayout.Toggle(OtherFindByNameContent, otherUseSceneSearchProp.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    otherUseSceneSearchProp.boolValue = otherUseSceneSearch;
                }

                GameObject otherTargetObj = null;
                if (otherUseSceneSearchProp.boolValue)
                {
                    EditorGUILayout.PropertyField(otherSearchNameProp, OtherObjectNameContent);
                    EditorGUI.BeginChangeCheck();
                    bool otherWaitForObject =
                        EditorGUILayout.Toggle(OtherWaitForObjectContent, otherWaitForObjectProp.boolValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        otherWaitForObjectProp.boolValue = otherWaitForObject;
                    }

                    if (otherFindRetryIntervalProp != null)
                    {
                        EditorGUILayout.PropertyField(otherFindRetryIntervalProp, OtherFindRetryIntervalContent);
                    }

                    if (!string.IsNullOrEmpty(otherSearchNameProp.stringValue))
                    {
                        otherTargetObj = ComponentBindingInspectorShared.FindSceneObject(otherSearchNameProp.stringValue);
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(otherSourceObjProp, OtherSourceObjectContent);
                    otherTargetObj = (GameObject)otherSourceObjProp.objectReferenceValue;
                }

                if (otherTargetObj == null)
                {
                    otherTargetObj = leftTargetObj;
                }

                if (otherTargetObj == null && condition != null)
                {
                    otherTargetObj = condition.gameObject;
                }

                if (otherTargetObj != null)
                {
                    NeoxiderEditorGUI.DrawKeyValueRow("Resolved Object", otherTargetObj.name,
                        Styles.ReadableAccent(new Color(0.76f, 0.82f, 1f, 1f)));

                    bool otherIsGO = otherSourceModeProp.enumValueIndex == (int)SourceMode.GameObject;
                    if (otherIsGO)
                    {
                        DrawGameObjectPropertyDropdown(otherPropNameProp);
                        if (Application.isPlaying && !string.IsNullOrEmpty(otherPropNameProp.stringValue))
                        {
                            DrawCurrentValueGameObject(otherTargetObj, otherPropNameProp.stringValue);
                        }
                    }
                    else
                    {
                        DrawComponentDropdown(otherTargetObj, otherCompTypeProp, otherCompIdxProp,
                            otherCompOccurrenceProp, otherPropNameProp, otherIsMethodProp);
                        string otherCompType = otherCompTypeProp.stringValue;
                        if (!string.IsNullOrEmpty(otherCompType))
                        {
                            Component otherComp =
                                ComponentBindingInspectorShared.FindComponentByTypeName(otherTargetObj, otherCompType,
                                    otherCompOccurrenceProp.intValue);
                            if (otherComp != null)
                            {
                                DrawPropertyDropdown(otherComp, otherPropNameProp, null,
                                    otherIsMethodProp, otherArgKindProp, otherArgIntProp, otherArgFloatProp,
                                    otherArgStringProp);
                                if (Application.isPlaying && !string.IsNullOrEmpty(otherPropNameProp.stringValue))
                                {
                                    DrawCurrentValue(otherComp, otherPropNameProp, valueTypeProp,
                                        otherIsMethodProp, otherArgKindProp, otherArgIntProp, otherArgFloatProp,
                                        otherArgStringProp);
                                }
                            }
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Assign Other Source Object or use Find By Name. If empty — same object as left is used at runtime.",
                        MessageType.Info);
                }

                EndAccentSection();
            }
            else
            {
                DrawCompareAndThreshold(compareOpProp, valueTypeProp,
                    thresholdIntProp, thresholdFloatProp, thresholdBoolProp, thresholdStringProp);
            }

            EndAccentSection();
        }

        private static void DrawCompareAndThreshold(
            SerializedProperty compareOpProp,
            SerializedProperty valueTypeProp,
            SerializedProperty thresholdIntProp,
            SerializedProperty thresholdFloatProp,
            SerializedProperty thresholdBoolProp,
            SerializedProperty thresholdStringProp)
        {
            ValueType vt = (ValueType)valueTypeProp.enumValueIndex;

            if (vt == ValueType.Bool)
            {
                EditorGUILayout.BeginHorizontal();
                int boolOp = compareOpProp.enumValueIndex <= 1 ? compareOpProp.enumValueIndex : 0;
                int newBoolOp = EditorGUILayout.Popup("Compare", boolOp, BoolCompareOps);
                if (newBoolOp != compareOpProp.enumValueIndex)
                {
                    compareOpProp.enumValueIndex = newBoolOp;
                }

                int thresholdIdx = thresholdBoolProp.boolValue ? 0 : 1;
                EditorGUI.BeginChangeCheck();
                int newThresholdIdx = EditorGUILayout.Popup(thresholdIdx, BoolThresholdNames, GUILayout.Width(60));
                if (EditorGUI.EndChangeCheck())
                {
                    thresholdBoolProp.boolValue = newThresholdIdx == 0;
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                int currentOp = compareOpProp.enumValueIndex;
                int newOp = EditorGUILayout.Popup("Compare", currentOp, CompareOpDisplayNames);
                if (newOp != currentOp)
                {
                    compareOpProp.enumValueIndex = newOp;
                }

                EditorGUI.BeginChangeCheck();
                switch (vt)
                {
                    case ValueType.Int:
                        int thresholdInt = EditorGUILayout.IntField(thresholdIntProp.intValue, GUILayout.MinWidth(50));
                        if (EditorGUI.EndChangeCheck())
                        {
                            thresholdIntProp.intValue = thresholdInt;
                        }

                        break;
                    case ValueType.Float:
                        float thresholdFloat =
                            EditorGUILayout.FloatField(thresholdFloatProp.floatValue, GUILayout.MinWidth(50));
                        if (EditorGUI.EndChangeCheck())
                        {
                            thresholdFloatProp.floatValue = thresholdFloat;
                        }

                        break;
                    case ValueType.String:
                        string thresholdString = EditorGUILayout.TextField(thresholdStringProp.stringValue,
                            GUILayout.MinWidth(50));
                        if (EditorGUI.EndChangeCheck())
                        {
                            thresholdStringProp.stringValue = thresholdString;
                        }

                        break;
                    default:
                        EditorGUI.EndChangeCheck();
                        break;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawComponentDropdown(GameObject targetObj, SerializedProperty compTypeProp,
            SerializedProperty compIdxProp, SerializedProperty compOccurrenceProp, SerializedProperty propNameProp,
            SerializedProperty isMethodProp = null)
        {
            List<string> names = ComponentNamesBuffer;
            List<string> typeNames = ComponentTypeNamesBuffer;
            ComponentBindingInspectorShared.BuildComponentPickLists(targetObj, names, typeNames);

            if (names.Count == 0)
            {
                EditorGUILayout.HelpBox("No components found on target object.", MessageType.Warning);
                return;
            }

            int currentIndex = ComponentBindingInspectorShared.IndexOfComponent(typeNames, compTypeProp.stringValue,
                compOccurrenceProp.intValue);

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Component", currentIndex,
                ComponentBindingInspectorShared.GetPopupOptions(targetObj, names));
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex >= 0 && newIndex < typeNames.Count)
                {
                    compTypeProp.stringValue = typeNames[newIndex];
                    compIdxProp.intValue = newIndex;
                    compOccurrenceProp.intValue =
                        ComponentBindingInspectorShared.ComponentOccurrenceAt(typeNames, newIndex);
                    propNameProp.stringValue = "";
                    if (isMethodProp != null)
                    {
                        isMethodProp.boolValue = false;
                    }
                }
            }

            if (currentIndex < 0 && (!string.IsNullOrEmpty(compTypeProp.stringValue) || compOccurrenceProp.intValue < 0))
            {
                EditorGUILayout.HelpBox(
                    $"Component occurrence {compOccurrenceProp.intValue} is unavailable. Pick a component explicitly; no fallback is applied.",
                    MessageType.Warning);
            }
        }

        private void DrawPropertyDropdown(Component comp, SerializedProperty propNameProp,
            SerializedProperty valueTypeProp = null,
            SerializedProperty isMethodProp = null,
            SerializedProperty argKindProp = null,
            SerializedProperty argIntProp = null,
            SerializedProperty argFloatProp = null,
            SerializedProperty argStringProp = null)
        {
            List<string> propertyNames = MemberNamesBuffer;
            List<ValueType> propertyTypes = MemberTypesBuffer;
            List<string> displayNames = MemberLabelsBuffer;
            List<bool> isMethodList = MemberIsMethodBuffer;
            List<ArgumentKind> argumentKinds = MemberArgumentKindsBuffer;

            ComponentBindingInspectorShared.BuildConditionMemberLists(comp, isMethodProp != null, propertyNames,
                propertyTypes, displayNames, isMethodList, argumentKinds);

            if (displayNames.Count == 0)
            {
                EditorGUILayout.HelpBox("No readable int/float/bool/string fields or methods found.", MessageType.Info);
                return;
            }

            string currentPropName = propNameProp.stringValue;
            bool currentIsMethod = isMethodProp != null && isMethodProp.boolValue;
            int currentArgKind = argKindProp != null ? argKindProp.enumValueIndex : 0;
            bool disambiguateMethods = isMethodProp != null;
            int currentIndex = ComponentBindingInspectorShared.FindConditionMemberIndex(propertyNames, isMethodList,
                argumentKinds, currentPropName, currentIsMethod, currentArgKind, disambiguateMethods);

            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Property", currentIndex,
                ComponentBindingInspectorShared.GetConditionMemberLabels(comp.GetType(), isMethodProp != null));
            if (EditorGUI.EndChangeCheck() || (currentIndex == 0 && string.IsNullOrEmpty(currentPropName)))
            {
                if (newIndex >= 0 && newIndex < propertyNames.Count)
                {
                    propNameProp.stringValue = propertyNames[newIndex];
                    if (valueTypeProp != null)
                    {
                        valueTypeProp.enumValueIndex = (int)propertyTypes[newIndex];
                    }

                    if (isMethodProp != null)
                    {
                        isMethodProp.boolValue = isMethodList[newIndex];
                        if (argKindProp != null)
                        {
                            argKindProp.enumValueIndex = (int)argumentKinds[newIndex];
                        }
                    }
                }
            }

            if (isMethodProp == null || !isMethodProp.boolValue)
            {
                return;
            }

            if (argKindProp == null || argIntProp == null)
            {
                return;
            }

            EditorGUI.indentLevel++;
            switch ((ArgumentKind)argKindProp.enumValueIndex)
            {
                case ArgumentKind.Int:
                    EditorGUILayout.PropertyField(argIntProp, ArgumentIntContent);
                    break;
                case ArgumentKind.Float:
                    EditorGUILayout.PropertyField(argFloatProp, ArgumentFloatContent);
                    break;
                case ArgumentKind.String:
                    EditorGUILayout.PropertyField(argStringProp, ArgumentStringContent);
                    break;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawGameObjectPropertyDropdown(SerializedProperty propNameProp,
            SerializedProperty valueTypeProp = null)
        {
            string[] displayNames = GameObjectPropertyDisplayNames;

            int currentIndex = -1;
            string currentPropName = propNameProp.stringValue;
            if (!string.IsNullOrEmpty(currentPropName))
            {
                for (int i = 0; i < GameObjectProperties.Length; i++)
                {
                    if (GameObjectProperties[i].Name == currentPropName)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Property", Mathf.Max(currentIndex, 0), displayNames);
            if (EditorGUI.EndChangeCheck() || currentIndex == -1)
            {
                if (newIndex >= 0 && newIndex < GameObjectProperties.Length)
                {
                    propNameProp.stringValue = GameObjectProperties[newIndex].Name;
                    if (valueTypeProp != null)
                    {
                        valueTypeProp.enumValueIndex = (int)GameObjectProperties[newIndex].Type;
                    }
                }
            }
        }

        private void DrawCurrentValueGameObject(GameObject go, string propertyName)
        {
            if (go == null || string.IsNullOrEmpty(propertyName))
            {
                return;
            }

            object value = null;
            try
            {
                if (!GameObjectPropertyCache.TryGetValue(propertyName, out PropertyInfo prop))
                {
                    prop = typeof(GameObject).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                    GameObjectPropertyCache[propertyName] = prop;
                }

                if (prop != null && prop.CanRead)
                {
                    value = prop.GetValue(go);
                }
            }
            catch
            {
                return;
            }

            if (value != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                DrawReadOnlyValueField("Current Value", value.ToString(), ValueAccent);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawCurrentValue(Component comp, string propertyName)
        {
            if (comp == null || string.IsNullOrEmpty(propertyName))
            {
                return;
            }

            object value = GetCurrentValueFromComponent(comp, propertyName, false, null, null, null, null);
            if (value != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                DrawReadOnlyValueField("Current Value", value.ToString(), ValueAccent);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawCurrentValue(Component comp, SerializedProperty propNameProp, SerializedProperty valueTypeProp,
            SerializedProperty isMethodProp, SerializedProperty argKindProp, SerializedProperty argIntProp,
            SerializedProperty argFloatProp, SerializedProperty argStringProp)
        {
            if (comp == null || propNameProp == null || string.IsNullOrEmpty(propNameProp.stringValue))
            {
                return;
            }

            bool isMethod = isMethodProp != null && isMethodProp.boolValue;
            object value = GetCurrentValueFromComponent(comp, propNameProp.stringValue, isMethod,
                argKindProp, argIntProp, argFloatProp, argStringProp);
            if (value != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                DrawReadOnlyValueField("Current Value", value.ToString(), ValueAccent);
                EditorGUI.EndDisabledGroup();
            }
        }

        private static void BeginAccentSection(string title, Color accent, string subtitle = null)
        {
            Rect rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(accent.r, accent.g, accent.b, 0.06f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height),
                    new Color(accent.r, accent.g, accent.b, 0.92f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f),
                    new Color(accent.r, accent.g, accent.b, 0.30f));
            }

            EditorGUILayout.LabelField(title, Styles.AccentTitle(Styles.ReadableAccent(accent)));

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                EditorGUILayout.LabelField(subtitle, Styles.AccentSubtitle);
            }

            EditorGUILayout.Space(2f);
        }

        private static void EndAccentSection()
        {
            EditorGUILayout.EndVertical();
        }

        private static void DrawReadOnlyObjectField(string label, Object value)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(ValueAccent.r, ValueAccent.g, ValueAccent.b, 0.08f));
            }

            EditorGUI.ObjectField(rect, label, value, typeof(GameObject), true);
        }

        private static void DrawReadOnlyValueField(string label, string value, Color accent)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(accent.r, accent.g, accent.b, 0.08f));
            }

            EditorGUI.TextField(rect, label, value, Styles.ReadOnlyValue);
        }

        internal static object GetCurrentValueFromComponent(Component comp, string propertyName, bool isMethod,
            SerializedProperty argKindProp, SerializedProperty argIntProp, SerializedProperty argFloatProp,
            SerializedProperty argStringProp)
        {
            Type type = comp.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (isMethod && argKindProp != null && argIntProp != null)
            {
                ArgumentKind kind = (ArgumentKind)argKindProp.enumValueIndex;
                object arg = kind switch
                {
                    ArgumentKind.Int => argIntProp.intValue,
                    ArgumentKind.Float => argFloatProp != null ? argFloatProp.floatValue : 0f,
                    ArgumentKind.String => argStringProp != null ? argStringProp.stringValue : "",
                    _ => argIntProp.intValue
                };
                MethodInfo method = FindValueMethod(type, propertyName, kind, flags);
                if (method == null)
                {
                    return null;
                }

                try
                {
                    Type parameterType = method.GetParameters()[0].ParameterType;
                    SingleArgumentBuffer[0] = Convert.ChangeType(arg, parameterType, CultureInfo.InvariantCulture);
                    return method.Invoke(comp, SingleArgumentBuffer);
                }
                catch
                {
                    return null;
                }
                finally
                {
                    SingleArgumentBuffer[0] = null;
                }
            }

            try
            {
                MemberInfo member = FindValueMember(type, propertyName, flags);
                return member switch
                {
                    PropertyInfo prop => prop.GetValue(comp),
                    FieldInfo field => field.GetValue(comp),
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        private static MethodInfo FindValueMethod(Type type, string methodName, ArgumentKind kind, BindingFlags flags)
        {
            (Type, string, ArgumentKind) key = (type, methodName, kind);
            if (ValueMethodCache.TryGetValue(key, out MethodInfo cached))
            {
                return cached;
            }

            MethodInfo found = null;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1 ||
                    ComponentBindingInspectorShared.TryGetConditionArgumentKind(parameters[0].ParameterType) != kind)
                {
                    continue;
                }

                found = method;
                break;
            }

            ValueMethodCache[key] = found;
            return found;
        }

        private static MemberInfo FindValueMember(Type type, string memberName, BindingFlags flags)
        {
            (Type, string) key = (type, memberName);
            if (ValueMemberCache.TryGetValue(key, out MemberInfo cached))
            {
                return cached;
            }

            MemberInfo found = null;
            PropertyInfo prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.CanRead && prop.GetIndexParameters().Length == 0)
            {
                found = prop;
            }
            else
            {
                found = type.GetField(memberName, flags);
            }

            ValueMemberCache[key] = found;
            return found;
        }

        private void DrawPlayModeInfo()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            NeoCondition nc = (NeoCondition)target;

            EditorGUILayout.Space(4);

            bool result = nc.LastResult;
            bool dark = EditorGUIUtility.isProSkin;
            Color resultColor = result
                ? dark ? new Color(0.3f, 1f, 0.4f) : new Color(0.08f, 0.50f, 0.18f)
                : dark ? new Color(1f, 0.35f, 0.35f) : new Color(0.72f, 0.10f, 0.10f);
            Color resultBg = result ? new Color(0.1f, 0.4f, 0.15f, 0.3f) : new Color(0.4f, 0.1f, 0.1f, 0.3f);

            Rect resultRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(resultRect, resultBg);
            }

            GUILayout.Label(result ? "Result: TRUE" : "Result: FALSE", Styles.Result(resultColor),
                GUILayout.Height(28));

            EditorGUILayout.EndHorizontal();

            // WHY: The result is live data, so it keeps refreshing even when decorative animation is off.
            EnsureLiveRepaint();
        }

        private static string BuildConditionSummary(
            SerializedProperty sourceModeProp,
            SerializedProperty useSceneSearchProp,
            SerializedProperty searchNameProp,
            SerializedProperty compTypeProp,
            SerializedProperty propNameProp,
            SerializedProperty compareOpProp,
            SerializedProperty valueTypeProp,
            SerializedProperty thresholdSourceProp,
            SerializedProperty thresholdIntProp,
            SerializedProperty thresholdFloatProp,
            SerializedProperty thresholdBoolProp,
            SerializedProperty thresholdStringProp,
            SerializedProperty otherSourceModeProp,
            SerializedProperty otherUseSceneSearchProp,
            SerializedProperty otherSearchNameProp,
            SerializedProperty otherCompTypeProp,
            SerializedProperty otherPropNameProp,
            SerializedProperty invertProp)
        {
            string propName = propNameProp.stringValue;
            bool isGameObjectMode = sourceModeProp.enumValueIndex == (int)SourceMode.GameObject;
            bool isSceneSearch = useSceneSearchProp.boolValue;
            bool isOtherObject = thresholdSourceProp.enumValueIndex == (int)ThresholdSource.OtherObject;

            string srcPrefix = "";
            if (isSceneSearch && !string.IsNullOrEmpty(searchNameProp.stringValue))
            {
                srcPrefix = $"\"{searchNameProp.stringValue}\".";
            }

            string thresholdStr;
            if (isOtherObject)
            {
                string otherPrefix = otherUseSceneSearchProp.boolValue &&
                                     !string.IsNullOrEmpty(otherSearchNameProp.stringValue)
                    ? $"\"{otherSearchNameProp.stringValue}\"."
                    : "";
                string otherComp = string.IsNullOrEmpty(otherCompTypeProp.stringValue)
                    ? "?"
                    : otherCompTypeProp.stringValue;
                int lastDot = otherComp.LastIndexOf('.');
                string shortOtherComp = lastDot >= 0 ? otherComp.Substring(lastDot + 1) : otherComp;
                string otherProp = string.IsNullOrEmpty(otherPropNameProp.stringValue)
                    ? "?"
                    : otherPropNameProp.stringValue;
                thresholdStr = otherSourceModeProp.enumValueIndex == (int)SourceMode.GameObject
                    ? $"{otherPrefix}GO.{otherProp}"
                    : $"{otherPrefix}{shortOtherComp}.{otherProp}";
            }
            else
            {
                ValueType vt = (ValueType)valueTypeProp.enumValueIndex;
                thresholdStr = FormatThreshold(vt, thresholdIntProp, thresholdFloatProp, thresholdBoolProp,
                    thresholdStringProp);
            }

            int opIdx = compareOpProp.enumValueIndex;
            string opSymbol = opIdx >= 0 && opIdx < CompareOpShortNames.Length ? CompareOpShortNames[opIdx] : "?";
            string prefix = invertProp.boolValue ? "NOT " : "";

            if (isGameObjectMode)
            {
                if (string.IsNullOrEmpty(propName))
                {
                    return isSceneSearch ? $"Find(\"{searchNameProp.stringValue}\") → ?" : "(not configured)";
                }

                return $"{prefix}{srcPrefix}GO.{propName} {opSymbol} {thresholdStr}";
            }

            {
                string compName = compTypeProp.stringValue;
                if (string.IsNullOrEmpty(compName) || string.IsNullOrEmpty(propName))
                {
                    return isSceneSearch ? $"Find(\"{searchNameProp.stringValue}\") → ?" : "(not configured)";
                }

                int lastDot = compName.LastIndexOf('.');
                string shortComp = lastDot >= 0 ? compName.Substring(lastDot + 1) : compName;
                return $"{prefix}{srcPrefix}{shortComp}.{propName} {opSymbol} {thresholdStr}";
            }
        }

        private static string FormatThreshold(ValueType vt,
            SerializedProperty thresholdIntProp,
            SerializedProperty thresholdFloatProp,
            SerializedProperty thresholdBoolProp,
            SerializedProperty thresholdStringProp)
        {
            return vt switch
            {
                ValueType.Int => thresholdIntProp.intValue.ToString(),
                ValueType.Float => thresholdFloatProp.floatValue.ToString("F1"),
                ValueType.Bool => thresholdBoolProp.boolValue ? "true" : "false",
                ValueType.String => $"\"{thresholdStringProp.stringValue}\"",
                _ => "?"
            };
        }

        private static void ResetConditionEntry(SerializedProperty entry)
        {
            entry.FindPropertyRelative("_sourceMode").enumValueIndex = (int)SourceMode.Component;
            entry.FindPropertyRelative("_useSceneSearch").boolValue = false;
            entry.FindPropertyRelative("_searchObjectName").stringValue = "";
            entry.FindPropertyRelative("_waitForObject").boolValue = false;
            entry.FindPropertyRelative("_findRetryIntervalSeconds").floatValue =
                BindingSourceGameObjectResolver.DefaultFindRetryIntervalSeconds;
            entry.FindPropertyRelative("_prefabPreview").objectReferenceValue = null;
            entry.FindPropertyRelative("_sourceObject").objectReferenceValue = null;
            entry.FindPropertyRelative("_componentIndex").intValue = 0;
            entry.FindPropertyRelative("_componentOccurrence").intValue = 0;
            entry.FindPropertyRelative("_componentTypeName").stringValue = "";
            entry.FindPropertyRelative("_propertyName").stringValue = "";
            entry.FindPropertyRelative("_valueType").enumValueIndex = 0;
            entry.FindPropertyRelative("_compareOp").enumValueIndex = 0;
            entry.FindPropertyRelative("_invert").boolValue = false;
            entry.FindPropertyRelative("_thresholdSource").enumValueIndex = (int)ThresholdSource.Constant;
            entry.FindPropertyRelative("_thresholdInt").intValue = 0;
            entry.FindPropertyRelative("_thresholdFloat").floatValue = 0f;
            entry.FindPropertyRelative("_thresholdBool").boolValue = true;
            entry.FindPropertyRelative("_thresholdString").stringValue = "";
            entry.FindPropertyRelative("_isMethodWithArgument").boolValue = false;
            entry.FindPropertyRelative("_propertyArgumentKind").enumValueIndex = 0;
            entry.FindPropertyRelative("_propertyArgumentInt").intValue = 0;
            entry.FindPropertyRelative("_propertyArgumentFloat").floatValue = 0f;
            entry.FindPropertyRelative("_propertyArgumentString").stringValue = "";
            entry.FindPropertyRelative("_otherSourceMode").enumValueIndex = (int)SourceMode.Component;
            entry.FindPropertyRelative("_otherUseSceneSearch").boolValue = false;
            entry.FindPropertyRelative("_otherSearchObjectName").stringValue = "";
            entry.FindPropertyRelative("_otherWaitForObject").boolValue = false;
            entry.FindPropertyRelative("_otherFindRetryIntervalSeconds").floatValue =
                BindingSourceGameObjectResolver.DefaultFindRetryIntervalSeconds;
            entry.FindPropertyRelative("_otherSourceObject").objectReferenceValue = null;
            entry.FindPropertyRelative("_otherComponentIndex").intValue = 0;
            entry.FindPropertyRelative("_otherComponentOccurrence").intValue = 0;
            entry.FindPropertyRelative("_otherComponentTypeName").stringValue = "";
            entry.FindPropertyRelative("_otherPropertyName").stringValue = "";
            entry.FindPropertyRelative("_otherIsMethodWithArgument").boolValue = false;
            entry.FindPropertyRelative("_otherPropertyArgumentKind").enumValueIndex = 0;
            entry.FindPropertyRelative("_otherPropertyArgumentInt").intValue = 0;
            entry.FindPropertyRelative("_otherPropertyArgumentFloat").floatValue = 0f;
            entry.FindPropertyRelative("_otherPropertyArgumentString").stringValue = "";
        }

        private struct GameObjectPropertyDef
        {
            public string Name;
            public ValueType Type;
            public string DisplayName;
        }

        /// <summary>
        ///     Cached text styles. Colours are applied per use and picked for the active editor skin: the old
        ///     inline styles were rebuilt on every IMGUI event and hard-coded near-white text, which vanished on
        ///     the light skin.
        /// </summary>
        private static class Styles
        {
            private static GUIStyle s_listHeader;
            private static GUIStyle s_logic;
            private static GUIStyle s_index;
            private static GUIStyle s_summary;
            private static GUIStyle s_accentTitle;
            private static GUIStyle s_accentSubtitle;
            private static GUIStyle s_readOnlyValue;
            private static GUIStyle s_result;
            private static bool s_builtForProSkin;

            /// <summary>
            ///     Copies of skin styles bake the skin's textures and text colours, so they are rebuilt after the
            ///     editor switches between the light and dark skin.
            /// </summary>
            private static void DropIfSkinChanged()
            {
                if (s_builtForProSkin == EditorGUIUtility.isProSkin)
                {
                    return;
                }

                s_builtForProSkin = EditorGUIUtility.isProSkin;
                s_listHeader = null;
                s_logic = null;
                s_index = null;
                s_summary = null;
                s_accentTitle = null;
                s_accentSubtitle = null;
                s_readOnlyValue = null;
                s_result = null;
            }

            public static GUIStyle ListHeader
            {
                get
                {
                    DropIfSkinChanged();
                    return s_listHeader ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
                }
            }

            public static GUIStyle Index
            {
                get
                {
                    DropIfSkinChanged();
                    s_index ??= new GUIStyle(EditorStyles.miniLabel);
                    s_index.normal.textColor = NeoInspectorTheme.MutedText;
                    return s_index;
                }
            }

            public static GUIStyle Summary
            {
                get
                {
                    DropIfSkinChanged();
                    s_summary ??= new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Italic };
                    s_summary.normal.textColor = NeoInspectorTheme.MutedText;
                    return s_summary;
                }
            }

            public static GUIStyle AccentSubtitle
            {
                get
                {
                    DropIfSkinChanged();
                    s_accentSubtitle ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                    s_accentSubtitle.normal.textColor = NeoInspectorTheme.MutedText;
                    return s_accentSubtitle;
                }
            }

            public static GUIStyle ReadOnlyValue
            {
                get
                {
                    DropIfSkinChanged();
                    return s_readOnlyValue ??= new GUIStyle(EditorStyles.textField) { fontStyle = FontStyle.Bold };
                }
            }

            public static GUIStyle Logic(Color color)
            {
                DropIfSkinChanged();
                s_logic ??= new GUIStyle(EditorStyles.miniBoldLabel) { fontSize = 11 };
                s_logic.normal.textColor = color;
                return s_logic;
            }

            public static GUIStyle AccentTitle(Color color)
            {
                DropIfSkinChanged();
                s_accentTitle ??= new GUIStyle(EditorStyles.miniBoldLabel) { fontSize = 11 };
                s_accentTitle.normal.textColor = color;
                return s_accentTitle;
            }

            public static GUIStyle Result(Color color)
            {
                DropIfSkinChanged();
                s_result ??= new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16
                };
                s_result.normal.textColor = color;
                return s_result;
            }

            public static Color LogicColor(bool isAnd)
            {
                Color color = isAnd ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.9f, 0.3f);
                return ReadableAccent(color);
            }

            /// <summary>Lightens a bright accent on the dark skin and darkens it on the light skin.</summary>
            public static Color ReadableAccent(Color accent)
            {
                return EditorGUIUtility.isProSkin
                    ? Color.Lerp(accent, Color.white, 0.20f)
                    : Color.Lerp(accent, Color.black, 0.45f);
            }
        }
    }
}
