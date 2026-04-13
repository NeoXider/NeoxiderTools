using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
#if MIRROR
using Mirror;
using Neo.Network;
#endif

namespace Neo.Tools
{
    public enum SpawnMode { Loop, Waves }

    /// <summary>
    ///     Spawns prefabs with configurable delays, per-axis Euler rotation ranges,
    ///     optional parent, and local or world rotation application.
    /// </summary>
    [NeoDoc("Tools/Spawner/Spawner.md")]
    [CreateFromMenu("Neoxider/Tools/Spawner/Spawner")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Spawner))]
#if MIRROR
    public class Spawner : NetworkBehaviour
#else
    public class Spawner : MonoBehaviour
#endif
    {
        [Header("Networking")]
        [Tooltip("If enabled, StartSpawn/StopSpawn/Clear are replicated, and Server dynamically spawns NetworkIdentities.")]
        [SerializeField]
        public bool isNetworked = false;
        [Header("Spawn Settings")] [SerializeField]
        private GameObject[] _prefabs;

        [Tooltip(
            "If true, spawn via pool when PoolManager exists; otherwise Instantiate. Pool is created on first request per prefab.")]
        [SerializeField]
        private bool _useObjectPool = true;

        [FormerlySerializedAs("_destroyDelay")]
        [SerializeField]
        [Tooltip("Delay before destroying object. If 0, object will not be destroyed automatically.")]
        public float destroyDelay;

        /// <summary>
        ///     UnityEvent invoked after a GameObject is successfully spawned.
        /// </summary>
        public UnityEvent<GameObject> OnObjectSpawned;

        [Space] [Header("If spawnLimit is zero then infinite spawn")]
        public int spawnLimit;

        [FormerlySerializedAs("_minSpawnDelay")] [SerializeField]
        public float minSpawnDelay = 0.5f;

        [FormerlySerializedAs("_maxSpawnDelay")] [SerializeField]
        public float maxSpawnDelay = 2f;

        [Header("Rotation Settings")]
        /// <summary>Rotation range around X (pitch), degrees.</summary>
        [SerializeField]
        private Vector2 _rotationX = Vector2.zero; // pitch

        /// <summary>Rotation range around Y (yaw), degrees.</summary>
        [SerializeField] private Vector2 _rotationY = Vector2.zero; // yaw

        /// <summary>Rotation range around Z (roll), degrees.</summary>
        [SerializeField] private Vector2 _rotationZ = Vector2.zero; // roll

        [SerializeField] [Tooltip("If true, rotation is relative to spawner (local). If false — in world space.")]
        private bool _useLocalRotation = true;

        [SerializeField] [Tooltip("If true, takes rotation from _spawnTransform")]
        private bool _useParentRotation;

        [Header("Mode & Wave Settings")]
        [SerializeField]
        [Tooltip("Spawn mode: Loop (classic continuous) or Waves.")]
        public SpawnMode spawnMode = SpawnMode.Loop;
        
        [SerializeField]
        [Tooltip("Number of objects to spawn in the first wave.")]
        private int _baseWaveCount = 3;
        
        [SerializeField]
        [Tooltip("How many MORE objects to spawn each subsequent wave.")]
        private int _countPerWave = 2;
        
        [SerializeField]
        [Tooltip("Time to wait after a wave finishes before starting the next one.")]
        private float _timeBetweenWaves = 5f;
        
        [Tooltip("Maximum allowed waves limit. 0 for infinite waves.")]
        public int maxWaves;
        
        /// <summary>
        /// Invoked when a new wave starts. Passes the current wave number.
        /// </summary>
        public UnityEvent<int> OnWaveStarted;

        /// <summary>
        /// Invoked after a GameObject is spawned during a wave. Passes the object and wave index.
        /// </summary>
        public UnityEvent<GameObject, int> OnWaveObjectSpawned;

        [Space] [Header("Other Settings")] [SerializeField]
        /// <summary>
        /// Spawn point. If unset, uses this spawner's transform.
        /// </summary>
        private Transform _spawnTransform;

        [SerializeField] private bool _spawnOnAwake;

        [Header("Parenting")] [SerializeField] [Tooltip("Parent for spawned objects. If null — spawn without parent.")]
        /// <summary>
        /// Parent for spawned objects. If null, spawned objects have no parent.
        /// </summary>
        private Transform _parentTransform;

        [Header("Spawn Area")] [SerializeField]
        private Collider _spawnAreaCollider;

        [SerializeField] private Collider2D _spawnAreaCollider2D;

        private int _spawnedCount;

        /// <summary>
        ///     Prefab array to spawn from. Validated on assignment.
        /// </summary>
        public GameObject[] prefabs
        {
            get => _prefabs;
            set => _prefabs = value ?? Array.Empty<GameObject>();
        }

        public bool isSpawning { get; private set; }

        public int CurrentWave { get; private set; } = 1;

        public List<GameObject> SpawnedObjects { get; } = new();

        private void Start()
        {
            if (_spawnOnAwake)
            {
                StartSpawn();
            }
        }

#if MIRROR
        protected override void OnValidate()
        {
            if (isNetworked)
            {
                base.OnValidate();
            }
#else
        private void OnValidate()
        {
#endif
            _spawnTransform ??= transform;

            // Validate delay range
            if (minSpawnDelay < 0)
            {
                minSpawnDelay = 0;
            }

            if (maxSpawnDelay < minSpawnDelay)
            {
                maxSpawnDelay = minSpawnDelay;
            }

            // Validate prefab array
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
        ///     Starts spawning (coroutine) if not already running.
        /// </summary>
        [Button]
        public void StartSpawn()
        {
#if MIRROR
            if (isNetworked && (NeoNetworkState.IsClient || NeoNetworkState.IsServer))
            {
                if (NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
                {
                    CmdStartSpawn();
                    return;
                }
            }
#endif
            if (isSpawning)
            {
                return;
            }

            _spawnedCount = 0;
            StartCoroutine(SpawnObjects());
        }

        /// <summary>
        ///     Stops spawning. Existing instances stay in the scene.
        /// </summary>
        [Button]
        public void StopSpawn()
        {
#if MIRROR
            if (isNetworked && (NeoNetworkState.IsClient || NeoNetworkState.IsServer))
            {
                if (NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
                {
                    CmdStopSpawn();
                    return;
                }
            }
#endif
            isSpawning = false;
        }

        /// <summary>
        ///     Main spawn coroutine; spawns at random intervals in [minSpawnDelay, maxSpawnDelay].
        /// </summary>
        public IEnumerator SpawnObjects()
        {
            isSpawning = true;
            CurrentWave = 1;

            if (spawnMode == SpawnMode.Waves)
            {
                while (isSpawning && (maxWaves == 0 || CurrentWave <= maxWaves))
                {
                    OnWaveStarted?.Invoke(CurrentWave);
#if MIRROR
                    if (isNetworked && NeoNetworkState.IsServer)
                    {
                        RpcNotifyWaveStarted(CurrentWave);
                    }
#endif
                    int spawnAmount = _baseWaveCount + ((CurrentWave - 1) * _countPerWave);
                    
                    for (int i = 0; i < spawnAmount; i++)
                    {
                        if (!isSpawning) break;
                        
                        GameObject obj = SpawnRandomObject();
                        // Delay between individual spawns within a wave
                        float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
                        yield return new WaitForSeconds(delay);
                    }
                    
                    if (isSpawning && (maxWaves == 0 || CurrentWave < maxWaves))
                    {
                        CurrentWave++;
                        yield return new WaitForSeconds(_timeBetweenWaves);
                    }
                    else
                    {
                        isSpawning = false;
                        break;
                    }
                }
            }
            else
            {
                while (isSpawning && (spawnLimit == 0 || _spawnedCount < spawnLimit))
                {
                    SpawnRandomObject();
                    _spawnedCount++;
                    float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
                    yield return new WaitForSeconds(delay);
                }
            }

            isSpawning = false;
        }

        /// <summary>
        ///     Spawns a random prefab using current position, rotation, and parent settings.
        /// </summary>
        [Button]
        public GameObject SpawnRandomObject()
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
            {
                if (_prefabs == null || _prefabs.Length == 0) return null;
                int rIndex = _prefabs.GetRandomIndex();
                CmdSpawnById(rIndex, GetSpawnPosition(), GetSpawnRotation());
                return null;
            }
#endif
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
        ///     Spawns the prefab at <paramref name="prefabId" /> at the given position.
        ///     Rotation and parent come from spawner settings.
        /// </summary>
        /// <param name="prefabId">Index in <see cref="prefabs" />.</param>
        /// <param name="position">World spawn position.</param>
        [Button]
        public GameObject SpawnById(int prefabId, Vector3 position)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
            {
                CmdSpawnById(prefabId, position, GetSpawnRotation());
                return null;
            }
#endif
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
        ///     Core spawn path: uses <see cref="SpawnUtility" /> when pooling is enabled
        ///     (<see cref="_useObjectPool" /> and PoolManager), otherwise Instantiate.
        /// </summary>
        /// <param name="prefab">Prefab to spawn.</param>
        /// <param name="position">World position.</param>
        /// <param name="rotation">World rotation.</param>
        /// <param name="parent">Parent transform, or null for no parent.</param>
        public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
            {
                Debug.LogWarning("[Spawner] Client cannot explicitly SpawnObject with a direct GameObject reference. Please use SpawnById or SpawnRandomObject which are safely routed to the Server.", this);
                return null;
            }
#endif
            if (prefab == null)
            {
                return null;
            }

            GameObject spawnedObject = _useObjectPool
                ? SpawnUtility.Spawn(prefab, position, rotation, parent)
                : Instantiate(prefab, position, rotation, parent);

            if (spawnedObject == null)
            {
                return null;
            }

#if MIRROR
            if (isNetworked && NeoNetworkState.IsServer)
            {
                NetworkIdentity id = spawnedObject.GetComponent<NetworkIdentity>();
                if (id != null)
                {
                    NetworkServer.Spawn(spawnedObject);
                }
                else
                {
                    Debug.LogWarning($"[Spawner] Spawned object {spawnedObject.name} has no NetworkIdentity. Mirror cannot replicate it.", this);
                }
            }
#endif

            if (SpawnedObjects.Capacity > 0 && SpawnedObjects.Count % 10 == 0)
            {
                SpawnedObjects.RemoveAll(obj => obj == null);
            }

            SpawnedObjects.Add(spawnedObject);

            if (destroyDelay > 0)
            {
                StartCoroutine(DelayedDestroy(spawnedObject, destroyDelay));
            }

            OnObjectSpawned?.Invoke(spawnedObject);
            
            if (spawnMode == SpawnMode.Waves)
            {
                OnWaveObjectSpawned?.Invoke(spawnedObject, CurrentWave);
            }

#if MIRROR
            if (isNetworked && NeoNetworkState.IsServer)
            {
                NetworkIdentity netId = spawnedObject.GetComponent<NetworkIdentity>();
                if (netId != null)
                {
                    RpcNotifyObjectSpawned(netId.gameObject, CurrentWave);
                }
            }
#endif

            return spawnedObject;
        }

#if MIRROR
        [ClientRpc]
        private void RpcNotifyWaveStarted(int wave)
        {
            if (isServerOnly) return;
            CurrentWave = wave;
            OnWaveStarted?.Invoke(wave);
        }

        [ClientRpc]
        private void RpcNotifyObjectSpawned(GameObject netObj, int wave)
        {
            if (isServerOnly || netObj == null) return;
            
            if (SpawnedObjects.Capacity > 0 && SpawnedObjects.Count % 10 == 0)
            {
                SpawnedObjects.RemoveAll(obj => obj == null);
            }
            SpawnedObjects.Add(netObj);

            OnObjectSpawned?.Invoke(netObj);
            
            if (spawnMode == SpawnMode.Waves)
            {
                OnWaveObjectSpawned?.Invoke(netObj, wave);
            }
        }
#endif

        private IEnumerator DelayedDestroy(GameObject objectToDestroy, float delay)
        {
            yield return new WaitForSeconds(delay);

            SpawnedObjects.Remove(objectToDestroy);

            if (objectToDestroy != null)
            {
                if (_useObjectPool)
                {
                    SpawnUtility.Despawn(objectToDestroy);
                }
                else
                {
                    Destroy(objectToDestroy);
                }
            }
        }

        /// <summary>
        ///     Spawn position: random point in area collider if set, otherwise spawn transform position.
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

            return _spawnTransform != null ? _spawnTransform.position : transform.position;
        }

        /// <summary>
        ///     Spawn rotation. If all axis ranges are zero, returns base rotation
        ///     (local: spawn point rotation; world: Quaternion.identity).
        /// </summary>
        private Quaternion GetSpawnRotation()
        {
            if (_useParentRotation)
            {
                return _spawnTransform.rotation;
            }

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

            var offset = Quaternion.Euler(rx, ry, rz);
            return _useLocalRotation ? baseRot * offset : offset;
        }

        [Button]
        public void Clear()
        {
#if MIRROR
            if (isNetworked && (NeoNetworkState.IsClient || NeoNetworkState.IsServer))
            {
                if (NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
                {
                    CmdClear();
                    return;
                }
            }
#endif
            StopAllCoroutines(); // Stop spawn and delayed destroy coroutines
            isSpawning = false;

            foreach (GameObject obj in SpawnedObjects)
            {
                if (obj != null)
                {
                    if (_useObjectPool)
                    {
                        SpawnUtility.Despawn(obj);
                    }
                    else
                    {
                        Destroy(obj);
                    }
                }
            }

            SpawnedObjects.Clear();
        }

#if MIRROR
        [Command(requiresAuthority = false)]
        private void CmdStartSpawn()
        {
            StartSpawn();
        }

        [Command(requiresAuthority = false)]
        private void CmdStopSpawn()
        {
            StopSpawn();
        }

        [Command(requiresAuthority = false)]
        private void CmdClear()
        {
            Clear();
        }

        [Command(requiresAuthority = false)]
        private void CmdSpawnById(int prefabId, Vector3 position, Quaternion rotation)
        {
            if (_prefabs == null || prefabId < 0 || prefabId >= _prefabs.Length) return;
            GameObject prefabToSpawn = _prefabs[prefabId];
            if (prefabToSpawn != null)
            {
                SpawnObject(prefabToSpawn, position, rotation, _parentTransform);
            }
        }
#endif

        public int GetActiveObjectCount()
        {
            SpawnedObjects.RemoveAll(obj => obj == null);
            return SpawnedObjects.Count(obj => obj.activeInHierarchy);
        }

        // --- Random point inside colliders ---
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

