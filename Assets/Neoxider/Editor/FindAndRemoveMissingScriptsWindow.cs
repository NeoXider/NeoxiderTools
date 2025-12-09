using Neo.Editor.Windows;
using UnityEditor;

/// <summary>
///     Окно редактора для поиска и удаления Missing Scripts во всех сценах и префабах
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
    ///     Показывает окно поиска Missing Scripts
    /// </summary>
    [MenuItem("Tools/Neoxider/Find & Remove Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow<FindAndRemoveMissingScriptsWindow>("Find Missing Scripts");
    }
}