using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for collections of GameObjects and Components for batch operations.
    /// </summary>
    public static class GameObjectArrayExtensions
    {
        /// <summary>
        ///     Sets the active state of all GameObjects in the collection.
        /// </summary>
        public static IEnumerable<GameObject> SetActiveAll(this IEnumerable<GameObject> gameObjects, bool active)
        {
            if (gameObjects == null) return null;
            foreach (var gameObject in gameObjects.Where(go => go != null)) gameObject.SetActive(active);
            return gameObjects;
        }

        /// <summary>
        ///     Sets the active state of all GameObjects containing the components in the collection.
        /// </summary>
        public static IEnumerable<T> SetActiveAll<T>(this IEnumerable<T> components, bool active)
            where T : Component
        {
            if (components == null) return null;
            foreach (var component in components.Where(c => c != null)) component.gameObject.SetActive(active);
            return components;
        }

        /// <summary>
        ///     Sets the active state of GameObjects up to a specified index in a list or array.
        /// </summary>
        public static IList<GameObject> SetActiveRange(this IList<GameObject> gameObjects, int upToIndex, bool active)
        {
            if (gameObjects == null) return null;
            for (var i = 0; i < Mathf.Min(upToIndex, gameObjects.Count); i++)
                if (gameObjects[i] != null)
                    gameObjects[i].SetActive(active);

            return gameObjects;
        }

        /// <summary>
        ///     Sets the active state of a GameObject at the specified index in a list or array.
        /// </summary>
        public static GameObject SetActiveAtIndex(this IList<GameObject> gameObjects, int index, bool active = true)
        {
            if (!gameObjects.IsValidIndex(index))
            {
                Debug.LogWarning($"Invalid index {index} for GameObject collection.");
                return null;
            }

            var gameObject = gameObjects[index];
            if (gameObject != null) gameObject.SetActive(active);
            return gameObject;
        }

        /// <summary>
        ///     Sets the active state of a GameObject with a component at the specified index in a list or array.
        /// </summary>
        public static GameObject SetActiveAtIndex<T>(this IList<T> components, int index, bool active)
            where T : Component
        {
            if (!components.IsValidIndex(index))
            {
                Debug.LogWarning($"Invalid index {index} for component collection.");
                return null;
            }

            var component = components[index];
            if (component != null)
            {
                component.gameObject.SetActive(active);
                return component.gameObject;
            }

            return null;
        }

        /// <summary>
        ///     Sets the active state of a GameObject with a component at the specified index in any enumerable collection.
        /// </summary>
        /// <remarks>Note: This method may be inefficient on large collections as it calls .ToList().</remarks>
        public static GameObject SetActiveAtIndex<T>(this IEnumerable<T> components, int index, bool active)
            where T : Component
        {
            return SetActiveAtIndex(components.ToList(), index, active);
        }

        /// <summary>
        ///     Sets the active state of a GameObject at the specified index in any enumerable collection.
        /// </summary>
        /// <remarks>Note: This method may be inefficient on large collections as it calls .ToList().</remarks>
        public static GameObject SetActiveAtIndex(this IEnumerable<GameObject> gameObjects, int index,
            bool active = true)
        {
            return SetActiveAtIndex(gameObjects.ToList(), index, active);
        }

        /// <summary>
        ///     Destroys all GameObjects in the collection.
        /// </summary>
        public static void DestroyAll(this IEnumerable<GameObject> gameObjects)
        {
            if (gameObjects == null) return;
            // ToList() is used to create a copy, avoiding issues with modifying the collection while iterating.
            foreach (var gameObject in gameObjects.ToList().Where(go => go != null)) Object.Destroy(gameObject);
        }

        /// <summary>
        ///     Destroys all GameObjects containing the components in the collection.
        /// </summary>
        public static void DestroyAll<T>(this IEnumerable<T> components) where T : Component
        {
            if (components == null) return;
            // ToList() is used to create a copy.
            foreach (var component in components.ToList().Where(c => c != null)) Object.Destroy(component.gameObject);
        }

        /// <summary>
        ///     Gets all active GameObjects from the collection.
        /// </summary>
        public static IEnumerable<GameObject> GetActiveObjects(this IEnumerable<GameObject> gameObjects)
        {
            return gameObjects?.Where(obj => obj != null && obj.activeSelf) ?? Enumerable.Empty<GameObject>();
        }

        /// <summary>
        ///     Gets components of type T from all GameObjects in the collection.
        /// </summary>
        public static IEnumerable<T> GetComponentsFromAll<T>(this IEnumerable<GameObject> gameObjects)
            where T : Component
        {
            if (gameObjects == null) return Enumerable.Empty<T>();
            return gameObjects
                .Where(obj => obj != null)
                .Select(obj => obj.GetComponent<T>())
                .Where(component => component != null);
        }

        /// <summary>
        ///     Gets the first component of type T found in the GameObject collection.
        /// </summary>
        public static T GetFirstComponentFromAll<T>(this IEnumerable<GameObject> gameObjects) where T : Component
        {
            if (gameObjects == null) return null;
            return gameObjects
                .Where(obj => obj != null)
                .Select(obj => obj.GetComponent<T>())
                .FirstOrDefault(component => component != null);
        }

        /// <summary>
        ///     Sets the position of all GameObjects in the collection.
        /// </summary>
        public static void SetPositionAll(this IEnumerable<GameObject> gameObjects, Vector3 position)
        {
            if (gameObjects == null) return;
            foreach (var gameObject in gameObjects.Where(go => go != null)) gameObject.transform.position = position;
        }

        /// <summary>
        ///     Finds the GameObject in the collection closest to a given position.
        /// </summary>
        /// <param name="gameObjects">The collection of GameObjects to search through.</param>
        /// <param name="position">The world position to measure distance from.</param>
        /// <returns>The closest GameObject, or null if the collection is empty.</returns>
        public static GameObject FindClosest(this IEnumerable<GameObject> gameObjects, Vector3 position)
        {
            GameObject closest = null;
            var minDistance = float.MaxValue;

            if (gameObjects == null) return null;

            foreach (var go in gameObjects.Where(go => go != null))
            {
                var distance = Vector3.Distance(go.transform.position, position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = go;
                }
            }

            return closest;
        }

        /// <summary>
        ///     Finds the Component in the collection whose GameObject is closest to a given position.
        /// </summary>
        /// <param name="components">The collection of Components to search through.</param>
        /// <param name="position">The world position to measure distance from.</param>
        /// <returns>The closest Component, or null if the collection is empty.</returns>
        public static T FindClosest<T>(this IEnumerable<T> components, Vector3 position) where T : Component
        {
            T closest = null;
            var minDistance = float.MaxValue;

            if (components == null) return null;

            foreach (var component in components.Where(c => c != null))
            {
                var distance = Vector3.Distance(component.transform.position, position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = component;
                }
            }

            return closest;
        }

        /// <summary>
        ///     Filters the collection to GameObjects that are within a specified distance from a point.
        /// </summary>
        /// <returns>A new collection of GameObjects within the specified distance.</returns>
        public static IEnumerable<GameObject> WithinDistance(this IEnumerable<GameObject> gameObjects, Vector3 position,
            float distance)
        {
            if (gameObjects == null) yield break;

            var sqrDistance = distance * distance;
            foreach (var go in gameObjects.Where(go => go != null))
                if (Vector3.SqrMagnitude(go.transform.position - position) <= sqrDistance)
                    yield return go;
        }

        /// <summary>
        ///     Filters the collection to Components whose GameObjects are within a specified distance from a point.
        /// </summary>
        /// <returns>A new collection of Components within the specified distance.</returns>
        public static IEnumerable<T> WithinDistance<T>(this IEnumerable<T> components, Vector3 position, float distance)
            where T : Component
        {
            if (components == null) yield break;

            var sqrDistance = distance * distance;
            foreach (var component in components.Where(c => c != null))
                if (Vector3.SqrMagnitude(component.transform.position - position) <= sqrDistance)
                    yield return component;
        }

        /// <summary>
        ///     Sets the parent for all GameObjects in the collection.
        /// </summary>
        /// <param name="worldPositionStays">If true, the world position of the child is preserved.</param>
        public static void SetParentAll(this IEnumerable<GameObject> gameObjects, Transform parent,
            bool worldPositionStays = true)
        {
            if (gameObjects == null) return;
            foreach (var go in gameObjects.Where(go => go != null)) go.transform.SetParent(parent, worldPositionStays);
        }

        /// <summary>
        ///     Sets the parent for all GameObjects of the components in the collection.
        /// </summary>
        /// <param name="worldPositionStays">If true, the world position of the child is preserved.</param>
        public static void SetParentAll<T>(this IEnumerable<T> components, Transform parent,
            bool worldPositionStays = true) where T : Component
        {
            if (components == null) return;
            foreach (var component in components.Where(c => c != null))
                component.transform.SetParent(parent, worldPositionStays);
        }

        /// <summary>
        ///     Calculates the average position (center of mass) of all GameObjects in the collection.
        /// </summary>
        /// <returns>The average world position, or Vector3.zero if the collection is empty.</returns>
        public static Vector3 GetAveragePosition(this IEnumerable<GameObject> gameObjects)
        {
            var sum = Vector3.zero;
            var count = 0;

            if (gameObjects == null) return sum;

            foreach (var go in gameObjects.Where(go => go != null))
            {
                sum += go.transform.position;
                count++;
            }

            return count == 0 ? Vector3.zero : sum / count;
        }

        /// <summary>
        ///     Calculates the average position (center of mass) of all GameObjects of the components in the collection.
        /// </summary>
        /// <returns>The average world position, or Vector3.zero if the collection is empty.</returns>
        public static Vector3 GetAveragePosition<T>(this IEnumerable<T> components) where T : Component
        {
            var sum = Vector3.zero;
            var count = 0;

            if (components == null) return sum;

            foreach (var component in components.Where(c => c != null))
            {
                sum += component.transform.position;
                count++;
            }

            return count == 0 ? Vector3.zero : sum / count;
        }

        /// <summary>
        ///     Calculates a single Bounding Box that encapsulates all Renderers in the collection of GameObjects (including
        ///     children).
        /// </summary>
        /// <returns>A Bounds object that contains all renderers, or a zero-sized bounds at origin if no renderers are found.</returns>
        public static Bounds GetCombinedBounds(this IEnumerable<GameObject> gameObjects)
        {
            if (gameObjects == null || !gameObjects.Any()) return new Bounds(Vector3.zero, Vector3.zero);

            var renderers = gameObjects
                .Where(go => go != null)
                .SelectMany(go => go.GetComponentsInChildren<Renderer>())
                .Where(r => r.enabled);

            if (!renderers.Any()) return new Bounds(Vector3.zero, Vector3.zero);

            var bounds = renderers.First().bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);

            return bounds;
        }

        /// <summary>
        ///     Calculates a single Bounding Box that encapsulates all Renderers in the GameObjects of the components in the
        ///     collection (including children).
        /// </summary>
        /// <returns>A Bounds object that contains all renderers, or a zero-sized bounds at origin if no renderers are found.</returns>
        public static Bounds GetCombinedBounds<T>(this IEnumerable<T> components) where T : Component
        {
            if (components == null) return new Bounds(Vector3.zero, Vector3.zero);

            var gameObjects = components.Where(c => c != null).Select(c => c.gameObject);
            return GetCombinedBounds(gameObjects);
        }
    }
}