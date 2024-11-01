using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeLoad : MonoBehaviour
{
    public float timeLoad = 2;
    public GameObject pageLoad;

    void Start()
    {
        pageLoad.SetActive(true);
        Invoke(nameof(EndLoad), timeLoad);
    }

    private void EndLoad()
    {
        pageLoad.SetActive(false);
    }
}
