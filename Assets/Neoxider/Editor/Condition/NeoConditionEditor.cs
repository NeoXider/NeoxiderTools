using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Condition;
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

        // ============================
        //  Compare operator + threshold
        // ============================

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



            // --- Logic Mode ---
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_logicMode"), new GUIContent("Logic Mode"));

            EditorGUILayout.Space(6);

            // --- Conditions list ---
            DrawConditionsList();

            EditorGUILayout.Space(6);

            // --- Check Mode ---
            SerializedProperty checkModeProp = serializedObject.FindProperty("_checkMode");
            EditorGUILayout.PropertyField(checkModeProp, new GUIContent("Check Mode"));

            if (checkModeProp.enumValueIndex == (int)CheckMode.Interval)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_checkInterval"),
                    new GUIContent("Interval (sec)"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_checkOnStart"),
                new GUIContent("Check On Start"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_onlyOnChange"),
                new GUIContent("Only On Change"));

            EditorGUILayout.Space(6);

            // --- Play mode info ---
            DrawPlayModeInfo();

            // --- Events (collapsible group like other Neoxider editors) ---
            DrawCollapsibleUnityEvents();

            serializedObject.ApplyModifiedProperties();
        }


        // ============================
        //  Conditions list
        // ============================

        private void DrawConditionsList()
        {
            SerializedProperty conditionsProp = serializedObject.FindProperty("_conditions");

            // Section header
            EditorGUILayout.BeginHorizontal();
            GUIStyle headerStyle = new(EditorStyles.boldLabel) { fontSize = 13 };
            EditorGUILayout.LabelField($"Conditions ({conditionsProp.arraySize})", headerStyle);
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
            SerializedProperty prefabPreviewProp = entryProp.FindPropertyRelative("_prefabPreview");
            SerializedProperty sourceObjProp = entryProp.FindPropertyRelative("_sourceObject");
            SerializedProperty compTypeProp = entryProp.FindPropertyRelative("_componentTypeName");
            SerializedProperty compIdxProp = entryProp.FindPropertyRelative("_componentIndex");
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

            var condition = (NeoCondition)target;
            SerializedProperty logicProp = serializedObject.FindProperty("_logicMode");
            bool isGameObjectMode = sourceModeProp.enumValueIndex == (int)SourceMode.GameObject;
            bool isSceneSearch = useSceneSearchProp.boolValue;

            // --- Box with colored left border ---
            Color accentNormal;
            if (isSceneSearch)
            {
                accentNormal = new Color(0.4f, 0.9f, 0.5f, 0.6f); // green accent for scene search
            }
            else if (isGameObjectMode)
            {
                accentNormal = new Color(0.9f, 0.7f, 0.2f, 0.6f); // yellow accent for GO mode
            }
            else
            {
                accentNormal = new Color(0.3f, 0.7f, 0.9f, 0.6f); // blue accent for Component mode
            }

            Rect boxRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Draw colored accent on the left
            if (Event.current.type == EventType.Repaint)
            {
                Color accentColor = invertProp.boolValue
                    ? new Color(0.9f, 0.3f, 0.3f, 0.8f)
                    : accentNormal;
                EditorGUI.DrawRect(new Rect(boxRect.x, boxRect.y, 3f, boxRect.height), accentColor);
            }

            // --- Header row ---
            EditorGUILayout.BeginHorizontal();

            // Logic label or index
            if (index > 0)
            {
                string logicLabel = logicProp.enumValueIndex == 0 ? "AND" : "OR";
                Color logicColor = logicProp.enumValueIndex == 0
                    ? new Color(0.3f, 0.9f, 1f)
                    : new Color(1f, 0.9f, 0.3f);
                GUIStyle logicStyle = new(EditorStyles.miniBoldLabel)
                {
                    fontSize = 11,
                    normal = { textColor = logicColor }
                };
                GUILayout.Label(logicLabel, logicStyle, GUILayout.Width(32));
            }
            else
            {
                GUIStyle idxStyle = new(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(1f, 1f, 1f, 0.5f) }
                };
                GUILayout.Label($"#{index}", idxStyle, GUILayout.Width(32));
            }

            // Summary text
            string summary = BuildConditionSummary(sourceModeProp, useSceneSearchProp, searchNameProp,
                compTypeProp, propNameProp, compareOpProp, valueTypeProp,
                thresholdSourceProp, thresholdIntProp, thresholdFloatProp, thresholdBoolProp, thresholdStringProp,
                otherSourceModeProp, otherUseSceneSearchProp, otherSearchNameProp, otherCompTypeProp, otherPropNameProp,
                invertProp);
            GUIStyle summaryStyle = new(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(1f, 1f, 1f, 0.6f) }
            };
            GUILayout.Label(summary, summaryStyle);

            GUILayout.FlexibleSpace();

            // NOT toggle
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = invertProp.boolValue ? new Color(1f, 0.4f, 0.4f) : Color.gray;
            invertProp.boolValue =
                GUILayout.Toggle(invertProp.boolValue, "NOT", EditorStyles.miniButton, GUILayout.Width(36));
            GUI.backgroundColor = oldBg;

            // Delete button
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
                propNameProp.stringValue = "";
                if (isMethodProp != null)
                {
                    isMethodProp.boolValue = false;
                }
            }

            EditorGUI.BeginChangeCheck();
            useSceneSearchProp.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Find By Name",
                    "Find a GameObject by name in the scene (GameObject.Find). Cached while the object exists."),
                useSceneSearchProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                compTypeProp.stringValue = "";
                compIdxProp.intValue = 0;
                propNameProp.stringValue = "";
                if (isMethodProp != null)
                {
                    isMethodProp.boolValue = false;
                }
            }

            if (isSceneSearch)
            {
                EditorGUILayout.PropertyField(searchNameProp,
                    new GUIContent("Object Name", "GameObject name to resolve via GameObject.Find()."));

                waitForObjectProp.boolValue = EditorGUILayout.Toggle(
                    new GUIContent("Wait For Object",
                        "Wait until the object appears in the scene (no warning). Useful for prefabs spawned later."),
                    waitForObjectProp.boolValue);

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
                    var found = GameObject.Find(searchNameProp.stringValue);
                    if (found != null)
                    {
                        targetObj = found;
                        EditorGUI.BeginDisabledGroup(true);
                        DrawReadOnlyObjectField("Preview", found);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(prefabPreviewProp,
                            new GUIContent("Prefab Preview",
                                "Drag a prefab from Project to configure components/properties before the instance exists."));

                        var prefab = prefabPreviewProp.objectReferenceValue as GameObject;
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
                EditorGUILayout.PropertyField(sourceObjProp, new GUIContent("Source Object"));
                if (EditorGUI.EndChangeCheck())
                {
                    compTypeProp.stringValue = "";
                    compIdxProp.intValue = 0;
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
                            otherSourceObjProp, otherCompTypeProp, otherCompIdxProp, otherPropNameProp);
                    }
                }
                else
                {
                    DrawComponentDropdown(targetObj, compTypeProp, compIdxProp, propNameProp, isMethodProp);

                    string selectedCompType = compTypeProp.stringValue;
                    if (!string.IsNullOrEmpty(selectedCompType))
                    {
                        Component selectedComp = FindComponentByTypeName(targetObj, selectedCompType);
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
                                    otherWaitForObjectProp,
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
            SerializedProperty otherSourceObjProp,
            SerializedProperty otherCompTypeProp,
            SerializedProperty otherCompIdxProp,
            SerializedProperty otherPropNameProp)
        {
            BeginAccentSection("Compare", CompareAccent);

            EditorGUILayout.PropertyField(thresholdSourceProp, new GUIContent("Compare With",
                "Constant: compare to a number or text. Other Object: compare to another object's field/property. If Other Source Object is empty, the same object as on the left is used."));

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

                otherUseSceneSearchProp.boolValue = EditorGUILayout.Toggle(
                    new GUIContent("Other: Find By Name"), otherUseSceneSearchProp.boolValue);

                GameObject otherTargetObj = null;
                if (otherUseSceneSearchProp.boolValue)
                {
                    EditorGUILayout.PropertyField(otherSearchNameProp, new GUIContent("Other Object Name"));
                    otherWaitForObjectProp.boolValue = EditorGUILayout.Toggle(
                        new GUIContent("Other: Wait For Object"),
                        otherWaitForObjectProp.boolValue);
                    if (!string.IsNullOrEmpty(otherSearchNameProp.stringValue))
                    {
                        otherTargetObj = GameObject.Find(otherSearchNameProp.stringValue);
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(otherSourceObjProp,
                        new GUIContent("Other Source Object",
                            "Empty = same object as on the left (compare two fields on one object)."));
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
                        new Color(0.76f, 0.82f, 1f, 1f));

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
                        DrawComponentDropdown(otherTargetObj, otherCompTypeProp, otherCompIdxProp, otherPropNameProp,
                            otherIsMethodProp);
                        string otherCompType = otherCompTypeProp.stringValue;
                        if (!string.IsNullOrEmpty(otherCompType))
                        {
                            Component otherComp = FindComponentByTypeName(otherTargetObj, otherCompType);
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
            var vt = (ValueType)valueTypeProp.enumValueIndex;

            if (vt == ValueType.Bool)
            {
                EditorGUILayout.BeginHorizontal();
                int boolOp = compareOpProp.enumValueIndex <= 1 ? compareOpProp.enumValueIndex : 0;
                int newBoolOp = EditorGUILayout.Popup("Compare", boolOp, BoolCompareOps);
                if (newBoolOp != boolOp)
                {
                    compareOpProp.enumValueIndex = newBoolOp;
                }

                int thresholdIdx = thresholdBoolProp.boolValue ? 0 : 1;
                int newThresholdIdx = EditorGUILayout.Popup(thresholdIdx, BoolThresholdNames, GUILayout.Width(60));
                thresholdBoolProp.boolValue = newThresholdIdx == 0;
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

                switch (vt)
                {
                    case ValueType.Int:
                        thresholdIntProp.intValue =
                            EditorGUILayout.IntField(thresholdIntProp.intValue, GUILayout.MinWidth(50));
                        break;
                    case ValueType.Float:
                        thresholdFloatProp.floatValue =
                            EditorGUILayout.FloatField(thresholdFloatProp.floatValue, GUILayout.MinWidth(50));
                        break;
                    case ValueType.String:
                        thresholdStringProp.stringValue = EditorGUILayout.TextField(thresholdStringProp.stringValue,
                            GUILayout.MinWidth(50));
                        break;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // ============================
        //  Component dropdown
        // ============================

        private void DrawComponentDropdown(GameObject targetObj, SerializedProperty compTypeProp,
            SerializedProperty compIdxProp, SerializedProperty propNameProp,
            SerializedProperty isMethodProp = null)
        {
            Component[] components = targetObj.GetComponents<Component>();
            List<string> names = new();
            List<string> typeNames = new();

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    continue;
                }

                Type t = components[i].GetType();
                if (t == typeof(Transform))
                {
                    continue;
                }

                string displayName = t.Name;
                int duplicateCount = 0;
                for (int j = 0; j < i; j++)
                {
                    if (components[j] != null && components[j].GetType() == t)
                    {
                        duplicateCount++;
                    }
                }

                if (duplicateCount > 0)
                {
                    displayName += $" ({duplicateCount + 1})";
                }

                names.Add(displayName);
                typeNames.Add(t.FullName);
            }

            if (names.Count == 0)
            {
                EditorGUILayout.HelpBox("No components found on target object.", MessageType.Warning);
                return;
            }

            int currentIndex = -1;
            string currentType = compTypeProp.stringValue;
            if (!string.IsNullOrEmpty(currentType))
            {
                for (int i = 0; i < typeNames.Count; i++)
                {
                    if (typeNames[i] == currentType)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Component", Mathf.Max(currentIndex, 0), names.ToArray());
            if (EditorGUI.EndChangeCheck() || (currentIndex == -1 && names.Count > 0))
            {
                if (newIndex >= 0 && newIndex < typeNames.Count && compTypeProp.stringValue != typeNames[newIndex])
                {
                    compTypeProp.stringValue = typeNames[newIndex];
                    compIdxProp.intValue = newIndex;
                    propNameProp.stringValue = "";
                    if (isMethodProp != null)
                    {
                        isMethodProp.boolValue = false;
                    }
                }
            }
        }

        // ============================
        //  Property dropdown
        // ============================

        private void DrawPropertyDropdown(Component comp, SerializedProperty propNameProp,
            SerializedProperty valueTypeProp = null,
            SerializedProperty isMethodProp = null,
            SerializedProperty argKindProp = null,
            SerializedProperty argIntProp = null,
            SerializedProperty argFloatProp = null,
            SerializedProperty argStringProp = null)
        {
            Type compType = comp.GetType();
            List<string> propertyNames = new();
            List<ValueType> propertyTypes = new();
            List<string> displayNames = new();
            List<bool> isMethodList = new();
            List<ArgumentKind> argumentKinds = new();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            foreach (PropertyInfo prop in compType.GetProperties(flags))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                ValueType? vt = GetValueType(prop.PropertyType);
                if (vt == null || IsUnityNoiseMember(prop.Name))
                {
                    continue;
                }

                propertyNames.Add(prop.Name);
                propertyTypes.Add(vt.Value);
                displayNames.Add($"{prop.Name}  ({vt.Value})  [prop]");
                isMethodList.Add(false);
                argumentKinds.Add(ArgumentKind.Int);
            }

            foreach (FieldInfo field in compType.GetFields(flags))
            {
                ValueType? vt = GetValueType(field.FieldType);
                if (vt == null || IsUnityNoiseMember(field.Name))
                {
                    continue;
                }

                propertyNames.Add(field.Name);
                propertyTypes.Add(vt.Value);
                displayNames.Add($"{field.Name}  ({vt.Value})");
                isMethodList.Add(false);
                argumentKinds.Add(ArgumentKind.Int);
            }

            if (isMethodProp != null)
            {
                foreach (MethodInfo method in compType.GetMethods(flags))
                {
                    if (IsUnityNoiseMember(method.Name))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 1)
                    {
                        continue;
                    }

                    ArgumentKind? argKind = GetArgumentKind(parameters[0].ParameterType);
                    if (argKind == null)
                    {
                        continue;
                    }

                    ValueType? returnVt = GetValueType(method.ReturnType);
                    if (returnVt == null)
                    {
                        continue;
                    }

                    string paramLabel = argKind switch
                    {
                        ArgumentKind.Int => "int",
                        ArgumentKind.Float => "float",
                        ArgumentKind.String => "string",
                        _ => "?"
                    };
                    propertyNames.Add(method.Name);
                    propertyTypes.Add(returnVt.Value);
                    displayNames.Add($"{method.Name} ({paramLabel}) → {returnVt.Value} [method]");
                    isMethodList.Add(true);
                    argumentKinds.Add(argKind.Value);
                }
            }

            if (displayNames.Count == 0)
            {
                EditorGUILayout.HelpBox("No readable int/float/bool/string fields or methods found.", MessageType.Info);
                return;
            }

            int currentIndex = -1;
            string currentPropName = propNameProp.stringValue;
            bool currentIsMethod = isMethodProp != null && isMethodProp.boolValue;
            int currentArgKind = argKindProp != null ? argKindProp.enumValueIndex : 0;
            for (int i = 0; i < propertyNames.Count; i++)
            {
                if (propertyNames[i] != currentPropName)
                {
                    continue;
                }

                if (isMethodProp == null)
                {
                    currentIndex = i;
                    break;
                }

                if (isMethodList[i] == currentIsMethod && (!isMethodList[i] || (int)argumentKinds[i] == currentArgKind))
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Property", currentIndex, displayNames.ToArray());
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
                    EditorGUILayout.PropertyField(argIntProp, new GUIContent("Argument (int)"));
                    break;
                case ArgumentKind.Float:
                    EditorGUILayout.PropertyField(argFloatProp, new GUIContent("Argument (float)"));
                    break;
                case ArgumentKind.String:
                    EditorGUILayout.PropertyField(argStringProp, new GUIContent("Argument (string)"));
                    break;
            }

            EditorGUI.indentLevel--;
        }

        private static ArgumentKind? GetArgumentKind(Type parameterType)
        {
            if (parameterType == typeof(int) || parameterType == typeof(long) || parameterType == typeof(short) ||
                parameterType == typeof(byte))
            {
                return ArgumentKind.Int;
            }

            if (parameterType == typeof(float) || parameterType == typeof(double))
            {
                return ArgumentKind.Float;
            }

            if (parameterType == typeof(string))
            {
                return ArgumentKind.String;
            }

            return null;
        }

        // ============================
        //  GameObject property dropdown
        // ============================

        private void DrawGameObjectPropertyDropdown(SerializedProperty propNameProp,
            SerializedProperty valueTypeProp = null)
        {
            string[] displayNames = new string[GameObjectProperties.Length];
            for (int i = 0; i < GameObjectProperties.Length; i++)
            {
                displayNames[i] = GameObjectProperties[i].DisplayName;
            }

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

        // ============================
        //  Current value (Play Mode) — GameObject
        // ============================

        private void DrawCurrentValueGameObject(GameObject go, string propertyName)
        {
            if (go == null || string.IsNullOrEmpty(propertyName))
            {
                return;
            }

            object value = null;
            try
            {
                PropertyInfo prop =
                    typeof(GameObject).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
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

        // ============================
        //  Current value (Play Mode) — Component
        // ============================

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

            GUIStyle titleStyle = new(EditorStyles.miniBoldLabel)
            {
                fontSize = 11,
                normal = { textColor = Color.Lerp(accent, Color.white, 0.20f) }
            };
            EditorGUILayout.LabelField(title, titleStyle);

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                GUIStyle subtitleStyle = new(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    normal = { textColor = new Color(0.80f, 0.84f, 0.92f, 0.92f) }
                };
                EditorGUILayout.LabelField(subtitle, subtitleStyle);
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

            GUIStyle valueStyle = new(EditorStyles.textField)
            {
                fontStyle = FontStyle.Bold
            };
            EditorGUI.TextField(rect, label, value, valueStyle);
        }

        private static object GetCurrentValueFromComponent(Component comp, string propertyName, bool isMethod,
            SerializedProperty argKindProp, SerializedProperty argIntProp, SerializedProperty argFloatProp,
            SerializedProperty argStringProp)
        {
            Type type = comp.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (isMethod && argKindProp != null && argIntProp != null)
            {
                var kind = (ArgumentKind)argKindProp.enumValueIndex;
                object arg = kind switch
                {
                    ArgumentKind.Int => argIntProp.intValue,
                    ArgumentKind.Float => argFloatProp != null ? argFloatProp.floatValue : 0f,
                    ArgumentKind.String => argStringProp != null ? argStringProp.stringValue : "",
                    _ => argIntProp.intValue
                };
                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    if (method.Name != propertyName || method.GetParameters().Length != 1)
                    {
                        continue;
                    }

                    if (GetArgumentKind(method.GetParameters()[0].ParameterType) != kind)
                    {
                        continue;
                    }

                    try
                    {
                        return method.Invoke(comp, new[] { arg });
                    }
                    catch
                    {
                        return null;
                    }
                }

                return null;
            }

            try
            {
                PropertyInfo prop = type.GetProperty(propertyName, flags);
                if (prop != null && prop.CanRead)
                {
                    return prop.GetValue(comp);
                }

                FieldInfo field = type.GetField(propertyName, flags);
                return field?.GetValue(comp);
            }
            catch
            {
                return null;
            }
        }

        // ============================
        //  Play mode result display
        // ============================

        private void DrawPlayModeInfo()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var nc = (NeoCondition)target;

            EditorGUILayout.Space(4);

            // Result display
            bool result = nc.LastResult;
            string resultText = result ? "TRUE" : "FALSE";
            Color resultColor = result ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.35f, 0.35f);
            Color resultBg = result ? new Color(0.1f, 0.4f, 0.15f, 0.3f) : new Color(0.4f, 0.1f, 0.1f, 0.3f);

            Rect resultRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(resultRect, resultBg);
            }

            GUIStyle resultStyle = new(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = resultColor }
            };
            GUILayout.Label($"Result: {resultText}", resultStyle, GUILayout.Height(28));

            EditorGUILayout.EndHorizontal();

            // Repaint to update values
            EnsureRepaint();
        }

        // ============================
        //  Summary text for collapsed view
        // ============================

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
                var vt = (ValueType)valueTypeProp.enumValueIndex;
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

        // ============================
        //  Helpers
        // ============================

        private static Component FindComponentByTypeName(GameObject obj, string typeName)
        {
            Component[] components = obj.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp == null)
                {
                    continue;
                }

                if (comp.GetType().FullName == typeName || comp.GetType().Name == typeName)
                {
                    return comp;
                }
            }

            return null;
        }

        private static ValueType? GetValueType(Type type)
        {
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            {
                return ValueType.Int;
            }

            if (type == typeof(float) || type == typeof(double))
            {
                return ValueType.Float;
            }

            if (type == typeof(bool))
            {
                return ValueType.Bool;
            }

            if (type == typeof(string))
            {
                return ValueType.String;
            }

            return null;
        }

        private static bool IsUnityNoiseMember(string name)
        {
            switch (name)
            {
                case "useGUILayout":
                case "runInEditMode":
                case "enabled":
                case "isActiveAndEnabled":
                case "gameObject":
                case "tag":
                case "name":
                case "hideFlags":
                case "destroyCancellationToken":
                    return true;
                default:
                    return false;
            }
        }

        private static void ResetConditionEntry(SerializedProperty entry)
        {
            entry.FindPropertyRelative("_sourceMode").enumValueIndex = (int)SourceMode.Component;
            entry.FindPropertyRelative("_useSceneSearch").boolValue = false;
            entry.FindPropertyRelative("_searchObjectName").stringValue = "";
            entry.FindPropertyRelative("_waitForObject").boolValue = false;
            entry.FindPropertyRelative("_prefabPreview").objectReferenceValue = null;
            entry.FindPropertyRelative("_sourceObject").objectReferenceValue = null;
            entry.FindPropertyRelative("_componentIndex").intValue = 0;
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
            entry.FindPropertyRelative("_otherSourceObject").objectReferenceValue = null;
            entry.FindPropertyRelative("_otherComponentIndex").intValue = 0;
            entry.FindPropertyRelative("_otherComponentTypeName").stringValue = "";
            entry.FindPropertyRelative("_otherPropertyName").stringValue = "";
            entry.FindPropertyRelative("_otherIsMethodWithArgument").boolValue = false;
            entry.FindPropertyRelative("_otherPropertyArgumentKind").enumValueIndex = 0;
            entry.FindPropertyRelative("_otherPropertyArgumentInt").intValue = 0;
            entry.FindPropertyRelative("_otherPropertyArgumentFloat").floatValue = 0f;
            entry.FindPropertyRelative("_otherPropertyArgumentString").stringValue = "";
        }

        // ============================
        //  Single condition entry
        // ============================

        // ============================
        //  GameObject property definitions
        // ============================

        private struct GameObjectPropertyDef
        {
            public string Name;
            public ValueType Type;
            public string DisplayName;
        }
    }
}
