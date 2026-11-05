using System;
using BlockJam.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace BlockJam.UI.Gameplay
{
    public sealed class PausePopupView : UIScreen
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;

        public event Action ResumeClicked;
        public event Action RestartClicked;
        public event Action HomeClicked;

        private void Awake()
        {
            resumeButton.onClick.AddListener(() => ResumeClicked?.Invoke());
            closeButton.onClick.AddListener(() => ResumeClicked?.Invoke());
            restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            homeButton.onClick.AddListener(() => HomeClicked?.Invoke());
        }
    }
}
