using UnityEngine;
using UnityEngine.Audio;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _clips;

    private void Awake()
    {
        Instance = this;
    }

    public void Play(int id)
    {
        _audioSource.PlayOneShot(_clips[id]);
    }

    public void OnClick()
    {
        _audioSource.PlayOneShot(_clips[^1]);
    }
}