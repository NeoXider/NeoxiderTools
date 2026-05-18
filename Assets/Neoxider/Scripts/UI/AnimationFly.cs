using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Neo
{
    public enum AnimationFlyCoordinateSpace
    {
        Auto = 0,
        World = 1,
        Canvas = 2,
        Screen = 3
    }

    public enum AnimationFlySpawnSpace
    {
        Auto = 0,
        World = 1,
        Canvas = 2
    }

    [CreateFromMenu("Neoxider/UI/AnimationFly")]
    [AddComponentMenu("Neoxider/UI/" + nameof(AnimationFly))]
    [NeoDoc("UI/AnimationFly.md")]
    public class AnimationFly : Singleton<AnimationFly>
    {
        public float arcStrength = 2.0f;
#if ODIN_INSPECTOR
        [TableList]
#endif
        public List<BonusPrefabData> bonusPrefabList = new();

        [Tooltip("Multiplier for number of spawned objects")]
        public float countMultiplier = 1f;

        public float delayBetweenBonuses = 0.1f;
        public Ease easyEnd = Ease.InQuad;

        [Space] public Ease easyStart = Ease.OutQuad;

        [Header("Animation")] public float flyDuration = 1.0f;
        public bool ignoreZ;

        [Tooltip("Max number of objects per call")]
        public int maxBonusCount = 1000;

        [Range(0, 1)] public float middlePoint = 0.4f;
        public float multY = 0.5f;

        public Canvas parentCanvas;

        public float scaleMult = 1;

        [Header("Coordinate Spaces")]
        [Tooltip("Default interpretation of Vector3/Transform start values when using Play/Execute methods.")]
        public AnimationFlyCoordinateSpace defaultStartSpace = AnimationFlyCoordinateSpace.Auto;

        [Tooltip("Default interpretation of Vector3/Transform end values when using Play/Execute methods.")]
        public AnimationFlyCoordinateSpace defaultEndSpace = AnimationFlyCoordinateSpace.Auto;

        [Tooltip("Where spawned fly objects should be animated. Canvas is recommended for UI coin/currency effects.")]
        public AnimationFlySpawnSpace spawnSpace = AnimationFlySpawnSpace.Auto;

        [Tooltip("Camera used for world/screen/canvas conversion. Falls back to Canvas.worldCamera, then Camera.main.")]
        public Camera animationCamera;

        [Tooltip("If true, UI fly objects use RectTransform.anchoredPosition instead of world DOMove.")]
        public bool useAnchoredPositionForUI = true;

        [Header("Spawn")] [Tooltip("Parent object for bonus spawn")]
        public Transform spawnParent;

        [Tooltip("Spawned objects are moved to the end of their parent hierarchy.")]
        public bool setAsLastSibling = true;

        [Tooltip("Destroy spawned objects after the fly reaches the target.")]
        public bool destroyOnComplete = true;

        [Header("Motion")]
        [Tooltip("Random offset applied to every start position.")]
        public Vector3 startRandomOffset;

        [Tooltip("Random offset applied to every end position.")]
        public Vector3 endRandomOffset;

        [Tooltip("Random offset applied to the middle arc point.")]
        public Vector3 middleRandomOffset;

        [Tooltip("If true, objects rotate around Z while flying.")]
        public bool rotateDuringFlight;

        public float rotationDegrees = 360f;

        public bool useUnscaledTime;
        private readonly Dictionary<int, BonusPrefabData> _prefabDict = new();

        protected override void Init()
        {
            base.Init();
            FillDictionary();
        }

        private void OnValidate()
        {
            FillDictionary();
        }

        private void FillDictionary()
        {
            _prefabDict.Clear();
            if (bonusPrefabList == null)
            {
                return;
            }

            foreach (BonusPrefabData data in bonusPrefabList)
            {
                if (!_prefabDict.ContainsKey(data.bonusType))
                {
                    _prefabDict.Add(data.bonusType, data);
                }
            }
        }

        public void RefreshPrefabCache()
        {
            FillDictionary();
        }

        public void Execute(int type, int bonusCount, Vector3 start, Action<GameObject> onStart = null,
            Action<GameObject> onEnd = null)
        {
            if (!_prefabDict.TryGetValue(type, out BonusPrefabData data))
            {
                Debug.LogError($"[AnimationBonus] No prefab for bonus type {type}");
                return;
            }

            Play(data.prefab, bonusCount, start, data.endPos, defaultStartSpace,
                data.EffectiveEndSpace(defaultEndSpace), spawnParent, onStart, onEnd);
        }

        public void Execute(int type, int bonusCount, Transform start, Action<GameObject> onStart = null,
            Action<GameObject> onEnd = null)
        {
            if (!_prefabDict.TryGetValue(type, out BonusPrefabData data))
            {
                Debug.LogError($"[AnimationBonus] No prefab for bonus type {type}");
                return;
            }

            Play(data.prefab, bonusCount, start, data.endPos, defaultStartSpace,
                data.EffectiveEndSpace(defaultEndSpace), spawnParent, onStart, onEnd);
        }

        public void Execute(int type, int bonusCount, Transform start, Transform end,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            if (!_prefabDict.TryGetValue(type, out BonusPrefabData data))
            {
                Debug.LogError($"[AnimationBonus] No prefab for bonus type {type}");
                return;
            }

            parent = parent ?? spawnParent;
            Execute(data.prefab, bonusCount, start, end, parent, onStart, onEnd);
        }

        public void Execute(GameObject prefab, int bonusCount, Transform start, Transform end,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            parent = parent ?? spawnParent;
            Play(prefab, bonusCount, start, end, defaultStartSpace, defaultEndSpace, parent, onStart, onEnd);
        }

        public void Execute(GameObject prefab, int bonusCount, Vector3 start, Vector3 end,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            parent = parent ?? spawnParent;
            Play(prefab, bonusCount, start, end, defaultStartSpace, defaultEndSpace, parent, onStart, onEnd);
        }

        public void PlayByType(int type, int bonusCount, Transform start, Transform end)
        {
            Execute(type, bonusCount, start, end);
        }

        public void PlayByTypeWorldToCanvas(int type, int bonusCount, Transform worldStart, RectTransform canvasEnd)
        {
            PlayByType(type, bonusCount, worldStart, canvasEnd, AnimationFlyCoordinateSpace.World,
                AnimationFlyCoordinateSpace.Canvas);
        }

        public void PlayByTypeCanvasToCanvas(int type, int bonusCount, RectTransform canvasStart, RectTransform canvasEnd)
        {
            PlayByType(type, bonusCount, canvasStart, canvasEnd, AnimationFlyCoordinateSpace.Canvas,
                AnimationFlyCoordinateSpace.Canvas);
        }

        public void PlayByTypeCanvasToWorld(int type, int bonusCount, RectTransform canvasStart, Transform worldEnd)
        {
            PlayByType(type, bonusCount, canvasStart, worldEnd, AnimationFlyCoordinateSpace.Canvas,
                AnimationFlyCoordinateSpace.World);
        }

        public void PlayByTypeWorldToWorld(int type, int bonusCount, Transform worldStart, Transform worldEnd)
        {
            PlayByType(type, bonusCount, worldStart, worldEnd, AnimationFlyCoordinateSpace.World,
                AnimationFlyCoordinateSpace.World);
        }

        public void PlayByType(int type, int bonusCount, Transform start, Transform end,
            AnimationFlyCoordinateSpace startSpace, AnimationFlyCoordinateSpace endSpace,
            Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            if (!_prefabDict.TryGetValue(type, out BonusPrefabData data))
            {
                Debug.LogError($"[AnimationFly] No prefab for bonus type {type}");
                return;
            }

            Play(data.prefab, bonusCount, start, end, startSpace, endSpace, spawnParent, onStart, onEnd);
        }

        public void Play(GameObject prefab, int bonusCount, Transform start, Transform end,
            AnimationFlyCoordinateSpace startSpace, AnimationFlyCoordinateSpace endSpace,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            if (start == null || end == null)
            {
                Debug.LogWarning("[AnimationFly] Start or end transform is missing.", this);
                return;
            }

            Play(prefab, bonusCount, start.position, end.position, startSpace, endSpace, parent, onStart, onEnd,
                start, end);
        }

        public void Play(GameObject prefab, int bonusCount, Vector3 start, Transform end,
            AnimationFlyCoordinateSpace startSpace, AnimationFlyCoordinateSpace endSpace,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            if (end == null)
            {
                Debug.LogWarning("[AnimationFly] End transform is missing.", this);
                return;
            }

            Play(prefab, bonusCount, start, end.position, startSpace, endSpace, parent, onStart, onEnd, null, end);
        }

        public void Play(GameObject prefab, int bonusCount, Vector3 start, Vector3 end,
            AnimationFlyCoordinateSpace startSpace, AnimationFlyCoordinateSpace endSpace,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            Play(prefab, bonusCount, start, end, startSpace, endSpace, parent, onStart, onEnd, null, null);
        }

        private void Play(GameObject prefab, int bonusCount, Vector3 start, Vector3 end,
            AnimationFlyCoordinateSpace startSpace, AnimationFlyCoordinateSpace endSpace,
            Transform parent, Action<GameObject> onStart, Action<GameObject> onEnd,
            Transform startTransform, Transform endTransform)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[AnimationFly] Prefab is missing.", this);
                return;
            }

            parent = parent ?? spawnParent;
            int finalBonusCount = Mathf.CeilToInt(bonusCount * countMultiplier);
            finalBonusCount = Mathf.Clamp(finalBonusCount, 0, maxBonusCount);
            if (finalBonusCount <= 0)
            {
                return;
            }

            AnimationFlySpawnSpace resolvedSpawnSpace = ResolveSpawnSpace(parent, prefab);
            Vector3 resolvedStart = ResolvePosition(start, startSpace, resolvedSpawnSpace, parent, startTransform);
            Vector3 resolvedEnd = ResolvePosition(end, endSpace, resolvedSpawnSpace, parent, endTransform);

            StartCoroutine(AnimationRoutine(prefab, finalBonusCount, resolvedStart, resolvedEnd, parent,
                resolvedSpawnSpace, onStart, onEnd));
        }

        public IEnumerator AnimationRoutine(GameObject prefab, int bonusCount, Vector3 start, Vector3 end,
            Transform parent, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            AnimationFlySpawnSpace resolvedSpawnSpace = ResolveSpawnSpace(parent, prefab);
            yield return AnimationRoutine(prefab, bonusCount, start, end, parent, resolvedSpawnSpace, onStart, onEnd);
        }

        private IEnumerator AnimationRoutine(GameObject prefab, int bonusCount, Vector3 start, Vector3 end,
            Transform parent, AnimationFlySpawnSpace resolvedSpawnSpace, Action<GameObject> onStart = null,
            Action<GameObject> onEnd = null)
        {
            if (ignoreZ)
            {
                start.z = 0f;
                end.z = 0f;
            }

            for (int i = 0; i < bonusCount; i++)
            {
                Vector3 startPos = start + RandomOffset(startRandomOffset);
                Vector3 endPos = end + RandomOffset(endRandomOffset);

                GameObject bonus = Instantiate(prefab, parent);
                SetInitialPosition(bonus.transform, startPos, resolvedSpawnSpace);
                if (setAsLastSibling)
                {
                    bonus.transform.SetAsLastSibling();
                }

                Vector3 scale;
                scale = bonus.transform.localScale;
                scale *= scaleMult;
                bonus.transform.localScale = scale;

                onStart?.Invoke(bonus);

                var midPoint = Vector3.Lerp(startPos, endPos, middlePoint);
                midPoint += new Vector3(
                    Random.Range(-arcStrength, arcStrength),
                    Random.Range(arcStrength * multY, arcStrength),
                    Random.Range(-arcStrength, arcStrength)
                );
                midPoint += RandomOffset(middleRandomOffset);

                Tween moveTween = CreateMoveTween(bonus.transform, midPoint, flyDuration / 2f, resolvedSpawnSpace);
                moveTween.SetEase(easyStart).SetUpdate(useUnscaledTime)
                    .OnComplete(() =>
                    {
                        Tween endTween = CreateMoveTween(bonus.transform, endPos, flyDuration / 2f, resolvedSpawnSpace);
                        endTween.SetEase(easyEnd).SetUpdate(useUnscaledTime)
                            .OnComplete(() =>
                            {
                                onEnd?.Invoke(bonus);
                                if (destroyOnComplete)
                                {
                                    Destroy(bonus);
                                }
                            });
                    });

                if (rotateDuringFlight)
                {
                    bonus.transform.DORotate(new Vector3(0f, 0f, rotationDegrees), flyDuration,
                            RotateMode.FastBeyond360)
                        .SetRelative()
                        .SetEase(Ease.Linear)
                        .SetUpdate(useUnscaledTime);
                }

                if (useUnscaledTime)
                {
                    yield return new WaitForSecondsRealtime(delayBetweenBonuses);
                }
                else
                {
                    yield return new WaitForSeconds(delayBetweenBonuses);
                }
            }
        }

        private Tween CreateMoveTween(Transform target, Vector3 position, float duration,
            AnimationFlySpawnSpace resolvedSpawnSpace)
        {
            if (resolvedSpawnSpace == AnimationFlySpawnSpace.Canvas && useAnchoredPositionForUI &&
                target is RectTransform rect)
            {
                return DOTween.To(
                    () => rect.anchoredPosition,
                    value => rect.anchoredPosition = value,
                    new Vector2(position.x, position.y),
                    duration);
            }

            return target.DOMove(position, duration);
        }

        private void SetInitialPosition(Transform target, Vector3 position, AnimationFlySpawnSpace resolvedSpawnSpace)
        {
            if (resolvedSpawnSpace == AnimationFlySpawnSpace.Canvas && useAnchoredPositionForUI &&
                target is RectTransform rect)
            {
                rect.anchoredPosition3D = position;
                return;
            }

            target.position = position;
        }

        private Vector3 ResolvePosition(Vector3 position, AnimationFlyCoordinateSpace sourceSpace,
            AnimationFlySpawnSpace targetSpace, Transform parent, Transform sourceTransform = null)
        {
            sourceSpace = ResolveCoordinateSpace(sourceSpace, sourceTransform);

            if (targetSpace == AnimationFlySpawnSpace.Canvas)
            {
                Canvas canvas = ResolveCanvas(parent);
                if (canvas == null)
                {
                    return position;
                }

                return sourceSpace switch
                {
                    AnimationFlyCoordinateSpace.World => WorldToCanvasLocalPosition(position, canvas, ResolveCamera(canvas)),
                    AnimationFlyCoordinateSpace.Screen => ScreenToCanvasLocalPosition(position, canvas, ResolveCamera(canvas)),
                    AnimationFlyCoordinateSpace.Canvas => CanvasLocalPosition(position, canvas, sourceTransform),
                    _ => position
                };
            }

            return sourceSpace switch
            {
                AnimationFlyCoordinateSpace.Canvas => CanvasToWorldPosition(position, sourceTransform, ResolveCanvas(parent)),
                AnimationFlyCoordinateSpace.Screen => ScreenToWorldPosition(position, ResolveCamera(null)),
                _ => position
            };
        }

        private AnimationFlyCoordinateSpace ResolveCoordinateSpace(AnimationFlyCoordinateSpace configured,
            Transform source)
        {
            if (configured != AnimationFlyCoordinateSpace.Auto)
            {
                return configured;
            }

            return source is RectTransform || source != null && source.GetComponentInParent<Canvas>() != null
                ? AnimationFlyCoordinateSpace.Canvas
                : AnimationFlyCoordinateSpace.World;
        }

        private AnimationFlySpawnSpace ResolveSpawnSpace(Transform parent, GameObject prefab)
        {
            if (spawnSpace != AnimationFlySpawnSpace.Auto)
            {
                return spawnSpace;
            }

            if (parent != null && parent.GetComponentInParent<Canvas>() != null)
            {
                return AnimationFlySpawnSpace.Canvas;
            }

            return prefab != null && prefab.GetComponent<RectTransform>() != null
                ? AnimationFlySpawnSpace.Canvas
                : AnimationFlySpawnSpace.World;
        }

        private Canvas ResolveCanvas(Transform parent)
        {
            if (parentCanvas != null)
            {
                return parentCanvas;
            }

            if (parent != null)
            {
                Canvas canvas = parent.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    return canvas;
                }
            }

            return null;
        }

        private Camera ResolveCamera(Canvas canvas)
        {
            if (animationCamera != null)
            {
                return animationCamera;
            }

            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            {
                return canvas.worldCamera;
            }

            return Camera.main;
        }

        private static Vector3 RandomOffset(Vector3 maxAbs)
        {
            return new Vector3(
                Random.Range(-Mathf.Abs(maxAbs.x), Mathf.Abs(maxAbs.x)),
                Random.Range(-Mathf.Abs(maxAbs.y), Mathf.Abs(maxAbs.y)),
                Random.Range(-Mathf.Abs(maxAbs.z), Mathf.Abs(maxAbs.z)));
        }

        private Vector3 CanvasLocalPosition(Vector3 position, Canvas canvas, Transform sourceTransform)
        {
            if (!(sourceTransform is RectTransform rect) || canvas == null)
            {
                return position;
            }

            Canvas sourceCanvas = rect.GetComponentInParent<Canvas>();
            if (sourceCanvas == canvas)
            {
                return rect.anchoredPosition3D;
            }

            Camera sourceCamera = ResolveCamera(sourceCanvas);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                sourceCanvas != null && sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sourceCamera,
                rect.position);
            return ScreenToCanvasLocalPosition(screenPoint, canvas, ResolveCamera(canvas));
        }

        private Vector3 CanvasToWorldPosition(Vector3 position, Transform sourceTransform, Canvas fallbackCanvas)
        {
            if (sourceTransform is RectTransform rect)
            {
                Canvas sourceCanvas = rect.GetComponentInParent<Canvas>() ?? fallbackCanvas;
                Camera camera = ResolveCamera(sourceCanvas);
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                    sourceCanvas != null && sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera,
                    rect.position);
                return ScreenToWorldPosition(screenPoint, camera);
            }

            return CanvasToWorldPosition(position, fallbackCanvas, ResolveCamera(fallbackCanvas));
        }

        private static Vector3 WorldToCanvasLocalPosition(Vector3 worldPosition, Canvas canvas, Camera camera)
        {
            if (canvas == null)
            {
                return worldPosition;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            return ScreenToCanvasLocalPosition(screenPoint, canvas, camera);
        }

        private static Vector3 ScreenToCanvasLocalPosition(Vector3 screenPosition, Canvas canvas, Camera camera)
        {
            if (canvas == null)
            {
                return screenPosition;
            }

            RectTransform canvasRect = canvas.transform as RectTransform;
            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera,
                out Vector2 localPoint);
            return localPoint;
        }

        private static Vector3 ScreenToWorldPosition(Vector3 screenPosition, Camera camera)
        {
            if (camera == null)
            {
                return screenPosition;
            }

            float z = screenPosition.z;
            if (Mathf.Approximately(z, 0f))
            {
                z = Mathf.Abs(camera.transform.position.z);
            }

            return camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, z));
        }

        public GameObject GetPrefab(int type)
        {
            if (_prefabDict.TryGetValue(type, out BonusPrefabData data))
            {
                return data.prefab;
            }

            Debug.LogWarning($"[AnimationBonus] No prefab for bonus type {type}");
            return null;
        }

        public Transform GetPos(int type)
        {
            if (_prefabDict.TryGetValue(type, out BonusPrefabData data))
            {
                return data.endPos;
            }

            Debug.LogWarning($"[AnimationBonus] No spawn point for bonus type {type}");
            return null;
        }

        public static Vector3 CanvasToWorldPosition(Vector2 uiPosition, Canvas canvas = null, Camera camera = null)
        {
            canvas = canvas ?? I.parentCanvas;
            if (canvas == null)
            {
                Debug.LogError(
                    "[AnimationFly] Canvas is not set and parentCanvas is not assigned!");
                return Vector3.zero;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                camera = null;
            }
            else if (camera == null)
            {
                camera = Camera.main;
            }

            Vector3 worldPos = Vector3.zero;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform,
                uiPosition,
                camera,
                out worldPos);
            return worldPos;
        }

        /// <summary>
        ///     Converts a world position to a position on the Canvas (Screen Space Overlay/Camera).
        /// </summary>
        /// <param name="worldPosition">World position</param>
        /// <param name="canvas">Canvas containing the UI element (if null, uses parentCanvas)</param>
        /// <param name="camera">Camera used by the Canvas (null for Overlay)</param>
        /// <returns>Position in Canvas space (screen point)</returns>
        public static Vector2 WorldToCanvasPosition(Vector3 worldPosition, Canvas canvas = null, Camera camera = null)
        {
            canvas = canvas ?? I.parentCanvas;
            if (canvas == null)
            {
                Debug.LogError(
                    "[AnimationFly] Canvas is not set and parentCanvas is not assigned!");
                return Vector2.zero;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                camera = null;
            }
            else if (camera == null)
            {
                camera = Camera.main;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            return screenPoint;
        }

        [Serializable]
        [Tooltip("Bonus prefabs by type and end point")]
        public struct BonusPrefabData
        {
            public int bonusType;
            public GameObject prefab;
            public Transform endPos;

            [FormerlySerializedAs("isWorld")] [FormerlySerializedAs("isCanvas")]
            public bool isWorldSpace;

            public AnimationFlyCoordinateSpace endSpace;

            public AnimationFlyCoordinateSpace EffectiveEndSpace(AnimationFlyCoordinateSpace fallback)
            {
                if (endSpace != AnimationFlyCoordinateSpace.Auto)
                {
                    return endSpace;
                }

                return isWorldSpace ? AnimationFlyCoordinateSpace.World : fallback;
            }
        }
    }
}
