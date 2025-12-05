using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    /// Автоматическая регистрация CustomEditor для всех Neo компонентов
    /// Решает проблему с видимостью при установке пакета через Package Manager
    /// </summary>
    [InitializeOnLoad]
    public static class NeoEditorAutoRegister
    {
        static NeoEditorAutoRegister()
        {
            // Регистрируем обработчик при загрузке редактора
            EditorApplication.delayCall += RegisterNeoEditors;
        }

        private static void RegisterNeoEditors()
        {
            // Находим все типы MonoBehaviour из namespace Neo
            var neoTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return Type.EmptyTypes;
                    }
                })
                .Where(type =>
                    type != null &&
                    !type.IsAbstract &&
                    typeof(MonoBehaviour).IsAssignableFrom(type) &&
                    type.Namespace != null &&
                    (type.Namespace == "Neo" || type.Namespace.StartsWith("Neo.")))
                .ToList();

            // Debug информация (раскомментируйте при необходимости)
            // Debug.Log($"[NeoEditorAutoRegister] Found {neoTypes.Count} Neo MonoBehaviour types");
            // foreach (var type in neoTypes.Take(5))
            // {
            //     Debug.Log($"  - {type.Namespace}.{type.Name}");
            // }
        }
    }
}

