using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [NeoDoc("Tools/FakeLeaderboard/LeaderboardMove.md")]
    [CreateFromMenu("Neoxider/Tools/FakeLeaderboard/LeaderboardMove", "Prefabs/FakeLeaderboard/Leaderboard.prefab")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(LeaderboardMove))]
    public class LeaderboardMove : MonoBehaviour
    {
        [Header("Leaderboard Reference")] [Tooltip("Specific leaderboard to use. If not set, singleton is used")]
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

        [Header("Diagnostics")] [SerializeField]
        private bool _debugLogging;

        [SerializeField] private bool _logWarnings;

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

            if (_debugLogging && lb.leaderboardItems != null &&
                idPlayer >= 0 && idPlayer < lb.leaderboardItems.Count)
            {
                NeoDiagnostics.Log($"[LeaderboardMove] Move to player item {idPlayer}.", this);
            }
        }

        private void OnEnable()
        {
            Enable?.Invoke();

            if (!isActiveAndEnabled)
            {
                return;
            }

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

        private void OnDestroy()
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

            if (!isActiveAndEnabled)
            {
                return;
            }

            Leaderboard lb = GetLeaderboard();
            if (lb == null)
            {
                if (_logWarnings)
                {
                    NeoDiagnostics.LogWarning("[LeaderboardMove] Leaderboard not found.", this);
                }

                return;
            }

            int idPlayer = lb.GetIdPlayer();

            if (lb.leaderboardItems != null && idPlayer >= 0 && idPlayer < lb.leaderboardItems.Count &&
                lb.leaderboardItems[idPlayer] != null)
            {
                if (_debugLogging)
                {
                    NeoDiagnostics.Log($"[LeaderboardMove] Move to player item {idPlayer}.", this);
                }

                LeaderboardItem targetItem = lb.leaderboardItems[idPlayer];

                // WHY: Remember base scale of the current player card before animating.
                Vector3 baseScale = targetItem.transform.localScale;
                lastAnimatedItem = targetItem;
                lastAnimatedItemBaseScale = baseScale;

                Vector3 targetPos = transform.position - targetItem.transform.position;

                _moveTween = transform.DOMoveY(targetPos.y + offsetY, timeMove)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        if (useAnimPlayer && targetItem != null && isActiveAndEnabled)
                        {
                            Vector3 targetScale = baseScale + Vector3.one * scaleDelta;

                            _scaleTween = targetItem.transform
                                .DOScale(targetScale, durationAnimPlayer)
                                .SetLoops(2, LoopType.Yoyo)
                                .OnKill(() => _scaleTween = null);
                        }
                    })
                    .OnKill(() => _moveTween = null);
            }
            else
            {
                if (_logWarnings)
                {
                    NeoDiagnostics.LogWarning("[LeaderboardMove] Player item was not found in leaderboard.", this);
                }
            }
        }

        /// <summary>
        ///     Cancels the pending move and owned animations, restoring the player row's base scale.
        /// </summary>
        public void KillActiveTweens()
        {
            CancelInvoke(nameof(Move));
            _moveTween?.Kill();
            _scaleTween?.Kill();
            _moveTween = null;
            _scaleTween = null;

            if (lastAnimatedItem != null)
            {
                lastAnimatedItem.transform.localScale = lastAnimatedItemBaseScale;
            }

            lastAnimatedItem = null;
        }
    }
}
