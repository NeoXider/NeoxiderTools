using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
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

    public enum AnimationFlyCompletionMode
    {
        Destroy = 0,
        DisableAndPool = 1,
        KeepAlive = 2
    }

    public enum AnimationFlyRewardTiming
    {
        Manual = 0,
        OnEachArrived = 1,
        OnAllArrived = 2
    }

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
        private readonly Dictionary<int, BonusPrefabData> _prefabDict = new();
        private readonly Dictionary<UnityEngine.Object, Stack<GameObject>> _pool = new();
        private Camera _cachedFallbackCamera;

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

        public AnimationFlyResult Play(AnimationFlyRequest request)
        {
            if (request == null)
            {
                NeoDiagnostics.LogWarning("[AnimationFly] Request is missing.", this);
                return AnimationFlyResult.Empty;
            }

            if (!TryResolveVisual(request, out GameObject prefab, out Sprite sprite, out UnityEngine.Object poolKey))
            {
                return AnimationFlyResult.Empty;
            }

            Transform parent = request.Parent ?? spawnParent;
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

            int finalCount = Mathf.CeilToInt(request.Count * request.CountMultiplier * countMultiplier);
            finalCount = Mathf.Clamp(finalCount, 0,
                request.MaxCount > 0 ? Mathf.Min(request.MaxCount, maxBonusCount) : maxBonusCount);
            if (finalCount <= 0)
            {
                return AnimationFlyResult.Empty;
            }

            var result = new AnimationFlyResult(finalCount);
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
            if (!_prefabDict.TryGetValue(type, out BonusPrefabData data))
            {
                NeoDiagnostics.LogError($"[AnimationBonus] No prefab for bonus type {type}");
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
                NeoDiagnostics.LogError($"[AnimationBonus] No prefab for bonus type {type}");
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
                NeoDiagnostics.LogError($"[AnimationBonus] No prefab for bonus type {type}");
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
            if (!_prefabDict.TryGetValue(type, out BonusPrefabData data))
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
                ResetVisualForFlight(bonus);
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
                        .SetUpdate(useUnscaledTime)
                        .SetTarget(bonus.transform)
                        .SetLink(bonus);
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
            for (int i = 0; i < bonusCount; i++)
            {
                Vector3 startPos = start + RandomOffset(request.StartRandomOffset ?? startRandomOffset);
                Vector3 endPos = end + RandomOffset(request.EndRandomOffset ?? endRandomOffset);

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
                request.OnItemStarted?.Invoke(bonus);

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
                {
                    request.OnItemArrived?.Invoke(bonus);
                    if (request.RewardTiming == AnimationFlyRewardTiming.OnEachArrived)
                    {
                        request.OnReward?.Invoke();
                    }

                    result.RegisterCompleted(bonus);
                    CompleteVisual(bonus, poolKey, completionMode);
                    if (result.IsCompleted)
                    {
                        if (request.RewardTiming == AnimationFlyRewardTiming.OnAllArrived)
                        {
                            request.OnReward?.Invoke();
                        }

                        request.OnAllArrived?.Invoke();
                    }
                });

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

                float itemDelay = ResolveItemDelay(request);
                yield return useUnscaledTime
                    ? new WaitForSecondsRealtime(itemDelay)
                    : new WaitForSeconds(itemDelay);
            }
        }

        private Vector3 BuildArcPoint(Vector3 startPos, Vector3 endPos, Vector3 randomOffset)
        {
            var arcPoint = Vector3.Lerp(startPos, endPos, middlePoint);
            arcPoint += new Vector3(
                Random.Range(-arcStrength, arcStrength),
                Random.Range(arcStrength * multY, arcStrength),
                Random.Range(-arcStrength, arcStrength));
            arcPoint += RandomOffset(randomOffset);
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
                                  RandomOffset(request.BurstRandomOffset ?? burstRandomOffset);
            if (ignoreZ)
            {
                launchPoint.z = 0f;
            }

            float popDuration =
                duration * Mathf.Clamp(request.BurstDurationRatio ?? burstDurationRatio, 0.05f, 0.85f);
            float holdDuration = Mathf.Max(0f, request.BurstHoldDuration ?? burstHoldDuration) /
                                 ResolveSpeedMultiplier(request);
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

            float scatterDuration = duration * Mathf.Clamp(request.BurstDurationRatio ?? burstDurationRatio,
                0.05f, 0.85f);
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
            float magnetRatio = Mathf.Clamp(request.MagnetDurationRatio ?? magnetDurationRatio, 0.05f, 0.85f);
            float magnetDuration = Mathf.Max(0.01f, duration * magnetRatio);
            float approachDuration = Mathf.Max(0.01f, duration - magnetDuration);
            Vector3 approachPoint = BuildMagnetApproachPoint(startPos, endPos,
                request.MagnetDistance ?? magnetDistance);

            sequence.Append(CreateMoveTween(target, approachPoint, approachDuration, resolvedSpawnSpace)
                .SetEase(request.CruiseEase ?? Ease.OutQuad));
            sequence.Append(CreateMoveTween(target, endPos, magnetDuration, resolvedSpawnSpace)
                .SetEase(request.MagnetEase ?? Ease.InQuad));
        }

        private static Vector3 BuildMagnetApproachPoint(Vector3 startPos, Vector3 endPos, float distance)
        {
            Vector3 direction = startPos - endPos;
            if (direction.sqrMagnitude < 0.0001f || distance <= 0f)
            {
                return Vector3.Lerp(startPos, endPos, 0.85f);
            }

            return endPos + direction.normalized * distance;
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

            if (!IsFinite(requestedSize.Value.x) || !IsFinite(requestedSize.Value.y))
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

            var corners = new Vector3[4];
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
            float duration = request.Duration ?? flyDuration;
            if (!IsFinite(duration))
            {
                duration = IsFinite(flyDuration) ? flyDuration : 1f;
            }

            duration = Mathf.Max(0.001f, duration);
            return duration / ResolveSpeedMultiplier(request);
        }

        private float ResolveItemDelay(AnimationFlyRequest request)
        {
            float delay = request.DelayBetweenItems ?? delayBetweenBonuses;
            if (!IsFinite(delay))
            {
                delay = IsFinite(delayBetweenBonuses) ? delayBetweenBonuses : 0f;
            }

            delay = Mathf.Max(0f, delay);
            return delay / ResolveSpeedMultiplier(request);
        }

        private static float ResolveSpeedMultiplier(AnimationFlyRequest request)
        {
            return IsFinite(request.SpeedMultiplier) && request.SpeedMultiplier > 0f
                ? request.SpeedMultiplier
                : 1f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
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

        private static Vector3 RandomOffset(Vector3 maxAbs)
        {
            return new Vector3(
                Random.Range(-Mathf.Abs(maxAbs.x), Mathf.Abs(maxAbs.x)),
                Random.Range(-Mathf.Abs(maxAbs.y), Mathf.Abs(maxAbs.y)),
                Random.Range(-Mathf.Abs(maxAbs.z), Mathf.Abs(maxAbs.z)));
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
                Canvas sourceCanvas = rect.GetComponentInParent<Canvas>() ?? fallbackCanvas;
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

        private static Vector3 ScreenToWorldPosition(Vector3 screenPosition, Camera camera, float? worldDepth = null)
        {
            if (camera == null)
            {
                return screenPosition;
            }

            float z = worldDepth ?? screenPosition.z;
            if (Mathf.Approximately(z, 0f))
            {
                z = HasInstance ? I.defaultScreenToWorldDepth : Mathf.Abs(camera.transform.position.z);
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
                if (_prefabDict.TryGetValue(request.Type.Value, out BonusPrefabData data))
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
            if (TryTakeFromPool(poolKey, parent, out GameObject pooled))
            {
                pooled.SetActive(true);
                return pooled;
            }

            if (prefab != null)
            {
                return Instantiate(prefab, parent);
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

            return go;
        }

        private bool TryTakeFromPool(UnityEngine.Object key, Transform parent, out GameObject instance)
        {
            instance = null;
            if (key == null || !_pool.TryGetValue(key, out Stack<GameObject> stack))
            {
                return false;
            }

            while (stack.Count > 0)
            {
                instance = stack.Pop();
                if (instance == null)
                {
                    continue;
                }

                if (parent != null)
                {
                    instance.transform.SetParent(parent, false);
                }

                return true;
            }

            return false;
        }

        private void CompleteVisual(GameObject instance, UnityEngine.Object poolKey, AnimationFlyCompletionMode mode)
        {
            if (instance == null)
            {
                return;
            }

            switch (mode)
            {
                case AnimationFlyCompletionMode.KeepAlive:
                    break;
                case AnimationFlyCompletionMode.DisableAndPool:
                    KillVisualTweens(instance);
                    instance.SetActive(false);
                    if (poolKey == null || maxPoolPerKey <= 0)
                    {
                        Destroy(instance);
                        break;
                    }

                    if (!_pool.TryGetValue(poolKey, out Stack<GameObject> stack))
                    {
                        stack = new Stack<GameObject>();
                        _pool.Add(poolKey, stack);
                    }

                    if (stack.Count < maxPoolPerKey)
                    {
                        stack.Push(instance);
                    }
                    else
                    {
                        Destroy(instance);
                    }

                    break;
                default:
                    Destroy(instance);
                    break;
            }
        }

        public GameObject GetPrefab(int type)
        {
            if (_prefabDict.TryGetValue(type, out BonusPrefabData data))
            {
                return data.prefab;
            }

            NeoDiagnostics.LogWarning($"[AnimationBonus] No prefab for bonus type {type}");
            return null;
        }

        public Transform GetPos(int type)
        {
            if (_prefabDict.TryGetValue(type, out BonusPrefabData data))
            {
                return data.endPos;
            }

            NeoDiagnostics.LogWarning($"[AnimationBonus] No spawn point for bonus type {type}");
            return null;
        }

        public static Vector3 CanvasToWorldPosition(Vector2 uiPosition, Canvas canvas = null, Camera camera = null,
            float? worldDepth = null)
        {
            canvas = canvas ?? I.parentCanvas;
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
                camera = ResolveStaticFallbackCamera();
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
            canvas = canvas ?? I.parentCanvas;
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
                camera = ResolveStaticFallbackCamera();
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

            public static AnimationFlyRequest FromPrefab(GameObject prefab, int count, Transform start, Transform end)
            {
                return new AnimationFlyRequest
                {
                    Prefab = prefab,
                    Count = count,
                    StartTransform = start,
                    EndTransform = end
                };
            }

            public static AnimationFlyRequest FromSprite(Sprite sprite, int count, Transform start, Transform end)
            {
                return new AnimationFlyRequest
                {
                    Sprite = sprite,
                    Count = count,
                    StartTransform = start,
                    EndTransform = end
                };
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
