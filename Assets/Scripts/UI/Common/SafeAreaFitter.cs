using UnityEngine;

namespace BlockJam.UI.Common
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea
                || _lastScreenSize.x != Screen.width
                || _lastScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            Vector2 min = _lastSafeArea.position;
            Vector2 max = _lastSafeArea.position + _lastSafeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
        }
    }
}
