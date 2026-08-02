using UnityEngine;
using UnityEngine.InputSystem;

public class LittleNightmareSideCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(6f, 4f, -10f);
    [SerializeField] private float stairsOffsetX = 12f;
    [SerializeField] private float ladderOffsetX = 6f;
    [SerializeField] private CrouchLockZone crouchCameraZone;
    [SerializeField] private float crouchUnderFloorOffsetY = 0f;
    [SerializeField] private float crouchUnderFloorBoundsPadding = 0.25f;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float followSharpness = 8f;
    [SerializeField] private float offsetTransitionSharpness = 4f;
    [SerializeField] private float snapDistance = 6f;
    [SerializeField] private float lookAroundDistance = 2f;
    [SerializeField] private float verticalLookAroundDistance = 2f;
    [SerializeField] private float cameraLookAroundShift = 1f;
    [SerializeField] private float lookAroundSharpness = 10f;

    private PlayerMovement playerMovement;
    private Bounds? crouchCameraFloorBounds;
    private Vector3 currentBaseOffset;
    private Vector3 currentLookAroundOffset;
    private Vector3 currentCameraLookAroundOffset;

    void Awake()
    {
        if (target != null)
        {
            playerMovement = target.GetComponent<PlayerMovement>();
        }

        CacheCrouchCameraFloorBounds();
        currentBaseOffset = offset;
        SnapToTarget();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        UpdateLookAroundOffset();
        UpdateBaseOffset();

        Vector3 desiredPosition = target.position + currentBaseOffset + currentCameraLookAroundOffset;
        if (playerMovement != null && playerMovement.IsClimbingLadder)
        {
            transform.position = desiredPosition;
        }
        else if (Vector3.Distance(transform.position, desiredPosition) > snapDistance)
        {
            transform.position = desiredPosition;
        }
        else
        {
            float followT = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followT);
        }

        Vector3 lookAtPosition = target.position + lookAtOffset + currentLookAroundOffset;
        Vector3 lookDirection = lookAtPosition - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    void UpdateLookAroundOffset()
    {
        if (playerMovement != null && playerMovement.IsClimbingLadder)
        {
            float resetT = 1f - Mathf.Exp(-lookAroundSharpness * Time.deltaTime);
            currentLookAroundOffset = Vector3.Lerp(currentLookAroundOffset, Vector3.zero, resetT);
            currentCameraLookAroundOffset = Vector3.Lerp(currentCameraLookAroundOffset, Vector3.zero, resetT);
            return;
        }

        Vector3 targetLookAroundOffset = Vector3.zero;
        Vector3 targetCameraLookAroundOffset = Vector3.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.isPressed)
            {
                targetLookAroundOffset += Vector3.forward;
                targetCameraLookAroundOffset += Vector3.forward;
            }

            if (Keyboard.current.leftArrowKey.isPressed)
            {
                targetLookAroundOffset += Vector3.back;
                targetCameraLookAroundOffset += Vector3.back;
            }

            if (Keyboard.current.upArrowKey.isPressed)
            {
                targetLookAroundOffset += Vector3.up;
                targetCameraLookAroundOffset += Vector3.up;
            }

            if (Keyboard.current.downArrowKey.isPressed)
            {
                targetLookAroundOffset += Vector3.down;
                targetCameraLookAroundOffset += Vector3.down;
            }
        }

        if (targetLookAroundOffset.sqrMagnitude > 1f)
        {
            targetLookAroundOffset.Normalize();
        }

        targetLookAroundOffset = new Vector3(
            0f,
            targetLookAroundOffset.y * verticalLookAroundDistance,
            targetLookAroundOffset.z * lookAroundDistance
        );

        targetCameraLookAroundOffset *= cameraLookAroundShift;

        float lookT = 1f - Mathf.Exp(-lookAroundSharpness * Time.deltaTime);
        currentLookAroundOffset = Vector3.Lerp(currentLookAroundOffset, targetLookAroundOffset, lookT);
        currentCameraLookAroundOffset = Vector3.Lerp(currentCameraLookAroundOffset, targetCameraLookAroundOffset, lookT);
    }

    void UpdateBaseOffset()
    {
        Vector3 targetOffset = offset;
        if (playerMovement != null && playerMovement.IsClimbingLadder)
        {
            targetOffset.x = ladderOffsetX;
            currentBaseOffset = targetOffset;
            return;
        }

        if (playerMovement != null && playerMovement.IsOnStairs && !playerMovement.IsClimbingLadder)
        {
            targetOffset.x = stairsOffsetX;
        }

        if (ShouldUseCrouchUnderFloorView())
        {
            targetOffset.y = crouchUnderFloorOffsetY;
        }

        float offsetT = 1f - Mathf.Exp(-offsetTransitionSharpness * Time.deltaTime);
        currentBaseOffset = Vector3.Lerp(currentBaseOffset, targetOffset, offsetT);
    }

    void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.position + currentBaseOffset;
        Vector3 lookAtPosition = target.position + lookAtOffset;
        Vector3 lookDirection = lookAtPosition - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    void CacheCrouchCameraFloorBounds()
    {
        if (crouchCameraZone != null)
        {
            crouchCameraFloorBounds = crouchCameraZone.Bounds;
        }
    }

    bool ShouldUseCrouchUnderFloorView()
    {
        if (target == null || playerMovement == null || !playerMovement.IsCrouching || !crouchCameraFloorBounds.HasValue)
        {
            return false;
        }

        Bounds floorBounds = crouchCameraFloorBounds.Value;
        Vector3 targetPosition = target.position;
        bool withinFloorX = targetPosition.x >= floorBounds.min.x - crouchUnderFloorBoundsPadding &&
                            targetPosition.x <= floorBounds.max.x + crouchUnderFloorBoundsPadding;
        bool withinFloorZ = targetPosition.z >= floorBounds.min.z - crouchUnderFloorBoundsPadding &&
                            targetPosition.z <= floorBounds.max.z + crouchUnderFloorBoundsPadding;
        bool belowFloor = targetPosition.y < floorBounds.center.y;

        return belowFloor && withinFloorX && withinFloorZ;
    }
}
