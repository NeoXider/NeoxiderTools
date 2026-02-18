using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [NeoDoc("Tools/FakeLeaderboard/LeaderboardMove.md")]
    [CreateFromMenu("Neoxider/Tools/LeaderboardMove", "Prefabs/FakeLeaderboard/Leaderboard.prefab")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(LeaderboardMove))]
    public class LeaderboardMove : MonoBehaviour
    {
        [Header("Leaderboard Reference")]
        [Tooltip("Specific leaderboard to use. If not set, singleton is used")]
        public Leaderboard leaderboard;

        public bool useMove = true;
        public float delayTime = 0.5f;
        public float timeMove = 0.5f;
        public float offsetY = 300;


        [Space] public bool useAnimPlayer = true;

        [Tooltip("How much the player entry is scaled up relative to its base size")]
        public float scaleDelta = 0.1f;

        public float durationAnimPlayer = 0.3f;
        public bool useAnimStartScale;

        [Space] public bool useSortEnable = true;
        public UnityEvent Enable;

        private Tweener _moveTween;
        private Tweener _scaleTween;

        private LeaderboardItem lastAnimatedItem;
        private Vector3 lastAnimatedItemBaseScale = Vector3.one;

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

        private void OnDisable()
        {
            KillActiveTweens();
        }

        private Leaderboard GetLeaderboard()
        {
            return leaderboard != null ? leaderboard : Leaderboard.I;
        }

        public void Move()
        {
            KillActiveTweens();

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

                // Сбрасываем масштаб предыдущей выделенной карточки, если она больше не является текущей.
                if (lastAnimatedItem != null && lastAnimatedItem != targetItem)
                {
                    lastAnimatedItem.transform.localScale = lastAnimatedItemBaseScale;
                }

                // Запоминаем базовый размер текущей карточки игрока перед анимацией.
                Vector3 baseScale = targetItem.transform.localScale;
                lastAnimatedItem = targetItem;
                lastAnimatedItemBaseScale = baseScale;

                Vector3 targetPos = transform.position - targetItem.transform.position;

                _moveTween = transform.DOMoveY(targetPos.y + offsetY, timeMove)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        if (useAnimPlayer)
                        {
                            Vector3 targetScale = baseScale + Vector3.one * scaleDelta;

                            // Увеличиваем карточку и возвращаем к базовому размеру в стиле "yoyo".
                            _scaleTween = targetItem.transform
                                .DOScale(targetScale, durationAnimPlayer)
                                .SetLoops(2, LoopType.Yoyo);
                        }
                    });
            }
            else
            {
                Debug.LogWarning("Not Find player in leaderboards");
            }
        }

        /// <summary>
        ///     Отменяет все активные анимации.
        /// </summary>
        public void KillActiveTweens()
        {
            _moveTween?.Kill();
            _scaleTween?.Kill();
            _moveTween = null;
            _scaleTween = null;
        }
    }
}
