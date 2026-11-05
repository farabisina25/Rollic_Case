using System;
using BlockJam.Core.Progress;
using BlockJam.Core.SceneManagement;
using BlockJam.Gameplay.Logic;
using BlockJam.Gameplay.Presentation;

namespace BlockJam.UI.Gameplay
{
    public sealed class GameplayPresenter : IDisposable
    {
        private enum State
        {
            Playing,
            Paused,
            Won,
            Failed
        }

        private readonly GameplayHudView _hud;
        private readonly PausePopupView _pausePopup;
        private readonly FailPopupView _failPopup;
        private readonly WinPopupView _winPopup;
        private readonly BoardModel _board;
        private readonly BoardPresenter _boardPresenter;
        private readonly LevelTimer _timer;
        private readonly IPlayerProgress _progress;
        private readonly int _coinReward;

        private State _state = State.Playing;

        public GameplayPresenter(
            GameplayHudView hud,
            PausePopupView pausePopup,
            FailPopupView failPopup,
            WinPopupView winPopup,
            BoardModel board,
            BoardPresenter boardPresenter,
            LevelTimer timer,
            IPlayerProgress progress,
            int coinReward)
        {
            _hud = hud;
            _pausePopup = pausePopup;
            _failPopup = failPopup;
            _winPopup = winPopup;
            _board = board;
            _boardPresenter = boardPresenter;
            _timer = timer;
            _progress = progress;
            _coinReward = coinReward;

            _hud.PauseClicked += OnPauseClicked;
            _pausePopup.ResumeClicked += OnResumeClicked;
            _pausePopup.RestartClicked += SceneLoader.LoadGameplay;
            _pausePopup.HomeClicked += SceneLoader.LoadMenu;
            _failPopup.RestartClicked += SceneLoader.LoadGameplay;
            _failPopup.HomeClicked += SceneLoader.LoadMenu;
            _winPopup.NextClicked += SceneLoader.LoadGameplay;
            _board.Cleared += OnBoardCleared;
            _timer.SecondsChanged += _hud.SetTime;
            _timer.Expired += OnTimerExpired;
            _progress.CoinsChanged += _hud.SetCoins;

            _hud.SetLevel(_progress.CurrentLevel);
            _hud.SetCoins(_progress.Coins);
            _hud.SetTime(_timer.RemainingSeconds);
        }

        public void Dispose()
        {
            _hud.PauseClicked -= OnPauseClicked;
            _pausePopup.ResumeClicked -= OnResumeClicked;
            _pausePopup.RestartClicked -= SceneLoader.LoadGameplay;
            _pausePopup.HomeClicked -= SceneLoader.LoadMenu;
            _failPopup.RestartClicked -= SceneLoader.LoadGameplay;
            _failPopup.HomeClicked -= SceneLoader.LoadMenu;
            _winPopup.NextClicked -= SceneLoader.LoadGameplay;
            _board.Cleared -= OnBoardCleared;
            _timer.SecondsChanged -= _hud.SetTime;
            _timer.Expired -= OnTimerExpired;
            _progress.CoinsChanged -= _hud.SetCoins;
        }

        public void Tick(float deltaTime)
        {
            if (_state == State.Playing)
            {
                _timer.Tick(deltaTime);
            }
        }

        private void OnPauseClicked()
        {
            if (_state != State.Playing)
            {
                return;
            }

            SetState(State.Paused);
            _pausePopup.Show();
        }

        private void OnResumeClicked()
        {
            _pausePopup.Hide();
            SetState(State.Playing);
        }

        private void OnBoardCleared()
        {
            SetState(State.Won);
            _progress.AddCoins(_coinReward);
            _progress.CompleteCurrentLevel();
            _winPopup.SetReward(_coinReward);
            _winPopup.Show();
        }

        private void OnTimerExpired()
        {
            SetState(State.Failed);
            _failPopup.Show();
        }

        private void SetState(State state)
        {
            _state = state;
            _boardPresenter.IsInputEnabled = state == State.Playing;
        }
    }
}
