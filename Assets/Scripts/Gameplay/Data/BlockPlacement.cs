using System;
using UnityEngine;

namespace BlockJam.Gameplay.Data
{
    [Serializable]
    public struct BlockPlacement
    {
        public BlockShape shape;
        public BlockColor color;
        public Vector2Int origin;
    }
}
