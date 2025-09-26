using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class Utils
{
    public static void WaitWhile(this MonoBehaviour owner,
        Func<bool> predicate,
        Action onComplete,
        PlayerLoopTiming timing = PlayerLoopTiming.Update)
    {
        owner.WaitWhileRoutine(predicate, onComplete, timing).Forget();
    }

    private static async UniTaskVoid WaitWhileRoutine(this MonoBehaviour owner,
        Func<bool> predicate,
        Action onComplete,
        PlayerLoopTiming timing)
    {
        await UniTask.WaitWhile(predicate, timing, owner.GetCancellationTokenOnDestroy());
        onComplete?.Invoke();
    }
}