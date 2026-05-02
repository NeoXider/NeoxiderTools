using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Condition;
using Neo.Reactive;
using ConditionValueType = Neo.Condition.ValueType;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Binding
{
    /// <summary>
    ///     Shared inspector data + helpers for picking a component on a <see cref="GameObject"/> and reflecting on its
    ///     members — used by <c>NeoCondition</c> editor and by <see cref="Neo.NoCode.ComponentFloatBinding"/> drawer
    ///     (SetProgress / NoCodeBindText).
    /// </summary>
    public static class ComponentBindingInspectorShared
    {
        /// <summary>NeoCondition member lists: public instance only (matches original Condition inspector).</summary>
        public const BindingFlags ConditionMemberFlags = BindingFlags.Public | BindingFlags.Instance;

        public const BindingFlags InstanceMembers =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>C#-style keyword for NeoCondition <see cref="ConditionValueType"/> in dropdown labels.</summary>
        public static string FormatConditionValueTypeKeyword(ConditionValueType vt)
        {
            return vt switch
            {
                ConditionValueType.Int => "int",
                ConditionValueType.Float => "float",
                ConditionValueType.Bool => "bool",
                ConditionValueType.String => "string",
                _ => vt.ToString()
            };
        }

        /// <summary>
        ///     CLR type → familiar C# names (<c>int</c>, <c>float</c>, <c>bool</c>) for inspector dropdowns.
        /// </summary>
        public static string FormatClrTypeDisplayName(Type type)
        {
            if (type == null)
            {
                return "?";
            }

            if (type == typeof(bool))
            {
                return "bool";
            }

            if (type == typeof(byte))
            {
                return "byte";
            }

            if (type == typeof(sbyte))
            {
                return "sbyte";
            }

            if (type == typeof(short))
            {
                return "short";
            }

            if (type == typeof(ushort))
            {
                return "ushort";
            }

            if (type == typeof(int))
            {
                return "int";
            }

            if (type == typeof(uint))
            {
                return "uint";
            }

            if (type == typeof(long))
            {
                return "long";
            }

            if (type == typeof(ulong))
            {
                return "ulong";
            }

            if (type == typeof(float))
            {
                return "float";
            }

            if (type == typeof(double))
            {
                return "double";
            }

            if (type == typeof(decimal))
            {
                return "decimal";
            }

            if (type == typeof(char))
            {
                return "char";
            }

            if (type == typeof(string))
            {
                return "string";
            }

            if (type == typeof(object))
            {
                return "object";
            }

            if (type == typeof(ReactivePropertyFloat))
            {
                return "ReactivePropertyFloat";
            }

            return type.Name;
        }

        /// <summary>
        ///     Fills lists for a Component popup (display name may include "(2)" for duplicate script types).
        /// </summary>
        public static void BuildComponentPickLists(GameObject targetObj, List<string> displayNames,
            List<string> fullTypeNames)
        {
            displayNames.Clear();
            fullTypeNames.Clear();
            if (targetObj == null)
            {
                return;
            }

            Component[] components = targetObj.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component c = components[i];
                if (c == null)
                {
                    continue;
                }

                Type t = c.GetType();
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

                displayNames.Add(displayName);
                fullTypeNames.Add(t.FullName);
            }
        }

        public static Component FindComponentByTypeName(GameObject obj, string typeName)
        {
            if (string.IsNullOrEmpty(typeName) || obj == null)
            {
                return null;
            }

            foreach (Component comp in obj.GetComponents<Component>())
            {
                if (comp == null)
                {
                    continue;
                }

                Type t = comp.GetType();
                if (t.FullName == typeName || t.Name == typeName)
                {
                    return comp;
                }
            }

            return null;
        }

        public static int IndexOfFullName(IReadOnlyList<string> fullTypeNames, string storedTypeName)
        {
            if (string.IsNullOrEmpty(storedTypeName))
            {
                return -1;
            }

            for (int i = 0; i < fullTypeNames.Count; i++)
            {
                if (fullTypeNames[i] == storedTypeName)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Maps a CLR type to <see cref="Neo.Condition.ValueType"/> for NeoCondition (primitives only).</summary>
        public static ConditionValueType? TryGetConditionValueType(Type type)
        {
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            {
                return ConditionValueType.Int;
            }

            if (type == typeof(float) || type == typeof(double))
            {
                return ConditionValueType.Float;
            }

            if (type == typeof(bool))
            {
                return ConditionValueType.Bool;
            }

            if (type == typeof(string))
            {
                return ConditionValueType.String;
            }

            return null;
        }

        /// <summary>Maps a single method parameter type to <see cref="ArgumentKind"/>.</summary>
        public static ArgumentKind? TryGetConditionArgumentKind(Type parameterType)
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

        /// <summary>
        ///     Lists fields, properties, and optionally single-arg methods whose return type maps to
        ///     <see cref="Neo.Condition.ValueType"/> — same rules as <c>NeoCondition</c> property dropdown.
        /// </summary>
        public static void BuildConditionMemberLists(Component comp, bool includeMethods, List<string> names,
            List<ConditionValueType> types, List<string> labels, List<bool> isMethodList, List<ArgumentKind> argKinds)
        {
            names.Clear();
            types.Clear();
            labels.Clear();
            isMethodList.Clear();
            argKinds.Clear();
            Type compType = comp.GetType();

            foreach (PropertyInfo prop in compType.GetProperties(ConditionMemberFlags))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                ConditionValueType? vt = TryGetConditionValueType(prop.PropertyType);
                if (vt == null || IsUnityNoiseMember(prop.Name))
                {
                    continue;
                }

                names.Add(prop.Name);
                types.Add(vt.Value);
                labels.Add($"{prop.Name}  ({FormatConditionValueTypeKeyword(vt.Value)})  [prop]");
                isMethodList.Add(false);
                argKinds.Add(ArgumentKind.Int);
            }

            foreach (FieldInfo field in compType.GetFields(ConditionMemberFlags))
            {
                ConditionValueType? vt = TryGetConditionValueType(field.FieldType);
                if (vt == null || IsUnityNoiseMember(field.Name))
                {
                    continue;
                }

                names.Add(field.Name);
                types.Add(vt.Value);
                labels.Add($"{field.Name}  ({FormatConditionValueTypeKeyword(vt.Value)})");
                isMethodList.Add(false);
                argKinds.Add(ArgumentKind.Int);
            }

            if (!includeMethods)
            {
                return;
            }

            foreach (MethodInfo method in compType.GetMethods(ConditionMemberFlags))
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

                ArgumentKind? argKind = TryGetConditionArgumentKind(parameters[0].ParameterType);
                if (argKind == null)
                {
                    continue;
                }

                ConditionValueType? returnVt = TryGetConditionValueType(method.ReturnType);
                if (returnVt == null)
                {
                    continue;
                }

                string paramLabel = argKind.Value switch
                {
                    ArgumentKind.Int => "int",
                    ArgumentKind.Float => "float",
                    ArgumentKind.String => "string",
                    _ => "?"
                };
                names.Add(method.Name);
                types.Add(returnVt.Value);
                labels.Add(
                    $"{method.Name} ({paramLabel}) → {FormatConditionValueTypeKeyword(returnVt.Value)} [method]");
                isMethodList.Add(true);
                argKinds.Add(argKind.Value);
            }
        }

        /// <summary>
        ///     Resolves which row in <see cref="BuildConditionMemberLists"/> matches serialized selection (name + method
        ///     metadata).
        /// </summary>
        public static int FindConditionMemberIndex(IReadOnlyList<string> names, IReadOnlyList<bool> isMethodEntries,
            IReadOnlyList<ArgumentKind> argumentKinds, string storedMemberName, bool storedIsMethod,
            int storedArgumentKindEnumIndex, bool disambiguateMethods)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i] != storedMemberName)
                {
                    continue;
                }

                if (!disambiguateMethods)
                {
                    return i;
                }

                if (isMethodEntries[i] == storedIsMethod &&
                    (!isMethodEntries[i] || (int)argumentKinds[i] == storedArgumentKindEnumIndex))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        ///     GameObject used for component/member pickers in the Editor: when Find By Name is on and
        ///     <c>_searchObjectName</c> is non-empty — <see cref="GameObject.Find"/>, then <c>_prefabPreview</c> (matches
        ///     <see cref="Neo.Condition.ConditionEntry"/> / NeoCondition inspector). Otherwise <c>_sourceRoot</c> or the host
        ///     <see cref="MonoBehaviour"/>'s <see cref="GameObject"/>.
        /// </summary>
        public static GameObject ResolveFloatBindingSourceRoot(SerializedProperty bindingProperty)
        {
            SerializedProperty useSearch = bindingProperty.FindPropertyRelative("_useSceneSearch");
            SerializedProperty searchName = bindingProperty.FindPropertyRelative("_searchObjectName");
            SerializedProperty prefabProp = bindingProperty.FindPropertyRelative("_prefabPreview");
            if (useSearch != null && useSearch.boolValue && searchName != null &&
                !string.IsNullOrEmpty(searchName.stringValue))
            {
                GameObject found = GameObject.Find(searchName.stringValue);
                if (found != null)
                {
                    return found;
                }

                if (prefabProp != null && prefabProp.objectReferenceValue is GameObject prefabGo)
                {
                    return prefabGo;
                }

                return null;
            }

            SerializedProperty sourceProp = bindingProperty.FindPropertyRelative("_sourceRoot");
            GameObject root = sourceProp.objectReferenceValue as GameObject;
            if (root != null)
            {
                return root;
            }

            if (bindingProperty.serializedObject.targetObject is MonoBehaviour mb)
            {
                return mb.gameObject;
            }

            return null;
        }

        /// <summary>
        ///     When <paramref name="componentTypeProp"/> is empty, picks first component type and first bindable float
        ///     member (same as drawer defaults).
        /// </summary>
        public static void ApplyFloatBindingDefaultsWhenTypeEmpty(SerializedProperty componentTypeProp,
            SerializedProperty memberNameProp, GameObject root, IReadOnlyList<string> fullTypeNames)
        {
            if (!string.IsNullOrEmpty(componentTypeProp.stringValue) || fullTypeNames.Count == 0 || root == null)
            {
                return;
            }

            componentTypeProp.stringValue = fullTypeNames[0];
            memberNameProp.stringValue = "";
            Component dc = FindComponentByTypeName(root, componentTypeProp.stringValue);
            if (dc == null)
            {
                return;
            }

            List<string> dk = new();
            List<string> dl = new();
            BuildFloatBindingMemberLists(dc, dk, dl);
            if (dk.Count > 0)
            {
                memberNameProp.stringValue = dk[0];
            }
        }

        /// <summary>Index of <paramref name="memberKey"/> in <paramref name="keys"/>, or -1.</summary>
        public static int IndexOfMemberKey(IReadOnlyList<string> keys, string memberKey)
        {
            if (string.IsNullOrEmpty(memberKey))
            {
                return -1;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] == memberKey)
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool TryAssignFirstMemberIfKeyMissing(SerializedProperty memberNameProp,
            IReadOnlyList<string> keys)
        {
            if (keys.Count == 0 || !string.IsNullOrEmpty(memberNameProp.stringValue))
            {
                return false;
            }

            memberNameProp.stringValue = keys[0];
            return true;
        }

        public static bool IsUnityNoiseMember(string name)
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

        /// <summary>
        ///     Members readable by <see cref="Neo.NoCode.ComponentFloatBinding"/> (numeric + reactive float).
        /// </summary>
        public static void BuildFloatBindingMemberLists(Component comp, List<string> keys, List<string> labels)
        {
            keys.Clear();
            labels.Clear();
            Type t = comp.GetType();

            foreach (PropertyInfo pi in t.GetProperties(InstanceMembers))
            {
                if (!pi.CanRead || pi.GetIndexParameters().Length > 0 || IsUnityNoiseMember(pi.Name))
                {
                    continue;
                }

                if (!IsFloatBindingMemberType(pi.PropertyType))
                {
                    continue;
                }

                keys.Add(pi.Name);
                labels.Add($"{pi.Name}  ({FormatClrTypeDisplayName(pi.PropertyType)})  [prop]");
            }

            foreach (FieldInfo fi in t.GetFields(InstanceMembers))
            {
                if (fi.IsLiteral || IsUnityNoiseMember(fi.Name))
                {
                    continue;
                }

                if (!IsFloatBindingMemberType(fi.FieldType))
                {
                    continue;
                }

                keys.Add(fi.Name);
                labels.Add($"{fi.Name}  ({FormatClrTypeDisplayName(fi.FieldType)})");
            }
        }

        private static bool IsFloatBindingMemberType(Type memberType)
        {
            if (memberType == typeof(float) || memberType == typeof(int) || memberType == typeof(double) ||
                memberType == typeof(bool))
            {
                return true;
            }

            return memberType == typeof(ReactivePropertyFloat);
        }
    }
}
