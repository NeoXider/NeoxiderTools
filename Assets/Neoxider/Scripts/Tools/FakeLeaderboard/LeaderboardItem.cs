using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class LeaderboardItem : MonoBehaviour
{
    public LeaderboardUser user;
    public int id;
    public bool isPlayer;

    [Space]
    public TMP_Text textScore;
    public TMP_Text textId;
    public TMP_Text textName;

    [Space]
    public UnityEvent OnUserTrue;
    public UnityEvent OnuserFalse;
    public UnityEvent<bool> OnUser;

    public void Set(LeaderboardUser user, int id, bool isPlayer)
    {
        this.user = user;
        this.id = id;
        this.isPlayer = isPlayer;

        textName.text = user.name;
        textScore.text = user.score.ToString();
        textId.text = (id + 1).ToString();

        Events(isPlayer);
    }

    private void Events(bool isPlayer)
    {
        OnUser?.Invoke(isPlayer);

        if (isPlayer) OnUserTrue?.Invoke();
        else OnuserFalse?.Invoke();
    }
}
