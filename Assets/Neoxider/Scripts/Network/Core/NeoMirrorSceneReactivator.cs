#if MIRROR
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Network
{
    /// <summary>
    ///     Runtime safety net for the offline-with-Mirror case: reactivates scene
    ///     <see cref="NetworkIdentity"/> objects that opt out of networking
    ///     (<see cref="INeoOptionalNetworked.IsNetworked"/> = <see langword="false"/>) if no
    ///     Mirror session is active.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The primary fix lives in the editor post-processor
    ///         <c>Neo.Editor.Network.NeoMirrorScenePostProcess</c> (callback order 100, runs after
    ///         Mirror's order 1). It bakes the correction into built scenes and applies it at Play
    ///         Mode entry, so this runtime hook is rarely needed.
    ///     </para>
    ///     <para>
    ///         It still helps with dynamic scene loading paths that bypass
    ///         <c>[PostProcessScene]</c> — additive scenes loaded at runtime, scenes opened by
    ///         user code from outside the build pipeline, etc.
    ///     </para>
    ///     <para>
    ///         Set <see cref="Enabled"/> to <see langword="false"/> at startup to opt out.
    ///     </para>
    /// </remarks>
    public static class NeoMirrorSceneReactivator
    {
        public static bool Enabled { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ReactivateScene(scene);
        }

        /// <summary>Reactivate eligible scene objects in <paramref name="scene"/>.</summary>
        public static void ReactivateScene(Scene scene)
        {
            if (!Enabled)
            {
                return;
            }

            if (NetworkServer.active || NetworkClient.active)
            {
                return;
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            // WHY: Walk scene roots only - Resources.FindObjectsOfTypeAll also visited prefab assets on
            // every scene load, which gets expensive in large projects.
            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                if (roots[r] == null)
                {
                    continue;
                }

                NetworkIdentity[] identities = roots[r].GetComponentsInChildren<NetworkIdentity>(true);
                for (int i = 0; i < identities.Length; i++)
                {
                    TryReactivate(identities[i]);
                }
            }
        }

        private static void TryReactivate(NetworkIdentity identity)
        {
            if (identity == null)
            {
                return;
            }

            GameObject go = identity.gameObject;
            if (go == null || identity.sceneId == 0 || go.activeSelf)
            {
                return;
            }

            HideFlags flags = go.hideFlags;
            if ((flags & HideFlags.HideAndDontSave) == HideFlags.HideAndDontSave)
            {
                return;
            }

            if ((flags & HideFlags.NotEditable) != 0)
            {
                return;
            }

            if (ShouldReactivate(go))
            {
                go.SetActive(true);
            }
        }

        private static bool ShouldReactivate(GameObject go)
        {
            INeoOptionalNetworked[] candidates = go.GetComponentsInChildren<INeoOptionalNetworked>(true);
            if (candidates.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null && candidates[i].IsNetworked)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#endif
