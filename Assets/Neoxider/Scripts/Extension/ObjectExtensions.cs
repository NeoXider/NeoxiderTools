using System;
using System.Collections.Generic;
using UnityEngine;
using Component = UnityEngine.Component;

public static class ObjectExtensions
{
    public static void SetActiveAll(this GameObject[] gameObjects, bool active)
    {
        if (gameObjects != null)
        {
            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (gameObjects[i] != null)
                {
                    gameObjects[i].SetActive(active);
                }
            }
        }
    }

    public static GameObject SetActiveId(this GameObject[] gameObjects, int id, bool activ = true)
    {
        if (id >= 0 && id < gameObjects.Length)
        {
            if (gameObjects[id] != null)
            {
                gameObjects[id].SetActive(activ);
                return gameObjects[id];
            }
        }
        else
        {
            Debug.LogWarning($"ID {id} вне диапазона массива.");
        }

        return null;
    }

    public static T[] SetActiveAll<T>(this T[] components, bool activ) where T : MonoBehaviour
    {
        foreach (var component in components)
        {
            if (component != null)
            {
                component.gameObject.SetActive(activ);
            }
        }

        return components;
    }

    public static GameObject SetActiveId<T>(this T[] components, int id, bool activ) where T : MonoBehaviour
    {
        if (id >= 0 && id < components.Length)
        {
            if (components[id] != null)
            {
                components[id].gameObject.SetActive(activ);
                return components[id].gameObject;
            }
        }
        else
        {
            Debug.LogWarning($"ID {id} вне диапазона массива.");
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
