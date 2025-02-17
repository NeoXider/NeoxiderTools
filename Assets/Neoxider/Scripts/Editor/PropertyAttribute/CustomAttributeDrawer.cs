using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class CustomAttributeDrawer : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            ProcessAttributeAssignments();
        }

        /// <summary>
        /// Processes all fields on the target MonoBehaviour by checking for custom attributes
        /// and, if necessary, assigning values from the scene, GameObject or Resources.
        /// </summary>
        private void ProcessAttributeAssignments()
        {
            if (!NeoxiderSettingsWindow.EnableAttributeSearch)
                return;

            var targetObject = target as MonoBehaviour;
            if (targetObject == null)
                return;

            var fields = targetObject.GetType().GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                object fieldValue = field.GetValue(targetObject);

                // When the field is null:
                if (fieldValue == null)
                {
                    if (HasAttribute<FindInSceneAttribute>(field))
                        AssignComponentFromScene(field, targetObject);
                    else if (HasAttribute<FindAllInSceneAttribute>(field))
                        AssignAllComponentsFromScene(field, targetObject);
                    else if (HasAttribute<GetComponentAttribute>(field))
                        AssignComponentFromGameObject(field, targetObject);
                    else if (HasAttribute<GetComponentsAttribute>(field))
                        AssignComponentsFromGameObject(field, targetObject);
                    else if (HasAttribute<LoadFromResourcesAttribute>(field))
                        AssignResource(field, targetObject);
                    else if (HasAttribute<LoadAllFromResourcesAttribute>(field))
                        AssignAllResources(field, targetObject);
                }
                else // In case the collection exists but has no elements…
                {
                    if ((fieldValue is Array arr && arr.Length == 0) ||
                        (fieldValue is IList list && list.Count == 0))
                    {
                        if (HasAttribute<GetComponentsAttribute>(field))
                            AssignComponentsFromGameObject(field, targetObject);
                        else if (HasAttribute<FindAllInSceneAttribute>(field))
                            AssignAllComponentsFromScene(field, targetObject);
                        else if (HasAttribute<LoadAllFromResourcesAttribute>(field))
                            AssignAllResources(field, targetObject);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a field has an attribute of type T.
        /// </summary>
        private bool HasAttribute<T>(System.Reflection.FieldInfo field) where T : Attribute
        {
            return field.GetCustomAttributes(typeof(T), false).Length > 0;
        }

        private void AssignComponentFromScene(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
        {
            var componentType = field.FieldType;
            var component = FindFirstObjectByType(componentType);
            if (component != null)
            {
                field.SetValue(targetObject, component);
                EditorUtility.SetDirty(targetObject);
            }
        }

        private void AssignAllComponentsFromScene(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(FindAllInSceneAttribute), false)
                                 .FirstOrDefault() as FindAllInSceneAttribute;
            if (attribute == null)
                return;

            // Determine the element type. If the field isn't a collection, GetElementType returns null.
            Type elementType = GetElementType(field);
            if (elementType == null)
            {
                Debug.LogWarning($"Field '{field.Name}': type {field.FieldType} is not an array or generic IList. " +
                                   "Falling back to single component search using FindInSceneAttribute logic.");
                AssignComponentFromScene(field, targetObject);
                return;
            }
            if (!typeof(Component).IsAssignableFrom(elementType))
            {
                Debug.LogWarning($"Field '{field.Name}': element type {elementType} is not a Component.");
                return;
            }

            var components = FindObjectsByType(elementType, attribute.SortMode);
            if (components != null && components.Length > 0)
            {
                Array typedArray = Array.CreateInstance(elementType, components.Length);
                Array.Copy(components, typedArray, components.Length);

                SetFieldValueForCollection(field, targetObject, typedArray);
                EditorUtility.SetDirty(targetObject);
            }
        }

        private void AssignComponentFromGameObject(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
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

        private void AssignComponentsFromGameObject(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(GetComponentsAttribute), false)
                                 .FirstOrDefault() as GetComponentsAttribute;
            if (attribute == null)
                return;

            Type elementType = GetElementType(field);
            if (elementType == null || !typeof(Component).IsAssignableFrom(elementType))
            {
                Debug.LogWarning($"Field '{field.Name}': type {field.FieldType} is not an array or generic IList of Component.");
                return;
            }

            var components = attribute.SearchInChildren
                ? targetObject.GetComponentsInChildren(elementType)
                : targetObject.GetComponents(elementType);

            if (components != null && components.Length > 0)
            {
                Array typedArray = Array.CreateInstance(elementType, components.Length);
                Array.Copy(components, typedArray, components.Length);

                SetFieldValueForCollection(field, targetObject, typedArray);
                EditorUtility.SetDirty(targetObject);
            }
        }

        private void AssignResource(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(LoadFromResourcesAttribute), false)
                                 .FirstOrDefault() as LoadFromResourcesAttribute;
            if (attribute == null)
                return;

            var resourcePath = attribute.ResourcePath;
            var resourceType = field.FieldType;
            UnityEngine.Object resource = null;

            if (!string.IsNullOrEmpty(resourcePath))
                resource = Resources.Load(resourcePath, resourceType);

            if (resource == null)
            {
                // Fallback: find the first loaded resource of the specified type.
                var resources = Resources.FindObjectsOfTypeAll(resourceType);
                resource = resources.FirstOrDefault();
            }

            if (resource != null)
            {
                field.SetValue(targetObject, resource);
                EditorUtility.SetDirty(targetObject);
            }
            else
            {
                Debug.LogWarning($"{resourceType} for field '{field.Name}' not found at Resources path: '{resourcePath}'.");
            }
        }

        private void AssignAllResources(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(LoadAllFromResourcesAttribute), false)
                                 .FirstOrDefault() as LoadAllFromResourcesAttribute;
            if (attribute == null)
                return;

            Type elementType = GetElementType(field);
            if (elementType == null)
            {
                Debug.LogWarning($"Field '{field.Name}': unable to determine element type for resource loading.");
                return;
            }

            object[] resources;
            if (!string.IsNullOrEmpty(attribute.ResourcePath))
                resources = Resources.LoadAll(attribute.ResourcePath, elementType);
            else
                resources = Resources.FindObjectsOfTypeAll(elementType);

            if (resources != null && resources.Length > 0)
            {
                Array typedArray = Array.CreateInstance(elementType, resources.Length);
                Array.Copy(resources, typedArray, resources.Length);

                SetFieldValueForCollection(field, targetObject, typedArray);
                EditorUtility.SetDirty(targetObject);
            }
        }

        /// <summary>
        /// Helper: Determines the element type of a field that is an array or a generic collection.
        /// </summary>
        private static Type GetElementType(System.Reflection.FieldInfo field)
        {
            if (field.FieldType.IsArray)
                return field.FieldType.GetElementType();
            else if (field.FieldType.IsGenericType)
            {
                var args = field.FieldType.GetGenericArguments();
                if (args.Length > 0)
                    return args[0];
            }
            return null;
        }

        /// <summary>
        /// Helper: Sets the field value on the target based on the field's type (array or IList).
        /// </summary>
        private void SetFieldValueForCollection(System.Reflection.FieldInfo field, MonoBehaviour targetObject, Array value)
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
            else
            {
                Debug.LogWarning($"Field '{field.Name}' is neither an array nor an IList. Assignment skipped.");
            }
        }
    }
}