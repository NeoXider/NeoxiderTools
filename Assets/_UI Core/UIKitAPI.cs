using System;
using Neo;
using Neo.Audio;
using Neo.Level;
using Neo.Shop;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

public class Wallet
{
    public static float Value => Money.I.money;
    public static float LevelValue => Money.I.levelMoney;
    public static UnityEvent<float> OnValueChange => Money.I.OnChangedMoney;
    public static UnityEvent<float> OnLevelValueChange => Money.I.OnChangedLevelMoney;
}

public class Score
{
    public static int Best => ScoreManager.I.BestScore;
    public static int Current => ScoreManager.I.Score;
    public static UnityEvent<int> OnCurrentChange => ScoreManager.I.OnValueChange;
    public static UnityEvent<int> OnBestChange => ScoreManager.I.OnBestValueChange;
}

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

    public static void Start()
    {
        Debug.Log("[G] " + nameof(Start));

        GM.I?.StartGame();
    }

    public static void Restart()
    {
        Debug.Log("[G] " + nameof(Restart));

        GM.I?.StartGame(true);
    }

    public static void Win()
    {
        Debug.Log("[G] " + nameof(Win));

        GM.I?.Win();
    }

    public static void Lose()
    {
        Debug.Log("[G] " + nameof(Lose));

        GM.I?.Lose();
    }

    public static void End()
    {
        Debug.Log("[G] " + nameof(End));

        GM.I?.End();
    }
}

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

    public static void PlayUI()
    {
        AM.I?.Play(0);
    }
}

public class Level
{
    public static int Current => LevelManager.I.CurrentLevel;
    public static int Best => LevelManager.I.MaxLevel;

    public static UnityEvent<int> OnCurrentChange => LevelManager.I.OnChangeLevel;
    public static UnityEvent<int> OnBestChange => LevelManager.I.OnChangeMaxLevel;
}


public class GameState
{
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