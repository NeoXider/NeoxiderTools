using UnityEngine;

namespace Neo.Tests.Performance
{
    /// <summary>Opens the active <see cref="NeoPerfUpdateWindow" /> before every other script Update.</summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class NeoPerfUpdateWindowBegin : MonoBehaviour
    {
        private void Update()
        {
            NeoPerfUpdateWindow.Active?.Begin();
        }
    }
}
