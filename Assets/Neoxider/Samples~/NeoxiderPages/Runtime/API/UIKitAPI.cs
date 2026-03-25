using Neo.Audio;
using Neo.Level;
using Neo.Shop;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Pages
{
    /// <summary>
    ///     Static facade for game events/state (via <see cref="GM" /> and <see cref="EM" />).
    /// </summary>
    public class G
    {
        public static UnityEvent OnMenu => EM.I?.OnMenu;
        public static UnityEvent OnStart => EM.I?.OnGameStart;
        public static UnityEvent OnRestart => EM.I?.OnRestart;
        public static UnityEvent OnPause => EM.I?.OnPause;
        public static UnityEvent OnResume => EM.I?.OnResume;
        public static UnityEvent OnWin => EM.I?.OnWin;
        public static UnityEvent OnLose => EM.I?.OnLose;
        public static UnityEvent OnEnd => EM.I?.OnEnd;
        public static bool Inited => GM.I != null;

        public static bool Pause
        {
            get => GM.I.State == GM.GameState.Pause;
            set
            {
                if (value)
                {
                    Debug.Log("[G] " + nameof(Pause));

                    GM.I?.Pause();
                }
                else
                {
                    Debug.Log("[G] " + "UnPause");

                    GM.I?.Resume();
                }
            }
        }

        /// <summary>
        ///     Goes to the menu via <see cref="GM" />.
        /// </summary>
        public static void GoMenu()
        {
            Debug.Log("[G] " + nameof(GoMenu));

            GM.I?.Menu();
        }

        /// <summary>
        ///     Starts the game via <see cref="GM" />.
        /// </summary>
        public static void Start()
        {
            Debug.Log("[G] " + nameof(Start));

            GM.I?.StartGame();
        }

        /// <summary>
        ///     Restarts the game via <see cref="GM" />.
        /// </summary>
        public static void Restart()
        {
            Debug.Log("[G] " + nameof(Restart));

            GM.I?.StartGame(true);
        }

        /// <summary>
        ///     Triggers a win via <see cref="GM" />.
        /// </summary>
        public static void Win()
        {
            Debug.Log("[G] " + nameof(Win));

            GM.I?.Win();
        }

        /// <summary>
        ///     Triggers a loss via <see cref="GM" />.
        /// </summary>
        public static void Lose()
        {
            Debug.Log("[G] " + nameof(Lose));

            GM.I?.Lose();
        }

        /// <summary>
        ///     Ends the game via <see cref="GM" />.
        /// </summary>
        public static void End()
        {
            Debug.Log("[G] " + nameof(End));

            GM.I?.End();
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
    ///     Static facade for the money system (<see cref="Money" />).
    /// </summary>
    public class Wallet
    {
        public static float Value => Money.I.money;
        public static float LevelValue => Money.I.levelMoney;
        public static UnityEvent<float> OnValueChange => Money.I.CurrentMoney.OnChanged;
        public static UnityEvent<float> OnLevelValueChange => Money.I.LevelMoney.OnChanged;
    }

    /// <summary>
    ///     Static facade for the score system (<see cref="ScoreManager" />).
    /// </summary>
    public class Score
    {
        public static int Best => ScoreManager.I.BestScoreValue;
        public static int Current => ScoreManager.I.ScoreValue;
        public static UnityEvent<int> OnCurrentChange => ScoreManager.I.Score.OnChanged;
        public static UnityEvent<int> OnBestChange => ScoreManager.I.BestScore.OnChanged;
    }

    /// <summary>
    ///     Static facade for audio settings (via <see cref="AMSettings" /> and <see cref="AM" />).
    /// </summary>
    public class Audio
    {
        public static bool IsActiveSound
        {
            get => AMSettings.I.IsActiveEfx;
            set => AMSettings.I?.SetEfx(value);
        }

        public static bool IsActiveMusic
        {
            get => AMSettings.I.IsActiveMusic;
            set => AMSettings.I?.SetMusic(value);
        }

        /// <summary>
        ///     Plays the default UI sound (clip with ID=0) via <see cref="AM" />.
        /// </summary>
        public static void PlayUI()
        {
            AM.I?.Play(0);
        }
    }

    /// <summary>
    ///     Static facade for levels (via <see cref="LevelManager" />).
    /// </summary>
    public class Level
    {
        public static int Current => LevelManager.I.CurrentLevel;
        public static int Best => LevelManager.I.MaxLevel;

        public static UnityEvent<int> OnCurrentChange => LevelManager.I.OnChangeLevel;
        public static UnityEvent<int> OnBestChange => LevelManager.I.OnChangeMaxLevel;
    }
}