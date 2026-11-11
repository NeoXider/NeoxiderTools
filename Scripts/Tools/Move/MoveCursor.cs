using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     The single place where the Move module reads and writes the native cursor (<see cref="Cursor.lockState" />
    ///     and <see cref="Cursor.visible" />). <see cref="CursorLockController" />,
    ///     <see cref="PlayerController3DPhysics" />, <see cref="NeoCameraInput" /> and
    ///     <see cref="FreeFlyCameraController" /> go through it, so their ownership and auto-lock logic can be verified
    ///     against an in-memory cursor on hosts that cannot hold a native lock (batch mode, unfocused Game view).
    ///     Without an installed backend every call goes straight to <see cref="UnityEngine.Cursor" />.
    /// </summary>
    internal static class MoveCursor
    {
        /// <summary>Replacement cursor storage. Tests install one; production code never does.</summary>
        internal interface IBackend
        {
            CursorLockMode LockState { get; set; }
            bool Visible { get; set; }
        }

        private static IBackend _backend;

        /// <summary>The cursor lock state: the installed backend's, otherwise <see cref="Cursor.lockState" />.</summary>
        internal static CursorLockMode LockState
        {
            get => _backend != null ? _backend.LockState : Cursor.lockState;
            set
            {
                if (_backend != null)
                {
                    _backend.LockState = value;
                    return;
                }

                Cursor.lockState = value;
            }
        }

        /// <summary>The cursor visibility: the installed backend's, otherwise <see cref="Cursor.visible" />.</summary>
        internal static bool Visible
        {
            get => _backend != null ? _backend.Visible : Cursor.visible;
            set
            {
                if (_backend != null)
                {
                    _backend.Visible = value;
                    return;
                }

                Cursor.visible = value;
            }
        }

        /// <summary>True while a replacement backend is installed instead of the native cursor.</summary>
        internal static bool HasBackend => _backend != null;

        /// <summary>
        ///     Installs <paramref name="backend" /> (null restores the native cursor) and returns the previously
        ///     installed one, so a caller can put it back when it is done.
        /// </summary>
        internal static IBackend SetBackend(IBackend backend)
        {
            IBackend previous = _backend;
            _backend = backend;
            return previous;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBackend()
        {
            // WHY: with domain reload disabled, a backend left behind by an aborted test must not outlive Play Mode.
            _backend = null;
        }
    }
}
