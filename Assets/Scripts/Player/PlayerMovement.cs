using UnityEngine;

public enum PlayerState
{
    Idle,
    Walk,
    CrouchIdle,
    CrouchWalk,
    Jump,
    Fall,
    PushPull,
    Climb
}

[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerInteraction))]
public partial class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    [SerializeField] private float sprintSpeed = 4f;
    [SerializeField] private float crouchSpeed = 1f;
    public float jumpForce = 3.5f;
    [SerializeField] private float pushPullSpeed = 2f;

    [Header("Checks")]
    public Transform groundCheck;
    public float checkRadius = 0.1f;
    public LayerMask groundLayer;

    private PlayerInputReader inputReader;
    private PlayerInteraction interaction;
    private Rigidbody rb;
    private Vector2 inputVector;
    private bool sprintHeld;
    private bool jumpRequested;
    private bool interactRequested;
    private float climbInput;
    private bool jumpAnimationActive;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    public Vector3 CurrentMoveDirection { get; private set; } = Vector3.zero;

    void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        if (inputReader == null)
        {
            inputReader = gameObject.AddComponent<PlayerInputReader>();
        }

        interaction = GetComponent<PlayerInteraction>();
        if (interaction == null)
        {
            interaction = gameObject.AddComponent<PlayerInteraction>();
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerMovement requires a Rigidbody component.");
            enabled = false;
            return;
        }

        rb.constraints = RigidbodyConstraints.FreezeRotation;

        InitializeCrouch();
    }

    void Update()
    {
        ReadInput();
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            CurrentMoveDirection = Vector3.zero;
            UpdateCrouchCollider();
            return;
        }

        UpdateCrouchCollider();

        bool isGrounded = IsGrounded();
        if (interaction != null)
        {
            interaction.UpdateHighlight(transform.position);
        }

        bool tireIsLifting = TirePickup.HeldTire != null && TirePickup.HeldTire.IsLifting;
        if (tireIsLifting || CandlePickup.IsTransitioning || KnockDownInteractable.IsAnyPulling)
        {
            CurrentMoveDirection = Vector3.zero;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            UpdateLocomotionState(isGrounded, Vector3.zero);
            return;
        }

        if (TryStartOrHandleLadderClimb())
        {
            return;
        }

        if (interactRequested)
        {
            interactRequested = false;
            if (interaction != null && interaction.TryInteract(this))
            {
                return;
            }

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

        MoveNormally(isGrounded);
    }

    void ReadInput()
    {
        if (inputReader == null)
        {
            inputVector = Vector2.zero;
            sprintHeld = false;
            jumpRequested = false;
            interactRequested = false;
            climbInput = 0f;
            return;
        }

        inputReader.Tick();
        inputVector = inputReader.MoveInput;
        sprintHeld = inputReader.SprintHeld;
        climbInput = inputReader.ClimbInput;

        if (inputReader.CrouchPressed)
        {
            if (!crouchToggled || !IsUnderCrouchLockFloor())
            {
                crouchToggled = !crouchToggled;
            }
        }

        if (inputReader.JumpPressed)
        {
            jumpRequested = true;
        }

        if (inputReader.InteractPressed)
        {
            interactRequested = true;
        }
    }

    void MoveNormally(bool isGrounded)
    {
        Vector3 moveDirection = GetMoveDirection();
        CurrentMoveDirection = moveDirection;
        Vector3 platformVelocity = currentPlatform != null ? currentPlatform.Velocity : Vector3.zero;
        float activeMoveSpeed = GetActiveMoveSpeed(moveDirection);

        rb.linearVelocity = new Vector3(
            moveDirection.x * activeMoveSpeed + platformVelocity.x,
            rb.linearVelocity.y,
            moveDirection.z * activeMoveSpeed + platformVelocity.z
        );

        bool groundedForState = isGrounded;
        UpdateLocomotionState(groundedForState, moveDirection);
    }

    void HandleJump(bool isGrounded)
    {
        jumpRequested = false;

        if (!isGrounded || crouchToggled)
        {
            return;
        }

        ReleaseBox();
        jumpAnimationActive = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        CurrentState = PlayerState.Jump;
    }

    void TogglePushPull()
    {
        if (interaction == null)
        {
            return;
        }

        if (CurrentState == PlayerState.PushPull)
        {
            ReleaseBox();
            return;
        }

        if (interaction.TogglePushPull(transform.position))
        {
            CurrentState = PlayerState.PushPull;
        }
    }

    void HandlePushPull(bool isGrounded)
    {
        if (interaction == null || !interaction.IsPushPulling || !isGrounded)
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

        interaction.SetPushPullVelocity(moveVelocity);
        CurrentState = PlayerState.PushPull;
    }

    void UpdateLocomotionState(bool isGrounded, Vector3 moveDirection)
    {
        if (jumpAnimationActive)
        {
            if (rb.linearVelocity.y > 0.1f)
            {
                CurrentState = PlayerState.Jump;
                return;
            }

            jumpAnimationActive = false;
        }

        if (!isGrounded)
        {
            CurrentState = rb.linearVelocity.y > 0.1f ? PlayerState.Jump : PlayerState.Fall;
            return;
        }

        if (crouchToggled)
        {
            CurrentState = moveDirection.sqrMagnitude > 0.01f ? PlayerState.CrouchWalk : PlayerState.CrouchIdle;
            return;
        }

        CurrentState = moveDirection.sqrMagnitude > 0.01f ? PlayerState.Walk : PlayerState.Idle;
    }

    float GetActiveMoveSpeed(Vector3 moveDirection)
    {
        if (crouchToggled)
        {
            return crouchSpeed;
        }

        return sprintHeld && moveDirection.sqrMagnitude > 0.01f ? sprintSpeed : moveSpeed;
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

    void ReleaseBox()
    {
        if (interaction != null)
        {
            interaction.ReleaseBox();
        }

        if (CurrentState == PlayerState.PushPull)
        {
            CurrentState = PlayerState.Idle;
        }
    }

    bool IsGrounded()
    {
        bool groundCheckHit = groundCheck != null && Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer);
        return groundCheckHit || hasWalkableContact;
    }

    void OnDisable()
    {
        StopLadderClimb();
        ReleaseBox();

        if (interaction != null)
        {
            interaction.ClearHighlight();
        }
    }

}
