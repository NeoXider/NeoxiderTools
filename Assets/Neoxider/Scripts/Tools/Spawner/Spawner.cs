using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Neo.Tools
{
    /// <summary>
    ///     Спавнер префабов с настраиваемыми задержками, диапазонами поворота по осям (Эйлер),
    ///     опциональным родителем и режимом применения поворота в локальных или мировых координатах.
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + nameof(Spawner))]
    public class Spawner : MonoBehaviour
    {
        [Header("Spawn Settings")] [SerializeField]
        private GameObject[] _prefabs;

        /// <summary>
        ///     Массив префабов для спавна. При установке выполняется валидация.
        /// </summary>
        public GameObject[] prefabs
        {
            get => _prefabs;
            set => _prefabs = value ?? Array.Empty<GameObject>();
        }

        [SerializeField] private bool _useObjectPool = true;

        [FormerlySerializedAs("_destroyDelay")]
        [SerializeField]
        [Tooltip("Задержка перед удалением объекта. Если 0, объект не будет удаляться автоматически.")]
        public float destroyDelay;

        [Header("Events")]
        /// <summary>
        /// UnityEvent, вызывается после успешного спавна игрового объекта.
        /// </summary>
        public UnityEvent<GameObject> OnObjectSpawned;

        [Space] [Header("If spawnLimit is zero then infinite spawn")]
        public int spawnLimit;

        [FormerlySerializedAs("_minSpawnDelay")] [SerializeField]
        public float minSpawnDelay = 0.5f;

        [FormerlySerializedAs("_maxSpawnDelay")] [SerializeField]
        public float maxSpawnDelay = 2f;

        [Header("Rotation Settings")]
        /// <summary>Диапазон поворота вокруг оси X (pitch), градусов.</summary>
        [SerializeField]
        private Vector2 _rotationX = Vector2.zero; // pitch

        /// <summary>Диапазон поворота вокруг оси Y (yaw), градусов.</summary>
        [SerializeField] private Vector2 _rotationY = Vector2.zero; // yaw

        /// <summary>Диапазон поворота вокруг оси Z (roll), градусов.</summary>
        [SerializeField] private Vector2 _rotationZ = Vector2.zero; // roll

        [SerializeField]
        [Tooltip("Если true, поворот задаётся относительно спавнера (локально). Если false — в мировых координатах.")]
        private bool _useLocalRotation = true;

        [Space] [Header("Other Settings")] [SerializeField]
        /// <summary>
        /// Точка спавна. Если не задана — используется transform самого спавнера.
        /// </summary>
        private Transform _spawnTransform;

        [SerializeField] private bool _spawnOnAwake;

        [Header("Parenting")]
        [SerializeField]
        [Tooltip("Родитель для заспавненных объектов. Если null — спавн без родителя.")]
        /// <summary>
        /// Родитель для заспавненных объектов. Если null — объект не получает родителя.
        /// </summary>
        private Transform _parentTransform;

        [Header("Spawn Area")] [SerializeField]
        private Collider _spawnAreaCollider;

        [SerializeField] private Collider2D _spawnAreaCollider2D;

        public bool isSpawning { get; private set; }

        private int _spawnedCount;

        public List<GameObject> SpawnedObjects { get; } = new();

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

            // Валидация диапазонов задержек
            if (minSpawnDelay < 0)
            {
                minSpawnDelay = 0;
            }

            if (maxSpawnDelay < minSpawnDelay)
            {
                maxSpawnDelay = minSpawnDelay;
            }

            // Валидация массива префабов
            if (_prefabs != null)
            {
                for (int i = 0; i < _prefabs.Length; i++)
                {
                    if (_prefabs[i] == null)
                    {
                        Debug.LogWarning($"[Spawner] Prefab at index {i} is null on {gameObject.name}!", this);
                    }
                }
            }
        }

        /// <summary>
        ///     Запускает процесс спавна (корутина), если он ещё не запущен.
        /// </summary>
#if ODIN_INSPECTOR
            [Button]
#else
        [ButtonAttribute]
#endif
        public void StartSpawn()
        {
            if (isSpawning)
            {
                return;
            }

            _spawnedCount = 0;
            StartCoroutine(SpawnObjects());
        }

        /// <summary>
        ///     Останавливает процесс спавна. Текущие объекты остаются в сцене.
        /// </summary>
#if ODIN_INSPECTOR
            [Button]
#else
        [ButtonAttribute]
#endif
        public void StopSpawn()
        {
            isSpawning = false;
        }

        /// <summary>
        ///     Главная корутина спавна, создает объекты с интервалами в пределах [minSpawnDelay, maxSpawnDelay].
        /// </summary>
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

        /// <summary>
        ///     Спавнит случайный префаб из списка по текущим настройкам позиции/поворота/родителя.
        /// </summary>
#if ODIN_INSPECTOR
            [Button]
#else
        [ButtonAttribute]
#endif
        public GameObject SpawnRandomObject()
        {
            if (_prefabs == null || _prefabs.Length == 0)
            {
                Debug.LogWarning($"[Spawner] Prefabs array is null or empty on {gameObject.name}. Cannot spawn.", this);
                return null;
            }

            int randomIndex = _prefabs.GetRandomIndex();
            GameObject prefabToSpawn = _prefabs[randomIndex];

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[Spawner] Prefab at index {randomIndex} is null on {gameObject.name}.", this);
                return null;
            }

            return SpawnObject(prefabToSpawn, GetSpawnPosition(), GetSpawnRotation(), _parentTransform);
        }

        // Note: The [Button] attribute on methods with parameters may require a library like Odin Inspector.
        /// <summary>
        ///     Спавнит префаб по индексу <paramref name="prefabId" /> в заданной позиции.
        ///     Поворот и родитель берутся из настроек спавнера.
        /// </summary>
        /// <param name="prefabId">Индекс префаба в массиве <see cref="prefabs" />.</param>
        /// <param name="position">Мировая позиция спавна.</param>
#if ODIN_INSPECTOR
            [Button]
#else
        [ButtonAttribute]
#endif
        public GameObject SpawnById(int prefabId, Vector3 position)
        {
            if (_prefabs == null || _prefabs.Length == 0)
            {
                Debug.LogError($"[Spawner] Prefabs array is null or empty on {gameObject.name}. Cannot spawn.", this);
                return null;
            }

            if (prefabId < 0 || prefabId >= _prefabs.Length)
            {
                Debug.LogError(
                    $"[Spawner] Prefab ID {prefabId} is out of range. Max ID is {_prefabs.Length - 1} on {gameObject.name}.",
                    this);
                return null;
            }

            GameObject prefabToSpawn = _prefabs[prefabId];
            if (prefabToSpawn == null)
            {
                Debug.LogError($"[Spawner] Prefab at index {prefabId} is null on {gameObject.name}.", this);
                return null;
            }

            return SpawnObject(prefabToSpawn, position, GetSpawnRotation(), _parentTransform);
        }

        /// <summary>
        ///     Базовый метод спавна/получения из пула.
        /// </summary>
        /// <param name="prefab">Префаб для спавна.</param>
        /// <param name="position">Мировая позиция.</param>
        /// <param name="rotation">Мировой поворот.</param>
        /// <param name="parent">Родитель. Если null — без родителя.</param>
        public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject spawnedObject = _useObjectPool && PoolManager.I != null
                ? PoolManager.Get(prefab, position, rotation)
                : Instantiate(prefab, position, rotation);

            if (parent != null)
            {
                spawnedObject.transform.SetParent(parent, true);
            }

            SpawnedObjects.Add(spawnedObject);

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
                SpawnedObjects.Remove(objectToDestroy);
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

        /// <summary>
        ///     Возвращает позицию спавна: случайная точка в зоне, если задана, иначе точка спавна.
        /// </summary>
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

        /// <summary>
        ///     Возвращает поворот спавна. Если диапазоны осей равны нулю, возвращает
        ///     базовый поворот (локальный: поворот точки спавна; мировой: Quaternion.identity).
        /// </summary>
        private Quaternion GetSpawnRotation()
        {
            bool zeroX = _rotationX == Vector2.zero;
            bool zeroY = _rotationY == Vector2.zero;
            bool zeroZ = _rotationZ == Vector2.zero;

            Quaternion baseRot = _spawnTransform != null ? _spawnTransform.rotation : Quaternion.identity;

            if (zeroX && zeroY && zeroZ)
            {
                return _useLocalRotation ? baseRot : Quaternion.identity;
            }

            float rx = zeroX
                ? 0f
                : Random.Range(Mathf.Min(_rotationX.x, _rotationX.y), Mathf.Max(_rotationX.x, _rotationX.y));
            float ry = zeroY
                ? 0f
                : Random.Range(Mathf.Min(_rotationY.x, _rotationY.y), Mathf.Max(_rotationY.x, _rotationY.y));
            float rz = zeroZ
                ? 0f
                : Random.Range(Mathf.Min(_rotationZ.x, _rotationZ.y), Mathf.Max(_rotationZ.x, _rotationZ.y));

            Quaternion offset = Quaternion.Euler(rx, ry, rz);
            return _useLocalRotation ? baseRot * offset : offset;
        }
#if ODIN_INSPECTOR
            [Button]
#else
        [ButtonAttribute]
#endif
        public void Clear()
        {
            StopAllCoroutines(); // Останавливаем все корутины (спавн и отложенное удаление)
            isSpawning = false; // Устанавливаем флаг, чтобы основной цикл спавна точно остановился

            foreach (GameObject obj in SpawnedObjects)
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

            SpawnedObjects.Clear();
        }

        public int GetActiveObjectCount()
        {
            return SpawnedObjects.Count(obj => obj != null && obj.activeInHierarchy);
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