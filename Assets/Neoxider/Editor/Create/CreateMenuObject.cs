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
        public static string startPath = "Assets/Neoxider/";

        public static T Create<T>() where T : MonoBehaviour
        {
            var parentObject = Selection.activeGameObject;
            var myObject = new GameObject(typeof(T).Name);
            myObject.transform.SetParent(parentObject?.transform);
            var component = myObject.AddComponent<T>();
            Selection.activeGameObject = myObject;
            return component;
        }

        public static T Create<T>(string path) where T : MonoBehaviour
        {
            var parentObject = Selection.activeGameObject;
            var component = GameObject.Instantiate(GetResources<T>(path), parentObject?.transform);
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
            var script = Create<VisualToggle>("Prefabs/UI/" + nameof(VisualToggle) + ".prefab");
        }

        [MenuItem("GameObject/Neoxider/" + "Bonus/" + nameof(TimeReward), false, 0)]
        private static void CreateTimeReward()
        {
            var script = Create<TimeReward>();
        }

        [MenuItem("GameObject/Neoxider/" + "Tools/" + nameof(ErrorLogger), false, 0)]
        public static void CreateErrorLogger()
        {
            var script = Create<ErrorLogger>("Prefabs/Tools/" + nameof(ErrorLogger) + ".prefab");
        }

        [MenuItem("GameObject/Neoxider/" + "Shop/" + nameof(Money), false, 0)]
        public static void CreateMoney()
        {
            var script = Create<Money>();
        }

        [MenuItem("GameObject/Neoxider/" + "Tools/" + nameof(TimerObject), false, 0)]
        public static void CreateTimerObject()
        {
            var script = Create<TimerObject>();
        }

        [MenuItem("GameObject/Neoxider/" + "Bonus/" + nameof(WheelFortune), false, 0)]
        public static void CreateRoulette()
        {
            var script = Create<WheelFortune>("Prefabs/UI/" + nameof(WheelFortune) + ".prefab");
        }

        [MenuItem("GameObject/Neoxider/UI/" + nameof(UIReady), false, 0)]
        public static void CreateUIReady()
        {
            var script = Create<UIReady>();
        }

        [MenuItem("GameObject/Neoxider/" + "UI/" + nameof(ButtonPrice), false, 0)]
        public static void CreateButtonPrice()
        {
            var script = Create<ButtonPrice>("Prefabs/UI/ButtonPrice.prefab");
        }

        [MenuItem("GameObject/Neoxider/" + "UI/" + nameof(UI.UI), false, 0)]
        public static void CreateSimpleUI()
        {
            var script = Create<UI.UI>();
        }

        [MenuItem("GameObject/Neoxider/" + "Shop/" + nameof(ShopItem), false, 0)]
        public static void CreateShopItem()
        {
            var script = Create<ShopItem>();
        }

        [MenuItem("GameObject/Neoxider/" + "Audio/" + nameof(AM), false, 0)]
        public static void CreateAM()
        {
            var script = Create<AM>();
        }


        [MenuItem("GameObject/Neoxider/" + "Tools/" + nameof(SwipeController), false, 0)]
        public static void CreateSwipeController()
        {
            var script = Create<SwipeController>();
        }

        [MenuItem("GameObject/Neoxider/" + "UI/" + nameof(Points), false, 0)]
        public static void CreatePoints()
        {
            var script = Create<Points>("Prefabs/UI/" + nameof(Points) + ".prefab");
        }

        [MenuItem("GameObject/Neoxider/" + "Tools/" + nameof(FPS), false, 0)]
        public static void CreateFPS()
        {
            var script = Create<FPS>();
        }

        [MenuItem("GameObject/Neoxider/" + "Bonus/" + nameof(LineRoulett), false, 0)]
        public static void CreateLineRoulett()
        {
            var script = Create<LineRoulett>("Prefabs/Bonus/" + nameof(LineRoulett) + ".prefab");
        }

        #endregion
    }
}