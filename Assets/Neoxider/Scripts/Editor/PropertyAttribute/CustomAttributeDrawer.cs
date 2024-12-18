using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviour), true)]
public class CustomAttributeDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        ExtensionMethod();
    }

    private void ExtensionMethod()
    {
        if (!NeoxiderSettingsWindow.EnableAttributeSearch) return;

        var targetObject = target as MonoBehaviour;
        var fields = targetObject.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.GetValue(targetObject) == null)
            {
                if (HasAttribute<FindInSceneAttribute>(field))
                {
                    AssignComponentFromScene(field, targetObject);
                }
                else if (HasAttribute<FindAllInSceneAttribute>(field))
                {
                    AssignAllComponentsFromScene(field, targetObject);
                }
                else if (HasAttribute<GetComponentAttribute>(field))
                {
                    AssignComponentFromGameObject(field, targetObject);
                }
                else if (HasAttribute<GetComponentsAttribute>(field))
                {
                    AssignComponentsFromGameObject(field, targetObject);
                }
                else if (HasAttribute<LoadFromResourcesAttribute>(field))
                {
                    AssignResource(field, targetObject);
                }
                else if (HasAttribute<LoadAllFromResourcesAttribute>(field))
                {
                    AssignAllResources(field, targetObject);
                }
            }
            else
            {
                var fieldValue = field.GetValue(targetObject);

                if (fieldValue is System.Array array && array.Length == 0)
                {
                    if (HasAttribute<GetComponentsAttribute>(field))
                    {
                        AssignComponentsFromGameObject(field, targetObject);
                    }
                    else if (HasAttribute<FindAllInSceneAttribute>(field))
                    {
                        AssignAllComponentsFromScene(field, targetObject);
                    }
                    else if (HasAttribute<LoadAllFromResourcesAttribute>(field))
                    {
                        AssignAllResources(field, targetObject);
                    }
                }
                else if (fieldValue is System.Collections.IList list && list.Count == 0)
                {
                    if (HasAttribute<GetComponentsAttribute>(field))
                    {
                        AssignComponentsFromGameObject(field, targetObject);
                    }
                    else if (HasAttribute<FindAllInSceneAttribute>(field))
                    {
                        AssignAllComponentsFromScene(field, targetObject);
                    }
                    else if (HasAttribute<LoadAllFromResourcesAttribute>(field))
                    {
                        AssignAllResources(field, targetObject);
                    }
                }
            }
        }
    }

    private bool HasAttribute<T>(System.Reflection.FieldInfo field) where T : System.Attribute
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
        var attribute = (FindAllInSceneAttribute)field.GetCustomAttributes(typeof(FindAllInSceneAttribute), false)[0];

        Type elementType = field.FieldType.IsArray
            ? field.FieldType.GetElementType()
            : field.FieldType.GetGenericArguments()[0];

        if (!typeof(Component).IsAssignableFrom(elementType))
        {
            Debug.LogWarning($"Тип {elementType} не является компонентом");
            return;
        }

        var components = FindObjectsByType(elementType, attribute.SortMode);

        if (components.Length > 0)
        {
            Array typedArray = Array.CreateInstance(elementType, components.Length);
            Array.Copy(components, typedArray, components.Length);

            if (field.GetValue(target) is System.Array array)
            {
                field.SetValue(targetObject, typedArray);
            }
            else if (field.GetValue(target) is System.Collections.IList list)
            {
                var genericList = typedArray.Cast<object>().ToList();
                field.SetValue(targetObject, genericList);
            }

            EditorUtility.SetDirty(targetObject);
        }
    }

    private void AssignComponentFromGameObject(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
    {
        var attribute = (GetComponentAttribute)field.GetCustomAttributes(typeof(GetComponentAttribute), false)[0];
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
        var attribute = (GetComponentsAttribute)field.GetCustomAttributes(typeof(GetComponentsAttribute), false)[0];

        Type elementType = field.FieldType.IsArray
        ? field.FieldType.GetElementType()
        : field.FieldType.GetGenericArguments()[0];

        if (!typeof(Component).IsAssignableFrom(elementType))
        {
            Debug.LogWarning($"Тип {elementType} не является компонентом");
            return;
        }

        var components = attribute.SearchInChildren
            ? targetObject.GetComponentsInChildren(elementType)
            : targetObject.GetComponents(elementType);

        if (components.Length > 0)
        {
            Array typedArray = Array.CreateInstance(elementType, components.Length);
            Array.Copy(components, typedArray, components.Length);

            if (field.GetValue(target) is System.Array array)
            {
                field.SetValue(targetObject, typedArray);
            }
            else if (field.GetValue(target) is System.Collections.IList list)
            {
                var genericList = typedArray.Cast<object>().ToList();
                field.SetValue(targetObject, genericList);
            }

            EditorUtility.SetDirty(targetObject);
        }
    }

    private void AssignResource(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
    {
        var attribute = (LoadFromResourcesAttribute)field.GetCustomAttributes(typeof(LoadFromResourcesAttribute), false)[0];
        var resourcePath = attribute.ResourcePath;
        var resourceType = field.FieldType;
        var resource = Resources.Load(resourcePath, resourceType);

        if (resourcePath == string.Empty && resource == null)
        {
            var resources = Resources.FindObjectsOfTypeAll(resourceType);
            if (resources.Length > 0)
                resource = resources.First();
        }

        if (resource != null)
        {
            field.SetValue(targetObject, resource);
            EditorUtility.SetDirty(targetObject);
        }
        else
        {
            Debug.LogWarning(resourceType + " <color=orange>AttributProperty</color> name = <color=red>" + field.Name + " </color><color=black> Resource not found at path: " + resourcePath);
        }
    }

    private void AssignAllResources(System.Reflection.FieldInfo field, MonoBehaviour targetObject)
    {
        var attribute = (LoadAllFromResourcesAttribute)field.GetCustomAttributes(typeof(LoadAllFromResourcesAttribute), false)[0];

        Type elementType = field.FieldType.IsArray
            ? field.FieldType.GetElementType()
            : field.FieldType.GetGenericArguments()[0];

        object[] resources;
        if (string.IsNullOrEmpty(attribute.ResourcePath))
        {
            resources = Resources.FindObjectsOfTypeAll(elementType);
        }
        else
        {
            resources = Resources.LoadAll(attribute.ResourcePath, elementType);
        }

        if (resources.Length > 0)
        {
            Array typedArray = Array.CreateInstance(elementType, resources.Length);
            Array.Copy(resources, typedArray, resources.Length);

            if (field.GetValue(target) is System.Array array)
            {
                field.SetValue(targetObject, typedArray);
            }
            else if (field.GetValue(target) is System.Collections.IList list)
            {
                var genericList = typedArray.Cast<object>().ToList();
                field.SetValue(targetObject, genericList);
            }

            EditorUtility.SetDirty(targetObject);
        }
    }
}