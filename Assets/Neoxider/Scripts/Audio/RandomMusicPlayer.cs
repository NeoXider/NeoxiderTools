using UnityEngine;

namespace Neo
{
    public class RandomMusicPlayer : MonoBehaviour
    {
        public AudioClip[] musicTracks;
        public bool playOnStart = true;
        private AudioSource audioSource;
        private int lastTrackIndex = -1;

        void Start()
        {
            if (playOnStart && musicTracks.Length > 0)
            {
                PlayRandomTrack();
            }
        }

        void Update()
        {
            if (!audioSource.isPlaying)
            {
                PlayRandomTrack();
            }
        }

        void PlayRandomTrack()
        {
            if (musicTracks.Length == 0)
            {
                Debug.LogWarning("����������� ����� �� ���������.");
                return;
            }

            int newTrackIndex;

            do
            {
                newTrackIndex = musicTracks.GetRandomIndex();
            }
            while (newTrackIndex == lastTrackIndex && musicTracks.Length > 1);

            audioSource.clip = musicTracks[newTrackIndex];
            audioSource.Play();

            lastTrackIndex = newTrackIndex;
        }

        private void OnValidate()
        {
            audioSource ??= GetComponent<AudioSource>();
        }
    }
}