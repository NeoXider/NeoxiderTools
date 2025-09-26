public class TextMoney : UISetText
{
    public bool level;

    private void Start()
    {
        this.WaitWhile(() => !G.Inited, Init);
    }

    private void Init()
    {
        if (level)
        {
            Set(Wallet.LevelValue);
            Wallet.OnLevelValueChange.AddListener(Set);
        }
        else
        {
            Set(Wallet.Value);
            Wallet.OnValueChange.AddListener(Set);
        }
    }
}