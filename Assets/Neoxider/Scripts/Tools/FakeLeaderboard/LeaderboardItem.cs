using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [NeoDoc("Tools/FakeLeaderboard/LeaderboardItem.md")]
    [CreateFromMenu("Neoxider/Tools/FakeLeaderboard/LeaderboardItem",
        "Prefabs/FakeLeaderboard/LeaderBoard Item.prefab")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(LeaderboardItem))]
    public class LeaderboardItem : MonoBehaviour
    {
        [Header("References")] public TMP_Text textId;
        public TMP_Text textName;
        public TMP_Text textScore;

        public UnityEvent OnUserTrue;
        public UnityEvent OnuserFalse;
        public UnityEvent<bool> OnUser;

        [Header("Debug")] public int id;
        public bool isPlayer;
        public LeaderboardUser user;

        public void Set(LeaderboardUser user, bool isPlayer, Leaderboard leaderboard = null)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            this.user = user;
            id = user.num;
            this.isPlayer = isPlayer;

            if (textName != null)
            {
                textName.text = user.name;
            }

            if (textScore != null)
            {
                // WHY: Player with no score: show placeholder (bypasses score formatting).
                if (isPlayer && user.score == 0)
                {
                    textScore.text = leaderboard != null ? leaderboard.noScoreText : "--";
                }
                else
                {
                    textScore.text = leaderboard != null ? leaderboard.FormatScore(user.score) : user.score.ToString();
                }
            }

            if (textId != null)
            {
                textId.text = (id + 1).ToString();
            }

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
