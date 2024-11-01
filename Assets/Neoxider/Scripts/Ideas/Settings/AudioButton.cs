using UnityEngine;
using UnityEngine.UI;

public class AudioButton : MonoBehaviour
{
    public Button button;

    private void OnEnable()
    {
        if (button != null)
        {
            OnValidate();
            button.onClick.AddListener(OnClick);
        }

    }

    private void OnClick()
    {
        AudioController.Instance.Play(0);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    private void OnValidate()
    {
        button ??= GetComponent<Button>();
    }
}
