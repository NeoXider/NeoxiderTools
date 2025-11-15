using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     Manages the creation and organization of a standard scene hierarchy
    /// </summary>
    public class CreateSceneHierarchy : ScriptableObject
    {
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
        ///     Creates the scene hierarchy with the configured structure
        /// </summary>
        public void CreateHierarchy()
        {
            Undo.SetCurrentGroupName("Create Scene Hierarchy");
            int group = Undo.GetCurrentGroup();

            try
            {
                foreach (string objectName in hierarchyObjects)
                {
                    if (string.IsNullOrEmpty(objectName))
                    {
                        continue;
                    }

                    string decoratedName = $"{separatorSymbols}{objectName}{separatorSymbols}";
                    GameObject obj = GameObject.Find(decoratedName);
                    if (obj == null)
                    {
                        obj = new GameObject(decoratedName);
                        Undo.RegisterCreatedObjectUndo(obj, "Create Hierarchy Object");

                        // Always reset transform for new objects
                        obj.transform.position = Vector3.zero;
                        obj.transform.rotation = Quaternion.identity;
                        obj.transform.localScale = Vector3.one;
                    }
                }

                if (sortAlphabetically)
                {
                    SortHierarchyObjects();
                }

                if (ValidateHierarchy())
                {
                    Debug.Log("Scene hierarchy created successfully");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create scene hierarchy: {e.Message}");
                Undo.RevertAllDownToGroup(group);
            }

            Undo.CollapseUndoOperations(group);
        }

        /// <summary>
        ///     Sorts all root hierarchy objects alphabetically
        /// </summary>
        private void SortHierarchyObjects()
        {
            // Собираем все объекты и их индексы
            List<(GameObject obj, int originalIndex)> objectsToSort = new();

            foreach (string objectName in hierarchyObjects)
            {
                string decoratedName = $"{separatorSymbols}{objectName}{separatorSymbols}";
                GameObject obj = GameObject.Find(decoratedName);
                if (obj != null)
                {
                    objectsToSort.Add((obj, obj.transform.GetSiblingIndex()));
                }
            }

            // Сортируем по имени
            List<(GameObject obj, int originalIndex)> sortedObjects = objectsToSort
                .OrderBy(x => x.obj.name)
                .ToList();

            // Регистрируем операцию для Undo
            Undo.SetCurrentGroupName("Sort Hierarchy Objects");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                // Применяем новые индексы
                for (int i = 0; i < sortedObjects.Count; i++)
                {
                    GameObject obj = sortedObjects[i].obj;
                    if (obj != null)
                    {
                        Undo.RecordObject(obj.transform, "Change Sibling Index");
                        obj.transform.SetSiblingIndex(i);
                    }
                }

                Debug.Log("Hierarchy objects sorted successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to sort hierarchy objects: {e.Message}");
                Undo.RevertAllDownToGroup(undoGroup);
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        /// <summary>
        ///     Validates that all hierarchy objects exist in the scene
        /// </summary>
        private bool ValidateHierarchy()
        {
            bool isValid = true;
            foreach (string objectName in hierarchyObjects)
            {
                if (string.IsNullOrEmpty(objectName))
                {
                    continue;
                }

                string decoratedName = $"{separatorSymbols}{objectName}{separatorSymbols}";
                if (GameObject.Find(decoratedName) == null)
                {
                    Debug.LogError($"Failed to find hierarchy object: {decoratedName}");
                    isValid = false;
                }
            }

            return isValid;
        }

        #region Editor Menu Items

        [MenuItem("GameObject/Neoxider/Btn/Create Scene Hierarchy", false, 10)]
        private static void CreateHierarchyMenuItem()
        {
            CreateSceneHierarchy creator = CreateInstance<CreateSceneHierarchy>();
            creator.CreateHierarchy();
            DestroyImmediate(creator);
        }

        [MenuItem("GameObject/Neoxider/Btn/Sort Hierarchy Objects", false, 11)]
        private static void SortHierarchyMenuItem()
        {
            CreateSceneHierarchy creator = CreateInstance<CreateSceneHierarchy>();
            if (creator.ValidateHierarchy())
            {
                creator.SortHierarchyObjects();
            }
            else
            {
                Debug.LogWarning("Some hierarchy objects are missing. Create the full hierarchy first.");
            }

            DestroyImmediate(creator);
        }

        #endregion
    }
}