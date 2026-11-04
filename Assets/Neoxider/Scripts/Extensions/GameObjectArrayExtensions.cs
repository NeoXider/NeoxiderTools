using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for collections of GameObjects and Components for batch operations.
    /// </summary>
    /// <remarks>
    ///     Null and destroyed entries are skipped. Eager helpers do not allocate for lists and arrays; queries that
    ///     return <see cref="IEnumerable{T}" /> are lazy and evaluated when enumerated.
    /// </remarks>
    public static class GameObjectArrayExtensions
    {
        private static readonly List<Renderer> RendererBuffer = new();

        /// <summary>
        ///     Sets the active state of all GameObjects in the collection.
        /// </summary>
        public static IEnumerable<GameObject> SetActiveAll(this IEnumerable<GameObject> gameObjects, bool active)
        {
            if (gameObjects == null)
            {
                return null;
            }

            if (gameObjects is IList<GameObject> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    GameObject gameObject = list[i];
                    if (gameObject != null)
                    {
                        gameObject.SetActive(active);
                    }
                }

                return gameObjects;
            }

            foreach (GameObject gameObject in gameObjects)
            {
                if (gameObject != null)
                {
                    gameObject.SetActive(active);
                }
            }

            return gameObjects;
        }

        /// <summary>
        ///     Sets the active state of all GameObjects containing the components in the collection.
        /// </summary>
        public static IEnumerable<T> SetActiveAll<T>(this IEnumerable<T> components, bool active)
            where T : Component
        {
            if (components == null)
            {
                return null;
            }

            if (components is IList<T> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    T component = list[i];
                    if (component != null)
                    {
                        component.gameObject.SetActive(active);
                    }
                }

                return components;
            }

            foreach (T component in components)
            {
                if (component != null)
                {
                    component.gameObject.SetActive(active);
                }
            }

            return components;
        }

        /// <summary>
        ///     Sets the active state of GameObjects up to a specified index in a list or array.
        /// </summary>
        public static IList<GameObject> SetActiveRange(this IList<GameObject> gameObjects, int upToIndex, bool active)
        {
            if (gameObjects == null)
            {
                return null;
            }

            int count = Mathf.Min(upToIndex, gameObjects.Count);
            for (int i = 0; i < count; i++)
            {
                if (gameObjects[i] != null)
                {
                    gameObjects[i].SetActive(active);
                }
            }

            return gameObjects;
        }

        /// <summary>
        ///     Sets the active state of a GameObject at the specified index in a list or array.
        /// </summary>
        public static GameObject SetActiveAtIndex(this IList<GameObject> gameObjects, int index, bool active = true)
        {
            if (!gameObjects.IsValidIndex(index))
            {
                NeoDiagnostics.LogWarning($"Invalid index {index} for GameObject collection.");
                return null;
            }

            GameObject gameObject = gameObjects[index];
            if (gameObject != null)
            {
                gameObject.SetActive(active);
            }

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
                NeoDiagnostics.LogWarning($"Invalid index {index} for component collection.");
                return null;
            }

            T component = components[index];
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
        /// <remarks>Lists and arrays use direct indexing; other sequences are walked up to the index without copying.</remarks>
        public static GameObject SetActiveAtIndex<T>(this IEnumerable<T> components, int index, bool active)
            where T : Component
        {
            if (components is IList<T> list)
            {
                return list.SetActiveAtIndex(index, active);
            }

            if (components != null && index >= 0)
            {
                int position = 0;
                foreach (T component in components)
                {
                    if (position++ != index)
                    {
                        continue;
                    }

                    if (component == null)
                    {
                        return null;
                    }

                    component.gameObject.SetActive(active);
                    return component.gameObject;
                }
            }

            NeoDiagnostics.LogWarning($"Invalid index {index} for component collection.");
            return null;
        }

        /// <summary>
        ///     Sets the active state of a GameObject at the specified index in any enumerable collection.
        /// </summary>
        /// <remarks>Lists and arrays use direct indexing; other sequences are walked up to the index without copying.</remarks>
        public static GameObject SetActiveAtIndex(this IEnumerable<GameObject> gameObjects, int index,
            bool active = true)
        {
            if (gameObjects is IList<GameObject> list)
            {
                return list.SetActiveAtIndex(index, active);
            }

            if (gameObjects != null && index >= 0)
            {
                int position = 0;
                foreach (GameObject gameObject in gameObjects)
                {
                    if (position++ != index)
                    {
                        continue;
                    }

                    if (gameObject != null)
                    {
                        gameObject.SetActive(active);
                    }

                    return gameObject;
                }
            }

            NeoDiagnostics.LogWarning($"Invalid index {index} for GameObject collection.");
            return null;
        }

        /// <summary>
        ///     Destroys all GameObjects in the collection.
        /// </summary>
        public static void DestroyAll(this IEnumerable<GameObject> gameObjects)
        {
            if (gameObjects == null)
            {
                return;
            }

            // WHY: ToList() copies the collection so destroying elements doesn't invalidate iteration.
            List<GameObject> snapshot = gameObjects.ToList();
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i] != null)
                {
                    Object.Destroy(snapshot[i]);
                }
            }
        }

        /// <summary>
        ///     Destroys all GameObjects containing the components in the collection.
        /// </summary>
        public static void DestroyAll<T>(this IEnumerable<T> components) where T : Component
        {
            if (components == null)
            {
                return;
            }

            List<T> snapshot = components.ToList();
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i] != null)
                {
                    Object.Destroy(snapshot[i].gameObject);
                }
            }
        }

        /// <summary>
        ///     Gets all active GameObjects from the collection (lazy).
        /// </summary>
        public static IEnumerable<GameObject> GetActiveObjects(this IEnumerable<GameObject> gameObjects)
        {
            return gameObjects?.Where(obj => obj != null && obj.activeSelf) ?? Enumerable.Empty<GameObject>();
        }

        /// <summary>
        ///     Gets components of type T from all GameObjects in the collection (lazy).
        /// </summary>
        public static IEnumerable<T> GetComponentsFromAll<T>(this IEnumerable<GameObject> gameObjects)
            where T : Component
        {
            if (gameObjects == null)
            {
                return Enumerable.Empty<T>();
            }

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
            if (gameObjects == null)
            {
                return null;
            }

            foreach (GameObject gameObject in gameObjects)
            {
                if (gameObject != null && gameObject.TryGetComponent(out T component))
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>
        ///     Sets the position of all GameObjects in the collection.
        /// </summary>
        public static void SetPositionAll(this IEnumerable<GameObject> gameObjects, Vector3 position)
        {
            if (gameObjects == null)
            {
                return;
            }

            foreach (GameObject gameObject in gameObjects)
            {
                if (gameObject != null)
                {
                    gameObject.transform.position = position;
                }
            }
        }

        /// <summary>
        ///     Finds the GameObject in the collection closest to a given position.
        /// </summary>
        /// <param name="gameObjects">The collection of GameObjects to search through.</param>
        /// <param name="position">The world position to measure distance from.</param>
        /// <returns>The closest GameObject, or null if the collection is empty.</returns>
        /// <remarks>Compares squared distances; lists and arrays are scanned by index without allocating.</remarks>
        public static GameObject FindClosest(this IEnumerable<GameObject> gameObjects, Vector3 position)
        {
            if (gameObjects == null)
            {
                return null;
            }

            GameObject closest = null;
            float minDistanceSqr = float.PositiveInfinity;
            if (gameObjects is IList<GameObject> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    GameObject candidate = list[i];
                    if (candidate != null)
                    {
                        ConsiderClosest(candidate, candidate.transform.position, position, ref closest,
                            ref minDistanceSqr);
                    }
                }

                return closest;
            }

            foreach (GameObject candidate in gameObjects)
            {
                if (candidate != null)
                {
                    ConsiderClosest(candidate, candidate.transform.position, position, ref closest, ref minDistanceSqr);
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
        /// <remarks>Compares squared distances; lists and arrays are scanned by index without allocating.</remarks>
        public static T FindClosest<T>(this IEnumerable<T> components, Vector3 position) where T : Component
        {
            if (components == null)
            {
                return null;
            }

            T closest = null;
            float minDistanceSqr = float.PositiveInfinity;
            if (components is IList<T> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    T candidate = list[i];
                    if (candidate != null)
                    {
                        ConsiderClosest(candidate, candidate.transform.position, position, ref closest,
                            ref minDistanceSqr);
                    }
                }

                return closest;
            }

            foreach (T candidate in components)
            {
                if (candidate != null)
                {
                    ConsiderClosest(candidate, candidate.transform.position, position, ref closest, ref minDistanceSqr);
                }
            }

            return closest;
        }

        /// <summary>
        ///     Filters the collection to GameObjects that are within a specified distance from a point (lazy).
        /// </summary>
        /// <returns>A new collection of GameObjects within the specified distance.</returns>
        public static IEnumerable<GameObject> WithinDistance(this IEnumerable<GameObject> gameObjects, Vector3 position,
            float distance)
        {
            if (gameObjects == null)
            {
                yield break;
            }

            float sqrDistance = distance * distance;
            foreach (GameObject go in gameObjects)
            {
                if (go != null && Vector3.SqrMagnitude(go.transform.position - position) <= sqrDistance)
                {
                    yield return go;
                }
            }
        }

        /// <summary>
        ///     Filters the collection to Components whose GameObjects are within a specified distance from a point (lazy).
        /// </summary>
        /// <returns>A new collection of Components within the specified distance.</returns>
        public static IEnumerable<T> WithinDistance<T>(this IEnumerable<T> components, Vector3 position, float distance)
            where T : Component
        {
            if (components == null)
            {
                yield break;
            }

            float sqrDistance = distance * distance;
            foreach (T component in components)
            {
                if (component != null &&
                    Vector3.SqrMagnitude(component.transform.position - position) <= sqrDistance)
                {
                    yield return component;
                }
            }
        }

        /// <summary>
        ///     Sets the parent for all GameObjects in the collection.
        /// </summary>
        /// <param name="worldPositionStays">If true, the world position of the child is preserved.</param>
        public static void SetParentAll(this IEnumerable<GameObject> gameObjects, Transform parent,
            bool worldPositionStays = true)
        {
            if (gameObjects == null)
            {
                return;
            }

            foreach (GameObject go in gameObjects)
            {
                if (go != null)
                {
                    go.transform.SetParent(parent, worldPositionStays);
                }
            }
        }

        /// <summary>
        ///     Sets the parent for all GameObjects of the components in the collection.
        /// </summary>
        /// <param name="worldPositionStays">If true, the world position of the child is preserved.</param>
        public static void SetParentAll<T>(this IEnumerable<T> components, Transform parent,
            bool worldPositionStays = true) where T : Component
        {
            if (components == null)
            {
                return;
            }

            foreach (T component in components)
            {
                if (component != null)
                {
                    component.transform.SetParent(parent, worldPositionStays);
                }
            }
        }

        /// <summary>
        ///     Calculates the average position (center of mass) of all GameObjects in the collection.
        /// </summary>
        /// <returns>The average world position, or Vector3.zero if the collection is empty.</returns>
        public static Vector3 GetAveragePosition(this IEnumerable<GameObject> gameObjects)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            if (gameObjects == null)
            {
                return sum;
            }

            if (gameObjects is IList<GameObject> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    GameObject go = list[i];
                    if (go != null)
                    {
                        sum += go.transform.position;
                        count++;
                    }
                }
            }
            else
            {
                foreach (GameObject go in gameObjects)
                {
                    if (go != null)
                    {
                        sum += go.transform.position;
                        count++;
                    }
                }
            }

            return count == 0 ? Vector3.zero : sum / count;
        }

        /// <summary>
        ///     Calculates the average position (center of mass) of all GameObjects of the components in the collection.
        /// </summary>
        /// <returns>The average world position, or Vector3.zero if the collection is empty.</returns>
        public static Vector3 GetAveragePosition<T>(this IEnumerable<T> components) where T : Component
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            if (components == null)
            {
                return sum;
            }

            if (components is IList<T> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    T component = list[i];
                    if (component != null)
                    {
                        sum += component.transform.position;
                        count++;
                    }
                }
            }
            else
            {
                foreach (T component in components)
                {
                    if (component != null)
                    {
                        sum += component.transform.position;
                        count++;
                    }
                }
            }

            return count == 0 ? Vector3.zero : sum / count;
        }

        /// <summary>
        ///     Calculates a single Bounding Box that encapsulates all enabled Renderers of active objects in the
        ///     collection of GameObjects (including children).
        /// </summary>
        /// <returns>A Bounds object that contains all renderers, or a zero-sized bounds at origin if no renderers are found.</returns>
        /// <remarks>Lists and arrays are indexed and the renderer buffer is shared, so a warm call on them does not allocate.</remarks>
        public static Bounds GetCombinedBounds(this IEnumerable<GameObject> gameObjects)
        {
            Bounds bounds = new(Vector3.zero, Vector3.zero);
            if (gameObjects == null)
            {
                return bounds;
            }

            bool hasRenderer = false;
            if (gameObjects is IList<GameObject> list)
            {
                // WHY: foreach over the IEnumerable interface boxes a List enumerator on every call.
                for (int i = 0; i < list.Count; i++)
                {
                    EncapsulateRenderers(list[i], ref bounds, ref hasRenderer);
                }

                return bounds;
            }

            foreach (GameObject go in gameObjects)
            {
                EncapsulateRenderers(go, ref bounds, ref hasRenderer);
            }

            return bounds;
        }

        /// <summary>
        ///     Calculates a single Bounding Box that encapsulates all enabled Renderers in the GameObjects of the
        ///     components in the collection (including children).
        /// </summary>
        /// <returns>A Bounds object that contains all renderers, or a zero-sized bounds at origin if no renderers are found.</returns>
        /// <remarks>Lists and arrays are indexed and the renderer buffer is shared, so a warm call on them does not allocate.</remarks>
        public static Bounds GetCombinedBounds<T>(this IEnumerable<T> components) where T : Component
        {
            Bounds bounds = new(Vector3.zero, Vector3.zero);
            if (components == null)
            {
                return bounds;
            }

            bool hasRenderer = false;
            if (components is IList<T> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    T component = list[i];
                    if (component != null)
                    {
                        EncapsulateRenderers(component.gameObject, ref bounds, ref hasRenderer);
                    }
                }

                return bounds;
            }

            foreach (T component in components)
            {
                if (component != null)
                {
                    EncapsulateRenderers(component.gameObject, ref bounds, ref hasRenderer);
                }
            }

            return bounds;
        }

        private static void ConsiderClosest<T>(T candidate, Vector3 candidatePosition, Vector3 origin, ref T closest,
            ref float minDistanceSqr) where T : class
        {
            float distanceSqr = (candidatePosition - origin).sqrMagnitude;
            // WHY: the equality branch keeps a result when every candidate is so far away that the square overflows.
            if (distanceSqr < minDistanceSqr || (closest == null && distanceSqr == minDistanceSqr))
            {
                minDistanceSqr = distanceSqr;
                closest = candidate;
            }
        }

        private static void EncapsulateRenderers(GameObject gameObject, ref Bounds bounds, ref bool hasRenderer)
        {
            if (gameObject == null)
            {
                return;
            }

            // WHY: a reused buffer instead of the array GetComponentsInChildren<T>() allocates per object.
            gameObject.GetComponentsInChildren(false, RendererBuffer);
            try
            {
                for (int i = 0; i < RendererBuffer.Count; i++)
                {
                    Renderer renderer = RendererBuffer[i];
                    if (!renderer.enabled)
                    {
                        continue;
                    }

                    if (!hasRenderer)
                    {
                        bounds = renderer.bounds;
                        hasRenderer = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }
            finally
            {
                RendererBuffer.Clear();
            }
        }
    }
}
