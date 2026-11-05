using System.Collections.Generic;
using BlockJam.Gameplay.Data;
using UnityEngine;

namespace BlockJam.Gameplay.Logic
{
    public sealed class BoardBlock
    {
        public BoardBlock(int id, BlockPlacement placement)
        {
            Id = id;
            Cells = placement.shape.Cells;
            Color = placement.color;
            Origin = placement.origin;
        }

        public int Id { get; }
        public IReadOnlyList<Vector2Int> Cells { get; }
        public BlockColor Color { get; }
        public Vector2Int Origin { get; internal set; }
        public bool HasExited { get; internal set; }
    }
}
