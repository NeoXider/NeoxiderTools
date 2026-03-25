using Neo.Editor.Windows;
using UnityEditor;

namespace Neo
{
    /// <summary>
    ///     Editor window for changing texture max size.
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
        ///     Opens the texture max size window.
        /// </summary>
        [MenuItem("Tools/Neoxider/Change Texture Max Size")]
        public static void ShowWindow()
        {
            GetWindow<TextureMaxSizeChanger>("Texture Max Size");
        }
    }
}
