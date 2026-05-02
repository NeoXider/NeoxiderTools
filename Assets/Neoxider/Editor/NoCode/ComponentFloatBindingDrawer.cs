using System.Collections.Generic;
using Neo.Editor.Binding;
using Neo.NoCode;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.NoCode
{
    /// <summary>
    ///     Inspector UI aligned with <see cref="Neo.Editor.Condition.NeoConditionEditor"/> source search: Find By Name,
    ///     Prefab Preview when the instance is missing in Edit Mode, then Component / Member pickers.
    /// </summary>
    [CustomPropertyDrawer(typeof(ComponentFloatBinding))]
    public sealed class ComponentFloatBindingDrawer : PropertyDrawer
    {
        private static readonly GUIContent FindByNameContent = new GUIContent("Find By Name",
            "Find a GameObject by name in the scene (GameObject.Find). Cached while the object exists.");

        private static readonly GUIContent ObjectNameContent =
            new GUIContent("Object Name", "GameObject name to resolve via GameObject.Find().");

        private static readonly GUIContent WaitForObjectContent = new GUIContent("Wait For Object",
            "Wait until the object appears in the scene (no warning). Useful for prefabs spawned later.");

        private static readonly GUIContent PrefabPreviewContent = new GUIContent("Prefab Preview",
            "Drag a prefab from Project to configure components/properties before the instance exists.");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float sp = EditorGUIUtility.standardVerticalSpacing;
            float h = line + sp;

            SerializedProperty useSearchProp = property.FindPropertyRelative("_useSceneSearch");
            SerializedProperty searchNameProp = property.FindPropertyRelative("_searchObjectName");
            SerializedProperty waitProp = property.FindPropertyRelative("_waitForObject");
            SerializedProperty prefabProp = property.FindPropertyRelative("_prefabPreview");

            if (useSearchProp != null)
            {
                h += line + sp;
                if (useSearchProp.boolValue && searchNameProp != null)
                {
                    h += (line + sp) * 3;
                    if (!Application.isPlaying && !string.IsNullOrEmpty(searchNameProp.stringValue))
                    {
                        GameObject found = GameObject.Find(searchNameProp.stringValue);
                        if (found != null)
                        {
                            h += line + sp;
                        }
                        else
                        {
                            h += line + sp;
                            string msg = BuildMissingObjectHelpText(searchNameProp.stringValue, waitProp,
                                prefabProp);
                            h += HelpBoxHeight(msg) + sp;
                        }
                    }
                }
            }

            if (useSearchProp == null || !useSearchProp.boolValue)
            {
                h += line + sp;
            }

            GameObject root = ComponentBindingInspectorShared.ResolveFloatBindingSourceRoot(property);
            if (root != null)
            {
                h += (line + sp) * 3 + line * 1.25f;
                return h;
            }

            bool skipGenericHelp = useSearchProp != null && useSearchProp.boolValue && searchNameProp != null &&
                                   !string.IsNullOrEmpty(searchNameProp.stringValue) && !Application.isPlaying &&
                                   GameObject.Find(searchNameProp.stringValue) == null;
            if (!skipGenericHelp)
            {
                h += line * 2.25f;
            }

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sourceProp = property.FindPropertyRelative("_sourceRoot");
            SerializedProperty typeProp = property.FindPropertyRelative("_componentTypeName");
            SerializedProperty memberProp = property.FindPropertyRelative("_memberName");

            float line = EditorGUIUtility.singleLineHeight;
            float sp = EditorGUIUtility.standardVerticalSpacing;
            float y = position.y;

            Rect row = new Rect(position.x, y, position.width, line);
            EditorGUI.LabelField(row, label, EditorStyles.boldLabel);
            y += line + sp;

            SerializedProperty useSearchProp = property.FindPropertyRelative("_useSceneSearch");
            SerializedProperty searchNameProp = property.FindPropertyRelative("_searchObjectName");
            SerializedProperty waitProp = property.FindPropertyRelative("_waitForObject");
            SerializedProperty prefabProp = property.FindPropertyRelative("_prefabPreview");
            SerializedProperty findRetryProp = property.FindPropertyRelative("_findRetryIntervalSeconds");

            if (useSearchProp != null)
            {
                row = new Rect(position.x, y, position.width, line);
                EditorGUI.PropertyField(row, useSearchProp, FindByNameContent);
                y += line + sp;

                if (useSearchProp.boolValue && searchNameProp != null)
                {
                    row = new Rect(position.x, y, position.width, line);
                    EditorGUI.PropertyField(row, searchNameProp, ObjectNameContent);
                    y += line + sp;

                    if (waitProp != null)
                    {
                        row = new Rect(position.x, y, position.width, line);
                        EditorGUI.PropertyField(row, waitProp, WaitForObjectContent);
                        y += line + sp;
                    }

                    if (findRetryProp != null)
                    {
                        row = new Rect(position.x, y, position.width, line);
                        EditorGUI.PropertyField(row, findRetryProp, new GUIContent("Find Retry Interval (sec)",
                            "How often GameObject.Find runs again while the object is missing. Non-blocking. 0 = every check. Default 1 s."));
                        y += line + sp;
                    }

                    if (!Application.isPlaying && !string.IsNullOrEmpty(searchNameProp.stringValue))
                    {
                        GameObject found = GameObject.Find(searchNameProp.stringValue);
                        if (found != null)
                        {
                            row = new Rect(position.x, y, position.width, line);
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUI.ObjectField(row, "Preview", found, typeof(GameObject), true);
                            EditorGUI.EndDisabledGroup();
                            y += line + sp;
                        }
                        else
                        {
                            row = new Rect(position.x, y, position.width, line);
                            EditorGUI.PropertyField(row, prefabProp, PrefabPreviewContent);
                            y += line + sp;

                            string helpText = BuildMissingObjectHelpText(searchNameProp.stringValue, waitProp,
                                prefabProp);
                            bool hasPrefabForMsg = prefabProp != null && prefabProp.objectReferenceValue != null;
                            bool waitForMsg = waitProp != null && waitProp.boolValue;
                            MessageType mt = hasPrefabForMsg || waitForMsg ? MessageType.Info : MessageType.Warning;
                            float hbH = HelpBoxHeight(helpText);
                            row = new Rect(position.x, y, position.width, hbH);
                            EditorGUI.HelpBox(row, helpText, mt);
                            y += hbH + sp;
                        }
                    }
                }
            }

            if (useSearchProp == null || !useSearchProp.boolValue)
            {
                row = new Rect(position.x, y, position.width, line);
                EditorGUI.PropertyField(row, sourceProp,
                    new GUIContent("Source Root",
                        "GameObject on which to pick the component. If empty — the GameObject with this NoCode component."));
                y += line + sp;
            }

            GameObject root = ComponentBindingInspectorShared.ResolveFloatBindingSourceRoot(property);

            if (root == null)
            {
                bool skipGenericHelp = useSearchProp != null && useSearchProp.boolValue && searchNameProp != null &&
                                       !string.IsNullOrEmpty(searchNameProp.stringValue) && !Application.isPlaying &&
                                       GameObject.Find(searchNameProp.stringValue) == null;
                if (!skipGenericHelp)
                {
                    string fallback =
                        "Put this component on a GameObject, assign Source Root, or use Find By Name with Prefab Preview when the instance is not in the scene.";
                    row = new Rect(position.x, y, position.width, line * 2.25f);
                    EditorGUI.HelpBox(row, fallback, MessageType.Warning);
                }

                EditorGUI.EndProperty();
                return;
            }

            List<string> displayNames = new();
            List<string> fullTypeNames = new();
            ComponentBindingInspectorShared.BuildComponentPickLists(root, displayNames, fullTypeNames);

            if (displayNames.Count == 0)
            {
                row = new Rect(position.x, y, position.width, line * 2f);
                EditorGUI.HelpBox(row, "No components on the resolved source object (except Transform).",
                    MessageType.Warning);
                EditorGUI.EndProperty();
                return;
            }

            if (string.IsNullOrEmpty(typeProp.stringValue))
            {
                ComponentBindingInspectorShared.ApplyFloatBindingDefaultsWhenTypeEmpty(typeProp, memberProp, root,
                    fullTypeNames);
                Apply(property);
            }

            int compIndex = ComponentBindingInspectorShared.IndexOfFullName(fullTypeNames, typeProp.stringValue);
            if (compIndex < 0)
            {
                compIndex = 0;
            }

            row = new Rect(position.x, y, position.width, line);
            EditorGUI.BeginChangeCheck();
            int newComp = EditorGUI.Popup(row, "Component", compIndex, displayNames.ToArray());
            if (EditorGUI.EndChangeCheck() && newComp >= 0 && newComp < fullTypeNames.Count)
            {
                typeProp.stringValue = fullTypeNames[newComp];
                memberProp.stringValue = "";
                Apply(property);
            }

            y += line + sp;

            Component selected = ComponentBindingInspectorShared.FindComponentByTypeName(root, typeProp.stringValue);
            if (selected == null)
            {
                row = new Rect(position.x, y, position.width, line * 2f);
                EditorGUI.HelpBox(row,
                    "Stored component type not found on the resolved source object. Pick Component again.",
                    MessageType.Warning);
                EditorGUI.EndProperty();
                return;
            }

            List<string> keys = new();
            List<string> labels = new();
            ComponentBindingInspectorShared.BuildFloatBindingMemberLists(selected, keys, labels);
            if (labels.Count == 0)
            {
                row = new Rect(position.x, y, position.width, line * 2f);
                EditorGUI.HelpBox(row,
                    "No float / int / bool / ReactivePropertyFloat fields or properties on this component.",
                    MessageType.Info);
                EditorGUI.EndProperty();
                return;
            }

            int memIdx = ComponentBindingInspectorShared.IndexOfMemberKey(keys, memberProp.stringValue);
            if (memIdx < 0)
            {
                memIdx = 0;
                if (ComponentBindingInspectorShared.TryAssignFirstMemberIfKeyMissing(memberProp, keys))
                {
                    Apply(property);
                }
            }

            row = new Rect(position.x, y, position.width, line);
            EditorGUI.BeginChangeCheck();
            int newMem = EditorGUI.Popup(row, "Member", memIdx, labels.ToArray());
            if (EditorGUI.EndChangeCheck() && newMem >= 0 && newMem < keys.Count)
            {
                memberProp.stringValue = keys[newMem];
                Apply(property);
            }

            y += line + sp;

            row = new Rect(position.x, y, position.width, line * 1.2f);
            Color c0 = GUI.contentColor;
            GUI.contentColor = new Color(0.65f, 0.7f, 0.78f, 1f);
            EditorGUI.LabelField(row,
                "Manual names (advanced): type = " +
                (string.IsNullOrEmpty(typeProp.stringValue) ? "—" : typeProp.stringValue) +
                ", member = " + (string.IsNullOrEmpty(memberProp.stringValue) ? "—" : memberProp.stringValue),
                EditorStyles.miniLabel);
            GUI.contentColor = c0;

            EditorGUI.EndProperty();
        }

        private static string BuildMissingObjectHelpText(string objectName, SerializedProperty waitProp,
            SerializedProperty prefabProp)
        {
            bool wait = waitProp != null && waitProp.boolValue;
            bool hasPrefab = prefabProp != null && prefabProp.objectReferenceValue != null;

            if (hasPrefab)
            {
                return wait
                    ? $"Object \"{objectName}\" is not in the scene yet — editing via prefab. Waiting for spawn (no warning)."
                    : $"Object \"{objectName}\" is not in the scene yet — editing via prefab.";
            }

            return wait
                ? $"GameObject \"{objectName}\" not found — waiting for spawn (no warning).\nDrag a prefab into Prefab Preview to edit properties."
                : $"GameObject \"{objectName}\" not found in the scene.\nDrag a prefab into Prefab Preview to edit properties.";
        }

        private static float HelpBoxHeight(string text)
        {
            float width = EditorGUIUtility.currentViewWidth;
            width -= 28f;
            if (width < 80f)
            {
                width = 280f;
            }

            return EditorStyles.helpBox.CalcHeight(new GUIContent(text), width);
        }

        private static void Apply(SerializedProperty bindingProperty)
        {
            bindingProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}
