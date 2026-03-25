using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/Spawner/SimpleSpawner.md")]
    [CreateFromMenu("Neoxider/Tools/Spawner/SimpleSpawner")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SimpleSpawner))]
    public class SimpleSpawner : MonoBehaviour
    {
        [Header("Prefabs")] public GameObject prefab;

        public Vector3 offset = Vector3.zero;
        public Vector3 eulerAngle = Vector3.zero;

        [Header("Behavior")] public bool useParent = true;

        [Tooltip(
            "If true, uses pool when PoolManager is in the scene; otherwise Instantiate. Pool can be added later — spawn always works.")]
        public bool useObjectPool = true;

        public void Spawn()
        {
            if (prefab == null)
            {
                Debug.LogError("Prefab is not assigned on SimpleSpawner!", this);
                return;
            }

            Vector3 spawnPosition = transform.position + offset;
            var spawnRotation = Quaternion.Euler(eulerAngle);
            Transform parent = useParent ? transform : null;

            if (useObjectPool)
            {
                SpawnUtility.Spawn(prefab, spawnPosition, spawnRotation, parent);
            }
            else
            {
                Instantiate(prefab, spawnPosition, spawnRotation, parent);
            }
        }
    }
}
