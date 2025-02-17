using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class LeaderboardMove : MonoBehaviour
{
    public bool useMove = true;
    public float delayTime = 0.5f;
    public float timeMove = 0.5f;
    public float offsetY = 0;


    [Space]
    public bool useAnimPlayer = true;

    [Space]
    public bool useSortEnable = true;
    public UnityEvent Enable;

    private void OnEnable()
    {
        Enable?.Invoke();

        if (useMove)
        {
            Invoke(nameof(Move), delayTime);
        }

        if (useSortEnable)
            Leaderboard.Instance.Sort();
    }

    public void Move()
    {
        int idPlayer = Leaderboard.Instance.GetIdPlayer();

        if (idPlayer >= 0)
        {
            print("move to " + idPlayer.ToString() + " pos");
            LeaderboardItem targetItem = Leaderboard.Instance.leaderboardItems[idPlayer];
            Vector3 targetPos = transform.position - targetItem.transform.position;

            transform.DOMoveY(targetPos.y + offsetY, timeMove)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    if (useAnimPlayer)
                    {
                        targetItem.transform.DOScale(1.2f, 0.3f).OnComplete(() =>
                        {
                            targetItem.transform.DOScale(1f, 0.3f);
                        });
                    }
                });
        }
        else
        {
            Debug.LogWarning("Not Find player in leaderboards");
        }
    }
}
