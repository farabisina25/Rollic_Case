using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockJam.UI.Home
{
    public sealed class HomeScreenView : MonoBehaviour
    {
        [SerializeField] private Button levelButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text livesText;

        public event Action LevelClicked;
        public event Action SettingsClicked;

        private void Awake()
        {
            levelButton.onClick.AddListener(() => LevelClicked?.Invoke());
            settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
        }

        public void SetLevel(int level) => levelText.SetText("Level {0}", level);
        public void SetCoins(int coins) => coinText.SetText("{0}", coins);
        public void SetLives(int lives) => livesText.SetText("{0}", lives);
    }
}
