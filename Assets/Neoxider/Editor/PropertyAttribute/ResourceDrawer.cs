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
    ///     Handles resource-related functionality in the custom editor
    /// </summary>
    public static class ResourceDrawer
    {
        public static void ProcessResourceAttributes(MonoBehaviour targetObject)
        {
            if (!NeoxiderSettings.EnableAttributeSearch)
                return;

            var fields = targetObject.GetType().GetFields(CustomEditorBase.FIELD_FLAGS);

            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(targetObject);

                if (fieldValue == null)
                    ProcessNullField(field, targetObject);
                else if (IsEmptyCollection(fieldValue)) ProcessEmptyCollection(field, targetObject);
            }
        }

        private static void ProcessNullField(FieldInfo field, MonoBehaviour targetObject)
        {
            if (HasAttribute<LoadFromResourcesAttribute>(field))
                AssignResource(field, targetObject);
            else if (HasAttribute<LoadAllFromResourcesAttribute>(field))
                AssignAllResources(field, targetObject);
        }

        private static void ProcessEmptyCollection(FieldInfo field, MonoBehaviour targetObject)
        {
            if (HasAttribute<LoadAllFromResourcesAttribute>(field))
                AssignAllResources(field, targetObject);
        }

        private static void AssignResource(FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(LoadFromResourcesAttribute), false)
                .FirstOrDefault() as LoadFromResourcesAttribute;
            if (attribute == null)
                return;

            var resourcePath = attribute.ResourcePath;
            var resourceType = field.FieldType;
            Object resource = null;

            if (!string.IsNullOrEmpty(resourcePath))
                resource = Resources.Load(resourcePath, resourceType);

            if (resource == null)
            {
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
                Debug.LogWarning(
                    $"{resourceType} for field '{field.Name}' not found at Resources path: '{resourcePath}'.");
            }
        }

        private static void AssignAllResources(FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(LoadAllFromResourcesAttribute), false)
                .FirstOrDefault() as LoadAllFromResourcesAttribute;
            if (attribute == null)
                return;

            var elementType = GetElementType(field);
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
                var typedArray = Array.CreateInstance(elementType, resources.Length);
                Array.Copy(resources, typedArray, resources.Length);

                SetFieldValueForCollection(field, targetObject, typedArray);
                EditorUtility.SetDirty(targetObject);
            }
        }

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
                    $"Field '{field.Name}' is neither an array nor an IList, and no resources were found.");
            }
        }

        private static bool HasAttribute<T>(FieldInfo field) where T : Attribute
        {
            return field.GetCustomAttributes(typeof(T), false).Length > 0;
        }

        private static bool IsEmptyCollection(object value)
        {
            return (value is Array arr && arr.Length == 0) ||
                   (value is IList list && list.Count == 0);
        }

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