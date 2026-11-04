using UnityEngine;

namespace Neo.Tests.Performance
{
    /// <summary>Closes the active <see cref="NeoPerfUpdateWindow" /> after every other script Update.</summary>
    [DefaultExecutionOrder(32000)]
    public sealed class NeoPerfUpdateWindowEnd : MonoBehaviour
    {
        private void Update()
        {
            NeoPerfUpdateWindow.Active?.End();
        }
    }
}
