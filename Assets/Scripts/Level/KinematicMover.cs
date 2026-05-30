using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class KinematicMover : MonoBehaviour
{
    protected Rigidbody rb;

    private Vector3 previousPosition;

    public Vector3 Velocity { get; private set; }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        previousPosition = rb.position;
    }

    protected void MoveTo(Vector3 nextPosition)
    {
        Velocity = (nextPosition - previousPosition) / Time.fixedDeltaTime;
        rb.MovePosition(nextPosition);
        previousPosition = nextPosition;
    }

    protected void StopMoving()
    {
        Velocity = Vector3.zero;
        previousPosition = rb.position;
    }
}
