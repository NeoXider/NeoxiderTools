using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neo/" + "Tools/" + nameof(LeaderboardItem))]
    public class LeaderboardItem : MonoBehaviour
    {
        public int id;
        public bool isPlayer;

        [Space] public UnityEvent OnUserTrue;
        public UnityEvent OnuserFalse;
        public UnityEvent<bool> OnUser;
        public TMP_Text textId;
        public TMP_Text textName;

        [Space] public TMP_Text textScore;
        public LeaderboardUser user;

        public void Set(LeaderboardUser user, bool isPlayer, Leaderboard leaderboard = null)
        {
            this.user = user;
            id = user.num;
            this.isPlayer = isPlayer;

            textName.text = user.name;
            
            // Если это игрок и у него нет счета, показываем текст "пропущено" (независимо от форматирования)
            if (isPlayer && user.score == 0)
            {
                textScore.text = leaderboard != null ? leaderboard.noScoreText : "--";
            }
            else if (leaderboard != null)
            {
                textScore.text = leaderboard.FormatScore(user.score);
            }
            else
            {
                textScore.text = user.score.ToString();
            }
            
            textId.text = (id + 1).ToString();

            Events(isPlayer);
        }

        private void Events(bool isPlayer)
        {
            OnUser?.Invoke(isPlayer);

            if (isPlayer)
            {
                OnUserTrue?.Invoke();
            }
            else
            {
                OnuserFalse?.Invoke();
            }
        }
    }
}