using System.Collections;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for the AudioSource component.
    /// </summary>
    public static class AudioExtensions
    {
        /// <summary>
        ///     Smoothly fades the volume of an AudioSource to a target volume over a specified duration.
        /// </summary>
        /// <param name="source">The AudioSource to fade.</param>
        /// <param name="targetVolume">The volume to fade to (between 0.0 and 1.0).</param>
        /// <param name="duration">The duration of the fade in seconds.</param>
        /// <returns>A handle to the running coroutine, allowing it to be stopped.</returns>
        public static CoroutineExtensions.CoroutineHandle FadeTo(this AudioSource source, float targetVolume,
            float duration)
        {
            if (source == null) return null;
            targetVolume = Mathf.Clamp01(targetVolume);

            return CoroutineExtensions.Start(FadeToRoutine(source, targetVolume, duration));
        }

        /// <summary>
        ///     Smoothly fades the volume of an AudioSource to 0 over a specified duration.
        /// </summary>
        public static CoroutineExtensions.CoroutineHandle FadeOut(this AudioSource source, float duration)
        {
            return source.FadeTo(0f, duration);
        }

        /// <summary>
        ///     Smoothly fades the volume of an AudioSource to a target volume (defaulting to 1.0) over a specified duration.
        /// </summary>
        public static CoroutineExtensions.CoroutineHandle FadeIn(this AudioSource source, float duration,
            float targetVolume = 1.0f)
        {
            if (!source.isPlaying)
            {
                source.volume = 0;
                source.Play();
            }

            return source.FadeTo(targetVolume, duration);
        }

        private static IEnumerator FadeToRoutine(AudioSource source, float targetVolume, float duration)
        {
            var startVolume = source.volume;
            float timer = 0;

            while (timer < duration)
            {
                if (source == null) yield break; // Stop if the source is destroyed

                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
                yield return null;
            }

            if (source != null)
            {
                source.volume = targetVolume;
                if (targetVolume == 0) source.Stop();
            }
        }
    }
}