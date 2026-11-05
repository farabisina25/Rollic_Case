using BlockJam.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace BlockJam.Editor.LevelEditor
{
    // Draws the board with a ring of wall slots around it and turns mouse input into edits.
    // Grid coordinates include the ring, so board cell (0, 0) is grid (1, height) because rows go down on screen.
    internal sealed class BoardCanvas
    {
        private const float MaxCellSize = 64f;
        private const float Padding = 12f;
        private const float BlockInset = 3f;
        private const float DoorThickness = 0.45f;

        private readonly LevelEditSession _session;
        private readonly ToolSettings _tools;

        private Rect _gridRect;
        private float _cellSize;
        private int _movingBlock = -1;
        private Vector2Int _grabOffset;
        private GUIStyle _numberStyle;

        public BoardCanvas(LevelEditSession session, ToolSettings tools)
        {
            _session = session;
            _tools = tools;
        }

        private LevelData Level => _session.Working;

        public void OnGUI(Rect area)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Layout(area);

            Event current = Event.current;
            if (current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(area, LevelEditorStyle.Background);
                Draw(current.mousePosition);
            }
            else
            {
                HandleInput(current, controlId, area);
            }
        }

        private void Layout(Rect area)
        {
            int columns = Level.Width + 2;
            int rows = Level.Height + 2;
            _cellSize = Mathf.Floor(Mathf.Min(MaxCellSize, (area.width - Padding * 2f) / columns, (area.height - Padding * 2f) / rows));
            Vector2 size = new Vector2(columns, rows) * _cellSize;
            _gridRect = new Rect(area.center - size * 0.5f, size);
        }

        private void HandleInput(Event current, int controlId, Rect area)
        {
            switch (current.type)
            {
                case EventType.MouseDown when area.Contains(current.mousePosition):
                    if (current.button == 1)
                    {
                        _movingBlock = -1;
                        RemoveAt(current.mousePosition);
                    }
                    else if (current.button == 0)
                    {
                        OnLeftClick(current.mousePosition, controlId);
                    }

                    current.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == controlId:
                    current.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == controlId:
                    GUIUtility.hotControl = 0;
                    FinishMove(current.mousePosition);
                    current.Use();
                    break;
            }
        }

        private void OnLeftClick(Vector2 mouse, int controlId)
        {
            switch (_tools.tool)
            {
                case EditTool.Block when TryGetCell(mouse, out Vector2Int cell):
                    int block = _session.FindBlockAt(cell);
                    if (block >= 0)
                    {
                        _movingBlock = block;
                        _grabOffset = cell - Level.Blocks[block].origin;
                        GUIUtility.hotControl = controlId;
                    }
                    else if (_tools.shape != null && _session.CanPlace(_tools.shape, cell))
                    {
                        _session.AddBlock(_tools.shape, _tools.color, cell);
                    }

                    break;

                case EditTool.Door when TryGetEdge(mouse, out BoardSide side, out int lane):
                    int length = Mathf.Min(_tools.doorLength, SideLength(side));
                    int start = Mathf.Clamp(lane, 0, SideLength(side) - length);
                    _session.AddDoor(side, start, length, _tools.color);
                    break;

                case EditTool.Erase:
                    RemoveAt(mouse);
                    break;
            }
        }

        private void FinishMove(Vector2 mouse)
        {
            int block = _movingBlock;
            _movingBlock = -1;
            if (block < 0 || !TryGetCell(mouse, out Vector2Int cell))
            {
                return;
            }

            BlockPlacement placement = Level.Blocks[block];
            Vector2Int origin = cell - _grabOffset;
            if (origin != placement.origin && _session.CanPlace(placement.shape, origin, block))
            {
                _session.MoveBlock(block, origin);
            }
        }

        private void RemoveAt(Vector2 mouse)
        {
            if (TryGetCell(mouse, out Vector2Int cell))
            {
                int block = _session.FindBlockAt(cell);
                if (block >= 0)
                {
                    _session.RemoveBlock(block);
                }
            }
            else if (TryGetEdge(mouse, out BoardSide side, out int lane))
            {
                int door = _session.FindDoorAt(side, lane);
                if (door >= 0)
                {
                    _session.RemoveDoor(door);
                }
            }
        }

        private void Draw(Vector2 mouse)
        {
            DrawWallsAndFloor();

            for (int i = 0; i < Level.Doors.Count; i++)
            {
                DoorPlacement door = Level.Doors[i];
                EditorGUI.DrawRect(DoorRect(door.side, door.start, door.length), LevelEditorStyle.For(door.color));
            }

            for (int i = 0; i < Level.Blocks.Count; i++)
            {
                DrawBlock(i, i == _movingBlock ? 0.35f : 1f);
            }

            DrawHover(mouse);
        }

        private void DrawWallsAndFloor()
        {
            EditorGUI.DrawRect(_gridRect, LevelEditorStyle.Wall);

            for (int y = 0; y < Level.Height; y++)
            {
                for (int x = 0; x < Level.Width; x++)
                {
                    EditorGUI.DrawRect(LevelEditorStyle.Shrink(CellRect(new Vector2Int(x, y)), 1f), LevelEditorStyle.Floor);
                }
            }
        }

        private void DrawBlock(int index, float alpha)
        {
            BlockPlacement block = Level.Blocks[index];
            if (block.shape == null || !FitsOnBoard(block.shape, block.origin))
            {
                return;
            }

            Color color = LevelEditorStyle.WithAlpha(LevelEditorStyle.For(block.color), alpha);
            LevelEditorStyle.DrawShape(block.shape.Cells, offset => CellRect(block.origin + offset), color, BlockInset);

            // The number matches the "Block N" names in the validation messages.
            GUI.Label(CellRect(block.origin), (index + 1).ToString(), NumberStyle);
        }

        private void DrawHover(Vector2 mouse)
        {
            if (_movingBlock >= 0)
            {
                if (TryGetCell(mouse, out Vector2Int cell))
                {
                    BlockPlacement block = Level.Blocks[_movingBlock];
                    DrawGhost(block.shape, block.color, cell - _grabOffset, _movingBlock);
                }

                return;
            }

            if (TryGetCell(mouse, out Vector2Int hoveredCell))
            {
                int block = _session.FindBlockAt(hoveredCell);
                if (block >= 0)
                {
                    Color outline = _tools.tool == EditTool.Block ? LevelEditorStyle.Highlight : LevelEditorStyle.Invalid;
                    OutlineBlock(block, outline);
                }
                else if (_tools.tool == EditTool.Block && _tools.shape != null)
                {
                    DrawGhost(_tools.shape, _tools.color, hoveredCell, -1);
                }
            }
            else if (TryGetEdge(mouse, out BoardSide side, out int lane))
            {
                int door = _session.FindDoorAt(side, lane);
                if (_tools.tool == EditTool.Door)
                {
                    int length = Mathf.Min(_tools.doorLength, SideLength(side));
                    int start = Mathf.Clamp(lane, 0, SideLength(side) - length);
                    EditorGUI.DrawRect(DoorRect(side, start, length), LevelEditorStyle.WithAlpha(LevelEditorStyle.For(_tools.color), 0.6f));
                }
                else if (door >= 0)
                {
                    DoorPlacement placed = Level.Doors[door];
                    LevelEditorStyle.DrawOutline(DoorRect(placed.side, placed.start, placed.length), LevelEditorStyle.Invalid);
                }
            }
        }

        private void DrawGhost(BlockShape shape, BlockColor color, Vector2Int origin, int ignoredBlock)
        {
            Color outline = _session.CanPlace(shape, origin, ignoredBlock) ? LevelEditorStyle.Valid : LevelEditorStyle.Invalid;
            Color fill = LevelEditorStyle.WithAlpha(LevelEditorStyle.For(color), 0.5f);
            foreach (Vector2Int offset in shape.Cells)
            {
                Rect rect = LevelEditorStyle.Shrink(CellRect(origin + offset), BlockInset);
                EditorGUI.DrawRect(rect, fill);
                LevelEditorStyle.DrawOutline(rect, outline);
            }
        }

        private void OutlineBlock(int index, Color color)
        {
            BlockPlacement block = Level.Blocks[index];
            foreach (Vector2Int offset in block.shape.Cells)
            {
                LevelEditorStyle.DrawOutline(LevelEditorStyle.Shrink(CellRect(block.origin + offset), BlockInset), color);
            }
        }

        private bool FitsOnBoard(BlockShape shape, Vector2Int origin)
        {
            foreach (Vector2Int offset in shape.Cells)
            {
                Vector2Int cell = origin + offset;
                if (cell.x < 0 || cell.y < 0 || cell.x >= Level.Width || cell.y >= Level.Height)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryGetCell(Vector2 mouse, out Vector2Int cell)
        {
            Vector2Int grid = ToGrid(mouse);
            cell = new Vector2Int(grid.x - 1, Level.Height - grid.y);
            return cell.x >= 0 && cell.y >= 0 && cell.x < Level.Width && cell.y < Level.Height;
        }

        private bool TryGetEdge(Vector2 mouse, out BoardSide side, out int lane)
        {
            Vector2Int grid = ToGrid(mouse);
            bool inColumns = grid.x >= 1 && grid.x <= Level.Width;
            bool inRows = grid.y >= 1 && grid.y <= Level.Height;

            if (inColumns && (grid.y == 0 || grid.y == Level.Height + 1))
            {
                side = grid.y == 0 ? BoardSide.Top : BoardSide.Bottom;
                lane = grid.x - 1;
                return true;
            }

            if (inRows && (grid.x == 0 || grid.x == Level.Width + 1))
            {
                side = grid.x == 0 ? BoardSide.Left : BoardSide.Right;
                lane = Level.Height - grid.y;
                return true;
            }

            side = default;
            lane = -1;
            return false;
        }

        private Vector2Int ToGrid(Vector2 mouse)
        {
            Vector2 local = (mouse - _gridRect.position) / _cellSize;
            return new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
        }

        private Rect GridRect(int column, int row) =>
            new Rect(_gridRect.x + column * _cellSize, _gridRect.y + row * _cellSize, _cellSize, _cellSize);

        private Rect CellRect(Vector2Int cell) => GridRect(cell.x + 1, Level.Height - cell.y);

        private Rect EdgeRect(BoardSide side, int lane)
        {
            switch (side)
            {
                case BoardSide.Bottom: return GridRect(lane + 1, Level.Height + 1);
                case BoardSide.Top: return GridRect(lane + 1, 0);
                case BoardSide.Left: return GridRect(0, Level.Height - lane);
                default: return GridRect(Level.Width + 1, Level.Height - lane);
            }
        }

        // The door bar sits on the half of the wall slot that touches the board.
        private Rect DoorRect(BoardSide side, int start, int length)
        {
            Rect first = EdgeRect(side, start);
            Rect last = EdgeRect(side, start + length - 1);
            Rect span = Rect.MinMaxRect(
                Mathf.Min(first.xMin, last.xMin), Mathf.Min(first.yMin, last.yMin),
                Mathf.Max(first.xMax, last.xMax), Mathf.Max(first.yMax, last.yMax));

            float thickness = _cellSize * DoorThickness;
            switch (side)
            {
                case BoardSide.Bottom: return new Rect(span.x + 2f, span.y, span.width - 4f, thickness);
                case BoardSide.Top: return new Rect(span.x + 2f, span.yMax - thickness, span.width - 4f, thickness);
                case BoardSide.Left: return new Rect(span.xMax - thickness, span.y + 2f, thickness, span.height - 4f);
                default: return new Rect(span.x, span.y + 2f, thickness, span.height - 4f);
            }
        }

        private int SideLength(BoardSide side) =>
            side == BoardSide.Left || side == BoardSide.Right ? Level.Height : Level.Width;

        private GUIStyle NumberStyle => _numberStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
    }
}
