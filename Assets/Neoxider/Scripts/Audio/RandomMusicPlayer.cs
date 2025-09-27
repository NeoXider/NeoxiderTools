using System.Collections;
using Neo.Extensions;
using UnityEngine;

namespace Neo
{
    [AddComponentMenu("Neoxider/" + "Audio/" + nameof(RandomMusicPlayer))]
    [RequireComponent(typeof(AudioSource))]
    public class RandomMusicPlayer : MonoBehaviour
    {
        public AudioClip[] musicTracks;
        public bool playOnStart = true;
        private AudioSource audioSource;
        private int lastTrackIndex = -1;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (playOnStart) StartCoroutine(PlayMusicLoop());
        }

        private IEnumerator PlayMusicLoop()
        {
            while (true)
            {
                if (musicTracks.Length == 0)
                {
                    Debug.LogWarning("Музыкальные треки не назначены в RandomMusicPlayer.");
                    yield break; // Stop the coroutine if there are no tracks
                }

                int newTrackIndex;
                do
                {
                    newTrackIndex = musicTracks.GetRandomIndex();
                } while (newTrackIndex == lastTrackIndex && musicTracks.Length > 1);

                lastTrackIndex = newTrackIndex;
                audioSource.clip = musicTracks[newTrackIndex];
                audioSource.Play();

                // Wait for the current clip to finish playing before choosing the next one
                yield return new WaitForSeconds(audioSource.clip.length);
            }
        }
    }
}