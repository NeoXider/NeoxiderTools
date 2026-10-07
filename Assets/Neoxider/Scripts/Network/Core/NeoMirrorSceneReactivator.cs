#if MIRROR
using System;
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

        /// <summary>
        ///     Activates every inactive scene object that carries a <see cref="NetworkIdentity"/> in all loaded scenes.
        ///     Meant for the moment a network session starts, on <b>every</b> peer.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Mirror's scene post-process disables every scene <see cref="NetworkIdentity"/> object, and only the
        ///         server wakes them (<c>NetworkServer.SpawnObjects</c>). A client keeps them off until the spawn message
        ///         for each one arrives, so a scene object that registers a message handler in <c>Awake</c> has no handler
        ///         when the first message lands and Mirror disconnects the client for an unknown message id.
        ///     </para>
        ///     <para>
        ///         Activating early is safe: Mirror's own client-side lookup does not care whether a scene object is active,
        ///         and on the server it is exactly what <c>SpawnObjects</c> does. Objects created at runtime
        ///         (<c>sceneId == 0</c>) are never touched.
        ///     </para>
        /// </remarks>
        /// <param name="skip">Optional filter: return <see langword="true"/> to leave an object disabled (a scene player template).</param>
        /// <returns>How many objects were activated.</returns>
        public static int ActivateNetworkedSceneObjects(Predicate<GameObject> skip = null)
        {
            int activated = 0;
            int sceneCount = SceneManager.sceneCount;
            for (int s = 0; s < sceneCount; s++)
            {
                activated += ActivateNetworkedSceneObjects(SceneManager.GetSceneAt(s), skip);
            }

            return activated;
        }

        /// <summary>
        ///     Same as <see cref="ActivateNetworkedSceneObjects(System.Predicate{UnityEngine.GameObject})"/> for one scene.
        /// </summary>
        /// <param name="scene">The scene to walk. Invalid or unloaded scenes are ignored.</param>
        /// <param name="skip">Optional filter: return <see langword="true"/> to leave an object disabled.</param>
        /// <returns>How many objects were activated.</returns>
        public static int ActivateNetworkedSceneObjects(Scene scene, Predicate<GameObject> skip = null)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }

            int activated = 0;
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
                    NetworkIdentity identity = identities[i];
                    if (identity == null || identity.sceneId == 0)
                    {
                        continue;
                    }

                    GameObject go = identity.gameObject;
                    if (go.activeSelf || (go.hideFlags & (HideFlags.HideAndDontSave | HideFlags.NotEditable)) != 0)
                    {
                        continue;
                    }

                    if (skip != null && skip(go))
                    {
                        continue;
                    }

                    go.SetActive(true);
                    activated++;
                }
            }

            return activated;
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
