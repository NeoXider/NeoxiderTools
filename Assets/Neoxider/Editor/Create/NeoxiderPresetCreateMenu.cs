using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo
{
    public static class NeoxiderPresetCreateMenu
    {
        [MenuItem("GameObject/Neoxider/Presets/System/System Root (--System--)", false, -90)]
        private static void CreateSystemRoot()
        {
            CreateFromPrefab("Prefabs/-System--.prefab");
        }

        [MenuItem("GameObject/Neoxider/Presets/Combat/Simple Weapon", false, -70)]
        private static void CreateSimpleWeapon()
        {
            CreateFromPrefab("Prefabs/Simple Weapon.prefab");
        }

        [MenuItem("GameObject/Neoxider/Presets/Combat/Bullet", false, -69)]
        private static void CreateBullet()
        {
            CreateFromPrefab("Prefabs/Bullet.prefab");
        }

        [MenuItem("GameObject/Neoxider/Presets/Player/Player (First Person Controller)", false, -50)]
        private static void CreatePlayerPreset()
        {
            CreateFromPrefab("Prefabs/Tools/First Person Controller.prefab");
        }

        [MenuItem("GameObject/Neoxider/Presets/Player/First Person Controller", false, -49)]
        private static void CreateFirstPersonController()
        {
            CreateFromPrefab("Prefabs/Tools/First Person Controller.prefab");
        }

        [MenuItem("GameObject/Neoxider/Presets/Interaction/Interactive Sphere", false, -30)]
        private static void CreateInteractiveSphere()
        {
            CreateFromPrefab("Prefabs/Tools/Interact/Interactive Sphere.prefab");
        }

        [MenuItem("GameObject/Neoxider/Presets/Interaction/Trigger Cube", false, -29)]
        private static void CreateTriggerCube()
        {
            CreateFromPrefab("Prefabs/Tools/Interact/Trigger Cube.prefab");
        }

        [MenuItem("GameObject/Neoxider/Presets/Interaction/Toggle Interactive", false, -28)]
        private static void CreateToggleInteractive()
        {
            CreateFromPrefab("Prefabs/Tools/Interact/Toggle Interactive.prefab");
        }

        private static void CreateFromPrefab(string relativePrefabPath)
        {
            CreatePreset(relativePrefabPath);
        }

        /// <summary>
        ///     Creates an instance of a preset prefab in the scene. Used by Create Neoxider Object window.
        /// </summary>
        public static void CreatePreset(string relativePrefabPath)
        {
            GameObject prefab = CreateMenuObject.GetResources<GameObject>(relativePrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[NeoxiderPresetCreateMenu] Prefab not found: {relativePrefabPath}");
                return;
            }

            GameObject parent = Selection.activeGameObject;
            GameObject created = Object.Instantiate(prefab, parent != null ? parent.transform : null);
            created.name = prefab.name;
            Selection.activeGameObject = created;
            Undo.RegisterCreatedObjectUndo(created, $"Create {prefab.name}");
        }
    }
}
