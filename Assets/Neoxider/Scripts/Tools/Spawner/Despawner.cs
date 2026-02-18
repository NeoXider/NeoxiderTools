using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Despawns the game object: returns to pool if it has <see cref="PooledObjectInfo" /> and pool, otherwise destroys.
    ///     Can run on disable, from code, or via Inspector [Button]. Optionally spawns a prefab at current position before
    ///     despawn; invokes UnityEvent.
    /// </summary>
    [NeoDoc("Tools/Spawner/Despawner.md")]
    [CreateFromMenu("Neoxider/Tools/Despawner")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Despawner))]
    public class Despawner : MonoBehaviour
    {
        [Header("When to despawn")] [Tooltip("Despawn when this component or GameObject is disabled.")] [SerializeField]
        private bool _despawnOnDisable;

        [Header("Spawn on despawn (optional)")] [SerializeField]
        private GameObject _spawnPrefabOnDespawn;

        [Tooltip("Parent for spawned object. If null, world space.")] [SerializeField]
        private Transform _spawnParent;

        [Tooltip(
            "Use this transform's position/rotation for spawned object. If false, uses world (0,0,0) and identity.")]
        [SerializeField]
        private bool _spawnAtThisTransform = true;

        [Header("Events")] [SerializeField] private UnityEvent _onDespawn = new();

        /// <summary>Fired before despawn with the GameObject that will be despawned (this).</summary>
        public UnityEvent OnDespawn => _onDespawn;

        private void OnDisable()
        {
            if (_despawnOnDisable)
            {
                Despawn();
            }
        }

        /// <summary>
        ///     Despawns this game object: if it has <see cref="PooledObjectInfo" /> and pool — returns to pool, otherwise
        ///     destroys.
        ///     Optionally spawns <see cref="_spawnPrefabOnDespawn" /> at current position first, then invokes
        ///     <see cref="OnDespawn" />.
        /// </summary>
        [Button("Despawn")]
        public void Despawn()
        {
            GameObject go = gameObject;
            if (!go.activeInHierarchy)
            {
                return;
            }

            if (_spawnPrefabOnDespawn != null)
            {
                Vector3 pos = _spawnAtThisTransform ? transform.position : Vector3.zero;
                Quaternion rot = _spawnAtThisTransform ? transform.rotation : Quaternion.identity;
                _spawnPrefabOnDespawn.SpawnFromPool(pos, rot, _spawnParent);
            }

            _onDespawn?.Invoke();

            if (go.TryGetComponent(out PooledObjectInfo info) && info.OwnerPool != null)
            {
                info.Return();
            }
            else
            {
                Destroy(go);
            }
        }

        /// <summary>
        ///     Despawns the given target (e.g. from UnityEvent with one argument). Use from Inspector: add listener, pass target
        ///     in the object field.
        /// </summary>
        public void DespawnOther(GameObject target)
        {
            DespawnObject(target);
        }

        /// <summary>
        ///     Despawns another object (e.g. from event). If the object has Despawner, calls its Despawn(); otherwise returns to
        ///     pool or destroys.
        /// </summary>
        public static void DespawnObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (target.TryGetComponent(out Despawner d))
            {
                d.Despawn();
                return;
            }

            if (target.TryGetComponent(out PooledObjectInfo info) && info.OwnerPool != null)
            {
                info.Return();
            }
            else
            {
                Destroy(target);
            }
        }
    }
}