using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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

            if (type == typeof(ReactivePropertyInt))
            {
                return "ReactivePropertyInt";
            }

            if (type == typeof(ReactivePropertyBool))
            {
                return "ReactivePropertyBool";
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

            List<Component> components = s_componentBuffer;
            Dictionary<Type, int> seen = s_typeCounts;
            components.Clear();
            seen.Clear();
            targetObj.GetComponents(components);
            for (int i = 0; i < components.Count; i++)
            {
                Component c = components[i];
                if (c == null)
                {
                    continue;
                }

                Type t = c.GetType();
                // WHY: one running count per type instead of rescanning every earlier component (was O(C^2)).
                seen.TryGetValue(t, out int duplicateCount);
                seen[t] = duplicateCount + 1;
                if (t == typeof(Transform))
                {
                    continue;
                }

                displayNames.Add(duplicateCount > 0 ? $"{t.Name} ({duplicateCount + 1})" : t.Name);
                fullTypeNames.Add(t.FullName);
            }

            components.Clear();
            seen.Clear();
        }

        // WHY: editor pickers are rebuilt on every IMGUI event; one reused buffer replaces a GetComponents array per call.
        // Editor IMGUI is single-threaded and none of these helpers call back into each other while iterating.
        private static readonly List<Component> s_componentBuffer = new();
        private static readonly Dictionary<Type, int> s_typeCounts = new();

        /// <summary>First component of <paramref name="obj" /> whose full or short type name is <paramref name="typeName" />.</summary>
        public static Component FindComponentByTypeName(GameObject obj, string typeName)
        {
            return FindComponentByTypeName(obj, typeName, 0);
        }

        /// <summary>Resolves a zero-based occurrence among matching types; missing/negative requests fail.</summary>
        public static Component FindComponentByTypeName(GameObject obj, string typeName, int occurrence)
        {
            if (string.IsNullOrEmpty(typeName) || obj == null || occurrence < 0)
            {
                return null;
            }

            List<Component> components = s_componentBuffer;
            components.Clear();
            obj.GetComponents(components);
            Component found = null;
            for (int i = 0; i < components.Count; i++)
            {
                Component comp = components[i];
                if (comp == null)
                {
                    continue;
                }

                Type t = comp.GetType();
                if (t.FullName == typeName || t.Name == typeName)
                {
                    if (occurrence == 0)
                    {
                        found = comp;
                        break;
                    }

                    occurrence--;
                }
            }

            components.Clear();
            return found;
        }

        /// <summary>Returns the popup row for the requested type occurrence, without clamping or fallback.</summary>
        public static int IndexOfComponent(IReadOnlyList<string> fullTypeNames, string storedTypeName, int occurrence)
        {
            if (string.IsNullOrEmpty(storedTypeName) || occurrence < 0)
            {
                return -1;
            }

            for (int i = 0; i < fullTypeNames.Count; i++)
            {
                string fullName = fullTypeNames[i];
                int separator = Math.Max(fullName.LastIndexOf('.'), fullName.LastIndexOf('+'));
                if (fullName == storedTypeName || fullName.Substring(separator + 1) == storedTypeName)
                {
                    if (occurrence == 0)
                    {
                        return i;
                    }

                    occurrence--;
                }
            }

            return -1;
        }

        /// <summary>Maps a popup row to its occurrence within that exact component type.</summary>
        public static int ComponentOccurrenceAt(IReadOnlyList<string> fullTypeNames, int index)
        {
            if (index < 0 || index >= fullTypeNames.Count)
            {
                return -1;
            }

            int occurrence = 0;
            for (int i = 0; i < index; i++)
            {
                if (fullTypeNames[i] == fullTypeNames[index])
                {
                    occurrence++;
                }
            }

            return occurrence;
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
            if (comp == null)
            {
                return;
            }

            ConditionMember[] members = GetConditionMembers(comp.GetType(), includeMethods);
            for (int i = 0; i < members.Length; i++)
            {
                ConditionMember member = members[i];
                names.Add(member.Name);
                types.Add(member.ValueType);
                labels.Add(member.Label);
                isMethodList.Add(member.IsMethod);
                argKinds.Add(member.ArgumentKind);
            }
        }

        private readonly struct ConditionMember
        {
            public readonly string Name;
            public readonly ConditionValueType ValueType;
            public readonly string Label;
            public readonly bool IsMethod;
            public readonly ArgumentKind ArgumentKind;

            public ConditionMember(string name, ConditionValueType valueType, string label, bool isMethod,
                ArgumentKind argumentKind)
            {
                Name = name;
                ValueType = valueType;
                Label = label;
                IsMethod = isMethod;
                ArgumentKind = argumentKind;
            }
        }

        private readonly struct FloatBindingMember
        {
            public readonly string Key;
            public readonly string Label;

            public FloatBindingMember(string key, string label)
            {
                Key = key;
                Label = label;
            }
        }

        // WHY: member pickers are rebuilt on every IMGUI event, but a type's members only change with a domain
        // reload; reflect and format the labels once per type instead of per draw.
        private static readonly Dictionary<(Type, bool), ConditionMember[]> s_conditionMembers = new();
        private static readonly Dictionary<Type, FloatBindingMember[]> s_floatBindingMembers = new();
        private static readonly Dictionary<(Type, bool), string[]> s_conditionMemberLabels = new();
        private static readonly Dictionary<Type, string[]> s_floatBindingMemberLabels = new();

        /// <summary>
        ///     Popup labels of <see cref="BuildConditionMemberLists" /> for <paramref name="compType" />, in the same
        ///     order. The array is cached per type and shared; callers must not modify it.
        /// </summary>
        internal static string[] GetConditionMemberLabels(Type compType, bool includeMethods)
        {
            if (compType == null)
            {
                return Array.Empty<string>();
            }

            if (s_conditionMemberLabels.TryGetValue((compType, includeMethods), out string[] cached))
            {
                return cached;
            }

            ConditionMember[] members = GetConditionMembers(compType, includeMethods);
            string[] labels = new string[members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                labels[i] = members[i].Label;
            }

            s_conditionMemberLabels[(compType, includeMethods)] = labels;
            return labels;
        }

        /// <summary>
        ///     Popup labels of <see cref="BuildFloatBindingMemberLists" /> for <paramref name="compType" />, in the
        ///     same order. The array is cached per type and shared; callers must not modify it.
        /// </summary>
        internal static string[] GetFloatBindingMemberLabels(Type compType)
        {
            if (compType == null)
            {
                return Array.Empty<string>();
            }

            if (s_floatBindingMemberLabels.TryGetValue(compType, out string[] cached))
            {
                return cached;
            }

            FloatBindingMember[] members = GetFloatBindingMembers(compType);
            string[] labels = new string[members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                labels[i] = members[i].Label;
            }

            s_floatBindingMemberLabels[compType] = labels;
            return labels;
        }

        // WHY: weak keys. The owners are scene GameObject wrappers and other short-lived objects; a plain
        // dictionary kept them (and destroyed objects' wrappers) alive for as long as the editor ran.
        private static ConditionalWeakTable<object, string[]> s_popupOptions = new();

        /// <summary>
        ///     Returns popup options equal to <paramref name="items" />, reusing the array handed out last time for
        ///     the same <paramref name="owner" /> while the text is unchanged. Pickers call this on every IMGUI event;
        ///     a new array is only allocated when the options actually change. The returned array is shared and
        ///     must not be modified. The owner is held weakly, so caching never keeps it alive.
        /// </summary>
        /// <param name="owner">Stable key for one picker, e.g. the GameObject whose components are listed.</param>
        /// <param name="items">Current option text.</param>
        internal static string[] GetPopupOptions(object owner, IReadOnlyList<string> items)
        {
            if (items == null || items.Count == 0)
            {
                return Array.Empty<string>();
            }

            if (owner == null)
            {
                return CopyOptions(items);
            }

            if (s_popupOptions.TryGetValue(owner, out string[] cached))
            {
                if (SameOptions(cached, items))
                {
                    return cached;
                }

                s_popupOptions.Remove(owner);
            }

            string[] fresh = CopyOptions(items);
            s_popupOptions.Add(owner, fresh);
            return fresh;
        }

        /// <summary>
        ///     Per-picker variant of <see cref="GetPopupOptions" /> for callers that own the cache slot, such as an
        ///     inspector keeping the array in an instance field: returns <paramref name="cached" /> while it still
        ///     equals <paramref name="items" />, otherwise stores and returns a fresh copy. Never rewrites an array
        ///     that was handed out.
        /// </summary>
        internal static string[] ReuseOptions(ref string[] cached, IReadOnlyList<string> items)
        {
            if (items == null || items.Count == 0)
            {
                cached = Array.Empty<string>();
                return cached;
            }

            if (cached == null || !SameOptions(cached, items))
            {
                cached = CopyOptions(items);
            }

            return cached;
        }

        /// <summary>Forgets every array handed out by <see cref="GetPopupOptions" />.</summary>
        internal static void ClearPopupOptionsCache()
        {
            s_popupOptions = new ConditionalWeakTable<object, string[]>();
        }

        private static string[] CopyOptions(IReadOnlyList<string> items)
        {
            string[] copy = new string[items.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = items[i];
            }

            return copy;
        }

        private static bool SameOptions(string[] cached, IReadOnlyList<string> items)
        {
            if (cached.Length != items.Count)
            {
                return false;
            }

            for (int i = 0; i < cached.Length; i++)
            {
                if (!string.Equals(cached[i], items[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static ConditionMember[] GetConditionMembers(Type compType, bool includeMethods)
        {
            if (s_conditionMembers.TryGetValue((compType, includeMethods), out ConditionMember[] cached))
            {
                return cached;
            }

            List<ConditionMember> members = new();
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

                members.Add(new ConditionMember(prop.Name, vt.Value,
                    $"{prop.Name}  ({FormatConditionValueTypeKeyword(vt.Value)})  [prop]", false, ArgumentKind.Int));
            }

            foreach (FieldInfo field in compType.GetFields(ConditionMemberFlags))
            {
                ConditionValueType? vt = TryGetConditionValueType(field.FieldType);
                if (vt == null || IsUnityNoiseMember(field.Name))
                {
                    continue;
                }

                members.Add(new ConditionMember(field.Name, vt.Value,
                    $"{field.Name}  ({FormatConditionValueTypeKeyword(vt.Value)})", false, ArgumentKind.Int));
            }

            if (includeMethods)
            {
                foreach (MethodInfo method in compType.GetMethods(ConditionMemberFlags))
                {
                    if (method.IsGenericMethod || IsUnityNoiseMember(method.Name))
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
                    members.Add(new ConditionMember(method.Name, returnVt.Value,
                        $"{method.Name} ({paramLabel}) → {FormatConditionValueTypeKeyword(returnVt.Value)} [method]",
                        true, argKind.Value));
                }
            }

            ConditionMember[] result = members.ToArray();
            s_conditionMembers[(compType, includeMethods)] = result;
            return result;
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
                GameObject found = FindSceneObject(searchName.stringValue);
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

        private readonly struct SceneLookup
        {
            public readonly GameObject Result;
            public readonly bool Found;
            public readonly double At;

            public SceneLookup(GameObject result, bool found, double at)
            {
                Result = result;
                Found = found;
                At = at;
            }
        }

        /// <summary>How long a by-name scene lookup answer is reused by the pickers, in seconds.</summary>
        public const double SceneLookupCacheSeconds = 0.5;

        private const int MaxSceneLookups = 64;
        private static readonly Dictionary<string, SceneLookup> s_sceneLookups = new(StringComparer.Ordinal);

        /// <summary>
        ///     <see cref="GameObject.Find" /> for inspector pickers. One drawer used to search the whole scene up to
        ///     four times per IMGUI event (height and draw, each twice); an answer is now reused for
        ///     <see cref="SceneLookupCacheSeconds" />, and a found object that has been destroyed is searched again
        ///     right away. Editor UI only: runtime code keeps its own lookup policy.
        /// </summary>
        /// <param name="objectName">Name or path accepted by <see cref="GameObject.Find" />.</param>
        public static GameObject FindSceneObject(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            double now = EditorApplication.timeSinceStartup;
            if (s_sceneLookups.TryGetValue(objectName, out SceneLookup cached) &&
                now >= cached.At && now - cached.At < SceneLookupCacheSeconds &&
                (!cached.Found || cached.Result != null))
            {
                return cached.Found ? cached.Result : null;
            }

            GameObject found = GameObject.Find(objectName);
            if (s_sceneLookups.Count >= MaxSceneLookups)
            {
                s_sceneLookups.Clear();
            }

            s_sceneLookups[objectName] = new SceneLookup(found, found != null, now);
            return found;
        }

        /// <summary>Forgets every cached <see cref="FindSceneObject" /> answer.</summary>
        public static void ClearSceneLookupCache()
        {
            s_sceneLookups.Clear();
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
        ///     Members readable by <see cref="Neo.NoCode.ComponentFloatBinding"/> (numeric + supported reactive values).
        /// </summary>
        public static void BuildFloatBindingMemberLists(Component comp, List<string> keys, List<string> labels)
        {
            keys.Clear();
            labels.Clear();
            if (comp == null)
            {
                return;
            }

            FloatBindingMember[] members = GetFloatBindingMembers(comp.GetType());
            for (int i = 0; i < members.Length; i++)
            {
                keys.Add(members[i].Key);
                labels.Add(members[i].Label);
            }
        }

        private static FloatBindingMember[] GetFloatBindingMembers(Type t)
        {
            if (s_floatBindingMembers.TryGetValue(t, out FloatBindingMember[] cached))
            {
                return cached;
            }

            List<FloatBindingMember> members = new();
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

                members.Add(new FloatBindingMember(pi.Name,
                    $"{pi.Name}  ({FormatClrTypeDisplayName(pi.PropertyType)})  [prop]"));
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

                members.Add(new FloatBindingMember(fi.Name, $"{fi.Name}  ({FormatClrTypeDisplayName(fi.FieldType)})"));
            }

            FloatBindingMember[] result = members.ToArray();
            s_floatBindingMembers[t] = result;
            return result;
        }

        public static bool TryGetFloatBindingMemberType(Component comp, string memberName, out Type memberType)
        {
            memberType = null;
            if (comp == null || string.IsNullOrEmpty(memberName))
            {
                return false;
            }

            Type t = comp.GetType();
            PropertyInfo pi = t.GetProperty(memberName, InstanceMembers);
            if (pi != null && pi.CanRead && pi.GetIndexParameters().Length == 0 &&
                IsFloatBindingMemberType(pi.PropertyType))
            {
                memberType = pi.PropertyType;
                return true;
            }

            FieldInfo fi = t.GetField(memberName, InstanceMembers);
            if (fi != null && !fi.IsLiteral && IsFloatBindingMemberType(fi.FieldType))
            {
                memberType = fi.FieldType;
                return true;
            }

            return false;
        }

        public static bool IsReactiveFloatBindingMemberType(Type memberType)
        {
            return memberType == typeof(ReactivePropertyFloat) ||
                   memberType == typeof(ReactivePropertyInt) ||
                   memberType == typeof(ReactivePropertyBool);
        }

        private static bool IsFloatBindingMemberType(Type memberType)
        {
            if (memberType == typeof(float) || memberType == typeof(int) || memberType == typeof(double) ||
                memberType == typeof(bool))
            {
                return true;
            }

            return IsReactiveFloatBindingMemberType(memberType);
        }
    }
}
