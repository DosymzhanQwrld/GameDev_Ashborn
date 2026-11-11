using UnityEngine;
using Ashborn.Player;

namespace Ashborn.Core
{
    /// <summary>
    /// Smooth 2D camera that follows the player, looks a bit ahead in the
    /// direction the player faces and ignores small jumps (vertical dead zone).
    /// Put it on the Main Camera.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;

        [Header("Framing")]
        [Tooltip("Camera offset from the player (y > 0 shows more of what is above).")]
        [SerializeField] Vector2 offset = new Vector2(0f, 1f);
        [Tooltip("How far the camera looks ahead in the facing direction.")]
        [SerializeField] float lookAhead = 2f;

        [Header("Smoothing (seconds to catch up)")]
        [SerializeField] float smoothTimeX = 0.2f;
        [SerializeField] float smoothTimeY = 0.3f;

        [Header("Vertical dead zone")]
        [Tooltip("The camera does not move up or down while the player stays inside this height.")]
        [SerializeField] float deadZoneHeight = 2f;

        PlayerMovement movement;
        float velocityX, velocityY;
        float focusY;

        void Start()
        {
            if (target == null) { enabled = false; Debug.LogWarning("CameraFollow: no target set.", this); return; }
            movement = target.GetComponent<PlayerMovement>();
            focusY = target.position.y;
            // Start already on the player, no fly-in on the first frame.
            transform.position = new Vector3(target.position.x + offset.x, focusY + offset.y, transform.position.z);
        }

        // LateUpdate: after the player has moved this frame.
        void LateUpdate()
        {
            Vector3 p = target.position;

            // Move the focus only when the player leaves the dead zone,
            // or when standing on ground (so the camera settles on platforms).
            float half = deadZoneHeight * 0.5f;
            if (p.y > focusY + half) focusY = p.y - half;
            else if (p.y < focusY - half) focusY = p.y + half;
            else if (movement != null && movement.IsGrounded) focusY = p.y;

            int facing = movement != null ? movement.FacingDirection : 1;
            float goalX = p.x + offset.x + facing * lookAhead;
            float goalY = focusY + offset.y;

            float x = Mathf.SmoothDamp(transform.position.x, goalX, ref velocityX, smoothTimeX);
            float y = Mathf.SmoothDamp(transform.position.y, goalY, ref velocityY, smoothTimeY);
            transform.position = new Vector3(x, y, transform.position.z);
        }

        void OnDrawGizmosSelected()
        {
            if (target == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(target.position.x, Application.isPlaying ? focusY : target.position.y, 0f),
                                new Vector3(1.5f, deadZoneHeight, 0f));
        }
    }
}
