using UnityEngine;
using System.Collections;

namespace Neo.Animations
{
    /// <summary>
    /// Статические вспомогательные методы для анимации различных компонентов.
    /// Предоставляет универсальные функции расчета анимированных значений по времени.
    /// </summary>
    public static class AnimationUtils
    {
        // -------------------------------------------------
        // 1) Перлин‑шум
        // -------------------------------------------------
        
        /// <summary>
        /// Получает значение шума Перлина для анимации.
        /// </summary>
        /// <param name="animationTime">Текущее время анимации</param>
        /// <param name="speed">Скорость анимации</param>
        /// <param name="randomOffset">Случайное смещение для уникальности</param>
        /// <param name="noiseOffset">Дополнительное смещение шума</param>
        /// <param name="noiseScale">Масштаб шума</param>
        /// <param name="use2DNoise">Использовать 2D шум вместо 1D</param>
        /// <returns>Значение шума от 0 до 1</returns>
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
                float x = ((animationTime * speed) + randomOffset.x + noiseOffset.x) * noiseScale;
                float y = (randomOffset.y + noiseOffset.y) * noiseScale;
                return Mathf.PerlinNoise(x, y);
            }
            else
            {
                return Mathf.PerlinNoise(((animationTime * speed) + randomOffset.x + noiseOffset.x) * noiseScale,
                                         0f);
            }
        }

        // -------------------------------------------------
        // 2) Целевое значение по типу анимации
        // -------------------------------------------------
        
        /// <summary>
        /// Получает целевое значение анимации на основе типа и времени.
        /// </summary>
        /// <param name="type">Тип анимации</param>
        /// <param name="min">Минимальное значение</param>
        /// <param name="max">Максимальное значение</param>
        /// <param name="animationTime">Текущее время анимации</param>
        /// <param name="speed">Скорость анимации (если 0, возвращает min)</param>
        /// <param name="use2DNoise">Использовать 2D шум для PerlinNoise</param>
        /// <param name="randomOffset">Случайное смещение</param>
        /// <param name="noiseOffset">Смещение шума</param>
        /// <param name="noiseScale">Масштаб шума</param>
        /// <param name="customCurve">Пользовательская кривая для CustomCurve типа</param>
        /// <returns>Анимированное значение между min и max</returns>
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
                return min;

            switch (type)
            {
                case AnimationType.RandomFlicker:
                    return UnityEngine.Random.Range(min, max);

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
        
        /// <summary>
        /// Получает фактор смешивания цвета для плавного перехода между цветами.
        /// </summary>
        /// <param name="animationTime">Текущее время анимации</param>
        /// <param name="speed">Скорость анимации</param>
        /// <param name="blendSpeed">Скорость смешивания цветов</param>
        /// <returns>Фактор смешивания от 0 до 1</returns>
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
        
        /// <summary>
        /// Применяет анимированные значения к источнику света.
        /// </summary>
        /// <param name="accessor">Интерфейс доступа к свету</param>
        /// <param name="targetIntensity">Целевая интенсивность</param>
        /// <param name="originalColor">Исходный цвет</param>
        /// <param name="changeColor">Изменять ли цвет</param>
        /// <param name="targetColor">Целевой цвет</param>
        /// <param name="colorBlendFactor">Фактор смешивания цвета</param>
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
                accessor.Color = Color.Lerp(originalColor, targetColor, colorBlendFactor);
        }

        // -------------------------------------------------
        // 5) Применение результата к MeshRenderer
        // -------------------------------------------------
        
        /// <summary>
        /// Применяет анимированные значения к материалу меша для эмиссии.
        /// </summary>
        /// <param name="mat">Материал меша</param>
        /// <param name="targetIntensity">Целевая интенсивность</param>
        /// <param name="originalEmission">Исходный цвет эмиссии</param>
        /// <param name="changeColor">Изменять ли цвет</param>
        /// <param name="targetColor">Целевой цвет</param>
        /// <param name="colorBlendFactor">Фактор смешивания цвета</param>
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
                emission = Color.Lerp(originalEmission, targetColor, colorBlendFactor);

            // Множим на интенсивность
            Color final = emission * targetIntensity;

            mat.SetColor("_EmissionColor", final);
        }

        // -------------------------------------------------
        // 6) Универсальные методы для любых значений
        // -------------------------------------------------
        
        /// <summary>
        /// Получает анимированное float значение.
        /// </summary>
        /// <param name="type">Тип анимации</param>
        /// <param name="min">Минимальное значение</param>
        /// <param name="max">Максимальное значение</param>
        /// <param name="animationTime">Текущее время анимации</param>
        /// <param name="speed">Скорость анимации</param>
        /// <param name="customCurve">Пользовательская кривая для CustomCurve</param>
        /// <returns>Анимированное значение</returns>
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

        /// <summary>
        /// Получает анимированный цвет путем интерполяции между двумя цветами.
        /// </summary>
        /// <param name="type">Тип анимации</param>
        /// <param name="colorA">Первый цвет</param>
        /// <param name="colorB">Второй цвет</param>
        /// <param name="animationTime">Текущее время анимации</param>
        /// <param name="speed">Скорость анимации</param>
        /// <param name="customCurve">Пользовательская кривая для CustomCurve</param>
        /// <returns>Анимированный цвет</returns>
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

        /// <summary>
        /// Получает анимированный Vector3 путем интерполяции между двумя векторами.
        /// </summary>
        /// <param name="type">Тип анимации</param>
        /// <param name="vectorA">Первый вектор</param>
        /// <param name="vectorB">Второй вектор</param>
        /// <param name="animationTime">Текущее время анимации</param>
        /// <param name="speed">Скорость анимации</param>
        /// <param name="customCurve">Пользовательская кривая для CustomCurve</param>
        /// <returns>Анимированный вектор</returns>
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

    /// <summary>
    /// Интерфейс для работы с различными типами источников света (Light, Light2D).
    /// </summary>
    public interface ILightAccessor
    {
        /// <summary>Интенсивность света</summary>
        float Intensity { get; set; }
        
        /// <summary>Цвет света</summary>
        Color Color { get; set; }
        
        /// <summary>Название реализации (для отладки)</summary>
        string ImplName { get; }
    }
}