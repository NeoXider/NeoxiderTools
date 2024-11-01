using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenScale : MonoBehaviour
{
    public Camera _camera;

    public float max = 50;
    public float min = 35f;

    void Start()
    {
        _camera = GetComponent<Camera>();
        AdjustFieldOfView();
    }

    private void AdjustFieldOfView()
    {
        float aspectRatio = (float)Screen.width / (float)Screen.height;

        if (Mathf.Approximately(aspectRatio, 4f / 3f))
        {
            _camera.fieldOfView = max;
        }
        else
        {
            _camera.fieldOfView = min;
        }
    }

    public void OnValidate()
    {
        _camera = GetComponent<Camera>();
        AdjustFieldOfView();
    }
}
