using Neo.Audio;
using Neo.Level;
using Neo.Shop;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Pages
{
    /// <summary>
    ///     Static facade for game events/state (via <see cref="GM" /> and <see cref="EM" />). Without an EM the event
    ///     getters return null; without a GM the commands do nothing.
    /// </summary>
    /// <remarks>
    ///     WHY: every manager is checked with Unity's equality, not <c>?.</c>: a destroyed singleton is a fake-null
    ///     object that <c>?.</c> does not catch.
    /// </remarks>
    public class G
    {
        public static UnityEvent OnMenu => Events() is EM em ? em.OnMenu : null;
        public static UnityEvent OnStart => Events() is EM em ? em.OnGameStart : null;
        public static UnityEvent OnRestart => Events() is EM em ? em.OnRestart : null;
        public static UnityEvent OnPause => Events() is EM em ? em.OnPause : null;
        public static UnityEvent OnResume => Events() is EM em ? em.OnResume : null;
        public static UnityEvent OnWin => Events() is EM em ? em.OnWin : null;
        public static UnityEvent OnLose => Events() is EM em ? em.OnLose : null;
        public static UnityEvent OnEnd => Events() is EM em ? em.OnEnd : null;
        public static bool Inited => GM.I != null;

        public static bool Pause
        {
            get
            {
                GM gm = GM.I;
                return gm != null && gm.State == GM.GameState.Pause;
            }
            set
            {
                GM gm = GM.I;
                if (gm == null)
                {
                    return;
                }

                if (value)
                {
                    gm.Pause();
                }
                else
                {
                    gm.Resume();
                }
            }
        }

        /// <summary>
        ///     Goes to the menu via <see cref="GM" />.
        /// </summary>
        public static void GoMenu()
        {
            GM gm = GM.I;
            if (gm != null)
            {
                gm.Menu();
            }
        }

        /// <summary>
        ///     Starts the game via <see cref="GM" />.
        /// </summary>
        public static void Start()
        {
            GM gm = GM.I;
            if (gm != null)
            {
                gm.StartGame();
            }
        }

        /// <summary>
        ///     Restarts the game via <see cref="GM" />.
        /// </summary>
        public static void Restart()
        {
            GM gm = GM.I;
            if (gm != null)
            {
                gm.StartGame(true);
            }
        }

        /// <summary>
        ///     Triggers a win via <see cref="GM" />.
        /// </summary>
        public static void Win()
        {
            GM gm = GM.I;
            if (gm != null)
            {
                gm.Win();
            }
        }

        /// <summary>
        ///     Triggers a loss via <see cref="GM" />.
        /// </summary>
        public static void Lose()
        {
            GM gm = GM.I;
            if (gm != null)
            {
                gm.Lose();
            }
        }

        /// <summary>
        ///     Ends the game via <see cref="GM" />.
        /// </summary>
        public static void End()
        {
            GM gm = GM.I;
            if (gm != null)
            {
                gm.End();
            }
        }

        private static EM Events()
        {
            EM em = EM.I;
            // WHY: turns a destroyed (fake-null) EM into a real null for the `is` patterns above.
            return em != null ? em : null;
        }
    }

    /// <summary>
    ///     Utility to perform actions that match a game state.
    /// </summary>
    public class GameState
    {
        /// <summary>
        ///     Game state kinds.
        /// </summary>
        public enum State
        {
            None,
            Menu,
            Start,
            Restart,
            Pause,
            Resume,
            Win,
            Lose,
            End
        }

        /// <summary>
        ///     Performs the action for the selected state.
        /// </summary>
        /// <param name="state">State.</param>
        public static void Set(State state)
        {
            switch (state)
            {
                case State.None:
                    break;
                case State.Menu:
                    G.GoMenu();
                    break;
                case State.Start:
                    G.Start();
                    break;
                case State.Restart:
                    G.Restart();
                    break;
                case State.Pause:
                    G.Pause = true;
                    break;
                case State.Resume:
                    G.Pause = false;
                    break;
                case State.Win:
                    G.Win();
                    break;
                case State.Lose:
                    G.Lose();
                    break;
                case State.End:
                    G.End();
                    break;
            }
        }
    }

    /// <summary>
    ///     Static facade for the money system (<see cref="Money" />). Without a Money in the scene the values read
    ///     as 0 and the events as null.
    /// </summary>
    public class Wallet
    {
        /// <summary>True when a <see cref="Money" /> instance is available.</summary>
        public static bool IsAvailable => Money.I != null;

        public static float Value
        {
            get
            {
                Money wallet = Money.I;
                return wallet != null ? wallet.money : 0f;
            }
        }

        public static float LevelValue
        {
            get
            {
                Money wallet = Money.I;
                return wallet != null ? wallet.levelMoney : 0f;
            }
        }

        public static UnityEvent<float> OnValueChange
        {
            get
            {
                Money wallet = Money.I;
                return wallet != null ? wallet.CurrentMoney.OnChanged : null;
            }
        }

        public static UnityEvent<float> OnLevelValueChange
        {
            get
            {
                Money wallet = Money.I;
                return wallet != null ? wallet.LevelMoney.OnChanged : null;
            }
        }
    }

    /// <summary>
    ///     Static facade for the score system (<see cref="ScoreManager" />). Without a ScoreManager in the scene the
    ///     values read as 0 and the events as null.
    /// </summary>
    public class Score
    {
        /// <summary>True when a <see cref="ScoreManager" /> instance is available.</summary>
        public static bool IsAvailable => ScoreManager.I != null;

        public static int Best
        {
            get
            {
                ScoreManager scores = ScoreManager.I;
                return scores != null ? scores.BestScoreValue : 0;
            }
        }

        public static int Current
        {
            get
            {
                ScoreManager scores = ScoreManager.I;
                return scores != null ? scores.ScoreValue : 0;
            }
        }

        public static UnityEvent<int> OnCurrentChange
        {
            get
            {
                ScoreManager scores = ScoreManager.I;
                return scores != null ? scores.Score.OnChanged : null;
            }
        }

        public static UnityEvent<int> OnBestChange
        {
            get
            {
                ScoreManager scores = ScoreManager.I;
                return scores != null ? scores.BestScore.OnChanged : null;
            }
        }
    }

    /// <summary>
    ///     Static facade for audio settings (via <see cref="AMSettings" /> and <see cref="AM" />). Without an
    ///     AMSettings in the scene the switches read as on and writes are ignored.
    /// </summary>
    public class Audio
    {
        /// <summary>True when an <see cref="AMSettings" /> instance is available.</summary>
        public static bool IsAvailable => AMSettings.I != null;

        public static bool IsActiveSound
        {
            get
            {
                AMSettings settings = AMSettings.I;
                return settings == null || settings.IsActiveEfx;
            }
            set
            {
                AMSettings settings = AMSettings.I;
                if (settings != null)
                {
                    settings.SetEfx(value);
                }
            }
        }

        public static bool IsActiveMusic
        {
            get
            {
                AMSettings settings = AMSettings.I;
                return settings == null || settings.IsActiveMusic;
            }
            set
            {
                AMSettings settings = AMSettings.I;
                if (settings != null)
                {
                    settings.SetMusic(value);
                }
            }
        }

        /// <summary>
        ///     Plays the default UI sound (clip with ID=0) via <see cref="AM" />.
        /// </summary>
        public static void PlayUI()
        {
            AM am = AM.I;
            if (am != null)
            {
                am.Play(0);
            }
        }
    }

    /// <summary>
    ///     Static facade for levels (via <see cref="LevelManager" />). Without a LevelManager in the scene the values
    ///     read as 0 and the events as null.
    /// </summary>
    public class Level
    {
        /// <summary>True when a <see cref="LevelManager" /> instance is available.</summary>
        public static bool IsAvailable => LevelManager.I != null;

        public static int Current
        {
            get
            {
                LevelManager levels = LevelManager.I;
                return levels != null ? levels.CurrentLevel : 0;
            }
        }

        public static int Best
        {
            get
            {
                LevelManager levels = LevelManager.I;
                return levels != null ? levels.MaxLevel : 0;
            }
        }

        public static UnityEvent<int> OnCurrentChange
        {
            get
            {
                LevelManager levels = LevelManager.I;
                return levels != null ? levels.OnChangeLevel : null;
            }
        }

        public static UnityEvent<int> OnBestChange
        {
            get
            {
                LevelManager levels = LevelManager.I;
                return levels != null ? levels.OnChangeMaxLevel : null;
            }
        }
    }
}
