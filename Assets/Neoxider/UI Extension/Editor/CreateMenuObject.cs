using UnityEditor;
using UnityEngine;

namespace Neo
{
    namespace UI
    {
        public class CreateMenuObject
        {
            public const string startPath = "Assets/Neoxider/UI Extension/Prefabs/";

            public const string createPatch = "GameObject/UI/Neoxider/";

            public static T Create<T>() where T : MonoBehaviour
            {
                GameObject parentObject = Selection.activeGameObject;
                GameObject myObject = new GameObject(typeof(T).Name);
                myObject.transform.SetParent(parentObject?.transform);
                T component = myObject.AddComponent<T>();
                Selection.activeGameObject = myObject;
                return component;
            }

            public static T Create<T>(string path) where T : MonoBehaviour
            {
                GameObject parentObject = Selection.activeGameObject;
                T component = GameObject.Instantiate(GetResources<T>(path), parentObject?.transform);
                component.name = typeof(T).Name;
                Selection.activeGameObject = component.gameObject;
                return component;
            }

            public static GameObject Create(string path)
            {
                GameObject parentObject = Selection.activeGameObject;
                GameObject obj = GameObject.Instantiate(GetResources<GameObject>(path), parentObject?.transform);
                Selection.activeGameObject = obj.gameObject;
                obj.name = GetResources<GameObject>(path).name;
                return obj;
            }

            private static GameObject CreatePrefab(string name, string path)
            {
                GameObject obj = Create(path + "/" + name + ".prefab");;
                Selection.activeObject = obj;
                return obj;
            }

            public static T GetResources<T>(string path) where T : Object
            {
                return AssetDatabase.LoadAssetAtPath<T>(startPath + path);
            }

            #region MenuItem
            [MenuItem(createPatch + "Canvas LandScape", false, 1)]
            private static void CanvasLandScape()
            {
                string nameGameObject = "Canvas LandScape";
                CreatePrefab(nameGameObject, "Canvas");
            }

            [MenuItem(createPatch + "Canvas Portait", false, 1)]
            private static void CanvasPortait()
            {
                string nameGameObject = "Canvas Portait";
                CreatePrefab(nameGameObject, "Canvas");
            }

            [MenuItem(createPatch + "Horizontal Layout", false, 1)]
            private static void HorizontalLayout()
            {
                string nameGameObject = "Horizontal Layout";
                CreatePrefab(nameGameObject, "Layout");
            }

            [MenuItem(createPatch + "Vertical Layout", false, 1)]
            private static void VerticalLayout()
            {
                string nameGameObject = "Vertical Layout";
                CreatePrefab(nameGameObject, "Layout");
            }

            [MenuItem(createPatch + "ScrollRect", false, 1)]
            private static void ScrollRect()
            {
                string nameGameObject = "ScrollRect";
                CreatePrefab(nameGameObject, "");
            }

            [MenuItem(createPatch + "Money Layout", false, 1)]
            private static void MoneyLayout()
            {
                string nameGameObject = "Money Layout";
                CreatePrefab(nameGameObject, "");
            }

            [MenuItem(createPatch + "Page", false, 1)]
            private static void Page()
            {
                string nameGameObject = "Page";
                CreatePrefab(nameGameObject, "");
            }
            #endregion
        }
    }
}