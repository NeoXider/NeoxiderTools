using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor
{
    /// <summary>
    ///     Handles component-related functionality in the custom editor.
    ///     Provides methods for finding and assigning components based on attributes.
    /// </summary>
    /// <remarks>
    ///     Only fields carrying <see cref="FindInSceneAttribute" />, <see cref="FindAllInSceneAttribute" />,
    ///     <see cref="GetComponentAttribute" /> or <see cref="GetComponentsAttribute" /> are read; the list is resolved once
    ///     per type. Assignments are recorded with Undo (prefab instances keep them as overrides) and every diagnostic
    ///     is reported once per object and field until the problem is resolved.
    /// </remarks>
    public static class ComponentDrawer
    {
        private static readonly InjectionDiagnostics Diagnostics = new();

        /// <summary>
        ///     Processes all component-related attributes on the target object's fields.
        /// </summary>
        /// <param name="targetObject">The MonoBehaviour to process attributes for.</param>
        public static void ProcessComponentAttributes(MonoBehaviour targetObject)
        {
            ProcessComponentAttributes(targetObject, null);
        }

        /// <summary>
        ///     Inspector entry point: like <see cref="ProcessComponentAttributes(MonoBehaviour)" />, but a lookup that
        ///     found nothing is not repeated until the hierarchy changes or <paramref name="throttle" /> allows it.
        /// </summary>
        internal static void ProcessComponentAttributes(MonoBehaviour targetObject, InjectionLookupThrottle throttle)
        {
            ProcessComponentAttributes(targetObject, throttle,
                throttle != null ? EditorApplication.timeSinceStartup : 0.0);
        }

        /// <summary>
        ///     Throttled pass at the editor time <paramref name="now" />; unresolved lookups are deferred from that time.
        /// </summary>
        internal static void ProcessComponentAttributes(MonoBehaviour targetObject, InjectionLookupThrottle throttle,
            double now)
        {
            if (targetObject == null || !NeoxiderSettings.EnableAttributeSearch)
            {
                return;
            }

            InjectedField[] fields = InjectedFieldCache.GetComponentFields(targetObject.GetType());
            if (fields.Length == 0)
            {
                return;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                InjectedField injected = fields[i];
                object fieldValue = injected.Field.GetValue(targetObject);
                bool isNull = IsMissing(fieldValue);
                if (!isNull && !IsEmptyCollection(fieldValue))
                {
                    continue;
                }

                if (throttle != null && !throttle.ShouldTry(targetObject, injected.Field, now))
                {
                    continue;
                }

                if (isNull)
                {
                    ProcessNullField(injected, targetObject);
                }
                else
                {
                    ProcessEmptyCollection(injected, targetObject);
                }

                if (throttle != null)
                {
                    object after = injected.Field.GetValue(targetObject);
                    if (IsMissing(after) || IsEmptyCollection(after))
                    {
                        throttle.Defer(targetObject, injected.Field, now);
                    }
                }
            }
        }

        /// <summary>True for a null reference and for a destroyed Unity object ("fake null").</summary>
        internal static bool IsMissing(object value)
        {
            return value == null || (value is Object obj && obj == null);
        }

        private static void ProcessNullField(InjectedField injected, MonoBehaviour targetObject)
        {
            if (injected.FindInScene != null)
            {
                AssignComponentFromScene(injected.Field, targetObject);
            }
            else if (injected.FindAllInScene != null)
            {
                AssignAllComponentsFromScene(injected.Field, injected.FindAllInScene, targetObject);
            }
            else if (injected.GetComponent != null)
            {
                AssignComponentFromGameObject(injected.Field, injected.GetComponent, targetObject);
            }
            else if (injected.GetComponents != null)
            {
                AssignComponentsFromGameObject(injected.Field, injected.GetComponents, targetObject);
            }
        }

        private static void ProcessEmptyCollection(InjectedField injected, MonoBehaviour targetObject)
        {
            if (injected.GetComponents != null)
            {
                AssignComponentsFromGameObject(injected.Field, injected.GetComponents, targetObject);
            }
            else if (injected.FindAllInScene != null)
            {
                AssignAllComponentsFromScene(injected.Field, injected.FindAllInScene, targetObject);
            }
        }

        private static Object[] GetComponentsFromScene(Type componentType,
            FindObjectsSortMode sortMode = FindObjectsSortMode.None)
        {
            if (componentType == typeof(GameObject))
            {
                return CustomEditorBase.FindObjectsByType<GameObject>(sortMode);
            }

            return CustomEditorBase.FindObjectsByType(componentType, sortMode);
        }

        private static void AssignComponentFromScene(FieldInfo field, MonoBehaviour targetObject)
        {
            if (field == null || targetObject == null)
            {
                return;
            }

            Type componentType = field.FieldType;
            if (!IsComponentOrGameObject(componentType))
            {
                WarnNotComponent(field, componentType, targetObject);
                return;
            }

            try
            {
                Object candidate = FindCompatibleSceneCandidate(field, GetComponentsFromScene(componentType));
                if (candidate != null)
                {
                    AssignField(field, targetObject, candidate, "Find attributed component");
                    Diagnostics.Resolve(targetObject, field);
                }
                else if (Diagnostics.ShouldReport(targetObject, field))
                {
                    Debug.LogWarning(
                        $"No compatible {componentType.Name} found in scene for field '{field.Name}'. " +
                        "All RequireInterface constraints must be satisfied.", targetObject);
                }
            }
            catch (Exception e)
            {
                if (Diagnostics.ShouldReport(targetObject, field))
                {
                    Debug.LogError($"Error finding {componentType.Name} in scene: {e.Message}", targetObject);
                }
            }
        }

        internal static Object FindCompatibleSceneCandidate(FieldInfo field, Object[] candidates)
        {
            RequireInterface[] requirements = InjectedFieldCache.GetRequirements(field);
            foreach (Object candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                bool compatible = true;
                foreach (RequireInterface requirement in requirements)
                {
                    if (!RequireInterfaceDrawer.IsReferenceValid(candidate, requirement.RequireType))
                    {
                        compatible = false;
                        break;
                    }
                }

                if (compatible)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void AssignAllComponentsFromScene(FieldInfo field, FindAllInSceneAttribute attribute,
            MonoBehaviour targetObject)
        {
            Type elementType = GetElementType(field) ?? field.FieldType;
            if (!IsComponentOrGameObject(elementType))
            {
                WarnNotComponent(field, elementType, targetObject);
                return;
            }

            Object[] components = GetComponentsFromScene(elementType, attribute.SortMode);
            SetFieldValueForCollection(field, targetObject, components);
        }

        private static void AssignComponentFromGameObject(FieldInfo field, GetComponentAttribute attribute,
            MonoBehaviour targetObject)
        {
            Component component = attribute.SearchInChildren
                ? targetObject.GetComponentInChildren(field.FieldType)
                : targetObject.GetComponent(field.FieldType);

            if (component != null)
            {
                AssignField(field, targetObject, component, "Get attributed component");
            }
        }

        private static void AssignComponentsFromGameObject(FieldInfo field, GetComponentsAttribute attribute,
            MonoBehaviour targetObject)
        {
            Type elementType = GetElementType(field) ?? field.FieldType;
            if (!IsComponentOrGameObject(elementType))
            {
                WarnNotComponent(field, elementType, targetObject);
                return;
            }

            if (elementType == typeof(GameObject))
            {
                GameObject[] gameObjects;
                if (attribute.SearchInChildren)
                {
                    Transform[] transforms = targetObject.GetComponentsInChildren<Transform>();
                    gameObjects = new GameObject[transforms.Length];
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        gameObjects[i] = transforms[i].gameObject;
                    }
                }
                else
                {
                    gameObjects = new[] { targetObject.gameObject };
                }

                SetFieldValueForCollection(field, targetObject, gameObjects);
                return;
            }

            Component[] components = attribute.SearchInChildren
                ? targetObject.GetComponentsInChildren(elementType)
                : targetObject.GetComponents(elementType);
            SetFieldValueForCollection(field, targetObject, components);
        }

        internal static void SetFieldValueForCollection(FieldInfo field, MonoBehaviour targetObject, Array value)
        {
            // WHY: an empty search result for a field that already holds an empty collection changes nothing.
            // Assigning it anyway ran Undo.RecordObject + SetDirty on every IMGUI event and kept the scene dirty.
            if (value.Length == 0 && IsEmptyCollection(field.GetValue(targetObject)))
            {
                return;
            }

            Type elementType = GetElementType(field);
            object assignment;
            if (elementType != null && field.FieldType.IsArray)
            {
                Array typedArray = Array.CreateInstance(elementType, value.Length);
                Array.Copy(value, typedArray, value.Length);
                assignment = typedArray;
            }
            else if (elementType != null)
            {
                Type listType = typeof(List<>).MakeGenericType(elementType);
                if (!field.FieldType.IsAssignableFrom(listType))
                {
                    return;
                }

                IList list = (IList)Activator.CreateInstance(listType);
                foreach (object item in value)
                {
                    list.Add(item);
                }

                assignment = list;
            }
            else if (value.Length > 0)
            {
                assignment = value.GetValue(0);
            }
            else
            {
                if (Diagnostics.ShouldReport(targetObject, field))
                {
                    Debug.LogWarning(
                        $"Field '{field.Name}' is neither an array nor an IList, and no components were found.",
                        targetObject);
                }

                return;
            }

            AssignField(field, targetObject, assignment, "Assign attributed collection");
        }

        // WHY: Undo.RecordObject also registers the change as a prefab override; a bare SetValue + SetDirty on a
        // prefab instance was silently reverted to the prefab value on the next reload.
        internal static void AssignField(FieldInfo field, Object targetObject, object value, string undoName)
        {
            Undo.RecordObject(targetObject, undoName);
            field.SetValue(targetObject, value);
            EditorUtility.SetDirty(targetObject);
        }

        private static bool IsComponentOrGameObject(Type type)
        {
            return typeof(Component).IsAssignableFrom(type) || type == typeof(GameObject);
        }

        private static void WarnNotComponent(FieldInfo field, Type type, Object targetObject)
        {
            if (Diagnostics.ShouldReport(targetObject, field))
            {
                Debug.LogWarning($"Field '{field.Name}': type {type} is not a Component or GameObject.", targetObject);
            }
        }

        internal static bool IsEmptyCollection(object value)
        {
            return (value is Array arr && arr.Length == 0) ||
                   (value is IList list && list.Count == 0);
        }

        internal static Type GetElementType(FieldInfo field)
        {
            if (field.FieldType.IsArray)
            {
                return field.FieldType.GetElementType();
            }

            if (field.FieldType.IsGenericType)
            {
                Type genericType = field.FieldType.GetGenericTypeDefinition();
                if (genericType == typeof(List<>) ||
                    genericType == typeof(IList<>) ||
                    genericType == typeof(ICollection<>) ||
                    genericType == typeof(IEnumerable<>))
                {
                    Type[] args = field.FieldType.GetGenericArguments();
                    if (args.Length > 0)
                    {
                        return args[0];
                    }
                }
            }

            return null;
        }
    }
}
