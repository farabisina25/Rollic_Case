using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlockJam.Gameplay.View
{
    // Sits on the board root. Presses on block colliders bubble up to it through the camera's PhysicsRaycaster;
    // the pointer is then followed every frame so fast swipes still move the block even without drag events.
    public sealed class BlockDragInput : MonoBehaviour, IPointerDownHandler
    {
        private Camera _camera;
        private bool _isDragging;
        private Vector3 _lastScreenPosition;

        public event Action<int, Vector3> DragStarted;
        public event Action<Vector3> Dragged;
        public event Action DragEnded;

        public void OnPointerDown(PointerEventData eventData)
        {
            GameObject pressed = eventData.pointerCurrentRaycast.gameObject;
            BlockView block = pressed != null ? pressed.GetComponentInParent<BlockView>() : null;
            _camera = eventData.pressEventCamera;
            if (_isDragging || block == null || !TryGetBoardPoint(eventData.position, out Vector3 point))
            {
                return;
            }

            _isDragging = true;
            _lastScreenPosition = eventData.position;
            DragStarted?.Invoke(block.Id, point);
        }

        private void Update()
        {
            if (!_isDragging)
            {
                return;
            }

            Vector3 screenPosition = Input.mousePosition;
            if (screenPosition != _lastScreenPosition && TryGetBoardPoint(screenPosition, out Vector3 point))
            {
                _lastScreenPosition = screenPosition;
                Dragged?.Invoke(point);
            }

            if (!Input.GetMouseButton(0))
            {
                _isDragging = false;
                DragEnded?.Invoke();
            }
        }

        private void OnDisable()
        {
            if (_isDragging)
            {
                _isDragging = false;
                DragEnded?.Invoke();
            }
        }

        private bool TryGetBoardPoint(Vector2 screenPosition, out Vector3 localPoint)
        {
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            var boardPlane = new Plane(transform.up, transform.position);
            if (!boardPlane.Raycast(ray, out float distance))
            {
                localPoint = default;
                return false;
            }

            localPoint = transform.InverseTransformPoint(ray.GetPoint(distance));
            return true;
        }
    }
}
