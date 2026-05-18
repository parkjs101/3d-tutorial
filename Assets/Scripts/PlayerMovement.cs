using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Idle,
    Walk,
    Jump,
    Fall,
    PushPull,
    Climb,
    Dead
}

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    [SerializeField] private float pushPullSpeed = 2f;
    [SerializeField] private float climbSpeed = 2f;

    [Header("Checks")]
    public Transform groundCheck;
    public float checkRadius = 0.1f;
    public LayerMask groundLayer;
    [SerializeField] private float interactRadius = 1.4f;
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private LayerMask wallLayer = ~0;
    [SerializeField] private float climbTopProbeHeight = 1.4f;
    [SerializeField] private float climbTopForwardOffset = 0.7f;
    [SerializeField] private float climbTopDownDistance = 1.8f;
    [SerializeField] private float climbTopExitBackOffset = 0.15f;

    private Rigidbody rb;
    private Vector2 inputVector;
    private bool jumpRequested;
    private bool interactRequested;
    private bool isDead;
    private bool originalUseGravity;
    private WaypointFollower currentPlatform;
    private PushPullBox currentBox;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    public Vector3 CurrentMoveDirection { get; private set; } = Vector3.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerMovement requires a Rigidbody component.");
            enabled = false;
            return;
        }

        originalUseGravity = rb.useGravity;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        ReadInput();
    }

    void FixedUpdate()
    {
        if (rb == null || isDead)
        {
            CurrentMoveDirection = Vector3.zero;
            return;
        }

        bool isGrounded = IsGrounded();

        if (interactRequested)
        {
            interactRequested = false;
            TogglePushPull();
        }

        if (jumpRequested)
        {
            HandleJump(isGrounded);
        }

        if (CurrentState == PlayerState.PushPull)
        {
            HandlePushPull(isGrounded);
            return;
        }

        if (TryHandleClimb(isGrounded))
        {
            return;
        }

        MoveNormally(isGrounded);
    }

    void ReadInput()
    {
        if (Keyboard.current == null || isDead)
        {
            inputVector = Vector2.zero;
            jumpRequested = false;
            interactRequested = false;
            return;
        }

        inputVector = Vector2.zero;

        if (Keyboard.current.dKey.isPressed) inputVector.y = 1;
        if (Keyboard.current.aKey.isPressed) inputVector.y = -1;
        if (Keyboard.current.wKey.isPressed) inputVector.x = -1;
        if (Keyboard.current.sKey.isPressed) inputVector.x = 1;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            interactRequested = true;
        }
    }

    void MoveNormally(bool isGrounded)
    {
        SetClimbMode(false);

        Vector3 moveDirection = GetMoveDirection();
        CurrentMoveDirection = moveDirection;
        Vector3 platformVelocity = currentPlatform != null ? currentPlatform.Velocity : Vector3.zero;

        rb.linearVelocity = new Vector3(
            moveDirection.x * moveSpeed + platformVelocity.x,
            rb.linearVelocity.y,
            moveDirection.z * moveSpeed + platformVelocity.z
        );

        bool groundedForState = isGrounded && rb.linearVelocity.y <= 0.1f;
        UpdateLocomotionState(groundedForState, moveDirection);
    }

    void HandleJump(bool isGrounded)
    {
        jumpRequested = false;

        if (!isGrounded)
        {
            return;
        }

        ReleaseBox();
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        CurrentState = PlayerState.Jump;
    }

    void TogglePushPull()
    {
        if (CurrentState == PlayerState.PushPull)
        {
            ReleaseBox();
            return;
        }

        currentBox = FindNearbyBox();
        if (currentBox != null)
        {
            currentBox.BeginPushPull();
            CurrentState = PlayerState.PushPull;
        }
    }

    void HandlePushPull(bool isGrounded)
    {
        SetClimbMode(false);

        if (currentBox == null || !isGrounded)
        {
            ReleaseBox();
            return;
        }

        Vector3 moveDirection = GetMoveDirection();
        CurrentMoveDirection = moveDirection;
        Vector3 moveVelocity = moveDirection * pushPullSpeed;
        Vector3 platformVelocity = currentPlatform != null ? currentPlatform.Velocity : Vector3.zero;

        rb.linearVelocity = new Vector3(
            moveVelocity.x + platformVelocity.x,
            rb.linearVelocity.y,
            moveVelocity.z + platformVelocity.z
        );

        currentBox.SetMoveVelocity(moveVelocity);
        CurrentState = PlayerState.PushPull;
    }

    bool TryHandleClimb(bool isGrounded)
    {
        if (!TryGetWallHit(out Vector3 wallDirection))
        {
            CurrentMoveDirection = Vector3.zero;
            SetClimbMode(false);
            return false;
        }

        if (isGrounded && inputVector.y < -0.01f)
        {
            CurrentMoveDirection = Vector3.zero;
            SetClimbMode(false);
            return false;
        }

        if (TryClimbOntoTop(wallDirection))
        {
            return true;
        }

        if (Mathf.Abs(inputVector.y) <= 0.01f)
        {
            CurrentMoveDirection = Vector3.zero;
            SetClimbMode(false);
            CurrentState = rb.linearVelocity.y > 0.1f ? PlayerState.Jump : PlayerState.Fall;
            return false;
        }

        ReleaseBox();
        CurrentMoveDirection = new Vector3(inputVector.x, 0f, 0f);
        SetClimbMode(true);
        rb.linearVelocity = new Vector3(inputVector.x * moveSpeed, inputVector.y * climbSpeed, 0f);
        CurrentState = PlayerState.Climb;
        return true;
    }

    void SetClimbMode(bool enabled)
    {
        if (rb != null)
        {
            rb.useGravity = enabled ? false : originalUseGravity;
        }
    }

    void UpdateLocomotionState(bool isGrounded, Vector3 moveDirection)
    {
        if (!isGrounded)
        {
            CurrentState = rb.linearVelocity.y > 0.1f ? PlayerState.Jump : PlayerState.Fall;
            return;
        }

        CurrentState = moveDirection.sqrMagnitude > 0.01f ? PlayerState.Walk : PlayerState.Idle;
    }

    Vector3 GetMoveDirection()
    {
        Vector3 moveDirection = new Vector3(inputVector.x, 0f, inputVector.y);
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        return moveDirection;
    }

    PushPullBox FindNearbyBox()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius);
        PushPullBox nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            PushPullBox box = hit.GetComponentInParent<PushPullBox>();
            if (box == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, box.transform.position);
            if (distance < nearestDistance)
            {
                nearest = box;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    void ReleaseBox()
    {
        if (currentBox != null)
        {
            currentBox.EndPushPull();
            currentBox = null;
        }

        if (CurrentState == PlayerState.PushPull)
        {
            CurrentState = PlayerState.Idle;
        }
    }

    bool IsGrounded()
    {
        return groundCheck != null && Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer);
    }

    bool TryGetWallHit(out Vector3 wallDirection)
    {
        Vector3 origin = transform.position + Vector3.up * 0.4f;
        Vector3[] directions =
        {
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back
        };

        foreach (Vector3 direction in directions)
        {
            if (Physics.Raycast(origin, direction, wallCheckDistance, wallLayer))
            {
                wallDirection = direction;
                return true;
            }
        }

        wallDirection = Vector3.zero;
        return false;
    }

    bool TryClimbOntoTop(Vector3 wallDirection)
    {
        if (inputVector.y <= 0.01f)
        {
            return false;
        }

        Vector3 upperWallCheckOrigin = transform.position + Vector3.up * climbTopProbeHeight;
        if (Physics.Raycast(upperWallCheckOrigin, wallDirection, wallCheckDistance, wallLayer))
        {
            return false;
        }

        Vector3 topProbeOrigin =
            transform.position +
            Vector3.up * climbTopProbeHeight +
            wallDirection * climbTopForwardOffset;

        if (!Physics.Raycast(topProbeOrigin, Vector3.down, out RaycastHit topHit, climbTopDownDistance, groundLayer))
        {
            return false;
        }

        SetClimbMode(false);
        float groundOffset = groundCheck != null ? transform.position.y - groundCheck.position.y : 0.5f;
        transform.position = topHit.point + Vector3.up * groundOffset - wallDirection * climbTopExitBackOffset;
        rb.linearVelocity = Vector3.zero;
        CurrentState = PlayerState.Idle;
        return true;
    }

    public void EnterDeadState()
    {
        isDead = true;
        ReleaseBox();
        CurrentState = PlayerState.Dead;

        if (rb != null)
        {
            SetClimbMode(false);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ResetAfterRespawn()
    {
        isDead = false;
        SetClimbMode(false);
        inputVector = Vector2.zero;
        CurrentMoveDirection = Vector3.zero;
        jumpRequested = false;
        interactRequested = false;
        ReleaseBox();
        CurrentState = PlayerState.Idle;
    }

    void OnCollisionStay(Collision collision)
    {
        if (!IsStandingOnCollision(collision))
        {
            return;
        }

        WaypointFollower platform = collision.collider.GetComponentInParent<WaypointFollower>();
        if (platform != null)
        {
            currentPlatform = platform;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        WaypointFollower platform = collision.collider.GetComponentInParent<WaypointFollower>();
        if (platform == currentPlatform)
        {
            currentPlatform = null;
        }
    }

    bool IsStandingOnCollision(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                return true;
            }
        }

        return false;
    }
}
