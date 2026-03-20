using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/Spawner/SimpleSpawner.md")]
    [CreateFromMenu("Neoxider/Tools/Spawner/SimpleSpawner")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SimpleSpawner))]
    public class SimpleSpawner : MonoBehaviour
    {
        [Header("Prefabs")] public GameObject prefab;

        public Vector3 offset = Vector3.zero; // Исправлена опечатка
        public Vector3 eulerAngle = Vector3.zero;

        [Header("Behavior")] public bool useParent = true;

        [Tooltip(
            "При true используется пул, если на сцене есть PoolManager; иначе Instantiate. Пул можно добавить позже — спавн всегда работает.")]
        public bool useObjectPool = true;

        public void Spawn()
        {
            if (prefab == null)
            {
                Debug.LogError("Prefab не назначен в SimpleSpawner!", this);
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
