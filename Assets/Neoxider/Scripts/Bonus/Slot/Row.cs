using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
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
        public float spaceY = 1f;
        public float offsetY;

        public UnityEvent OnStop = new();

        private SpritesData _allSpritesData;
        public bool is_spinning { get; private set; }

        private Tween _spinTween;

        // «Смещение» барабана в юнитах. 0 означает, что элементы стоят строго на сетке.
        private float _reelPosition;   // растет в + при вращении
        private float _reelHeight;     // SlotElements.Length * spaceY

        private void Awake()  { ApplyLayout(); }
        private void OnValidate() { ApplyLayout(); }

        private void Update()
        {
            if (!is_spinning || SlotElements == null || SlotElements.Length == 0) return;
            UpdateElementsPosition();
        }

        public void ApplyLayout()
        {
            SlotElements = GetComponentsInChildren<SlotElement>(true);
            if (SlotElements == null || SlotElements.Length == 0) return;

            _reelHeight = Mathf.Max(0.0001f, SlotElements.Length * Mathf.Abs(spaceY));

            for (int i = 0; i < SlotElements.Length; i++)
            {
                float yPos = offsetY + i * spaceY;
                SetLocalY(SlotElements[i].transform, yPos);
            }

            // После ребилда сетки – аккуратно нормализуем смещение
            SnapReelPositionToGrid(keepMultipleTurns: false);
            UpdateElementsPosition();
        }

        private void UpdateElementsPosition()
        {
            // Нормализуем, чтобы число не разрасталось и % работал стабильно
            _reelPosition = PositiveMod(_reelPosition, _reelHeight);

            for (int i = 0; i < SlotElements.Length; i++)
            {
                float initialY = i * spaceY;
                // Сдвигаем сетку вниз на _reelPosition и замыкаем кольцо
                float y = offsetY + PositiveMod(initialY - _reelPosition, _reelHeight);
                SetLocalY(SlotElements[i].transform, y);
            }
        }

        private IEnumerator SpinCoroutine()
        {
            is_spinning = true;

            // Фаза 1: постоянная скорость
            float tEnd = Time.time + Mathf.Max(0f, speedControll.timeSpin);
            while (Time.time < tEnd)
            {
                _reelPosition += speedControll.speed * Time.deltaTime;
                yield return null;
            }

            // Фаза 2: замедление с «посадкой» на ближайшую щёлку кратно spaceY
            // 1) Берем остаток до сетки
            float remainder = PositiveMod(_reelPosition, spaceY);
            float toNextStep = (remainder == 0f) ? 0f : (spaceY - remainder);

            // 2) Добавим 1–2 оборота для визуальной «массы»
            float extraTurns = _reelHeight * 2f;

            // 3) Цель: текущее + до ближайшей сетки + дополнительные обороты
            float target = _reelPosition + toNextStep + extraTurns;

            // 4) Плавный твин до target. Никаких подмен визуалов здесь не делаем!
            _spinTween = DOTween.To(() => _reelPosition, v => _reelPosition = v, target, speedControll.decelerationTime)
                .SetEase(speedControll.decelerationEase)
                .OnComplete(() =>
                {
                    // На финише – выравниваем точно на сетку и нормализуем
                    SnapReelPositionToGrid(keepMultipleTurns: false);
                    UpdateElementsPosition();

                    is_spinning = false;
                    OnStop?.Invoke();
                });

            yield break;
        }

        private void SetLocalY(Transform t, float y)
        {
            if (t is RectTransform rt) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            else t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);
        }

        private float GetLocalY(Transform t)
        {
            return t is RectTransform rt ? rt.anchoredPosition.y : t.localPosition.y;
        }

        public void Spin(SpritesData allSpritesData, SlotVisualData[] /*ignored*/ finalVisuals)
        {
            _spinTween?.Kill();
            StopAllCoroutines();

            _allSpritesData = allSpritesData;

            // На старте подкидываем рандомные визуалы – контроллер потом считает то, что реально видно
            if (_allSpritesData != null && _allSpritesData.visuals != null && _allSpritesData.visuals.Length > 0)
            {
                foreach (var slot in SlotElements)
                {
                    var v = GetRandomVisualData();
                    if (v != null) slot.SetVisuals(v);
                }
            }

            StartCoroutine(SpinCoroutine());
        }

        public void Stop()
        {
            _spinTween?.Kill();
            StopAllCoroutines();

            // Мгновенная посадка на ближайшую сетку — без броска
            SnapReelPositionToGrid(keepMultipleTurns: false);
            UpdateElementsPosition();

            is_spinning = false;
            OnStop?.Invoke();
        }

        public void SetVisuals(SlotVisualData data)
        {
            if (data == null) return;
            foreach (var s in SlotElements) s.SetVisuals(data);
        }

        public SlotElement[] GetVisibleTopDown()
        {
            // Чем выше Y – тем «видимее». Top → Down
            return SlotElements
                .OrderByDescending(s => GetLocalY(s.transform))
                .Take(3)
                .ToArray();
        }

        private SlotVisualData GetRandomVisualData()
        {
            if (_allSpritesData == null || _allSpritesData.visuals == null || _allSpritesData.visuals.Length == 0) return null;
            int idx = Random.Range(0, _allSpritesData.visuals.Length);
            return _allSpritesData.visuals[idx];
        }

        // --- Helpers ---

        private static float PositiveMod(float a, float m)
        {
            if (m <= 0f) return 0f;
            float r = a % m;
            return (r < 0f) ? r + m : r;
        }

        /// <summary>Ставит _reelPosition на ближайшую вниз/вверх сетку (по умолчанию — вниз) и нормализует.</summary>
        private void SnapReelPositionToGrid(bool keepMultipleTurns)
        {
            if (spaceY <= 0f || _reelHeight <= 0f) return;

            // приводим к [0, _reelHeight)
            float pos = PositiveMod(_reelPosition, _reelHeight);

            // округление до ближайшего шага (к сетке)
            float steps = Mathf.Round(pos / spaceY);
            pos = steps * spaceY;

            // гарантируем допустимый диапазон и положительность
            pos = PositiveMod(pos, _reelHeight);

            // хотим ли сохранить «число оборотов»? обычно не нужно
            _reelPosition = keepMultipleTurns ? (_reelPosition - PositiveMod(_reelPosition, _reelHeight) + pos) : pos;
        }
    }
}
