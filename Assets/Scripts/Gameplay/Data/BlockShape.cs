using System.Collections.Generic;
using UnityEngine;

namespace BlockJam.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Block Jam/Block Shape", fileName = "Shape_")]
    public sealed class BlockShape : ScriptableObject
    {
        [Tooltip("Cells relative to the block origin. x goes right, y goes up on the board.")]
        [SerializeField] private Vector2Int[] cells = { Vector2Int.zero };

        public IReadOnlyList<Vector2Int> Cells => cells;
    }
}
