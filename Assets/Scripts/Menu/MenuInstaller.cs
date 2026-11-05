using BlockJam.Core.Progress;
using BlockJam.Core.Settings;
using BlockJam.UI.Home;
using BlockJam.UI.Settings;
using UnityEngine;

namespace BlockJam.Menu
{
    public sealed class MenuInstaller : MonoBehaviour
    {
        [SerializeField] private HomeScreenView homeView;
        [SerializeField] private SettingsScreenView settingsScreen;

        private HomePresenter _homePresenter;
        private SettingsPresenter _settingsPresenter;

        private void Start()
        {
            _homePresenter = new HomePresenter(homeView, settingsScreen, new PlayerPrefsProgress());
            _settingsPresenter = new SettingsPresenter(settingsScreen, new PlayerPrefsGameSettings());
        }

        private void OnDestroy()
        {
            _homePresenter?.Dispose();
            _settingsPresenter?.Dispose();
        }
    }
}
