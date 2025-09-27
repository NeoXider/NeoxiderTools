using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Neo
{
    /// <summary>
    ///     Universal parallax tiler that keeps a small pool of sprites aligned with the camera.
    ///     Tiles recycle seamlessly as the camera moves, optionally randomising their appearance.
    /// </summary>
    [DisallowMultipleComponent]
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Camera Binding")] [SerializeField]
        private Camera targetCamera;

        [Tooltip("0 - layer sticks to the camera, 1 - moves in the opposite direction with the same magnitude.")]
        [SerializeField]
        private Vector2 parallaxMultiplier = new(0.5f, 0f);

        [Tooltip("Additional world-space scrolling (units per second). Positive X moves layer right.")] [SerializeField]
        private Vector2 scrollSpeed = Vector2.zero;

        [Header("Editor Preview")]
        [Tooltip("Generate tile instances in editor mode for preview. Disable to hide tiles while editing.")]
        [SerializeField]
        private bool generateInEditor = true;

        [Header("Spacing")]
        [Tooltip("Дополнительный зазор между тайлами (мировые единицы). Положительные значения добавляют промежуток.")]
        [SerializeField]
        private Vector2 tileSpacing = Vector2.zero;

        [Header("Tiling")] [Tooltip("Enable horizontal tiling and recycling.")] [SerializeField]
        private bool tileHorizontally = true;

        [Tooltip("Enable vertical tiling and recycling.")] [SerializeField]
        private bool tileVertically;

        [Tooltip("Extra tile rows/columns to keep off-screen for smooth recycling (per axis).")] [SerializeField]
        private Vector2Int paddingTiles = new(1, 1);

        [Header("Sprites")] [SerializeField] private SpriteRenderer templateRenderer;

        [Tooltip("Optional additional sprites that can be used when spawning/recycling tiles.")] [SerializeField]
        private Sprite[] spriteVariants;

        [Tooltip("Try a random sprite variant when creating tiles.")] [SerializeField]
        private bool randomiseOnInit = true;

        [Tooltip("Randomise sprite when tile is recycled onto the opposite side.")] [SerializeField]
        private bool randomiseOnRecycle;

        [Tooltip(
            "Scale sprites so every tile matches the largest sprite bounds. Keep enabled to avoid gaps when sprites have different sizes.")]
        [SerializeField]
        private bool fitToMaxSpriteSize = true;

        private readonly List<Tile> tiles = new();
        private Vector2 accumulatedScroll;
        private Sprite[] availableSprites = Array.Empty<Sprite>();

        private Vector2 cellSizeLocal;
        private Vector2 cellSizeWorld;
        private float gridHeightWorld;
        private float gridWidthWorld;
        private bool hasTemplateSnapshot;
        private Vector3 initialCameraPosition;
        private Vector3 initialLayerPosition;
        private bool isInitialised;
        private Vector2 spacingLocal;
        private Vector2 spacingWorld;
        private Vector3 templateBaseScale;
        private Vector3 templateInitialLocalPosition;
        private Quaternion templateInitialLocalRotation;
        private Vector3 templateInitialLocalScale;
        private Sprite templateInitialSprite;
        private int tilesX = 1;
        private int tilesY = 1;

        private void Awake()
        {
            targetCamera ??= Camera.main;
            templateRenderer ??= GetComponent<SpriteRenderer>();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (generateInEditor) Initialise();

                return;
            }
#endif

            Initialise();
        }

        private void LateUpdate()
        {
            if (!isInitialised) return;

            if (targetCamera == null) return;

            UpdateParallaxOffset();
            RecycleTiles();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (generateInEditor && !isInitialised) Initialise();

                return;
            }
#endif

            if (!isInitialised) Initialise();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Cleanup(true);
                return;
            }
#endif

            if (Application.isPlaying) Cleanup();
        }

        private void OnValidate()
        {
            paddingTiles.x = Mathf.Max(0, paddingTiles.x);
            paddingTiles.y = Mathf.Max(0, paddingTiles.y);

            templateRenderer ??= GetComponent<SpriteRenderer>();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (!isActiveAndEnabled)
                {
                    Cleanup(true);
                    return;
                }

                if (generateInEditor)
                    Initialise();
                else
                    Cleanup(true);

                return;
            }
#endif

            if (Application.isPlaying && isActiveAndEnabled) Initialise();
        }

        private void Initialise()
        {
            var currentPosition = transform.position;

            hasTemplateSnapshot = false;

            if (isInitialised)
                Cleanup(!Application.isPlaying);
            else
                tiles.Clear();

            if (targetCamera == null) targetCamera = Camera.main;

            if (targetCamera == null)
            {
                Debug.LogWarning($"[{nameof(ParallaxLayer)}] Camera not assigned and no MainCamera found.", this);
                return;
            }

            if (templateRenderer == null) templateRenderer = GetComponent<SpriteRenderer>();

            if (templateRenderer == null)
            {
                Debug.LogWarning($"[{nameof(ParallaxLayer)}] SpriteRenderer template is missing.", this);
                return;
            }

            if (templateRenderer.sprite == null && (spriteVariants == null || spriteVariants.Length == 0))
            {
                Debug.LogWarning($"[{nameof(ParallaxLayer)}] No sprites defined for tiling.", this);
                return;
            }

            templateBaseScale = templateRenderer.transform.localScale;
            templateInitialSprite = templateRenderer.sprite;
            templateInitialLocalPosition = templateRenderer.transform.localPosition;
            templateInitialLocalRotation = templateRenderer.transform.localRotation;
            templateInitialLocalScale = templateRenderer.transform.localScale;
            hasTemplateSnapshot = true;

            ComputeCellSize();
            BuildSpritePool();
            ComputeTileCounts();

            BuildTiles();

            initialCameraPosition = targetCamera.transform.position;
            initialLayerPosition = currentPosition;
            accumulatedScroll = Vector2.zero;
            isInitialised = true;
        }

        private void ComputeCellSize()
        {
            var maxLocalSize = Vector2.zero;

            Vector2 SampleSize(Sprite sprite)
            {
                if (sprite == null) return Vector2.zero;

                if (templateRenderer.drawMode == SpriteDrawMode.Simple) return sprite.bounds.size;

                if (templateRenderer.drawMode == SpriteDrawMode.Sliced ||
                    templateRenderer.drawMode == SpriteDrawMode.Tiled)
                    return templateRenderer.size;

                return sprite.bounds.size;
            }

            var size = SampleSize(templateInitialSprite);
            maxLocalSize = new Vector2(
                Mathf.Max(maxLocalSize.x, size.x),
                Mathf.Max(maxLocalSize.y, size.y));
            if (spriteVariants != null)
                foreach (var sprite in spriteVariants)
                {
                    size = SampleSize(sprite);
                    maxLocalSize.x = Mathf.Max(maxLocalSize.x, size.x);
                    maxLocalSize.y = Mathf.Max(maxLocalSize.y, size.y);
                }

            if (maxLocalSize == Vector2.zero)
                maxLocalSize = templateInitialSprite != null ? templateInitialSprite.bounds.size : Vector2.one;

            cellSizeLocal = maxLocalSize;
            var lossy = transform.lossyScale;
            cellSizeWorld = new Vector2(
                cellSizeLocal.x * Mathf.Abs(lossy.x),
                cellSizeLocal.y * Mathf.Abs(lossy.y));

            spacingWorld = tileSpacing;
            spacingLocal = new Vector2(
                Mathf.Approximately(Mathf.Abs(lossy.x), 0f) ? 0f : tileSpacing.x / Mathf.Abs(lossy.x),
                Mathf.Approximately(Mathf.Abs(lossy.y), 0f) ? 0f : tileSpacing.y / Mathf.Abs(lossy.y));
        }

        private void ComputeTileCounts()
        {
            var cam = targetCamera;
            if (cam.orthographic)
            {
                var halfHeight = cam.orthographicSize;
                var halfWidth = halfHeight * cam.aspect;

                tilesX = tileHorizontally
                    ? Mathf.Max(3,
                        Mathf.CeilToInt(halfWidth * 2f / Mathf.Max(cellSizeWorld.x + spacingWorld.x, 0.0001f)) + 2 +
                        paddingTiles.x * 2)
                    : 1;
                tilesY = tileVertically
                    ? Mathf.Max(3,
                        Mathf.CeilToInt(halfHeight * 2f / Mathf.Max(cellSizeWorld.y + spacingWorld.y, 0.0001f)) + 2 +
                        paddingTiles.y * 2)
                    : 1;
            }
            else
            {
                tilesX = tileHorizontally ? Mathf.Max(3, 6 + paddingTiles.x * 2) : 1;
                tilesY = tileVertically ? Mathf.Max(3, 4 + paddingTiles.y * 2) : 1;
            }

            gridWidthWorld = (cellSizeWorld.x + spacingWorld.x) * tilesX;
            gridHeightWorld = (cellSizeWorld.y + spacingWorld.y) * tilesY;
        }

        private void BuildTiles()
        {
            RemoveExistingTiles(!Application.isPlaying);
            tiles.Clear();

            templateRenderer.transform.localScale = templateBaseScale;

            var anchorX = (tilesX - 1) / 2;
            var anchorY = (tilesY - 1) / 2;
            Vector2 origin = new(
                templateInitialLocalPosition.x - anchorX * (cellSizeLocal.x + spacingLocal.x),
                templateInitialLocalPosition.y - anchorY * (cellSizeLocal.y + spacingLocal.y));

            for (var y = 0; y < tilesY; y++)
            for (var x = 0; x < tilesX; x++)
            {
                var tile = CreateTile();
                Vector3 localPos = new(
                    origin.x + x * (cellSizeLocal.x + spacingLocal.x),
                    origin.y + y * (cellSizeLocal.y + spacingLocal.y),
                    templateInitialLocalPosition.z);
                tile.Transform.localPosition = localPos;

                AssignSprite(tile, randomiseOnInit);
                tiles.Add(tile);
            }

            templateRenderer.enabled = false;
            templateRenderer.sprite = templateInitialSprite;
            templateRenderer.transform.localPosition = templateInitialLocalPosition;
            templateRenderer.transform.localRotation = templateInitialLocalRotation;
            templateRenderer.transform.localScale = templateInitialLocalScale;
        }

        private void RemoveExistingTiles(bool immediate)
        {
            List<Transform> toRemove = new();
            foreach (Transform child in transform)
            {
                if (templateRenderer != null && child == templateRenderer.transform) continue;

                toRemove.Add(child);
            }

            for (var i = 0; i < toRemove.Count; i++)
            {
                var child = toRemove[i];
                if (child == null) continue;

                if (Application.isPlaying && !immediate)
                {
                    Destroy(child.gameObject);
                }
                else
                {
#if UNITY_EDITOR
                    DestroyImmediate(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
                }
            }
        }

        private Tile CreateTile()
        {
            GameObject go = new($"Tile_{tiles.Count}");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            CopyRendererSettings(templateRenderer, renderer);
            renderer.transform.localRotation = templateInitialLocalRotation;
            renderer.transform.localScale = templateInitialLocalScale;
            renderer.enabled = true;

            if (templateInitialSprite != null) renderer.sprite = templateInitialSprite;

            return new Tile(go.transform, renderer);
        }

        private static void CopyRendererSettings(SpriteRenderer source, SpriteRenderer target)
        {
            target.sprite = source.sprite;
            target.color = source.color;
            target.flipX = source.flipX;
            target.flipY = source.flipY;
            target.drawMode = source.drawMode;
#if UNITY_2021_2_OR_NEWER
            target.tileMode = source.tileMode;
#endif
            target.size = source.size;
            target.maskInteraction = source.maskInteraction;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder;
            target.sharedMaterial = source.sharedMaterial;
            target.shadowCastingMode = source.shadowCastingMode;
            target.receiveShadows = source.receiveShadows;
            target.lightProbeUsage = source.lightProbeUsage;
            target.reflectionProbeUsage = source.reflectionProbeUsage;
        }

        private void BuildSpritePool()
        {
            List<Sprite> pool = new();
            if (templateInitialSprite != null) pool.Add(templateInitialSprite);

            if (spriteVariants != null)
                foreach (var sprite in spriteVariants)
                    if (sprite != null && !pool.Contains(sprite))
                        pool.Add(sprite);

            availableSprites = pool.Count > 0 ? pool.ToArray() : Array.Empty<Sprite>();
        }

        private void AssignSprite(Tile tile, bool allowRandom)
        {
            var sprite = templateInitialSprite;

            if (allowRandom && availableSprites.Length > 0)
                sprite = availableSprites[Random.Range(0, availableSprites.Length)];
            else if (sprite == null && availableSprites.Length > 0) sprite = availableSprites[0];

            if (sprite == null) return;

            tile.Renderer.sprite = sprite;

            if (templateRenderer.drawMode != SpriteDrawMode.Simple)
            {
                tile.Transform.localScale = templateBaseScale;
                tile.Renderer.size = templateRenderer.size;
                return;
            }

            if (!fitToMaxSpriteSize)
            {
                tile.Transform.localScale = templateBaseScale;
                return;
            }

            Vector2 spriteSize = sprite.bounds.size;
            if (spriteSize == Vector2.zero) spriteSize = Vector2.one;

            var scaleX = spriteSize.x <= 0f ? 1f : cellSizeLocal.x / spriteSize.x;
            var scaleY = spriteSize.y <= 0f ? 1f : cellSizeLocal.y / spriteSize.y;

            tile.Transform.localScale = new Vector3(
                templateBaseScale.x * scaleX,
                templateBaseScale.y * scaleY,
                templateBaseScale.z);
        }

        private void Cleanup(bool immediate = false)
        {
            RemoveExistingTiles(immediate);
            tiles.Clear();
            isInitialised = false;
            accumulatedScroll = Vector2.zero;

            if (templateRenderer != null && hasTemplateSnapshot)
            {
                templateRenderer.transform.localPosition = templateInitialLocalPosition;
                templateRenderer.transform.localRotation = templateInitialLocalRotation;
                templateRenderer.transform.localScale = templateInitialLocalScale;
                templateRenderer.enabled = true;
                if (templateInitialSprite != null) templateRenderer.sprite = templateInitialSprite;
            }
        }

        private void UpdateParallaxOffset()
        {
            var camPos = targetCamera.transform.position;
            var delta = camPos - initialCameraPosition;
            accumulatedScroll += scrollSpeed * Time.deltaTime;
            Vector3 offset = new(
                delta.x * -parallaxMultiplier.x + accumulatedScroll.x,
                delta.y * -parallaxMultiplier.y + accumulatedScroll.y,
                0f);
            transform.position = initialLayerPosition + offset;
        }

        private void RecycleTiles()
        {
            var camPos = targetCamera.transform.position;
            var halfWidth = targetCamera.orthographic ? targetCamera.orthographicSize * targetCamera.aspect : 10f;
            var halfHeight = targetCamera.orthographic ? targetCamera.orthographicSize : 10f;

            var thresholdX = tileHorizontally ? halfWidth + cellSizeWorld.x + spacingWorld.x : float.PositiveInfinity;
            var thresholdY = tileVertically ? halfHeight + cellSizeWorld.y + spacingWorld.y : float.PositiveInfinity;

            for (var i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                var tilePos = tile.Transform.position;

                if (tileHorizontally)
                {
                    var deltaX = camPos.x - tilePos.x;
                    while (deltaX > thresholdX)
                    {
                        ShiftTile(tile, Vector3.right * gridWidthWorld);
                        tilePos = tile.Transform.position;
                        deltaX = camPos.x - tilePos.x;
                    }

                    while (deltaX < -thresholdX)
                    {
                        ShiftTile(tile, Vector3.left * gridWidthWorld);
                        tilePos = tile.Transform.position;
                        deltaX = camPos.x - tilePos.x;
                    }
                }

                if (tileVertically)
                {
                    var deltaY = camPos.y - tilePos.y;
                    while (deltaY > thresholdY)
                    {
                        ShiftTile(tile, Vector3.up * gridHeightWorld);
                        tilePos = tile.Transform.position;
                        deltaY = camPos.y - tilePos.y;
                    }

                    while (deltaY < -thresholdY)
                    {
                        ShiftTile(tile, Vector3.down * gridHeightWorld);
                        tilePos = tile.Transform.position;
                        deltaY = camPos.y - tilePos.y;
                    }
                }
            }
        }

        private void ShiftTile(Tile tile, Vector3 worldOffset)
        {
            tile.Transform.position += worldOffset;
            if (randomiseOnRecycle) AssignSprite(tile, true);
        }

        private readonly struct Tile
        {
            public Tile(Transform transform, SpriteRenderer renderer)
            {
                Transform = transform;
                Renderer = renderer;
            }

            public Transform Transform { get; }
            public SpriteRenderer Renderer { get; }
        }
    }
}