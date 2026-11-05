using BlockJam.Gameplay.Data;

namespace BlockJam.Gameplay
{
    // Set by the level editor's Play button so the Gameplay scene opens the level being edited.
    public static class PlaytestLevel
    {
#if UNITY_EDITOR
        public static LevelData Override { get; set; }
#else
        public static LevelData Override => null;
#endif
    }
}
