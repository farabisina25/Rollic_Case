using System;

namespace BlockJam.Core.Settings
{
    public interface IGameSettings
    {
        event Action<SettingType, bool> Changed;

        bool IsEnabled(SettingType type);
        void SetEnabled(SettingType type, bool isEnabled);
    }
}
