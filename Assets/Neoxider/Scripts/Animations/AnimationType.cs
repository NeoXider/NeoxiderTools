namespace Neo.Animations
{
    /// <summary>
    /// Типы анимации для различных компонентов (свет, эмиссия, цвета, значения).
    /// </summary>
    public enum AnimationType
    {
        /// <summary>Случайное мерцание между минимальным и максимальным значениями</summary>
        RandomFlicker,
        
        /// <summary>Плавное пульсирование по синусоиде</summary>
        Pulsing,
        
        /// <summary>Плавный переход туда-обратно между значениями</summary>
        SmoothTransition,
        
        /// <summary>Анимация на основе шума Перлина для естественного эффекта</summary>
        PerlinNoise,
        
        /// <summary>Синусоидальная волна</summary>
        SinWave,
        
        /// <summary>Экспоненциальное затухание</summary>
        Exponential,
        
        /// <summary>Отскок с затуханием</summary>
        BounceEase,
        
        /// <summary>Эластичный эффект</summary>
        ElasticEase,
        
        /// <summary>Анимация по пользовательской кривой</summary>
        CustomCurve
    }
}