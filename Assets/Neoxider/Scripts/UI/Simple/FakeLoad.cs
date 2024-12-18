using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FakeLoad : MonoBehaviour
{
    [SerializeField] private Vector2 timeLoad = new Vector2(1.5f, 2);
    [SerializeField] private bool isLoadOne = true;

    public UnityEvent OnStart;
    public UnityEvent OnFinisLoad;

    private static bool isInitialized = false;

    private void Awake()
    {
        if (!isLoadOne || (isLoadOne && !isInitialized))
        {
            float time = Random.Range(timeLoad.x, timeLoad.y);
            OnStart?.Invoke();
            Invoke(nameof(EndLoad), time);
            isInitialized = true;
        }
    }

    private void EndLoad()
    {
        OnFinisLoad?.Invoke();
    }
}
