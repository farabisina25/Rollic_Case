using System.Collections.Generic;
using BlockJam.Gameplay.Data;
using BlockJam.Gameplay.Logic;
using UnityEngine;

namespace BlockJam.Gameplay.View
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private BoardTheme theme;
        [SerializeField] private Transform floorRoot;
        [SerializeField] private Transform edgeRoot;
        [SerializeField] private Transform blockRoot;

        private BlockView[] _blocks;

        public void Build(BoardModel model, BoardLayout layout)
        {
            BuildFloor(model, layout);
            BuildEdges(model, layout);
            BuildBlocks(model.Blocks, layout);
        }

        public void MoveBlock(int id, Vector3 localPosition) => _blocks[id].MoveTo(localPosition);
        public void ExitBlock(int id, Vector2Int direction) => _blocks[id].PlayExit(new Vector3(direction.x, 0f, direction.y));

        private void BuildFloor(BoardModel model, BoardLayout layout)
        {
            for (int y = 0; y < model.Height; y++)
            {
                for (int x = 0; x < model.Width; x++)
                {
                    Instantiate(theme.FloorTile, floorRoot).transform.localPosition = layout.CellToLocal(new Vector2Int(x, y));
                }
            }
        }

        private void BuildEdges(BoardModel model, BoardLayout layout)
        {
            for (int x = 0; x < model.Width; x++)
            {
                BuildEdge(model, layout, BoardSide.Bottom, x, new Vector2Int(x, 0), Vector2Int.down);
                BuildEdge(model, layout, BoardSide.Top, x, new Vector2Int(x, model.Height - 1), Vector2Int.up);
            }

            for (int y = 0; y < model.Height; y++)
            {
                BuildEdge(model, layout, BoardSide.Left, y, new Vector2Int(0, y), Vector2Int.left);
                BuildEdge(model, layout, BoardSide.Right, y, new Vector2Int(model.Width - 1, y), Vector2Int.right);
            }
        }

        private void BuildEdge(BoardModel model, BoardLayout layout, BoardSide side, int lane, Vector2Int cell, Vector2Int outward)
        {
            GameObject prefab = theme.WallSegment;
            foreach (DoorPlacement door in model.Doors)
            {
                if (door.Covers(side, lane))
                {
                    prefab = theme.GetDoor(door.color);
                    break;
                }
            }

            var direction = new Vector3(outward.x, 0f, outward.y);
            Transform segment = Instantiate(prefab, edgeRoot).transform;
            segment.localPosition = layout.CellToLocal(cell) + direction * 0.5f;
            segment.localRotation = Quaternion.LookRotation(direction);
        }

        private void BuildBlocks(IReadOnlyList<BoardBlock> blocks, BoardLayout layout)
        {
            _blocks = new BlockView[blocks.Count];
            foreach (BoardBlock block in blocks)
            {
                var blockObject = new GameObject($"Block_{block.Id}_{block.Color}");
                blockObject.transform.SetParent(blockRoot, false);

                GameObject cellPrefab = theme.GetBlockCell(block.Color);
                foreach (Vector2Int offset in block.Cells)
                {
                    Instantiate(cellPrefab, blockObject.transform).transform.localPosition = new Vector3(offset.x, 0f, offset.y);
                }

                BlockView view = blockObject.AddComponent<BlockView>();
                view.Init(block.Id, layout.CellToLocal(block.Origin));
                _blocks[block.Id] = view;
            }
        }
    }
}
