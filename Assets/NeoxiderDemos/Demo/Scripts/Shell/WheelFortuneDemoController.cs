using System.Globalization;
using Neo.Bonus;
using Neo.Shop;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Bonus</b> wheel of fortune demo (WheelFortuneExample.unity). Its
    ///     DemoSceneKit builder saves the frame, the wallet, the prize table and every listener in the scene:
    ///     the wheel's own SPIN/STOP buttons call <see cref="WheelFortune.Spin" /> / <see cref="WheelFortune.Stop" />,
    ///     and <see cref="WheelFortune.OnWinIdVariant" /> calls <see cref="WheelMoneyWin.Win" /> (pays the sector prize
    ///     into the <see cref="Money" /> wallet and writes the prize label) and <see cref="OnWheelResult" />. This
    ///     component only mirrors the wheel state into the authored labels, logs what happens and offers the C#
    ///     path (<see cref="SpinFromCode" />, <see cref="Rearm" />). It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Wheel Fortune Demo")]
    public sealed class WheelFortuneDemoController : MonoBehaviour
    {
        private const string GoodColor = "#42D9A3";
        private const string WarnColor = "#FFB84D";

        [Header("Module")]
        [Tooltip("Wheel of the scene (the WheelFortune prefab instance).")]
        [SerializeField] private WheelFortune _wheel;

        [Tooltip("Turns the sector id into coins (wins[id]).")]
        [SerializeField] private WheelMoneyWin _payout;

        [Tooltip("Wallet that receives the prizes.")]
        [SerializeField] private Money _wallet;

        [Tooltip("Seconds between Spin() and Stop() for the Spin from code button.")]
        [SerializeField] [Min(0.1f)] private float _codeSpinSeconds = 1.5f;

        [Header("Live values")]
        [SerializeField] private TMP_Text _stateValue;
        [SerializeField] private TMP_Text _canUseValue;
        [SerializeField] private TMP_Text _sectorValue;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private WheelFortune.SpinState _shownState;
        private bool _shownCanUse;
        private bool _hasShownStatus;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned (the scene builder ran).</summary>
        public bool IsSceneBuilt => _wheel != null && _payout != null && _wallet != null && _stateValue != null &&
                                    _canUseValue != null && _sectorValue != null && _log != null;

        public WheelFortune Wheel => _wheel;
        public WheelMoneyWin Payout => _payout;
        public Money Wallet => _wallet;

        /// <summary>Sector reported by the last <see cref="WheelFortune.OnWinIdVariant" /> (-1 before the first spin).</summary>
        public int LastSector { get; private set; } = -1;

        /// <summary>Spins completed since Play started.</summary>
        public int SpinsCompleted { get; private set; }

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            RefreshStatus(true);
            Log("Wheel ready: press SPIN under the wheel, then STOP");
        }

        private void Update()
        {
            RefreshStatus(false);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(StopScheduledSpin));
        }

        /// <summary>
        ///     <see cref="WheelFortune.OnWinIdVariant" /> listener (runs after <see cref="WheelMoneyWin.Win" />, which
        ///     already paid the prize).
        /// </summary>
        public void OnWheelResult(int sector)
        {
            LastSector = sector;
            SpinsCompleted++;
            if (_sectorValue != null)
            {
                _sectorValue.text = sector < 0 ? "-" : "#" + sector;
            }

            Log("OnWinIdVariant(" + sector + ") -> WheelMoneyWin.Win -> Money.Add(" + PrizeFor(sector) + ")");
        }

        /// <summary>Button target: <c>wheel.canUse = true</c> lets a single-use wheel spin again.</summary>
        public void Rearm()
        {
            if (_wheel == null)
            {
                WarnUnbuilt();
                return;
            }

            if (_wheel.State != WheelFortune.SpinState.Idle)
            {
                Log("Wait until the wheel stops (State = " + _wheel.State + ")");
                return;
            }

            if (_wheel.canUse)
            {
                Log("canUse is already true: the wheel can spin");
                return;
            }

            _wheel.canUse = true;
            Log("wheel.canUse = true: the single-use wheel can spin again");
        }

        /// <summary>Button target: the C# path, <c>wheel.Spin()</c> now and <c>wheel.Stop()</c> a moment later.</summary>
        public void SpinFromCode()
        {
            if (_wheel == null)
            {
                WarnUnbuilt();
                return;
            }

            if (_wheel.State != WheelFortune.SpinState.Idle)
            {
                Log("The wheel is busy (State = " + _wheel.State + ")");
                return;
            }

            _wheel.Spin();
            if (_wheel.State != WheelFortune.SpinState.Spinning)
            {
                Log("Spin() ignored: canUse is false on this single-use wheel - press Re-arm wheel");
                return;
            }

            CancelInvoke(nameof(StopScheduledSpin));
            Invoke(nameof(StopScheduledSpin), _codeSpinSeconds);
            Log("wheel.Spin(); wheel.Stop() follows in " + _codeSpinSeconds.ToString("0.#", CultureInfo.InvariantCulture) + " s");
        }

        private void StopScheduledSpin()
        {
            if (_wheel != null && _wheel.State == WheelFortune.SpinState.Spinning)
            {
                _wheel.Stop();
            }
        }

        private void RefreshStatus(bool force)
        {
            if (_wheel == null)
            {
                return;
            }

            WheelFortune.SpinState state = _wheel.State;
            if (force || !_hasShownStatus || state != _shownState)
            {
                if (_hasShownStatus && state != _shownState)
                {
                    LogTransition(state);
                }

                _shownState = state;
                if (_stateValue != null)
                {
                    _stateValue.text = state.ToString();
                }
            }

            bool canUse = _wheel.canUse;
            if (force || !_hasShownStatus || canUse != _shownCanUse)
            {
                _shownCanUse = canUse;
                if (_canUseValue != null)
                {
                    _canUseValue.text = canUse
                        ? "<color=" + GoodColor + ">yes</color>"
                        : "<color=" + WarnColor + ">no - used</color>";
                }
            }

            _hasShownStatus = true;
        }

        private void LogTransition(WheelFortune.SpinState state)
        {
            switch (state)
            {
                case WheelFortune.SpinState.Spinning:
                    Log("Spin(): the wheel accelerates (State = Spinning)");
                    break;
                case WheelFortune.SpinState.Decelerating:
                    Log("Stop(): the wheel slows down (State = Decelerating)");
                    break;
                case WheelFortune.SpinState.Aligning:
                    Log("Snapping to the sector under the arrow (State = Aligning)");
                    break;
            }
        }

        private int PrizeFor(int sector)
        {
            int[] wins = _payout != null ? _payout.wins : null;
            return wins != null && sector >= 0 && sector < wins.Length ? wins[sector] : 0;
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[WheelFortuneDemoController] The scene is not authored. Run Neoxider/Demo/Rebuild Active Demo Scene.",
                this);
        }
    }
}
