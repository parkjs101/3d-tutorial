using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CrouchLockZone : MonoBehaviour
{
    public Bounds Bounds
    {
        get
        {
            Collider zoneCollider = GetComponent<Collider>();
            return zoneCollider.bounds;
        }
    }
}
