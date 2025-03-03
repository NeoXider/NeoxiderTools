using Neo.Audio;
using UnityEngine;

namespace Neo.Audio
{
    public class SettingsAudio : MonoBehaviour
    {
        [SerializeField]
        [FindAllInScene]
        private AM _am;
        public AudioSource efx;
        public AudioSource music;

        private float _startEfxVolume;
        private float _startMusicVolume;

        public float startEfxVolume => _startEfxVolume;
        public float startMusicVolume => _startMusicVolume;


        private void Start()
        {
            _startEfxVolume = efx.volume;
            _startMusicVolume = music.volume;

            SetEfx(true);
            SetMusic(true);
        }

        public void SetEfx(bool active)
        {
            efx.mute = !active;
        }

        public void SetMusic(bool active)
        {
            music.mute = !active;
        }

        public void SetMusicAndEfx(bool active)
        {
            SetEfx(active);
            SetMusic(active);
        }

        public void SetMusicAndEfxVolume(float percent)
        {
            SetEfxVolume(percent);
            SetMusicVolume(percent);
        }

        public void SetMusicVolume(float percent)
        {
            music.volume = Mathf.Lerp(0, _startMusicVolume, percent);
        }

        public void SetEfxVolume(float percent)
        {
            efx.volume = Mathf.Lerp(0, _startEfxVolume, percent);
        }

        public void ToggleMusic()
        {
            SetMusic(music.mute);
        }

        public void ToggleEfx()
        {
            SetEfx(efx.mute);
        }

        public void ToggleMusicAndEfx()
        {
            SetEfx(music.mute);
            SetMusic(music.mute);
        }

        private void OnValidate()
        {
            if (_am != null)
            {
                efx = _am.Efx;
                music = _am.Music;
            }
        }
    }
}