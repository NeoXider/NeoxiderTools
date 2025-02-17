using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neo.Tools;
using UnityEngine;

namespace Neo
{
    public class Spawner : MonoBehaviour
    {
        [Header("Spawn Setting")]
        [SerializeField] private GameObject[] _prefabs;
        [SerializeField] private ChanceData _chanceData;

        [Space, Header("If spawnLimit is zero then infinite spawn")]
        public int spawnLimit = 0;
        [SerializeField] private float _minSpawnDelay = 0.5f;
        [SerializeField] private float _maxSpawnDelay = 2f;

        [Space, Header("Other Settings")]
        [SerializeField] private Transform _spawnTransform;
        [SerializeField] private bool _spawnOnAwake;

        [Space, Header("if have ObjectPool")]
        [SerializeField] private ObjectPool<GameObject>[] _objectPools;


        [Header("Spawn Area")]
        [SerializeField] private Collider _spawnAreaCollider;
        [SerializeField] private Collider2D _spawnAreaCollider2D;

        public bool isSpawning;

        private List<GameObject> _spawnedObjects = new List<GameObject>();
        private int _spawnedCount = 0;

        void Start()
        {
            for (int i = 0; i < _objectPools.Length; i++)
            {
                _objectPools[i].Init(_prefabs[i]);
            }

            if (_spawnOnAwake)
                StartSpawn();
        }

        public void StartSpawn()
        {
            _spawnedCount = 0;

            if (!isSpawning)
            {
                StartCoroutine(SpawnObjects());
            }
        }

        public void SetSpawning(bool active)
        {
            isSpawning = active;
        }

        private IEnumerator SpawnObjects()
        {
            isSpawning = true;
            float timer = 0f;

            while (isSpawning && ((spawnLimit == 0) || _spawnedCount < spawnLimit))
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

            int randomIndex = 0;

            if (_chanceData == null)
            {
                randomIndex = Random.Range(0, _prefabs.Length);
            }
            else
            {
                randomIndex = _chanceData.GenerateId();
            }

            GameObject prefabToSpawn = _prefabs[randomIndex];
            GameObject spawnedObject;

            if (randomIndex < _objectPools.Length && _objectPools[randomIndex] != null)
            {
                spawnedObject = _objectPools[randomIndex].GetObject(GetSpawnPosition(), Quaternion.identity);
                if (spawnedObject != null)
                {
                    spawnedObject.transform.SetParent(_spawnTransform);
                    _spawnedObjects.Add(spawnedObject);
                }
            }
            else
            {
                spawnedObject = Instantiate(prefabToSpawn, GetSpawnPosition(), Quaternion.identity, _spawnTransform);
                _spawnedObjects.Add(spawnedObject);
            }
        }

        public GameObject SpawnObject(Vector3? pos = null, int id = 0, Transform transform = null)
        {
            if (id == -1)
                id = _prefabs.GetRandomIndex();

            Transform transformParent = transform ?? _spawnTransform;

            GameObject obj = Instantiate(_prefabs[id], pos ?? transformParent.position, Quaternion.identity, transformParent);
            _spawnedObjects.Add(obj);

            return obj;
        }

        public GameObject SpawnObjectWithCollider(int id = 0)
        {
            GameObject obj = Instantiate(_prefabs[id], GetSpawnPosition(), Quaternion.identity, _spawnTransform);
            _spawnedObjects.Add(obj);

            return obj;
        }

        public GameObject SpawnObjectWithCollider(GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, GetSpawnPosition(), Quaternion.identity, _spawnTransform);
            _spawnedObjects.Add(obj);

            return obj;
        }

        public Vector3 GetSpawnPosition()
        {
            Vector3 spawnPosition = transform.position;

            if (_spawnAreaCollider != null)
                spawnPosition = GetRandomPointInCollider(_spawnAreaCollider);
            else if (_spawnAreaCollider2D != null)
                spawnPosition = GetRandomPointInCollider2D(_spawnAreaCollider2D);
            return spawnPosition;
        }

        public void Clear()
        {
            isSpawning = false;

            foreach (GameObject obj in _spawnedObjects)
            {
                if (obj != null)
                {
                    int index = System.Array.IndexOf(_prefabs, obj);
                    if (index >= 0 && _objectPools[index] != null)
                    {
                        _objectPools[index].ReturnObject(obj);
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
            return _spawnedObjects.Count(obj => obj != null);
        }

        private Vector3 GetRandomPointInCollider2D(Collider2D collider)
        {
            if (collider is BoxCollider2D boxCollider)
            {
                Vector2 center = boxCollider.offset;
                Vector2 size = boxCollider.size;

                Vector2 randomPoint = new Vector2(
                    Random.Range(center.x - size.x / 2, center.x + size.x / 2),
                    Random.Range(center.y - size.y / 2, center.y + size.y / 2)
                );

                return boxCollider.transform.TransformPoint(randomPoint);
            }
            else if (collider is CircleCollider2D circleCollider)
            {
                Vector2 center = circleCollider.offset;
                float radius = circleCollider.radius;

                Vector2 randomPoint = Random.insideUnitCircle * radius + center;

                return circleCollider.transform.TransformPoint(randomPoint);
            }
            else
            {
                Debug.LogWarning("Unsupported collider type for spawning.");
                return _spawnTransform.position;
            }
        }

        private Vector3 GetRandomPointInCollider(Collider collider)
        {
            if (collider is BoxCollider boxCollider)
            {
                Vector3 center = boxCollider.center;
                Vector3 size = boxCollider.size;

                Vector3 randomPoint = new Vector3(
                    Random.Range(center.x - size.x / 2, center.x + size.x / 2),
                    Random.Range(center.y - size.y / 2, center.y + size.y / 2),
                    Random.Range(center.z - size.z / 2, center.z + size.z / 2)
                );

                return boxCollider.transform.TransformPoint(randomPoint);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Vector3 center = sphereCollider.center;
                float radius = sphereCollider.radius;

                Vector3 randomPoint = Random.insideUnitSphere * radius + center;

                return sphereCollider.transform.TransformPoint(randomPoint);
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                Vector3 center = capsuleCollider.center;
                float radius = capsuleCollider.radius;
                float height = capsuleCollider.height;

                Vector3 randomPoint = Random.insideUnitSphere * radius + center;
                randomPoint.y = Random.Range(center.y - height / 2, center.y + height / 2);

                return capsuleCollider.transform.TransformPoint(randomPoint);
            }
            else
            {
                Debug.LogWarning("Unsupported collider type for spawning.");
                return _spawnTransform.position;
            }
        }

        private void OnValidate()
        {
            _spawnTransform ??= transform;
        }
    }
}
