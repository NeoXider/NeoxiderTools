using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    /// Поиск ассетов по проекту (Assets + Packages). Используется в инструментах редактора
    /// (иконки скриптов, NeoLogo и т.д.), чтобы не дублировать логику при разном расположении библиотеки.
    /// </summary>
    public static class NeoxiderEditorAssets
    {
        /// <summary>
        /// Ищет первый ассет по имени и типу по всему проекту (Assets и Packages).
        /// </summary>
        /// <param name="nameOrFilter">Имя ассета или фильтр поиска (например "NeoLogo" или "NeoLogo t:Texture2D")</param>
        /// <param name="typeFilter">Опциональный фильтр типа для FindAssets, например "Texture2D", "MonoScript"</param>
        /// <returns>Путь к ассету или null</returns>
        public static string FindAssetPath(string nameOrFilter, string typeFilter = null)
        {
            string filter = string.IsNullOrEmpty(typeFilter) ? nameOrFilter : $"{nameOrFilter} t:{typeFilter}";
            string[] guids = AssetDatabase.FindAssets(filter);
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        /// <summary>
        /// Загружает первый найденный ассет по имени/фильтру по всему проекту (Assets + Packages).
        /// </summary>
        public static T FindAndLoad<T>(string nameOrFilter, string typeFilter = null) where T : Object
        {
            string path = FindAssetPath(nameOrFilter, typeFilter ?? typeof(T).Name);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        /// <summary>
        /// Ищет текстуру NeoLogo в проекте или пакетах (для иконок компонентов и т.п.).
        /// </summary>
        public static Texture2D FindNeoLogo()
        {
            return FindAndLoad<Texture2D>("NeoLogo", "Texture2D");
        }
    }
}