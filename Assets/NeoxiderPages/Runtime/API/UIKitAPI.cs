using System;
using Neo;
using Neo.Audio;
using Neo.Level;
using Neo.Shop;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Pages
{
    /// <summary>
    /// Статический фасад к игровым событиям/состояниям (через <see cref="GM"/> и <see cref="EM"/>).
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

        /// <summary>
        /// Переходит в меню через <see cref="GM"/>.
        /// </summary>
        public static void GoMenu()
        {
            Debug.Log("[G] " + nameof(GoMenu));

            GM.I?.Menu();
        }

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
        /// Запускает игру через <see cref="GM"/>.
        /// </summary>
        public static void Start()
        {
            Debug.Log("[G] " + nameof(Start));

            GM.I?.StartGame();
        }

        /// <summary>
        /// Перезапускает игру через <see cref="GM"/>.
        /// </summary>
        public static void Restart()
        {
            Debug.Log("[G] " + nameof(Restart));

            GM.I?.StartGame(true);
        }

        /// <summary>
        /// Вызывает победу через <see cref="GM"/>.
        /// </summary>
        public static void Win()
        {
            Debug.Log("[G] " + nameof(Win));

            GM.I?.Win();
        }

        /// <summary>
        /// Вызывает поражение через <see cref="GM"/>.
        /// </summary>
        public static void Lose()
        {
            Debug.Log("[G] " + nameof(Lose));

            GM.I?.Lose();
        }

        /// <summary>
        /// Завершает игру через <see cref="GM"/>.
        /// </summary>
        public static void End()
        {
            Debug.Log("[G] " + nameof(End));

            GM.I?.End();
        }
    }

    /// <summary>
    /// Утилита для выполнения действий, соответствующих игровому состоянию.
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// Типы игровых состояний.
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
        /// Выполняет действие, соответствующее выбранному состоянию.
        /// </summary>
        /// <param name="state">Состояние.</param>
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
    /// Статический фасад к системе денег (<see cref="Money"/>).
    /// </summary>
    public class Wallet
    {
        public static float Value => Money.I.money;
        public static float LevelValue => Money.I.levelMoney;
        public static UnityEvent<float> OnValueChange => Money.I.OnChangedMoney;
        public static UnityEvent<float> OnLevelValueChange => Money.I.OnChangedLevelMoney;
    }

    /// <summary>
    /// Статический фасад к системе очков (<see cref="ScoreManager"/>).
    /// </summary>
    public class Score
    {
        public static int Best => ScoreManager.I.BestScore;
        public static int Current => ScoreManager.I.Score;
        public static UnityEvent<int> OnCurrentChange => ScoreManager.I.OnValueChange;
        public static UnityEvent<int> OnBestChange => ScoreManager.I.OnBestValueChange;
    }

    /// <summary>
    /// Статический фасад к аудио-настройкам (через <see cref="AMSettings"/> и <see cref="AM"/>).
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
        /// Проигрывает стандартный UI звук (клип с ID=0) через <see cref="AM"/>.
        /// </summary>
        public static void PlayUI()
        {
            AM.I?.Play(0);
        }
    }

    /// <summary>
    /// Статический фасад к уровням (через <see cref="LevelManager"/>).
    /// </summary>
    public class Level
    {
        public static int Current => LevelManager.I.CurrentLevel;
        public static int Best => LevelManager.I.MaxLevel;

        public static UnityEvent<int> OnCurrentChange => LevelManager.I.OnChangeLevel;
        public static UnityEvent<int> OnBestChange => LevelManager.I.OnChangeMaxLevel;
    }
}