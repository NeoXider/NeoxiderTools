using System.Collections.Generic;
using System;
using UnityEngine;
using Component = UnityEngine.Component;
using System.Collections;
using System.Linq;

namespace Neo
{
    /// <summary>
    /// Extension methods for arrays and collections of GameObjects and Components
    /// </summary>
    public static class GameObjectArrayExtensions
    {
        /// <summary>
        /// Sets the active state of all GameObjects with the specified components
        /// </summary>
        public static IEnumerable<T> SetActiveAll<T>(this IEnumerable<T> components, bool active) where T : MonoBehaviour
        {
            if (components == null) return null;
            foreach (var component in components.Where(c => c != null))
            {
                component.gameObject.SetActive(active);
            }
            return components;
        }

        /// <summary>
        /// Sets the active state of all GameObjects in the array
        /// </summary>
        public static GameObject[] SetActiveAll(this GameObject[] gameObjects, bool active)
        {
            if (gameObjects == null) return null;
            foreach (var gameObject in gameObjects.Where(go => go != null))
            {
                gameObject.SetActive(active);
            }
            return gameObjects;
        }

        /// <summary>
        /// Sets the active state of GameObjects up to a specified index
        /// </summary>
        public static GameObject[] SetActiveRange(this GameObject[] gameObjects, int upToIndex, bool active)
        {
            if (gameObjects == null) return null;
            for (int i = 0; i < Mathf.Min(upToIndex, gameObjects.Length); i++)
            {
                if (gameObjects[i] != null)
                {
                    gameObjects[i].SetActive(active);
                }
            }
            return gameObjects;
        }

        /// <summary>
        /// Sets the active state of a GameObject at the specified index
        /// </summary>
        public static GameObject SetActiveAtIndex(this GameObject[] gameObjects, int index, bool active = true)
        {
            if (gameObjects == null || !IsValidIndex(gameObjects, index)) 
            {
                Debug.LogWarning($"Invalid index {index} for GameObject array.");
                return null;
            }

            var gameObject = gameObjects[index];
            if (gameObject != null)
            {
                gameObject.SetActive(active);
            }
            return gameObject;
        }

        /// <summary>
        /// Sets the active state of all GameObjects with the specified components
        /// </summary>
        public static T[] SetActiveAll<T>(this T[] components, bool active) where T : MonoBehaviour
        {
            if (components == null) return null;
            foreach (var component in components.Where(c => c != null))
            {
                component.gameObject.SetActive(active);
            }
            return components;
        }

        /// <summary>
        /// Sets the active state of a GameObject with component at the specified index
        /// </summary>
        public static GameObject SetActiveAtIndex<T>(this IEnumerable<T> components, int index, bool active) where T : MonoBehaviour
        {
            if (components == null) return null;
            
            var componentList = components.ToList();
            if (!IsValidIndex(componentList, index))
            {
                Debug.LogWarning($"Invalid index {index} for component collection.");
                return null;
            }

            var component = componentList[index];
            if (component != null)
            {
                component.gameObject.SetActive(active);
                return component.gameObject;
            }
            return null;
        }

        /// <summary>
        /// Destroys all GameObjects in the array
        /// </summary>
        public static void DestroyAll(this GameObject[] gameObjects)
        {
            if (gameObjects == null) return;
            foreach (var gameObject in gameObjects.Where(go => go != null))
            {
                GameObject.Destroy(gameObject);
            }
        }

        /// <summary>
        /// Gets all active GameObjects from the array
        /// </summary>
        public static GameObject[] GetActiveObjects(this GameObject[] gameObjects)
        {
            return gameObjects?.Where(obj => obj != null && obj.activeSelf).ToArray() ?? new GameObject[0];
        }

        /// <summary>
        /// Gets components of type T from all GameObjects in the array
        /// </summary>
        public static T[] GetComponentsFromAll<T>(this GameObject[] gameObjects) where T : Component
        {
            if (gameObjects == null) return new T[0];
            return gameObjects
                .Where(obj => obj != null)
                .Select(obj => obj.GetComponent<T>())
                .Where(component => component != null)
                .ToArray();
        }

        /// <summary>
        /// Gets the first component of type T from the GameObject array
        /// </summary>
        public static T GetFirstComponentFromAll<T>(this GameObject[] gameObjects) where T : Component
        {
            return gameObjects?
                .Where(obj => obj != null)
                .Select(obj => obj.GetComponent<T>())
                .FirstOrDefault(component => component != null);
        }

        /// <summary>
        /// Sets the position of all GameObjects in the array
        /// </summary>
        public static void SetPositionAll(this GameObject[] gameObjects, Vector3 position)
        {
            if (gameObjects == null) return;
            foreach (var gameObject in gameObjects.Where(go => go != null))
            {
                gameObject.transform.position = position;
            }
        }

        // Helper method for index validation
        private static bool IsValidIndex<T>(IList<T> collection, int index)
        {
            return collection != null && index >= 0 && index < collection.Count;
        }
    }
}