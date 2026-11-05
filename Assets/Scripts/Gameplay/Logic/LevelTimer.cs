using System;
using UnityEngine;

namespace BlockJam.Gameplay.Logic
{
    public sealed class LevelTimer
    {
        private float _remaining;
        private int _shownSeconds;

        public event Action<int> SecondsChanged;
        public event Action Expired;

        public LevelTimer(int seconds)
        {
            _remaining = seconds;
            _shownSeconds = seconds;
        }

        public int RemainingSeconds => _shownSeconds;
        public bool IsExpired => _remaining <= 0f;

        public void Tick(float deltaTime)
        {
            if (IsExpired)
            {
                return;
            }

            _remaining = Mathf.Max(0f, _remaining - deltaTime);

            int seconds = Mathf.CeilToInt(_remaining);
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                SecondsChanged?.Invoke(seconds);
            }

            if (IsExpired)
            {
                Expired?.Invoke();
            }
        }
    }
}
