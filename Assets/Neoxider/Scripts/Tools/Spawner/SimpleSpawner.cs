using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Spawns one prefab at this transform (plus offset) from a UnityEvent or code, pooled by default.
    /// </summary>
    [NeoDoc("Tools/Spawner/SimpleSpawner.md")]
    [CreateFromMenu("Neoxider/Tools/Spawner/SimpleSpawner")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SimpleSpawner))]
    public class SimpleSpawner : MonoBehaviour
    {
        [Header("Prefabs")] [Tooltip("Prefab spawned by Spawn().")]
        public GameObject prefab;

        [Tooltip("World-space offset added to this transform's position.")]
        public Vector3 offset = Vector3.zero;

        [Tooltip("World rotation of the spawned object, in Euler degrees.")]
        public Vector3 eulerAngle = Vector3.zero;

        [Header("Behavior")] [Tooltip("Parent the spawned object to this transform.")]
        public bool useParent = true;

        [Tooltip(
            "If true, uses PoolManager or an automatic fallback pool. Otherwise uses Instantiate.")]
        public bool useObjectPool = true;

        /// <summary>UnityEvent entry point; same as <see cref="SpawnInstance" /> without the result.</summary>
        [Button(PlayModeOnly = true)]
        public void Spawn()
        {
            SpawnInstance();
        }

        /// <summary>Spawns <see cref="prefab" /> and returns the instance, or null when no prefab is assigned.</summary>
        public GameObject SpawnInstance()
        {
            if (prefab == null)
            {
                NeoDiagnostics.LogError("Prefab is not assigned on SimpleSpawner!", this);
                return null;
            }

            Vector3 spawnPosition = transform.position + offset;
            Quaternion spawnRotation = Quaternion.Euler(eulerAngle);
            Transform parent = useParent ? transform : null;

            return useObjectPool
                ? SpawnUtility.Spawn(prefab, spawnPosition, spawnRotation, parent)
                : Instantiate(prefab, spawnPosition, spawnRotation, parent);
        }
    }
}
