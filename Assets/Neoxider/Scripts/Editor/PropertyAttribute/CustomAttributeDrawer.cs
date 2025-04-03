using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo
{
    /// <summary>
    /// Custom inspector drawer that handles automatic component and resource assignment based on attributes.
    /// Supports finding components in scene, on GameObject, and loading from Resources.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class CustomAttributeDrawer : UnityEditor.Editor
    {
        // Binding flags for field reflection
        private const System.Reflection.BindingFlags FIELD_FLAGS = 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance;

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            base.OnInspectorGUI();
            
            // Process custom attributes if enabled
            ProcessAttributeAssignments();
        }

        #region Main Processing

        /// <summary>
        /// Processes all fields on the target MonoBehaviour by checking for custom attributes
        /// and, if necessary, assigning values from the scene, GameObject or Resources.
        /// </summary>
        private void ProcessAttributeAssignments()
        {
            // Skip if attribute search is disabled in settings
            if (!NeoxiderSettings.EnableAttributeSearch)
                return;

            var targetObject = target as MonoBehaviour;
            if (targetObject == null)
                return;

            // Get all fields from the target object
            var fields = targetObject.GetType().GetFields(FIELD_FLAGS);

            foreach (var field in fields)
            {
                object fieldValue = field.GetValue(targetObject);

                // Process null fields
                if (fieldValue == null)
                {
                    ProcessNullField(field, targetObject);
                }
                // Process empty collections
                else if (IsEmptyCollection(fieldValue))
                {
                    ProcessEmptyCollection(field, targetObject);
                }
            }
        }

        /// <summary>
        /// Process a field that has null value
        /// </summary>
        private void ProcessNullField(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
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

        /// <summary>
        /// Process a field that is a collection with no elements
        /// </summary>
        private void ProcessEmptyCollection(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
        {
            if (HasAttribute<GetComponentsAttribute>(field))
                AssignComponentsFromGameObject(field, targetObject);
            else if (HasAttribute<FindAllInSceneAttribute>(field))
                AssignAllComponentsFromScene(field, targetObject);
            else if (HasAttribute<LoadAllFromResourcesAttribute>(field))
                AssignAllResources(field, targetObject);
        }

        #endregion

        #region Component Assignment

        /// <summary>
        /// Finds and assigns a component from the scene to the field
        /// </summary>
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

        /// <summary>
        /// Finds and assigns all components of a type from the scene to the field
        /// </summary>
        private void AssignAllComponentsFromScene(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(FindAllInSceneAttribute), false)
                                 .FirstOrDefault() as FindAllInSceneAttribute;
            if (attribute == null)
                return;

            // Get element type for collection
            Type elementType = GetElementType(field);
            if (elementType == null)
            {
                // If not a collection, try using the field type directly
                elementType = field.FieldType;
            }

            // Validate element type is Component or GameObject
            if (!typeof(Component).IsAssignableFrom(elementType) && elementType != typeof(GameObject))
            {
                Debug.LogWarning($"Field '{field.Name}': type {elementType} is not a Component or GameObject.");
                return;
            }

            // Handle GameObject type differently
            if (elementType == typeof(GameObject))
            {
                var gameObjects = FindObjectsByType<GameObject>(attribute.SortMode);

                if (gameObjects != null && gameObjects.Length > 0)
                {
                    if (field.FieldType.IsArray)
                    {
                        // Handle array type
                        Array typedArray = Array.CreateInstance(typeof(GameObject), gameObjects.Length);
                        Array.Copy(gameObjects, typedArray, gameObjects.Length);
                        field.SetValue(targetObject, typedArray);
                    }
                    else if (typeof(IList).IsAssignableFrom(field.FieldType))
                    {
                        // Handle List type
                        var list = gameObjects.ToList();
                        field.SetValue(targetObject, list);
                    }
                    else if (gameObjects.Length > 0)
                    {
                        // Handle single GameObject
                        field.SetValue(targetObject, gameObjects[0]);
                    }

                    EditorUtility.SetDirty(targetObject);
                }
                return;
            }

            // Find and assign components
            var components = FindObjectsByType(elementType, attribute.SortMode);
            if (components != null && components.Length > 0)
            {
                if (field.FieldType.IsArray)
                {
                    // Handle array type
                    Array typedArray = Array.CreateInstance(elementType, components.Length);
                    Array.Copy(components, typedArray, components.Length);
                    field.SetValue(targetObject, typedArray);
                }
                else if (typeof(IList).IsAssignableFrom(field.FieldType))
                {
                    // Handle List type
                    var list = components.ToList();
                    field.SetValue(targetObject, list);
                }
                else if (components.Length > 0)
                {
                    // Handle single component
                    field.SetValue(targetObject, components[0]);
                }

                EditorUtility.SetDirty(targetObject);
            }
        }

        /// <summary>
        /// Gets and assigns a component from the target GameObject
        /// </summary>
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

        /// <summary>
        /// Gets and assigns all components of a type from the target GameObject
        /// </summary>
        private void AssignComponentsFromGameObject(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(GetComponentsAttribute), false)
                                 .FirstOrDefault() as GetComponentsAttribute;
            if (attribute == null)
                return;

            // Get the type of component we're looking for
            Type elementType = GetElementType(field);
            if (elementType == null)
            {
                // If not a collection, try using the field type directly
                elementType = field.FieldType;
            }

            // Validate the type is a Component or GameObject
            if (!typeof(Component).IsAssignableFrom(elementType) && elementType != typeof(GameObject))
            {
                Debug.LogWarning($"Field '{field.Name}': type {elementType} is not a Component or GameObject.");
                return;
            }

            // Handle GameObject type differently
            if (elementType == typeof(GameObject))
            {
                var gameObjects = attribute.SearchInChildren
                    ? targetObject.GetComponentsInChildren<Transform>().Select(t => t.gameObject).ToArray()
                    : new[] { targetObject.gameObject };

                if (gameObjects != null && gameObjects.Length > 0)
                {
                    if (field.FieldType.IsArray)
                    {
                        // Handle array type
                        Array typedArray = Array.CreateInstance(typeof(GameObject), gameObjects.Length);
                        Array.Copy(gameObjects, typedArray, gameObjects.Length);
                        field.SetValue(targetObject, typedArray);
                    }
                    else if (typeof(IList).IsAssignableFrom(field.FieldType))
                    {
                        // Handle List type
                        var list = gameObjects.ToList();
                        field.SetValue(targetObject, list);
                    }
                    else if (gameObjects.Length > 0)
                    {
                        // Handle single GameObject
                        field.SetValue(targetObject, gameObjects[0]);
                    }

                    EditorUtility.SetDirty(targetObject);
                }
                return;
            }

            // Get components based on search scope
            var components = attribute.SearchInChildren
                ? targetObject.GetComponentsInChildren(elementType)
                : targetObject.GetComponents(elementType);

            if (components != null && components.Length > 0)
            {
                if (field.FieldType.IsArray)
                {
                    // Handle array type
                    Array typedArray = Array.CreateInstance(elementType, components.Length);
                    Array.Copy(components, typedArray, components.Length);
                    field.SetValue(targetObject, typedArray);
                }
                else if (typeof(IList).IsAssignableFrom(field.FieldType))
                {
                    // Handle List type
                    var list = components.ToList();
                    field.SetValue(targetObject, list);
                }
                else if (components.Length > 0)
                {
                    // Handle single component
                    field.SetValue(targetObject, components[0]);
                }

                EditorUtility.SetDirty(targetObject);
            }
        }

        #endregion

        #region Resource Assignment

        /// <summary>
        /// Loads and assigns a resource to the field
        /// </summary>
        private void AssignResource(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
        {
            var attribute = field.GetCustomAttributes(typeof(LoadFromResourcesAttribute), false)
                                 .FirstOrDefault() as LoadFromResourcesAttribute;
            if (attribute == null)
                return;

            var resourcePath = attribute.ResourcePath;
            var resourceType = field.FieldType;
            UnityEngine.Object resource = null;

            // Try to load by path first
            if (!string.IsNullOrEmpty(resourcePath))
                resource = Resources.Load(resourcePath, resourceType);

            // Fallback: find first resource of type
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
                Debug.LogWarning($"{resourceType} for field '{field.Name}' not found at Resources path: '{resourcePath}'.");
            }
        }

        /// <summary>
        /// Loads and assigns all resources of a type to the field
        /// </summary>
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

        #endregion

        #region Helper Methods

        /// <summary>
        /// Determines if a field has an attribute of type T
        /// </summary>
        private bool HasAttribute<T>(System.Reflection.FieldInfo field) where T : Attribute
        {
            return field.GetCustomAttributes(typeof(T), false).Length > 0;
        }

        /// <summary>
        /// Checks if a value is a collection (array or IList) with no elements
        /// </summary>
        private bool IsEmptyCollection(object value)
        {
            return (value is Array arr && arr.Length == 0) ||
                   (value is IList list && list.Count == 0);
        }

        /// <summary>
        /// Determines the element type of a field that is an array or a generic collection
        /// </summary>
        private static Type GetElementType(System.Reflection.FieldInfo field)
        {
            // Check if it's an array
            if (field.FieldType.IsArray)
                return field.FieldType.GetElementType();
            
            // Check if it's a generic collection
            if (field.FieldType.IsGenericType)
            {
                Type genericType = field.FieldType.GetGenericTypeDefinition();
                // Check if it's a List<T> or other collection type
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

        /// <summary>
        /// Sets the field value on the target based on the field's type (array or IList)
        /// </summary>
        private void SetFieldValueForCollection(System.Reflection.FieldInfo field, MonoBehaviour targetObject, Array value)
        {
            if (field.FieldType.IsArray)
            {
                // Handle array type
                field.SetValue(targetObject, value);
            }
            else if (typeof(IList).IsAssignableFrom(field.FieldType))
            {
                // Handle List type
                var list = value.Cast<object>().ToList();
                field.SetValue(targetObject, list);
            }
            else if (value.Length > 0)
            {
                // Handle single component
                field.SetValue(targetObject, value.GetValue(0));
            }
            else
            {
                Debug.LogWarning($"Field '{field.Name}' is neither an array nor an IList, and no components were found.");
            }
        }

        #endregion
    }
}