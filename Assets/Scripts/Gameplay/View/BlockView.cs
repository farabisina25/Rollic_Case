using UnityEngine;

namespace BlockJam.Gameplay.View
{
    public sealed class BlockView : MonoBehaviour
    {
        private const float MoveSpeed = 18f;
        private const float ExitDistance = 4f;

        private Vector3 _target;
        private bool _isMoving;
        private bool _isExiting;

        public int Id { get; private set; }

        public void Init(int id, Vector3 localPosition)
        {
            Id = id;
            transform.localPosition = localPosition;
            _target = localPosition;
        }

        public void MoveTo(Vector3 localPosition)
        {
            _target = localPosition;
            _isMoving = true;
        }

        public void PlayExit(Vector3 direction)
        {
            MoveTo(_target + direction * ExitDistance);
            _isExiting = true;
        }

        private void Update()
        {
            if (!_isMoving)
            {
                return;
            }

            transform.localPosition = Vector3.MoveTowards(transform.localPosition, _target, MoveSpeed * Time.deltaTime);
            if (transform.localPosition != _target)
            {
                return;
            }

            _isMoving = false;
            if (_isExiting)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
