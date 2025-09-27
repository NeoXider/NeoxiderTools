using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    public class LeaderboardMove : MonoBehaviour
    {
        public bool useMove = true;
        public float delayTime = 0.5f;
        public float timeMove = 0.5f;
        public float offsetY = 300;


        [Space] public bool useAnimPlayer = true;
        public float scale = 1.2f;
        public float durationAnimPlayer = 0.3f;
        public bool useAnimStartScale;

        [Space] public bool useSortEnable = true;
        public UnityEvent Enable;

        private float starScale;

        private void Start()
        {
            var idPlayer = Leaderboard.I.GetIdPlayer();

            if (idPlayer >= 0)
            {
                print("move to " + idPlayer + " pos");
                var targetItem = Leaderboard.I.leaderboardItems[idPlayer];

                starScale = targetItem.transform.localScale.x;
            }
        }

        private void OnEnable()
        {
            Enable?.Invoke();

            if (useMove) Invoke(nameof(Move), delayTime);

            if (useSortEnable)
                if (Leaderboard.I != null)
                    Leaderboard.I.Sort();
        }

        public void Move()
        {
            var idPlayer = Leaderboard.I.GetIdPlayer();

            if (idPlayer >= 0)
            {
                print("move to " + idPlayer + " pos");
                var targetItem = Leaderboard.I.leaderboardItems[idPlayer];
                var targetPos = transform.position - targetItem.transform.position;

                transform.DOMoveY(targetPos.y + offsetY, timeMove)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        if (useAnimPlayer)
                            targetItem.transform.DOScale(scale, durationAnimPlayer).OnComplete(() =>
                            {
                                if (useAnimStartScale)
                                    targetItem.transform.DOScale(1, durationAnimPlayer);
                            });
                    });
            }
            else
            {
                Debug.LogWarning("Not Find player in leaderboards");
            }
        }
    }
}