using Neo.Editor.Windows;
using UnityEditor;

/// <summary>
///     Editor window to find and remove Missing Scripts across scenes and prefabs.
/// </summary>
public class FindAndRemoveMissingScriptsWindow : EditorWindow
{
    private FindAndRemoveMissingScriptsWindowGUI _gui;

    private void OnEnable()
    {
        _gui = new FindAndRemoveMissingScriptsWindowGUI();
    }

    private void OnDisable()
    {
        _gui?.OnDisable();
    }

    private void OnGUI()
    {
        _gui?.OnGUI(this);
    }

    /// <summary>
    ///     Opens the Missing Scripts finder window.
    /// </summary>
    [MenuItem("Tools/Neoxider/Find & Remove Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow<FindAndRemoveMissingScriptsWindow>("Find Missing Scripts");
    }
}
