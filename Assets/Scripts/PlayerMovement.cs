using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Idle,
    Walk,
    CrouchIdle,
    CrouchWalk,
    Jump,
    Fall,
    PushPull,
    Dead
}

public class PlayerMovement : MonoBehaviour
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
    [SerializeField] private float interactRadius = 1.4f;

    private Rigidbody rb;
    private Vector2 inputVector;
    private bool sprintHeld;
    private bool crouchToggled;
    private bool jumpRequested;
    private bool interactRequested;
    private bool jumpAnimationActive;
    private bool hasWalkableContact;
    private bool isDead;
    private WaypointFollower currentPlatform;
    private PushPullBox currentBox;
    private Door highlightedDoor;
    private readonly HashSet<Collider> stairContacts = new HashSet<Collider>();

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    public Vector3 CurrentMoveDirection { get; private set; } = Vector3.zero;
    public bool IsOnStairs => stairContacts.Count > 0;
    public bool IsCrouching => crouchToggled;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerMovement requires a Rigidbody component.");
            enabled = false;
            return;
        }

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
        UpdateDoorHighlight();

        if (interactRequested)
        {
            interactRequested = false;
            if (TryInteractWithDoor())
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
        if (Keyboard.current == null || isDead)
        {
            inputVector = Vector2.zero;
            sprintHeld = false;
            jumpRequested = false;
            interactRequested = false;
            return;
        }

        inputVector = Vector2.zero;

        if (Keyboard.current.dKey.isPressed) inputVector.y = 1;
        if (Keyboard.current.aKey.isPressed) inputVector.y = -1;
        if (Keyboard.current.wKey.isPressed) inputVector.x = -1;
        if (Keyboard.current.sKey.isPressed) inputVector.x = 1;

        sprintHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame || Keyboard.current.rightCtrlKey.wasPressedThisFrame)
        {
            crouchToggled = !crouchToggled;
        }

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

    bool TryInteractWithDoor()
    {
        Door door = FindNearbyDoor(requireInteractable: true);
        return door != null && door.TryOpen(transform.position);
    }

    void UpdateDoorHighlight()
    {
        Door nearestDoor = FindNearbyDoor(requireInteractable: true);

        if (highlightedDoor == nearestDoor)
        {
            return;
        }

        if (highlightedDoor != null)
        {
            highlightedDoor.SetHighlighted(false);
        }

        highlightedDoor = nearestDoor;

        if (highlightedDoor != null)
        {
            highlightedDoor.SetHighlighted(true);
        }
    }

    Door FindNearbyDoor(bool requireInteractable)
    {
        Door[] doors = Object.FindObjectsByType<Door>(FindObjectsInactive.Exclude);
        Door nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Door door in doors)
        {
            if (door == null)
            {
                continue;
            }

            if (requireInteractable && !door.CanInteract(transform.position))
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, door.transform.position);
            if (distance < nearestDistance)
            {
                nearest = door;
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
        bool groundCheckHit = groundCheck != null && Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer);
        return groundCheckHit || hasWalkableContact;
    }

    public void EnterDeadState()
    {
        isDead = true;
        jumpAnimationActive = false;
        ReleaseBox();
        CurrentState = PlayerState.Dead;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ResetAfterRespawn()
    {
        isDead = false;
        inputVector = Vector2.zero;
        CurrentMoveDirection = Vector3.zero;
        sprintHeld = false;
        crouchToggled = false;
        jumpRequested = false;
        interactRequested = false;
        jumpAnimationActive = false;
        hasWalkableContact = false;
        stairContacts.Clear();
        ReleaseBox();
        CurrentState = PlayerState.Idle;
    }

    void OnCollisionStay(Collision collision)
    {
        if (!IsStandingOnCollision(collision))
        {
            return;
        }

        hasWalkableContact = true;

        WaypointFollower platform = collision.collider.GetComponentInParent<WaypointFollower>();
        if (platform != null)
        {
            currentPlatform = platform;
        }

        if (IsStairCollider(collision.collider))
        {
            stairContacts.Add(collision.collider);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        hasWalkableContact = false;
        stairContacts.Remove(collision.collider);

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

    bool IsStairCollider(Collider collider)
    {
        Transform current = collider.transform;
        while (current != null)
        {
            if (current.name.IndexOf("stair", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
