using System;

namespace BlockJam.Core.Progress
{
    public interface IPlayerProgress
    {
        int CurrentLevel { get; }
        int Coins { get; }
        int Lives { get; }

        event Action<int> CoinsChanged;

        void AddCoins(int amount);
        void CompleteCurrentLevel();
    }
}
