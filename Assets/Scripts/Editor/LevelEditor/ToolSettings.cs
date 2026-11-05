using System;
using BlockJam.Gameplay.Data;

namespace BlockJam.Editor.LevelEditor
{
    internal enum EditTool
    {
        Block,
        Door,
        Erase
    }

    // What a left click on the board does. Serializable so the window keeps it across domain reloads.
    [Serializable]
    internal sealed class ToolSettings
    {
        public const int MaxDoorLength = 3;

        public EditTool tool;
        public BlockColor color;
        public BlockShape shape;
        public int doorLength = 1;
    }
}
