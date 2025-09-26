using UnityEngine.Events;

public class UIKit
{
    public enum Page
    {
        None,

        _CloseCurrentPage,
        _ChangeLastPage,

        Menu,

        Settings,
        Shop,
        Leader,
        Info,
        Levels,

        Game,
        Win,
        Lose,
        Pause,
        End,

        Main,
        Grade,
        Bonus,
        Inventory,
        Map,

        Privacy,

        Other
    }

    public static UnityEvent<Page> OnShowPage = new();

    public static void ShowPage(Page page)
    {
        OnShowPage.Invoke(page);
    }
}