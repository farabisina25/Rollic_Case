using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockJam.UI.Gameplay
{
    public sealed class GameplayHudView : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text coinText;

        public event Action PauseClicked;

        private void Awake() => pauseButton.onClick.AddListener(() => PauseClicked?.Invoke());

        public void SetLevel(int level) => levelText.SetText("Level {0}", level);
        public void SetCoins(int coins) => coinText.SetText("{0}", coins);
        public void SetTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.SetText(seconds < 10 ? "{0}:0{1}" : "{0}:{1}", minutes, seconds);
        }
    }
}
