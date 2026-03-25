using UnityEditor;

namespace Neo.Editor.Windows
{
    /// <summary>
    ///     Base class for drawing an <see cref="EditorWindow" /> GUI.
    /// </summary>
    public abstract class EditorWindowGUI
    {
        /// <summary>
        ///     Draws the window GUI.
        /// </summary>
        /// <param name="window">Window to draw into.</param>
        public abstract void OnGUI(EditorWindow window);
    }
}
