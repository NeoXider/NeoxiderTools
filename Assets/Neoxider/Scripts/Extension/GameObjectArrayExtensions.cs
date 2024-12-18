using System.Collections.Generic;
using System;
using UnityEngine;
using Component = UnityEngine.Component;
using System.Collections;
using System.Linq;

namespace Neoxider
{
    public static class GameObjectArrayExtensions
    {
        public static IEnumerable<T> SetActiveAll<T>(this IEnumerable<T> components, bool active) where T : MonoBehaviour
        {
            List<T> componentList = components.ToList();

            componentList.SetActiveAll(active);

            return components;
        }

        public static GameObject[] SetActiveAll(this GameObject[] gameObjects, bool active)
        {
            if (gameObjects == null) return null;


            foreach (var gameObject in gameObjects)
            {
                if (gameObject != null)
                {
                    gameObject.SetActive(active);
                }
            }
            return gameObjects;
        }

        public static GameObject[] SetActiveAll(this GameObject[] gameObjects, int upToIndex, bool active)
        {
            if (gameObjects == null) return null;

            for (int i = 0; i < upToIndex && i < gameObjects.Length; i++)
            {
                if (gameObjects[i] != null)
                {
                    gameObjects[i].SetActive(active);
                }
            }
            return gameObjects;
        }

        public static GameObject SetActiveId(this GameObject[] gameObjects, int id, bool active)
        {
            if (gameObjects == null) return null;

            if (id >= 0 && id < gameObjects.Length)
            {
                if (gameObjects[id] != null)
                {
                    gameObjects[id].SetActive(active);
                    return gameObjects[id];
                }
            }
            else
            {
                Debug.LogWarning($"ID {id} .");
            }

            return null;
        }

        public static T[] SetActiveAll<T>(this T[] components, bool active) where T : MonoBehaviour
        {
            foreach (var component in components)
            {
                if (component != null)
                {
                    component.gameObject.SetActive(active);
                }
            }

            return components;
        }

        public static GameObject SetActiveById<T>(this IEnumerable<T> components, int id, bool active) where T : MonoBehaviour
        {
            List<T> componentList = components.ToList();

            return componentList.SetActiveById(id, active);
        }

        public static GameObject SetActiveId<T>(this T[] components, int id, bool active) where T : MonoBehaviour
        {
            if (id >= 0 && id < components.Length)
            {
                if (components[id] != null)
                {
                    components[id].gameObject.SetActive(active);
                    return components[id].gameObject;
                }
            }
            else
            {
                Debug.LogWarning($"ID {id} .");
            }

            return null;
        }

        public static void DestroyAll(this GameObject[] gameObjects)
        {
            if (gameObjects != null)
            {
                for (int i = 0; i < gameObjects.Length; i++)
                {
                    if (gameObjects[i] != null)
                    {
                        GameObject.Destroy(gameObjects[i]);
                    }
                }
            }
        }

        public static GameObject[] GetActiveObjects(this GameObject[] gameObjects)
        {
            return Array.FindAll(gameObjects, obj => obj != null && obj.activeSelf);
        }

        public static T[] GetComponentsFromAll<T>(this GameObject[] gameObjects) where T : Component
        {
            if (gameObjects == null || gameObjects.Length == 0) return new T[0];

            List<T> list = new List<T>();

            foreach (var obj in gameObjects)
            {
                if (obj != null)
                {
                    T component = obj.GetComponent<T>();

                    if (component != null)
                    {
                        list.Add(component);
                    }
                }
            }

            return list.ToArray();
        }

        public static T GetFirstComponentFromAll<T>(this GameObject[] gameObjects) where T : Component
        {
            foreach (var obj in gameObjects)
            {
                if (obj != null)
                {
                    T component = obj.GetComponent<T>();

                    if (component != null)
                    {
                        return component;
                    }
                }
            }
            return null;
        }

        public static void SetPositionAll(this GameObject[] gameObjects, Vector3 position)
        {
            if (gameObjects != null)
            {
                for (int i = 0; i < gameObjects.Length; i++)
                {
                    if (gameObjects[i] != null)
                    {
                        gameObjects[i].transform.position = position;
                    }
                }
            }
        }
    }
}