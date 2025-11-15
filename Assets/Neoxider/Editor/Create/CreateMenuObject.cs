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

        [MenuItem("GameObject/Neoxider/" + "Bonus/" + nameof(TimeReward), false, 0)]
        private static void CreateTimeReward()
        {
            TimeReward script = Create<TimeReward>();
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