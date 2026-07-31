using UnityEditor;
using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     GameObject menu items that instantiate ready-made Neoxider preset prefabs.
    /// </summary>
    public static class NeoxiderPresetCreateMenu
    {
        private const string MenuRoot = "GameObject/Neoxider/Presets/";

        [MenuItem(MenuRoot + "System Root", false, 20)]
        private static void CreateSystemRoot(MenuCommand command)
        {
            CreatePreset("Prefabs/-System--.prefab", command);
        }

        [MenuItem(MenuRoot + "Character (First Person)", false, 21)]
        private static void CreateCharacterFirstPerson(MenuCommand command)
        {
            CreatePreset("Prefabs/Tools/Character Controller/Character First Person.prefab", command);
        }

        [MenuItem(MenuRoot + "Character (Third Person)", false, 22)]
        private static void CreateCharacterThirdPerson(MenuCommand command)
        {
            CreatePreset("Prefabs/Tools/Character Controller/Character Third Person.prefab", command);
        }

        [MenuItem(MenuRoot + "Character (Third Person, Animated)", false, 24)]
        private static void CreateCharacterThirdPersonAnimated(MenuCommand command)
        {
            CreatePreset("Prefabs/Tools/Character Controller/Character Third Person (Animated).prefab", command);
        }

        [MenuItem(MenuRoot + "Character (Top Down, Animated)", false, 25)]
        private static void CreateCharacterTopDownAnimated(MenuCommand command)
        {
            CreatePreset("Prefabs/Tools/Character Controller/Character Top Down (Animated).prefab", command);
        }

        [MenuItem(MenuRoot + "Character (Side Scroller, Animated)", false, 26)]
        private static void CreateCharacterSideScrollerAnimated(MenuCommand command)
        {
            CreatePreset("Prefabs/Tools/Character Controller/Character Side Scroller (Animated).prefab", command);
        }

        [MenuItem(MenuRoot + "Character (Click To Move, Animated)", false, 27)]
        private static void CreateCharacterClickToMoveAnimated(MenuCommand command)
        {
            CreatePreset("Prefabs/Tools/Character Controller/Character Click To Move (Animated).prefab", command);
        }

        [MenuItem(MenuRoot + "Moving Platform", false, 45)]
        private static void CreateMovingPlatform(MenuCommand command)
        {
            CreatePreset("Prefabs/Tools/Environment/Moving Platform.prefab", command);
        }

        [MenuItem(MenuRoot + "Legacy/First Person Controller", false, 90)]
        private static void CreateFirstPersonController(MenuCommand command)
        {
            CreatePreset("Prefabs/Tools/First Person Controller.prefab", command);
        }

        [MenuItem(MenuRoot + "Simple Weapon", false, 40)]
        private static void CreateSimpleWeapon(MenuCommand command)
        {
            CreatePreset("Prefabs/Simple Weapon.prefab", command);
        }

        [MenuItem(MenuRoot + "Bullet", false, 41)]
        private static void CreateBullet(MenuCommand command)
        {
            CreatePreset("Prefabs/Bullet.prefab", command);
        }

        [MenuItem(MenuRoot + "Interactive Sphere", false, 60)]
        private static void CreateInteractiveSphere(MenuCommand command)
        {
            CreatePreset("Prefabs/Tools/Interact/Interactive Sphere.prefab", command);
        }

        [MenuItem(MenuRoot + "Toggle Interactive", false, 61)]
        private static void CreateToggleInteractive(MenuCommand command)
        {
            CreatePreset("Prefabs/Tools/Interact/Toggle Interactive.prefab", command);
        }

        [MenuItem(MenuRoot + "Trigger Cube", false, 62)]
        private static void CreateTriggerCube(MenuCommand command)
        {
            CreatePreset("Prefabs/Tools/Interact/Trigger Cube.prefab", command);
        }

        /// <summary>
        ///     Instantiates a preset prefab (keeping the prefab link) and places it in the scene.
        ///     Also used by the Create Neoxider Object window.
        /// </summary>
        public static void CreatePreset(string relativePrefabPath, MenuCommand command = null)
        {
            // WHY: Unity invokes GameObject/ menu handlers once per selected object; act only on the
            // first invocation so one click with a multi-selection creates a single instance.
            if (command?.context != null && Selection.objects.Length > 1 &&
                command.context != Selection.objects[0])
            {
                return;
            }

            GameObject prefab = CreateMenuObject.GetResources<GameObject>(relativePrefabPath);
            if (prefab == null)
            {
                Debug.LogError(
                    $"[Neoxider] Preset prefab not found at '{CreateMenuObject.startPath}{relativePrefabPath}'.");
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"[Neoxider] Failed to instantiate preset prefab '{prefab.name}'.");
                return;
            }

            GameObject parent = command?.context as GameObject;
            if (parent == null)
            {
                parent = Selection.activeGameObject;
            }

            CreateMenuObject.PlaceInScene(instance, parent, $"Create {prefab.name}");
        }
    }
}
