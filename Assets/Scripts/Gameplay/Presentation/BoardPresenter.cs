using System;
using BlockJam.Gameplay.Logic;
using BlockJam.Gameplay.View;
using UnityEngine;

namespace BlockJam.Gameplay.Presentation
{
    // Turns a drag on a block into single-cell steps on the board model, so blocks never skip over each other.
    public sealed class BoardPresenter : IDisposable
    {
        private const int NoBlock = -1;
        private const float StepThreshold = 0.5f;

        private readonly BoardView _view;
        private readonly BlockDragInput _input;
        private readonly BoardModel _model;
        private readonly BoardLayout _layout;

        private int _draggedBlock = NoBlock;
        private Vector2 _grabOffset;

        public BoardPresenter(BoardView view, BlockDragInput input, BoardModel model, BoardLayout layout)
        {
            _view = view;
            _input = input;
            _model = model;
            _layout = layout;

            _view.Build(model, layout);

            _input.DragStarted += OnDragStarted;
            _input.Dragged += OnDragged;
            _input.DragEnded += OnDragEnded;
            _model.BlockMoved += OnBlockMoved;
            _model.BlockExited += OnBlockExited;
        }

        public bool IsInputEnabled { get; set; } = true;

        public void Dispose()
        {
            _input.DragStarted -= OnDragStarted;
            _input.Dragged -= OnDragged;
            _input.DragEnded -= OnDragEnded;
            _model.BlockMoved -= OnBlockMoved;
            _model.BlockExited -= OnBlockExited;
        }

        private void OnDragStarted(int blockId, Vector3 point)
        {
            if (!IsInputEnabled)
            {
                return;
            }

            _draggedBlock = blockId;
            _grabOffset = _layout.LocalToCell(point) - _model.Blocks[blockId].Origin;
        }

        private void OnDragged(Vector3 point)
        {
            if (!IsInputEnabled || _draggedBlock == NoBlock)
            {
                return;
            }

            Vector2 target = _layout.LocalToCell(point) - _grabOffset;
            int maxSteps = _model.Width + _model.Height;
            for (int i = 0; i < maxSteps; i++)
            {
                if (_draggedBlock == NoBlock || !TryStepToward(target))
                {
                    return;
                }
            }
        }

        private bool TryStepToward(Vector2 target)
        {
            Vector2 delta = target - _model.Blocks[_draggedBlock].Origin;
            bool horizontalFirst = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            Vector2Int horizontal = new Vector2Int(StepSign(delta.x), 0);
            Vector2Int vertical = new Vector2Int(0, StepSign(delta.y));

            return horizontalFirst
                ? TryStep(horizontal) || TryStep(vertical)
                : TryStep(vertical) || TryStep(horizontal);
        }

        private bool TryStep(Vector2Int direction) =>
            direction != Vector2Int.zero && _model.TryStep(_draggedBlock, direction);

        private static int StepSign(float delta) => Mathf.Abs(delta) < StepThreshold ? 0 : (int)Mathf.Sign(delta);

        private void OnDragEnded() => _draggedBlock = NoBlock;

        private void OnBlockMoved(BoardBlock block) => _view.MoveBlock(block.Id, _layout.CellToLocal(block.Origin));

        private void OnBlockExited(BoardBlock block, Vector2Int direction)
        {
            _view.ExitBlock(block.Id, direction);
            _draggedBlock = NoBlock;
        }
    }
}
