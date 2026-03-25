using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Neo.Extensions;
using UnityEngine;

namespace Neo.Audio
{
    /// <summary>
    ///     Controller for random music playback from a track list without consecutive repeats. Not a MonoBehaviour; used
    ///     by AM.
    /// </summary>
    public class RandomMusicController
    {
        private AudioSource _audioSource;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPlaying;
        private int _lastTrackIndex = -1;
        private AudioClip[] _tracks;

        /// <summary>
        ///     Gets the currently playing track clip.
        /// </summary>
        public AudioClip CurrentTrack => _audioSource != null ? _audioSource.clip : null;

        /// <summary>
        ///     True while music is actively playing (not paused).
        /// </summary>
        public bool IsPlaying => _isPlaying && !IsPaused;

        /// <summary>
        ///     True when playback is paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        ///     Raised when the active track changes.
        /// </summary>
        public event Action<AudioClip> OnTrackChanged;

        /// <summary>
        ///     Raised when playback stops.
        /// </summary>
        public event Action OnStopped;

        /// <summary>
        ///     Initializes the controller with an <see cref="AudioSource"/> and track list.
        /// </summary>
        /// <param name="audioSource">Source used for music playback.</param>
        /// <param name="tracks">Tracks to choose from.</param>
        public void Initialize(AudioSource audioSource, AudioClip[] tracks)
        {
            if (audioSource == null)
            {
                Debug.LogError("[RandomMusicController] AudioSource cannot be null.");
                return;
            }

            _audioSource = audioSource;
            _tracks = tracks ?? Array.Empty<AudioClip>();
        }

        /// <summary>Starts random music playback from the track list.</summary>
        public void Start()
        {
            if (_audioSource == null)
            {
                Debug.LogError(
                    "[RandomMusicController] AudioSource is not initialized. Call Initialize() first.");
                return;
            }

            if (_tracks == null || _tracks.Length == 0)
            {
                Debug.LogWarning("[RandomMusicController] Track list is empty.");
                return;
            }

            Stop();

            _isPlaying = true;
            IsPaused = false;
            _cancellationTokenSource = new CancellationTokenSource();
            PlayMusicLoop(_cancellationTokenSource.Token).Forget();
        }

        /// <summary>
        ///     Stops music playback.
        /// </summary>
        public void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            _isPlaying = false;
            IsPaused = false;

            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            OnStopped?.Invoke();
        }

        /// <summary>
        ///     Pauses music playback.
        /// </summary>
        public void Pause()
        {
            if (!_isPlaying || IsPaused)
            {
                return;
            }

            if (_audioSource != null)
            {
                _audioSource.Pause();
            }

            IsPaused = true;
        }

        /// <summary>
        ///     Resumes paused music playback.
        /// </summary>
        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            if (_audioSource != null)
            {
                _audioSource.UnPause();
            }

            IsPaused = false;
        }

        private async UniTaskVoid PlayMusicLoop(CancellationToken cancellationToken)
        {
            while (_isPlaying && !cancellationToken.IsCancellationRequested)
            {
                if (_tracks.Length == 0)
                {
                    Debug.LogWarning("[RandomMusicController] Track list is empty.");
                    break;
                }

                int newTrackIndex;
                do
                {
                    newTrackIndex = _tracks.GetRandomIndex();
                } while (newTrackIndex == _lastTrackIndex && _tracks.Length > 1);

                _lastTrackIndex = newTrackIndex;
                AudioClip track = _tracks[newTrackIndex];

                if (track == null)
                {
                    Debug.LogWarning($"[RandomMusicController] Track at index {newTrackIndex} is null.");
                    break;
                }

                _audioSource.clip = track;
                _audioSource.Play();

                OnTrackChanged?.Invoke(track);

                try
                {
                    await UniTask.Delay((int)(track.length * 1000), cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
