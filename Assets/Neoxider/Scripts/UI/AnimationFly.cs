using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Neo
{
    public class AnimationFly : Singleton<AnimationFly>
    {
        private readonly Dictionary<int, BonusPrefabData> _prefabDict = new();
        public float arcStrength = 2.0f;

#if ODIN_INSPECTOR
        [TableList]
#endif
        public List<BonusPrefabData> bonusPrefabList = new();

        [Tooltip("Множитель количества спавнящихся объектов")]
        public float countMultiplier = 1f;

        public float delayBetweenBonuses = 0.1f;
        public Ease easyEnd = Ease.InQuad;

        [Space] public Ease easyStart = Ease.OutQuad;

        [Header("Анимация")] public float flyDuration = 1.0f;
        public bool ignoreZ = false;

        [Tooltip("Максимальное количество объектов за один вызов")]
        public int maxBonusCount = 1000;

        [Range(0, 1)] public float middlePoint = 0.4f;
        public float multY = 0.5f;

        public Canvas parentCanvas;

        public float scaleMult = 1;

        [Header("Настройки спавна бонусов")] [Tooltip("Родительский объект для спавна бонусов")]
        public Transform spawnParent;

        public bool useUnscaledTime = false;

        private void Start()
        {
            FillDictionary();
        }

        private void OnValidate()
        {
            FillDictionary();
        }

        private void FillDictionary()
        {
            _prefabDict.Clear();
            foreach (var data in bonusPrefabList)
                if (!_prefabDict.ContainsKey(data.bonusType))
                    _prefabDict.Add(data.bonusType, data);
        }

        public void Execute(int type, int bonusCount, Vector3 start, Action<GameObject> onStart = null,
            Action<GameObject> onEnd = null)
        {
            if (!_prefabDict.ContainsKey(type))
            {
                Debug.LogError($"[AnimationBonus] Нет префаба для бонуса типа {type}");
                return;
            }

            var finalBonusCount = Mathf.CeilToInt(bonusCount * countMultiplier);
            finalBonusCount = Mathf.Min(finalBonusCount, maxBonusCount);

            var endPos = _prefabDict[type].isWorldSpace
                ? Camera.main.WorldToScreenPoint(_prefabDict[type].endPos.position)
                : _prefabDict[type].endPos.position;

            StartCoroutine(AnimationRoutine(_prefabDict[type].prefab, finalBonusCount, start,
                endPos,
                spawnParent, onStart, onEnd));
        }

        public void Execute(int type, int bonusCount, Transform start, Action<GameObject> onStart = null,
            Action<GameObject> onEnd = null)
        {
            if (!_prefabDict.ContainsKey(type))
            {
                Debug.LogError($"[AnimationBonus] Нет префаба для бонуса типа {type}");
                return;
            }

            var finalBonusCount = Mathf.CeilToInt(bonusCount * countMultiplier);
            finalBonusCount = Mathf.Min(finalBonusCount, maxBonusCount);

            var endPos = _prefabDict[type].isWorldSpace
                ? CanvasToWorldPosition(_prefabDict[type].endPos.position)
                : _prefabDict[type].endPos.position;

            StartCoroutine(AnimationRoutine(_prefabDict[type].prefab, finalBonusCount, start.position,
                endPos,
                spawnParent, onStart, onEnd));
        }

        public void Execute(int type, int bonusCount, Transform start, Transform end,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            if (!_prefabDict.ContainsKey(type))
            {
                Debug.LogError($"[AnimationBonus] Нет префаба для бонуса типа {type}");
                return;
            }

            parent = parent ?? spawnParent;
            Execute(_prefabDict[type].prefab, bonusCount, start, end, parent, onStart, onEnd);
        }

        public void Execute(GameObject prefab, int bonusCount, Transform start, Transform end,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            parent = parent ?? spawnParent;
            var finalBonusCount = Mathf.CeilToInt(bonusCount * countMultiplier);
            finalBonusCount = Mathf.Min(finalBonusCount, maxBonusCount);
            StartCoroutine(AnimationRoutine(prefab, finalBonusCount, start.position, end.position, parent, onStart,
                onEnd));
        }

        public void Execute(GameObject prefab, int bonusCount, Vector3 start, Vector3 end,
            Transform parent = null, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            parent = parent ?? spawnParent;
            var finalBonusCount = Mathf.CeilToInt(bonusCount * countMultiplier);
            finalBonusCount = Mathf.Min(finalBonusCount, maxBonusCount);
            StartCoroutine(AnimationRoutine(prefab, finalBonusCount, start, end, parent, onStart, onEnd));
        }

        public IEnumerator AnimationRoutine(GameObject prefab, int bonusCount, Vector3 start, Vector3 end,
            Transform parent, Action<GameObject> onStart = null, Action<GameObject> onEnd = null)
        {
            if (ignoreZ)
            {
                start.z = 0f;
                end.z = 0f;
            }

            for (var i = 0; i < bonusCount; i++)
            {
                var bonus = Instantiate(prefab, start, Quaternion.identity, parent);
                Vector3 scale;
                scale = bonus.transform.localScale;
                scale *= scaleMult;
                bonus.transform.localScale = scale;

                onStart?.Invoke(bonus);

                var startPos = start;
                var endPos = end;
                var midPoint = Vector3.Lerp(startPos, endPos, middlePoint);
                midPoint += new Vector3(
                    Random.Range(-arcStrength, arcStrength),
                    Random.Range(arcStrength * multY, arcStrength),
                    Random.Range(-arcStrength, arcStrength)
                );
                bonus.transform.DOMove(midPoint, flyDuration / 2f)
                    .SetEase(easyStart).SetUpdate(useUnscaledTime)
                    .OnComplete(() =>
                    {
                        bonus.transform.DOMove(endPos, flyDuration / 2f)
                            .SetEase(easyEnd).SetUpdate(useUnscaledTime)
                            .OnComplete(() =>
                            {
                                onEnd?.Invoke(bonus);
                                Destroy(bonus);
                            });
                    });


                yield return new WaitForSeconds(delayBetweenBonuses);
            }
        }

        public GameObject GetPrefab(int type)
        {
            if (_prefabDict.TryGetValue(type, out var data))
                return data.prefab;
            Debug.LogWarning($"[AnimationBonus] Нет префаба для бонуса типа {type}");
            return null;
        }

        public Transform GetPos(int type)
        {
            if (_prefabDict.TryGetValue(type, out var data))
                return data.endPos;
            Debug.LogWarning($"[AnimationBonus] Нет точки спавна для бонуса типа {type}");
            return null;
        }

        public static Vector3 CanvasToWorldPosition(Vector2 uiPosition, Canvas canvas = null, Camera camera = null)
        {
            canvas = canvas ?? I.parentCanvas;
            if (canvas == null)
            {
                Debug.LogError(
                    "[AnimationFly] Canvas не задан и не установлен parentCanvas! / Canvas is not set and parentCanvas is not assigned!");
                return Vector3.zero;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                camera = null;
            else if (camera == null) camera = Camera.main;

            var worldPos = Vector3.zero;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform,
                uiPosition,
                camera,
                out worldPos);
            return worldPos;
        }

        /// <summary>
        ///     Преобразует мировую позицию в позицию на Canvas (Screen Space Overlay/Camera).
        ///     Converts a world position to a position on the Canvas (Screen Space Overlay/Camera).
        /// </summary>
        /// <param name="worldPosition">Мировая позиция / World position</param>
        /// <param name="canvas">
        ///     Canvas, в котором находится UI элемент (если null — используется parentCanvas) / Canvas containing
        ///     the UI element (if null, uses parentCanvas)
        /// </param>
        /// <param name="camera">Камера, используемая Canvas (null для Overlay) / Camera used by the Canvas (null for Overlay)</param>
        /// <returns>Позиция в пространстве Canvas (ScreenPoint) / Position in Canvas space (ScreenPoint)</returns>
        public static Vector2 WorldToCanvasPosition(Vector3 worldPosition, Canvas canvas = null, Camera camera = null)
        {
            canvas = canvas ?? I.parentCanvas;
            if (canvas == null)
            {
                Debug.LogError(
                    "[AnimationFly] Canvas не задан и не установлен parentCanvas! / Canvas is not set and parentCanvas is not assigned!");
                return Vector2.zero;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                camera = null;
            else if (camera == null) camera = Camera.main;

            var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            return screenPoint;
        }

        [Serializable]
        [Tooltip("Префабы бонусов по типу и конечная точка")]
        public struct BonusPrefabData
        {
            public int bonusType;
            public GameObject prefab;
            public Transform endPos;

            [FormerlySerializedAs("isWorld")] [FormerlySerializedAs("isCanvas")]
            public bool isWorldSpace;
        }
    }
}