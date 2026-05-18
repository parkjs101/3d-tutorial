using UnityEngine;

public class PlayerAnimatorBridge : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private float walkSpeedThreshold = 0.05f;
    [SerializeField] private float rotationSharpness = 12f;

    private Transform visualRoot;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int FreeFallHash = Animator.StringToHash("FreeFall");

    void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            visualRoot = animator.transform;
        }
    }

    void Update()
    {
        if (playerMovement == null || animator == null)
        {
            return;
        }

        PlayerState state = playerMovement.CurrentState;
        float horizontalSpeed = GetHorizontalSpeed();
        bool grounded = state == PlayerState.Idle ||
                        state == PlayerState.Walk ||
                        state == PlayerState.PushPull ||
                        state == PlayerState.Climb;

        animator.SetFloat(SpeedHash, state == PlayerState.Walk ? Mathf.Max(horizontalSpeed, walkSpeedThreshold) : 0f);
        animator.SetFloat(MotionSpeedHash, 1f);
        animator.SetBool(JumpHash, state == PlayerState.Jump);
        animator.SetBool(GroundedHash, grounded);
        animator.SetBool(FreeFallHash, state == PlayerState.Fall);

        RotateVisualTowardMovement();
    }

    private float GetHorizontalSpeed()
    {
        if (playerRigidbody == null)
        {
            return 0f;
        }

        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.y = 0f;
        return velocity.magnitude;
    }

    private void RotateVisualTowardMovement()
    {
        if (visualRoot == null)
        {
            return;
        }

        Vector3 moveDirection = playerMovement.CurrentMoveDirection;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
        float rotateT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
        visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, targetRotation, rotateT);
    }
}
