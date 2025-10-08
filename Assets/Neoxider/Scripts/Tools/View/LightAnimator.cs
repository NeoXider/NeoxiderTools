using UnityEngine;
using System;
using System.Reflection;
using System.Collections;

public class LightAnimator : MonoBehaviour
{
    public enum AnimationType
    {
        RandomFlicker,
        Pulsing,
        SmoothTransition,
        PerlinNoise,
        SinWave,
    }

    // Универсальный аксессор под Light или Light2D
    private interface ILightAccessor
    {
        float Intensity { get; set; }
        Color Color { get; set; }
        string ImplName { get; }
    }

    private sealed class UnityLightAccessor : ILightAccessor
    {
        private readonly Light _light;
        public UnityLightAccessor(Light l) => _light = l;
        public float Intensity { get => _light.intensity; set => _light.intensity = value; }
        public Color Color { get => _light.color; set => _light.color = value; }
        public string ImplName => "Light";
    }

    private sealed class URPLight2DAccessor : ILightAccessor
    {
        private readonly Component _light2D;
        private readonly Func<Component, float> _getIntensity;
        private readonly Action<Component, float> _setIntensity;
        private readonly Func<Component, Color> _getColor;
        private readonly Action<Component, Color> _setColor;

        public URPLight2DAccessor(Component comp, PropertyInfo pIntensity, PropertyInfo pColor)
        {
            _light2D = comp;

            var getI = pIntensity.GetGetMethod();
            var setI = pIntensity.GetSetMethod();
            var getC = pColor.GetGetMethod();
            var setC = pColor.GetSetMethod();

            // Кэшируем делегаты (быстро работать в рантайме)
            _getIntensity = (Func<Component, float>)Delegate.CreateDelegate(typeof(Func<Component, float>), null, getI);
            _setIntensity = (Action<Component, float>)Delegate.CreateDelegate(typeof(Action<Component, float>), null, setI);
            _getColor     = (Func<Component, Color>)Delegate.CreateDelegate(typeof(Func<Component, Color>), null, getC);
            _setColor     = (Action<Component, Color>)Delegate.CreateDelegate(typeof(Action<Component, Color>), null, setC);
        }

        public float Intensity { get => _getIntensity(_light2D); set => _setIntensity(_light2D, value); }
        public Color Color { get => _getColor(_light2D); set => _setColor(_light2D, value); }
        public string ImplName => "Light2D";
    }

    private ILightAccessor _light;

    [Header("Animation Settings")]
    public AnimationType animationType = AnimationType.PerlinNoise;

    [Header("Intensity Settings")]
    [Range(0f, 10f)] public float minIntensity = 0.5f;
    [Range(0f, 10f)] public float maxIntensity = 1.5f;
    [Range(0.1f, 10f)] public float animationSpeed = 1.0f;

    [Header("Noise Settings")]
    [Range(0.1f, 10f)] public float noiseScale = 1f;
    public bool use2DNoise = true;
    public Vector2 noiseOffset;

    [Header("Color Settings")]
    public bool changeColor = false;
    public Color targetColor = Color.white;
    [Range(0f, 1f)] public float colorBlendSpeed = 1f;

    [Header("Debug Settings")]
    public bool enableDebugging = false;

    private float originalIntensity;
    private Color originalColor;
    private float animationTime;
    private Vector2 randomOffset;

    void Awake()
    {
        // 1) Попробуем Light2D через reflection (URP может быть не установлен — тогда тип будет null)
        // Полное имя типа и сборки у Light2D:
        // "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime"
        Type light2DType = Type.GetType(
            "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime",
            throwOnError: false
        );

        if (light2DType != null)
        {
            var comp = GetComponent(light2DType);
            if (comp != null)
            {
                var pIntensity = light2DType.GetProperty("intensity", BindingFlags.Instance | BindingFlags.Public);
                var pColor     = light2DType.GetProperty("color", BindingFlags.Instance | BindingFlags.Public);

                if (pIntensity != null && pIntensity.CanRead && pIntensity.CanWrite &&
                    pColor != null && pColor.CanRead && pColor.CanWrite)
                {
                    _light = new URPLight2DAccessor(comp, pIntensity, pColor);
                }
            }
        }

        // 2) Если Light2D не найден — обычный Light
        if (_light == null)
        {
            var l = GetComponent<Light>();
            if (l != null)
                _light = new UnityLightAccessor(l);
        }

        if (_light != null)
        {
            originalIntensity = _light.Intensity;
            originalColor = _light.Color;

            if (enableDebugging)
                Debug.Log($"[{gameObject.name}] Using {_light.ImplName}");
        }
        else
        {
            Debug.LogError("No Light or Light2D component found on this GameObject!", this);
            enabled = false;
        }
    }

    void Start()
    {
        if (_light == null) return;

        animationTime = UnityEngine.Random.Range(0f, 1000f);
        randomOffset = new Vector2(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );
        StartCoroutine(AnimateLight());
    }

    private float GetPerlinNoiseValue()
    {
        if (use2DNoise)
        {
            float x = ((animationTime * animationSpeed) + randomOffset.x + noiseOffset.x) * noiseScale;
            float y = (randomOffset.y + noiseOffset.y) * noiseScale;
            return Mathf.PerlinNoise(x, y);
        }
        else
        {
            return Mathf.PerlinNoise(((animationTime * animationSpeed) + randomOffset.x + noiseOffset.x) * noiseScale, 0f);
        }
    }

    private IEnumerator AnimateLight()
    {
        var waitForEndOfFrame = new WaitForEndOfFrame();

        while (true)
        {
            animationTime += Time.deltaTime;

            float targetIntensity = minIntensity;

            switch (animationType)
            {
                case AnimationType.RandomFlicker:
                    targetIntensity = UnityEngine.Random.Range(minIntensity, maxIntensity);
                    break;

                case AnimationType.Pulsing:
                    {
                        float pulseValue = (Mathf.Sin(animationTime * animationSpeed * Mathf.PI) + 1f) * 0.5f;
                        targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, pulseValue);
                    }
                    break;

                case AnimationType.SmoothTransition:
                    targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, Mathf.PingPong(animationTime * animationSpeed, 1));
                    break;

                case AnimationType.PerlinNoise:
                    targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, GetPerlinNoiseValue());
                    break;

                case AnimationType.SinWave:
                    {
                        float wave = (Mathf.Sin(animationTime * animationSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                        targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, wave);
                    }
                    break;
            }

            _light.Intensity = targetIntensity;

            if (changeColor)
            {
                float colorT = Mathf.PingPong(animationTime * animationSpeed * colorBlendSpeed, 1f);
                _light.Color = Color.Lerp(originalColor, targetColor, colorT);
            }

            if (enableDebugging)
            {
                // Безопасно вызываем шум только при необходимости, чтобы не считать дважды
                float dbgNoise = (animationType == AnimationType.PerlinNoise) ? GetPerlinNoiseValue() : 0f;
                Debug.Log($"[{gameObject.name}] Impl: {_light.ImplName}, Intensity: {_light.Intensity:F2}, Time: {animationTime:F2}, Speed: {animationSpeed:F2}, Noise: {dbgNoise:F2}");
            }

            yield return waitForEndOfFrame;
        }
    }

    private void OnDisable()
    {
        if (_light != null)
        {
            _light.Intensity = originalIntensity;
            _light.Color = originalColor;
        }
    }

    private void OnValidate()
    {
        if (maxIntensity < minIntensity)
            maxIntensity = minIntensity;
    }
}
