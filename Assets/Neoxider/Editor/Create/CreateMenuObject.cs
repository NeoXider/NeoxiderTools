using System.IO;
using Neo.Audio;
using Neo.Bonus;
using Neo.Shop;
using Neo.UI;
using UnityEditor;
using UnityEngine;

namespace Neo
{
    public class CreateMenuObject
    {
        private static string _startPath;

        /// <summary>
        ///     Динамически определяет путь к корневой папке Neoxider, работая как при установке через Git, так и как обычный пакет
        /// </summary>
        public static string startPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_startPath))
                {
                    return _startPath;
                }

                // Ищем путь к скрипту через поиск по имени
                string[] guids = AssetDatabase.FindAssets("CreateMenuObject t:Script");
                string scriptPath = null;

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("Neoxider") && path.Contains("Editor/Create"))
                    {
                        scriptPath = path;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(scriptPath))
                {
                    // Определяем базовый путь на основе расположения скрипта
                    // Убираем "Editor/Create" из пути
                    string basePath = Path.GetDirectoryName(scriptPath); // Editor/Create
                    basePath = Path.GetDirectoryName(basePath); // Editor
                    basePath = Path.GetDirectoryName(basePath); // Neoxider
                    _startPath = basePath + "/";
                }
                else
                {
                    // Fallback - пробуем стандартные пути
                    string assetsPath = "Assets/Neoxider/";
                    string packagesPath = "Packages/com.neoxider.tools/";

                    // Проверяем, существует ли папка Packages
                    if (AssetDatabase.IsValidFolder("Packages/com.neoxider.tools"))
                    {
                        _startPath = packagesPath;
                    }
                    else if (AssetDatabase.IsValidFolder("Assets/Neoxider"))
                    {
                        _startPath = assetsPath;
                    }
                    else
                    {
                        // Последняя попытка - ищем любой префаб Neoxider
                        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
                        foreach (string guid in prefabGuids)
                        {
                            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (prefabPath.Contains("Neoxider"))
                            {
                                // Находим корень Neoxider
                                int neoxiderIndex = prefabPath.IndexOf("Neoxider");
                                _startPath = prefabPath.Substring(0, neoxiderIndex + "Neoxider".Length) + "/";
                                break;
                            }
                        }

                        if (string.IsNullOrEmpty(_startPath))
                        {
                            _startPath = assetsPath; // Fallback на стандартный путь
                        }
                    }
                }

                return _startPath;
            }
        }

        public static T Create<T>() where T : MonoBehaviour
        {
            GameObject parentObject = Selection.activeGameObject;
            GameObject myObject = new(typeof(T).Name);
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

        public static T GetResources<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(startPath + path);
        }

        #region MenuItem

        [MenuItem("GameObject/Neoxider/" + "UI/" + nameof(VisualToggle), false, 0)]
        private static void CreateVisualToggle()
        {
            VisualToggle script = Create<VisualToggle>("Prefabs/UI/" + nameof(VisualToggle) + ".prefab");
        }

        [MenuItem("GameObject/Neoxider/" + "Bonus/" + nameof(CooldownReward), false, 0)]
        private static void CreateCooldownReward()
        {
            CooldownReward script = Create<CooldownReward>();
        }

        [MenuItem("GameObject/Neoxider/" + "Tools/" + nameof(ErrorLogger), false, 0)]
        public static void CreateErrorLogger()
        {
            ErrorLogger script = Create<ErrorLogger>("Prefabs/Tools/" + nameof(ErrorLogger) + ".prefab");
        }

        [MenuItem("GameObject/Neoxider/" + "Shop/" + nameof(Money), false, 0)]
        public static void CreateMoney()
        {
            Money script = Create<Money>();
        }

        [MenuItem("GameObject/Neoxider/" + "Tools/" + nameof(TimerObject), false, 0)]
        public static void CreateTimerObject()
        {
            TimerObject script = Create<TimerObject>();
        }

        [MenuItem("GameObject/Neoxider/" + "Bonus/" + nameof(WheelFortune), false, 0)]
        public static void CreateRoulette()
        {
            WheelFortune script = Create<WheelFortune>("Prefabs/UI/" + nameof(WheelFortune) + ".prefab");
        }

        [MenuItem("GameObject/Neoxider/UI/" + nameof(UIReady), false, 0)]
        public static void CreateUIReady()
        {
            UIReady script = Create<UIReady>();
        }

        [MenuItem("GameObject/Neoxider/" + "UI/" + nameof(ButtonPrice), false, 0)]
        public static void CreateButtonPrice()
        {
            ButtonPrice script = Create<ButtonPrice>("Prefabs/UI/ButtonPrice.prefab");
        }

        [MenuItem("GameObject/Neoxider/" + "UI/" + nameof(UI.UI), false, 0)]
        public static void CreateSimpleUI()
        {
            UI.UI script = Create<UI.UI>();
        }

        [MenuItem("GameObject/Neoxider/" + "Shop/" + nameof(ShopItem), false, 0)]
        public static void CreateShopItem()
        {
            ShopItem script = Create<ShopItem>();
        }

        [MenuItem("GameObject/Neoxider/" + "Audio/" + nameof(AM), false, 0)]
        public static void CreateAM()
        {
            AM script = Create<AM>();
        }


        [MenuItem("GameObject/Neoxider/" + "Tools/" + nameof(SwipeController), false, 0)]
        public static void CreateSwipeController()
        {
            SwipeController script = Create<SwipeController>();
        }

        [MenuItem("GameObject/Neoxider/" + "UI/" + nameof(Points), false, 0)]
        public static void CreatePoints()
        {
            Points script = Create<Points>("Prefabs/UI/" + nameof(Points) + ".prefab");
        }

        [MenuItem("GameObject/Neoxider/" + "Tools/" + nameof(FPS), false, 0)]
        public static void CreateFPS()
        {
            FPS script = Create<FPS>();
        }

        [MenuItem("GameObject/Neoxider/" + "Bonus/" + nameof(LineRoulett), false, 0)]
        public static void CreateLineRoulett()
        {
            LineRoulett script = Create<LineRoulett>("Prefabs/Bonus/" + nameof(LineRoulett) + ".prefab");
        }

        #endregion
    }
}