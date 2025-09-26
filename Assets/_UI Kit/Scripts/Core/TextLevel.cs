using UnityEngine.Serialization;

public class TextLevel : UISetText
{
    public bool best;

    private void Start()
    {
        IndexOffset = 1;
        _timeAnim = 0;
        this.WaitWhile(() => !G.Inited, Init);
    }

    private void Init()
    {
        if (best)
        {
            Set(Level.Best);
            Level.OnBestChange.AddListener(Set);
        }
        else
        {
            Set(Level.Current);
            Level.OnCurrentChange.AddListener(Set);
        }
    }
}