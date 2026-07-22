using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushPullBox : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private float followStrength = 12f;
    [SerializeField] private float maxCorrectionSpeed = 3f;

    private const RigidbodyConstraints FreeMoveConstraints =
        RigidbodyConstraints.FreezeRotationX |
        RigidbodyConstraints.FreezeRotationY |
        RigidbodyConstraints.FreezeRotationZ;

    private const RigidbodyConstraints LockedConstraints =
        FreeMoveConstraints |
        RigidbodyConstraints.FreezePositionX |
        RigidbodyConstraints.FreezePositionZ;

    private Rigidbody rb;
    private Rigidbody playerRigidbody;
    private Vector3 heldOffset;
    private bool isHeld;

    public bool IsHeld => isHeld;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = LockedConstraints;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    public void BeginPushPull(Rigidbody playerBody)
    {
        playerRigidbody = playerBody;
        if (playerRigidbody == null)
        {
            return;
        }

        heldOffset = rb.position - playerRigidbody.position;
        isHeld = true;
        rb.constraints = FreeMoveConstraints;
    }

    public void SetMoveVelocity(Vector3 velocity)
    {
        if (!isHeld)
        {
            return;
        }

        Vector3 targetPosition = playerRigidbody.position + heldOffset;
        Vector3 positionError = targetPosition - rb.position;
        positionError.y = 0f;

        Vector3 correctionVelocity = Vector3.ClampMagnitude(
            positionError * followStrength,
            maxCorrectionSpeed);
        Vector3 playerVelocity = playerRigidbody.linearVelocity;

        rb.linearVelocity = new Vector3(
            playerVelocity.x + correctionVelocity.x,
            rb.linearVelocity.y,
            playerVelocity.z + correctionVelocity.z);
    }

    public void EndPushPull()
    {
        isHeld = false;
        playerRigidbody = null;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.constraints = LockedConstraints;
    }
}
