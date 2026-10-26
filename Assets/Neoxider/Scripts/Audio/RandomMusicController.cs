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
        private bool _rebinding;
        private long _playbackGeneration;

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
                NeoDiagnostics.LogError("[RandomMusicController] AudioSource cannot be null.");
                return;
            }

            if (_rebinding)
            {
                return;
            }

            _rebinding = true;
            _playbackGeneration++;
            try
            {
                StopPlayback();
            }
            finally
            {
                // Rebind owns this transition: OnStopped cannot restart or replace the old binding.
                _lastTrackIndex = -1;
                _audioSource = audioSource;
                _tracks = tracks ?? Array.Empty<AudioClip>();
                _rebinding = false;
            }
        }

        /// <summary>Starts random music playback from the track list.</summary>
        [Button("Play Random Music", PlayModeOnly = true)]
        public void Start()
        {
            if (_rebinding)
            {
                return;
            }

            if (_audioSource == null)
            {
                NeoDiagnostics.LogError(
                    "[RandomMusicController] AudioSource is not initialized. Call Initialize() first.");
                return;
            }

            if (_tracks == null || _tracks.Length == 0)
            {
                NeoDiagnostics.LogWarning("[RandomMusicController] Track list is empty.");
                return;
            }

            long generation = ++_playbackGeneration;
            StopPlayback();
            if (generation != _playbackGeneration || _audioSource == null)
            {
                return;
            }

            _isPlaying = true;
            IsPaused = false;
            _cancellationTokenSource = new CancellationTokenSource();
            PlayMusicLoop(_cancellationTokenSource.Token, generation, _audioSource, _tracks).Forget();
        }

        /// <summary>
        ///     Stops music playback. Raises <see cref="OnStopped"/> only when playback was actually active.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void Stop()
        {
            _playbackGeneration++;
            StopPlayback();
        }

        private void StopPlayback()
        {
            // WHY: Start() calls Stop() defensively before (re)starting — without this guard every
            // Start() fired a spurious OnStopped to subscribers.
            bool wasActive = _isPlaying;

            CancellationTokenSource cancellation = _cancellationTokenSource;
            _cancellationTokenSource = null;
            _isPlaying = false;
            IsPaused = false;

            if (_audioSource != null && wasActive)
            {
                _audioSource.Stop();
            }

            if (cancellation != null)
            {
                try
                {
                    cancellation.Cancel();
                }
                finally
                {
                    cancellation.Dispose();
                }
            }

            if (wasActive)
            {
                OnStopped?.Invoke();
            }
        }

        /// <summary>
        ///     Pauses music playback.
        /// </summary>
        [Button(PlayModeOnly = true)]
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
        [Button(PlayModeOnly = true)]
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

        private async UniTaskVoid PlayMusicLoop(CancellationToken cancellationToken, long generation,
            AudioSource source, AudioClip[] tracks)
        {
            try
            {
                while (generation == _playbackGeneration && _isPlaying && !cancellationToken.IsCancellationRequested)
                {
                    if (source == null)
                    {
                        break;
                    }

                    if (tracks.Length == 0)
                    {
                        NeoDiagnostics.LogWarning("[RandomMusicController] Track list is empty.");
                        break;
                    }

                    int newTrackIndex;
                    do
                    {
                        newTrackIndex = tracks.GetRandomIndex();
                    } while (newTrackIndex == _lastTrackIndex && tracks.Length > 1);

                    _lastTrackIndex = newTrackIndex;
                    AudioClip track = tracks[newTrackIndex];

                    if (track == null)
                    {
                        NeoDiagnostics.LogWarning($"[RandomMusicController] Track at index {newTrackIndex} is null.");
                        break;
                    }

                    source.clip = track;
                    source.Play();

                    OnTrackChanged?.Invoke(track);

                    // WHY: audio plays in real time and can be paused — poll with unscaled time
                    // instead of a scaled one-shot delay, so Pause() holds the current track
                    // (no silent auto-advance) and Time.timeScale never cuts tracks short.
                    float remaining = track.length;
                    while (remaining > 0f && generation == _playbackGeneration && _isPlaying && source != null)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                        if (generation == _playbackGeneration && source != null && !IsPaused &&
                            (!AudioListener.pause || source.ignoreListenerPause))
                        {
                            remaining -= Time.unscaledDeltaTime * Mathf.Abs(source.pitch);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                // A cancelled older loop must never stop a replacement started by an event subscriber.
                if (generation == _playbackGeneration && !cancellationToken.IsCancellationRequested)
                {
                    Stop();
                }
            }
        }
    }
}
