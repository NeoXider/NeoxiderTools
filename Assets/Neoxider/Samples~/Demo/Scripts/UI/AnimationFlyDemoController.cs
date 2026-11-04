using System;
using DG.Tweening;
using Neo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>AnimationFly</b> demo scene. The whole scene (frame, play buttons, tuning
    ///     sliders, wallet and gem targets, world pickup and chest, the <see cref="AnimationFly" /> service and the
    ///     info card) is saved in AnimationFlyDemo.unity by its DemoSceneKit builder; buttons, sliders and toggles
    ///     call the public methods below through persistent UnityEvents. Every Play method builds one
    ///     <see cref="AnimationFly.AnimationFlyRequest" /> and calls <see cref="AnimationFly.Play" />, the C# path a
    ///     game uses. This component builds no UI and creates no textures at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/UI/Animation Fly Demo Controller")]
    public sealed class AnimationFlyDemoController : MonoBehaviour
    {
        private const int DefaultCount = 8;

        [Header("Service")]
        [Tooltip("Scene AnimationFly. Falls back to AnimationFly.I when left empty.")]
        [SerializeField] private AnimationFly _fly;

        [Header("Visuals (assets)")]
        [Tooltip("Sprite asset used by the Sample Sprite and Fountain + Magnet flights.")]
        [SerializeField] private Sprite _sampleSprite;

        [SerializeField] private Sprite _coinSprite;
        [SerializeField] private Sprite _gemSprite;

        [Tooltip("Prefab asset used by the Pooled Prefab flight (Prefab instead of Sprite in the request).")]
        [SerializeField] private GameObject _prefabVisual;

        [Header("Flight points")]
        [Tooltip("Canvas layer the visuals fly in (AnimationFly.spawnParent). Must not block raycasts.")]
        [SerializeField] private RectTransform _flyRoot;

        [SerializeField] private Transform _worldPickup;
        [SerializeField] private Transform _worldChest;

        [Tooltip("UI start point of the canvas-to-canvas flights (the button that launches them).")]
        [SerializeField] private RectTransform _sourceButton;

        [Tooltip("UI start point of the Scatter flight. Falls back to Source Button.")]
        [SerializeField] private RectTransform _scatterSource;

        [SerializeField] private RectTransform _walletTarget;
        [SerializeField] private RectTransform _gemTarget;

        [Header("Tuning sliders")]
        [SerializeField] private Slider _countSlider;
        [SerializeField] private Slider _durationSlider;
        [SerializeField] private Slider _delaySlider;
        [SerializeField] private Slider _arcSlider;
        [SerializeField] private Slider _scaleSlider;
        [SerializeField] private Slider _rotationSlider;

        [Header("Time")]
        [SerializeField] private Toggle _pauseToggle;
        [SerializeField] private Toggle _unscaledToggle;

        [Header("Readouts")]
        [SerializeField] private TMP_Text _coinValue;
        [SerializeField] private TMP_Text _gemValue;
        [SerializeField] private TMP_Text _rewardValue;
        [SerializeField] private TMP_Text _startedValue;
        [SerializeField] private TMP_Text _ownedValue;
        [SerializeField] private DemoLogFeed _log;

        [Tooltip("Play buttons of the scene (counted by the smoke test).")]
        [SerializeField] private Button[] _demoButtons = Array.Empty<Button>();

        [Header("Behaviour")]
        [Tooltip("Plays the World -> Wallet flight once when the scene starts.")]
        [SerializeField] private bool _playOnStart = true;

        private int _rewardCounter;
        private int _startedCounter;
        private int _coins;
        private int _gems;
        private bool _pausedByDemo;
        private float _timeScaleBeforePause = 1f;
        private bool _warnedUnbuilt;
        private Action _onReward;
        private Action<GameObject> _onItemStarted;
        private Action<GameObject> _onCoinArrived;
        private Action<GameObject> _onGemArrived;

        public int RewardCounter => _rewardCounter;
        public int StartedCounter => _startedCounter;
        public int Coins => _coins;
        public int Gems => _gems;
        public int DemoButtonCount => CountAssigned(_demoButtons);

        public int DemoSliderCount =>
            (_countSlider != null ? 1 : 0) + (_durationSlider != null ? 1 : 0) + (_delaySlider != null ? 1 : 0) +
            (_arcSlider != null ? 1 : 0) + (_scaleSlider != null ? 1 : 0) + (_rotationSlider != null ? 1 : 0);

        public bool FlyRootBlocksRaycasts => _flyRoot != null &&
                                             _flyRoot.TryGetComponent(out Graphic graphic) &&
                                             graphic.raycastTarget;

        /// <summary>True when the authored scene references are assigned (the scene builder ran).</summary>
        public bool IsSceneBuilt => _fly != null && _flyRoot != null && _walletTarget != null && _gemTarget != null &&
                                    _log != null;

        public AnimationFly Fly => _fly;

        private int CurrentCount => _countSlider != null ? Mathf.RoundToInt(_countSlider.value) : DefaultCount;

        private void Awake()
        {
            _onReward = HandleReward;
            _onItemStarted = HandleItemStarted;
            _onCoinArrived = HandleCoinArrived;
            _onGemArrived = HandleGemArrived;
        }

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            ResolveFly();
            ApplyAllTuning();
            if (_unscaledToggle != null && _fly != null)
            {
                _unscaledToggle.SetIsOnWithoutNotify(_fly.useUnscaledTime);
            }

            if (_pauseToggle != null)
            {
                _pauseToggle.SetIsOnWithoutNotify(false);
            }

            RefreshLabels();
            Log("Press a Play button: each one builds an AnimationFlyRequest and calls AnimationFly.Play");
            if (_playOnStart)
            {
                PlayWorldToWallet();
            }
        }

        private void OnDisable()
        {
            RestoreTimeScale();
        }

        public void PlayWorldToWallet()
        {
            AnimationFly.AnimationFlyRequest request = CoinRequest(_worldPickup, AnimationFlyCoordinateSpace.World);
            request.CompletionMode = AnimationFlyCompletionMode.DisableAndPool;
            Play("World pickup -> wallet · World -> Canvas, pooled", request);
        }

        public void PlayUiToUiBurst()
        {
            AnimationFly.AnimationFlyRequest request = GemRequest(_sourceButton, AnimationFlyCoordinateSpace.Canvas);
            request.CompletionMode = AnimationFlyCompletionMode.Destroy;
            request.RewardTiming = AnimationFlyRewardTiming.OnEachArrived;
            Play("Button -> gems · Canvas -> Canvas, reward per item", request);
        }

        public void PlayPrefabPooled()
        {
            AnimationFly.AnimationFlyRequest request = GemRequest(_worldChest, AnimationFlyCoordinateSpace.World);
            request.Sprite = null;
            request.Prefab = _prefabVisual;
            request.UiSizeSource = null;
            request.CompletionMode = AnimationFlyCompletionMode.DisableAndPool;
            Play("Prefab visual (Prefab, not Sprite) · DisableAndPool reuses instances", request);
        }

        public void PlayScreenToUi()
        {
            AnimationFly.AnimationFlyRequest request = CoinRequest(null, AnimationFlyCoordinateSpace.Screen);
            request.StartPosition = new Vector3(Screen.width * 0.5f, Screen.height * 0.35f, 0f);
            request.CompletionMode = AnimationFlyCompletionMode.Destroy;
            Play("Screen point -> wallet · StartPosition in pixels (taps, clicks)", request);
        }

        public void PlaySampleSpriteToUi()
        {
            AnimationFly.AnimationFlyRequest request = GemRequest(_worldChest, AnimationFlyCoordinateSpace.World);
            request.Sprite = _sampleSprite != null ? _sampleSprite : _gemSprite;
            request.CompletionMode = AnimationFlyCompletionMode.DisableAndPool;
            Play("Any Sprite asset -> gems · Sprite = item icon", request);
        }

        public void PlayChestFountain()
        {
            AnimationFly.AnimationFlyRequest request = CoinRequest(_worldChest, AnimationFlyCoordinateSpace.World);
            request.MotionPreset = AnimationFlyMotionPreset.Fountain;
            request.BurstOffset = new Vector3(0f, 220f, 0f);
            request.BurstRandomOffset = new Vector3(170f, 90f, 0f);
            request.BurstDurationRatio = 0.34f;
            request.BurstHoldDuration = 0.08f;
            request.BurstEase = Ease.OutBack;
            request.CruiseEase = Ease.InQuad;
            Play("Chest fountain -> wallet · MotionPreset.Fountain", request);
        }

        public void PlayMagnetToWallet()
        {
            AnimationFly.AnimationFlyRequest request = CoinRequest(_worldPickup, AnimationFlyCoordinateSpace.World);
            request.MotionPreset = AnimationFlyMotionPreset.Magnet;
            request.MagnetDistance = 130f;
            request.MagnetDurationRatio = 0.32f;
            request.CruiseEase = Ease.OutQuad;
            request.MagnetEase = Ease.InCubic;
            request.EndRandomOffset = Vector3.zero;
            Play("Magnet pull -> wallet · MotionPreset.Magnet", request);
        }

        public void PlayFountainMagnet()
        {
            AnimationFly.AnimationFlyRequest request = GemRequest(_worldChest, AnimationFlyCoordinateSpace.World);
            request.Sprite = _sampleSprite != null ? _sampleSprite : _gemSprite;
            request.MotionPreset = AnimationFlyMotionPreset.FountainMagnet;
            request.BurstOffset = new Vector3(0f, 240f, 0f);
            request.BurstRandomOffset = new Vector3(190f, 100f, 0f);
            request.BurstDurationRatio = 0.30f;
            request.BurstHoldDuration = 0.06f;
            request.MagnetDistance = 120f;
            request.MagnetDurationRatio = 0.28f;
            request.BurstEase = Ease.OutBack;
            request.CruiseEase = Ease.InOutQuad;
            request.MagnetEase = Ease.InCubic;
            Play("Fountain + magnet -> gems · MotionPreset.FountainMagnet", request);
        }

        public void PlayScatterBurst()
        {
            RectTransform source = _scatterSource != null ? _scatterSource : _sourceButton;
            AnimationFly.AnimationFlyRequest request = GemRequest(source, AnimationFlyCoordinateSpace.Canvas);
            request.CompletionMode = AnimationFlyCompletionMode.Destroy;
            request.RewardTiming = AnimationFlyRewardTiming.OnEachArrived;
            request.MotionPreset = AnimationFlyMotionPreset.Scatter;
            request.BurstRandomOffset = new Vector3(220f, 150f, 0f);
            request.BurstDurationRatio = 0.22f;
            request.BurstEase = Ease.OutQuad;
            request.CruiseEase = Ease.InOutQuad;
            Play("Scatter burst -> gems · MotionPreset.Scatter", request);
        }

        /// <summary>
        ///     Inspector-list path: flies <see cref="AnimationFly.bonusPrefabList" /> entry 0 (prefab and end point
        ///     configured on the AnimationFly component) from the world chest.
        /// </summary>
        public void PlayByTypeFromList()
        {
            if (!ResolveFly())
            {
                return;
            }

            if (_worldChest == null || _gemTarget == null)
            {
                WarnUnbuilt();
                return;
            }

            int count = CurrentCount;
            _fly.PlayByType(0, count, _worldChest, _gemTarget, AnimationFlyCoordinateSpace.World,
                AnimationFlyCoordinateSpace.Canvas, _onItemStarted, _onGemArrived);
            Log("AnimationFly.PlayByType(0, " + count + ", chest, gems, World, Canvas) · Bonus Prefab List entry 0");
            RefreshLabels();
        }

        public void ResetCounters()
        {
            _rewardCounter = 0;
            _startedCounter = 0;
            _coins = 0;
            _gems = 0;
            RefreshLabels();
            Log("Counters reset");
        }

        /// <summary>Duration slider target: <see cref="AnimationFly.flyDuration" /> in seconds.</summary>
        public void SetDuration(float seconds)
        {
            if (ResolveFly())
            {
                _fly.flyDuration = Mathf.Max(0.001f, seconds);
            }
        }

        /// <summary>Delay slider target: <see cref="AnimationFly.delayBetweenBonuses" /> in seconds.</summary>
        public void SetDelay(float seconds)
        {
            if (ResolveFly())
            {
                _fly.delayBetweenBonuses = Mathf.Max(0f, seconds);
            }
        }

        /// <summary>Arc slider target: <see cref="AnimationFly.arcStrength" /> in spawn-space units (canvas pixels here).</summary>
        public void SetArc(float strength)
        {
            if (ResolveFly())
            {
                _fly.arcStrength = Mathf.Max(0f, strength);
            }
        }

        /// <summary>Scale slider target: <see cref="AnimationFly.scaleMult" />.</summary>
        public void SetScale(float multiplier)
        {
            if (ResolveFly())
            {
                _fly.scaleMult = Mathf.Max(0.01f, multiplier);
            }
        }

        /// <summary>Rotation slider target: degrees turned during one flight (0 disables rotation).</summary>
        public void SetRotation(float degrees)
        {
            if (ResolveFly())
            {
                _fly.rotationDegrees = degrees;
                _fly.rotateDuringFlight = degrees > 0.01f;
            }
        }

        /// <summary>Pause toggle target: freezes scaled time like a pause or shop menu does.</summary>
        public void SetGamePaused(bool paused)
        {
            if (paused)
            {
                if (!_pausedByDemo)
                {
                    _timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
                    _pausedByDemo = true;
                }

                Time.timeScale = 0f;
                Log("Time.timeScale = 0 · scaled tweens stop, unscaled flights keep flying");
            }
            else
            {
                RestoreTimeScale();
                Log("Time.timeScale = " + Time.timeScale.ToString("0.##"));
            }
        }

        /// <summary>Unscaled-time toggle target: <see cref="AnimationFly.useUnscaledTime" />.</summary>
        public void SetUnscaledTime(bool unscaled)
        {
            if (!ResolveFly())
            {
                return;
            }

            _fly.useUnscaledTime = unscaled;
            Log("AnimationFly.useUnscaledTime = " + (unscaled ? "true" : "false") +
                (unscaled ? " · flights ignore Time.timeScale" : " · flights follow Time.timeScale"));
        }

        private AnimationFly.AnimationFlyRequest CoinRequest(Transform start, AnimationFlyCoordinateSpace startSpace)
        {
            return BaseRequest(_coinSprite, start, startSpace, _walletTarget, _onCoinArrived);
        }

        private AnimationFly.AnimationFlyRequest GemRequest(Transform start, AnimationFlyCoordinateSpace startSpace)
        {
            return BaseRequest(_gemSprite, start, startSpace, _gemTarget, _onGemArrived);
        }

        private AnimationFly.AnimationFlyRequest BaseRequest(Sprite sprite, Transform start,
            AnimationFlyCoordinateSpace startSpace, RectTransform end, Action<GameObject> onArrived)
        {
            return new AnimationFly.AnimationFlyRequest
            {
                Sprite = sprite,
                Count = CurrentCount,
                StartTransform = start,
                EndTransform = end,
                Parent = _flyRoot,
                StartSpace = startSpace,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                CompletionMode = AnimationFlyCompletionMode.DisableAndPool,
                RewardTiming = AnimationFlyRewardTiming.OnAllArrived,
                // WHY: flying sprites match the target icon instead of the sprite's pixel size.
                UiSizeSource = end,
                OnItemStarted = _onItemStarted,
                OnItemArrived = onArrived,
                OnReward = _onReward
            };
        }

        private void Play(string label, AnimationFly.AnimationFlyRequest request)
        {
            if (!ResolveFly())
            {
                return;
            }

            if (request.StartTransform == null && request.StartSpace != AnimationFlyCoordinateSpace.Screen)
            {
                WarnUnbuilt();
                return;
            }

            AnimationFly.AnimationFlyResult result = _fly.Play(request);
            Log("Play: " + label + " · " + result.TotalCount + " item(s)");
            RefreshLabels();
        }

        private bool ResolveFly()
        {
            if (_fly == null)
            {
                _fly = AnimationFly.I;
            }

            if (_fly != null)
            {
                return true;
            }

            WarnUnbuilt();
            return false;
        }

        private void ApplyAllTuning()
        {
            if (_durationSlider != null)
            {
                SetDuration(_durationSlider.value);
            }

            if (_delaySlider != null)
            {
                SetDelay(_delaySlider.value);
            }

            if (_arcSlider != null)
            {
                SetArc(_arcSlider.value);
            }

            if (_scaleSlider != null)
            {
                SetScale(_scaleSlider.value);
            }

            if (_rotationSlider != null)
            {
                SetRotation(_rotationSlider.value);
            }
        }

        private void HandleItemStarted(GameObject item)
        {
            _startedCounter++;
            SetText(_startedValue, _startedCounter);
            SetText(_ownedValue, _fly != null ? _fly.OwnedVisualCount : 0);
        }

        private void HandleCoinArrived(GameObject item)
        {
            _coins++;
            SetText(_coinValue, _coins);
            Punch(_walletTarget);
            SetText(_ownedValue, _fly != null ? _fly.OwnedVisualCount : 0);
        }

        private void HandleGemArrived(GameObject item)
        {
            _gems++;
            SetText(_gemValue, _gems);
            Punch(_gemTarget);
            SetText(_ownedValue, _fly != null ? _fly.OwnedVisualCount : 0);
        }

        private void HandleReward()
        {
            _rewardCounter++;
            SetText(_rewardValue, _rewardCounter);
        }

        private void RefreshLabels()
        {
            SetText(_coinValue, _coins);
            SetText(_gemValue, _gems);
            SetText(_rewardValue, _rewardCounter);
            SetText(_startedValue, _startedCounter);
            SetText(_ownedValue, _fly != null ? _fly.OwnedVisualCount : 0);
        }

        private void RestoreTimeScale()
        {
            if (!_pausedByDemo)
            {
                return;
            }

            _pausedByDemo = false;
            Time.timeScale = _timeScaleBeforePause;
        }

        private static void Punch(RectTransform target)
        {
            if (target == null)
            {
                return;
            }

            target.DOKill(true);
            target.DOPunchScale(new Vector3(0.28f, 0.28f, 0f), 0.22f, 6, 0.6f).SetUpdate(true).SetLink(target.gameObject);
        }

        private static void SetText(TMP_Text label, int value)
        {
            if (label != null)
            {
                label.SetText("{0}", value);
            }
        }

        private static int CountAssigned(Button[] buttons)
        {
            if (buttons == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    count++;
                }
            }

            return count;
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
                "[AnimationFlyDemoController] The scene is not authored. Run Neoxider/Demo/Rebuild Active Demo Scene.",
                this);
        }
    }
}
