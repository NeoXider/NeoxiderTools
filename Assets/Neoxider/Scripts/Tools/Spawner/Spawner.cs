using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Neo.Tools
{
    public class Spawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject[] _prefabs;
        [SerializeField] private bool _useObjectPool = true;

        [Space]
        [Header("If spawnLimit is zero then infinite spawn")]
        public int spawnLimit;
        [SerializeField] private float _minSpawnDelay = 0.5f;
        [SerializeField] private float _maxSpawnDelay = 2f;

        [Space]
        [Header("Other Settings")]
        [SerializeField] private Transform _spawnTransform;
        [SerializeField] private bool _spawnOnAwake;

        [Header("Spawn Area")]
        [SerializeField] private Collider _spawnAreaCollider;
        [SerializeField] private Collider2D _spawnAreaCollider2D;

        public bool isSpawning { get; private set; }

        private readonly List<GameObject> _spawnedObjects = new();
        private ChanceData _chanceData;
        private int _spawnedCount;

        private void Start()
        {
            if (_spawnOnAwake)
                StartSpawn();
        }

        private void OnValidate()
        {
            _spawnTransform ??= transform;
        }

        public void StartSpawn()
        {
            if (isSpawning) return;
            
            _spawnedCount = 0;
            StartCoroutine(SpawnObjects());
        }

        public void StopSpawn()
        {
            isSpawning = false;
        }

        private IEnumerator SpawnObjects()
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
                    timer = Random.Range(_minSpawnDelay, _maxSpawnDelay);
                }

                yield return null;
            }

            isSpawning = false;
        }

        private void SpawnRandomObject()
        {
            if (_prefabs.Length == 0) return;

            int randomIndex = (_chanceData == null) 
                ? Random.Range(0, _prefabs.Length) 
                : _chanceData.GenerateId();

            GameObject prefabToSpawn = _prefabs[randomIndex];
            SpawnObject(prefabToSpawn, GetSpawnPosition(), Quaternion.identity, _spawnTransform);
        }

        public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab == null) return null;

            GameObject spawnedObject = _useObjectPool
                ? PoolManager.Get(prefab, position, rotation)
                : Instantiate(prefab, position, rotation);
            
            spawnedObject.transform.SetParent(parent);
            _spawnedObjects.Add(spawnedObject);
            return spawnedObject;
        }

        public Vector3 GetSpawnPosition()
        {
            if (_spawnAreaCollider != null)
                return GetRandomPointInCollider(_spawnAreaCollider);
            if (_spawnAreaCollider2D != null)
                return GetRandomPointInCollider2D(_spawnAreaCollider2D);
            
            return _spawnTransform.position;
        }

        public void Clear()
        {
            StopSpawn();

            foreach (var obj in _spawnedObjects)
            {
                if (obj != null)
                {
                    if (_useObjectPool)
                        PoolManager.Release(obj);
                    else
                        Destroy(obj);
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
                var center = boxCollider.offset;
                var size = boxCollider.size;
                var randomPoint = new Vector2(
                    Random.Range(center.x - size.x / 2, center.x + size.x / 2),
                    Random.Range(center.y - size.y / 2, center.y + size.y / 2)
                );
                return boxCollider.transform.TransformPoint(randomPoint);
            }
            if (collider is CircleCollider2D circleCollider)
            {
                var center = circleCollider.offset;
                var radius = circleCollider.radius;
                var randomPoint = Random.insideUnitCircle * radius + center;
                return circleCollider.transform.TransformPoint(randomPoint);
            }
            Debug.LogWarning("Unsupported 2D collider type for spawning.");
            return _spawnTransform.position;
        }

        private Vector3 GetRandomPointInCollider(Collider collider)
        {
            if (collider is BoxCollider boxCollider)
            {
                var center = boxCollider.center;
                var size = boxCollider.size;
                var randomPoint = new Vector3(
                    Random.Range(center.x - size.x / 2, center.x + size.x / 2),
                    Random.Range(center.y - size.y / 2, center.y + size.y / 2),
                    Random.Range(center.z - size.z / 2, center.z + size.z / 2)
                );
                return boxCollider.transform.TransformPoint(randomPoint);
            }
            if (collider is SphereCollider sphereCollider)
            {
                var center = sphereCollider.center;
                var radius = sphereCollider.radius;
                var randomPoint = Random.insideUnitSphere * radius + center;
                return sphereCollider.transform.TransformPoint(randomPoint);
            }
            if (collider is CapsuleCollider capsuleCollider)
            {
                var center = capsuleCollider.center;
                var radius = capsuleCollider.radius;
                var height = capsuleCollider.height;
                var randomPoint = Random.insideUnitSphere * radius + center;
                randomPoint.y = Random.Range(center.y - height / 2, center.y + height / 2);
                return capsuleCollider.transform.TransformPoint(randomPoint);
            }
            Debug.LogWarning("Unsupported 3D collider type for spawning.");
            return _spawnTransform.position;
        }
    }
}