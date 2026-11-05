using System;
using BlockJam.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace BlockJam.UI.Gameplay
{
    public sealed class FailPopupView : UIScreen
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;

        public event Action RestartClicked;
        public event Action HomeClicked;

        private void Awake()
        {
            restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            homeButton.onClick.AddListener(() => HomeClicked?.Invoke());
        }
    }
}
