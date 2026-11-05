using System;
using UnityEngine;

namespace BlockJam.Gameplay.Data
{
    [Serializable]
    public struct DoorPlacement
    {
        public BoardSide side;
        [Tooltip("First cell along the side. Columns for Bottom/Top, rows for Left/Right.")]
        public int start;
        [Min(1)] public int length;
        public BlockColor color;

        public bool Covers(BoardSide doorSide, int index) => side == doorSide && index >= start && index < start + length;
    }
}
