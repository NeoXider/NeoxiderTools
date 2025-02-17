using UnityEngine;

namespace Neo
{
    public static class ComponentExtensions
    {
        /// <summary>
        ///receives a component or adds it if it is not on the object.    
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            T comp = component.GetComponent<T>();
            if (comp == null)
            {
                comp = component.gameObject.AddComponent<T>();
            }
            return comp;
        }
    }
} 