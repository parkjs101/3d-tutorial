using UnityEngine;

public partial class PlayerMovement
{
    [Header("Crouch Collider")]
    [SerializeField] private float crouchColliderHeight = 1.2f;
    [SerializeField] private float crouchColliderCenterY = -0.4f;
    [SerializeField] private float crouchColliderSharpness = 12f;

    [Header("Crouch Lock")]
    [SerializeField] private CrouchLockZone crouchLockZone;
    [SerializeField] private float crouchLockBoundsPadding = 0.25f;

    private CapsuleCollider capsuleCollider;
    private Bounds? crouchLockFloorBounds;
    private float standingColliderHeight;
    private Vector3 standingColliderCenter;
    private bool crouchToggled;

    public bool IsCrouching => crouchToggled;

    private void InitializeCrouch()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            standingColliderHeight = capsuleCollider.height;
            standingColliderCenter = capsuleCollider.center;
        }

        if (crouchLockZone != null)
        {
            crouchLockFloorBounds = crouchLockZone.Bounds;
        }
    }

    private void UpdateCrouchCollider(bool immediate = false)
    {
        if (capsuleCollider == null)
        {
            return;
        }

        float targetHeight = crouchToggled ? crouchColliderHeight : standingColliderHeight;
        Vector3 targetCenter = crouchToggled
            ? new Vector3(standingColliderCenter.x, crouchColliderCenterY, standingColliderCenter.z)
            : standingColliderCenter;

        if (immediate)
        {
            capsuleCollider.height = targetHeight;
            capsuleCollider.center = targetCenter;
            return;
        }

        float colliderT = 1f - Mathf.Exp(-crouchColliderSharpness * Time.fixedDeltaTime);
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, colliderT);
        capsuleCollider.center = Vector3.Lerp(capsuleCollider.center, targetCenter, colliderT);
    }

    private bool IsUnderCrouchLockFloor()
    {
        if (!crouchLockFloorBounds.HasValue)
        {
            return false;
        }

        Bounds floorBounds = crouchLockFloorBounds.Value;
        Vector3 playerPosition = transform.position;
        bool withinFloorX = playerPosition.x >= floorBounds.min.x - crouchLockBoundsPadding
            && playerPosition.x <= floorBounds.max.x + crouchLockBoundsPadding;
        bool withinFloorZ = playerPosition.z >= floorBounds.min.z - crouchLockBoundsPadding
            && playerPosition.z <= floorBounds.max.z + crouchLockBoundsPadding;
        bool belowFloor = playerPosition.y < floorBounds.center.y;

        return belowFloor && withinFloorX && withinFloorZ;
    }
}
