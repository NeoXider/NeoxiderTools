using UnityEngine;

public class AnchorMove : MonoBehaviour
{
    [Range(0, 1)] public float x = 0.5f;

    [Range(0, 1)] public float y = 0.5f;

    private RectTransform rect;

    private void OnValidate()
    {
        rect ??= transform as RectTransform;

        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(x, y);

        rect.anchoredPosition = Vector2.zero;


        rect.anchoredPosition = Vector2.zero;


        print(rect.localPosition);
    }
}