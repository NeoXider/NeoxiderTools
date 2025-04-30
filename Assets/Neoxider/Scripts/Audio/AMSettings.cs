using UnityEngine;

namespace Neo.Audio
{
    public class AMSettings : MonoBehaviour
    {
        [SerializeField] private AM _am;

        public AudioSource efx;
        public AudioSource music;

        public float startEfxVolume { get; private set; }

        public float startMusicVolume { get; private set; }
        
        private void Start()
        {
            startEfxVolume = efx.volume;
            startMusicVolume = music.volume;

            SetEfx(true);
            SetMusic(true);
        }

        private void OnValidate()
        {
            _am = FindFirstObjectByType<AM>();

            if (_am != null)
            {
                efx = _am.Efx;
                music = _am.Music;
            }
        }

        [Button]
        public void SetEfx(bool active)
        {
            efx.mute = !active;
        }

        [Button]
        public void SetMusic(bool active)
        {
            music.mute = !active;
        }

        [Button]
        public void SetMusicAndEfx(bool active)
        {
            SetEfx(active);
            SetMusic(active);
        }

        [Button]
        public void SetMusicAndEfxVolume(float percent)
        {
            SetEfxVolume(percent);
            SetMusicVolume(percent);
        }

        [Button]
        public void SetMusicVolume(float percent)
        {
            music.volume = Mathf.Lerp(0, startMusicVolume, percent);
        }

        [Button]
        public void SetEfxVolume(float percent)
        {
            efx.volume = Mathf.Lerp(0, startEfxVolume, percent);
        }

        [Button]
        public void ToggleMusic()
        {
            SetMusic(music.mute);
        }

        [Button]
        public void ToggleEfx()
        {
            SetEfx(efx.mute);
        }

        [Button]
        public void ToggleMusicAndEfx()
        {
            SetEfx(music.mute);
            SetMusic(music.mute);
        }
    }
}