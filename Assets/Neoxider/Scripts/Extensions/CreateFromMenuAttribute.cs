using System;

namespace Neo
{
    /// <summary>
    ///     Помечает MonoBehaviour как доступный для быстрого создания через GameObject → Neoxider.
    ///     Редактор строит меню по рефлексии; при выборе создаётся объект с компонентом (и префабом при наличии).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CreateFromMenuAttribute : Attribute
    {
        public CreateFromMenuAttribute(string menuPath, string prefabPath = null)
        {
            MenuPath = menuPath ?? string.Empty;
            PrefabPath = string.IsNullOrEmpty(prefabPath) ? null : prefabPath;
        }

        /// <summary>Путь в подменю, например "Neoxider/UI/VisualToggle".</summary>
        public string MenuPath { get; }

        /// <summary>
        ///     Относительный путь к префабу от корня пакета, например "Prefabs/UI/VisualToggle.prefab". Если пусто —
        ///     создаётся только объект с компонентом.
        /// </summary>
        public string PrefabPath { get; }
    }
}