using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for Unity Component class
    /// </summary>
    public static class ComponentExtensions
    {
        /// <summary>
        ///     Gets an existing component or adds it if it doesn't exist
        /// </summary>
        /// <typeparam name="T">Type of component</typeparam>
        /// <param name="component">Source component</param>
        /// <returns>Existing or new component of type T</returns>
        public static T GetOrAdd<T>(this Component component) where T : Component
        {
            var comp = component.GetComponent<T>();
            if (comp == null) comp = component.gameObject.AddComponent<T>();
            return comp;
        }


        /// <summary>
        ///     Gets the full hierarchy path of this component's GameObject
        /// </summary>
        /// <param name="component">Source component</param>
        /// <returns>Full path in format "Parent/Child/GameObject"</returns>
        public static string GetPath(this Component component)
        {
            if (component == null || component.gameObject == null)
                return string.Empty;

            return GetGameObjectPath(component.gameObject);
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            var path = obj.name;
            var parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}