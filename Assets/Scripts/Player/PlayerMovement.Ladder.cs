using UnityEngine;

public partial class PlayerMovement
{
    [Header("Ladder")]
    [SerializeField] private Transform ladderBottom;
    [SerializeField] private Transform ladderTop;
    [SerializeField] private float ladderEnterRadius = 1.2f;
    [SerializeField] private float ladderClimbSpeed = 2f;
    [SerializeField] private float ladderTopExitThreshold = 0.15f;

    private bool isClimbingLadder;
    private bool cachedGravityBeforeLadder;
    private bool cachedKinematicBeforeLadder;

    public bool IsClimbingLadder => isClimbingLadder;

    private bool TryStartOrHandleLadderClimb()
    {
        if (isClimbingLadder)
        {
            HandleLadderClimb();
            return true;
        }

        if (climbInput <= 0.1f || !HasLadderPoints() || !IsNearLadderBottom())
        {
            return false;
        }

        StartLadderClimb();
        HandleLadderClimb();
        return true;
    }

    private bool HasLadderPoints()
    {
        return ladderBottom != null && ladderTop != null;
    }

    private bool IsNearLadderBottom()
    {
        return Vector3.Distance(GetPlayerBottomPosition(), ladderBottom.position) <= ladderEnterRadius;
    }

    private void StartLadderClimb()
    {
        ReleaseBox();
        crouchToggled = false;
        jumpRequested = false;
        interactRequested = false;
        jumpAnimationActive = false;
        CurrentMoveDirection = Vector3.zero;

        cachedGravityBeforeLadder = rb.useGravity;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        cachedKinematicBeforeLadder = rb.isKinematic;
        rb.useGravity = false;
        rb.isKinematic = true;

        isClimbingLadder = true;
        CurrentState = PlayerState.Climb;
        SnapPlayerToLadder();
    }

    private void HandleLadderClimb()
    {
        CurrentState = PlayerState.Climb;
        CurrentMoveDirection = Vector3.zero;

        bool canExitAtTop = IsPlayerBottomNearLadderTop();
        if (canExitAtTop && jumpRequested)
        {
            jumpRequested = false;
            ExitLadderWithJump();
            return;
        }

        if (canExitAtTop && IsHorizontalExitPressed())
        {
            StopLadderClimb();
            CurrentState = PlayerState.Idle;
            return;
        }

        jumpRequested = false;

        Vector3 nextPosition = GetSnappedLadderPosition();
        float bottomOffset = GetPlayerBottomOffset();
        float minY = Mathf.Min(ladderBottom.position.y, ladderTop.position.y) + bottomOffset;
        float maxY = Mathf.Max(ladderBottom.position.y, ladderTop.position.y) + bottomOffset;
        nextPosition.y = Mathf.Clamp(
            transform.position.y + climbInput * ladderClimbSpeed * Time.fixedDeltaTime,
            minY,
            maxY
        );

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
        }

        rb.MovePosition(nextPosition);
    }

    private void ExitLadderWithJump()
    {
        StopLadderClimb();
        jumpAnimationActive = true;
        rb.linearVelocity = new Vector3(0f, jumpForce, 0f);
        CurrentState = PlayerState.Jump;
    }

    private void StopLadderClimb()
    {
        if (!isClimbingLadder || rb == null)
        {
            return;
        }

        isClimbingLadder = false;
        rb.isKinematic = cachedKinematicBeforeLadder;
        rb.useGravity = cachedGravityBeforeLadder;

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    private void SnapPlayerToLadder()
    {
        if (rb == null || !HasLadderPoints())
        {
            return;
        }

        rb.position = GetSnappedLadderPosition();
    }

    private Vector3 GetSnappedLadderPosition()
    {
        Vector3 position = transform.position;
        Vector3 ladderPosition = ladderBottom.position;
        position.x = ladderPosition.x;
        position.z = ladderPosition.z;
        return position;
    }

    private Vector3 GetPlayerBottomPosition()
    {
        if (capsuleCollider != null)
        {
            return new Vector3(transform.position.x, capsuleCollider.bounds.min.y, transform.position.z);
        }

        return transform.position;
    }

    private float GetPlayerBottomOffset()
    {
        return transform.position.y - GetPlayerBottomPosition().y;
    }

    private bool IsPlayerBottomNearLadderTop()
    {
        return GetPlayerBottomPosition().y >= ladderTop.position.y - ladderTopExitThreshold;
    }

    private bool IsHorizontalExitPressed()
    {
        return Mathf.Abs(inputVector.y) > 0.1f;
    }
}
