using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public static class ComponentDrawer
    {
        /// <summary>
        ///     Processes all component-related attributes on the target object's fields.
        /// </summary>
        /// <param name="targetObject">The MonoBehaviour to process attributes for.</param>
        public static void ProcessComponentAttributes(MonoBehaviour targetObject)
        {
            if (!NeoxiderSettings.EnableAttributeSearch)
                return;

            var fields = targetObject.GetType().GetFields(CustomEditorBase.FIELD_FLAGS);

            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(targetObject);
                var isNull = fieldValue == null || (fieldValue is Object obj && obj == null);

                if (isNull)
                    ProcessNullField(field, targetObject);
                else if (IsEmptyCollection(fieldValue)) ProcessEmptyCollection(field, targetObject);
            }
        }

        /// <summary>
        ///     Processes fields that are null or have None value.
        /// </summary>
        private static void ProcessNullField(FieldInfo field, MonoBehaviour targetObject)
        {
            if (HasAttribute<FindInSceneAttribute>(field))
                AssignComponentFromScene(field, targetObject);
            else if (HasAttribute<FindAllInSceneAttribute>(field))
                AssignAllComponentsFromScene(field, targetObject);
            else if (HasAttribute<GetComponentAttribute>(field))
                AssignComponentFromGameObject(field, targetObject);
            else if (HasAttribute<GetComponentsAttribute>(field))
                AssignComponentsFromGameObject(field, targetObject);
        }

        /// <summary>
        ///     Processes empty collections (arrays or lists with zero elements).
        /// </summary>
        private static void ProcessEmptyCollection(FieldInfo field, MonoBehaviour targetObject)
        {
            if (HasAttribute<GetComponentsAttribute>(field))
                AssignComponentsFromGameObject(field, targetObject);
            else if (HasAttribute<FindAllInSceneAttribute>(field))
                AssignAllComponentsFromScene(field, targetObject);
        }

        /// <summary>
        ///     Gets all components of the specified type from the scene.
        /// </summary>
        /// <param name="componentType">The type of component to find.</param>
        /// <param name="sortMode">Optional sort mode for the results.</param>
        /// <returns>Array of found components or GameObjects.</returns>
        private static Object[] GetComponentsFromScene(Type componentType,
            FindObjectsSortMode sortMode = FindObjectsSortMode.None)
        {
            if (componentType == typeof(GameObject))
                return CustomEditorBase.FindObjectsByType<GameObject>(sortMode);
            return CustomEditorBase.FindObjectsByType(componentType, sortMode);
        }

        /// <summary>
        ///     Assigns a single component from the scene to the field.
        /// </summary>
        private static void AssignComponentFromScene(FieldInfo field, MonoBehaviour targetObject)
        {
            if (field == null || targetObject == null) return;

            var componentType = field.FieldType;
            if (!typeof(Component).IsAssignableFrom(componentType) && componentType != typeof(GameObject))
            {
                Debug.LogWarning($"Field '{field.Name}': type {componentType} is not a Component or GameObject.");
                return;
            }

            try
            {
                var components = GetComponentsFromScene(componentType);
                if (components != null && components.Length > 0)
                {
                    field.SetValue(targetObject, components[0]);
                    EditorUtility.SetDirty(targetObject);
                }
                else
                {
                    Debug.LogWarning($"No {componentType.Name} found in scene for field '{field.Name}'");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error finding {componentType.Name} in scene: {e.Message}");
            }
        }

        /// <summary>
        ///     Assigns all components of the specified type from the scene to the field.
        /// </summary>
        private static void AssignAllComponentsFromScene(FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(FindAllInSceneAttribute), false)
                .FirstOrDefault() as FindAllInSceneAttribute;
            if (attribute == null)
                return;

            var elementType = GetElementType(field);
            if (elementType == null) elementType = field.FieldType;

            if (!typeof(Component).IsAssignableFrom(elementType) && elementType != typeof(GameObject))
            {
                Debug.LogWarning($"Field '{field.Name}': type {elementType} is not a Component or GameObject.");
                return;
            }

            var components = GetComponentsFromScene(elementType, attribute.SortMode);
            SetFieldValueForCollection(field, targetObject, components);
        }

        /// <summary>
        ///     Assigns a component from the GameObject to the field.
        /// </summary>
        private static void AssignComponentFromGameObject(FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(GetComponentAttribute), false)
                .FirstOrDefault() as GetComponentAttribute;
            if (attribute == null)
                return;

            var component = attribute.SearchInChildren
                ? targetObject.GetComponentInChildren(field.FieldType)
                : targetObject.GetComponent(field.FieldType);

            if (component != null)
            {
                field.SetValue(targetObject, component);
                EditorUtility.SetDirty(targetObject);
            }
        }

        /// <summary>
        ///     Assigns all components from the GameObject to the field.
        /// </summary>
        private static void AssignComponentsFromGameObject(FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(GetComponentsAttribute), false)
                .FirstOrDefault() as GetComponentsAttribute;
            if (attribute == null)
                return;

            var elementType = GetElementType(field);
            if (elementType == null) elementType = field.FieldType;

            if (!typeof(Component).IsAssignableFrom(elementType) && elementType != typeof(GameObject))
            {
                Debug.LogWarning($"Field '{field.Name}': type {elementType} is not a Component or GameObject.");
                return;
            }

            if (elementType == typeof(GameObject))
            {
                var gameObjects = attribute.SearchInChildren
                    ? targetObject.GetComponentsInChildren<Transform>().Select(t => t.gameObject).ToArray()
                    : new[] { targetObject.gameObject };
                SetFieldValueForCollection(field, targetObject, gameObjects);
                return;
            }

            var components = attribute.SearchInChildren
                ? targetObject.GetComponentsInChildren(elementType)
                : targetObject.GetComponents(elementType);
            SetFieldValueForCollection(field, targetObject, components);
        }

        /// <summary>
        ///     Sets the field value for a collection type (array or list).
        /// </summary>
        private static void SetFieldValueForCollection(FieldInfo field, MonoBehaviour targetObject, Array value)
        {
            if (field.FieldType.IsArray)
            {
                field.SetValue(targetObject, value);
            }
            else if (typeof(IList).IsAssignableFrom(field.FieldType))
            {
                var list = value.Cast<object>().ToList();
                field.SetValue(targetObject, list);
            }
            else if (value.Length > 0)
            {
                field.SetValue(targetObject, value.GetValue(0));
            }
            else
            {
                Debug.LogWarning(
                    $"Field '{field.Name}' is neither an array nor an IList, and no components were found.");
            }

            EditorUtility.SetDirty(targetObject);
        }

        /// <summary>
        ///     Checks if a field has a specific attribute.
        /// </summary>
        private static bool HasAttribute<T>(FieldInfo field) where T : Attribute
        {
            return field.GetCustomAttributes(typeof(T), false).Length > 0;
        }

        /// <summary>
        ///     Checks if a value is an empty collection (array or list).
        /// </summary>
        private static bool IsEmptyCollection(object value)
        {
            return (value is Array arr && arr.Length == 0) ||
                   (value is IList list && list.Count == 0);
        }

        /// <summary>
        ///     Gets the element type of a collection field (array or list).
        /// </summary>
        private static Type GetElementType(FieldInfo field)
        {
            if (field.FieldType.IsArray)
                return field.FieldType.GetElementType();

            if (field.FieldType.IsGenericType)
            {
                var genericType = field.FieldType.GetGenericTypeDefinition();
                if (genericType == typeof(List<>) ||
                    genericType == typeof(IList<>) ||
                    genericType == typeof(ICollection<>) ||
                    genericType == typeof(IEnumerable<>))
                {
                    var args = field.FieldType.GetGenericArguments();
                    if (args.Length > 0)
                        return args[0];
                }
            }

            return null;
        }
    }
}