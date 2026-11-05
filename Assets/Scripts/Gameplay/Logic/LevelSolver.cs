using System.Collections.Generic;
using BlockJam.Gameplay.Data;
using UnityEngine;

namespace BlockJam.Gameplay.Logic
{
    public enum SolveResult
    {
        Solvable,
        Unsolvable,
        TooComplex
    }

    // Breadth-first search over block positions, using the same move rules as the game.
    public static class LevelSolver
    {
        public const int DefaultStateLimit = 200_000;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        // The level must have no validation errors, because the board model expects every block inside the board.
        public static SolveResult Solve(LevelData level, int stateLimit = DefaultStateLimit)
        {
            return TryFindMoves(level, out _, stateLimit);
        }

        // Same search as Solve, but records every TryStep so the path can be replayed on a live board.
        public static SolveResult TryFindMoves(
            LevelData level,
            out List<(int blockId, Vector2Int direction)> moves,
            int stateLimit = DefaultStateLimit)
        {
            moves = new List<(int blockId, Vector2Int direction)>();
            var board = new BoardModel(level);
            int blockCount = board.Blocks.Count;
            var start = new int[blockCount];
            board.CaptureState(start);

            if (!DrainFreeBlocks(board, start, moves))
            {
                return SolveResult.Unsolvable;
            }

            if (board.IsCleared)
            {
                return SolveResult.Solvable;
            }

            var visited = new HashSet<string> { ToKey(start) };
            var pending = new Queue<(int[] state, List<(int blockId, Vector2Int direction)> path)>();
            pending.Enqueue((start, new List<(int blockId, Vector2Int direction)>(moves)));

            while (pending.Count > 0)
            {
                (int[] current, List<(int blockId, Vector2Int direction)> path) = pending.Dequeue();
                for (int blockId = 0; blockId < blockCount; blockId++)
                {
                    foreach (Vector2Int direction in Directions)
                    {
                        board.RestoreState(current);
                        if (!board.TryStep(blockId, direction))
                        {
                            continue;
                        }

                        var next = new int[blockCount];
                        board.CaptureState(next);
                        var nextPath = new List<(int blockId, Vector2Int direction)>(path) { (blockId, direction) };
                        if (!DrainFreeBlocks(board, next, nextPath))
                        {
                            continue;
                        }

                        if (board.IsCleared)
                        {
                            moves = nextPath;
                            return SolveResult.Solvable;
                        }

                        if (!visited.Add(ToKey(next)))
                        {
                            continue;
                        }

                        if (visited.Count > stateLimit)
                        {
                            moves = null;
                            return SolveResult.TooComplex;
                        }

                        pending.Enqueue((next, nextPath));
                    }
                }
            }

            moves = null;
            return SolveResult.Unsolvable;
        }

        // Removes every block that can reach its door alone, appending the concrete TryStep path.
        // Expects board to match state; leaves board matching the updated state. False if a path cannot be rebuilt.
        private static bool DrainFreeBlocks(
            BoardModel board,
            int[] state,
            List<(int blockId, Vector2Int direction)> moves)
        {
            bool removedAny = true;
            while (removedAny)
            {
                removedAny = false;
                for (int blockId = 0; blockId < state.Length; blockId++)
                {
                    if (state[blockId] < 0)
                    {
                        continue;
                    }

                    if (!TryGetExitPath(board, state, blockId, out List<Vector2Int> path))
                    {
                        continue;
                    }

                    foreach (Vector2Int direction in path)
                    {
                        board.RestoreState(state);
                        if (!board.TryStep(blockId, direction))
                        {
                            board.RestoreState(state);
                            return false;
                        }

                        board.CaptureState(state);
                        moves.Add((blockId, direction));
                    }

                    removedAny = true;
                }
            }

            board.RestoreState(state);
            return true;
        }

        // Leaves state/board restored to the entry values when returning false; on true, state still matches entry
        // (path is not applied here).
        private static bool TryGetExitPath(BoardModel board, int[] state, int blockId, out List<Vector2Int> path)
        {
            path = null;
            int startCell = state[blockId];
            var original = (int[])state.Clone();
            var visited = new HashSet<int> { startCell };
            var pending = new Queue<int>();
            var cameFrom = new Dictionary<int, (int prev, Vector2Int direction)>();
            pending.Enqueue(startCell);

            while (pending.Count > 0)
            {
                int cell = pending.Dequeue();
                foreach (Vector2Int direction in Directions)
                {
                    state[blockId] = cell;
                    board.RestoreState(state);
                    if (!board.TryStep(blockId, direction))
                    {
                        continue;
                    }

                    board.CaptureState(state);
                    if (state[blockId] < 0)
                    {
                        path = new List<Vector2Int>();
                        int walk = cell;
                        while (walk != startCell)
                        {
                            (int prev, Vector2Int stepDir) = cameFrom[walk];
                            path.Add(stepDir);
                            walk = prev;
                        }

                        path.Reverse();
                        path.Add(direction);
                        for (int i = 0; i < original.Length; i++)
                        {
                            state[i] = original[i];
                        }

                        board.RestoreState(state);
                        return true;
                    }

                    if (!visited.Add(state[blockId]))
                    {
                        continue;
                    }

                    cameFrom[state[blockId]] = (cell, direction);
                    pending.Enqueue(state[blockId]);
                }
            }

            for (int i = 0; i < original.Length; i++)
            {
                state[i] = original[i];
            }

            board.RestoreState(state);
            return false;
        }

        private static string ToKey(int[] state)
        {
            var key = new char[state.Length];
            for (int i = 0; i < state.Length; i++)
            {
                key[i] = (char)(state[i] + 1);
            }

            return new string(key);
        }
    }
}
