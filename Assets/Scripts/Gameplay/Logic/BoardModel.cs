using System;
using System.Collections.Generic;
using BlockJam.Gameplay.Data;
using UnityEngine;

namespace BlockJam.Gameplay.Logic
{
    public sealed class BoardModel
    {
        private const int EmptyCell = -1;

        private readonly int[] _occupancy;
        private readonly BoardBlock[] _blocks;
        private readonly IReadOnlyList<DoorPlacement> _doors;
        private int _remainingBlocks;

        public event Action<BoardBlock> BlockMoved;
        public event Action<BoardBlock, Vector2Int> BlockExited;
        public event Action Cleared;

        public BoardModel(LevelData level)
        {
            Width = level.Width;
            Height = level.Height;
            _doors = level.Doors;
            _occupancy = new int[Width * Height];
            Array.Fill(_occupancy, EmptyCell);

            _blocks = new BoardBlock[level.Blocks.Count];
            for (int i = 0; i < _blocks.Length; i++)
            {
                _blocks[i] = new BoardBlock(i, level.Blocks[i]);
                Occupy(_blocks[i], i);
            }

            _remainingBlocks = _blocks.Length;
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<BoardBlock> Blocks => _blocks;
        public IReadOnlyList<DoorPlacement> Doors => _doors;

        // Moves the block one cell. Returns false when a wall, a wrong door or another block is in the way.
        public bool TryStep(int blockId, Vector2Int direction)
        {
            BoardBlock block = _blocks[blockId];
            if (block.HasExited)
            {
                return false;
            }

            bool reachesEdge = false;
            foreach (Vector2Int offset in block.Cells)
            {
                Vector2Int target = block.Origin + offset + direction;
                if (!IsInside(target))
                {
                    reachesEdge = true;
                    continue;
                }

                int occupant = _occupancy[ToIndex(target)];
                if (occupant != EmptyCell && occupant != blockId)
                {
                    return false;
                }
            }

            if (reachesEdge)
            {
                return TryExit(block, direction);
            }

            Occupy(block, EmptyCell);
            block.Origin += direction;
            Occupy(block, blockId);
            BlockMoved?.Invoke(block);
            return true;
        }

        private bool TryExit(BoardBlock block, Vector2Int direction)
        {
            if (!CanExit(block, direction))
            {
                return false;
            }

            Occupy(block, EmptyCell);
            block.HasExited = true;
            _remainingBlocks--;
            BlockExited?.Invoke(block, direction);

            if (_remainingBlocks == 0)
            {
                Cleared?.Invoke();
            }

            return true;
        }

        // Every lane the block covers must face a door of its color, and the path to that door must be free.
        private bool CanExit(BoardBlock block, Vector2Int direction)
        {
            BoardSide side = ToSide(direction);
            foreach (Vector2Int offset in block.Cells)
            {
                Vector2Int cell = block.Origin + offset;
                int lane = direction.x != 0 ? cell.y : cell.x;
                if (!HasDoor(side, lane, block.Color) || !IsPathClear(cell + direction, direction, block.Id))
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasDoor(BoardSide side, int lane, BlockColor color)
        {
            foreach (DoorPlacement door in _doors)
            {
                if (door.color == color && door.Covers(side, lane))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPathClear(Vector2Int cell, Vector2Int direction, int blockId)
        {
            for (; IsInside(cell); cell += direction)
            {
                int occupant = _occupancy[ToIndex(cell)];
                if (occupant != EmptyCell && occupant != blockId)
                {
                    return false;
                }
            }

            return true;
        }

        private void Occupy(BoardBlock block, int value)
        {
            foreach (Vector2Int offset in block.Cells)
            {
                _occupancy[ToIndex(block.Origin + offset)] = value;
            }
        }

        private bool IsInside(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;
        private int ToIndex(Vector2Int cell) => cell.y * Width + cell.x;

        private static BoardSide ToSide(Vector2Int direction)
        {
            if (direction.x < 0) return BoardSide.Left;
            if (direction.x > 0) return BoardSide.Right;
            return direction.y < 0 ? BoardSide.Bottom : BoardSide.Top;
        }
    }
}
