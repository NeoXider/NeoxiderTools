using UnityEditor;
using UnityEngine;

using Neoxider.Audio;
using Neoxider.Shop;
using Neoxider.UI;
using Neoxider.Bonus;

namespace Neoxider
{
    public class CreateMenuObject 
    {
        public static string startPath = "Assets/Neoxider/";

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
            var script = Create<TimeReward>("Prefabs/" + nameof(TimeReward) + ".prefab");
        }

        [MenuItem("GameObject/Neoxider/" + "Tools/" + nameof(ErrorLogger), false, 0)]
        public static void CreateErrorLogger()
        {
            var script = Create<ErrorLogger>("Prefabs/UI/" + nameof(ErrorLogger) + ".prefab");
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

        [MenuItem("GameObject/Neoxider/" + "UI/" + "Page/" + nameof(Page), false, 0)]
        public static void CreatePage()
        {
            var script = Create<Page>("Prefabs/UI/Page.prefab");
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

        [MenuItem("GameObject/Neoxider/" + "UI/" + "Page/" + nameof(ButtonPageSwitch), false, 0)]
        public static void CreateButtonPageSwitch()
        {
            var script = Create<ButtonPageSwitch>("Prefabs/UI/" + nameof(ButtonPageSwitch) + ".prefab");
        }

        [MenuItem("GameObject/Neoxider/" + "Shop/" + nameof(ShopItem), false, 0)]
        public static void CreateShopItem()
        {
            var script = Create<ShopItem>();
        }

        [MenuItem("GameObject/Neoxider/" + "Audio/" + nameof(AudioManager), false, 0)]
        public static void CreateAudioManager()
        {
            var script = Create<AudioManager>("Prefabs/AudioManager.prefab");
        }

        [MenuItem("GameObject/Neoxider/" + "UI/" + nameof(Toggle), false, 0)]
        public static void CreateToggle()
        {
            var script = Create<Toggle>("Prefabs/UI/" + nameof(Toggle) + ".prefab");
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

        [MenuItem("GameObject/Neoxider/" + "UI/" + "Page/" + nameof(PagesManager), false, 0)]
        public static void CreatePagesManager()
        {
            var script = Create<PagesManager>();
        }

        [MenuItem("GameObject/Neoxider/" + "Tools/" + nameof(FPS), false, 0)]
        public static void CreateFPS()
        {
            var script = Create<FPS>();
        }

        [MenuItem("GameObject/Neoxider/" + "Tools/" + nameof(LineRoulett), false, 0)]
        public static void CreateLineRoulett()
        {
            var script = Create<LineRoulett>("Prefabs/UI/" + nameof(LineRoulett) + ".prefab");
        }
        #endregion
    }
}
