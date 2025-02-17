using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FakeLoad : MonoBehaviour
{
    [SerializeField] private bool _loadOnAwake = true;
    [SerializeField] private Vector2 timeLoad = new Vector2(1.5f, 2);
    [SerializeField] private bool isLoadOne = true;

    public UnityEvent OnStart;
    public UnityEvent OnFinisLoad;
    public UnityEvent<float> OnChange;

    private static bool isInitialized = false;

    private void Awake()
    {
        if (_loadOnAwake)
        {
            Load();
        }
    }

    public void Load()
    {
        if (!isLoadOne || (isLoadOne && !isInitialized))
        {
            float time = Random.Range(timeLoad.x, timeLoad.y);
            OnStart?.Invoke();
            StartCoroutine(Loading(time));
            isInitialized = true;
        }
    }

    private IEnumerator Loading(float time)
    {
        float timer = 0;

        while (timer < time)
        {
            float percent = timer / time;
            OnChange?.Invoke(percent);

            yield return null;

            timer += Time.deltaTime;
        }

        EndLoad();
    }

    public void EndLoad()
    {
        CancelInvoke();
        OnFinisLoad?.Invoke();
    }
}
