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
            var group = Undo.GetCurrentGroup();

            try
            {
                foreach (var objectName in hierarchyObjects)
                {
                    if (string.IsNullOrEmpty(objectName)) continue;

                    var decoratedName = $"{separatorSymbols}{objectName}{separatorSymbols}";
                    var obj = GameObject.Find(decoratedName);
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

                if (sortAlphabetically) SortHierarchyObjects();

                if (ValidateHierarchy()) Debug.Log("Scene hierarchy created successfully");
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
            var objectsToSort = new List<(GameObject obj, int originalIndex)>();

            foreach (var objectName in hierarchyObjects)
            {
                var decoratedName = $"{separatorSymbols}{objectName}{separatorSymbols}";
                var obj = GameObject.Find(decoratedName);
                if (obj != null) objectsToSort.Add((obj, obj.transform.GetSiblingIndex()));
            }

            // Сортируем по имени
            var sortedObjects = objectsToSort
                .OrderBy(x => x.obj.name)
                .ToList();

            // Регистрируем операцию для Undo
            Undo.SetCurrentGroupName("Sort Hierarchy Objects");
            var undoGroup = Undo.GetCurrentGroup();

            try
            {
                // Применяем новые индексы
                for (var i = 0; i < sortedObjects.Count; i++)
                {
                    var obj = sortedObjects[i].obj;
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
            var isValid = true;
            foreach (var objectName in hierarchyObjects)
            {
                if (string.IsNullOrEmpty(objectName)) continue;

                var decoratedName = $"{separatorSymbols}{objectName}{separatorSymbols}";
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
            var creator = CreateInstance<CreateSceneHierarchy>();
            creator.CreateHierarchy();
            DestroyImmediate(creator);
        }

        [MenuItem("GameObject/Neoxider/Btn/Sort Hierarchy Objects", false, 11)]
        private static void SortHierarchyMenuItem()
        {
            var creator = CreateInstance<CreateSceneHierarchy>();
            if (creator.ValidateHierarchy())
                creator.SortHierarchyObjects();
            else
                Debug.LogWarning("Some hierarchy objects are missing. Create the full hierarchy first.");
            DestroyImmediate(creator);
        }

        #endregion
    }
}