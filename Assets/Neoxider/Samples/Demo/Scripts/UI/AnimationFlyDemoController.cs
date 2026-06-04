using System;
using Neo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Samples
{
    [AddComponentMenu("Neoxider/Demo/UI/Animation Fly Demo Controller")]
    public sealed class AnimationFlyDemoController : MonoBehaviour
    {
        private const int ButtonHeight = 42;

        [SerializeField] private Sprite _sampleSprite;

        private AnimationFly _fly;
        private Canvas _canvas;
        private RectTransform _flyRoot;
        private RectTransform _walletTarget;
        private RectTransform _gemTarget;
        private RectTransform _sourceButton;
        private Transform _worldPickup;
        private Transform _worldChest;
        private Sprite _coinSprite;
        private Sprite _gemSprite;
        private GameObject _prefabVisual;
        private Text _statusText;
        private Text _rewardText;
        private Text _countValueText;
        private Text _durationValueText;
        private Text _delayValueText;
        private Text _arcValueText;
        private Text _scaleValueText;
        private Text _rotationValueText;
        private Slider _countSlider;
        private Slider _durationSlider;
        private Slider _delaySlider;
        private Slider _arcSlider;
        private Slider _scaleSlider;
        private Slider _rotationSlider;
        private int _rewardCounter;
        private int _startedCounter;

        public int RewardCounter => _rewardCounter;
        public int StartedCounter => _startedCounter;
        public int DemoButtonCount { get; private set; }
        public int DemoSliderCount { get; private set; }

        private void Awake()
        {
            EnsureEventSystem();
            EnsureCamera();
            BuildCanvas();
            BuildWorldTargets();
            BuildFlyService();
            BuildUi();
        }

        private void Start()
        {
            PlayWorldToWallet();
        }

        public void PlayWorldToWallet()
        {
            ApplySliderSettings();
            PlayRequest("World pickup -> UI wallet", new AnimationFly.AnimationFlyRequest
            {
                Sprite = _coinSprite,
                Count = CurrentCount,
                StartTransform = _worldPickup,
                EndTransform = _walletTarget,
                Parent = _flyRoot,
                StartSpace = AnimationFlyCoordinateSpace.World,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                CompletionMode = AnimationFlyCompletionMode.DisableAndPool,
                RewardTiming = AnimationFlyRewardTiming.OnAllArrived,
                OnReward = AddReward,
                OnItemStarted = _ => _startedCounter++
            });
        }

        public void PlayUiToUiBurst()
        {
            ApplySliderSettings();
            PlayRequest("UI button -> gem counter", new AnimationFly.AnimationFlyRequest
            {
                Sprite = _gemSprite,
                Count = CurrentCount,
                StartTransform = _sourceButton,
                EndTransform = _gemTarget,
                Parent = _flyRoot,
                StartSpace = AnimationFlyCoordinateSpace.Canvas,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                CompletionMode = AnimationFlyCompletionMode.Destroy,
                RewardTiming = AnimationFlyRewardTiming.OnEachArrived,
                OnReward = AddReward,
                OnItemStarted = _ => _startedCounter++
            });
        }

        public void PlayPrefabPooled()
        {
            ApplySliderSettings();
            PlayRequest("Prefab visual with pooling", new AnimationFly.AnimationFlyRequest
            {
                Prefab = _prefabVisual,
                Count = CurrentCount,
                StartTransform = _worldChest,
                EndTransform = _walletTarget,
                Parent = _flyRoot,
                StartSpace = AnimationFlyCoordinateSpace.World,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                CompletionMode = AnimationFlyCompletionMode.DisableAndPool,
                RewardTiming = AnimationFlyRewardTiming.OnAllArrived,
                OnReward = AddReward,
                OnItemStarted = _ => _startedCounter++
            });
        }

        public void PlayScreenToUi()
        {
            ApplySliderSettings();
            Vector3 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.35f, 0f);
            PlayRequest("Screen point -> UI wallet", new AnimationFly.AnimationFlyRequest
            {
                Sprite = _coinSprite,
                Count = CurrentCount,
                StartPosition = screenCenter,
                EndTransform = _walletTarget,
                Parent = _flyRoot,
                StartSpace = AnimationFlyCoordinateSpace.Screen,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                CompletionMode = AnimationFlyCompletionMode.Destroy,
                RewardTiming = AnimationFlyRewardTiming.OnAllArrived,
                OnReward = AddReward,
                OnItemStarted = _ => _startedCounter++
            });
        }

        public void PlaySampleSpriteToUi()
        {
            ApplySliderSettings();
            PlayRequest("Demo sprite asset -> gems", new AnimationFly.AnimationFlyRequest
            {
                Sprite = _sampleSprite != null ? _sampleSprite : _gemSprite,
                Count = CurrentCount,
                StartTransform = _worldChest,
                EndTransform = _gemTarget,
                Parent = _flyRoot,
                StartSpace = AnimationFlyCoordinateSpace.World,
                EndSpace = AnimationFlyCoordinateSpace.Canvas,
                SpawnSpace = AnimationFlySpawnSpace.Canvas,
                CompletionMode = AnimationFlyCompletionMode.DisableAndPool,
                RewardTiming = AnimationFlyRewardTiming.OnAllArrived,
                OnReward = AddReward,
                OnItemStarted = _ => _startedCounter++
            });
        }

        public void ResetCounters()
        {
            _rewardCounter = 0;
            _startedCounter = 0;
            RefreshLabels("Counters reset.");
        }

        private void PlayRequest(string label, AnimationFly.AnimationFlyRequest request)
        {
            AnimationFly.AnimationFlyResult result = _fly.Play(request);
            RefreshLabels($"{label}: started {result.TotalCount} item(s).");
        }

        private void AddReward()
        {
            _rewardCounter++;
            RefreshLabels("Reward callback fired.");
        }

        private void RefreshLabels(string message)
        {
            RefreshSliderLabels();
            if (_statusText != null)
            {
                _statusText.text = message;
            }

            if (_rewardText != null)
            {
                _rewardText.text = $"Rewards: {_rewardCounter}   Started visuals: {_startedCounter}";
            }
        }

        private void BuildFlyService()
        {
            _fly = GetComponent<AnimationFly>();
            if (_fly == null)
            {
                _fly = gameObject.AddComponent<AnimationFly>();
            }

            _fly.parentCanvas = _canvas;
            _fly.spawnParent = _flyRoot;
            _fly.animationCamera = Camera.main;
            _fly.spawnSpace = AnimationFlySpawnSpace.Canvas;
            _fly.defaultStartSpace = AnimationFlyCoordinateSpace.Auto;
            _fly.defaultEndSpace = AnimationFlyCoordinateSpace.Auto;
            _fly.defaultCompletionMode = AnimationFlyCompletionMode.DisableAndPool;
            _fly.flyDuration = 0.65f;
            _fly.delayBetweenBonuses = 0.035f;
            _fly.startRandomOffset = new Vector3(0.2f, 0.2f, 0f);
            _fly.endRandomOffset = new Vector3(0.08f, 0.08f, 0f);
            _fly.middleRandomOffset = new Vector3(0.15f, 0.35f, 0f);
            _fly.rotateDuringFlight = true;
            _fly.rotationDegrees = 220f;
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new("AnimationFlyDemoCanvas");
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            _flyRoot = CreatePanel(canvasObject.transform, "FlyRoot", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0f));
            _flyRoot.SetAsLastSibling();
        }

        private void BuildUi()
        {
            _coinSprite = CreateCircleSprite("AnimationFlyCoinSprite", new Color(1f, 0.78f, 0.12f), 96);
            _gemSprite = CreateDiamondSprite("AnimationFlyGemSprite", new Color(0.2f, 0.8f, 1f), 96);
            _prefabVisual = CreateFlyPrefab();

            RectTransform panel = CreatePanel(_canvas.transform, "Controls", new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Color(0.08f, 0.09f, 0.11f, 0.88f));
            panel.sizeDelta = new Vector2(460f, 610f);

            CreateText(panel, "Title", "AnimationFly Demo", 24, TextAnchor.MiddleLeft, new Vector2(18f, -20f),
                new Vector2(386f, 36f));
            _statusText = CreateText(panel, "Status", "Press a button to run a fly effect.", 15, TextAnchor.UpperLeft,
                new Vector2(18f, -58f), new Vector2(386f, 44f));
            _rewardText = CreateText(panel, "Rewards", "Rewards: 0   Started visuals: 0", 15, TextAnchor.MiddleLeft,
                new Vector2(18f, -102f), new Vector2(386f, 28f));

            _countSlider = CreateSlider(panel, "Count", new Vector2(18f, -140f), 1f, 30f, 8f, true,
                out _countValueText);
            _durationSlider = CreateSlider(panel, "Duration", new Vector2(18f, -188f), 0.15f, 2f, 0.65f, false,
                out _durationValueText);
            _delaySlider = CreateSlider(panel, "Delay", new Vector2(18f, -236f), 0f, 0.2f, 0.035f, false,
                out _delayValueText);
            _arcSlider = CreateSlider(panel, "Arc", new Vector2(18f, -284f), 0f, 4f, 2f, false,
                out _arcValueText);
            _scaleSlider = CreateSlider(panel, "Scale", new Vector2(18f, -332f), 0.5f, 2.5f, 1f, false,
                out _scaleValueText);
            _rotationSlider = CreateSlider(panel, "Rotation", new Vector2(18f, -380f), 0f, 720f, 220f, false,
                out _rotationValueText);

            _sourceButton = CreateButton(panel, "World -> Wallet", new Vector2(18f, -438f), PlayWorldToWallet);
            CreateButton(panel, "UI Burst -> Gems", new Vector2(18f, -486f), PlayUiToUiBurst);
            CreateButton(panel, "Pooled Prefab", new Vector2(18f, -534f), PlayPrefabPooled);
            CreateButton(panel, "Screen Point", new Vector2(216f, -438f), PlayScreenToUi);
            CreateButton(panel, "Reset", new Vector2(216f, -486f), ResetCounters);
            CreateButton(panel, "Sample Sprite", new Vector2(216f, -534f), PlaySampleSpriteToUi);
            DemoButtonCount = 6;
            DemoSliderCount = 6;
            RefreshSliderLabels();

            _walletTarget = CreateCounter(_canvas.transform, "Wallet", "Coins", _coinSprite, new Vector2(-170f, -60f));
            _gemTarget = CreateCounter(_canvas.transform, "GemCounter", "Gems", _gemSprite, new Vector2(-170f, -136f));
            _flyRoot.SetAsLastSibling();
        }

        private void BuildWorldTargets()
        {
            _worldPickup = CreateWorldMarker("World Pickup", new Vector3(-2.5f, -1.3f, 0f), new Color(1f, 0.74f, 0.12f));
            _worldChest = CreateWorldMarker("World Chest", new Vector3(2.2f, -1f, 0f), new Color(0.65f, 0.38f, 0.12f));
        }

        private Transform CreateWorldMarker(string name, Vector3 position, Color color)
        {
            GameObject marker = new(name);
            marker.transform.SetParent(transform, false);
            marker.transform.position = position;
            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateCircleSprite(name + "Sprite", color, 64);
            renderer.sortingOrder = 2;
            marker.transform.localScale = Vector3.one * 0.8f;
            return marker.transform;
        }

        private RectTransform CreateCounter(Transform parent, string name, string label, Sprite sprite, Vector2 anchored)
        {
            RectTransform counter = CreatePanel(parent, name, new Vector2(1f, 1f), anchored, new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Color(0.1f, 0.13f, 0.18f, 0.92f));
            counter.sizeDelta = new Vector2(150f, 58f);

            GameObject icon = new(name + "Icon");
            icon.transform.SetParent(counter, false);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(28f, 0f);
            iconRect.sizeDelta = new Vector2(36f, 36f);
            Image image = icon.AddComponent<Image>();
            image.sprite = sprite;

            CreateText(counter, name + "Label", label, 18, TextAnchor.MiddleLeft, new Vector2(54f, 0f),
                new Vector2(92f, 42f), new Vector2(0f, 0.5f));
            return counter;
        }

        private RectTransform CreateButton(Transform parent, string label, Vector2 anchored, Action action)
        {
            GameObject buttonObject = new(label + " Button");
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(180f, ButtonHeight);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.33f, 0.68f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => action?.Invoke());
            CreateText(rect, label + " Text", label, 15, TextAnchor.MiddleCenter, Vector2.zero,
                new Vector2(176f, ButtonHeight));
            return rect;
        }

        private Slider CreateSlider(Transform parent, string label, Vector2 anchored, float min, float max, float value,
            bool wholeNumbers, out Text valueText)
        {
            RectTransform row = CreatePanel(parent, label + " Slider Row", new Vector2(0f, 1f), anchored,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Color(0f, 0f, 0f, 0f));
            row.sizeDelta = new Vector2(420f, 42f);

            CreateText(row, label + " Label", label, 14, TextAnchor.MiddleLeft, Vector2.zero,
                new Vector2(100f, 28f));
            valueText = CreateText(row, label + " Value", string.Empty, 14, TextAnchor.MiddleRight,
                new Vector2(332f, 0f), new Vector2(82f, 28f));

            GameObject sliderObject = new(label + " Slider");
            sliderObject.transform.SetParent(row, false);
            RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
            sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0f, 1f);
            sliderRect.pivot = new Vector2(0f, 1f);
            sliderRect.anchoredPosition = new Vector2(104f, -6f);
            sliderRect.sizeDelta = new Vector2(220f, 24f);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.wholeNumbers = wholeNumbers;

            RectTransform background = CreateSliderImage(sliderRect, "Background", new Color(0.2f, 0.22f, 0.26f, 1f));
            background.anchorMin = new Vector2(0f, 0.25f);
            background.anchorMax = new Vector2(1f, 0.75f);
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;

            RectTransform fillArea = CreateSliderRect(sliderRect, "Fill Area");
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.offsetMin = Vector2.zero;
            fillArea.offsetMax = Vector2.zero;
            RectTransform fill = CreateSliderImage(fillArea, "Fill", new Color(0.22f, 0.55f, 1f, 1f));
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;

            RectTransform handleArea = CreateSliderRect(sliderRect, "Handle Slide Area");
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);
            RectTransform handle = CreateSliderImage(handleArea, "Handle", new Color(1f, 1f, 1f, 1f));
            handle.sizeDelta = new Vector2(18f, 18f);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.onValueChanged.AddListener(_ => RefreshSliderLabels());
            return slider;
        }

        private static RectTransform CreateSliderRect(Transform parent, string name)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private static RectTransform CreateSliderImage(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateSliderRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private RectTransform CreatePanel(Transform parent, string name, Vector2 pivot, Vector2 anchored,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject panel = new(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchored;
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private Text CreateText(Transform parent, string name, string text, int size, TextAnchor alignment,
            Vector2 anchored, Vector2 sizeDelta)
        {
            return CreateText(parent, name, text, size, alignment, anchored, sizeDelta, new Vector2(0f, 1f));
        }

        private Text CreateText(Transform parent, string name, string text, int size, TextAnchor alignment,
            Vector2 anchored, Vector2 sizeDelta, Vector2 anchor)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchored;
            rect.sizeDelta = sizeDelta;
            Text label = textObject.AddComponent<Text>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return label;
        }

        private GameObject CreateFlyPrefab()
        {
            GameObject prefab = new("AnimationFlyDemoPrefabVisual");
            prefab.SetActive(false);
            RectTransform rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(34f, 34f);
            Image image = prefab.AddComponent<Image>();
            image.sprite = _gemSprite;
            image.color = Color.white;
            prefab.transform.SetParent(transform, false);
            return prefab;
        }

        private static Sprite CreateCircleSprite(string name, Color color, int size)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.42f;
            float outlineRadius = size * 0.47f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    Color pixel = Color.clear;
                    if (distance <= outlineRadius)
                    {
                        pixel = distance <= radius ? color : new Color(1f, 1f, 1f, 0.85f);
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            texture.name = name + "Texture";
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = name;
            return sprite;
        }

        private static Sprite CreateDiamondSprite(string name, Color color, int size)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.46f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float diamond = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                    Color pixel = diamond <= radius ? color : Color.clear;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            texture.name = name + "Texture";
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = name;
            return sprite;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
        }

        private int CurrentCount => _countSlider != null ? Mathf.RoundToInt(_countSlider.value) : 8;

        private void ApplySliderSettings()
        {
            if (_fly == null)
            {
                return;
            }

            _fly.flyDuration = _durationSlider != null ? _durationSlider.value : 0.65f;
            _fly.delayBetweenBonuses = _delaySlider != null ? _delaySlider.value : 0.035f;
            _fly.arcStrength = _arcSlider != null ? _arcSlider.value : 2f;
            _fly.scaleMult = _scaleSlider != null ? _scaleSlider.value : 1f;
            _fly.rotationDegrees = _rotationSlider != null ? _rotationSlider.value : 220f;
            _fly.rotateDuringFlight = _fly.rotationDegrees > 0.01f;
        }

        private void RefreshSliderLabels()
        {
            SetSliderText(_countValueText, CurrentCount.ToString());
            SetSliderText(_durationValueText, FormatFloat(_durationSlider, "s"));
            SetSliderText(_delayValueText, FormatFloat(_delaySlider, "s"));
            SetSliderText(_arcValueText, FormatFloat(_arcSlider, string.Empty));
            SetSliderText(_scaleValueText, FormatFloat(_scaleSlider, "x"));
            SetSliderText(_rotationValueText, FormatFloat(_rotationSlider, "deg"));
        }

        private static string FormatFloat(Slider slider, string suffix)
        {
            if (slider == null)
            {
                return "-";
            }

            return string.IsNullOrEmpty(suffix)
                ? slider.value.ToString("0.##")
                : slider.value.ToString("0.##") + " " + suffix;
        }

        private static void SetSliderText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
