using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Bonus
{
    /// <summary>
    ///     Reel without tweens:
    ///     - Single phase (_offset): no gaps or teleports
    ///     - Up/down from speed sign
    ///     - Repeat spins without artifacts
    ///     - Stops on grid snaps (target on the grid)
    ///     - Sprite swaps only outside the window (below/above the mask)
    ///     - Window is anchored at offsetY (bottom of window). windowStartY mirrors for the inspector.
    /// </summary>
    [NeoDoc("Bonus/Slot/Row.md")]
    [CreateFromMenu("Neoxider/Bonus/Row", "Prefabs/Bonus/Slot/Row.prefab")]
    [AddComponentMenu("Neoxider/" + "Bonus/" + nameof(Row))]
    public class Row : MonoBehaviour
    {
        private const float EPS = 1e-4f;

        [Header("Visible")] public int countSlotElement = 3;

        [Header("Elements (Usually x2)")] public SlotElement[] SlotElements;

        [Header("Speed setup")] public SpeedControll speedControll; // start speed (units/s), sign = direction

        public float defaultStartSpeed = 20f; // when speedControll.speed == 0

        [Header("Layout")] public float spaceY = 1f; // grid step (in units)

        [Tooltip("Bottom bound of window (local Y) for visible slots")]
        public float offsetY = 1f; // <-- primary window anchor

        [Tooltip("Mirror of offsetY (compatibility/inspector)")]
        public float windowStartY = 1f; // mirror of offsetY (not used directly in math)

        [Header("Hidden Paddings")]
        [Tooltip("How far below window element appears when wrapping from top (recomm. ≥ 0.6 * spaceY)")]
        public float hiddenPaddingBottom = 0.6f;

        [Tooltip("How far above window element appears when wrapping from bottom (recomm. ≥ 0.6 * spaceY)")]
        public float hiddenPaddingTop = 0.6f;

        [Header("Stop look&feel")] [Tooltip("Min extra whole steps to target when auto-braking (inertia)")]
        public int extraStepsAtDecel = 3;

        [Tooltip("|a| limit when braking (units/s²). 0 = no limit (ideal formula)")]
        public float maxDecel;

        public UnityEvent OnStop = new();
        private float _acc; // units/s²

        /// <summary>Desired visible symbol ids bottom→top when the reel stops; cleared after apply.</summary>
        private int[] _spinTargetIdsBottomUp;

        // Visual source (optional)
        private SpritesData _allSpritesData;
        private float _bottomSpawn; // below window
        private int _decelSign; // direction to target (+1 up, -1 down)

        // Deceleration toward target
        private float _decelTarget; // phase target (on grid)
        private int _dirLast = 1; // +1 up, -1 down

        // Kinematics
        private float _offset; // phase (increases = up)

        // Position cache for wrap
        private float[] _prevY;
        private float _runTEnd;
        private State _state = State.Idle;

        // Geometry
        private float _step; // |spaceY|
        private float _topSpawn; // above window
        private float _totalSpan; // SlotElements.Length * _step
        private float _vel; // units/s
        private float _viewBottom; // = offsetY
        private float _viewTop; // = offsetY + (countSlotElement-1)*_step

        // Controller API
        public bool is_spinning { get; private set; }

        // ---------------- Unity ----------------

        private void Awake()
        {
            ApplyLayout();
        }

        private void Update()
        {
            if (_state == State.Idle)
            {
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            switch (_state)
            {
                case State.Run:
                    Integrate(dt, 0f);
                    if (Time.time >= _runTEnd)
                    {
                        BeginDecel();
                    }

                    break;

                case State.Decel:
                    Integrate(dt, _acc);
                    // Distance left to target along _decelSign
                    float remaining = _decelSign * (_decelTarget - _offset);
                    bool passedTarget = remaining <= 0f;
                    bool reversedVel = Mathf.Sign(_vel) != _decelSign && Mathf.Abs(_vel) > EPS;

                    if (passedTarget || reversedVel || Mathf.Abs(_vel) <= 0.0005f)
                    {
                        _offset = _decelTarget; // snap exactly to grid
                        _vel = 0f;
                        _acc = 0f;
                        UpdatePositionsAndHandleWraps();
                        FinishStop();
                    }

                    break;
            }
        }

        private void OnValidate()
        {
            // offsetY is source of truth; keep mirror for inspector
            windowStartY = offsetY;
            ApplyLayout();
        }

        // ---------------- Public API ----------------

        public void ApplyLayout()
        {
            // offsetY -> windowStartY (mirror)
            windowStartY = offsetY;

            SlotElements = GetComponentsInChildren<SlotElement>(true);
            if (SlotElements == null || SlotElements.Length == 0)
            {
                return;
            }

            if (spaceY == 0f)
            {
                spaceY = 1f;
            }

            _step = Mathf.Abs(spaceY);

            // window is measured FROM offsetY
            _viewBottom = offsetY;
            _viewTop = offsetY + (countSlotElement - 1) * _step;

            // padding: at least 0.6 step
            float minPad = Mathf.Max(0.6f * _step, 0.001f);
            if (hiddenPaddingBottom < minPad)
            {
                hiddenPaddingBottom = minPad;
            }

            if (hiddenPaddingTop < minPad)
            {
                hiddenPaddingTop = minPad;
            }

            _bottomSpawn = _viewBottom - hiddenPaddingBottom;
            _topSpawn = _viewTop + hiddenPaddingTop;

            _totalSpan = Mathf.Max(_step, SlotElements.Length * _step);

            // normalize state
            _offset = PositiveMod(_offset, _totalSpan);
            _vel = 0f;
            _acc = 0f;
            _state = State.Idle;
            is_spinning = false;

            if (_prevY == null || _prevY.Length != SlotElements.Length)
            {
                _prevY = new float[SlotElements.Length];
            }

            // initial grid from bottom spawn zone
            for (int i = 0; i < SlotElements.Length; i++)
            {
                float y = ResolveY(i, _offset);
                _prevY[i] = y;
                SetLocalY(SlotElements[i].transform, y);
            }
        }

        /// <summary>Convenience to change the window anchor at runtime from code.</summary>
        public void SetOffsetY(float y, bool reapply = true)
        {
            offsetY = y;
            windowStartY = y;
            if (reapply)
            {
                ApplyLayout();
            }
        }

        /// <param name="targetVisibleIdsBottomUp">Length must match <see cref="countSlotElement"/> or null to skip forcing symbols at stop.</param>
        public void Spin(SpritesData allSpritesData, int[] targetVisibleIdsBottomUp)
        {
            StopAllCoroutines(); // safety
            _allSpritesData = allSpritesData;

            _spinTargetIdsBottomUp = targetVisibleIdsBottomUp != null && targetVisibleIdsBottomUp.Length > 0
                ? (int[])targetVisibleIdsBottomUp.Clone()
                : null;

            // (optional) shuffle visuals
            if (_allSpritesData?.visuals != null && _allSpritesData.visuals.Length > 0)
            {
                for (int i = 0; i < SlotElements.Length; i++)
                {
                    SlotVisualData v = GetRandomVisualData();
                    if (v != null)
                    {
                        SlotElements[i].SetVisuals(v);
                    }
                }
            }

            // start speed
            _vel = Mathf.Abs(speedControll.speed) > EPS
                ? speedControll.speed
                : defaultStartSpeed * (_dirLast == 0 ? 1 : _dirLast);

            _dirLast = _vel >= 0f ? 1 : -1;

            // sync prev
            for (int i = 0; i < SlotElements.Length; i++)
            {
                _prevY[i] = ResolveY(i, _offset);
            }

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

        // ---------------- Kinematics and deceleration ----------------

        private void Integrate(float dt, float a)
        {
            // semi-implicit integration (more stable)
            float vMid = _vel + 0.5f * a * dt;
            _offset += vMid * dt;
            _vel = vMid + 0.5f * a * dt;

            if (Mathf.Abs(_vel) > EPS)
            {
                _dirLast = _vel > 0f ? 1 : -1;
            }

            UpdatePositionsAndHandleWraps();
        }

        private void BeginDecel()
        {
            _decelSign = Mathf.Abs(_vel) > EPS ? _vel >= 0f ? 1 : -1 : _dirLast >= 0 ? 1 : -1;

            // distance to nearest snap AHEAD along motion
            float phase = PositiveMod(_offset, _step);
            float dSnapForward = _decelSign > 0
                ? phase <= EPS ? 0f : _step - phase // up: to next snap
                : phase <= EPS
                    ? 0f
                    : phase; // down: to previous snap

            // inertia: at least extraStepsAtDecel whole steps
            int minK = Mathf.Max(0, extraStepsAtDecel);

            // acceleration limit
            float v0 = Mathf.Max(0.001f, Mathf.Abs(_vel));
            float aLimit = maxDecel > EPS ? Mathf.Abs(maxDecel) : float.PositiveInfinity;

            // pick k so |a| = v^2/(2s) <= aLimit
            int k = minK;
            float sGrid = dSnapForward + k * _step;
            float aMag = v0 * v0 / (2f * Mathf.Max(sGrid, 0.001f));

            if (aMag > aLimit && float.IsFinite(aLimit) && aLimit > 0f)
            {
                float sNeeded = v0 * v0 / (2f * aLimit);
                int kNeeded = Mathf.CeilToInt(Mathf.Max(0f, (sNeeded - dSnapForward) / _step));
                k = Mathf.Max(minK, kNeeded);
                sGrid = dSnapForward + k * _step;
                aMag = (v0 * 0f + v0 * v0) / (2f * sGrid); // equivalent (kept as hint)
                aMag = v0 * v0 / (2f * sGrid);
            }

            if (sGrid < 0.25f * _step)
            {
                k += 1;
                sGrid = dSnapForward + k * _step;
                aMag = v0 * v0 / (2f * sGrid);
            }

            _decelTarget = _offset + _decelSign * sGrid;
            _decelTarget = SnapValueToGrid(_decelTarget, _step, _decelSign); // exactly on grid
            _acc = -_decelSign * aMag;

            _state = State.Decel;
        }

        private void SnapToNearestStepDirectional()
        {
            float phase = PositiveMod(_offset, _step);
            if (_dirLast >= 0)
            {
                _offset += phase <= EPS ? 0f : _step - phase;
            }
            else
            {
                _offset -= phase <= EPS ? 0f : phase;
            }

            _offset = PositiveMod(_offset, _totalSpan);
            _vel = 0f;
            _acc = 0f;
            UpdatePositionsAndHandleWraps();
        }

        private void FinishStop()
        {
            ApplySpinTargetVisualsIfNeeded();

            _vel = 0f;
            _acc = 0f;
            _state = State.Idle;
            is_spinning = false;
            OnStop?.Invoke();
        }

        private void ApplySpinTargetVisualsIfNeeded()
        {
            if (_spinTargetIdsBottomUp == null || SlotElements == null || SlotElements.Length == 0)
            {
                _spinTargetIdsBottomUp = null;
                return;
            }

            if (_spinTargetIdsBottomUp.Length != countSlotElement)
            {
                Debug.LogWarning(
                    $"[{nameof(Row)}] Target id count {_spinTargetIdsBottomUp.Length} != {nameof(countSlotElement)} {countSlotElement}. Ignoring targets.",
                    this);
                _spinTargetIdsBottomUp = null;
                return;
            }

            SlotElement[] topDown = GetVisibleTopDown();
            if (topDown == null || topDown.Length != countSlotElement)
            {
                _spinTargetIdsBottomUp = null;
                return;
            }

            if (_allSpritesData?.visuals == null || _allSpritesData.visuals.Length == 0)
            {
                _spinTargetIdsBottomUp = null;
                return;
            }

            for (int topIdx = 0; topIdx < countSlotElement; topIdx++)
            {
                int bottomUpIdx = countSlotElement - 1 - topIdx;
                int id = _spinTargetIdsBottomUp[bottomUpIdx];
                SlotElement el = topDown[topIdx];
                SlotVisualData v = FindVisualById(id);
                if (el != null && v != null)
                {
                    el.SetVisuals(v);
                }
            }

            _spinTargetIdsBottomUp = null;
        }

        private SlotVisualData FindVisualById(int id)
        {
            if (_allSpritesData?.visuals == null)
            {
                return null;
            }

            foreach (SlotVisualData x in _allSpritesData.visuals)
            {
                if (x != null && x.id == id)
                {
                    return x;
                }
            }

            return null;
        }

        /// <summary>Returns row index in the visible window (0 = bottom) if this element is locked to a window slot.</summary>
        public bool TryGetWindowRowFromBottom(SlotElement element, out int rowFromBottom)
        {
            rowFromBottom = -1;
            if (element == null || SlotElements == null || SlotElements.Length == 0 || countSlotElement <= 0)
            {
                return false;
            }

            SlotElement[] topDown = GetVisibleTopDown();
            if (topDown == null || topDown.Length != countSlotElement)
            {
                return false;
            }

            for (int topIdx = 0; topIdx < topDown.Length; topIdx++)
            {
                if (topDown[topIdx] == element)
                {
                    rowFromBottom = countSlotElement - 1 - topIdx;
                    return true;
                }
            }

            return false;
        }

        // ---------------- Positions + wrap ----------------

        private float ResolveY(int index, float offset)
        {
            float phase = PositiveMod(offset, _totalSpan);
            float y = _bottomSpawn + (index * spaceY + phase);
            float rel = (y - _bottomSpawn) % _totalSpan;
            if (rel < 0f)
            {
                rel += _totalSpan;
            }

            return _bottomSpawn + rel;
        }

        private void UpdatePositionsAndHandleWraps()
        {
            for (int i = 0; i < SlotElements.Length; i++)
            {
                float yPrev = _prevY[i];
                float yNew = ResolveY(i, _offset);

                // wrap as a large jump between frames
                bool wrappedFromTop = yNew + EPS < yPrev - 0.5f * _step; // up -> bottom
                bool wrappedFromBottom = yNew - EPS > yPrev + 0.5f * _step; // bottom -> up

                if (_vel >= 0f && wrappedFromTop)
                {
                    if (yNew < _viewBottom - EPS)
                    {
                        MaybeAssignNewVisual(i);
                    }
                }
                else if (_vel < 0f && wrappedFromBottom)
                {
                    if (yNew > _viewTop + EPS)
                    {
                        MaybeAssignNewVisual(i);
                    }
                }

                _prevY[i] = yNew;
                SetLocalY(SlotElements[i].transform, yNew);
            }
        }

        // ---------------- Visible elements (exactly 3) ----------------

        private static float GetLocalY(Transform t)
        {
            if (t is RectTransform rt)
            {
                return rt.anchoredPosition.y;
            }

            return t.localPosition.y;
        }

        /// <summary>
        ///     Exactly three visible slots top-down in [offsetY .. offsetY+(count-1)*spaceY].
        ///     For each step k=0..count-1, pick the element closest to the ideal position.
        /// </summary>
        public SlotElement[] GetVisibleTopDown()
        {
            if (SlotElements == null || SlotElements.Length == 0 || countSlotElement <= 0)
            {
                return new SlotElement[0];
            }

            float step = _step;
            float viewBottom = _viewBottom;

            var buckets = new SlotElement[countSlotElement];
            float[] bucketErr = new float[countSlotElement];
            for (int k = 0; k < countSlotElement; k++)
            {
                bucketErr[k] = float.PositiveInfinity;
            }

            // Assign to nearest window step
            for (int i = 0; i < SlotElements.Length; i++)
            {
                SlotElement se = SlotElements[i];
                if (se == null)
                {
                    continue;
                }

                float y = GetLocalY(se.transform);
                float t = (y - viewBottom) / step;
                int k = Mathf.RoundToInt(t);

                if (k < 0 || k >= countSlotElement)
                {
                    continue;
                }

                float err = Mathf.Abs(t - k);
                if (err < bucketErr[k])
                {
                    bucketErr[k] = err;
                    buckets[k] = se;
                }
            }

            // Fallback (should not run with correct layout)
            if (buckets.Any(b => b == null))
            {
                var byY = SlotElements.OrderByDescending(se => GetLocalY(se.transform)).ToList();
                foreach (int k in Enumerable.Range(0, countSlotElement).Where(x => buckets[x] == null))
                {
                    foreach (SlotElement se in byY)
                    {
                        if (!buckets.Contains(se))
                        {
                            buckets[k] = se;
                            break;
                        }
                    }
                }
            }

            // Return Top→Down order: k = count-1 .. 0
            var result = new SlotElement[countSlotElement];
            for (int dst = 0, k = countSlotElement - 1; k >= 0; k--, dst++)
            {
                result[dst] = buckets[k];
            }

            return result;
        }

        // ---------------- Helpers ----------------

        private static float PositiveMod(float a, float m)
        {
            if (m <= 0f)
            {
                return 0f;
            }

            float r = a % m;
            return r < 0f ? r + m : r;
        }

        private static float SnapValueToGrid(float value, float step, int dirSign)
        {
            float phase = value % step;
            if (phase < 0f)
            {
                phase += step;
            }

            if (dirSign >= 0)
            {
                return value + (phase <= EPS ? 0f : step - phase);
            }

            return value - (phase <= EPS ? 0f : phase);
        }

        private void MaybeAssignNewVisual(int i)
        {
            if (_allSpritesData?.visuals == null || _allSpritesData.visuals.Length == 0)
            {
                return;
            }

            SlotVisualData v = GetRandomVisualData();
            if (v != null)
            {
                SlotElements[i].SetVisuals(v);
            }
        }

        private SlotVisualData GetRandomVisualData()
        {
            int n = _allSpritesData.visuals.Length;
            return n > 0 ? _allSpritesData.visuals[Random.Range(0, n)] : null;
        }

        private void SetLocalY(Transform t, float y)
        {
            if (t is RectTransform rt)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            }
            else
            {
                t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);
            }
        }

        // Public utilities

        public void SetVisuals(SlotVisualData data)
        {
            if (data == null || SlotElements == null)
            {
                return;
            }

            foreach (SlotElement s in SlotElements)
            {
                s.SetVisuals(data);
            }
        }

        /// <summary>For debug / bottom-up reading.</summary>
        public SlotElement[] GetVisibleBottomUp()
        {
            SlotElement[] topDown = GetVisibleTopDown();
            Array.Reverse(topDown); // now Bottom→Top
            return topDown;
        }

        // States
        private enum State
        {
            Idle,
            Run,
            Decel
        }
    }
}
