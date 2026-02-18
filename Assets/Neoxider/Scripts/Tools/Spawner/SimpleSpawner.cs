using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/Spawner/SimpleSpawner.md")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SimpleSpawner))]
    public class SimpleSpawner : MonoBehaviour
    {
        [Header("Prefabs")] public GameObject prefab;

        public Vector3 offset = Vector3.zero; // Исправлена опечатка
        public Vector3 eulerAngle = Vector3.zero;

        [Header("Behavior")] public bool useParent = true;

        public bool useObjectPool = true;

        public void Spawn()
        {
            if (prefab == null)
            {
                Debug.LogError("Prefab не назначен в SimpleSpawner!", this);
                return;
            }

            Vector3 spawnPosition = transform.position + offset;
            Quaternion spawnRotation = Quaternion.Euler(eulerAngle);
            Transform parent = useParent ? transform : null; // Улучшена логика родительства

            if (useObjectPool)
            {
                PoolManager.Get(prefab, spawnPosition, spawnRotation, parent);
            }
            else
            {
                Instantiate(prefab, spawnPosition, spawnRotation, parent);
            }
        }
    }
}
