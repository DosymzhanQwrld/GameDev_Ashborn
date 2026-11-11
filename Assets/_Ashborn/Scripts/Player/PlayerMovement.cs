using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashborn.Player
{
    /// <summary>
    /// Run and jump for the player.
    /// Reads "Player/Move" and "Player/Jump" from the project-wide Input Actions
    /// (Assets/Settings/InputSystem_Actions), so keyboard, gamepad and later
    /// the on-screen mobile buttons all drive the same code.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] float moveSpeed = 8f;
        [Tooltip("How fast the player reaches full speed (units/s²).")]
        [SerializeField] float acceleration = 90f;
        [Tooltip("How fast the player stops when there is no input (units/s²).")]
        [SerializeField] float deceleration = 110f;

        [Header("Jump")]
        [Tooltip("Upward speed at the moment of the jump.")]
        [SerializeField] float jumpSpeed = 15f;
        [Tooltip("Gravity multiplier while falling. Bigger = snappier jump.")]
        [SerializeField] float fallGravityMultiplier = 2f;
        [Tooltip("Upward speed is multiplied by this when the jump button is released early.")]
        [Range(0f, 1f)]
        [SerializeField] float jumpCutMultiplier = 0.5f;
        [SerializeField] float maxFallSpeed = 20f;

        [Header("Forgiveness")]
        [Tooltip("Seconds after leaving a ledge when a jump still works.")]
        [SerializeField] float coyoteTime = 0.1f;
        [Tooltip("Seconds a jump press is remembered before landing.")]
        [SerializeField] float jumpBufferTime = 0.1f;

        [Header("Ground check")]
        [SerializeField] LayerMask groundLayer;
        [Tooltip("Size of the box under the feet that looks for ground.")]
        [SerializeField] Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
        [Tooltip("Offset of that box from the player's pivot.")]
        [SerializeField] Vector2 groundCheckOffset = new Vector2(0f, -0.5f);

        Rigidbody2D body;
        InputAction moveAction;
        InputAction jumpAction;
        float baseGravity;

        float moveInput;
        float coyoteTimer;
        float jumpBufferTimer;
        bool jumpHeld;
        bool isJumping;

        public bool IsGrounded { get; private set; }
        /// <summary>+1 = facing right, -1 = facing left. Used later by attack and roll.</summary>
        public int FacingDirection { get; private set; } = 1;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            baseGravity = body.gravityScale;

            moveAction = InputSystem.actions.FindAction("Player/Move", throwIfNotFound: true);
            jumpAction = InputSystem.actions.FindAction("Player/Jump", throwIfNotFound: true);
        }

        void OnEnable()
        {
            moveAction.Enable();
            jumpAction.Enable();
        }

        void OnDisable()
        {
            moveAction.Disable();
            jumpAction.Disable();
        }

        // Input and timers every frame, so no button press is missed.
        void Update()
        {
            moveInput = moveAction.ReadValue<Vector2>().x;
            jumpHeld = jumpAction.IsPressed();

            if (jumpAction.WasPressedThisFrame())
                jumpBufferTimer = jumpBufferTime;
            else
                jumpBufferTimer -= Time.deltaTime;

            if (IsGrounded)
                coyoteTimer = coyoteTime;
            else
                coyoteTimer -= Time.deltaTime;

            if (moveInput > 0.01f) Face(1);
            else if (moveInput < -0.01f) Face(-1);
        }

        // Physics in FixedUpdate.
        void FixedUpdate()
        {
            IsGrounded = CheckGround();
            if (IsGrounded && body.linearVelocity.y <= 0f)
                isJumping = false;

            Run();
            TryJump();
            ApplyGravity();
        }

        void Run()
        {
            float target = moveInput * moveSpeed;
            float rate = Mathf.Abs(moveInput) > 0.01f ? acceleration : deceleration;
            float x = Mathf.MoveTowards(body.linearVelocity.x, target, rate * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(x, body.linearVelocity.y);
        }

        void TryJump()
        {
            if (jumpBufferTimer > 0f && coyoteTimer > 0f && !isJumping)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
                isJumping = true;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }

            // Released early while still going up: cut the jump short.
            if (isJumping && !jumpHeld && body.linearVelocity.y > 0f)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * jumpCutMultiplier);
                isJumping = false;
            }
        }

        void ApplyGravity()
        {
            body.gravityScale = body.linearVelocity.y < 0f ? baseGravity * fallGravityMultiplier : baseGravity;

            if (body.linearVelocity.y < -maxFallSpeed)
                body.linearVelocity = new Vector2(body.linearVelocity.x, -maxFallSpeed);
        }

        bool CheckGround()
        {
            Vector2 center = (Vector2)transform.position + groundCheckOffset;
            return Physics2D.OverlapBox(center, groundCheckSize, 0f, groundLayer) != null;
        }

        void Face(int direction)
        {
            if (direction == FacingDirection) return;
            FacingDirection = direction;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * direction;
            transform.localScale = s;
        }

        // Green box = touching ground, red = in the air. Visible in the Scene view.
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Application.isPlaying && IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube((Vector2)transform.position + groundCheckOffset, groundCheckSize);
        }
    }
}
