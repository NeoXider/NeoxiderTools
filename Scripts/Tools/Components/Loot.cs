using Neo.Extensions;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>Plane used to scatter dropped loot around the <see cref="Loot" /> object.</summary>
    public enum LootScatterPlane
    {
        /// <summary>Horizontal X/Z scatter at the object's height (3D games).</summary>
        XZ = 0,

        /// <summary>X/Y scatter at the object's depth (2D games).</summary>
        XY = 1
    }

    /// <summary>
    ///     Drops random prefabs around this object: automatically when it is destroyed during gameplay, or on demand
    ///     through <see cref="DropLoot" />.
    /// </summary>
    [NeoDoc("Tools/Components/Loot.md")]
    [CreateFromMenu("Neoxider/Tools/Components/Loot")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Loot))]
    public class Loot : MonoBehaviour
    {
        private static bool _applicationQuitting;

        [Tooltip("Prefabs to pick from; each dropped item is chosen uniformly at random.")]
        public GameObject[] lootItems;

        [Space] [Range(0, 1)] [Tooltip("Probability (0..1) that one DropLoot call drops anything at all.")]
        public float dropChance = 1;

        [Space] [Header("Settings")]
        [Tooltip("Drop automatically when this object is destroyed during gameplay (not on scene unload or quit).")]
        public bool spawnOnDestroy = true;

        [Min(0)] [Tooltip("Minimum number of items per successful drop.")]
        public int minCount = 1;

        [Min(0)] [Tooltip("Maximum number of items per successful drop (inclusive).")]
        public int maxCount = 3;

        [Tooltip("Random scatter radius around this object (0 = drop exactly at its position).")]
        public float dropRadius;

        [Tooltip("XZ keeps the object's height (3D); XY keeps its depth (2D).")]
        [SerializeField]
        private LootScatterPlane _scatterPlane = LootScatterPlane.XZ;

        /// <summary>Plane used to scatter drops: <see cref="LootScatterPlane.XZ" /> for 3D, <see cref="LootScatterPlane.XY" /> for 2D.</summary>
        public LootScatterPlane ScatterPlane
        {
            get => _scatterPlane;
            set => _scatterPlane = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _applicationQuitting = false;
            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
        }

        private static void OnApplicationQuitting()
        {
            _applicationQuitting = true;
        }

        private void OnDestroy()
        {
            if (spawnOnDestroy &&
                ShouldDropOnDestroy(Application.isPlaying, gameObject.scene.isLoaded, _applicationQuitting))
            {
                DropLoot();
            }
        }

        /// <summary>
        ///     Destroy-time drops only happen for gameplay destruction: never in Edit Mode, while the owning scene
        ///     unloads (the spawned objects would leak into the next scene) or while the application quits.
        /// </summary>
        internal static bool ShouldDropOnDestroy(bool isPlaying, bool sceneLoaded, bool applicationQuitting)
        {
            return isPlaying && sceneLoaded && !applicationQuitting;
        }

        /// <summary>
        ///     Rolls <see cref="dropChance" /> and, on success, instantiates <see cref="minCount" />..<see cref="maxCount" />
        ///     random prefabs scattered within <see cref="dropRadius" />.
        /// </summary>
        public void DropLoot()
        {
            if (lootItems == null || lootItems.Length == 0)
            {
                return;
            }

            if (dropChance.Chance())
            {
                int lootCount = minCount.RandomToValue(maxCount + 1);

                for (int i = 0; i < lootCount; i++)
                {
                    GameObject selectedItem = GetRandomPrefab();

                    if (selectedItem != null)
                    {
                        Instantiate(selectedItem, GetDropPosition(), Quaternion.identity);
                    }
                }
            }
        }

        private Vector3 GetDropPosition()
        {
            Vector3 origin = transform.position;
            if (_scatterPlane == LootScatterPlane.XY)
            {
                Vector2 offset = Random.insideUnitCircle * dropRadius;
                return new Vector3(origin.x + offset.x, origin.y + offset.y, origin.z);
            }

            Vector3 dropPosition = origin + Random.insideUnitSphere * dropRadius;
            dropPosition.y = origin.y;
            return dropPosition;
        }

        private GameObject GetRandomPrefab()
        {
            if (lootItems == null || lootItems.Length == 0)
            {
                return null;
            }

            if (lootItems.Length == 1)
            {
                return lootItems[0];
            }

            return lootItems.GetRandomElement();
        }
    }
}
