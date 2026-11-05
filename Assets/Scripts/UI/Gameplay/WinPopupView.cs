using System;
using BlockJam.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockJam.UI.Gameplay
{
    public sealed class WinPopupView : UIScreen
    {
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text rewardText;

        public event Action NextClicked;

        private void Awake() => nextButton.onClick.AddListener(() => NextClicked?.Invoke());

        public void SetReward(int coins) => rewardText.SetText("+{0}", coins);
    }
}
