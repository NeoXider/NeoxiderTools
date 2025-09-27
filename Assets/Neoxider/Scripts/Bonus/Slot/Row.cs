using System.Collections;
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
        private SlotVisualData[] _finalVisuals;
        private float _resetPositionY;

        private float[] _yPositions;
        public bool is_spinning { get; private set; }

        private void Awake()
        {
            ApplyLayout();
        }

        private void OnValidate()
        {
            ApplyLayout();
        }

        /// <summary>
        ///     Расставляет дочерние элементы в правильные локальные позиции.
        /// </summary>
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
                var elementTransform = element.transform;

                if (element.TryGetComponent<RectTransform>(out var rectTransform))
                    rectTransform.anchoredPosition = new Vector2(0, yPos);
                else
                    elementTransform.localPosition = new Vector3(0, yPos, 0);
            }
        }

        private IEnumerator SpinCoroutine()
        {
            is_spinning = true;

            // 1. Фаза вращения
            float timerSpin = 0;
            while (timerSpin < speedControll.timeSpin)
            {
                timerSpin += Time.deltaTime;
                MoveSlots(speedControll.speed * Time.deltaTime);
                yield return null;
            }

            SetFinalVisuals();

            // 2. Фаза замедления
            var decelerationTimer = 0f;
            var initialSpeed = speedControll.speed;
            while (decelerationTimer < speedControll.decelerationTime)
            {
                decelerationTimer += Time.deltaTime;
                var t = decelerationTimer / speedControll.decelerationTime;
                var currentSpeed = Mathf.Lerp(initialSpeed, speedControll.minSpeed, t);
                MoveSlots(currentSpeed * Time.deltaTime, false);
                yield return null;
            }

            // 3. Фаза финального выравнивания
            var sortedSlots = SlotElements.OrderBy(s => GetLocalY(s.transform)).ToArray();
            var firstSlot = sortedSlots.First();
            var targetY = _yPositions.First();
            var currentY = GetLocalY(firstSlot.transform);

            var travelDistance = 0f;
            var maxTravel = spaceY * 2; // Максимальное расстояние для избежания вечного цикла

            while (currentY > targetY && travelDistance < maxTravel)
            {
                var moveDelta = speedControll.minSpeed * Time.deltaTime;
                MoveSlots(moveDelta, false);
                travelDistance += moveDelta;
                currentY = GetLocalY(firstSlot.transform);
                yield return null;
            }

            // Финальная доводка и расстановка всех элементов по местам
            var finalOffset = targetY - GetLocalY(firstSlot.transform);
            foreach (var slot in SlotElements) MoveSlot(slot.transform, finalOffset);

            // Пересортировка и финальное выравнивание по эталонным позициям
            sortedSlots = SlotElements.OrderBy(s => GetLocalY(s.transform)).ToArray();
            for (var i = 0; i < sortedSlots.Length; i++) SetLocalY(sortedSlots[i].transform, _yPositions[i]);

            is_spinning = false;
            OnStop?.Invoke();
        }

        private void MoveSlots(float delta, bool resetVisuals = true)
        {
            foreach (var slot in SlotElements)
            {
                MoveSlot(slot.transform, -delta);

                if (GetLocalY(slot.transform) <= _resetPositionY)
                {
                    var highestY = SlotElements.Max(s => GetLocalY(s.transform));
                    SetLocalY(slot.transform, highestY + spaceY);

                    if (resetVisuals) slot.SetVisuals(GetRandomVisualData());
                }
            }
        }

        private void MoveSlot(Transform t, float yDelta)
        {
            if (t is RectTransform rt)
                rt.anchoredPosition += new Vector2(0, yDelta);
            else
                t.localPosition += new Vector3(0, yDelta, 0);
        }

        private float GetLocalY(Transform t)
        {
            return t is RectTransform rt ? rt.anchoredPosition.y : t.localPosition.y;
        }

        private void SetLocalY(Transform t, float y)
        {
            if (t is RectTransform rt)
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            else
                t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);
        }

        private void SetFinalVisuals()
        {
            // Сортируем слоты по их текущей позиции, чтобы правильно назначить финальные спрайты
            var sortedByY = SlotElements.OrderBy(s => GetLocalY(s.transform)).ToArray();
            for (var i = 0; i < sortedByY.Length; i++)
            {
                var visualIndex = i % countSlotElement;
                if (visualIndex < _finalVisuals.Length) sortedByY[i].SetVisuals(_finalVisuals[visualIndex]);
            }
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
            _finalVisuals = finalVisuals;

            foreach (var slot in SlotElements) slot.SetVisuals(GetRandomVisualData());

            StartCoroutine(SpinCoroutine());
        }

        public void Stop()
        {
            is_spinning = false;
        }

        public void SetVisuals(SlotVisualData data)
        {
            foreach (var s in SlotElements) s.SetVisuals(data);
        }
    }
}