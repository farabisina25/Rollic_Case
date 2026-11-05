using System;
using BlockJam.Core.Progress;
using BlockJam.Core.SceneManagement;
using BlockJam.UI.Common;

namespace BlockJam.UI.Home
{
    public sealed class HomePresenter : IDisposable
    {
        private readonly HomeScreenView _view;
        private readonly UIScreen _settingsScreen;
        private readonly IPlayerProgress _progress;

        public HomePresenter(HomeScreenView view, UIScreen settingsScreen, IPlayerProgress progress)
        {
            _view = view;
            _settingsScreen = settingsScreen;
            _progress = progress;

            _view.LevelClicked += OnLevelClicked;
            _view.SettingsClicked += OnSettingsClicked;
            _progress.CoinsChanged += OnCoinsChanged;

            Refresh();
        }

        public void Dispose()
        {
            _view.LevelClicked -= OnLevelClicked;
            _view.SettingsClicked -= OnSettingsClicked;
            _progress.CoinsChanged -= OnCoinsChanged;
        }

        private void Refresh()
        {
            _view.SetLevel(_progress.CurrentLevel);
            _view.SetCoins(_progress.Coins);
            _view.SetLives(_progress.Lives);
        }

        private void OnLevelClicked() => SceneLoader.LoadGameplay();
        private void OnSettingsClicked() => _settingsScreen.Show();
        private void OnCoinsChanged(int coins) => _view.SetCoins(coins);
    }
}
