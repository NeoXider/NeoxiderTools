using System;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Tests
{
    /// <summary>Owns a preview scene without replacing or saving the editor's open scenes.</summary>
    internal sealed class Audit20261023NeoEditorTestsPreviewSceneScope : IDisposable
    {
        public Audit20261023NeoEditorTestsPreviewSceneScope(string path = null)
        {
            Scene = string.IsNullOrEmpty(path)
                ? EditorSceneManager.NewPreviewScene()
                : EditorSceneManager.OpenPreviewScene(path);
        }

        public Scene Scene { get; }

        public void Dispose()
        {
            if (Scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(Scene);
            }
        }
    }
}
