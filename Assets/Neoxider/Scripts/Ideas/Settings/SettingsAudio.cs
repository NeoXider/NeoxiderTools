using UnityEngine;

public class SettingsAudio : MonoBehaviour
{
    public AudioSource efx;
    public AudioSource music;

    private void Start()
    {
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
}
