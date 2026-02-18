using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Condition;
using UnityEditor;
using UnityEngine;
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
            new() { Name = "activeSelf", Type = ValueType.Bool, DisplayName = "activeSelf  (bool) — объект включён" },
            new()
            {
                Name = "activeInHierarchy", Type = ValueType.Bool,
                DisplayName = "activeInHierarchy  (bool) — активен в иерархии"
            },
            new() { Name = "isStatic", Type = ValueType.Bool, DisplayName = "isStatic  (bool) — статический" },
            new() { Name = "tag", Type = ValueType.String, DisplayName = "tag  (string) — тег объекта" },
            new() { Name = "name", Type = ValueType.String, DisplayName = "name  (string) — имя объекта" },
            new() { Name = "layer", Type = ValueType.Int, DisplayName = "layer  (int) — слой объекта" }
        };

        private static readonly string[] SourceModeNames = { "Component", "GameObject" };

        // ============================
        //  Compare operator + threshold
        // ============================

        private static readonly string[] BoolCompareOps = { "==  Equal", "!=  Not Equal" };
        private static readonly string[] BoolThresholdNames = { "true", "false" };

        private static readonly string[] ThresholdSourceNames = { "Constant (number/text)", "Other Object (variable)" };

        /// <summary>
        ///     Включаем кастомный UI внутри фирменного Neoxider-стиля.
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
        ///     Рисуем собственный инспектор, но внутри Neo рамки (подпись, радужная линия, Actions).
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

            NeoCondition condition = (NeoCondition)target;
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

            // --- Source Mode ---
            EditorGUI.BeginChangeCheck();
            int newModeIdx = EditorGUILayout.Popup("Source", sourceModeProp.enumValueIndex, SourceModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                sourceModeProp.enumValueIndex = newModeIdx;
                // Reset selections when switching mode
                compTypeProp.stringValue = "";
                compIdxProp.intValue = 0;
                propNameProp.stringValue = "";
            }

            // --- Scene Search toggle ---
            EditorGUI.BeginChangeCheck();
            useSceneSearchProp.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Find By Name",
                    "Искать GameObject по имени в сцене (GameObject.Find). Кешируется пока объект жив."),
                useSceneSearchProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                // При переключении сбрасываем привязки
                compTypeProp.stringValue = "";
                compIdxProp.intValue = 0;
                propNameProp.stringValue = "";
            }

            // --- Source Object or Search Name ---
            GameObject targetObj = null;

            if (isSceneSearch)
            {
                // Поле для ввода имени
                EditorGUILayout.PropertyField(searchNameProp,
                    new GUIContent("Object Name", "Имя GameObject для поиска через GameObject.Find()"));

                // Wait For Object — тихое ожидание спавна без Warning
                waitForObjectProp.boolValue = EditorGUILayout.Toggle(
                    new GUIContent("Wait For Object",
                        "Ожидать появления объекта в сцене (без Warning). Полезно для префабов, которые будут заспавнены позже."),
                    waitForObjectProp.boolValue);

                // В Play Mode показываем найденный объект
                if (Application.isPlaying && condition != null)
                {
                    IReadOnlyList<ConditionEntry> entries = condition.Conditions;
                    if (index < entries.Count)
                    {
                        ConditionEntry runtimeEntry = entries[index];
                        GameObject found = runtimeEntry?.FoundByNameObject;
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField("Found Object", found, typeof(GameObject), true);
                        EditorGUI.EndDisabledGroup();

                        if (found != null)
                        {
                            targetObj = found;
                        }
                    }
                }

                // В Edit Mode пробуем найти объект для отображения компонентов/свойств
                if (!Application.isPlaying && !string.IsNullOrEmpty(searchNameProp.stringValue))
                {
                    GameObject found = GameObject.Find(searchNameProp.stringValue);
                    if (found != null)
                    {
                        targetObj = found;
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField("Preview", found, typeof(GameObject), true);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        // Объект не на сцене — пробуем использовать Prefab Preview
                        EditorGUILayout.PropertyField(prefabPreviewProp,
                            new GUIContent("Prefab Preview",
                                "Перетащите префаб из Project, чтобы настроить компоненты/свойства до спавна объекта."));

                        GameObject prefab = prefabPreviewProp.objectReferenceValue as GameObject;
                        if (prefab != null)
                        {
                            targetObj = prefab;
                            EditorGUILayout.HelpBox(
                                waitForObjectProp.boolValue
                                    ? $"Объект \"{searchNameProp.stringValue}\" ещё не на сцене — настройка по префабу. Ожидание спавна (без Warning)."
                                    : $"Объект \"{searchNameProp.stringValue}\" ещё не на сцене — настройка по префабу.",
                                MessageType.Info);
                        }
                        else
                        {
                            if (waitForObjectProp.boolValue)
                            {
                                EditorGUILayout.HelpBox(
                                    $"GameObject \"{searchNameProp.stringValue}\" не найден — ожидание спавна (без Warning).\nПеретащите префаб в «Prefab Preview» для настройки свойств.",
                                    MessageType.Info);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(
                                    $"GameObject \"{searchNameProp.stringValue}\" не найден в сцене.\nПеретащите префаб в «Prefab Preview» для настройки свойств.",
                                    MessageType.Warning);
                            }
                        }
                    }
                }

                if (targetObj == null && condition != null && !Application.isPlaying)
                {
                    targetObj = null; // Не показываем компоненты если объект не найден
                }
            }
            else
            {
                // Прямая ссылка на объект
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(sourceObjProp, new GUIContent("Source Object"));
                if (EditorGUI.EndChangeCheck())
                {
                    compTypeProp.stringValue = "";
                    compIdxProp.intValue = 0;
                    propNameProp.stringValue = "";
                }

                targetObj = (GameObject)sourceObjProp.objectReferenceValue;
                if (targetObj == null && condition != null)
                {
                    targetObj = condition.gameObject;
                }
            }

            if (targetObj != null)
            {
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
                    DrawComponentDropdown(targetObj, compTypeProp, compIdxProp, propNameProp);

                    string selectedCompType = compTypeProp.stringValue;
                    if (!string.IsNullOrEmpty(selectedCompType))
                    {
                        Component selectedComp = FindComponentByTypeName(targetObj, selectedCompType);
                        if (selectedComp != null)
                        {
                            DrawPropertyDropdown(selectedComp, propNameProp, valueTypeProp);

                            if (Application.isPlaying && !string.IsNullOrEmpty(propNameProp.stringValue))
                            {
                                DrawCurrentValue(selectedComp, propNameProp.stringValue);
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
            EditorGUILayout.PropertyField(thresholdSourceProp, new GUIContent("Compare With",
                "Constant: сравнить с числом или текстом. Other Object: сравнить с полем/свойством другого объекта. Если Other Source Object пуст — используется тот же объект, что и слева."));

            bool isOtherObject = thresholdSourceProp.enumValueIndex == (int)ThresholdSource.OtherObject;

            if (isOtherObject)
            {
                int currentOp = compareOpProp.enumValueIndex;
                int newOp = EditorGUILayout.Popup("Operator", currentOp, CompareOpDisplayNames);
                if (newOp != currentOp)
                {
                    compareOpProp.enumValueIndex = newOp;
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Other Object (right side)", EditorStyles.boldLabel);

                int otherModeIdx =
                    EditorGUILayout.Popup("Other Source", otherSourceModeProp.enumValueIndex, SourceModeNames);
                if (otherModeIdx != otherSourceModeProp.enumValueIndex)
                {
                    otherSourceModeProp.enumValueIndex = otherModeIdx;
                    otherCompTypeProp.stringValue = "";
                    otherPropNameProp.stringValue = "";
                }

                otherUseSceneSearchProp.boolValue = EditorGUILayout.Toggle(
                    new GUIContent("Other: Find By Name"), otherUseSceneSearchProp.boolValue);

                GameObject otherTargetObj = null;
                if (otherUseSceneSearchProp.boolValue)
                {
                    EditorGUILayout.PropertyField(otherSearchNameProp, new GUIContent("Other Object Name"));
                    otherWaitForObjectProp.boolValue = EditorGUILayout.Toggle(new GUIContent("Other: Wait For Object"),
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
                            "Пусто = тот же объект, что и слева (сравнение двух полей одного объекта)."));
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
                        DrawComponentDropdown(otherTargetObj, otherCompTypeProp, otherCompIdxProp, otherPropNameProp);
                        string otherCompType = otherCompTypeProp.stringValue;
                        if (!string.IsNullOrEmpty(otherCompType))
                        {
                            Component otherComp = FindComponentByTypeName(otherTargetObj, otherCompType);
                            if (otherComp != null)
                            {
                                DrawPropertyDropdown(otherComp, otherPropNameProp);
                                if (Application.isPlaying && !string.IsNullOrEmpty(otherPropNameProp.stringValue))
                                {
                                    DrawCurrentValue(otherComp, otherPropNameProp.stringValue);
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
            }
            else
            {
                DrawCompareAndThreshold(compareOpProp, valueTypeProp,
                    thresholdIntProp, thresholdFloatProp, thresholdBoolProp, thresholdStringProp);
            }
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
            SerializedProperty compIdxProp, SerializedProperty propNameProp)
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
                if (newIndex >= 0 && newIndex < typeNames.Count)
                {
                    if (compTypeProp.stringValue != typeNames[newIndex])
                    {
                        compTypeProp.stringValue = typeNames[newIndex];
                        compIdxProp.intValue = newIndex;
                        propNameProp.stringValue = "";
                    }
                }
            }
        }

        // ============================
        //  Property dropdown
        // ============================

        private void DrawPropertyDropdown(Component comp, SerializedProperty propNameProp,
            SerializedProperty valueTypeProp = null)
        {
            Type compType = comp.GetType();
            List<string> propertyNames = new();
            List<ValueType> propertyTypes = new();
            List<string> displayNames = new();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            foreach (PropertyInfo prop in compType.GetProperties(flags))
            {
                if (!prop.CanRead)
                {
                    continue;
                }

                if (prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                ValueType? vt = GetValueType(prop.PropertyType);
                if (vt == null)
                {
                    continue;
                }

                if (IsUnityNoiseMember(prop.Name))
                {
                    continue;
                }

                propertyNames.Add(prop.Name);
                propertyTypes.Add(vt.Value);
                displayNames.Add($"{prop.Name}  ({vt.Value})  [prop]");
            }

            foreach (FieldInfo field in compType.GetFields(flags))
            {
                ValueType? vt = GetValueType(field.FieldType);
                if (vt == null)
                {
                    continue;
                }

                if (IsUnityNoiseMember(field.Name))
                {
                    continue;
                }

                propertyNames.Add(field.Name);
                propertyTypes.Add(vt.Value);
                displayNames.Add($"{field.Name}  ({vt.Value})");
            }

            if (displayNames.Count == 0)
            {
                EditorGUILayout.HelpBox("No readable int/float/bool/string fields found.", MessageType.Info);
                return;
            }

            int currentIndex = -1;
            string currentPropName = propNameProp.stringValue;
            if (!string.IsNullOrEmpty(currentPropName))
            {
                currentIndex = propertyNames.IndexOf(currentPropName);
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Property", Mathf.Max(currentIndex, 0), displayNames.ToArray());
            if (EditorGUI.EndChangeCheck() || currentIndex == -1)
            {
                if (newIndex >= 0 && newIndex < propertyNames.Count)
                {
                    propNameProp.stringValue = propertyNames[newIndex];
                    if (valueTypeProp != null)
                    {
                        valueTypeProp.enumValueIndex = (int)propertyTypes[newIndex];
                    }
                }
            }
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
                GUIStyle valueStyle = new(EditorStyles.textField)
                {
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.TextField("Current Value", value.ToString(), valueStyle);
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

            Type type = comp.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            object value = null;
            try
            {
                PropertyInfo prop = type.GetProperty(propertyName, flags);
                if (prop != null && prop.CanRead)
                {
                    value = prop.GetValue(comp);
                }
                else
                {
                    FieldInfo field = type.GetField(propertyName, flags);
                    if (field != null)
                    {
                        value = field.GetValue(comp);
                    }
                }
            }
            catch
            {
                return;
            }

            if (value != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUIStyle valueStyle = new(EditorStyles.textField)
                {
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.TextField("Current Value", value.ToString(), valueStyle);
                EditorGUI.EndDisabledGroup();
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

            NeoCondition nc = (NeoCondition)target;

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
            entry.FindPropertyRelative("_otherSourceMode").enumValueIndex = (int)SourceMode.Component;
            entry.FindPropertyRelative("_otherUseSceneSearch").boolValue = false;
            entry.FindPropertyRelative("_otherSearchObjectName").stringValue = "";
            entry.FindPropertyRelative("_otherWaitForObject").boolValue = false;
            entry.FindPropertyRelative("_otherSourceObject").objectReferenceValue = null;
            entry.FindPropertyRelative("_otherComponentIndex").intValue = 0;
            entry.FindPropertyRelative("_otherComponentTypeName").stringValue = "";
            entry.FindPropertyRelative("_otherPropertyName").stringValue = "";
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