using UnityEngine;
using UnityEngine.EventSystems;

namespace BlockJam.UI.Common
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float SnapThresholdSqr = 0.00001f;

        [SerializeField] private float pressedScale = 0.9f;
        [SerializeField] private float speed = 20f;

        private Vector3 _baseScale;
        private Vector3 _targetScale;
        private bool _isAnimating;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
        }

        private void OnDisable()
        {
            transform.localScale = _baseScale;
            _targetScale = _baseScale;
            _isAnimating = false;
        }

        public void OnPointerDown(PointerEventData eventData) => AnimateTo(_baseScale * pressedScale);
        public void OnPointerUp(PointerEventData eventData) => AnimateTo(_baseScale);
        public void OnPointerExit(PointerEventData eventData) => AnimateTo(_baseScale);

        private void AnimateTo(Vector3 target)
        {
            _targetScale = target;
            _isAnimating = true;
        }

        private void Update()
        {
            if (!_isAnimating)
            {
                return;
            }

            Vector3 scale = Vector3.Lerp(transform.localScale, _targetScale, speed * Time.unscaledDeltaTime);
            if ((scale - _targetScale).sqrMagnitude < SnapThresholdSqr)
            {
                scale = _targetScale;
                _isAnimating = false;
            }

            transform.localScale = scale;
        }
    }
}
