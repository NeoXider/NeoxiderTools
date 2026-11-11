using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor
{
    /// <summary>
    ///     Handles resource-related functionality in the custom editor
    /// </summary>
    /// <remarks>
    ///     Only fields carrying <see cref="LoadFromResourcesAttribute" /> or <see cref="LoadAllFromResourcesAttribute" />
    ///     are read; the list is resolved once per type. Assignments are recorded with Undo, and a missing resource is
    ///     reported once per object and field instead of on every inspector repaint.
    /// </remarks>
    public static class ResourceDrawer
    {
        private static readonly InjectionDiagnostics Diagnostics = new();

        /// <summary>
        ///     Loads every missing <see cref="LoadFromResourcesAttribute" /> reference and every empty
        ///     <see cref="LoadAllFromResourcesAttribute" /> collection on <paramref name="targetObject" />.
        ///     Assigned references and non-empty collections are left untouched.
        /// </summary>
        /// <param name="targetObject">The MonoBehaviour to process attributes for.</param>
        public static void ProcessResourceAttributes(MonoBehaviour targetObject)
        {
            ProcessResourceAttributes(targetObject, null);
        }

        /// <summary>
        ///     Inspector entry point: like <see cref="ProcessResourceAttributes(MonoBehaviour)" />, but a lookup that
        ///     found nothing is not repeated until the project changes or <paramref name="throttle" /> allows it
        ///     (<c>Resources.FindObjectsOfTypeAll</c> is one of the most expensive editor calls).
        /// </summary>
        internal static void ProcessResourceAttributes(MonoBehaviour targetObject, InjectionLookupThrottle throttle)
        {
            ProcessResourceAttributes(targetObject, throttle,
                throttle != null ? EditorApplication.timeSinceStartup : 0.0);
        }

        /// <summary>
        ///     Throttled pass at the editor time <paramref name="now" />; unresolved lookups are deferred from that time.
        /// </summary>
        internal static void ProcessResourceAttributes(MonoBehaviour targetObject, InjectionLookupThrottle throttle,
            double now)
        {
            if (targetObject == null || !NeoxiderSettings.EnableAttributeSearch)
            {
                return;
            }

            InjectedField[] fields = InjectedFieldCache.GetResourceFields(targetObject.GetType());
            if (fields.Length == 0)
            {
                return;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                InjectedField injected = fields[i];
                object fieldValue = injected.Field.GetValue(targetObject);
                // WHY: A field whose asset was deleted holds a "fake null" Unity wrapper (!= managed null);
                // treat it as unassigned so [LoadFromResources] can repopulate it, matching ComponentDrawer.
                bool isNull = ComponentDrawer.IsMissing(fieldValue);
                if (!isNull && !ComponentDrawer.IsEmptyCollection(fieldValue))
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
                    if (ComponentDrawer.IsMissing(after) || ComponentDrawer.IsEmptyCollection(after))
                    {
                        throttle.Defer(targetObject, injected.Field, now);
                    }
                }
            }
        }

        private static void ProcessNullField(InjectedField injected, MonoBehaviour targetObject)
        {
            if (injected.LoadFromResources != null)
            {
                AssignResource(injected.Field, injected.LoadFromResources, targetObject);
            }
            else if (injected.LoadAllFromResources != null)
            {
                AssignAllResources(injected.Field, injected.LoadAllFromResources, targetObject);
            }
        }

        private static void ProcessEmptyCollection(InjectedField injected, MonoBehaviour targetObject)
        {
            if (injected.LoadAllFromResources != null)
            {
                AssignAllResources(injected.Field, injected.LoadAllFromResources, targetObject);
            }
        }

        private static void AssignResource(FieldInfo field, LoadFromResourcesAttribute attribute,
            MonoBehaviour targetObject)
        {
            string resourcePath = attribute.ResourcePath;
            Type resourceType = field.FieldType;
            Object resource = null;

            if (!string.IsNullOrEmpty(resourcePath))
            {
                resource = Resources.Load(resourcePath, resourceType);
            }

            if (resource == null)
            {
                Object[] resources = Resources.FindObjectsOfTypeAll(resourceType);
                resource = resources.Length > 0 ? resources[0] : null;
            }

            if (resource != null)
            {
                ComponentDrawer.AssignField(field, targetObject, resource, "Load attributed resource");
                Diagnostics.Resolve(targetObject, field);
            }
            else if (Diagnostics.ShouldReport(targetObject, field))
            {
                Debug.LogWarning(
                    $"{resourceType} for field '{field.Name}' not found at Resources path: '{resourcePath}'.",
                    targetObject);
            }
        }

        private static void AssignAllResources(FieldInfo field, LoadAllFromResourcesAttribute attribute,
            MonoBehaviour targetObject)
        {
            Type elementType = ComponentDrawer.GetElementType(field);
            if (elementType == null)
            {
                if (Diagnostics.ShouldReport(targetObject, field))
                {
                    Debug.LogWarning($"Field '{field.Name}': unable to determine element type for resource loading.",
                        targetObject);
                }

                return;
            }

            object[] resources = !string.IsNullOrEmpty(attribute.ResourcePath)
                ? Resources.LoadAll(attribute.ResourcePath, elementType)
                : Resources.FindObjectsOfTypeAll(elementType);

            if (resources != null && resources.Length > 0)
            {
                Array typedArray = Array.CreateInstance(elementType, resources.Length);
                Array.Copy(resources, typedArray, resources.Length);

                ComponentDrawer.SetFieldValueForCollection(field, targetObject, typedArray);
            }
        }

    }
}
