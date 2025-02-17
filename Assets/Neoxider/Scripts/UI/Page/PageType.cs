using UnityEngine;

namespace Neo
{
    namespace UI
    {

        [SerializeField]
        public enum PageType
        {
            None,

            Main,

            Menu,
            Settings,
            Shop,
            Leader,
            Info,
            Levels,

            Grade,
            Bonus,
            Inventory,

            Game,
            HUD,
            Win,
            Lose,
            Pause,
            Finish,
            Map,

            Other

        }
    }
}