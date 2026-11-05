using UnityEngine;

namespace BlockJam.Gameplay.View
{
    // Maps board cells to the board root's local space. Cells are 1 unit wide and the board is centered on the root.
    public readonly struct BoardLayout
    {
        private readonly Vector2 _centerOffset;

        public BoardLayout(int width, int height)
        {
            Size = new Vector2(width, height);
            _centerOffset = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        }

        public Vector2 Size { get; }

        public Vector3 CellToLocal(Vector2Int cell) =>
            new Vector3(cell.x - _centerOffset.x, 0f, cell.y - _centerOffset.y);

        public Vector2 LocalToCell(Vector3 local) =>
            new Vector2(local.x + _centerOffset.x, local.z + _centerOffset.y);
    }
}
