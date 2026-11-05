using System;
using UnityEngine;

namespace BlockJam.Core.Settings
{
    public sealed class PlayerPrefsGameSettings : IGameSettings
    {
        private const int Enabled = 1;
        private const int Disabled = 0;

        public event Action<SettingType, bool> Changed;

        public bool IsEnabled(SettingType type) => PlayerPrefs.GetInt(GetKey(type), Enabled) == Enabled;

        public void SetEnabled(SettingType type, bool isEnabled)
        {
            PlayerPrefs.SetInt(GetKey(type), isEnabled ? Enabled : Disabled);
            PlayerPrefs.Save();
            Changed?.Invoke(type, isEnabled);
        }

        private static string GetKey(SettingType type) => type switch
        {
            SettingType.Sound => "settings.sound",
            SettingType.Music => "settings.music",
            SettingType.Vibration => "settings.vibration",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}
