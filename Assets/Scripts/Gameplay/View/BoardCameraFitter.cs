using UnityEngine;

namespace BlockJam.Gameplay.View
{
    // Moves the camera along its view axis until the board fits the screen. Keeps the rotation set in the scene.
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCameraFitter : MonoBehaviour
    {
        [Tooltip("Extra world units around the board, so walls and doors stay visible.")]
        [SerializeField] private float sidePadding = 1f;
        [Tooltip("Extra world units above and below the board, room for the HUD and the booster bar.")]
        [SerializeField] private float verticalPadding = 3f;

        public void Fit(Transform board, Vector2 boardSize)
        {
            var targetCamera = GetComponent<Camera>();
            float tanHalfFov = Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            float halfWidth = boardSize.x * 0.5f + sidePadding;
            float halfDepth = boardSize.y * 0.5f + verticalPadding;
            float distance = Mathf.Max(halfWidth / (tanHalfFov * targetCamera.aspect), halfDepth / tanHalfFov);

            transform.position = board.position - transform.forward * distance;
        }
    }
}
