using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Windows
{
    /// <summary>
    /// Базовый класс для отрисовки GUI EditorWindow
    /// </summary>
    public abstract class EditorWindowGUI
    {
        /// <summary>
        /// Отрисовка GUI окна
        /// </summary>
        /// <param name="window">Окно для отрисовки</param>
        public abstract void OnGUI(EditorWindow window);
    }
}

