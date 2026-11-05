using System.Collections.Generic;
using BlockJam.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace BlockJam.UI.Settings
{
    public sealed class SettingsScreenView : UIScreen
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimButton;
        [SerializeField] private SettingToggleView[] toggles;

        public IReadOnlyList<SettingToggleView> Toggles => toggles;

        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            dimButton.onClick.AddListener(Hide);
        }
    }
}
