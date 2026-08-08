using UnityEngine;

public class KnockDownForcePoint : MonoBehaviour
{
    [SerializeField] private Vector3 localForceDirection = Vector3.forward;
    [SerializeField, Min(0f)] private float forceStrength = 1.2f;

    public void ApplyTo(Rigidbody targetRigidbody)
    {
        if (targetRigidbody == null)
        {
            return;
        }

        Vector3 direction = transform.TransformDirection(localForceDirection.normalized);
        targetRigidbody.AddForceAtPosition(
            direction * forceStrength,
            transform.position,
            ForceMode.Impulse);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.08f);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(
            transform.position,
            transform.TransformDirection(localForceDirection.normalized) * forceStrength);
    }
}
