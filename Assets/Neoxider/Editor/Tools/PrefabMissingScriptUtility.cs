using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Tools
{
    public static class PrefabMissingScriptUtility
    {
        private static readonly string[] DefaultRoots =
        {
            "Assets/Neoxider/Prefabs",
            "Assets/Neoxider/Samples"
        };

        [MenuItem("Neoxider/Tools/Repair Missing Scripts In Prefabs", false, 121)]
        public static void RepairDefaultPrefabRoots()
        {
            int repaired = RepairPrefabRoots(DefaultRoots);
            Debug.Log($"[Neoxider] Repaired missing scripts in {repaired} prefab(s).");
        }

        [MenuItem("Neoxider/Tools/Repair Missing Scripts In Open Scene", false, 122)]
        public static void RepairOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("[Neoxider] No valid active scene to repair.");
                return;
            }

            int removed = RemoveMissingScriptsInScene(scene);
            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[Neoxider] Removed {removed} missing script component(s) from scene '{scene.path}'.");
        }

        public static int RepairPrefabRoots(params string[] roots)
        {
            if (roots == null || roots.Length == 0)
            {
                return 0;
            }

            var validRoots = new List<string>();
            for (int i = 0; i < roots.Length; i++)
            {
                if (AssetDatabase.IsValidFolder(roots[i]))
                {
                    validRoots.Add(roots[i]);
                }
            }

            if (validRoots.Count == 0)
            {
                return 0;
            }

            int repairedPrefabs = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", validRoots.ToArray());
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                int removed = RemoveMissingScriptsRecursive(prefab);
                if (removed <= 0)
                {
                    continue;
                }

                PrefabUtility.SavePrefabAsset(prefab);
                repairedPrefabs++;
            }

            if (repairedPrefabs > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return repairedPrefabs;
        }

        private static int RemoveMissingScriptsRecursive(GameObject root)
        {
            int removed = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[i].gameObject);
            }

            return removed;
        }

        private static int RemoveMissingScriptsInScene(Scene scene)
        {
            int removed = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                removed += RemoveMissingScriptsRecursive(roots[i]);
            }

            return removed;
        }
    }
}
