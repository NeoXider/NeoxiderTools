using UnityEngine;
using UnityEngine.EventSystems;

public class PlayAudio : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Audio.PlayUI();
    }
}