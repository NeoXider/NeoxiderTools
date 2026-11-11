#if MIRROR
using Mirror;
using Neo.Network;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Neo.Editor.Network
{
    /// <summary>
    ///     Runs after Mirror's <see cref="Mirror.NetworkScenePostProcess"/> and re-enables every
    ///     scene <see cref="NetworkIdentity"/> whose Neo components opt out of networking
    ///     (<see cref="INeoOptionalNetworked.IsNetworked"/> = <see langword="false"/>).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Mirror's post-processor (callback order 1) force-disables every baked scene
    ///         <c>NetworkIdentity</c> so <c>NetworkServer.SpawnObjects()</c> can spawn them when a
    ///         session starts. For offline-only Neo components this is wrong — they are never
    ///         spawned, so they stay dead forever. We hook the same pipeline at order 100 (after
    ///         Mirror) and reactivate eligible objects.
    ///     </para>
    ///     <para>
    ///         Because <c>[PostProcessScene]</c> runs at build time and when entering Play Mode,
    ///         the fix is applied to both — built scenes are saved with the objects active, and
    ///         editor Play Mode sees them active before any <c>Awake</c> fires. No runtime work is
    ///         required for the offline case (the runtime
    ///         <see cref="NeoMirrorSceneReactivator"/> remains as a safety net for unusual
    ///         scene-load paths).
    ///     </para>
    /// </remarks>
    internal static class NeoMirrorScenePostProcess
    {
        [PostProcessScene(100)]
        public static void OnPostProcessScene()
        {
            NetworkIdentity[] all = Resources.FindObjectsOfTypeAll<NetworkIdentity>();
            for (int i = 0; i < all.Length; i++)
            {
                NetworkIdentity identity = all[i];
                if (identity == null)
                {
                    continue;
                }

                GameObject go = identity.gameObject;
                if (go == null)
                {
                    continue;
                }

                if (identity.sceneId == 0)
                {
                    continue;
                }

                if (go.activeSelf)
                {
                    continue;
                }

                if (go.scene.name == "DontDestroyOnLoad")
                {
                    continue;
                }

                if (Utils.IsPrefab(go))
                {
                    continue;
                }

                HideFlags flags = go.hideFlags;
                if ((flags & HideFlags.HideAndDontSave) == HideFlags.HideAndDontSave)
                {
                    continue;
                }

                if ((flags & HideFlags.NotEditable) != 0)
                {
                    continue;
                }

                if (ShouldReactivate(go))
                {
                    go.SetActive(true);
                }
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
