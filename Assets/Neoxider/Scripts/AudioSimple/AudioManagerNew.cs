using System.Collections.Generic;
using UnityEngine;

namespace Neoxider
{
    namespace Audio
    {

        public static class AM
        {
            private static GameObject audioHost;
            private static AudioSource mainAudioSource;

            // Cache of audio clips loaded from Resources
            private static Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();

            // Track the last play time for each clip to prevent rapid repeats
            private static Dictionary<string, float> clipPlayTimes = new Dictionary<string, float>();

            // Minimum interval between the same clip plays to avoid double play
            private const float minPlayInterval = 0.1f;

            // Ensure the AudioManager is initialized only once
            private static bool isInitialized = false;

            /// <summary>
            /// Initializes AudioManager, loads clips, and prepares main AudioSource.
            /// </summary>
            private static void Initialize()
            {
                if (isInitialized) return;

                // Create an audio host GameObject that persists across scenes
                audioHost = new GameObject("AudioManager");
                Object.DontDestroyOnLoad(audioHost);

                // Add an AudioSource component to handle non-looping sounds
                mainAudioSource = audioHost.AddComponent<AudioSource>();

                // Load all AudioClips from Resources/Audio
                AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");
                foreach (var clip in clips)
                {
                    audioClipCache[clip.name] = clip;
                    clipPlayTimes[clip.name] = -minPlayInterval; // Default last play time
                }

                isInitialized = true;
            }

            /// <summary>
            /// Plays an audio clip by name with optional volume control.
            /// </summary>
            /// <param name="clipName">The name of the audio clip to play.</param>
            /// <param name="volume">Volume level (0.0 to 1.0).</param>
            public static void Play(string clipName, float volume = 1f)
            {
                Initialize();

                if (!audioClipCache.TryGetValue(clipName, out AudioClip clip))
                {
                    Debug.LogWarning($"Audio clip '{clipName}' not found!");
                    return;
                }

                // Check if minimum interval has passed since last play
                float lastPlayTime = clipPlayTimes[clipName];
                if (Time.time - lastPlayTime < minPlayInterval)
                {
                    Debug.LogWarning($"Audio clip '{clipName}' is played too frequently!");
                    return;
                }

                // Play the clip with specified volume and update play time
                mainAudioSource.PlayOneShot(clip, volume);
                clipPlayTimes[clipName] = Time.time;
            }

            /// <summary>
            /// Stops all currently playing audio clips.
            /// </summary>
            public static void StopAll()
            {
                Initialize();
                mainAudioSource.Stop();
            }
        }
    }
}