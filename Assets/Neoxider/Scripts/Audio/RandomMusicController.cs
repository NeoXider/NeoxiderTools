using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Neo.Extensions;
using UnityEngine;

namespace Neo.Audio
{
    /// <summary>
    /// Контроллер для воспроизведения случайной музыки из списка треков без повторов подряд.
    /// Не является MonoBehaviour, управляется через AM.
    /// </summary>
    public class RandomMusicController
    {
        private AudioSource _audioSource;
        private AudioClip[] _tracks;
        private int _lastTrackIndex = -1;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPlaying;
        private bool _isPaused;

        /// <summary>
        /// Событие вызывается при смене трека.
        /// </summary>
        public event Action<AudioClip> OnTrackChanged;

        /// <summary>
        /// Событие вызывается при остановке воспроизведения.
        /// </summary>
        public event Action OnStopped;

        /// <summary>
        /// Возвращает текущий воспроизводимый трек.
        /// </summary>
        public AudioClip CurrentTrack => _audioSource != null ? _audioSource.clip : null;

        /// <summary>
        /// Возвращает true, если музыка воспроизводится.
        /// </summary>
        public bool IsPlaying => _isPlaying && !_isPaused;

        /// <summary>
        /// Возвращает true, если воспроизведение приостановлено.
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Инициализирует контроллер с указанным AudioSource и списком треков.
        /// </summary>
        /// <param name="audioSource">AudioSource для воспроизведения музыки.</param>
        /// <param name="tracks">Массив музыкальных треков для воспроизведения.</param>
        public void Initialize(AudioSource audioSource, AudioClip[] tracks)
        {
            if (audioSource == null)
            {
                Debug.LogError("[RandomMusicController] AudioSource не может быть null.");
                return;
            }

            _audioSource = audioSource;
            _tracks = tracks ?? Array.Empty<AudioClip>();
        }

        /// <summary>
        /// Начинает воспроизведение случайной музыки из списка треков.
        /// </summary>
        public void Start()
        {
            if (_audioSource == null)
            {
                Debug.LogError("[RandomMusicController] AudioSource не инициализирован. Вызовите Initialize() сначала.");
                return;
            }

            if (_tracks == null || _tracks.Length == 0)
            {
                Debug.LogWarning("[RandomMusicController] Список треков пуст.");
                return;
            }

            Stop();

            _isPlaying = true;
            _isPaused = false;
            _cancellationTokenSource = new CancellationTokenSource();
            PlayMusicLoop(_cancellationTokenSource.Token).Forget();
        }

        /// <summary>
        /// Останавливает воспроизведение музыки.
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
            _isPaused = false;

            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            OnStopped?.Invoke();
        }

        /// <summary>
        /// Приостанавливает воспроизведение музыки.
        /// </summary>
        public void Pause()
        {
            if (!_isPlaying || _isPaused)
            {
                return;
            }

            if (_audioSource != null)
            {
                _audioSource.Pause();
            }

            _isPaused = true;
        }

        /// <summary>
        /// Возобновляет воспроизведение приостановленной музыки.
        /// </summary>
        public void Resume()
        {
            if (!_isPaused)
            {
                return;
            }

            if (_audioSource != null)
            {
                _audioSource.UnPause();
            }

            _isPaused = false;
        }

        private async UniTaskVoid PlayMusicLoop(CancellationToken cancellationToken)
        {
            while (_isPlaying && !cancellationToken.IsCancellationRequested)
            {
                if (_tracks.Length == 0)
                {
                    Debug.LogWarning("[RandomMusicController] Список треков пуст.");
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
                    Debug.LogWarning($"[RandomMusicController] Трек по индексу {newTrackIndex} равен null.");
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
