namespace Neo.Animations
{
    /// <summary>Animation types for various components (light, emission, colors, values).</summary>
    public enum AnimationType
    {
        /// <summary>Random flicker between min and max values.</summary>
        RandomFlicker,

        /// <summary>Smooth sine-based pulsing.</summary>
        Pulsing,

        /// <summary>Smooth back-and-forth transition between values.</summary>
        SmoothTransition,

        /// <summary>Perlin noise-based animation for natural effect.</summary>
        PerlinNoise,

        /// <summary>Sine wave.</summary>
        SinWave,

        /// <summary>Exponential decay.</summary>
        Exponential,

        /// <summary>Bounce with decay.</summary>
        BounceEase,

        /// <summary>Elastic effect.</summary>
        ElasticEase,

        /// <summary>Animation driven by a custom curve.</summary>
        CustomCurve
    }
}