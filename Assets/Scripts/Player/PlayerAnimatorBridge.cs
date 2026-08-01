using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorBridge : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private float walkSpeedThreshold = 0.05f;
    [SerializeField] private float rotationSharpness = 12f;
    [SerializeField, Range(0f, 1f)] private float pushPullDirectionThreshold = 0.25f;

    private Transform visualRoot;
    private readonly HashSet<int> availableAnimatorParameters = new HashSet<int>();

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int FreeFallHash = Animator.StringToHash("FreeFall");
    private static readonly int CrouchHash = Animator.StringToHash("Crouch");
    private static readonly int CrouchSpeedHash = Animator.StringToHash("CrouchSpeed");
    private static readonly int PushPullHash = Animator.StringToHash("PushPull");
    private static readonly int PushPullDirectionHash = Animator.StringToHash("PushPullDirection");
    private static readonly int TireLiftHash = Animator.StringToHash("TireLift");
    private static readonly int TireCarryHash = Animator.StringToHash("TireCarry");

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
            CacheAnimatorParameters();
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
                        state == PlayerState.CrouchIdle ||
                        state == PlayerState.CrouchWalk ||
                        state == PlayerState.PushPull ||
                        state == PlayerState.Climb;
        bool crouching = state == PlayerState.CrouchIdle || state == PlayerState.CrouchWalk;
        float crouchSpeed = state == PlayerState.CrouchWalk ? Mathf.Max(horizontalSpeed, walkSpeedThreshold) : 0f;

        SetFloatIfAvailable(SpeedHash, IsMovingState(state) ? Mathf.Max(horizontalSpeed, walkSpeedThreshold) : 0f);
        SetFloatIfAvailable(MotionSpeedHash, 1f);
        SetBoolIfAvailable(JumpHash, state == PlayerState.Jump);
        SetBoolIfAvailable(GroundedHash, grounded);
        SetBoolIfAvailable(FreeFallHash, state == PlayerState.Fall);
        SetBoolIfAvailable(CrouchHash, crouching);
        SetFloatIfAvailable(CrouchSpeedHash, crouchSpeed);
        SetBoolIfAvailable(PushPullHash, state == PlayerState.PushPull);
        SetFloatIfAvailable(PushPullDirectionHash, GetPushPullDirection(state));

        TirePickup heldTire = TirePickup.HeldTire;
        SetBoolIfAvailable(TireLiftHash, heldTire != null && heldTire.IsLifting);
        SetBoolIfAvailable(TireCarryHash, heldTire != null && heldTire.IsCarrying);

        if (state != PlayerState.PushPull)
        {
            RotateVisualTowardMovement();
        }
    }

    private float GetPushPullDirection(PlayerState state)
    {
        if (state != PlayerState.PushPull || visualRoot == null)
        {
            return 0f;
        }

        Vector3 moveDirection = playerMovement.CurrentMoveDirection;
        moveDirection.y = 0f;
        if (moveDirection.sqrMagnitude <= 0.001f)
        {
            return 0f;
        }

        Vector3 facingDirection = visualRoot.forward;
        facingDirection.y = 0f;
        float direction = Vector3.Dot(moveDirection.normalized, facingDirection.normalized);
        return Mathf.Abs(direction) >= pushPullDirectionThreshold ? direction : 0f;
    }

    private void CacheAnimatorParameters()
    {
        availableAnimatorParameters.Clear();

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            availableAnimatorParameters.Add(parameter.nameHash);
        }
    }

    private bool HasParameter(int parameterHash)
    {
        return availableAnimatorParameters.Contains(parameterHash);
    }

    private void SetFloatIfAvailable(int parameterHash, float value)
    {
        if (HasParameter(parameterHash))
        {
            animator.SetFloat(parameterHash, value);
        }
    }

    private void SetBoolIfAvailable(int parameterHash, bool value)
    {
        if (HasParameter(parameterHash))
        {
            animator.SetBool(parameterHash, value);
        }
    }

    private bool IsMovingState(PlayerState state)
    {
        return state == PlayerState.Walk || state == PlayerState.CrouchWalk;
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
