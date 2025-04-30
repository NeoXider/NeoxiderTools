using System;

namespace Neo
{
    public interface IGameState
    {
        void Prepare();
        void StartGame();
        void StopGame();
        void Lose();
        void Win();
        void Pause();
        void Resume();
    }
}