using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Bonus
{
    /// <summary>
    /// Барабан без твинов:
    /// - Единая фаза (_offset): нет «дыр» и телепортов
    /// - Вверх/вниз по знаку скорости
    /// - Повторные запуски без артефактов
    /// - Расчётная остановка ровно в щёлку (target на сетке)
    /// - Смена спрайтов только вне окна (ниже/выше маски)
    /// - Окно считается от offsetY (нижняя граница окна). windowStartY — зеркало для инспектора.
    /// </summary>
    public class Row : MonoBehaviour
    {
        [Header("Visible")]
        public int countSlotElement = 3;

        [Header("Elements (обычно x2 от видимых)")]
        public SlotElement[] SlotElements;

        [Header("Speed setup")]
        public SpeedControll speedControll = new();   // стартовая скорость (юн/с), знак = направление
        public float defaultStartSpeed = 20f;         // если speedControll.speed == 0

        [Header("Layout")]
        public float spaceY = 1f;                     // шаг сетки (в юнитах)

        [Tooltip("Нижняя граница окна (локальный Y), откуда считаются видимые слоты")]
        public float offsetY = 1f;                    // <-- ГЛАВНЫЙ якорь окна

        [Tooltip("Отражение offsetY (для совместимости/инспектора)")]
        public float windowStartY = 1f;               // зеркало offsetY (не используется напрямую в расчётах)

        [Header("Hidden paddings (зона смены вне окна)")]
        [Tooltip("Сколько НИЖЕ окна появляется элемент при wrap сверху (рекоменд. ≥ 0.6 * spaceY)")]
        public float hiddenPaddingBottom = 0.6f;
        [Tooltip("Сколько ВЫШЕ окна появляется элемент при wrap снизу (рекоменд. ≥ 0.6 * spaceY)")]
        public float hiddenPaddingTop = 0.6f;

        [Header("Stop look&feel")]
        [Tooltip("Мин. добавочные целые шаги до цели при авто-торможении (инерция)")]
        public int extraStepsAtDecel = 3;
        [Tooltip("Ограничение по |a| при торможении (юн/с²). 0 = без ограничения (идеально по формуле)")]
        public float maxDecel = 0f;

        public UnityEvent OnStop = new();

        // API для контроллера
        public bool is_spinning { get; private set; }

        // Источник визуалов (опционально)
        private SpritesData _allSpritesData;

        // Геометрия
        private float _step;           // |spaceY|
        private float _totalSpan;      // SlotElements.Length * _step
        private float _viewBottom;     // = offsetY
        private float _viewTop;        // = offsetY + (countSlotElement-1)*_step
        private float _bottomSpawn;    // ниже окна
        private float _topSpawn;       // выше окна

        // Кинематика
        private float _offset;         // фаза (растёт — вверх)
        private float _vel;            // юн/с
        private float _acc;            // юн/с²
        private int _dirLast = 1;      // +1 вверх, -1 вниз

        // Декелерация к цели
        private float _decelTarget;    // цель фазы (на сетке)
        private int   _decelSign;      // направление до цели (+1 вверх, -1 вниз)

        // Состояния
        private enum State { Idle, Run, Decel }
        private State _state = State.Idle;
        private float _runTEnd;

        // Кэш позиций для wrap
        private float[] _prevY;

        private const float EPS = 1e-4f;

        // ---------------- Unity ----------------

        private void Awake()  { ApplyLayout(); }

        private void OnValidate()
        {
            // теперь offsetY — источник истины; зеркало поддерживаем для инспектора
            windowStartY = offsetY;
            ApplyLayout();
        }

        private void Update()
        {
            if (_state == State.Idle) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            switch (_state)
            {
                case State.Run:
                    Integrate(dt, 0f);
                    if (Time.time >= _runTEnd) BeginDecel();
                    break;

                case State.Decel:
                    Integrate(dt, _acc);
                    // Осталось пройти до цели в направлении _decelSign
                    float remaining = _decelSign * (_decelTarget - _offset);
                    bool passedTarget = remaining <= 0f;
                    bool reversedVel  = Mathf.Sign(_vel) != _decelSign && Mathf.Abs(_vel) > EPS;

                    if (passedTarget || reversedVel || Mathf.Abs(_vel) <= 0.0005f)
                    {
                        _offset = _decelTarget; // фиксируем ровно на сетке
                        _vel = 0f; _acc = 0f;
                        UpdatePositionsAndHandleWraps();
                        FinishStop();
                    }
                    break;
            }
        }

        // ---------------- Публичное API ----------------

        public void ApplyLayout()
        {
            // offsetY -> windowStartY (зеркало)
            windowStartY = offsetY;

            SlotElements = GetComponentsInChildren<SlotElement>(true);
            if (SlotElements == null || SlotElements.Length == 0) return;

            if (spaceY == 0f) spaceY = 1f;
            _step = Mathf.Abs(spaceY);

            // окно считается ОТ offsetY
            _viewBottom = offsetY;
            _viewTop    = offsetY + (countSlotElement - 1) * _step;

            // паддинги: не меньше 0.6 шага
            float minPad = Mathf.Max(0.6f * _step, 0.001f);
            if (hiddenPaddingBottom < minPad) hiddenPaddingBottom = minPad;
            if (hiddenPaddingTop    < minPad) hiddenPaddingTop    = minPad;

            _bottomSpawn = _viewBottom - hiddenPaddingBottom;
            _topSpawn    = _viewTop + hiddenPaddingTop;

            _totalSpan = Mathf.Max(_step, SlotElements.Length * _step);

            // нормализация состояния
            _offset = PositiveMod(_offset, _totalSpan);
            _vel = 0f; _acc = 0f;
            _state = State.Idle;
            is_spinning = false;

            if (_prevY == null || _prevY.Length != SlotElements.Length)
                _prevY = new float[SlotElements.Length];

            // стартовая решётка от нижней зоны спауна
            for (int i = 0; i < SlotElements.Length; i++)
            {
                float y = ResolveY(i, _offset);
                _prevY[i] = y;
                SetLocalY(SlotElements[i].transform, y);
            }
        }

        /// <summary>Удобный метод, если хочешь менять якорь окна в рантайме кодом.</summary>
        public void SetOffsetY(float y, bool reapply = true)
        {
            offsetY = y;
            windowStartY = y;
            if (reapply) ApplyLayout();
        }

        public void Spin(SpritesData allSpritesData, SlotVisualData[] /*ignored*/ finalVisuals)
        {
            StopAllCoroutines(); // на всякий случай
            _allSpritesData = allSpritesData;

            // (опционально) перемешаем визуалы
            if (_allSpritesData?.visuals != null && _allSpritesData.visuals.Length > 0)
            {
                for (int i = 0; i < SlotElements.Length; i++)
                {
                    var v = GetRandomVisualData();
                    if (v != null) SlotElements[i].SetVisuals(v);
                }
            }

            // стартовая скорость
            _vel = Mathf.Abs(speedControll.speed) > EPS
                ? speedControll.speed
                : defaultStartSpeed * (_dirLast == 0 ? 1 : _dirLast);

            _dirLast = _vel >= 0f ? 1 : -1;

            // синхронизируем prev
            for (int i = 0; i < SlotElements.Length; i++)
                _prevY[i] = ResolveY(i, _offset);

            _runTEnd = Time.time + Mathf.Max(0f, speedControll.timeSpin);
            _state = State.Run;
            is_spinning = true;
        }

        public void Stop(bool animate = true)
        {
            StopAllCoroutines();

            if (!animate)
            {
                SnapToNearestStepDirectional();
                FinishStop();
                return;
            }

            if (_state == State.Idle)
            {
                SnapToNearestStepDirectional();
                FinishStop();
            }
            else
            {
                BeginDecel();
            }
        }

        // ---------------- Кинематика и декелерация ----------------

        private void Integrate(float dt, float a)
        {
            // полу-неявная интеграция (устойчивее)
            float vMid = _vel + 0.5f * a * dt;
            _offset += vMid * dt;
            _vel = vMid + 0.5f * a * dt;

            if (Mathf.Abs(_vel) > EPS) _dirLast = _vel > 0f ? 1 : -1;

            UpdatePositionsAndHandleWraps();
        }

        private void BeginDecel()
        {
            _decelSign = (Mathf.Abs(_vel) > EPS) ? (_vel >= 0f ? 1 : -1) : (_dirLast >= 0 ? 1 : -1);

            // расстояние до ближайшей «щёлки» ВПЕРЁД по направлению
            float phase = PositiveMod(_offset, _step);
            float dSnapForward = (_decelSign > 0)
                ? ((phase <= EPS) ? 0f : (_step - phase))  // вверх: до следующей щёлки
                : ((phase <= EPS) ? 0f : phase);           // вниз: до предыдущей щёлки

            // инерция: минимум extraStepsAtDecel целых шагов
            int minK = Mathf.Max(0, extraStepsAtDecel);

            // лимит по акселерации
            float v0 = Mathf.Max(0.001f, Mathf.Abs(_vel));
            float aLimit = (maxDecel > EPS) ? Mathf.Abs(maxDecel) : float.PositiveInfinity;

            // подбираем k так, чтобы |a| = v^2/(2s) <= aLimit
            int k = minK;
            float sGrid = dSnapForward + k * _step;
            float aMag = (v0 * v0) / (2f * Mathf.Max(sGrid, 0.001f));

            if (aMag > aLimit && float.IsFinite(aLimit) && aLimit > 0f)
            {
                float sNeeded = (v0 * v0) / (2f * aLimit);
                int kNeeded = Mathf.CeilToInt(Mathf.Max(0f, (sNeeded - dSnapForward) / _step));
                k = Mathf.Max(minK, kNeeded);
                sGrid = dSnapForward + k * _step;
                aMag = (v0 * 0f + v0 * v0) / (2f * sGrid); // эквивалент (оставлено как подсказка)
                aMag = (v0 * v0) / (2f * sGrid);
            }

            if (sGrid < 0.25f * _step)
            {
                k += 1;
                sGrid = dSnapForward + k * _step;
                aMag = (v0 * v0) / (2f * sGrid);
            }

            _decelTarget = _offset + _decelSign * sGrid;
            _decelTarget = SnapValueToGrid(_decelTarget, _step, _decelSign); // ровно на сетку
            _acc = -_decelSign * aMag;

            _state = State.Decel;
        }

        private void SnapToNearestStepDirectional()
        {
            float phase = PositiveMod(_offset, _step);
            if (_dirLast >= 0) _offset += (phase <= EPS) ? 0f : (_step - phase);
            else               _offset -= (phase <= EPS) ? 0f : phase;

            _offset = PositiveMod(_offset, _totalSpan);
            _vel = 0f; _acc = 0f;
            UpdatePositionsAndHandleWraps();
        }

        private void FinishStop()
        {
            _vel = 0f; _acc = 0f;
            _state = State.Idle;
            is_spinning = false;
            OnStop?.Invoke();
        }

        // ---------------- Позиции + wrap ----------------

        private float ResolveY(int index, float offset)
        {
            float phase = PositiveMod(offset, _totalSpan);
            float y = _bottomSpawn + (index * spaceY + phase);
            float rel = (y - _bottomSpawn) % _totalSpan;
            if (rel < 0f) rel += _totalSpan;
            return _bottomSpawn + rel;
        }

        private void UpdatePositionsAndHandleWraps()
        {
            for (int i = 0; i < SlotElements.Length; i++)
            {
                float yPrev = _prevY[i];
                float yNew  = ResolveY(i, _offset);

                // wrap как существенный скачок между кадрами
                bool wrappedFromTop    = (yNew + EPS) < (yPrev - 0.5f * _step); // вверх → низ
                bool wrappedFromBottom = (yNew - EPS) > (yPrev + 0.5f * _step); // низ → верх

                if (_vel >= 0f && wrappedFromTop)
                {
                    if (yNew < _viewBottom - EPS) MaybeAssignNewVisual(i);
                }
                else if (_vel < 0f && wrappedFromBottom)
                {
                    if (yNew > _viewTop + EPS) MaybeAssignNewVisual(i);
                }

                _prevY[i] = yNew;
                SetLocalY(SlotElements[i].transform, yNew);
            }
        }

        // ---------------- Видимые элементы (ровно 3) ----------------

        private static float GetLocalY(Transform t)
        {
            if (t is RectTransform rt) return rt.anchoredPosition.y;
            return t.localPosition.y;
        }

        /// <summary>
        /// Ровно три видимых сверху-вниз из окна [offsetY .. offsetY+(count-1)*spaceY].
        /// Для каждой ступени k=0..count-1 выбираем ближайший элемент к идеальной позиции.
        /// </summary>
        public SlotElement[] GetVisibleTopDown()
        {
            if (SlotElements == null || SlotElements.Length == 0 || countSlotElement <= 0)
                return new SlotElement[0];

            float step = _step;
            float viewBottom = _viewBottom;

            var buckets = new SlotElement[countSlotElement];
            var bucketErr = new float[countSlotElement];
            for (int k = 0; k < countSlotElement; k++) bucketErr[k] = float.PositiveInfinity;

            // Раскладываем по ближайшей ступени окна
            for (int i = 0; i < SlotElements.Length; i++)
            {
                var se = SlotElements[i];
                if (se == null) continue;

                float y = GetLocalY(se.transform);
                float t = (y - viewBottom) / step;
                int k = Mathf.RoundToInt(t);

                if (k < 0 || k >= countSlotElement) continue;

                float err = Mathf.Abs(t - k);
                if (err < bucketErr[k])
                {
                    bucketErr[k] = err;
                    buckets[k] = se;
                }
            }

            // Фолбэк (не должен сработать при корректной раскладке)
            if (buckets.Any(b => b == null))
            {
                var byY = SlotElements.OrderByDescending(se => GetLocalY(se.transform)).ToList();
                foreach (int k in Enumerable.Range(0, countSlotElement).Where(x => buckets[x] == null))
                {
                    foreach (var se in byY)
                    {
                        if (!buckets.Contains(se))
                        {
                            buckets[k] = se;
                            break;
                        }
                    }
                }
            }

            // Вернуть в порядке Top→Down: k = count-1 .. 0
            var result = new SlotElement[countSlotElement];
            for (int dst = 0, k = countSlotElement - 1; k >= 0; k--, dst++)
                result[dst] = buckets[k];

            return result;
        }

        // ---------------- Helpers ----------------

        private static float PositiveMod(float a, float m)
        {
            if (m <= 0f) return 0f;
            float r = a % m;
            return (r < 0f) ? r + m : r;
        }

        private static float SnapValueToGrid(float value, float step, int dirSign)
        {
            float phase = value % step;
            if (phase < 0f) phase += step;
            if (dirSign >= 0)
                return value + ((phase <= EPS) ? 0f : (step - phase));
            else
                return value - ((phase <= EPS) ? 0f : phase);
        }

        private void MaybeAssignNewVisual(int i)
        {
            if (_allSpritesData?.visuals == null || _allSpritesData.visuals.Length == 0) return;
            var v = GetRandomVisualData();
            if (v != null) SlotElements[i].SetVisuals(v);
        }

        private SlotVisualData GetRandomVisualData()
        {
            int n = _allSpritesData.visuals.Length;
            return n > 0 ? _allSpritesData.visuals[Random.Range(0, n)] : null;
        }

        private void SetLocalY(Transform t, float y)
        {
            if (t is RectTransform rt)
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            else
                t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);
        }

        // Публичные утилиты

        public void SetVisuals(SlotVisualData data)
        {
            if (data == null || SlotElements == null) return;
            foreach (var s in SlotElements) s.SetVisuals(data);
        }

        /// <summary>Для отладки/чтения снизу-вверх.</summary>
        public SlotElement[] GetVisibleBottomUp()
        {
            var topDown = GetVisibleTopDown();
            System.Array.Reverse(topDown); // теперь Bottom→Top
            return topDown;
        }
    }
}
