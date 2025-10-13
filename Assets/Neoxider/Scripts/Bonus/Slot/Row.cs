using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Bonus
{
    public class Row : MonoBehaviour
    {
        public int countSlotElement = 3;
        [Header("SlotElement x2")] public SlotElement[] SlotElements;
        [SerializeField] public SpeedControll speedControll = new();
        public float spaceY = 1;
        public float offsetY;

        public UnityEvent OnStop = new();

        private SpritesData _allSpritesData;
        private float _resetPositionY;

        private float[] _yPositions;
        public bool is_spinning { get; private set; }

        private Queue<SlotVisualData> _finalFeed;   // очередь финальных визуалов, подаётся контроллером
        private bool _useFinalFeed;                 // включаем во время замедления
        private bool _aligning;                     // фаза финального выравнивания

        private void Awake() { ApplyLayout(); }
        private void OnValidate() { ApplyLayout(); }

        /// <summary>Расставляет дочерние элементы в правильные локальные позиции.</summary>
        public void ApplyLayout()
        {
            SlotElements = GetComponentsInChildren<SlotElement>(true);
            if (SlotElements == null || SlotElements.Length == 0) return;

            _yPositions = new float[SlotElements.Length];
            _resetPositionY = offsetY - spaceY;

            for (var i = 0; i < SlotElements.Length; i++)
            {
                var yPos = offsetY + spaceY * i;
                _yPositions[i] = yPos;

                var element = SlotElements[i];

                if (element.TryGetComponent<RectTransform>(out var rectTransform))
                    rectTransform.anchoredPosition = new Vector2(0, yPos);
                else
                    element.transform.localPosition = new Vector3(0, yPos, 0);
            }
        }

        private IEnumerator SpinCoroutine()
        {
            is_spinning = true;

            // 1) Константная скорость
            float timerSpin = 0f;
            while (timerSpin < speedControll.timeSpin)
            {
                timerSpin += Time.deltaTime;
                MoveSlots(speedControll.speed * Time.deltaTime, resetVisuals: true); // рандом при рецикле
                yield return null;
            }

            // 2) Замедление: переключаемся на "корм финала"
            _useFinalFeed = true;

            var decelT = 0f;
            var v0 = speedControll.speed;
            var v1 = Mathf.Max(speedControll.minSpeed, 1f);

            while (decelT < speedControll.decelerationTime)
            {
                decelT += Time.deltaTime;
                var t = Mathf.Clamp01(decelT / speedControll.decelerationTime);
                var v = Mathf.Lerp(v0, v1, t);
                MoveSlots(v * Time.deltaTime, resetVisuals: false); // без рандома; при рецикле — финальный корм
                yield return null;
            }

            // 3) Финальная докрутка до следующей сетки ВНИЗ (без смены спрайтов)
            _aligning = true;
            _useFinalFeed = false;

            var sorted = SlotElements.OrderBy(s => GetLocalY(s.transform)).ToArray();
            var first = sorted.First(); // нижний видимый как якорь
            var targetY = FindNextGridYDown(GetLocalY(first.transform));

            while (!Mathf.Approximately(GetLocalY(first.transform), targetY))
            {
                float current = GetLocalY(first.transform);
                float newY = Mathf.MoveTowards(current, targetY, speedControll.minSpeed * Time.deltaTime);
                float delta = newY - current; // отрицательное (вниз)
                MoveSlots(-delta, resetVisuals: false);
                yield return null;
            }

            // Финальная фиксация по сетке (позиции), спрайты НЕ трогаем
            sorted = SlotElements.OrderBy(s => GetLocalY(s.transform)).ToArray();
            for (int i = 0; i < sorted.Length; i++)
                SetLocalY(sorted[i].transform, _yPositions[i]);

            is_spinning = false;
            OnStop?.Invoke();
        }

        private float FindNextGridYDown(float currentY)
        {
            const float eps = 1e-4f;
            float best = float.NegativeInfinity;
            bool found = false;

            for (int i = 0; i < _yPositions.Length; i++)
            {
                float y = _yPositions[i];
                if (y <= currentY + eps && y > best)
                {
                    best = y;
                    found = true;
                }
            }

            if (!found) best = _yPositions.Min();
            return best;
        }

        private void MoveSlots(float delta, bool resetVisuals)
        {
            foreach (var slot in SlotElements)
            {
                MoveSlot(slot.transform, -delta);

                // если ушёл ниже порога — переезжает наверх и, при надобности, «кормится»
                if (GetLocalY(slot.transform) <= _resetPositionY)
                {
                    float highestY = SlotElements.Max(s => GetLocalY(s.transform));
                    SlotVisualData lastFed = null;

                    while (GetLocalY(slot.transform) <= _resetPositionY)
                    {
                        highestY += spaceY;
                        SetLocalY(slot.transform, highestY);

                        if (_useFinalFeed && _finalFeed != null && _finalFeed.Count > 0)
                            lastFed = _finalFeed.Dequeue();
                    }

                    if (_useFinalFeed)
                    {
                        if (lastFed != null) slot.SetVisuals(lastFed);
                    }
                    else if (resetVisuals)
                    {
                        var rnd = GetRandomVisualData();
                        if (rnd != null) slot.SetVisuals(rnd);
                    }
                }
            }
        }

        private void MoveSlot(Transform t, float yDelta)
        {
            if (t is RectTransform rt) rt.anchoredPosition += new Vector2(0, yDelta);
            else t.localPosition += new Vector3(0, yDelta, 0);
        }

        private float GetLocalY(Transform t)
        {
            return t is RectTransform rt ? rt.anchoredPosition.y : t.localPosition.y;
        }

        private void SetLocalY(Transform t, float y)
        {
            if (t is RectTransform rt) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            else t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);
        }

        private SlotVisualData GetRandomVisualData()
        {
            if (_allSpritesData == null || _allSpritesData.visuals.Length == 0) return null;
            var randomIndex = Random.Range(0, _allSpritesData.visuals.Length);
            return _allSpritesData.visuals[randomIndex];
        }

        public void Spin(SpritesData allSpritesData, SlotVisualData[] finalVisuals)
        {
            _allSpritesData = allSpritesData;
            _finalFeed = new Queue<SlotVisualData>(finalVisuals ?? new SlotVisualData[0]);

            _useFinalFeed = false;
            _aligning = false;

            foreach (var slot in SlotElements)
            {
                var rnd = GetRandomVisualData();
                if (rnd != null) slot.SetVisuals(rnd);
            }

            StartCoroutine(SpinCoroutine());
        }

        public void Stop() { is_spinning = false; }

        public void SetVisuals(SlotVisualData data)
        {
            if (data == null) return;
            foreach (var s in SlotElements) s.SetVisuals(data);
        }

        /// <summary>
        /// Возвращает РОВНО три реально видимых слота (Top→Down) по их текущей Y-позиции.
        /// Видимое окно считаем от нижнего порога (_resetPositionY) на 3 шага spaceY.
        /// </summary>
        public SlotElement[] GetVisibleTopDown()
        {
            const float eps = 1e-4f;
            float bottom = _resetPositionY + eps;
            float top = _resetPositionY + spaceY * 3f + eps;

            var inWindow = SlotElements
                .Where(s =>
                {
                    float y = GetLocalY(s.transform);
                    return y > bottom && y <= top;
                })
                .OrderByDescending(s => GetLocalY(s.transform)) // Top→Down
                .ToList();

            // Если по каким-то причинам попало не 3 — подстрахуемся
            if (inWindow.Count < 3)
            {
                // добираем самые «верхние» элементы
                var extra = SlotElements
                    .Except(inWindow)
                    .OrderByDescending(s => GetLocalY(s.transform))
                    .Take(3 - inWindow.Count);
                inWindow.AddRange(extra);
                inWindow = inWindow
                    .OrderByDescending(s => GetLocalY(s.transform))
                    .Take(3)
                    .ToList();
            }

            return inWindow.Take(3).ToArray();
        }
    }
}
