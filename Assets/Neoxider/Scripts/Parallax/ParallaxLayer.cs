using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Random = UnityEngine.Random;

namespace Neo.Parallax
{
    /// <summary>
    ///     Universal parallax tiler that keeps a small pool of sprites aligned with the camera.
    ///     Tiles recycle seamlessly as the camera moves, optionally randomising their appearance.
    /// </summary>
    [DisallowMultipleComponent]
    [NeoDoc("Parallax/ParallaxLayer.md")]
    [CreateFromMenu("Neoxider/Parallax/ParallaxLayer")]
    [AddComponentMenu("Neoxider/" + "Parallax/" + nameof(ParallaxLayer))]
    [MovedFrom(true, sourceNamespace: "Neo")]
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Camera Binding")] [SerializeField]
        private Camera targetCamera;

        [Tooltip(
            "Resolve Camera.main only when Target Camera is empty. Disable when the camera is injected by scene setup.")]
        [SerializeField]
        private bool useMainCameraFallback = true;

        [Tooltip(
            "Log missing camera through NeoDiagnostics. Warnings are still controlled by the global diagnostics gate.")]
        [SerializeField]
        private bool logMissingCamera;

        [Tooltip("0 - layer stays at its initial world position, 1 - moves opposite to the camera delta.")]
        [SerializeField]
        private Vector2 parallaxMultiplier = new(0.5f, 0f);

        [Tooltip("Additional world-space scrolling (units per second). Positive X moves layer right.")] [SerializeField]
        private Vector2 scrollSpeed = Vector2.zero;

        [Header("Editor Preview")]
        [Tooltip("Generate tile instances in editor mode for preview. Disable to hide tiles while editing.")]
        [SerializeField]
        private bool generateInEditor = true;

        [Header("Spacing")]
        [Tooltip("Extra gap between tiles (world units). Positive values add spacing.")]
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
        private Vector2 maxSpriteSize;
        private float gridHeightWorld;
        private float gridWidthWorld;
        private bool hasTemplateSnapshot;
        private Vector3 initialCameraPosition;
        private Vector3 initialLayerPosition;
        private bool isInitialised;
        private bool missingCameraLogged;
        private double pitchLocalX;
        private double pitchLocalY;
        private double originLocalX;
        private double originLocalY;
        private Vector2 spacingWorld;
        private Vector3 templateBaseScale;
        private SpriteRenderer templateSnapshotRenderer;
        private bool templateInitialEnabled;
        private Vector3 templateInitialLocalPosition;
        private Quaternion templateInitialLocalRotation;
        private Vector3 templateInitialLocalScale;
        private Sprite templateInitialSprite;
        private Vector3 templateTileLocalPosition;
        private Quaternion templateTileLocalRotation;
        private int tilesX = 1;
        private int tilesY = 1;

#if UNITY_EDITOR
        private bool editorPreviewRefreshQueued;
#endif

        private void Awake()
        {
            templateRenderer ??= GetComponent<SpriteRenderer>();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (generateInEditor)
                {
                    Initialise();
                }

                return;
            }
#endif

            Initialise();
        }

        private void LateUpdate()
        {
            if (!isInitialised)
            {
                return;
            }

            if (targetCamera == null)
            {
                return;
            }

            UpdateParallaxOffset();
            RecycleTiles();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (generateInEditor && !isInitialised)
                {
                    Initialise();
                }

                return;
            }
#endif

            if (!isInitialised)
            {
                Initialise();
            }
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= RefreshEditorPreviewDelayed;
            editorPreviewRefreshQueued = false;
#endif

            Cleanup();
        }

        private void OnDestroy()
        {
            OnDisable();
        }

        private void OnValidate()
        {
            paddingTiles.x = Mathf.Max(0, paddingTiles.x);
            paddingTiles.y = Mathf.Max(0, paddingTiles.y);

#if UNITY_EDITOR
            QueueEditorPreviewRefresh();
#else
            if (Application.isPlaying && isActiveAndEnabled)
            {
                Initialise();
            }
#endif
        }

        public Camera TargetCamera => targetCamera;

        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            missingCameraLogged = false;

            if (isActiveAndEnabled)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying && !generateInEditor)
                {
                    Cleanup();
                    return;
                }
#endif

                Initialise();
            }
        }

#if UNITY_EDITOR
        private void QueueEditorPreviewRefresh()
        {
            if (editorPreviewRefreshQueued)
            {
                return;
            }

            editorPreviewRefreshQueued = true;
            UnityEditor.EditorApplication.delayCall += RefreshEditorPreviewDelayed;
        }

        private void RefreshEditorPreviewDelayed()
        {
            editorPreviewRefreshQueued = false;

            if (this == null)
            {
                return;
            }

            if (!isActiveAndEnabled || (!Application.isPlaying && !generateInEditor))
            {
                Cleanup();
                return;
            }

            Initialise();
        }
#endif

        private void Initialise()
        {
            Vector3 currentPosition = transform.position;

            Cleanup(false);

            targetCamera = ResolveTargetCamera();

            if (targetCamera == null)
            {
                return;
            }

            if (templateRenderer == null)
            {
                templateRenderer = GetComponent<SpriteRenderer>();
            }

            if (templateRenderer == null)
            {
                NeoDiagnostics.LogWarning($"[{nameof(ParallaxLayer)}] SpriteRenderer template is missing.", this);
                return;
            }

            templateInitialSprite = templateRenderer.sprite;
            BuildSpritePool();
            if (availableSprites.Length == 0)
            {
                NeoDiagnostics.LogWarning($"[{nameof(ParallaxLayer)}] No sprites defined for tiling.", this);
                return;
            }

            templateSnapshotRenderer = templateRenderer;
            templateInitialEnabled = templateRenderer.enabled;
            templateInitialLocalPosition = templateRenderer.transform.localPosition;
            templateInitialLocalRotation = templateRenderer.transform.localRotation;
            templateInitialLocalScale = templateRenderer.transform.localScale;

            Matrix4x4 templateToLayer = transform.worldToLocalMatrix * templateRenderer.transform.localToWorldMatrix;
            templateTileLocalPosition = templateToLayer.MultiplyPoint3x4(Vector3.zero);
            templateTileLocalRotation = templateRenderer.transform == transform
                ? Quaternion.identity
                : Quaternion.Inverse(transform.rotation) * templateRenderer.transform.rotation;
            templateBaseScale = templateRenderer.transform == transform
                ? Vector3.one
                : templateRenderer.transform.parent == transform
                    ? templateInitialLocalScale
                    : templateToLayer.lossyScale;
            hasTemplateSnapshot = true;

            ComputeCellSize();
            float strideX = cellSizeWorld.x + spacingWorld.x;
            float strideY = cellSizeWorld.y + spacingWorld.y;
            if ((tileHorizontally && (strideX <= 0.0001f || float.IsNaN(strideX) || float.IsInfinity(strideX))) ||
                (tileVertically && (strideY <= 0.0001f || float.IsNaN(strideY) || float.IsInfinity(strideY))))
            {
                NeoDiagnostics.LogWarning($"[{nameof(ParallaxLayer)}] Tiled axes require a finite, positive tile stride.",
                    this);
                return;
            }

            if (!ComputeTileCounts() || !ComputeLocalGeometry())
            {
                NeoDiagnostics.LogWarning(
                    $"[{nameof(ParallaxLayer)}] Tile counts, grid dimensions or local positions exceed supported numeric limits.",
                    this);
                return;
            }

            BuildTiles();

            initialCameraPosition = targetCamera.transform.position;
            initialLayerPosition = currentPosition;
            accumulatedScroll = Vector2.zero;
            isInitialised = true;
        }

        private Camera ResolveTargetCamera()
        {
            if (targetCamera != null)
            {
                return targetCamera;
            }

            if (useMainCameraFallback)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null && logMissingCamera && !missingCameraLogged)
            {
                missingCameraLogged = true;
                NeoDiagnostics.LogWarningThrottled(
                    $"{nameof(ParallaxLayer)}.{NeoDiagnostics.StableId(this)}.MissingCamera",
                    $"[{nameof(ParallaxLayer)}] Target Camera is not assigned and MainCamera fallback is unavailable.",
                    this,
                    5f);
            }

            return targetCamera;
        }

        private void ComputeCellSize()
        {
            Vector2 maxLocalSize = Vector2.zero;

            Vector2 SampleSize(Sprite sprite)
            {
                if (sprite == null)
                {
                    return Vector2.zero;
                }

                if (templateRenderer.drawMode == SpriteDrawMode.Simple)
                {
                    return sprite.bounds.size;
                }

                if (templateRenderer.drawMode == SpriteDrawMode.Sliced ||
                    templateRenderer.drawMode == SpriteDrawMode.Tiled)
                {
                    return templateRenderer.size;
                }

                return sprite.bounds.size;
            }

            Vector2 size = SampleSize(templateInitialSprite);
            maxLocalSize = new Vector2(
                Mathf.Max(maxLocalSize.x, size.x),
                Mathf.Max(maxLocalSize.y, size.y));
            if (spriteVariants != null)
            {
                foreach (Sprite sprite in spriteVariants)
                {
                    size = SampleSize(sprite);
                    maxLocalSize.x = Mathf.Max(maxLocalSize.x, size.x);
                    maxLocalSize.y = Mathf.Max(maxLocalSize.y, size.y);
                }
            }

            if (maxLocalSize == Vector2.zero)
            {
                maxLocalSize = templateInitialSprite != null ? templateInitialSprite.bounds.size : Vector2.one;
            }

            maxSpriteSize = maxLocalSize;
            cellSizeLocal = new Vector2(
                maxLocalSize.x * Mathf.Abs(templateBaseScale.x),
                maxLocalSize.y * Mathf.Abs(templateBaseScale.y));
            Vector3 lossy = transform.lossyScale;
            cellSizeWorld = new Vector2(
                cellSizeLocal.x * Mathf.Abs(lossy.x),
                cellSizeLocal.y * Mathf.Abs(lossy.y));

            spacingWorld = tileSpacing;
            // Disabled axes keep the source coordinate without evaluating unused spacing.
            pitchLocalX = tileHorizontally ? cellSizeLocal.x + (double)tileSpacing.x / Math.Abs((double)lossy.x) : 0d;
            pitchLocalY = tileVertically ? cellSizeLocal.y + (double)tileSpacing.y / Math.Abs((double)lossy.y) : 0d;
        }

        private bool ComputeTileCounts()
        {
            Camera cam = targetCamera;
            float strideX = cellSizeWorld.x + spacingWorld.x;
            float strideY = cellSizeWorld.y + spacingWorld.y;
            double paddingX = Math.Max(0, paddingTiles.x) * 2d;
            double paddingY = Math.Max(0, paddingTiles.y) * 2d;
            double requestedTilesX;
            double requestedTilesY;

            if (cam.orthographic)
            {
                double height = (double)cam.orthographicSize * 2d;
                double width = height * cam.aspect;
                if ((tileHorizontally && (double.IsNaN(width) || double.IsInfinity(width) || width <= 0d)) ||
                    (tileVertically && (double.IsNaN(height) || double.IsInfinity(height) || height <= 0d)))
                {
                    return false;
                }

                requestedTilesX = tileHorizontally ? Math.Max(3d, Math.Ceiling(width / strideX) + 2d + paddingX) : 1d;
                requestedTilesY = tileVertically ? Math.Max(3d, Math.Ceiling(height / strideY) + 2d + paddingY) : 1d;
            }
            else
            {
                requestedTilesX = tileHorizontally ? 6d + paddingX : 1d;
                requestedTilesY = tileVertically ? 4d + paddingY : 1d;
            }

            if (double.IsNaN(requestedTilesX) || double.IsInfinity(requestedTilesX) ||
                double.IsNaN(requestedTilesY) || double.IsInfinity(requestedTilesY) ||
                requestedTilesX > int.MaxValue || requestedTilesY > int.MaxValue ||
                requestedTilesX * requestedTilesY > int.MaxValue)
            {
                return false;
            }

            double widthWorld = tileHorizontally ? strideX * requestedTilesX : 0d;
            double heightWorld = tileVertically ? strideY * requestedTilesY : 0d;
            if (double.IsNaN(widthWorld) || double.IsInfinity(widthWorld) || widthWorld > float.MaxValue ||
                double.IsNaN(heightWorld) || double.IsInfinity(heightWorld) || heightWorld > float.MaxValue)
            {
                return false;
            }

            tilesX = (int)requestedTilesX;
            tilesY = (int)requestedTilesY;
            gridWidthWorld = (float)widthWorld;
            gridHeightWorld = (float)heightWorld;
            return true;
        }

        private bool ComputeLocalGeometry()
        {
            if ((tileHorizontally && !(pitchLocalX > 0d && pitchLocalX <= float.MaxValue)) ||
                (tileVertically && !(pitchLocalY > 0d && pitchLocalY <= float.MaxValue)))
            {
                return false;
            }

            int anchorX = (tilesX - 1) / 2;
            int anchorY = (tilesY - 1) / 2;
            double firstX = templateTileLocalPosition.x - anchorX * pitchLocalX;
            double firstY = templateTileLocalPosition.y - anchorY * pitchLocalY;
            double lastX = firstX + (tilesX - 1) * pitchLocalX;
            double lastY = firstY + (tilesY - 1) * pitchLocalY;
            double furthestX = Math.Max(Math.Abs(firstX), Math.Abs(lastX));
            double furthestY = Math.Max(Math.Abs(firstY), Math.Abs(lastY));

            // Positive pitches make the first/last coordinates bound every generated position.
            if (!(furthestX <= float.MaxValue && furthestY <= float.MaxValue) ||
                float.IsNaN(templateTileLocalPosition.z) || float.IsInfinity(templateTileLocalPosition.z))
            {
                return false;
            }

            originLocalX = firstX;
            originLocalY = firstY;
            return true;
        }

        private void BuildTiles()
        {
            RemoveExistingTiles();
            tiles.Clear();

            for (int y = 0; y < tilesY; y++)
            for (int x = 0; x < tilesX; x++)
            {
                Vector3 localPos = new(
                    (float)(originLocalX + x * pitchLocalX),
                    (float)(originLocalY + y * pitchLocalY),
                    templateTileLocalPosition.z);
                Tile tile = CreateTile();
                tile.Transform.localPosition = localPos;

                AssignSprite(tile, randomiseOnInit);
                tiles.Add(tile);
            }

            templateRenderer.enabled = false;
        }

        private void RemoveExistingTiles()
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                Transform child = tiles[i].Transform;
                if (child == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    child.gameObject.SetActive(false);
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
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            CopyRendererSettings(templateRenderer, renderer);
            renderer.transform.localRotation = templateTileLocalRotation;
            renderer.transform.localScale = templateBaseScale;
            renderer.enabled = true;

            if (templateInitialSprite != null)
            {
                renderer.sprite = templateInitialSprite;
            }

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
            if (templateInitialSprite != null)
            {
                pool.Add(templateInitialSprite);
            }

            if (spriteVariants != null)
            {
                foreach (Sprite sprite in spriteVariants)
                {
                    if (sprite != null && !pool.Contains(sprite))
                    {
                        pool.Add(sprite);
                    }
                }
            }

            availableSprites = pool.Count > 0 ? pool.ToArray() : Array.Empty<Sprite>();
        }

        private void AssignSprite(Tile tile, bool allowRandom)
        {
            Sprite sprite = templateInitialSprite;

            if (allowRandom && availableSprites.Length > 0)
            {
                sprite = availableSprites[Random.Range(0, availableSprites.Length)];
            }
            else if (sprite == null && availableSprites.Length > 0)
            {
                sprite = availableSprites[0];
            }

            if (sprite == null)
            {
                return;
            }

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
            if (spriteSize == Vector2.zero)
            {
                spriteSize = Vector2.one;
            }

            float scaleX = spriteSize.x <= 0f ? 1f : maxSpriteSize.x / spriteSize.x;
            float scaleY = spriteSize.y <= 0f ? 1f : maxSpriteSize.y / spriteSize.y;

            tile.Transform.localScale = new Vector3(
                templateBaseScale.x * scaleX,
                templateBaseScale.y * scaleY,
                templateBaseScale.z);
        }

        private void Cleanup(bool restoreTemplateTransform = true)
        {
            RemoveExistingTiles();
            tiles.Clear();
            isInitialised = false;
            accumulatedScroll = Vector2.zero;

            if (templateSnapshotRenderer != null && hasTemplateSnapshot)
            {
                if (restoreTemplateTransform)
                {
                    templateSnapshotRenderer.transform.localPosition = templateInitialLocalPosition;
                    templateSnapshotRenderer.transform.localRotation = templateInitialLocalRotation;
                    templateSnapshotRenderer.transform.localScale = templateInitialLocalScale;
                }

                templateSnapshotRenderer.enabled = templateInitialEnabled;
            }

            hasTemplateSnapshot = false;
            templateSnapshotRenderer = null;
        }

        private void UpdateParallaxOffset()
        {
            Vector3 camPos = targetCamera.transform.position;
            Vector3 delta = camPos - initialCameraPosition;
            accumulatedScroll += scrollSpeed * Time.deltaTime;
            Vector3 offset = new(
                delta.x * -parallaxMultiplier.x + accumulatedScroll.x,
                delta.y * -parallaxMultiplier.y + accumulatedScroll.y,
                0f);
            transform.position = initialLayerPosition + offset;
        }

        private void RecycleTiles()
        {
            Vector3 camPos = targetCamera.transform.position;

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile tile = tiles[i];
                if (tile.Transform == null || tile.Renderer == null)
                {
                    continue;
                }

                Vector3 tilePos = tile.Transform.position;
                Vector3 offset = Vector3.zero;

                if (tileHorizontally)
                {
                    offset.x = Mathf.Floor((camPos.x - tilePos.x + gridWidthWorld * 0.5f) / gridWidthWorld) *
                               gridWidthWorld;
                }

                if (tileVertically)
                {
                    offset.y = Mathf.Floor((camPos.y - tilePos.y + gridHeightWorld * 0.5f) / gridHeightWorld) *
                               gridHeightWorld;
                }

                if (offset != Vector3.zero)
                {
                    ShiftTile(tile, offset);
                }
            }
        }

        private void ShiftTile(Tile tile, Vector3 worldOffset)
        {
            tile.Transform.position += worldOffset;
            if (randomiseOnRecycle)
            {
                AssignSprite(tile, true);
            }
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
