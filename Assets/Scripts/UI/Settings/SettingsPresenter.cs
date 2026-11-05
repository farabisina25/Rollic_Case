using System;
using System.Collections.Generic;
using BlockJam.Core.Settings;

namespace BlockJam.UI.Settings
{
    public sealed class SettingsPresenter : IDisposable
    {
        private readonly IReadOnlyList<SettingToggleView> _toggles;
        private readonly IGameSettings _settings;

        public SettingsPresenter(SettingsScreenView view, IGameSettings settings)
        {
            _toggles = view.Toggles;
            _settings = settings;

            foreach (SettingToggleView toggle in _toggles)
            {
                toggle.Clicked += OnToggleClicked;
                toggle.SetState(_settings.IsEnabled(toggle.Type));
            }

            _settings.Changed += OnSettingChanged;
        }

        public void Dispose()
        {
            foreach (SettingToggleView toggle in _toggles)
            {
                toggle.Clicked -= OnToggleClicked;
            }

            _settings.Changed -= OnSettingChanged;
        }

        private void OnToggleClicked(SettingType type) => _settings.SetEnabled(type, !_settings.IsEnabled(type));

        private void OnSettingChanged(SettingType type, bool isEnabled)
        {
            foreach (SettingToggleView toggle in _toggles)
            {
                if (toggle.Type == type)
                {
                    toggle.SetState(isEnabled);
                }
            }
        }
    }
}
