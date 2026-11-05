using BlockJam.Core.Progress;
using BlockJam.Gameplay.Data;
using BlockJam.Gameplay.Logic;
using BlockJam.Gameplay.Presentation;
using BlockJam.Gameplay.View;
using BlockJam.UI.Gameplay;
using UnityEngine;

namespace BlockJam.Gameplay
{
    public sealed class GameplayInstaller : MonoBehaviour
    {
        [SerializeField] private LevelCatalog levelCatalog;

        [Header("Board")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private BlockDragInput dragInput;
        [SerializeField] private BoardCameraFitter cameraFitter;

        [Header("UI")]
        [SerializeField] private GameplayHudView hud;
        [SerializeField] private PausePopupView pausePopup;
        [SerializeField] private FailPopupView failPopup;
        [SerializeField] private WinPopupView winPopup;

        private BoardPresenter _boardPresenter;
        private GameplayPresenter _gameplayPresenter;

        private void Start()
        {
            var progress = new PlayerPrefsProgress();
            LevelData level = PlaytestLevel.Override ?? levelCatalog.GetLevel(progress.CurrentLevel);

            var board = new BoardModel(level);
            var layout = new BoardLayout(level.Width, level.Height);
            cameraFitter.Fit(boardView.transform, layout.Size);

            _boardPresenter = new BoardPresenter(boardView, dragInput, board, layout);
            _gameplayPresenter = new GameplayPresenter(
                hud, pausePopup, failPopup, winPopup,
                board, _boardPresenter, new LevelTimer(level.TimeLimitSeconds), progress, level.CoinReward);
        }

        private void Update() => _gameplayPresenter?.Tick(Time.deltaTime);

        private void OnDestroy()
        {
            _gameplayPresenter?.Dispose();
            _boardPresenter?.Dispose();
        }
    }
}
