using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.UI
{
    /// <summary>
    ///     Compatibility facade for the UI Extension create API that existed before the editor menu consolidation.
    /// </summary>
    [Obsolete("Use Neo.CreateMenuObject instead.")]
    public class CreateMenuObject
    {
        public const string createPatch = "GameObject/UI/Neoxider/";

        public static string startPath => Neo.CreateMenuObject.startPath + "UI Extension/Prefabs/";

        public static T Create<T>() where T : MonoBehaviour
        {
            return Neo.CreateMenuObject.Create<T>();
        }

        public static T Create<T>(string path) where T : MonoBehaviour
        {
            T prefabComponent = GetResources<T>(path);
            if (prefabComponent == null)
            {
                Debug.LogWarning($"[Neoxider UI] Prefab component not found: {startPath}{path}");
                return null;
            }

            GameObject instance = Create(path);
            if (instance == null)
            {
                return null;
            }

            T component = instance.GetComponent<T>() ?? instance.GetComponentInChildren<T>(true);
            if (component == null)
            {
                Debug.LogWarning($"[Neoxider UI] Prefab does not contain {typeof(T).Name}: {startPath}{path}");
                return null;
            }

            component.name = typeof(T).Name;
            Selection.activeGameObject = component.gameObject;
            return component;
        }

        public static GameObject Create(string path)
        {
            string assetPath = startPath + path;
            return Neo.CreateMenuObject.CreatePrefabObject(assetPath,
                $"[Neoxider UI] Prefab not found: {assetPath}");
        }

        public static T GetResources<T>(string path) where T : Object
        {
            return Neo.CreateMenuObject.GetResources<T>("UI Extension/Prefabs/" + path);
        }
    }
}
