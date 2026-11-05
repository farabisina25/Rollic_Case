using System.Collections.Generic;
using BlockJam.Gameplay.Data;
using UnityEngine;

namespace BlockJam.Gameplay.Logic
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public readonly struct ValidationMessage
    {
        public ValidationMessage(ValidationSeverity severity, string text)
        {
            Severity = severity;
            Text = text;
        }

        public ValidationSeverity Severity { get; }
        public string Text { get; }
    }

    public sealed class ValidationReport
    {
        private readonly List<ValidationMessage> _messages = new List<ValidationMessage>();

        public IReadOnlyList<ValidationMessage> Messages => _messages;
        public bool HasErrors { get; private set; }
        public SolveResult? Solvability { get; private set; }

        internal void Add(ValidationSeverity severity, string text)
        {
            _messages.Add(new ValidationMessage(severity, text));
            HasErrors |= severity == ValidationSeverity.Error;
        }

        internal void SetSolveResult(SolveResult result)
        {
            Solvability = result;
            switch (result)
            {
                case SolveResult.Solvable:
                    Add(ValidationSeverity.Info, "The level can be cleared.");
                    break;
                case SolveResult.Unsolvable:
                    Add(ValidationSeverity.Warning, "No sequence of moves clears this level.");
                    break;
                case SolveResult.TooComplex:
                    Add(ValidationSeverity.Warning, "The level is too big to check. It may not be solvable.");
                    break;
            }
        }
    }

    public static class LevelValidator
    {
        // Runs the layout checks first. The solver only runs on a layout without errors.
        public static ValidationReport Validate(LevelData level, bool checkSolvable)
        {
            var report = new ValidationReport();
            CheckBlocks(level, report);
            CheckDoors(level, report);

            if (level.Blocks.Count == 0)
            {
                report.Add(ValidationSeverity.Warning, "The level has no blocks.");
            }

            if (checkSolvable && !report.HasErrors)
            {
                report.SetSolveResult(LevelSolver.Solve(level));
            }

            return report;
        }

        private static void CheckBlocks(LevelData level, ValidationReport report)
        {
            var owners = new int[level.Width * level.Height];
            for (int i = 0; i < level.Blocks.Count; i++)
            {
                BlockPlacement block = level.Blocks[i];
                string label = $"Block {i + 1} ({block.color})";
                if (block.shape == null)
                {
                    report.Add(ValidationSeverity.Error, $"{label} has no shape.");
                    continue;
                }

                if (!IsInside(level, block.origin))
                {
                    report.Add(ValidationSeverity.Error, $"{label} is outside the board.");
                    continue;
                }

                foreach (Vector2Int offset in block.shape.Cells)
                {
                    Vector2Int cell = block.origin + offset;
                    if (!IsInside(level, cell))
                    {
                        report.Add(ValidationSeverity.Error, $"{label} is outside the board.");
                        break;
                    }

                    int index = cell.y * level.Width + cell.x;
                    if (owners[index] != 0)
                    {
                        report.Add(ValidationSeverity.Error, $"{label} overlaps block {owners[index]}.");
                        break;
                    }

                    owners[index] = i + 1;
                }

                if (!HasExitDoor(level, block))
                {
                    report.Add(ValidationSeverity.Warning, $"{label} has no {block.color} door wide enough for it.");
                }
            }
        }

        private static void CheckDoors(LevelData level, ValidationReport report)
        {
            for (int i = 0; i < level.Doors.Count; i++)
            {
                DoorPlacement door = level.Doors[i];
                string label = $"Door {i + 1} ({door.color}, {door.side})";
                if (door.start < 0 || door.start + door.length > SideLength(level, door.side))
                {
                    report.Add(ValidationSeverity.Error, $"{label} goes past the end of its side.");
                }

                for (int j = 0; j < i; j++)
                {
                    DoorPlacement other = level.Doors[j];
                    if (other.side == door.side && door.start < other.start + other.length && other.start < door.start + door.length)
                    {
                        report.Add(ValidationSeverity.Error, $"{label} overlaps door {j + 1}.");
                    }
                }

                if (!HasBlockOfColor(level, door.color))
                {
                    report.Add(ValidationSeverity.Warning, $"{label} has no {door.color} block to use it.");
                }
            }
        }

        // True when some side has doors of the block's color across a run of lanes as wide as the block.
        // The block can slide along the side, so the run may be anywhere on it.
        private static bool HasExitDoor(LevelData level, BlockPlacement block)
        {
            for (var side = BoardSide.Bottom; side <= BoardSide.Right; side++)
            {
                int blockWidth = LaneSpan(block.shape, side);
                for (int start = 0; start + blockWidth <= SideLength(level, side); start++)
                {
                    if (CoversLanes(level, side, start, blockWidth, block.color))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int LaneSpan(BlockShape shape, BoardSide side)
        {
            bool alongRows = side == BoardSide.Left || side == BoardSide.Right;
            int min = int.MaxValue;
            int max = int.MinValue;
            foreach (Vector2Int cell in shape.Cells)
            {
                int lane = alongRows ? cell.y : cell.x;
                min = Mathf.Min(min, lane);
                max = Mathf.Max(max, lane);
            }

            return max - min + 1;
        }

        private static bool CoversLanes(LevelData level, BoardSide side, int start, int count, BlockColor color)
        {
            for (int lane = start; lane < start + count; lane++)
            {
                if (!HasDoor(level, side, lane, color))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasDoor(LevelData level, BoardSide side, int lane, BlockColor color)
        {
            foreach (DoorPlacement door in level.Doors)
            {
                if (door.color == color && door.Covers(side, lane))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasBlockOfColor(LevelData level, BlockColor color)
        {
            foreach (BlockPlacement block in level.Blocks)
            {
                if (block.color == color)
                {
                    return true;
                }
            }

            return false;
        }

        private static int SideLength(LevelData level, BoardSide side) =>
            side == BoardSide.Left || side == BoardSide.Right ? level.Height : level.Width;

        private static bool IsInside(LevelData level, Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < level.Width && cell.y < level.Height;
    }
}
