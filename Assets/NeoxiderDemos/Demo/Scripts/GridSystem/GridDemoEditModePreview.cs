using UnityEngine;

namespace Neo.Demo.GridSystem
{
    /// <summary>
    ///     Root of an authored, edit-mode-only picture of a board that a view draws at runtime (tiles, tray,
    ///     backdrop). It lets the saved scene show the layout and camera framing without entering Play Mode, and
    ///     hides itself on Awake so only the live view is visible while playing. It holds no gameplay state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Neoxider/Demo/GridSystem/GridDemoEditModePreview")]
    public sealed class GridDemoEditModePreview : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.SetActive(false);
        }
    }
}
