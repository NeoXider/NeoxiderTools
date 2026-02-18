using Neo;
using Neo.Extensions;
using Neo.UI;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Neo.Pages
{
    [MovedFrom("")]
    [NeoDoc("NeoxiderPages/ToggleAudio.md")]
    [AddComponentMenu("Neoxider/Pages/" + nameof(ToggleAudio))]
    public class ToggleAudio : MonoBehaviour
    {
        [FormerlySerializedAs("_toggleView")] [SerializeField]
        private VisualToggle uiToggleView;

        [SerializeField] private bool isMusic;

        private void Awake()
        {
            this.WaitWhile(() => !G.Inited, Init);
        }

        private void OnEnable()
        {
            uiToggleView.SetActive(isMusic ? Audio.IsActiveMusic : Audio.IsActiveSound);
        }

        private void OnValidate()
        {
            uiToggleView ??= GetComponent<VisualToggle>();
        }

        private void Init()
        {
            if (isMusic)
            {
                uiToggleView?.OnValueChanged.AddListener(value => Audio.IsActiveMusic = value);
            }
            else
            {
                uiToggleView?.OnValueChanged.AddListener(value => Audio.IsActiveSound = value);
            }
        }
    }
}