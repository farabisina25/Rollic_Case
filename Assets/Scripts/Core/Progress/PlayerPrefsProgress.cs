using System;
using UnityEngine;

namespace BlockJam.Core.Progress
{
    public sealed class PlayerPrefsProgress : IPlayerProgress
    {
        private const string CurrentLevelKey = "progress.currentLevel";
        private const string CoinsKey = "progress.coins";
        private const int DefaultLevel = 1;
        private const int DefaultCoins = 100;
        private const int MaxLives = 5;

        public event Action<int> CoinsChanged;

        public int CurrentLevel => PlayerPrefs.GetInt(CurrentLevelKey, DefaultLevel);
        public int Coins => PlayerPrefs.GetInt(CoinsKey, DefaultCoins);

        // The life system is a placeholder in the case scope, so lives are always full.
        public int Lives => MaxLives;

        public void AddCoins(int amount)
        {
            int coins = Coins + amount;
            PlayerPrefs.SetInt(CoinsKey, coins);
            PlayerPrefs.Save();
            CoinsChanged?.Invoke(coins);
        }

        public void CompleteCurrentLevel()
        {
            PlayerPrefs.SetInt(CurrentLevelKey, CurrentLevel + 1);
            PlayerPrefs.Save();
        }
    }
}
