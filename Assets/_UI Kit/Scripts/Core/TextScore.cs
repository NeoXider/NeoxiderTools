public class TextScore : UISetText
{
    public bool best;

    private void Start()
    {
        this.WaitWhile(() => !G.Inited, Init);
    }

    private void Init()
    {
        if (best)
        {
            Set(Score.Best);
            Score.OnBestChange.AddListener(Set);
        }
        else
        {
            Set(Score.Current);
            Score.OnCurrentChange.AddListener(Set);
        }
    }
}