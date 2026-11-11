using UnityEngine;

namespace Neo.Tools
{
    /// <summary>Shared camera fallback policy for interaction components.</summary>
    public static class InteractionCameraResolver
    {
        /// <summary>
        ///     Keeps a valid cached camera, otherwise resolves Camera.main and optionally the first scene camera.
        /// </summary>
        public static Camera Resolve(Camera cachedCamera = null, bool allowSceneFallback = true)
        {
            if (cachedCamera != null)
            {
                return cachedCamera;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                return mainCamera;
            }

            return allowSceneFallback ? Object.FindFirstObjectByType<Camera>() : null;
        }
    }
}
