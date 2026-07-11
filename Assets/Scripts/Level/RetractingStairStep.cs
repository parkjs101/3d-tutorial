using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RetractingStairStep : KinematicMover
{
    private const float ArrivalTolerance = 0.001f;

    public bool MoveLocalX(float targetLocalX, float speed)
    {
        if (!EnsureRigidbody())
        {
            return true;
        }

        Vector3 currentLocalPosition = transform.localPosition;
        float nextLocalX = Mathf.MoveTowards(
            currentLocalPosition.x,
            targetLocalX,
            Mathf.Max(0f, speed) * Time.fixedDeltaTime
        );

        currentLocalPosition.x = nextLocalX;
        MoveTo(GetWorldPosition(currentLocalPosition));

        bool arrived = Mathf.Abs(nextLocalX - targetLocalX) <= ArrivalTolerance;
        if (arrived)
        {
            StopMoving();
        }

        return arrived;
    }

    public void SnapLocalX(float localX)
    {
        if (!EnsureRigidbody())
        {
            return;
        }

        Vector3 localPosition = transform.localPosition;
        localPosition.x = localX;

        if (transform.parent != null)
        {
            rb.position = transform.parent.TransformPoint(localPosition);
        }
        else
        {
            rb.position = localPosition;
        }

        StopMoving();
    }

    private Vector3 GetWorldPosition(Vector3 localPosition)
    {
        return transform.parent != null ? transform.parent.TransformPoint(localPosition) : localPosition;
    }

    private bool EnsureRigidbody()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogError("RetractingStairStep requires a Rigidbody.", this);
            return false;
        }

        rb.isKinematic = true;
        return true;
    }
}
