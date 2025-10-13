using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Neo.Tools
{
    public class Spawner : MonoBehaviour
    {
        [FormerlySerializedAs("_prefabs")] [Header("Spawn Settings")] [SerializeField]
        public GameObject[] prefabs;

        [SerializeField] private bool _useObjectPool = true;

        [FormerlySerializedAs("_destroyDelay")]
        [SerializeField]
        [Tooltip("Задержка перед удалением объекта. Если 0, объект не будет удаляться автоматически.")]
        public float destroyDelay = 0f;

        [Header("Events")]
        public UnityEvent<GameObject> OnObjectSpawned;

        [Space] [Header("If spawnLimit is zero then infinite spawn")]
        public int spawnLimit;

        [FormerlySerializedAs("_minSpawnDelay")] [SerializeField]
        public float minSpawnDelay = 0.5f;

        [FormerlySerializedAs("_maxSpawnDelay")] [SerializeField]
        public float maxSpawnDelay = 2f;

        [Space] [Header("Other Settings")] [SerializeField]
        private Transform _spawnTransform;

        [SerializeField] private bool _spawnOnAwake;

        [Header("Spawn Area")] [SerializeField]
        private Collider _spawnAreaCollider;

        [SerializeField] private Collider2D _spawnAreaCollider2D;

        public bool isSpawning { get; private set; }

        private List<GameObject> _spawnedObjects = new();
        private int _spawnedCount;

        public List<GameObject> SpawnedObjects => _spawnedObjects;

        private void Start()
        {
            if (_spawnOnAwake)
            {
                StartSpawn();
            }
        }

        private void OnValidate()
        {
            _spawnTransform ??= transform;
        }

        [Button]
        public void StartSpawn()
        {
            if (isSpawning)
            {
                return;
            }

            _spawnedCount = 0;
            StartCoroutine(SpawnObjects());
        }

        [Button]
        public void StopSpawn()
        {
            isSpawning = false;
        }

        public IEnumerator SpawnObjects()
        {
            isSpawning = true;
            float timer = 0f;

            while (isSpawning && (spawnLimit == 0 || _spawnedCount < spawnLimit))
            {
                timer -= Time.deltaTime;

                if (timer <= 0)
                {
                    SpawnRandomObject();
                    _spawnedCount++;
                    timer = Random.Range(minSpawnDelay, maxSpawnDelay);
                }

                yield return null;
            }

            isSpawning = false;
        }

        [Button]
        public GameObject SpawnRandomObject()
        {
            if (prefabs.Length == 0)
            {
                return null;
            }

            int randomIndex = prefabs.GetRandomIndex();
            GameObject prefabToSpawn = prefabs[randomIndex];
            return SpawnObject(prefabToSpawn, GetSpawnPosition(), Quaternion.identity, _spawnTransform);
        }

        // Note: The [Button] attribute on methods with parameters may require a library like Odin Inspector.
        [Button]
        public GameObject SpawnById(int prefabId, Vector3 position)
        {
            if (prefabId < 0 || prefabId >= prefabs.Length)
            {
                Debug.LogError($"Prefab ID {prefabId} is out of range. Max ID is {prefabs.Length - 1}.");
                return null;
            }

            GameObject prefabToSpawn = prefabs[prefabId];
            return SpawnObject(prefabToSpawn, position, Quaternion.identity, _spawnTransform);
        }

        public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject spawnedObject = _useObjectPool && PoolManager.I != null
                ? PoolManager.Get(prefab, position, rotation)
                : Instantiate(prefab, position, rotation);

            spawnedObject.transform.SetParent(parent);
            _spawnedObjects.Add(spawnedObject);

            if (destroyDelay > 0)
            {
                StartCoroutine(DelayedDestroy(spawnedObject, destroyDelay));
            }

            OnObjectSpawned?.Invoke(spawnedObject);

            return spawnedObject;
        }

        private IEnumerator DelayedDestroy(GameObject objectToDestroy, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (objectToDestroy != null)
            {
                _spawnedObjects.Remove(objectToDestroy);
                if (_useObjectPool)
                {
                    PoolManager.Release(objectToDestroy);
                }
                else
                {
                    Destroy(objectToDestroy);
                }
            }
        }

        public Vector3 GetSpawnPosition()
        {
            if (_spawnAreaCollider != null)
            {
                return GetRandomPointInCollider(_spawnAreaCollider);
            }

            if (_spawnAreaCollider2D != null)
            {
                return GetRandomPointInCollider2D(_spawnAreaCollider2D);
            }

            return _spawnTransform.position;
        }

        [Button]
        public void Clear()
        {
            StopAllCoroutines(); // Останавливаем все корутины (спавн и отложенное удаление)
            isSpawning = false; // Устанавливаем флаг, чтобы основной цикл спавна точно остановился

            foreach (GameObject obj in _spawnedObjects)
            {
                if (obj != null)
                {
                    if (_useObjectPool)
                    {
                        PoolManager.Release(obj);
                    }
                    else
                    {
                        Destroy(obj);
                    }
                }
            }

            _spawnedObjects.Clear();
        }

        public int GetActiveObjectCount()
        {
            return _spawnedObjects.Count(obj => obj != null && obj.activeInHierarchy);
        }

        // --- Методы для получения случайной точки в коллайдерах (без изменений) ---
        private Vector3 GetRandomPointInCollider2D(Collider2D collider)
        {
            if (collider is BoxCollider2D boxCollider)
            {
                Vector2 center = boxCollider.offset;
                Vector2 size = boxCollider.size;
                Vector2 randomPoint = new(
                    Random.Range(center.x - size.x / 2, center.x + size.x / 2),
                    Random.Range(center.y - size.y / 2, center.y + size.y / 2)
                );
                return boxCollider.transform.TransformPoint(randomPoint);
            }

            if (collider is CircleCollider2D circleCollider)
            {
                Vector2 center = circleCollider.offset;
                float radius = circleCollider.radius;
                Vector2 randomPoint = Random.insideUnitCircle * radius + center;
                return circleCollider.transform.TransformPoint(randomPoint);
            }

            Debug.LogWarning("Unsupported 2D collider type for spawning.");
            return _spawnTransform.position;
        }

        private Vector3 GetRandomPointInCollider(Collider collider)
        {
            if (collider is BoxCollider boxCollider)
            {
                Vector3 center = boxCollider.center;
                Vector3 size = boxCollider.size;
                Vector3 randomPoint = new(
                    Random.Range(center.x - size.x / 2, center.x + size.x / 2),
                    Random.Range(center.y - size.y / 2, center.y + size.y / 2),
                    Random.Range(center.z - size.z / 2, center.z + size.z / 2)
                );
                return boxCollider.transform.TransformPoint(randomPoint);
            }

            if (collider is SphereCollider sphereCollider)
            {
                Vector3 center = sphereCollider.center;
                float radius = sphereCollider.radius;
                Vector3 randomPoint = Random.insideUnitSphere * radius + center;
                return sphereCollider.transform.TransformPoint(randomPoint);
            }

            if (collider is CapsuleCollider capsuleCollider)
            {
                Vector3 center = capsuleCollider.center;
                float radius = capsuleCollider.radius;
                float height = capsuleCollider.height;
                Vector3 randomPoint = Random.insideUnitSphere * radius + center;
                randomPoint.y = Random.Range(center.y - height / 2, center.y + height / 2);
                return capsuleCollider.transform.TransformPoint(randomPoint);
            }

            Debug.LogWarning("Unsupported 3D collider type for spawning.");
            return _spawnTransform.position;
        }
    }
}