using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushPullBox : MonoBehaviour
{
    private const RigidbodyConstraints FreeMoveConstraints =
        RigidbodyConstraints.FreezeRotationX |
        RigidbodyConstraints.FreezeRotationY |
        RigidbodyConstraints.FreezeRotationZ;

    private const RigidbodyConstraints LockedConstraints =
        FreeMoveConstraints |
        RigidbodyConstraints.FreezePositionX |
        RigidbodyConstraints.FreezePositionZ;

    private Rigidbody rb;
    private bool isHeld;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = LockedConstraints;
    }

    public void BeginPushPull()
    {
        isHeld = true;
        rb.constraints = FreeMoveConstraints;
    }

    public void SetMoveVelocity(Vector3 velocity)
    {
        if (!isHeld)
        {
            return;
        }

        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
    }

    public void EndPushPull()
    {
        isHeld = false;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.constraints = LockedConstraints;
    }
}
