using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo
{
    /// <summary>
    ///     Creates and sorts the standard set of root container objects (for example --System--, --UI--)
    ///     used to keep scenes organized. The object list is configured in the Neoxider settings window.
    /// </summary>
    public class CreateSceneHierarchy : ScriptableObject
    {
        private const string CreateMenuPath = "GameObject/Neoxider/Create Scene Hierarchy";
        private const string SortMenuPath = "GameObject/Neoxider/Sort Scene Hierarchy";

        [SerializeField] private string[] hierarchyObjects =
        {
            "System",
            "Environment",
            "UI",
            "Cameras",
            "Lights",
            "Dynamic",
            "VFX",
            "Audio"
        };

        [SerializeField] private bool sortAlphabetically = true;

        [SerializeField] private string separatorSymbols = "--";

        /// <summary>
        ///     Creates the configured container objects in the active scene, skipping ones that already exist.
        /// </summary>
        public void CreateHierarchy()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogWarning("[Neoxider] Scene containers can only be created in a scene, not in prefab mode.");
                return;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Scene Hierarchy");

            try
            {
                foreach (string decoratedName in GetDecoratedNames())
                {
                    if (FindContainer(decoratedName) != null)
                    {
                        continue;
                    }

                    GameObject container = new(decoratedName);
                    Undo.RegisterCreatedObjectUndo(container, "Create Scene Container");
                }

                if (sortAlphabetically)
                {
                    SortContainers();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Neoxider] Failed to create scene hierarchy: {e.Message}");
                Undo.RevertAllDownToGroup(group);
            }

            Undo.CollapseUndoOperations(group);
        }

        /// <summary>
        ///     Sorts the existing container objects alphabetically among the scene roots.
        /// </summary>
        public void SortHierarchy()
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Sort Scene Hierarchy");
            SortContainers();
            Undo.CollapseUndoOperations(group);
        }

        private void SortContainers()
        {
            // WHY: Sorting assigns root sibling indices 0..N of the ACTIVE scene — nested or
            // additively-loaded containers must be left alone (deep FindContainer is only for
            // the "already exists" check).
            UnityEngine.SceneManagement.Scene activeScene =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            List<GameObject> containers = GetDecoratedNames()
                .Select(FindContainer)
                .Where(container => container != null &&
                                    container.transform.parent == null &&
                                    container.scene == activeScene)
                .OrderBy(container => container.name, StringComparer.Ordinal)
                .ToList();

            for (int i = 0; i < containers.Count; i++)
            {
                Undo.SetSiblingIndex(containers[i].transform, i, "Sort Scene Containers");
            }
        }

        private bool HasAnyContainer()
        {
            return GetDecoratedNames().Any(name => FindContainer(name) != null);
        }

        private IEnumerable<string> GetDecoratedNames()
        {
            return hierarchyObjects
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => $"{separatorSymbols}{name}{separatorSymbols}");
        }

        /// <summary>
        ///     Finds a container by name in any loaded scene, at any depth, including inactive objects,
        ///     so grouped or additively-loaded containers are not duplicated.
        /// </summary>
        private static GameObject FindContainer(string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Transform match = FindInHierarchy(root.transform, name);
                    if (match != null)
                    {
                        return match.gameObject;
                    }
                }
            }

            return null;
        }

        private static Transform FindInHierarchy(Transform current, string name)
        {
            if (current.name == name)
            {
                return current;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                Transform match = FindInHierarchy(current.GetChild(i), name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        #region Editor Menu Items

        [MenuItem(CreateMenuPath, false, 80)]
        private static void CreateHierarchyMenuItem()
        {
            NeoxiderSettings.SceneHierarchy.CreateHierarchy();
        }

        [MenuItem(CreateMenuPath, true)]
        private static bool ValidateCreateHierarchyMenuItem()
        {
            return PrefabStageUtility.GetCurrentPrefabStage() == null;
        }

        [MenuItem(SortMenuPath, false, 81)]
        private static void SortHierarchyMenuItem()
        {
            NeoxiderSettings.SceneHierarchy.SortHierarchy();
        }

        private static bool _cachedHasContainer;
        private static double _hasContainerCheckedAt = -10.0;

        [MenuItem(SortMenuPath, true)]
        private static bool ValidateSortHierarchyMenuItem()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                return false;
            }

            // WHY: Validators fire on every menu/context-menu open; the deep container scan over big
            // scenes is too costly there, so the result is cached for a second.
            double now = EditorApplication.timeSinceStartup;
            if (now - _hasContainerCheckedAt > 1.0)
            {
                _hasContainerCheckedAt = now;
                _cachedHasContainer = NeoxiderSettings.SceneHierarchy.HasAnyContainer();
            }

            return _cachedHasContainer;
        }

        #endregion
    }
}
