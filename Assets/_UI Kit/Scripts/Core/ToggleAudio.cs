using UnityEngine;
using UnityEngine.Serialization;

public class ToggleAudio : MonoBehaviour
{
    [FormerlySerializedAs("_toggleView")] [SerializeField]
    private UIToggleView uiToggleView;

    [SerializeField] private bool isMusic;

    private void Awake()
    {
        this.WaitWhile(() => !G.Inited, Init);
    }

    private void Init()
    {
        if (isMusic)
            uiToggleView?.OnValueChanged.AddListener((value) => Audio.IsActiveMusic = value);
        else
            uiToggleView?.OnValueChanged.AddListener((value) => Audio.IsActiveSound = value);
    }

    private void OnEnable()
    {
        uiToggleView.Set(isMusic ? Audio.IsActiveMusic : Audio.IsActiveSound);
    }

    private void OnValidate()
    {
        uiToggleView ??= GetComponent<UIToggleView>();
    }
}