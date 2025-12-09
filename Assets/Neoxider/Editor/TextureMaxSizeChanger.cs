using Neo.Editor.Windows;
using UnityEditor;

namespace Neo
{
    /// <summary>
    ///     Окно редактора для изменения максимального размера текстур
    /// </summary>
    public class TextureMaxSizeChanger : EditorWindow
    {
        private TextureMaxSizeChangerGUI _gui;

        private void OnEnable()
        {
            _gui = new TextureMaxSizeChangerGUI();
        }

        private void OnGUI()
        {
            _gui?.OnGUI(this);
        }

        /// <summary>
        ///     Показывает окно изменения размера текстур
        /// </summary>
        [MenuItem("Tools/Neoxider/Change Texture Max Size")]
        public static void ShowWindow()
        {
            GetWindow<TextureMaxSizeChanger>("Texture Max Size");
        }
    }
}