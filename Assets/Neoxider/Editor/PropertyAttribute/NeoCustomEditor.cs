using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Custom inspector drawer that handles automatic component and resource assignment based on attributes.
    ///     Supports finding components in scene, on GameObject, and loading from Resources.
    ///     Works with Odin Inspector by using DrawDefaultInspector when Odin is active.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class NeoCustomEditor : CustomEditorBase
    {
        // Отладка: проверяем, создается ли экземпляр
        static NeoCustomEditor()
        {
            //Debug.Log("[NeoCustomEditor] Класс загружен и зарегистрирован как CustomEditor для MonoBehaviour");
        }
        
        // Отладка: проверяем, создается ли экземпляр редактора
        public NeoCustomEditor()
        {
            //Debug.Log($"[NeoCustomEditor] Создан экземпляр редактора для {target?.GetType().Name}");
        }
        
        protected override void ProcessAttributeAssignments()
        {
            var targetObject = target as MonoBehaviour;
            if (targetObject == null)
                return;

            // Process component attributes
            ComponentDrawer.ProcessComponentAttributes(targetObject);

            // Process resource attributes
            ResourceDrawer.ProcessResourceAttributes(targetObject);
        }
    }
}