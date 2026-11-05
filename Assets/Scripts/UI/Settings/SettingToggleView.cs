using System;
using BlockJam.Core.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockJam.UI.Settings
{
    public sealed class SettingToggleView : MonoBehaviour
    {
        [SerializeField] private SettingType type;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text offLabel;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Color offTint = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField, Range(0f, 1f)] private float offIconAlpha = 0.4f;

        public SettingType Type => type;

        public event Action<SettingType> Clicked;

        private void Awake()
        {
            button.onClick.AddListener(() => Clicked?.Invoke(type));
        }

        public void SetState(bool isOn)
        {
            background.sprite = isOn ? onSprite : offSprite;
            background.color = isOn ? Color.white : offTint;
            offLabel.gameObject.SetActive(!isOn);

            Color iconColor = icon.color;
            iconColor.a = isOn ? 1f : offIconAlpha;
            icon.color = iconColor;
        }
    }
}
