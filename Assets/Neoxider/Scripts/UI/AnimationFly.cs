using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Neo.UI
{
    [MovedFrom(true, sourceNamespace: "Neo")]
    public enum AnimationFlyCoordinateSpace
    {
        Auto = 0,
        World = 1,
        Canvas = 2,
        Screen = 3
    }

    [MovedFrom(true, sourceNamespace: "Neo")]
    public enum AnimationFlySpawnSpace
    {
        Auto = 0,
        World = 1,
        Canvas = 2
    }

    [MovedFrom(true, sourceNamespace: "Neo")]
    public enum AnimationFlyCompletionMode
    {
        Destroy = 0,
        DisableAndPool = 1,
        KeepAlive = 2
    }

    [MovedFrom(true, sourceNamespace: "Neo")]
    public enum AnimationFlyRewardTiming
    {
        Manual = 0,
        OnEachArrived = 1,
        OnAllArrived = 2
    }

    [MovedFrom(true, sourceNamespace: "Neo")]
    public enum AnimationFlyMotionPreset
    {
        Arc = 0,
        Fountain = 1,
        Magnet = 2,
        FountainMagnet = 3,
        Scatter = 4
    }

    [CreateFromMenu("Neoxider/UI/AnimationFly")]
    [AddComponentMenu("Neoxider/UI/" + nameof(AnimationFly))]
    [NeoDoc("UI/AnimationFly.md")]
    [MovedFrom(true, sourceNamespace: "Neo")]
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

        [Header("Animation")] [Min(0.001f)] public float flyDuration = 1.0f;
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

        [Tooltip(
            "Camera used for world/screen/canvas conversion. Falls back to Canvas.worldCamera, then a cached Camera.main lookup.")]
        public Camera animationCamera;

        [Tooltip("If true, UI fly objects use RectTransform.anchoredPosition instead of world DOMove.")]
        public bool useAnchoredPositionForUI = true;

        [Tooltip("Depth used when converting screen/canvas points to world positions and no target depth is known.")]
        [Min(0.01f)]
        public float defaultScreenToWorldDepth = 10f;

        [Header("Spawn")] [Tooltip("Parent object for bonus spawn")]
        public Transform spawnParent;

        [Tooltip("Spawned objects are moved to the end of their parent hierarchy.")]
        public bool setAsLastSibling = true;

        [Tooltip("Destroy spawned objects after the fly reaches the target.")]
        public bool destroyOnComplete = true;

        [Tooltip("Default completion behavior for the typed request API.")]
        public AnimationFlyCompletionMode defaultCompletionMode = AnimationFlyCompletionMode.Destroy;

        [Tooltip("Maximum pooled objects retained per prefab/sprite key when using DisableAndPool.")] [Min(0)]
        public int maxPoolPerKey = 64;

        [Header("Motion")] [Tooltip("Random offset applied to every start position.")]
        public Vector3 startRandomOffset;

        [Tooltip("Random offset applied to every end position.")]
        public Vector3 endRandomOffset;

        [Tooltip("Random offset applied to the middle arc point.")]
        public Vector3 middleRandomOffset;

        [Tooltip("If true, objects rotate around Z while flying.")]
        public bool rotateDuringFlight;

        public float rotationDegrees = 360f;

        [Header("Advanced Motion")] [Tooltip("Default trajectory preset for typed AnimationFlyRequest calls.")]
        public AnimationFlyMotionPreset motionPreset = AnimationFlyMotionPreset.Arc;

        [Tooltip("Initial pop offset used by Fountain and FountainMagnet presets, in resolved spawn-space units.")]
        public Vector3 burstOffset = new(0f, 180f, 0f);

        [Tooltip("Random spread around the initial pop point used by Fountain, FountainMagnet and Scatter presets.")]
        public Vector3 burstRandomOffset = new(120f, 60f, 0f);

        [Range(0.05f, 0.85f)] [Tooltip("Part of flyDuration reserved for the initial pop stage.")]
        public float burstDurationRatio = 0.28f;

        [Min(0f)] [Tooltip("Optional pause after the initial pop before items fly to the target.")]
        public float burstHoldDuration = 0.04f;

        [Min(0f)] [Tooltip("Distance from the target where Magnet presets switch to the final pull.")]
        public float magnetDistance = 90f;

        [Range(0.05f, 0.85f)] [Tooltip("Part of flyDuration reserved for the final magnet pull.")]
        public float magnetDurationRatio = 0.25f;

        public bool useUnscaledTime;
        private static readonly Vector3[] CornerBuffer = new Vector3[4];
        private static readonly Action<GameObject> KillTweensBeforeDestroy = KillVisualTweens;
        private readonly Dictionary<int, BonusPrefabData> _prefabDict = new();
        private readonly AnimationFlyVisualPool _visuals = new();
        private Camera _cachedFallbackCamera;

        /// <summary>Number of spawned visuals this fly still owns (in flight or waiting in the pool).</summary>
        public int OwnedVisualCount => _visuals.OwnedCount;

        protected override void Init()
        {
            base.Init();
            FillDictionary();
        }

        protected override void OnDestroy()
        {
            StopAllCoroutines();
            _visuals.DestroyAll(KillTweensBeforeDestroy);
            base.OnDestroy();
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

        [Button]
        public void RefreshPrefabCache()
        {
            FillDictionary();
        }

        // WHY: Init (which fills the cache) runs only for the instance that Set Instance On Awake makes the
        // singleton, and OnValidate only in the Editor. An additional AnimationFly in a player build therefore
        // kept an empty cache and rejected every bonus type; a miss rescans the authored list before failing.
        private bool TryGetPrefabData(int type, out BonusPrefabData data)
        {
            if (_prefabDict.TryGetValue(type, out data))
            {
                return true;
            }

            if (bonusPrefabList != null)
            {
                for (int i = 0; i < bonusPrefabList.Count; i++)
                {
                    if (bonusPrefabList[i].bonusType == type)
                    {
                        data = bonusPrefabList[i];
                        _prefabDict[type] = data;
                        return true;
                    }
                }
            }

            data = default;
            return false;
        }

        /// <summary>
        ///     Inspector smoke-test: flies <paramref name="bonusCount" /> items of <paramref name="type" />
        ///     from the spawn parent (or this transform) to the end point configured in <see cref="bonusPrefabList" />.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void TestFlyByType(int type, int bonusCount = 5)
        {
            Transform start = spawnParent != null ? spawnParent : transform;
            Execute(type, bonusCount, start);
        }

        /// <summary>
        ///     Starts a typed flight. The request is copied on entry, so the same instance can be reused or
        ///     changed for the next call without affecting flights that are already running.
        /// </summary>
        /// <returns>Live progress of the flight, or <see cref="AnimationFlyResult.Empty" /> when nothing spawns.</returns>
        public AnimationFlyResult Play(AnimationFlyRequest request)
        {
            if (request == null)
            {
                NeoDiagnostics.LogWarning("[AnimationFly] Request is missing.", this);
                return AnimationFlyResult.Empty;
            }

            // WHY: the coroutine and tween callbacks read the request long after Play returns, and type
            // resolution fills in the end transform. Working on a private copy keeps a reused or edited
            // request from retargeting or re-wiring flights that are already in the air.
            request = request.Clone();
            _visuals.PruneDestroyed();
            if (!TryResolveVisual(request, out GameObject prefab, out Sprite sprite, out UnityEngine.Object poolKey))
            {
                return AnimationFlyResult.Empty;
            }

            Transform parent = ResolveParent(request.Parent);
            AnimationFlySpawnSpace resolvedSpawnSpace = ResolveSpawnSpace(parent, prefab, sprite, request.SpawnSpace);
            AnimationFlyCoordinateSpace startSpace = ResolveCoordinateSpace(request.StartSpace, request.StartTransform);
            AnimationFlyCoordinateSpace endSpace = ResolveCoordinateSpace(request.EndSpace, request.EndTransform);

            Vector3 rawStart = request.StartTransform != null ? request.StartTransform.position : request.StartPosition;
            Vector3 rawEnd = request.EndTransform != null ? request.EndTransform.position : request.EndPosition;

            float? startDepth = null;
            float? endDepth = null;
            Camera camera = ResolveCamera(ResolveCanvas(parent));
            if (camera != null)
            {
                if (endSpace == AnimationFlyCoordinateSpace.World)
                {
                    startDepth = Mathf.Max(0.01f, camera.WorldToScreenPoint(rawEnd).z);
                }

                if (startSpace == AnimationFlyCoordinateSpace.World)
                {
                    endDepth = Mathf.Max(0.01f, camera.WorldToScreenPoint(rawStart).z);
                }
            }

            Vector3 resolvedStart = ResolvePosition(rawStart, startSpace, resolvedSpawnSpace, parent,
                request.StartTransform, startDepth);
            Vector3 resolvedEnd = ResolvePosition(rawEnd, endSpace, resolvedSpawnSpace, parent,
                request.EndTransform, endDepth);

            int finalCount = AnimationFlyMath.ResolveCount(request.Count, request.CountMultiplier, countMultiplier,
                request.MaxCount, maxBonusCount);
            if (finalCount <= 0)
            {
                return AnimationFlyResult.Empty;
            }

            AnimationFlyResult result = new AnimationFlyResult(finalCount);
            StartCoroutine(AnimationRoutine(request, prefab, sprite, poolKey, finalCount, resolvedStart, resolvedEnd,
                parent, resolvedSpawnSpace, result));
            return result;
        }

        public AnimationFlyResult PlaySprite(Sprite sprite, int count, Transform start, Transform end,
            Transform parent = null, Action onReward = null)
        {
            return Play(new AnimationFlyRequest
            {
                Sprite = sprite,
                Count = count,
                StartTransform = start,
                EndTransform = end,
                Parent = parent,
                OnReward = onReward
            });
        }

        public AnimationFlyResult PlaySpriteWorldToCanvas(Sprite sprite, int count, Transform worldStart,
            RectTransform canvasEnd, Transform parent = null, Action onReward = null)
        {
            return Play(new AnimationFlyRequest
            {
                Sprite = sprite,
                Count = count,
                StartTransform = worldStart,
                EndTransform = canvasEnd,
                StartSpace = AnimationFlyCoordinateSpace.World,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                Parent = parent,
                OnReward = onReward
            });
        }

        public void Execute(int type, int bonusCount, Vector3 start, Action<GameObject> onStart = null,
            Action<GameObject> onEnd = null)
        {
            if (!TryGetPrefabData(type, out BonusPrefabData data))
            {
                NeoDiagnostics.LogError($"[AnimationFly] No prefab for bonus type {type}", this);
                return;
            }

            Play(data.prefab, bonusCount, start, data.endPos, defaultStartSpace,
                data.EffectiveEndSpace(defaultEndSpace), spawnParent, onStart, onEnd);
        }

        public void Execute(int type, int bonusCount, Transform start, Action<GameObject> onStart = null,
            Action<GameObject> onEnd = null)
        {
            if (!TryGetPrefabData(type, out BonusPrefabData data))
            {
                NeoDiagnostics.LogError($"[AnimationFly] No prefab for bonus type {type}", this);
                return;
            }

            Play(data.prefab, bonusCount, start, data.endPos, defaultStartSpace,
                data.EffectiveEndSpace(defaultEndSpace), spawnParent, onStart, onEnd);
        }

        public void Execute(int type, int bonusCount, Transform start, Transform end,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            if (!TryGetPrefabData(type, out BonusPrefabData data))
            {
                NeoDiagnostics.LogError($"[AnimationFly] No prefab for bonus type {type}", this);
                return;
            }

            parent = ResolveParent(parent);
            Execute(data.prefab, bonusCount, start, end, parent, onStart, onEnd);
        }

        public void Execute(GameObject prefab, int bonusCount, Transform start, Transform end,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            parent = ResolveParent(parent);
            Play(prefab, bonusCount, start, end, defaultStartSpace, defaultEndSpace, parent, onStart, onEnd);
        }

        public void Execute(GameObject prefab, int bonusCount, Vector3 start, Vector3 end,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            parent = ResolveParent(parent);
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

        public void PlayByTypeCanvasToCanvas(int type, int bonusCount, RectTransform canvasStart,
            RectTransform canvasEnd)
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
            if (!TryGetPrefabData(type, out BonusPrefabData data))
            {
                NeoDiagnostics.LogError($"[AnimationFly] No prefab for bonus type {type}");
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
                NeoDiagnostics.LogWarning("[AnimationFly] Start or end transform is missing.", this);
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
                NeoDiagnostics.LogWarning("[AnimationFly] End transform is missing.", this);
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
                NeoDiagnostics.LogWarning("[AnimationFly] Prefab is missing.", this);
                return;
            }

            parent = ResolveParent(parent);
            int finalBonusCount = AnimationFlyMath.ResolveCount(bonusCount, 1f, countMultiplier, 0, maxBonusCount);
            if (finalBonusCount <= 0)
            {
                return;
            }

            _visuals.PruneDestroyed();
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

            ItemDelayYield delayYield = default;
            for (int i = 0; i < bonusCount; i++)
            {
                Vector3 startPos = start + AnimationFlyMath.RandomOffset(startRandomOffset);
                Vector3 endPos = end + AnimationFlyMath.RandomOffset(endRandomOffset);

                GameObject bonus = Instantiate(prefab, parent);
                _visuals.Track(bonus);
                ResetVisualForFlight(bonus);
                SetInitialPosition(bonus.transform, startPos, resolvedSpawnSpace);
                if (setAsLastSibling)
                {
                    bonus.transform.SetAsLastSibling();
                }

                bonus.transform.localScale *= scaleMult;

                InvokeSafely(onStart, bonus);

                Vector3 midPoint = BuildArcPoint(startPos, endPos, middleRandomOffset);
                Tween moveTween = CreateMoveTween(bonus.transform, midPoint, flyDuration / 2f, resolvedSpawnSpace);
                moveTween.SetEase(easyStart).SetUpdate(useUnscaledTime)
                    .OnComplete(() =>
                    {
                        Tween endTween = CreateMoveTween(bonus.transform, endPos, flyDuration / 2f, resolvedSpawnSpace);
                        endTween.SetEase(easyEnd).SetUpdate(useUnscaledTime)
                            .OnComplete(() =>
                            {
                                // WHY: a throwing onEnd used to skip the release below and leak the visual.
                                InvokeSafely(onEnd, bonus);
                                _visuals.Release(bonus);
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
                        .SetUpdate(useUnscaledTime)
                        .SetTarget(bonus.transform)
                        .SetLink(bonus);
                }

                yield return delayYield.Get(delayBetweenBonuses, useUnscaledTime);
            }
        }

        private IEnumerator AnimationRoutine(AnimationFlyRequest request, GameObject prefab, Sprite sprite,
            UnityEngine.Object poolKey, int bonusCount, Vector3 start, Vector3 end, Transform parent,
            AnimationFlySpawnSpace resolvedSpawnSpace, AnimationFlyResult result)
        {
            if (ignoreZ)
            {
                start.z = 0f;
                end.z = 0f;
            }

            AnimationFlyCompletionMode completionMode = request.CompletionMode ?? defaultCompletionMode;
            Vector2? resolvedUiSize = ResolveUiSize(request, parent, resolvedSpawnSpace);
            ItemDelayYield delayYield = default;
            for (int i = 0; i < bonusCount; i++)
            {
                Vector3 startPos = start + AnimationFlyMath.RandomOffset(request.StartRandomOffset ?? startRandomOffset);
                Vector3 endPos = end + AnimationFlyMath.RandomOffset(request.EndRandomOffset ?? endRandomOffset);

                GameObject bonus = SpawnVisual(prefab, sprite, poolKey, parent, resolvedSpawnSpace);
                AnimationFlyVisualState visualState = ResetVisualForFlight(bonus);
                SetInitialPosition(bonus.transform, startPos, resolvedSpawnSpace);
                if (setAsLastSibling)
                {
                    bonus.transform.SetAsLastSibling();
                }

                ApplyUiSize(bonus, resolvedUiSize, resolvedSpawnSpace);
                float startScaleMultiplier = request.ScaleMultiplier ?? scaleMult;
                bonus.transform.localScale = visualState.BaseLocalScale * startScaleMultiplier;
                result.RegisterStarted(bonus);
                // WHY: an exception here used to end the coroutine with the visual spawned but never
                // tweened, so it stayed on screen and the result could never complete.
                InvokeSafely(request.OnItemStarted, bonus);

                Tween flightTween = CreateFlightTween(bonus.transform, startPos, endPos, request, resolvedSpawnSpace);
                float totalDuration = Mathf.Max(0.001f, flightTween.Duration(false));
                if (request.EndScaleMultiplier.HasValue)
                {
                    Sequence flightAndScale = DOTween.Sequence()
                        .SetUpdate(useUnscaledTime)
                        .SetTarget(bonus.transform)
                        .SetLink(bonus);
                    flightAndScale.Join(flightTween);
                    flightAndScale.Join(bonus.transform
                        .DOScale(visualState.BaseLocalScale * request.EndScaleMultiplier.Value, totalDuration)
                        .SetEase(request.ScaleEase ?? Ease.InQuad));
                    flightTween = flightAndScale;
                }

                flightTween.OnComplete(() =>
                    HandleItemArrived(request, result, bonus, poolKey, resolvedSpawnSpace, completionMode));

                if (rotateDuringFlight)
                {
                    bonus.transform.DORotate(new Vector3(0f, 0f, rotationDegrees), totalDuration,
                            RotateMode.FastBeyond360)
                        .SetRelative()
                        .SetEase(Ease.Linear)
                        .SetUpdate(useUnscaledTime)
                        .SetTarget(bonus.transform)
                        .SetLink(bonus);
                }

                yield return delayYield.Get(ResolveItemDelay(request), useUnscaledTime);
            }
        }

        // WHY: user callbacks run inside DOTween completion. Every callback is isolated so a throwing one
        // can neither skip the completion bookkeeping (leaking the visual and stalling the result) nor
        // propagate into DOTween's update loop when safe mode is off.
        private void HandleItemArrived(AnimationFlyRequest request, AnimationFlyResult result, GameObject bonus,
            UnityEngine.Object poolKey, AnimationFlySpawnSpace resolvedSpawnSpace,
            AnimationFlyCompletionMode completionMode)
        {
            InvokeSafely(request.OnItemArrived, bonus);
            if (request.RewardTiming == AnimationFlyRewardTiming.OnEachArrived)
            {
                InvokeSafely(request.OnReward);
            }

            result.RegisterCompleted(bonus);
            CompleteVisual(bonus, poolKey, resolvedSpawnSpace, completionMode);
            if (!result.IsCompleted)
            {
                return;
            }

            if (request.RewardTiming == AnimationFlyRewardTiming.OnAllArrived)
            {
                InvokeSafely(request.OnReward);
            }

            InvokeSafely(request.OnAllArrived);
        }

        private void InvokeSafely(Action callback)
        {
            if (callback == null)
            {
                return;
            }

            try
            {
                callback();
            }
            catch (Exception exception)
            {
                NeoDiagnostics.LogException(exception, this);
            }
        }

        private void InvokeSafely(Action<GameObject> callback, GameObject item)
        {
            if (callback == null)
            {
                return;
            }

            try
            {
                callback(item);
            }
            catch (Exception exception)
            {
                NeoDiagnostics.LogException(exception, this);
            }
        }

        private Vector3 BuildArcPoint(Vector3 startPos, Vector3 endPos, Vector3 randomOffset)
        {
            Vector3 arcPoint = Vector3.Lerp(startPos, endPos, middlePoint);
            arcPoint += new Vector3(
                Random.Range(-arcStrength, arcStrength),
                Random.Range(arcStrength * multY, arcStrength),
                Random.Range(-arcStrength, arcStrength));
            arcPoint += AnimationFlyMath.RandomOffset(randomOffset);
            return arcPoint;
        }

        private Tween CreateFlightTween(Transform target, Vector3 startPos, Vector3 endPos,
            AnimationFlyRequest request, AnimationFlySpawnSpace resolvedSpawnSpace)
        {
            float duration = ResolveDuration(request);
            AnimationFlyMotionPreset preset = request.MotionPreset ?? motionPreset;
            switch (preset)
            {
                case AnimationFlyMotionPreset.Fountain:
                    return CreateFountainTween(target, startPos, endPos, request, resolvedSpawnSpace, duration, false);
                case AnimationFlyMotionPreset.Magnet:
                    return CreateMagnetTween(target, startPos, endPos, request, resolvedSpawnSpace, duration);
                case AnimationFlyMotionPreset.FountainMagnet:
                    return CreateFountainTween(target, startPos, endPos, request, resolvedSpawnSpace, duration, true);
                case AnimationFlyMotionPreset.Scatter:
                    return CreateScatterTween(target, startPos, endPos, request, resolvedSpawnSpace, duration);
                default:
                    return CreateArcTween(target, startPos, endPos, request, resolvedSpawnSpace, duration);
            }
        }

        private Tween CreateArcTween(Transform target, Vector3 startPos, Vector3 endPos,
            AnimationFlyRequest request, AnimationFlySpawnSpace resolvedSpawnSpace, float duration)
        {
            Vector3 arcPoint = BuildArcPoint(startPos, endPos, request.MiddleRandomOffset ?? middleRandomOffset);
            Sequence sequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetTarget(target)
                .SetLink(target.gameObject);
            sequence.Append(CreateMoveTween(target, arcPoint, duration / 2f, resolvedSpawnSpace)
                .SetEase(request.CruiseEase ?? easyStart));
            sequence.Append(CreateMoveTween(target, endPos, duration / 2f, resolvedSpawnSpace)
                .SetEase(request.MagnetEase ?? easyEnd));
            return sequence;
        }

        private Tween CreateFountainTween(Transform target, Vector3 startPos, Vector3 endPos,
            AnimationFlyRequest request, AnimationFlySpawnSpace resolvedSpawnSpace, float duration,
            bool useMagnetFinish)
        {
            Vector3 launchPoint = startPos + (request.BurstOffset ?? burstOffset) +
                                  AnimationFlyMath.RandomOffset(request.BurstRandomOffset ?? burstRandomOffset);
            if (ignoreZ)
            {
                launchPoint.z = 0f;
            }

            float popDuration =
                duration * AnimationFlyMath.ClampStageRatio(request.BurstDurationRatio ?? burstDurationRatio);
            float holdDuration = Mathf.Max(0f, request.BurstHoldDuration ?? burstHoldDuration) /
                                 AnimationFlyMath.ResolveSpeedMultiplier(request.SpeedMultiplier);
            float remaining = Mathf.Max(0.01f, duration - popDuration);

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetTarget(target)
                .SetLink(target.gameObject);
            sequence.Append(CreateMoveTween(target, launchPoint, popDuration, resolvedSpawnSpace)
                .SetEase(request.BurstEase ?? Ease.OutBack));
            if (holdDuration > 0f)
            {
                sequence.AppendInterval(holdDuration);
            }

            if (useMagnetFinish)
            {
                AppendMagnetStages(sequence, target, launchPoint, endPos, remaining, request, resolvedSpawnSpace);
            }
            else
            {
                sequence.Append(CreateMoveTween(target, endPos, remaining, resolvedSpawnSpace)
                    .SetEase(request.CruiseEase ?? easyEnd));
            }

            return sequence;
        }

        private Tween CreateMagnetTween(Transform target, Vector3 startPos, Vector3 endPos,
            AnimationFlyRequest request, AnimationFlySpawnSpace resolvedSpawnSpace, float duration)
        {
            Sequence sequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetTarget(target)
                .SetLink(target.gameObject);
            AppendMagnetStages(sequence, target, startPos, endPos, duration, request, resolvedSpawnSpace);
            return sequence;
        }

        private Tween CreateScatterTween(Transform target, Vector3 startPos, Vector3 endPos,
            AnimationFlyRequest request, AnimationFlySpawnSpace resolvedSpawnSpace, float duration)
        {
            Vector3 spread = request.BurstRandomOffset ?? burstRandomOffset;
            Vector3 launchPoint = startPos + new Vector3(
                Random.Range(-spread.x, spread.x),
                Random.Range(-spread.y * 0.25f, spread.y),
                Random.Range(-spread.z, spread.z));
            if (ignoreZ)
            {
                launchPoint.z = 0f;
            }

            float scatterDuration =
                duration * AnimationFlyMath.ClampStageRatio(request.BurstDurationRatio ?? burstDurationRatio);
            float remaining = Mathf.Max(0.01f, duration - scatterDuration);
            Sequence sequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetTarget(target)
                .SetLink(target.gameObject);
            sequence.Append(CreateMoveTween(target, launchPoint, scatterDuration, resolvedSpawnSpace)
                .SetEase(request.BurstEase ?? Ease.OutQuad));
            sequence.Append(CreateMoveTween(target, endPos, remaining, resolvedSpawnSpace)
                .SetEase(request.CruiseEase ?? Ease.InOutQuad));
            return sequence;
        }

        private void AppendMagnetStages(Sequence sequence, Transform target, Vector3 startPos, Vector3 endPos,
            float duration, AnimationFlyRequest request, AnimationFlySpawnSpace resolvedSpawnSpace)
        {
            float magnetRatio = AnimationFlyMath.ClampStageRatio(request.MagnetDurationRatio ?? magnetDurationRatio);
            float magnetDuration = Mathf.Max(0.01f, duration * magnetRatio);
            float approachDuration = Mathf.Max(0.01f, duration - magnetDuration);
            Vector3 approachPoint = AnimationFlyMath.MagnetApproachPoint(startPos, endPos,
                request.MagnetDistance ?? magnetDistance);

            sequence.Append(CreateMoveTween(target, approachPoint, approachDuration, resolvedSpawnSpace)
                .SetEase(request.CruiseEase ?? Ease.OutQuad));
            sequence.Append(CreateMoveTween(target, endPos, magnetDuration, resolvedSpawnSpace)
                .SetEase(request.MagnetEase ?? Ease.InQuad));
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
                        duration)
                    .SetTarget(rect)
                    .SetLink(rect.gameObject);
            }

            return target.DOMove(position, duration)
                .SetTarget(target)
                .SetLink(target.gameObject);
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

        private static AnimationFlyVisualState ResetVisualForFlight(GameObject instance)
        {
            if (instance == null)
            {
                return null;
            }

            KillVisualTweens(instance);
            AnimationFlyVisualState state = instance.GetComponent<AnimationFlyVisualState>();
            if (state == null)
            {
                state = instance.AddComponent<AnimationFlyVisualState>();
                state.BaseLocalScale = instance.transform.localScale;
                state.BaseLocalRotation = instance.transform.localRotation;
                if (instance.transform is RectTransform initialRect)
                {
                    state.HasBaseSizeDelta = true;
                    state.BaseSizeDelta = initialRect.sizeDelta;
                }
            }

            instance.transform.localScale = state.BaseLocalScale;
            instance.transform.localRotation = state.BaseLocalRotation;
            if (state.HasBaseSizeDelta && instance.transform is RectTransform rect)
            {
                rect.sizeDelta = state.BaseSizeDelta;
            }

            return state;
        }

        private Vector2? ResolveUiSize(AnimationFlyRequest request, Transform parent,
            AnimationFlySpawnSpace resolvedSpawnSpace)
        {
            if (resolvedSpawnSpace != AnimationFlySpawnSpace.Canvas)
            {
                return null;
            }

            Vector2? requestedSize = request.UiSize;
            if (!requestedSize.HasValue && request.UiSizeSource != null)
            {
                requestedSize = GetRectSizeInParent(request.UiSizeSource, parent);
            }

            if (!requestedSize.HasValue)
            {
                return null;
            }

            if (!AnimationFlyMath.IsFinite(requestedSize.Value.x) || !AnimationFlyMath.IsFinite(requestedSize.Value.y))
            {
                NeoDiagnostics.LogWarning("[AnimationFly] UiSize must contain finite values.", this);
                return null;
            }

            return new Vector2(Mathf.Max(0f, requestedSize.Value.x), Mathf.Max(0f, requestedSize.Value.y));
        }

        private static void ApplyUiSize(GameObject instance, Vector2? requestedSize,
            AnimationFlySpawnSpace resolvedSpawnSpace)
        {
            if (!requestedSize.HasValue || resolvedSpawnSpace != AnimationFlySpawnSpace.Canvas ||
                !(instance.transform is RectTransform rect))
            {
                return;
            }

            Vector2 size = requestedSize.Value;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        private static Vector2 GetRectSizeInParent(RectTransform source, Transform targetParent)
        {
            if (targetParent == null)
            {
                return source.rect.size;
            }

            Vector3[] corners = CornerBuffer;
            source.GetWorldCorners(corners);
            Vector3 first = targetParent.InverseTransformPoint(corners[0]);
            float minX = first.x;
            float maxX = first.x;
            float minY = first.y;
            float maxY = first.y;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 local = targetParent.InverseTransformPoint(corners[i]);
                minX = Mathf.Min(minX, local.x);
                maxX = Mathf.Max(maxX, local.x);
                minY = Mathf.Min(minY, local.y);
                maxY = Mathf.Max(maxY, local.y);
            }

            return new Vector2(maxX - minX, maxY - minY);
        }

        private float ResolveDuration(AnimationFlyRequest request)
        {
            return AnimationFlyMath.ResolveDuration(request.Duration, flyDuration, request.SpeedMultiplier);
        }

        private float ResolveItemDelay(AnimationFlyRequest request)
        {
            return AnimationFlyMath.ResolveItemDelay(request.DelayBetweenItems, delayBetweenBonuses,
                request.SpeedMultiplier);
        }

        private static void KillVisualTweens(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.transform.DOKill();
            if (instance.transform is RectTransform rect)
            {
                rect.DOKill();
            }
        }

        private Vector3 ResolvePosition(Vector3 position, AnimationFlyCoordinateSpace sourceSpace,
            AnimationFlySpawnSpace targetSpace, Transform parent, Transform sourceTransform = null,
            float? worldDepth = null)
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
                    AnimationFlyCoordinateSpace.World => WorldToCanvasLocalPosition(position, canvas,
                        ResolveCamera(canvas), parent),
                    AnimationFlyCoordinateSpace.Screen => ScreenToCanvasLocalPosition(position, canvas,
                        ResolveCamera(canvas), parent),
                    AnimationFlyCoordinateSpace.Canvas => CanvasLocalPosition(position, canvas, sourceTransform,
                        parent),
                    _ => position
                };
            }

            return sourceSpace switch
            {
                AnimationFlyCoordinateSpace.Canvas => CanvasToWorldPosition(position, sourceTransform,
                    ResolveCanvas(parent), worldDepth),
                AnimationFlyCoordinateSpace.Screen => ScreenToWorldPosition(position, ResolveCamera(null), worldDepth),
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

            return source is RectTransform || (source != null && source.GetComponentInParent<Canvas>() != null)
                ? AnimationFlyCoordinateSpace.Canvas
                : AnimationFlyCoordinateSpace.World;
        }

        private AnimationFlySpawnSpace ResolveSpawnSpace(Transform parent, GameObject prefab)
        {
            return ResolveSpawnSpace(parent, prefab, null, AnimationFlySpawnSpace.Auto);
        }

        private AnimationFlySpawnSpace ResolveSpawnSpace(Transform parent, GameObject prefab, Sprite sprite,
            AnimationFlySpawnSpace configured)
        {
            AnimationFlySpawnSpace requested = configured != AnimationFlySpawnSpace.Auto ? configured : spawnSpace;
            if (requested != AnimationFlySpawnSpace.Auto)
            {
                return requested;
            }

            if (parent != null && parent.GetComponentInParent<Canvas>() != null)
            {
                return AnimationFlySpawnSpace.Canvas;
            }

            return sprite != null || (prefab != null && prefab.GetComponent<RectTransform>() != null)
                ? AnimationFlySpawnSpace.Canvas
                : AnimationFlySpawnSpace.World;
        }

        // WHY: explicit Unity null check; `??` keeps a destroyed parent and Instantiate under it fails.
        private Transform ResolveParent(Transform parent)
        {
            return parent != null ? parent : spawnParent;
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

            return ResolveFallbackCamera();
        }

        private Camera ResolveFallbackCamera()
        {
            if (_cachedFallbackCamera == null)
            {
                _cachedFallbackCamera = Camera.main;
            }

            return _cachedFallbackCamera;
        }

        private Vector3 CanvasLocalPosition(Vector3 position, Canvas canvas, Transform sourceTransform,
            Transform targetParent)
        {
            if (!(sourceTransform is RectTransform rect) || canvas == null)
            {
                return position;
            }

            Canvas sourceCanvas = rect.GetComponentInParent<Canvas>();
            RectTransform targetRect = ResolveTargetRect(canvas, targetParent);
            if (sourceCanvas == canvas && rect.parent == targetRect)
            {
                return rect.anchoredPosition3D;
            }

            Camera sourceCamera = ResolveCamera(sourceCanvas);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                sourceCanvas != null && sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sourceCamera,
                rect.position);
            return ScreenToCanvasLocalPosition(screenPoint, canvas, ResolveCamera(canvas), targetParent);
        }

        private Vector3 CanvasToWorldPosition(Vector3 position, Transform sourceTransform, Canvas fallbackCanvas,
            float? worldDepth = null)
        {
            if (sourceTransform is RectTransform rect)
            {
                Canvas sourceCanvas = rect.GetComponentInParent<Canvas>();
                if (sourceCanvas == null)
                {
                    sourceCanvas = fallbackCanvas;
                }

                Camera camera = ResolveCamera(sourceCanvas);
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                    sourceCanvas != null && sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera,
                    rect.position);
                return ScreenToWorldPosition(screenPoint, camera, worldDepth);
            }

            return CanvasToWorldPosition(position, fallbackCanvas, ResolveCamera(fallbackCanvas), worldDepth);
        }

        private static Vector3 WorldToCanvasLocalPosition(Vector3 worldPosition, Canvas canvas, Camera camera,
            Transform targetParent = null)
        {
            if (canvas == null)
            {
                return worldPosition;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            return ScreenToCanvasLocalPosition(screenPoint, canvas, camera, targetParent);
        }

        private static Vector3 ScreenToCanvasLocalPosition(Vector3 screenPosition, Canvas canvas, Camera camera,
            Transform targetParent = null)
        {
            if (canvas == null)
            {
                return screenPosition;
            }

            RectTransform canvasRect = ResolveTargetRect(canvas, targetParent);
            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera,
                out Vector2 localPoint);
            return localPoint;
        }

        private static RectTransform ResolveTargetRect(Canvas canvas, Transform targetParent)
        {
            if (targetParent is RectTransform targetRect)
            {
                return targetRect;
            }

            return canvas != null ? canvas.transform as RectTransform : null;
        }

        // WHY: an instance method so each AnimationFly uses its own Default Screen To World Depth; reading the
        // singleton's value gave a second fly the wrong depth, or none when it was the only one in the scene.
        private Vector3 ScreenToWorldPosition(Vector3 screenPosition, Camera camera, float? worldDepth = null)
        {
            if (camera == null)
            {
                return screenPosition;
            }

            float z = worldDepth ?? screenPosition.z;
            if (Mathf.Approximately(z, 0f))
            {
                z = defaultScreenToWorldDepth > 0f
                    ? defaultScreenToWorldDepth
                    : Mathf.Abs(camera.transform.position.z);
            }

            return camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, z));
        }

        private bool TryResolveVisual(AnimationFlyRequest request, out GameObject prefab, out Sprite sprite,
            out UnityEngine.Object poolKey)
        {
            prefab = request.Prefab;
            sprite = request.Sprite;

            if (prefab == null && request.Type.HasValue)
            {
                if (TryGetPrefabData(request.Type.Value, out BonusPrefabData data))
                {
                    prefab = data.prefab;
                    if (request.EndTransform == null && data.endPos != null)
                    {
                        request.EndTransform = data.endPos;
                    }
                }
                else
                {
                    NeoDiagnostics.LogWarning($"[AnimationFly] No prefab for bonus type {request.Type.Value}", this);
                }
            }

            poolKey = prefab != null ? prefab : sprite;
            if (poolKey != null)
            {
                return true;
            }

            NeoDiagnostics.LogWarning("[AnimationFly] Request has neither prefab, sprite, nor known type.", this);
            return false;
        }

        private GameObject SpawnVisual(GameObject prefab, Sprite sprite, UnityEngine.Object poolKey, Transform parent,
            AnimationFlySpawnSpace resolvedSpawnSpace)
        {
            if (_visuals.TryTake(poolKey, resolvedSpawnSpace, parent, out GameObject pooled))
            {
                pooled.SetActive(true);
                return pooled;
            }

            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, parent);
                _visuals.Track(instance);
                return instance;
            }

            GameObject go = new("AnimationFlySprite");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            if (resolvedSpawnSpace == AnimationFlySpawnSpace.Canvas)
            {
                RectTransform rect = go.AddComponent<RectTransform>();
                rect.sizeDelta =
                    sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) : Vector2.one * 32f;
                Image image = go.AddComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
            }
            else
            {
                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
            }

            _visuals.Track(go);
            return go;
        }

        private void CompleteVisual(GameObject instance, UnityEngine.Object poolKey, AnimationFlySpawnSpace space,
            AnimationFlyCompletionMode mode)
        {
            if (instance == null)
            {
                return;
            }

            switch (mode)
            {
                case AnimationFlyCompletionMode.KeepAlive:
                    _visuals.Release(instance);
                    break;
                case AnimationFlyCompletionMode.DisableAndPool:
                    KillVisualTweens(instance);
                    instance.SetActive(false);
                    if (!_visuals.TryReturn(poolKey, space, instance, maxPoolPerKey))
                    {
                        Destroy(instance);
                    }

                    break;
                default:
                    _visuals.Release(instance);
                    Destroy(instance);
                    break;
            }
        }

        public GameObject GetPrefab(int type)
        {
            if (TryGetPrefabData(type, out BonusPrefabData data))
            {
                return data.prefab;
            }

            NeoDiagnostics.LogWarning($"[AnimationFly] No prefab for bonus type {type}", this);
            return null;
        }

        public Transform GetPos(int type)
        {
            if (TryGetPrefabData(type, out BonusPrefabData data))
            {
                return data.endPos;
            }

            NeoDiagnostics.LogWarning($"[AnimationFly] No spawn point for bonus type {type}", this);
            return null;
        }

        public static Vector3 CanvasToWorldPosition(Vector2 uiPosition, Canvas canvas = null, Camera camera = null,
            float? worldDepth = null)
        {
            // WHY: HasInstance, not I — the singleton returns null in a scene without an AnimationFly,
            // and dereferencing it here would throw instead of reaching the fallback below.
            if (canvas == null && HasInstance)
            {
                canvas = I.parentCanvas;
            }

            if (canvas == null)
            {
                NeoDiagnostics.LogError(
                    "[AnimationFly] Canvas is not set and parentCanvas is not assigned!");
                return Vector3.zero;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                camera = null;
            }
            else if (camera == null)
            {
                camera = canvas.worldCamera != null ? canvas.worldCamera : ResolveStaticFallbackCamera();
            }

            Vector3 worldPos = Vector3.zero;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform,
                uiPosition,
                camera,
                out worldPos);
            if (camera != null && worldDepth.HasValue)
            {
                worldPos = camera.ScreenToWorldPoint(new Vector3(uiPosition.x, uiPosition.y, worldDepth.Value));
            }

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
            // WHY: HasInstance, not I — the singleton returns null in a scene without an AnimationFly,
            // and dereferencing it here would throw instead of reaching the fallback below.
            if (canvas == null && HasInstance)
            {
                canvas = I.parentCanvas;
            }

            if (canvas == null)
            {
                NeoDiagnostics.LogError(
                    "[AnimationFly] Canvas is not set and parentCanvas is not assigned!");
                return Vector2.zero;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                camera = null;
            }
            else if (camera == null)
            {
                camera = canvas.worldCamera != null ? canvas.worldCamera : ResolveStaticFallbackCamera();
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            return screenPoint;
        }

        private static Camera ResolveStaticFallbackCamera()
        {
            return HasInstance ? I.ResolveFallbackCamera() : Camera.main;
        }

        [Serializable]
        public sealed class AnimationFlyRequest
        {
            public int? Type;
            public GameObject Prefab;
            public Sprite Sprite;
            public int Count = 1;
            public int MaxCount;
            public float CountMultiplier = 1f;
            public float? Duration;
            public float SpeedMultiplier = 1f;
            public float? DelayBetweenItems;
            public Transform StartTransform;
            public Transform EndTransform;
            public Vector3 StartPosition;
            public Vector3 EndPosition;
            public AnimationFlyCoordinateSpace StartSpace = AnimationFlyCoordinateSpace.Auto;
            public AnimationFlyCoordinateSpace EndSpace = AnimationFlyCoordinateSpace.Auto;
            public AnimationFlySpawnSpace SpawnSpace = AnimationFlySpawnSpace.Auto;
            public Transform Parent;
            public AnimationFlyCompletionMode? CompletionMode;
            public AnimationFlyRewardTiming RewardTiming = AnimationFlyRewardTiming.OnAllArrived;
            public Action<GameObject> OnItemStarted;
            public Action<GameObject> OnItemArrived;
            public Action OnAllArrived;
            public Action OnReward;
            public Vector3? StartRandomOffset;
            public Vector3? EndRandomOffset;
            public Vector3? MiddleRandomOffset;
            public Vector2? UiSize;
            public RectTransform UiSizeSource;
            public float? ScaleMultiplier;
            public float? EndScaleMultiplier;
            public Ease? ScaleEase;
            public AnimationFlyMotionPreset? MotionPreset;
            public Vector3? BurstOffset;
            public Vector3? BurstRandomOffset;
            public float? BurstDurationRatio;
            public float? BurstHoldDuration;
            public float? MagnetDistance;
            public float? MagnetDurationRatio;
            public Ease? BurstEase;
            public Ease? CruiseEase;
            public Ease? MagnetEase;

            /// <summary>Request for <paramref name="count" /> prefab visuals; set the remaining fields afterwards.</summary>
            public static AnimationFlyRequest FromPrefab(GameObject prefab, int count = 1, Transform start = null,
                Transform end = null)
            {
                return new AnimationFlyRequest
                {
                    Prefab = prefab,
                    Count = count,
                    StartTransform = start,
                    EndTransform = end
                };
            }

            /// <summary>Request for <paramref name="count" /> sprite visuals; set the remaining fields afterwards.</summary>
            public static AnimationFlyRequest FromSprite(Sprite sprite, int count = 1, Transform start = null,
                Transform end = null)
            {
                return new AnimationFlyRequest
                {
                    Sprite = sprite,
                    Count = count,
                    StartTransform = start,
                    EndTransform = end
                };
            }

            /// <summary>
            ///     Shallow copy: scene references and callbacks are shared, values are copied. Use it to keep a
            ///     configured request as a template. <see cref="AnimationFly.Play(AnimationFlyRequest)" /> already
            ///     copies the request it receives.
            /// </summary>
            public AnimationFlyRequest Clone()
            {
                return (AnimationFlyRequest)MemberwiseClone();
            }
        }

        public sealed class AnimationFlyResult
        {
            public static readonly AnimationFlyResult Empty = new(0);
            private readonly List<GameObject> _activeItems = new();

            public AnimationFlyResult(int totalCount)
            {
                TotalCount = Mathf.Max(0, totalCount);
            }

            public int TotalCount { get; }
            public int StartedCount { get; private set; }
            public int CompletedCount { get; private set; }
            public bool IsCompleted => CompletedCount >= TotalCount;
            public IReadOnlyList<GameObject> ActiveItems => _activeItems;

            internal void RegisterStarted(GameObject item)
            {
                StartedCount++;
                if (item != null)
                {
                    _activeItems.Add(item);
                }
            }

            internal void RegisterCompleted(GameObject item)
            {
                CompletedCount++;
                _activeItems.Remove(item);
            }
        }

        // WHY: one yield instruction per flight instead of one per spawned item. Rebuilt only when the
        // delay or the time mode changes; WaitForSecondsRealtime resets itself after each completed wait.
        internal struct ItemDelayYield
        {
            private object _instruction;
            private float _delay;
            private bool _unscaled;

            /// <summary>The cached instruction for this delay and time mode, rebuilt only when either changes.</summary>
            public object Get(float delay, bool unscaled)
            {
                if (_instruction == null || _delay != delay || _unscaled != unscaled)
                {
                    _instruction = unscaled ? new WaitForSecondsRealtime(delay) : new WaitForSeconds(delay);
                    _delay = delay;
                    _unscaled = unscaled;
                }

                return _instruction;
            }
        }

        private sealed class AnimationFlyVisualState : MonoBehaviour
        {
            public Vector3 BaseLocalScale = Vector3.one;
            public Quaternion BaseLocalRotation = Quaternion.identity;
            public bool HasBaseSizeDelta;
            public Vector2 BaseSizeDelta;
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
