using UnityEngine;

namespace Neo.Animations
{
    /// <summary>
    ///     Static helper methods for animating various components. Provides universal functions to compute animated
    ///     values over time.
    /// </summary>
    public static class AnimationUtils
    {
        // -------------------------------------------------
        // 1) Перлин‑шум
        // -------------------------------------------------

        /// <summary>Gets a Perlin noise value for animation.</summary>
        /// <param name="animationTime">Current animation time.</param>
        /// <param name="speed">Animation speed.</param>
        /// <param name="randomOffset">Random offset for uniqueness.</param>
        /// <param name="noiseOffset">Additional noise offset.</param>
        /// <param name="noiseScale">Noise scale.</param>
        /// <param name="use2DNoise">Use 2D noise instead of 1D.</param>
        /// <returns>Noise value between 0 and 1.</returns>
        public static float GetPerlinNoiseValue(
            float animationTime,
            float speed,
            Vector2 randomOffset,
            Vector2 noiseOffset,
            float noiseScale,
            bool use2DNoise)
        {
            if (use2DNoise)
            {
                float x = (animationTime * speed + randomOffset.x + noiseOffset.x) * noiseScale;
                float y = (randomOffset.y + noiseOffset.y) * noiseScale;
                return Mathf.PerlinNoise(x, y);
            }

            return Mathf.PerlinNoise((animationTime * speed + randomOffset.x + noiseOffset.x) * noiseScale,
                0f);
        }

        // -------------------------------------------------
        // 2) Целевое значение по типу анимации
        // -------------------------------------------------

        /// <summary>Gets the target animation value based on type and time.</summary>
        /// <param name="type">Animation type.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <param name="animationTime">Current animation time.</param>
        /// <param name="speed">Animation speed (if 0, returns min).</param>
        /// <param name="use2DNoise">Use 2D noise for PerlinNoise.</param>
        /// <param name="randomOffset">Random offset.</param>
        /// <param name="noiseOffset">Noise offset.</param>
        /// <param name="noiseScale">Noise scale.</param>
        /// <param name="customCurve">Custom curve for CustomCurve type.</param>
        /// <returns>Animated value between min and max.</returns>
        public static float GetTargetValue(
            AnimationType type,
            float min,
            float max,
            float animationTime,
            float speed,
            bool use2DNoise,
            Vector2 randomOffset,
            Vector2 noiseOffset,
            float noiseScale,
            AnimationCurve customCurve = null)
        {
            // Если скорость равна 0, возвращаем минимальное значение
            if (speed <= 0f)
            {
                return min;
            }

            switch (type)
            {
                case AnimationType.RandomFlicker:
                    return Random.Range(min, max);

                case AnimationType.Pulsing:
                    float pulse = (Mathf.Sin(animationTime * speed * Mathf.PI) + 1f) * 0.5f;
                    return Mathf.Lerp(min, max, pulse);

                case AnimationType.SmoothTransition:
                    return Mathf.Lerp(min, max,
                        Mathf.PingPong(animationTime * speed, 1));

                case AnimationType.PerlinNoise:
                    return Mathf.Lerp(min, max,
                        GetPerlinNoiseValue(animationTime, speed, randomOffset,
                            noiseOffset, noiseScale, use2DNoise));

                case AnimationType.SinWave:
                    float wave = (Mathf.Sin(animationTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;
                    return Mathf.Lerp(min, max, wave);

                case AnimationType.Exponential:
                    float exp = Mathf.Exp(-animationTime * speed * 0.1f);
                    return Mathf.Lerp(min, max, exp);

                case AnimationType.BounceEase:
                    float bounce = Mathf.Abs(Mathf.Sin(animationTime * speed * Mathf.PI * 2f)) *
                                   Mathf.Exp(-animationTime * speed * 0.2f);
                    return Mathf.Lerp(min, max, bounce);

                case AnimationType.ElasticEase:
                    float elastic = Mathf.Sin(animationTime * speed * Mathf.PI * 3f) *
                                    Mathf.Exp(-animationTime * speed * 0.15f);
                    float normalizedElastic = (elastic + 1f) * 0.5f;
                    return Mathf.Lerp(min, max, normalizedElastic);

                case AnimationType.CustomCurve:
                    if (customCurve != null)
                    {
                        float curveValue = customCurve.Evaluate(animationTime * speed);
                        return Mathf.Lerp(min, max, curveValue);
                    }

                    return min; // fallback если кривая не задана

                default:
                    return min; // безопасный fallback
            }
        }

        // -------------------------------------------------
        // 3) Фактор смешивания цвета
        // -------------------------------------------------

        /// <summary>Gets color blend factor for smooth transition between colors.</summary>
        /// <param name="animationTime">Current animation time.</param>
        /// <param name="speed">Animation speed.</param>
        /// <param name="blendSpeed">Color blend speed.</param>
        /// <returns>Blend factor from 0 to 1.</returns>
        public static float GetColorBlendFactor(
            float animationTime,
            float speed,
            float blendSpeed)
        {
            return Mathf.PingPong(animationTime * speed * blendSpeed, 1f);
        }

        // -------------------------------------------------
        // 4) Применение результата к Light (ILightAccessor)
        // -------------------------------------------------

        /// <summary>Applies animated values to a light source.</summary>
        /// <param name="accessor">Light accessor interface.</param>
        /// <param name="targetIntensity">Target intensity.</param>
        /// <param name="originalColor">Original color.</param>
        /// <param name="changeColor">Whether to change color.</param>
        /// <param name="targetColor">Target color.</param>
        /// <param name="colorBlendFactor">Color blend factor.</param>
        public static void ApplyToLight(
            ILightAccessor accessor,
            float targetIntensity,
            Color originalColor,
            bool changeColor,
            Color targetColor,
            float colorBlendFactor)
        {
            accessor.Intensity = targetIntensity;
            if (changeColor)
            {
                accessor.Color = Color.Lerp(originalColor, targetColor, colorBlendFactor);
            }
        }

        // -------------------------------------------------
        // 5) Применение результата к MeshRenderer
        // -------------------------------------------------

        /// <summary>Applies animated values to mesh material for emission.</summary>
        /// <param name="mat">Mesh material.</param>
        /// <param name="targetIntensity">Target intensity.</param>
        /// <param name="originalEmission">Original emission color.</param>
        /// <param name="changeColor">Whether to change color.</param>
        /// <param name="targetColor">Target color.</param>
        /// <param name="colorBlendFactor">Color blend factor.</param>
        public static void ApplyToMesh(
            Material mat,
            float targetIntensity,
            Color originalEmission,
            bool changeColor,
            Color targetColor,
            float colorBlendFactor)
        {
            Color emission = originalEmission;
            if (changeColor)
            {
                emission = Color.Lerp(originalEmission, targetColor, colorBlendFactor);
            }

            // Множим на интенсивность
            Color final = emission * targetIntensity;

            mat.SetColor("_EmissionColor", final);
        }

        // -------------------------------------------------
        // 6) Универсальные методы для любых значений
        // -------------------------------------------------

        /// <summary>Gets an animated float value.</summary>
        /// <param name="type">Animation type.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <param name="animationTime">Current animation time.</param>
        /// <param name="speed">Animation speed.</param>
        /// <param name="customCurve">Custom curve for CustomCurve type.</param>
        /// <returns>Animated value.</returns>
        public static float GetAnimatedFloat(
            AnimationType type,
            float min,
            float max,
            float animationTime,
            float speed,
            AnimationCurve customCurve = null)
        {
            return GetTargetValue(type, min, max, animationTime, speed,
                false, Vector2.zero, Vector2.zero, 1f, customCurve);
        }

        /// <summary>Gets an animated color by interpolating between two colors.</summary>
        /// <param name="type">Animation type.</param>
        /// <param name="colorA">First color.</param>
        /// <param name="colorB">Second color.</param>
        /// <param name="animationTime">Current animation time.</param>
        /// <param name="speed">Animation speed.</param>
        /// <param name="customCurve">Custom curve for CustomCurve type.</param>
        /// <returns>Animated color.</returns>
        public static Color GetAnimatedColor(
            AnimationType type,
            Color colorA,
            Color colorB,
            float animationTime,
            float speed,
            AnimationCurve customCurve = null)
        {
            float factor = GetAnimatedFloat(type, 0f, 1f, animationTime, speed, customCurve);
            return Color.Lerp(colorA, colorB, factor);
        }

        /// <summary>Gets an animated Vector3 by interpolating between two vectors.</summary>
        /// <param name="type">Animation type.</param>
        /// <param name="vectorA">First vector.</param>
        /// <param name="vectorB">Second vector.</param>
        /// <param name="animationTime">Current animation time.</param>
        /// <param name="speed">Animation speed.</param>
        /// <param name="customCurve">Custom curve for CustomCurve type.</param>
        /// <returns>Animated vector.</returns>
        public static Vector3 GetAnimatedVector3(
            AnimationType type,
            Vector3 vectorA,
            Vector3 vectorB,
            float animationTime,
            float speed,
            AnimationCurve customCurve = null)
        {
            float factor = GetAnimatedFloat(type, 0f, 1f, animationTime, speed, customCurve);
            return Vector3.Lerp(vectorA, vectorB, factor);
        }
    }

    /// <summary>Interface for different light source types (Light, Light2D).</summary>
    public interface ILightAccessor
    {
        /// <summary>Light intensity.</summary>
        float Intensity { get; set; }

        /// <summary>Light color.</summary>
        Color Color { get; set; }

        /// <summary>Implementation name (for debugging).</summary>
        string ImplName { get; }
    }
}
