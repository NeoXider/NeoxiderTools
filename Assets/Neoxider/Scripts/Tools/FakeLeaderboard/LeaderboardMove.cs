using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neo/" + "Tools/" + nameof(LeaderboardMove))]
    public class LeaderboardMove : MonoBehaviour
    {
        private const float ScaleDelta = 0.1f;

        [Header("Leaderboard Reference")]
        [Tooltip("Конкретный лидерборд для использования. Если не указан, используется синглтон")]
        public Leaderboard leaderboard;

        public bool useMove = true;
        public float delayTime = 0.5f;
        public float timeMove = 0.5f;
        public float offsetY = 300;


        [Space] public bool useAnimPlayer = true;
        public float durationAnimPlayer = 0.3f;
        public bool useAnimStartScale;

        [Space] public bool useSortEnable = true;
        public UnityEvent Enable;

        private float starScale = 1f;

        private Leaderboard GetLeaderboard()
        {
            return leaderboard != null ? leaderboard : Leaderboard.I;
        }

        private void Start()
        {
            Leaderboard lb = GetLeaderboard();
            if (lb == null)
            {
                return;
            }

            int idPlayer = lb.GetIdPlayer();

            if (idPlayer >= 0 && idPlayer < lb.leaderboardItems.Count)
            {
                print("move to " + idPlayer + " pos");
                LeaderboardItem targetItem = lb.leaderboardItems[idPlayer];

                starScale = targetItem.transform.localScale.x;
            }
        }

        private void OnEnable()
        {
            Enable?.Invoke();

            if (useMove)
            {
                Invoke(nameof(Move), delayTime);
            }

            if (useSortEnable)
            {
                Leaderboard lb = GetLeaderboard();
                if (lb != null)
                {
                    lb.Sort();
                }
            }
        }

        public void Move()
        {
            Leaderboard lb = GetLeaderboard();
            if (lb == null)
            {
                Debug.LogWarning("Leaderboard not found");
                return;
            }

            int idPlayer = lb.GetIdPlayer();

            if (idPlayer >= 0 && idPlayer < lb.leaderboardItems.Count)
            {
                print("move to " + idPlayer + " pos");
                LeaderboardItem targetItem = lb.leaderboardItems[idPlayer];

                // Сбрасываем масштаб всех элементов перед новой анимацией, чтобы
                // старые выделенные карточки не сохраняли увеличенный размер.
                foreach (LeaderboardItem item in lb.leaderboardItems)
                {
                    if (item != null)
                    {
                        item.transform.localScale = Vector3.one * starScale;
                    }
                }

                Vector3 targetPos = transform.position - targetItem.transform.position;

                transform.DOMoveY(targetPos.y + offsetY, timeMove)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        if (useAnimPlayer)
                        {
                            float targetScale = starScale + ScaleDelta;

                            targetItem.transform.DOScale(targetScale, durationAnimPlayer).OnComplete(() =>
                            {
                                if (useAnimStartScale)
                                {
                                    targetItem.transform.DOScale(starScale, durationAnimPlayer);
                                }
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
}